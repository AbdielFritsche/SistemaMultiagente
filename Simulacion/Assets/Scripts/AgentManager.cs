using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class AgentManager : MonoBehaviour
{
    public GameObject carPrefab;        // Prefab para agentes tipo "Carro"
    public GameObject pedestrianPrefab;    // Start is called before the first frame update
    public float scaleFactor = 5f;
    public float spawnInterval = 3.5f;     // Intervalo en segundos entre spawns de coches

    [Header("SpawnPoints for Cars")]
    public Transform topSideSpawn;     // Punto base del lado superior
    public Transform bottomSideSpawn;  // Punto base del lado inferior
    public Transform leftSideSpawn;    // Punto base del lado izquierdo
    public Transform rightSideSpawn;   // Punto base del lado derecho

    [Header("Objectives for Cars")]
    public Transform topSideObjective;
    public Transform bottomSideObjective;
    public Transform leftSideObjective;
    public Transform rightSideObjective;

    [Header("CenterPoints for Cars")]
    public Transform centerPointTop;
    public Transform centerPointBott;
    public Transform centerPointLeft;
    public Transform centerPointRight;

    [Header("TrafficLights")]
    public Transform trafficLightTop;
    public Transform trafficLightBottom;
    public Transform trafficLightLeft;
    public Transform trafficLightRight;

    [Header("SpawnPoints for Pedestrians")]
    public Transform topSideSpawnPedestrians;     // Punto base del lado superior
    public Transform topSideSpawnPedestrians2;     // Punto base del lado superior
    public Transform bottomSideSpawnPedestrians;  // Punto base del lado inferior
    public Transform bottomSideSpawnPedestrians2;  // Punto base del lado inferior
    public Transform leftSideSpawnPedestrians;    // Punto base del lado izquierdo
    public Transform leftSideSpawnPedestrians2;    // Punto base del lado izquierdo
    public Transform rightSideSpawnPedestrians;   // Punto base del lado derecho
    public Transform rightSideSpawnPedestrians2;   // Punto base del lado derecho

    [Header("CenterPoints for Pedestrians")]
    public Transform centerPointTopPedestrians;
    public Transform centerPointBottPedestrians;
    public Transform centerPointLeftPedestrians;
    public Transform centerPointRightPedestrians;

    [Header("Capacity Control for cars")]
    public int maxCarsPerLane = 5;           // Maximum number of cars allowed per lane
    public float laneCheckDistance = 30f;     // Distance to check for cars in each lane
    public LayerMask carLayer;               // Layer for car detection

    [Header("Capacity Control for Pedestrians")]
    public int maxPedestriansPerArea = 10;    // Máximo de peatones por área
    public float pedestrianAreaCheckRadius = 5f;
    public LayerMask pedestrianLayer;

    private Dictionary<string, List<GameObject>> pedestriansInAreas = new Dictionary<string, List<GameObject>>()
    {
        { "NorthWest", new List<GameObject>() },
        { "NorthEast", new List<GameObject>() },
        { "SouthWest", new List<GameObject>() },
        { "SouthEast", new List<GameObject>() }
    };


    // Dictionary to track cars in each lane
    private Dictionary<string, List<GameObject>> carsInLanes = new Dictionary<string, List<GameObject>>()
    {
        { "top", new List<GameObject>() },
        { "bottom", new List<GameObject>() },
        { "left", new List<GameObject>() },
        { "right", new List<GameObject>() }
    };

    private void Start()
    {
        ValidatePedestrianSpawnPoints();
    }

    private bool CheckLaneCapacity(Vector3 spawnPosition)
    {
        string lane = GetLaneIdentifier(spawnPosition);
        Vector3 checkPosition = spawnPosition;

        // Clear inactive cars from the lane list
        carsInLanes[lane].RemoveAll(car => car == null);

        // Check if we already have too many cars in this lane
        if (carsInLanes[lane].Count >= maxCarsPerLane)
        {
            Debug.Log($"Lane {lane} is at maximum capacity ({maxCarsPerLane} cars). Waiting for space...");
            return false;
        }

        // Perform physical check for cars in the lane
        Collider[] carsInRange = Physics.OverlapBox(
            checkPosition,
            new Vector3(2f, 1f, laneCheckDistance / 2),
            Quaternion.identity,
            carLayer
        );

        int activeCarsInLane = carsInRange.Length;

        if (activeCarsInLane >= maxCarsPerLane)
        {
            Debug.Log($"Physical check: Lane {lane} is crowded with {activeCarsInLane} cars. Waiting for space...");
            return false;
        }

        return true;
    }

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

    public void StartSpawn(List<AgenteData> agentList)
    {
        if (agentList == null || agentList.Count == 0)
        {
            Debug.LogWarning("La lista de agentes está vacía o es nula.");
            return;
        }

        // Separar agentes en dos listas: coches y personas
        List<AgenteData> carAgents = new List<AgenteData>();
        List<AgenteData> pedestrianAgents = new List<AgenteData>();

        foreach (AgenteData agentData in agentList)
        {
            if (agentData.type == "Carro")
            {
                carAgents.Add(agentData);
            }
            else if (agentData.type == "Persona")
            {
                pedestrianAgents.Add(agentData);
            }
            else
            {
                Debug.LogWarning($"Tipo de agente desconocido: {agentData.type}. No se spawneará.");
            }
        }

        // Spawnear personas inmediatamente
        if (pedestrianAgents.Count > 0)
        {
            StartCoroutine(SpawnPedestriansProgressively(pedestrianAgents));
        }
        else
        {
            Debug.Log("No se encontraron agentes tipo 'Persona' para spawnear.");
        }

        // Spawnear coches de forma progresiva
        if (carAgents.Count > 0)
        {
            StartCoroutine(SpawnCarsProgressively(carAgents));
        }
        else
        {
            Debug.Log("No se encontraron agentes tipo 'Carro' para spawnear.");
        }
    }
   
    private IEnumerator SpawnCarsProgressively(List<AgenteData> carAgents)
    {
        if (carAgents == null || carAgents.Count == 0)
        {
            Debug.LogWarning("No hay agentes tipo 'Carro' para spawnear progresivamente.");
            yield break;
        }

        foreach (AgenteData carAgentData in carAgents)
        {
            Vector3 spawnPoint = GetSpawnPositionFromFirstMovement(carAgentData.movements);

            // Wait until there's space in the lane
            while (!CheckLaneCapacity(spawnPoint))
            {
                yield return new WaitForSeconds(1f); // Check every second
            }

            GameObject newCar = SpawnAgent(carAgentData, carPrefab, spawnPoint);
            if (newCar != null)
            {
                // Add the car to the lane tracking
                string lane = GetLaneIdentifier(spawnPoint);
                carsInLanes[lane].Add(newCar);

                // Start a coroutine to remove the car from tracking when it's destroyed
                StartCoroutine(TrackCarDestruction(newCar, lane));
            }

            yield return new WaitForSeconds(spawnInterval);
        }

        Debug.Log("Todos los agentes tipo 'Carro' han sido spawneados progresivamente.");
    }

    private IEnumerator TrackCarDestruction(GameObject car, string lane)
    {
        yield return new WaitUntil(() => car == null);
        carsInLanes[lane].Remove(car);
        Debug.Log($"Car removed from {lane} lane tracking. Current count: {carsInLanes[lane].Count}");
    }

    private GameObject SpawnAgent(AgenteData agentData, GameObject prefab, Vector3 spawnPosition)
    {
        if (prefab == null)
        {
            Debug.LogError("El prefab proporcionado es nulo.");
            return null;
        }

        GameObject newAgent = Instantiate(prefab, spawnPosition, Quaternion.identity);

        Agente agentComponent = newAgent.GetComponent<Agente>();
        if (agentComponent == null)
        {
            agentComponent = newAgent.AddComponent<Agente>();
        }

        CarAgentController carController = newAgent.GetComponent<CarAgentController>();
        if (carController == null)
        {
            carController = newAgent.AddComponent<CarAgentController>();
        }

        // Determinar el punto intermedio basado en el spawnPosition
        Transform relevantCenterPoint = GetRelevantCenterPoint(spawnPosition);

        // Determinar el punto objetivo basado en el último movimiento
        Movimiento finalMove = agentData.movements[agentData.movements.Count - 1];
        Debug.Log($"Final Move for {agentData.type}: x={finalMove.x}, y={finalMove.y}");

        Transform objectivePoint = GetObjectivePoint(spawnPosition,finalMove);

        Transform relevantTrafficLight = GetRelevantTrafficLight(spawnPosition);


        // Configurar el controlador con el punto intermedio y objetivo final
        carController.Initialize(agentData.movements, relevantCenterPoint.position, objectivePoint.position, relevantTrafficLight);

        Debug.Log($"Spawneado agente del tipo: {agentData.type} en posición {spawnPosition}, punto intermedio: {relevantCenterPoint.name}, objetivo final: {objectivePoint.name}");

        return newAgent;
    }

    private Transform GetRandomSpawnPoint()
    {
        Transform[] spawnPoints = { topSideSpawn, bottomSideSpawn, leftSideSpawn, rightSideSpawn };
        int randomIndex = Random.Range(0, spawnPoints.Length);
        return spawnPoints[randomIndex];
    }

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

    private void OnDestroy()
    {
        // This ensures the car is properly cleaned up when it reaches its destination
        // or is otherwise destroyed
        Debug.Log($"Car {gameObject.name} destroyed and cleaned up");
    }










    private IEnumerator SpawnPedestriansProgressively(List<AgenteData> pedestrianAgents)
    {
        foreach (var pedestrianData in pedestrianAgents)
        {
            Vector3 spawnPoint = GetSpawnPositionForPedestrian(pedestrianData.movements);

            int attempts = 0;
            const int maxAttempts = 10;

            while (!CheckPedestrianAreaCapacity(spawnPoint) && attempts < maxAttempts)
            {
                attempts++;
                yield return new WaitForSeconds(1f);
            }

            if (attempts < maxAttempts)
            {
                GameObject pedestrian = SpawnPedestrian(pedestrianData, spawnPoint);
                if (pedestrian != null)
                {
                    string area = GetPedestrianAreaIdentifier(spawnPoint);
                    pedestriansInAreas[area].Add(pedestrian);
                    StartCoroutine(TrackPedestrianDestruction(pedestrian, area));
                }
            }

            yield return new WaitForSeconds(spawnInterval * 0.5f);
        }
    }
    private IEnumerator TrackPedestrianDestruction(GameObject pedestrian, string area)
    {
        yield return new WaitUntil(() => pedestrian == null || !pedestrian.activeSelf);

        if (pedestriansInAreas.ContainsKey(area) && pedestrian != null)
        {
            pedestriansInAreas[area].Remove(pedestrian);
            Debug.Log($"Peatón removido del área {area}. Quedan: {pedestriansInAreas[area].Count}");
        }
    }

    private Vector3 GetSpawnPositionForPedestrian(List<Movimiento> movements)
    {
        if (movements == null || movements.Count < 2)
        {
            Debug.LogWarning("El peatón no tiene suficientes movimientos definidos.");
            return Vector3.zero;
        }

        Vector3 direction = CalculateInitialDirection(movements[0], movements[1]);
        return SelectPedestrianSpawnPoint(direction);
    }

    private Vector3 CalculateInitialDirection(Movimiento first, Movimiento second)
    {
        return new Vector3(second.x - first.x, 0, second.y - first.y).normalized;
    }

    private Vector3 SelectPedestrianSpawnPoint(Vector3 direction)
    {
        Transform selectedSpawn;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            // Movimiento horizontal
            if (direction.x > 0)
                selectedSpawn = Random.value > 0.5f ? leftSideSpawnPedestrians : leftSideSpawnPedestrians2;
            else
                selectedSpawn = Random.value > 0.5f ? rightSideSpawnPedestrians : rightSideSpawnPedestrians2;
        }
        else
        {
            // Movimiento vertical
            if (direction.z > 0)
                selectedSpawn = Random.value > 0.5f ? bottomSideSpawnPedestrians : bottomSideSpawnPedestrians2;
            else
                selectedSpawn = Random.value > 0.5f ? topSideSpawnPedestrians : topSideSpawnPedestrians2;
        }

        return selectedSpawn.position;
    }

    private GameObject SpawnPedestrian(AgenteData pedestrianData, Vector3 spawnPoint)
    {
        if (pedestrianPrefab == null)
        {
            Debug.LogError("Prefab de peatón no asignado.");
            return null;
        }

        GameObject pedestrian = Instantiate(pedestrianPrefab, spawnPoint, Quaternion.identity);

        Agente agentComponent = pedestrian.GetComponent<Agente>();
        if (agentComponent == null)
        {
            agentComponent = pedestrian.AddComponent<Agente>();
        }

        PeatonController controller = pedestrian.GetComponent<PeatonController>();

        if (controller == null)
        {
            controller = pedestrian.AddComponent<PeatonController>();
        }

        Transform centerPoint = GetRelevantCenterPointForPedestrians(spawnPoint);
        controller.Initialize(pedestrianData.movements, centerPoint.position);

        Debug.Log($"Peatón spawneado en {spawnPoint}, dirigiéndose a {centerPoint.name}");
        return pedestrian;
    }

    private string GetPedestrianAreaIdentifier(Vector3 spawnPoint)
    {
        // Determinar en qué cuadrante/área está el punto de spawn
        if (spawnPoint.x <= 0)
        {
            if (spawnPoint.z >= 0)
                return "NorthWest";
            else
                return "SouthWest";
        }
        else
        {
            if (spawnPoint.z >= 0)
                return "NorthEast";
            else
                return "SouthEast";
        }
    }

    private void CleanupInactiveEntities<T>(List<T> entities) where T : UnityEngine.Object
    {
        entities.RemoveAll(e => e == null);
    }

    private bool CheckPedestrianAreaCapacity(Vector3 spawnPoint)
    {
        string area = GetPedestrianAreaIdentifier(spawnPoint);
        CleanupInactiveEntities(pedestriansInAreas[area]);

        if (pedestriansInAreas[area].Count >= maxPedestriansPerArea)
        {
            Debug.Log($"Área {area} llena ({maxPedestriansPerArea} peatones).");
            return false;
        }

        Collider[] pedestriansNearby = Physics.OverlapSphere(
            spawnPoint,
            pedestrianAreaCheckRadius,
            pedestrianLayer
        );

        if (pedestriansNearby.Length >= maxPedestriansPerArea)
        {
            Debug.Log($"Demasiados peatones cerca del punto de spawn en {area}.");
            return false;
        }

        return true;
    }

    private Transform GetRelevantCenterPointForPedestrians(Vector3 spawnPosition)
    {
        float tolerance = 0.1f; // Tolerancia para comparaciones de posición

        // Comprobación para spawns superiores
        if (Vector3.Distance(spawnPosition, topSideSpawnPedestrians.position) < tolerance ||
            Vector3.Distance(spawnPosition, topSideSpawnPedestrians2.position) < tolerance)
        {
            Debug.Log($"Peatón desde spawn superior, usando centerPoint: {centerPointTopPedestrians.name}");
            return centerPointTopPedestrians;
        }

        // Comprobación para spawns inferiores
        if (Vector3.Distance(spawnPosition, bottomSideSpawnPedestrians.position) < tolerance ||
            Vector3.Distance(spawnPosition, bottomSideSpawnPedestrians2.position) < tolerance)
        {
            Debug.Log($"Peatón desde spawn inferior, usando centerPoint: {centerPointBottPedestrians.name}");
            return centerPointBottPedestrians;
        }

        // Comprobación para spawns izquierdos
        if (Vector3.Distance(spawnPosition, leftSideSpawnPedestrians.position) < tolerance ||
            Vector3.Distance(spawnPosition, leftSideSpawnPedestrians2.position) < tolerance)
        {
            Debug.Log($"Peatón desde spawn izquierdo, usando centerPoint: {centerPointLeftPedestrians.name}");
            return centerPointLeftPedestrians;
        }

        // Comprobación para spawns derechos
        if (Vector3.Distance(spawnPosition, rightSideSpawnPedestrians.position) < tolerance ||
            Vector3.Distance(spawnPosition, rightSideSpawnPedestrians2.position) < tolerance)
        {
            Debug.Log($"Peatón desde spawn derecho, usando centerPoint: {centerPointRightPedestrians.name}");
            return centerPointRightPedestrians;
        }

        // Alternativa: usar el punto más cercano si no hay coincidencia exacta
        Transform[] centerPoints = {
        centerPointTopPedestrians,
        centerPointBottPedestrians,
        centerPointLeftPedestrians,
        centerPointRightPedestrians
    };

        Transform nearestCenter = centerPoints[0];
        float nearestDistance = Vector3.Distance(spawnPosition, nearestCenter.position);

        foreach (Transform center in centerPoints)
        {
            float distance = Vector3.Distance(spawnPosition, center.position);
            if (distance < nearestDistance)
            {
                nearestCenter = center;
                nearestDistance = distance;
            }
        }

        Debug.LogWarning($"No se encontró coincidencia exacta para el punto de spawn {spawnPosition}. " +
                         $"Usando el centro más cercano: {nearestCenter.name}");
        return nearestCenter;
    }

    // Método auxiliar opcional para validación
    private void ValidatePedestrianSpawnPoints()
    {
        // Lista de todos los puntos de spawn y centros que deben estar asignados
        Transform[] requiredPoints = {
        topSideSpawnPedestrians, topSideSpawnPedestrians2,
        bottomSideSpawnPedestrians, bottomSideSpawnPedestrians2,
        leftSideSpawnPedestrians, leftSideSpawnPedestrians2,
        rightSideSpawnPedestrians, rightSideSpawnPedestrians2,
        centerPointTopPedestrians,
        centerPointBottPedestrians,
        centerPointLeftPedestrians,
        centerPointRightPedestrians
    };

        foreach (Transform point in requiredPoints)
        {
            if (point == null)
            {
                Debug.LogError($"Punto de spawn o centro faltante en {gameObject.name}. " +
                              "Verifica que todos los puntos estén asignados en el Inspector.");
            }
        }
    }


}
