using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentController : MonoBehaviour
{
    [SerializeField] private CarSpawner carSpawner;
    [SerializeField] private PedestrianSpawner pedestrianSpawner;

    private void Start()
    {
        ValidateSpawners();
    }

    private void ValidateSpawners()
    {
        if (carSpawner == null)
        {
            Debug.LogError("CarSpawner no está asignado en AgentManager");
        }
        if (pedestrianSpawner == null)
        {
            Debug.LogError("PedestrianSpawner no está asignado en AgentManager");
        }
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
            pedestrianSpawner.StartSpawningPedestrians(pedestrianAgents);
        }
        else
        {
            Debug.Log("No se encontraron agentes tipo 'Persona' para spawnear.");
        }

        // Spawnear coches de forma progresiva
        if (carAgents.Count > 0)
        {
            
            carSpawner.StartSpawningCars(carAgents);
        }
        else
        {
            Debug.Log("No se encontraron agentes tipo 'Carro' para spawnear.");
        }
    }
}
