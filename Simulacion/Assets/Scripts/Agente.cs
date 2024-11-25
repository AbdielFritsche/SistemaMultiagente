using System.Collections.Generic;
using UnityEngine;

public class Agente : MonoBehaviour
{
    public string type;
    public List<Movimiento> movements;

    public void Initialize(AgenteData data)
    {
        this.type = data.type;
        this.movements = new List<Movimiento>(data.movements);

        Debug.Log($"Agente inicializado: {type} con {movements.Count} movimientos.");
    }
}

[System.Serializable]
public class AgenteData
{
    public string type;                 // Tipo de agente
    public List<Movimiento> movements; // Movimientos del agente
}

[System.Serializable]
public class Movimiento  // Corrección de la palabra MonoBehaviour
{
    public float x;         // Coordenada X del movimiento
    public float y;         // Coordenada Y del movimiento
    public string state;    // Estado del movimiento (e.g., "walking")
    public int step;        // Paso en el movimiento
}

