using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

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

                // Procesa la respuesta JSON
                ProcessJsonResponse(jsonResponse);
            }
        }
    }

    void ProcessJsonResponse(string json)
    {
        // Analiza el JSON usando SimpleJSON
        var jsonObject = JSON.Parse(json);

        // Recorre cada agente en el JSON
        foreach (var agenteKvp in jsonObject)
        {
            // La clave del agente, por ejemplo "1690"
            string agenteId = agenteKvp.Key;

            // Nodo del agente
            var agenteNode = agenteKvp.Value["Agente"];

            // Obtén los datos básicos del agente
            string id = agenteNode["ID"];
            string tipo = agenteNode["Tipo"];

            // Obtén la lista de movimientos
            var movimientosArray = agenteNode["Movimientos"].AsArray;

            // Lista para almacenar los movimientos
            List<Movimiento> movimientos = new List<Movimiento>();

            // Itera sobre cada movimiento
            foreach (var movimientoData in movimientosArray)
            {
                Movimiento movimiento = new Movimiento
                {
                    X = movimientoData.Value["X"],                     // Posición X
                    Y = movimientoData.Value["Y"],                     // Posición Y
                    EstadoSemaforo = movimientoData.Value["EstadoSemaforo"],   // Estado del semáforo
                    Accion = movimientoData.Value["Accion"]                    // Acción
                };

                movimientos.Add(movimiento);
            }

            // Imprime los datos del agente
            Debug.Log($"Agente ID: {id}, Tipo: {tipo}");
            foreach (var movimiento in movimientos)
            {
                Debug.Log($"  X: {movimiento.X}, Y: {movimiento.Y}, Semáforo: {movimiento.EstadoSemaforo}, Acción: {movimiento.Accion}");
            }
        }
    }

    // Clases para mapear los datos del JSON
    [System.Serializable]
    public class Movimiento
    {
        public string Tipo; // Tipo de agente (e.g., "carro")
        public int X; // Posición X
        public int Y; // Posición Y
        public string EstadoSemaforo; // Estado del semáforo (e.g., "red_light")
        public string Accion; // Acción tomada (e.g., "continue")
    }

    [System.Serializable]
    public class Agente
    {
        public int Id; // ID del agente
        public List<Movimiento> Movimientos; // Lista de movimientos del agente
    }
}