using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GetSimulationData : MonoBehaviour
{
    // URL del endpoint Flask
    private string url = "http://127.0.0.1:5000/simulacion";

    void Start()
    {
        // Inicia la petición al servidor
        StartCoroutine(GetDataFromServer());
    }

    IEnumerator GetDataFromServer()
    {
        // Realiza una solicitud GET al endpoint
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // Envía la solicitud y espera la respuesta
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                // Imprime cualquier error
                Debug.LogError($"Error: {webRequest.error}");
            }
            else
            {
                // Procesa la respuesta JSON
                string jsonResponse = webRequest.downloadHandler.text;
                Debug.Log($"Respuesta del servidor: {jsonResponse}");

                // Si necesitas procesar el JSON, deserialízalo aquí
                ProcessJsonResponse(jsonResponse);
            }
        }
    }

    void ProcessJsonResponse(string json)
    {
        // Usa esta función para procesar el JSON recibido
        // Ejemplo simple: conviértelo en un objeto C# si es necesario
        SimulationData data = JsonUtility.FromJson<SimulationData>(json);

        // Imprime la información deserializada
        Debug.Log($"Simulation Name: {data.name}");
        Debug.Log($"Agents: {data.agents}");
        Debug.Log($"Environment: {data.environment}");
    }

    // Clase para mapear los datos del JSON
    [System.Serializable]
    public class SimulationData
    {
        public string name;
        public int agents;
        public string environment;
    }
}
