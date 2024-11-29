using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianSpawner : BaseSpawner
{
    [Header("Pedestrian Prefab")]
    public GameObject pedestrianPrefab;

    [Header("Spawn Points")]
    public Transform topSideSpawnPedestrians;
    public Transform topSideSpawnPedestrians2;
    public Transform bottomSideSpawnPedestrians;
    public Transform bottomSideSpawnPedestrians2;
    public Transform leftSideSpawnPedestrians;
    public Transform leftSideSpawnPedestrians2;
    public Transform rightSideSpawnPedestrians;
    public Transform rightSideSpawnPedestrians2;

    [Header("Center Points")]
    public Transform centerPointTopPedestrians;
    public Transform centerPointBottPedestrians;
    public Transform centerPointLeftPedestrians;
    public Transform centerPointRightPedestrians;

    [Header("Corner Points")]
    public Transform topLeftCorner;
    public Transform topRightCorner;
    public Transform bottomLeftCorner;
    public Transform bottomRightCorner;

    [Header("Capacity Control")]
    public int maxPedestriansPerArea = 10;
    public float pedestrianAreaCheckRadius = 5f;
    public LayerMask pedestrianLayer;

    private Dictionary<string, List<GameObject>> pedestriansInAreas;

    protected override void Start()
    {
        base.Start();
        InitializePedestrianAreas();
        ValidatePedestrianSpawnPoints();
    }

    private void InitializePedestrianAreas()
    {
        pedestriansInAreas = new Dictionary<string, List<GameObject>>()
        {
            { "NorthWest", new List<GameObject>() },
            { "NorthEast", new List<GameObject>() },
            { "SouthWest", new List<GameObject>() },
            { "SouthEast", new List<GameObject>() }
        };
    }

    private Transform GetTransformForPosition(Vector3 position)
    {
        // Asume que tienes una lista de posibles puntos de spawn como Transforms
        Transform[] possiblePoints = { topSideSpawnPedestrians,topSideSpawnPedestrians2,
                                       bottomSideSpawnPedestrians,bottomSideSpawnPedestrians2,
                                       leftSideSpawnPedestrians,leftSideSpawnPedestrians2,
                                       rightSideSpawnPedestrians,rightSideSpawnPedestrians2 
                                      };

        foreach (Transform point in possiblePoints)
        {
            if (Vector3.Distance(position, point.position) < 0.1f) // Comparación con tolerancia
            {
                return point;
            }
        }

        Debug.LogWarning($"No se encontró un Transform para la posición {position}");
        return null;
    }

    public void StartSpawningPedestrians(List<AgenteData> pedestrianAgents)
    {
        if (pedestrianAgents == null || pedestrianAgents.Count == 0)
        {
            Debug.LogWarning("No hay agentes tipo 'Persona' para spawnear.");
            return;
        }

        StartCoroutine(SpawnPedestriansProgressively(pedestrianAgents));
    }

    private IEnumerator SpawnPedestriansProgressively(List<AgenteData> pedestrianAgents)
    {
        List<Vector3> availableSpawnPoints = SelectPedestrianSpawnPoints();

        foreach (var pedestrianData in pedestrianAgents)
        {
            Vector3 spawnPoint = GetSpawnPositionForPedestrian(pedestrianData.movements, availableSpawnPoints);
            Transform start = GetTransformForPosition(spawnPoint);
            Transform end = GetObjectivePointForPedestrian(spawnPoint);

            if (start != null && end != null)
            {
                int attempts = 0;
                while (!CheckPedestrianAreaCapacity(spawnPoint) && attempts < 10)
                {
                    attempts++;
                    yield return new WaitForSeconds(1f);
                }

                if (attempts < 10)
                {
                    GameObject pedestrian = SpawnPedestrian(pedestrianData, start, end);
                    if (pedestrian != null)
                    {
                        string area = GetPedestrianAreaIdentifier(spawnPoint);
                        pedestriansInAreas[area].Add(pedestrian);
                        StartCoroutine(TrackPedestrianDestruction(pedestrian, area));
                    }
                }
            }

            yield return new WaitForSeconds(spawnInterval * 0.5f);
        }
    }

    private GameObject SpawnPedestrian(AgenteData pedestrianData, Transform start, Transform end)
    {
        GameObject pedestrian = Instantiate(pedestrianPrefab, start.position, Quaternion.identity);
        PeatonController controller = pedestrian.GetComponent<PeatonController>();

        if (controller == null)
        {
            controller = pedestrian.AddComponent<PeatonController>();
        }

        Transform centerPoint = GetRelevantCenterPointForPedestrians(start.position);
        List<Transform> path = mapManager.GetPath(start, end);

        controller.SetCrossingPoints(GetCrossingPoints());
        controller.Initialize(pedestrianData.movements, centerPoint.position, end.position);
        controller.InitializePath(path);

        return pedestrian;
    }

    private Dictionary<string, Transform> GetCrossingPoints()
    {
        return new Dictionary<string, Transform>
        {
            { "TopLeft", topLeftCorner },
            { "TopRight", topRightCorner },
            { "BottomLeft", bottomLeftCorner },
            { "BottomRight", bottomRightCorner }
        };
    }

    private IEnumerator TrackPedestrianDestruction(GameObject pedestrian, string area)
    {
        yield return new WaitUntil(() => pedestrian == null);
        if (pedestriansInAreas.ContainsKey(area))
        {
            pedestriansInAreas[area].Remove(pedestrian);
        }
    }

    private Vector3 GetSpawnPositionForPedestrian(List<Movimiento> movements, List<Vector3> availableSpawnPoints)
    {
        if (movements == null || movements.Count < 2)
        {
            Debug.LogWarning("El peatón no tiene suficientes movimientos definidos.");
            return Vector3.zero;
        }

        if (availableSpawnPoints == null || availableSpawnPoints.Count == 0)
        {
            Debug.LogWarning("No hay puntos de spawn disponibles.");
            return Vector3.zero;
        }

        // Selecciona el primer punto disponible y lo elimina de la lista
        Vector3 selectedSpawnPoint = availableSpawnPoints[0];
        availableSpawnPoints.RemoveAt(0);

        return selectedSpawnPoint;
    }

    // ValidatePedestrianSpawnPoints,
    private void ValidatePedestrianSpawnPoints()
    {
        // Lista de todos los puntos de spawn y centros que deben estar asignados
        Transform[] requiredPoints = {
        topSideSpawnPedestrians, topSideSpawnPedestrians2,
        bottomSideSpawnPedestrians, bottomSideSpawnPedestrians2,
        leftSideSpawnPedestrians, leftSideSpawnPedestrians2,
        rightSideSpawnPedestrians, rightSideSpawnPedestrians2,
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

    // SelectPedestrianSpawnPoints,
    private List<Vector3> SelectPedestrianSpawnPoints()
    {
        // Crea listas para cada lado
        Transform[] leftSideSpawns = new Transform[] { leftSideSpawnPedestrians, leftSideSpawnPedestrians2 };
        Transform[] rightSideSpawns = new Transform[] { rightSideSpawnPedestrians, rightSideSpawnPedestrians2 };
        Transform[] bottomSideSpawns = new Transform[] { bottomSideSpawnPedestrians, bottomSideSpawnPedestrians2 };
        Transform[] topSideSpawns = new Transform[] { topSideSpawnPedestrians, topSideSpawnPedestrians2 };

        // Lista para almacenar los puntos seleccionados
        List<Vector3> selectedSpawnPoints = new List<Vector3>();

        // Asegúrate de que todos los puntos son válidos antes de agregarlos
        foreach (var spawn in leftSideSpawns)
        {
            if (spawn != null) selectedSpawnPoints.Add(spawn.position);
        }

        foreach (var spawn in rightSideSpawns)
        {
            if (spawn != null) selectedSpawnPoints.Add(spawn.position);
        }

        foreach (var spawn in bottomSideSpawns)
        {
            if (spawn != null) selectedSpawnPoints.Add(spawn.position);
        }

        foreach (var spawn in topSideSpawns)
        {
            if (spawn != null) selectedSpawnPoints.Add(spawn.position);
        }

        if (selectedSpawnPoints.Count == 0)
        {
            Debug.LogError("No se encontraron puntos de spawn válidos.");
        }

        return selectedSpawnPoints;
    }
    // GetPedestrianAreaIdentifier,
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

    // CheckPedestrianAreaCapacity,
    private bool CheckPedestrianAreaCapacity(Vector3 spawnPoint)
    {
        string area = GetPedestrianAreaIdentifier(spawnPoint);

        // Verifica que el área exista en el diccionario
        if (!pedestriansInAreas.ContainsKey(area))
        {
            Debug.LogError($"El área {area} no existe en el diccionario de áreas. Revisa tu configuración.");
            return false;
        }

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
    // GetRelevantCenterPointForPedestrians,
    private Transform GetRelevantCenterPointForPedestrians(Vector3 spawnPosition)
    {
        // Diccionario para mapear puntos de spawn a sus centros correspondientes
        Dictionary<Transform, Transform> spawnToCenterMap = new Dictionary<Transform, Transform>
    {
        { topSideSpawnPedestrians2, centerPointTopPedestrians },
        { leftSideSpawnPedestrians2, centerPointTopPedestrians },
        { topSideSpawnPedestrians, centerPointRightPedestrians },
        { rightSideSpawnPedestrians, centerPointRightPedestrians },
        { rightSideSpawnPedestrians2, centerPointBottPedestrians },
        { bottomSideSpawnPedestrians, centerPointBottPedestrians },
        { bottomSideSpawnPedestrians2, centerPointLeftPedestrians },
        { leftSideSpawnPedestrians, centerPointLeftPedestrians }
    };

        // Busca el punto de spawn en el diccionario
        foreach (var entry in spawnToCenterMap)
        {
            if (Vector3.Distance(spawnPosition, entry.Key.position) < 0.1f) // Tolerancia para evitar discrepancias menores
            {
                Debug.Log($"Peatón desde {entry.Key.name}, usando centerPoint: {entry.Value.name}");
                return entry.Value;
            }
        }

        // Si no se encuentra coincidencia exacta, lanza una advertencia o toma acción alternativa
        Debug.LogWarning($"No se encontró coincidencia para el punto de spawn {spawnPosition}. " +
                         "Considera revisar las posiciones de spawn.");
        return leftSideSpawnPedestrians2;
    }
    // GetObjectivePointForPedestrian 
    private Transform GetObjectivePointForPedestrian(Vector3 spawnPosition)
    {
        float tolerance = 0.1f; // Rango de tolerancia para comparar posiciones flotantes

        // Lista de todos los destinos posibles
        List<Transform> allDestinations = new List<Transform>
        {
            topSideSpawnPedestrians,
            topSideSpawnPedestrians2,
            bottomSideSpawnPedestrians,
            bottomSideSpawnPedestrians2,
            leftSideSpawnPedestrians,
            leftSideSpawnPedestrians2,
            rightSideSpawnPedestrians,
            rightSideSpawnPedestrians2
        };

        // Determina el tipo del spawn actual y excluye los puntos del mismo tipo
        if (Vector3.Distance(spawnPosition, topSideSpawnPedestrians.position) < tolerance || Vector3.Distance(spawnPosition, topSideSpawnPedestrians2.position) < tolerance)
        {
            // Spawn en la zona superior, excluye top destinos
            allDestinations.Remove(topSideSpawnPedestrians);
            allDestinations.Remove(topSideSpawnPedestrians2);
        }
        else if (Vector3.Distance(spawnPosition, bottomSideSpawnPedestrians.position) < tolerance || Vector3.Distance(spawnPosition, bottomSideSpawnPedestrians2.position) < tolerance)
        {
            // Spawn en la zona inferior, excluye bottom destinos
            allDestinations.Remove(bottomSideSpawnPedestrians);
            allDestinations.Remove(bottomSideSpawnPedestrians2);
        }
        else if (Vector3.Distance(spawnPosition, leftSideSpawnPedestrians.position) < tolerance || Vector3.Distance(spawnPosition, leftSideSpawnPedestrians2.position) < tolerance)
        {
            // Spawn en la zona izquierda, excluye left destinos
            allDestinations.Remove(leftSideSpawnPedestrians);
            allDestinations.Remove(leftSideSpawnPedestrians2);
        }
        else if (Vector3.Distance(spawnPosition, rightSideSpawnPedestrians.position) < tolerance || Vector3.Distance(spawnPosition, rightSideSpawnPedestrians2.position) < tolerance)
        {
            // Spawn en la zona derecha, excluye right destinos
            allDestinations.Remove(rightSideSpawnPedestrians);
            allDestinations.Remove(rightSideSpawnPedestrians2);
        }

        // Selecciona un destino aleatorio de los destinos restantes
        if (allDestinations.Count > 0)
        {
            Transform randomDestination = allDestinations[Random.Range(0, allDestinations.Count)];
            Debug.Log($"Peatón desde {spawnPosition} tiene como destino: {randomDestination.name}");
            return randomDestination;
        }

        // Si no hay destinos válidos, lanza una advertencia y usa un destino predeterminado
        Debug.LogWarning($"No se pudo determinar un destino válido para spawnPosition: {spawnPosition}. Usando un destino predeterminado.");
        return topSideSpawnPedestrians; // Por defecto
    }
}
