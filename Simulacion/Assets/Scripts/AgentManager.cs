using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AgentManager : MonoBehaviour
{
    public GameObject carPrefab;        // Prefab para agentes tipo "Carro"
    public GameObject pedestrianPrefab;    // Start is called before the first frame update
    public float scaleFactor = 5f;
    public float spawnInterval = 2f;     // Intervalo en segundos entre spawns de coches

    public Transform topSideSpawn;     // Punto base del lado superior
    public Transform bottomSideSpawn;  // Punto base del lado inferior
    public Transform leftSideSpawn;    // Punto base del lado izquierdo
    public Transform rightSideSpawn;   // Punto base del lado derecho
    public Transform topSideObjective;
    public Transform bottomSideObjective;
    public Transform leftSideObjective;
    public Transform rightSideObjective;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartSpawn(List<AgenteData> agentList)
    {
        // Separar agentes en dos listas: coches y personas
        List<AgenteData> carAgents = new List<AgenteData>();
        List<AgenteData> pedestrianAgents = new List<AgenteData>();

        foreach (AgenteData agentData in agentList)
        {
            if (agentData.type == "Carro")
                carAgents.Add(agentData);
            else if (agentData.type == "Persona")
                pedestrianAgents.Add(agentData);
            else
                Debug.LogWarning($"Tipo de agente desconocido: {agentData.type}. No se spawneará.");
        }

        // Spawnear personas inmediatamente
        foreach (AgenteData pedestrianData in pedestrianAgents)
        {
            Vector3 spawnPosition = GetSpawnPositionFromFirstMovement(pedestrianData.movements);
            SpawnAgent(pedestrianData, pedestrianPrefab, spawnPosition);
        }

        // Spawnear coches de forma progresiva
        StartCoroutine(SpawnCarsProgressively(carAgents));
    }


    private IEnumerator SpawnCarsProgressively(List<AgenteData> carAgents)
    {
        foreach (AgenteData carAgentData in carAgents)
        {
            // Elegir un punto de spawn al azar para el coche
            Vector3 spawnPoint = GetSpawnPositionFromFirstMovement(carAgentData.movements);

            // Spawnear el coche
            SpawnAgent(carAgentData, carPrefab, spawnPoint);

            // Esperar antes de spawnear el siguiente coche
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnAgent(AgenteData agentData, GameObject prefab, Vector3 spawnPosition)
    {
        // Verificar que el prefab no sea nulo
        if (prefab == null)
        {
            Debug.LogError("El prefab proporcionado es nulo.");
            return;
        }

        // Instanciar el prefab en la posición deseada
        GameObject newAgent = Instantiate(prefab, spawnPosition, Quaternion.identity);

        // Verificar si tiene el componente Agente, y agregarlo si no lo tiene
        Agente agentComponent = newAgent.GetComponent<Agente>();
        if (agentComponent == null)
        {
            agentComponent = newAgent.AddComponent<Agente>(); // Agregar dinámicamente el componente Agente
        }
        CarAgentController carController = newAgent.GetComponent<CarAgentController>();
        if (agentComponent == null)
        {
            carController = newAgent.AddComponent<CarAgentController>(); // Agregar dinámicamente el componente Agente
        }
        // Inicializar el agente con los datos de AgenteData
        agentComponent.Initialize(agentData);

        carController.GetMovements(agentData.movements);

        Debug.Log($"Spawneado agente del tipo: {agentData.type} en posición {spawnPosition}");
    }

    private Transform GetRandomSpawnPoint()
    {
        Transform[] spawnPoints = { topSideSpawn, bottomSideSpawn, leftSideSpawn, rightSideSpawn };
        int randomIndex = Random.Range(0, spawnPoints.Length);
        return spawnPoints[randomIndex];
    }

    private Vector3 GetSpawnPositionFromFirstMovement(List<Movimiento> movements)
    {
        if (movements == null || movements.Count == 0)
        {
            Debug.LogWarning("El agente no tiene movimientos definidos. Se usará Vector3.zero como posición inicial.");
            return Vector3.zero;
        }
        
        Transform[] spawnPoints = { topSideSpawn, bottomSideSpawn, leftSideSpawn, rightSideSpawn };
        // Tomar el primer movimiento del agente
        Movimiento firstMove = movements[0];
        Movimiento secondMove = movements[1];

        if(secondMove.x < firstMove.x && secondMove.y == firstMove.y)
        {
            return rightSideSpawn.position;
        }
        else if(secondMove.x > firstMove.x && secondMove.y == firstMove.y)
        {
            return leftSideSpawn.position;
        }
        if(secondMove.y < firstMove.y && secondMove.x == firstMove.x)
        {
            return bottomSideSpawn.position;
        }
        else if(secondMove.y > firstMove.y && secondMove.x == firstMove.x)
        {
            return topSideSpawn.position;
        }
        // Lista de puntos de spawn
        return Vector3.zero;

    }
}
