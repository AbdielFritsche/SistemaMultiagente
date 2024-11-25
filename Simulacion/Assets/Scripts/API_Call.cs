using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using System;
using static GetSimulationData;




public class GetSimulationData : MonoBehaviour
{
    private string url = "http://127.0.0.1:5000/simulacion";
    public AgentManager agentManager;
    public List<Agente> agentes = new List<Agente>();

    void Start()
    {
        StartCoroutine(GetDataFromServer());
    }

    IEnumerator GetDataFromServer()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error: {webRequest.error}");
            }
            else
            {
                string jsonResponse = webRequest.downloadHandler.text;
                Debug.Log($"Respuesta del servidor: {jsonResponse}");
                ProcessJsonResponse(jsonResponse);
            }
        }
    }

    void ProcessJsonResponse(string json)
    {
        var jsonObject = JSON.Parse(json);

        // Verificar si el JSON está vacío o no contiene la clave "AGENTS"
        if (jsonObject == null || !jsonObject.HasKey("AGENTS"))
        {
            Debug.LogError("El JSON no contiene la clave 'AGENTS' o es inválido.");
            return;
        }

        var agents = jsonObject["AGENTS"];

        if (agents == null || agents.Count == 0)
        {
            Debug.LogError("No se encontraron agentes en el JSON.");
            return;
        }

        // Lista de agentes para almacenar los datos procesados
        List<AgenteData> agentes = new List<AgenteData>();

        foreach (var kvp in agents)
        {
            // Validar y parsear cada agente a la clase AgenteData
            var agenteJson = kvp.Value;

            if (agenteJson == null)
            {
                Debug.LogWarning("Un agente en el JSON es nulo y será ignorado.");
                continue;
            }

            // Crear una nueva instancia de AgenteData
            AgenteData agenteData = new AgenteData
            {
                type = agenteJson["type"] != null ? agenteJson["type"] : "Desconocido",
                movements = new List<Movimiento>()
            };

            // Validar y parsear los movimientos
            if (agenteJson.HasKey("movements") && agenteJson["movements"].IsArray)
            {
                foreach (var movementJson in agenteJson["movements"].AsArray)
                {
                    if (movementJson.Value == null)
                    {
                        Debug.LogWarning("Un movimiento en el agente es nulo y será ignorado.");
                        continue;
                    }

                    Movimiento movimiento = new Movimiento
                    {
                        x = movementJson.Value.HasKey("x") ? movementJson.Value["x"].AsFloat : 0f,
                        y = movementJson.Value.HasKey("y") ? movementJson.Value["y"].AsFloat : 0f,
                        state = movementJson.Value.HasKey("state") ? movementJson.Value["state"] : "",
                        step = movementJson.Value.HasKey("step") ? movementJson.Value["step"].AsInt : 0
                    };

                    agenteData.movements.Add(movimiento);
                }
            }
            else
            {
                Debug.LogWarning($"El agente '{agenteJson["type"]}' no tiene movimientos válidos.");
            }

            // Agregar el agente procesado a la lista
            agentes.Add(agenteData);
        }

        // Iniciar el proceso de spawneo de agentes en la escena
        if (agentes.Count > 0)
        {
            agentManager.StartSpawn(agentes);
        }
        else
        {
            Debug.LogError("No se pudo procesar ningún agente válido del JSON.");
        }
    }
}

