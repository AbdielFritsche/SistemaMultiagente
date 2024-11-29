using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("Corner Points")]
    public Transform topLeftCorner;
    public Transform topRightCorner;
    public Transform bottomLeftCorner;
    public Transform bottomRightCorner;

    [Header("Top Spawn Points")]
    public Transform topSideSpawnPedestrians;
    public Transform topSideSpawnPedestrians2;

    [Header("Bottom Spawn Points")]
    public Transform bottomSideSpawnPedestrians;
    public Transform bottomSideSpawnPedestrians2;

    [Header("Left Spawn Points")]
    public Transform leftSideSpawnPedestrians;
    public Transform leftSideSpawnPedestrians2;

    [Header("Right Spawn Points")]
    public Transform rightSideSpawnPedestrians;
    public Transform rightSideSpawnPedestrians2;

    [Header("Path Settings")]
    [SerializeField] private bool visualizeGraph = true;
    [SerializeField] private Color safePathColor = Color.green;
    [SerializeField] private Color dangerousPathColor = Color.red;

    private Graph graph;
    private Dictionary<Transform, List<Transform>> validConnections;

    private void Awake()
    {
        ValidateComponents();
        InitializeGraph();
    }

    private void ValidateComponents()
    {
        Transform[] requiredPoints = {
            topLeftCorner, topRightCorner, bottomLeftCorner, bottomRightCorner,
            topSideSpawnPedestrians, topSideSpawnPedestrians2,
            bottomSideSpawnPedestrians, bottomSideSpawnPedestrians2,
            leftSideSpawnPedestrians, leftSideSpawnPedestrians2,
            rightSideSpawnPedestrians, rightSideSpawnPedestrians2
        };

        foreach (Transform point in requiredPoints)
        {
            if (point == null)
            {
                Debug.LogError($"Punto faltante en {gameObject.name}. Verifica todas las referencias en el Inspector.");
                return;
            }
        }
    }

    private void InitializeGraph()
    {
        graph = new Graph();
        InitializeNodes();
        CreateConnections();
        if (visualizeGraph)
        {
            graph.PrintGraph();
        }
    }

    private void InitializeNodes()
    {
        Transform[] allNodes = {
            topLeftCorner, topRightCorner, bottomLeftCorner, bottomRightCorner,
            topSideSpawnPedestrians, topSideSpawnPedestrians2,
            bottomSideSpawnPedestrians, bottomSideSpawnPedestrians2,
            leftSideSpawnPedestrians, leftSideSpawnPedestrians2,
            rightSideSpawnPedestrians, rightSideSpawnPedestrians2
        };

        foreach (Transform node in allNodes)
        {
            graph.AddNode(node);
        }
    }

    private void CreateConnections()
    {
        // Conexiones centrales (cruce principal)
        CreateCrossingConnections();

        // Conexiones desde puntos de spawn a sus esquinas más cercanas
        CreateSpawnToCornerConnections();
    }

    private void CreateCrossingConnections()
    {
        // Solo conexiones horizontales y verticales en el cruce central
        graph.AddEdge(topLeftCorner, topRightCorner, true);     // Cruce superior
        graph.AddEdge(bottomLeftCorner, bottomRightCorner, true); // Cruce inferior
        graph.AddEdge(topLeftCorner, bottomLeftCorner, true);    // Cruce izquierdo
        graph.AddEdge(topRightCorner, bottomRightCorner, true);  // Cruce derecho
    }

    private void CreateSpawnToCornerConnections()
    {
        // Conexiones del lado superior
        graph.AddEdge(topSideSpawnPedestrians, topRightCorner);
        graph.AddEdge(topSideSpawnPedestrians2, topLeftCorner);

        // Conexiones del lado inferior
        graph.AddEdge(bottomSideSpawnPedestrians, bottomRightCorner);
        graph.AddEdge(bottomSideSpawnPedestrians2, bottomLeftCorner);

        // Conexiones del lado izquierdo
        graph.AddEdge(leftSideSpawnPedestrians, bottomLeftCorner);
        graph.AddEdge(leftSideSpawnPedestrians2, topLeftCorner);

        // Conexiones del lado derecho
        graph.AddEdge(rightSideSpawnPedestrians, topRightCorner);
        graph.AddEdge(rightSideSpawnPedestrians2, bottomRightCorner);
    }

    public List<Transform> GetPath(Transform start, Transform end)
    {
        if (start == null || end == null)
        {
            Debug.LogError("Punto inicial o final nulo");
            return new List<Transform>();
        }

        List<Transform> path = graph.FindShortestPath(start, end);

        if (path.Count == 0)
        {
            Debug.LogWarning($"No se encontró ruta de {start.name} a {end.name}");
        }
        else if (visualizeGraph)
        {
            Debug.Log($"Ruta encontrada de {start.name} a {end.name} con {path.Count} nodos");
        }

        return path;
    }

    public (List<Transform> nodes, List<Vector3> detourPoints) GetPathWithDetours(Transform start, Transform end)
{
    return graph.GetFullPath(start, end);
}

    private void OnDrawGizmos()
    {
        if (!visualizeGraph || !Application.isPlaying) return;

        // Dibujar nodos
        Gizmos.color = Color.white;
        foreach (Transform node in GetAllNodes())
        {
            Gizmos.DrawWireSphere(node.position, 0.5f);
        }

        // Dibujar conexiones
        graph.DrawGizmoConnections(safePathColor, dangerousPathColor);
    }

    private Transform[] GetAllNodes()
    {
        return new Transform[] {
            topLeftCorner, topRightCorner,
            bottomLeftCorner, bottomRightCorner,
            topSideSpawnPedestrians, topSideSpawnPedestrians2,
            bottomSideSpawnPedestrians, bottomSideSpawnPedestrians2,
            leftSideSpawnPedestrians, leftSideSpawnPedestrians2,
            rightSideSpawnPedestrians, rightSideSpawnPedestrians2
        };
    }
}



