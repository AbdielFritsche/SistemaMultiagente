using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpawner : BaseSpawner
{
    [Header("Car Prefab")]
    public GameObject carPrefab;

    [Header("Spawn Points")]
    public Transform topSideSpawn;
    public Transform bottomSideSpawn;
    public Transform leftSideSpawn;
    public Transform rightSideSpawn;

    [Header("Objectives")]
    public Transform topSideObjective;
    public Transform bottomSideObjective;
    public Transform leftSideObjective;
    public Transform rightSideObjective;

    [Header("Center Points")]
    public Transform centerPointTop;
    public Transform centerPointBott;
    public Transform centerPointLeft;
    public Transform centerPointRight;

    [Header("Traffic Lights")]
    public Transform trafficLightTop;
    public Transform trafficLightBottom;
    public Transform trafficLightLeft;
    public Transform trafficLightRight;

    [Header("Capacity Control")]
    public int maxCarsPerLane = 5;
    public float laneCheckDistance = 30f;
    public LayerMask carLayer;

    private Dictionary<string, List<GameObject>> carsInLanes;

    protected override void Start()
    {
        base.Start();
        InitializeCarsInLanes();
    }

    private void InitializeCarsInLanes()
    {
        carsInLanes = new Dictionary<string, List<GameObject>>()
        {
            { "top", new List<GameObject>() },
            { "bottom", new List<GameObject>() },
            { "left", new List<GameObject>() },
            { "right", new List<GameObject>() }
        };
    }

    public void StartSpawningCars(List<AgenteData> carAgents)
    {
        if (carAgents == null || carAgents.Count == 0)
        {
            Debug.LogWarning("No hay agentes tipo 'Carro' para spawnear.");
            return;
        }

        StartCoroutine(SpawnCarsProgressively(carAgents));
    }

    private IEnumerator SpawnCarsProgressively(List<AgenteData> carAgents)
    {
        foreach (AgenteData carAgentData in carAgents)
        {
            Vector3 spawnPoint = GetSpawnPositionFromFirstMovement(carAgentData.movements);

            while (!CheckLaneCapacity(spawnPoint))
            {
                yield return new WaitForSeconds(1f);
            }

            GameObject newCar = SpawnCar(carAgentData, spawnPoint);
            if (newCar != null)
            {
                string lane = GetLaneIdentifier(spawnPoint);
                carsInLanes[lane].Add(newCar);
                StartCoroutine(TrackCarDestruction(newCar, lane));
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private GameObject SpawnCar(AgenteData agentData, Vector3 spawnPosition)
    {
        GameObject newCar = Instantiate(carPrefab, spawnPosition, Quaternion.identity);

        CarAgentController carController = newCar.GetComponent<CarAgentController>();
        if (carController == null)
        {
            carController = newCar.AddComponent<CarAgentController>();
        }

        Transform relevantCenterPoint = GetRelevantCenterPoint(spawnPosition);
        Transform objectivePoint = GetObjectivePoint(spawnPosition, agentData.movements[agentData.movements.Count - 1]);
        Transform relevantTrafficLight = GetRelevantTrafficLight(spawnPosition);

        carController.Initialize(agentData.movements, relevantCenterPoint.position,
                               objectivePoint.position, relevantTrafficLight);

        return newCar;
    }

    private bool CheckLaneCapacity(Vector3 spawnPosition)
    {
        string lane = GetLaneIdentifier(spawnPosition);
        CleanupInactiveEntities(carsInLanes[lane]);

        if (carsInLanes[lane].Count >= maxCarsPerLane)
        {
            return false;
        }

        Collider[] carsInRange = Physics.OverlapBox(
            spawnPosition,
            new Vector3(2f, 1f, laneCheckDistance / 2),
            Quaternion.identity,
            carLayer
        );

        return carsInRange.Length < maxCarsPerLane;
    }

    private IEnumerator TrackCarDestruction(GameObject car, string lane)
    {
        yield return new WaitUntil(() => car == null);
        if (carsInLanes.ContainsKey(lane))
        {
            carsInLanes[lane].Remove(car);
        }
    }

    // [Aquí irían los métodos GetLaneIdentifier,
    private string GetLaneIdentifier(Vector3 spawnPosition)
    {
        if (Vector3.Distance(spawnPosition, topSideSpawn.position) < 0.1f)
            return "top";
        else if (Vector3.Distance(spawnPosition, bottomSideSpawn.position) < 0.1f)
            return "bottom";
        else if (Vector3.Distance(spawnPosition, leftSideSpawn.position) < 0.1f)
            return "left";
        else
            return "right";
    }
    // GetSpawnPositionFromFirstMovement, 
    private Vector3 GetSpawnPositionFromFirstMovement(List<Movimiento> movements)
    {
        if (movements == null || movements.Count < 2)
        {
            Debug.LogWarning("El agente no tiene movimientos suficientes definidos. Se usará Vector3.zero como posición inicial.");
            return Vector3.zero;
        }

        // Calcular el punto inicial y la dirección para decidir el spawn
        Movimiento firstMove = movements[0];
        Movimiento secondMove = movements[1];
        Vector3 direction = new Vector3(secondMove.x - firstMove.x, 0, secondMove.y - firstMove.y).normalized;

        // Elegir el punto de spawn más cercano
        Transform closestSpawnPoint;
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            closestSpawnPoint = direction.x > 0 ? rightSideSpawn : leftSideSpawn;
        }
        else
        {
            closestSpawnPoint = direction.z > 0 ? topSideSpawn : bottomSideSpawn;
        }

        return closestSpawnPoint.position;
    }
    // GetRelevantCenterPoint,
    private Transform GetRelevantCenterPoint(Vector3 spawnPosition)
    {
        // Asociar cada punto de spawn con su punto intermedio correspondiente
        if (spawnPosition == topSideSpawn.position)
            return centerPointTop;
        else if (spawnPosition == bottomSideSpawn.position)
            return centerPointBott;
        else if (spawnPosition == leftSideSpawn.position)
            return centerPointLeft;
        else if (spawnPosition == rightSideSpawn.position)
            return centerPointRight;

        // Si no se encuentra un punto intermedio, usar un predeterminado
        Debug.LogWarning("No se pudo encontrar un punto intermedio para el punto de spawn dado. Usando punto intermedio por defecto.");
        return centerPointTop;
    }
    // GetObjectivePoint
    private Transform GetObjectivePoint(Vector3 spawnPosition, Movimiento finalMove)
    {
        float tolerance = 0.1f; // Rango de tolerancia para comparar posiciones flotantes

        // Comprobar de dónde viene el coche basándonos en el spawn
        if (Vector3.Distance(spawnPosition, topSideSpawn.position) < tolerance)
        {
            // Si el coche viene del spawn superior
            if (finalMove.y > 0) // Sigue recto hacia el norte
                return bottomSideObjective;
            else // Gira a la derecha hacia el este
                return rightSideObjective;
        }
        else if (Vector3.Distance(spawnPosition, bottomSideSpawn.position) < tolerance)
        {
            // Si el coche viene del spawn inferior
            if (finalMove.y < 0) // Sigue recto hacia el sur
                return topSideObjective;
            else // Gira a la derecha hacia el oeste
                return leftSideObjective;
        }
        else if (Vector3.Distance(spawnPosition, leftSideSpawn.position) < tolerance)
        {
            // Si el coche viene del spawn izquierdo
            if (finalMove.x > 0) // Sigue recto hacia el este
                return rightSideObjective;
            else // Gira a la derecha hacia el norte
                return topSideObjective;
        }
        else if (Vector3.Distance(spawnPosition, rightSideSpawn.position) < tolerance)
        {
            // Si el coche viene del spawn derecho
            if (finalMove.x < 0) // Sigue recto hacia el oeste
                return leftSideObjective;
            else // Gira a la derecha hacia el sur
                return bottomSideObjective;
        }

        // Si no se encuentra un objetivo válido
        Debug.LogWarning($"No se pudo determinar el objetivo para spawnPosition: {spawnPosition} y finalMove: ({finalMove.x}, {finalMove.y}). Usando un objetivo predeterminado.");
        return topSideObjective; // Por defecto
    }
    // GetRelevantTrafficLight que ya tienes]
    private Transform GetRelevantTrafficLight(Vector3 spawnPosition)
    {
        if (Vector3.Distance(spawnPosition, topSideSpawn.position) < 0.1f)
        {
            // Retorna el semáforo correcto para el carril superior
            return trafficLightTop;
        }
        else if (Vector3.Distance(spawnPosition, bottomSideSpawn.position) < 0.1f)
        {
            // Retorna el semáforo correcto para el carril inferior
            return trafficLightBottom;
        }
        else if (Vector3.Distance(spawnPosition, leftSideSpawn.position) < 0.1f)
        {
            // Retorna el semáforo correcto para el carril izquierdo
            return trafficLightLeft;
        }
        else if (Vector3.Distance(spawnPosition, rightSideSpawn.position) < 0.1f)
        {
            // Retorna el semáforo correcto para el carril derecho
            return trafficLightRight;
        }

        Debug.LogWarning("No se encontró un semáforo relevante para el spawn position.");
        return trafficLightTop; // Por defecto
    }
}