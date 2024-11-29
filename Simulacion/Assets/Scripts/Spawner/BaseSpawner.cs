using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseSpawner : MonoBehaviour
{
    [Header("Common Settings")]
    public float spawnInterval = 3.5f;
    protected MapManager mapManager;

    protected virtual void Start()
    {
        mapManager = FindObjectOfType<MapManager>();
        if (mapManager == null)
        {
            Debug.LogError("MapManager no encontrado en la escena");
        }
    }

    protected void CleanupInactiveEntities<T>(List<T> entities) where T : UnityEngine.Object
    {
        entities.RemoveAll(e => e == null);
    }

    protected virtual Transform GetTransformForPosition(Vector3 position, Transform[] possiblePoints, float tolerance = 0.1f)
    {
        foreach (Transform point in possiblePoints)
        {
            if (Vector3.Distance(position, point.position) < tolerance)
            {
                return point;
            }
        }
        Debug.LogWarning($"No se encontró un Transform para la posición {position}");
        return null;
    }

    protected virtual void ValidateRequiredComponents()
    {
        if (mapManager == null)
        {
            Debug.LogError($"MapManager no asignado en {gameObject.name}");
        }
    }

    protected virtual void OnDestroy()
    {
        // Cleanup cuando el spawner es destruido
        Debug.Log($"Spawner {gameObject.name} destruido y limpiado");
    }
}
