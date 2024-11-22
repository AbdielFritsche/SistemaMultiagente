using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

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
        // Deserializa el JSON en un diccionario
        var objetos = JsonConvert.DeserializeObject<Dictionary<string, List<List<object>>>>(json);

        // Convierte el diccionario en una lista de objetos con sus movimientos
        List<ObjetoMovil> objetosMoviles = new List<ObjetoMovil>();

        foreach (var objeto in objetos)
        {
            int id = int.Parse(objeto.Key);
            List<Movimiento> movimientos = new List<Movimiento>();

            foreach (var movimientoData in objeto.Value)
            {
                Movimiento movimiento = new Movimiento
                {
                    Tipo = (string)movimientoData[0],
                    X = Convert.ToDouble(movimientoData[1]),
                    Y = Convert.ToDouble(movimientoData[2]),
                    Estado = (string)movimientoData[3],
                    Id = Convert.ToInt32(movimientoData[4]),
                    Paso = Convert.ToInt32(movimientoData[5])
                };
                movimientos.Add(movimiento);
            }

            objetosMoviles.Add(new ObjetoMovil { Id = id, Movimientos = movimientos });
        }

        // Ejemplo de salida: imprime los movimientos de cada objeto
        foreach (var objeto in objetosMoviles)
        {
            Debug.Log($"Objeto ID: {objeto.Id}");
            foreach (var movimiento in objeto.Movimientos)
            {
                Debug.Log($"  Tipo: {movimiento.Tipo}, X: {movimiento.X}, Y: {movimiento.Y}, Estado: {movimiento.Estado}, Id: {movimiento.Id}, Paso: {movimiento.Paso}");
            }
        }
    }

    // Clases para mapear los datos del JSON
    [System.Serializable]
    public class Movimiento
    {
        public string Tipo; // "Carro" o "Persona"
        public double X;     // self.x
        public double Y;     // self.y
        public string Estado; // state
        public int Id;       // self.id
        public int Paso;     // self.step
    }

    [System.Serializable]
    public class ObjetoMovil
    {
        public int Id; // ID del objeto
        public List<Movimiento> Movimientos; // Lista de movimientos del objeto
    }
}
