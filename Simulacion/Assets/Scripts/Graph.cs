using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Graph
{
    private Dictionary<Transform, List<EdgeInfo>> adjacencyList = new Dictionary<Transform, List<EdgeInfo>>();
    private const float RAYCAST_INTERVAL = 0.5f; // Intervalo para revisar obstáculos
    private const float DETOUR_OFFSET = 1.5f;

    // Estructura para almacenar información de las aristas
    public class EdgeInfo
    {
        public Transform node;
        public List<Vector3> waypoints;  // Puntos intermedios para evitar obstáculos
        public float weight;
        public bool isCrossing;

        public EdgeInfo(Transform node, bool isCrossing)
        {
            this.node = node;
            this.waypoints = new List<Vector3>();
            this.isCrossing = isCrossing;
        }
    }

    public void AddNode(Transform node)
    {
        if (!adjacencyList.ContainsKey(node))
        {
            adjacencyList[node] = new List<EdgeInfo>();
        }
    }

    public void AddEdge(Transform from, Transform to, bool isCrossing = false)
    {
        if (!adjacencyList.ContainsKey(from)) AddNode(from);
        if (!adjacencyList.ContainsKey(to)) AddNode(to);

        // Generar waypoints para evitar obstáculos
        List<Vector3> waypoints = GenerateWaypoints(from.position, to.position);
        float totalWeight = CalculatePathWeight(from.position, to.position, waypoints);

        // Crear la conexión con los waypoints
        var edgeInfo = new EdgeInfo(to, isCrossing);
        edgeInfo.waypoints = waypoints;
        edgeInfo.weight = totalWeight;
        adjacencyList[from].Add(edgeInfo);

        // Crear la conexión inversa
        var reverseWaypoints = new List<Vector3>(waypoints);
        reverseWaypoints.Reverse();
        var reverseEdge = new EdgeInfo(from, isCrossing);
        reverseEdge.waypoints = reverseWaypoints;
        reverseEdge.weight = totalWeight;
        adjacencyList[to].Add(reverseEdge);
    }

    private List<Vector3> GenerateWaypoints(Vector3 start, Vector3 end)
    {
        List<Vector3> waypoints = new List<Vector3>();
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        // Lanzar un rayo para detectar obstáculos
        RaycastHit hit;
        if (Physics.Raycast(start, direction, out hit, distance))
        {
            if (hit.collider.CompareTag("Obstaculos") || hit.collider.CompareTag("Coche"))
            {
                // Obtener el tamaño del collider
                Bounds bounds = hit.collider.bounds;
                float offset = Mathf.Max(bounds.extents.x, bounds.extents.z) + 1f;

                // Determinar dirección de evasión (derecha o izquierda del obstáculo)
                Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;

                // Crear punto de desvío alrededor del obstáculo
                Vector3 detourPoint = bounds.center + right * offset;

                // Mantener la altura original
                detourPoint.y = start.y;

                waypoints.Add(detourPoint);

                // Debug visual
                Debug.DrawLine(start, detourPoint, Color.yellow, 1f);
                Debug.DrawLine(detourPoint, end, Color.yellow, 1f);
            }
        }

        return waypoints;
    }
    /*
    private List<Vector3> GenerateWaypoints(Vector3 start, Vector3 end)
    {
        List<Vector3> waypoints = new List<Vector3>();
        Vector3 direction = (end - start);
        float distance = direction.magnitude;
        direction.Normalize();

        // Primero, verificar si hay obstáculo con un raycast directo
        RaycastHit hit;
        if (Physics.Raycast(start, direction, out hit, distance))
        {
            // Si golpea algo que no es un peatón ni un coche
            if (!hit.collider.CompareTag("Peaton") && !hit.collider.CompareTag("Coche"))
            {
                Debug.Log($"Obstáculo detectado: {hit.collider.name} en la posición {hit.point}");

                // Posición del obstáculo
                Vector3 obstacleCenter = hit.collider.bounds.center;

                // Calcular radio del obstáculo
                float obstacleRadius = Mathf.Max(hit.collider.bounds.extents.x, hit.collider.bounds.extents.z);

                // Aumentar el offset basado en el tamaño del obstáculo
                float actualOffset = DETOUR_OFFSET + obstacleRadius;

                // Determinar la mejor dirección para rodear (derecha o izquierda)
                Vector3 rightDirection = Vector3.Cross(direction, Vector3.up).normalized;
                Vector3 leftDirection = -rightDirection;

                // Probar ambas direcciones y elegir la mejor
                bool canGoRight = !Physics.Raycast(hit.point + rightDirection * actualOffset, direction, distance);
                bool canGoLeft = !Physics.Raycast(hit.point + leftDirection * actualOffset, direction, distance);

                Vector3 avoidDirection = canGoRight ? rightDirection : leftDirection;

                // Si ambas direcciones están bloqueadas, aumentar el offset
                if (!canGoRight && !canGoLeft)
                {
                    actualOffset *= 1.5f;
                    avoidDirection = rightDirection; // Por defecto ir a la derecha con más espacio
                }

                // Crear los puntos de desvío
                Vector3 beforeObstacle = obstacleCenter - direction * actualOffset;
                Vector3 aroundObstacle = obstacleCenter + avoidDirection * actualOffset;
                Vector3 afterObstacle = obstacleCenter + direction * actualOffset;

                // Ajustar altura de los puntos
                beforeObstacle.y = start.y;
                aroundObstacle.y = start.y;
                afterObstacle.y = start.y;

                // Añadir puntos con validación
                if (IsPointValid(beforeObstacle)) waypoints.Add(beforeObstacle);
                if (IsPointValid(aroundObstacle)) waypoints.Add(aroundObstacle);
                if (IsPointValid(afterObstacle)) waypoints.Add(afterObstacle);

                // Debug visual
                Debug.DrawLine(start, beforeObstacle, Color.yellow, 5f);
                Debug.DrawLine(beforeObstacle, aroundObstacle, Color.yellow, 5f);
                Debug.DrawLine(aroundObstacle, afterObstacle, Color.yellow, 5f);
                Debug.DrawLine(afterObstacle, end, Color.yellow, 5f);
            }
        }

        return waypoints;
    }*/

    private bool IsPointValid(Vector3 point)
    {
        // Verificar que el punto no esté dentro de ningún obstáculo
        Collider[] colliders = Physics.OverlapSphere(point, 0.1f);
        foreach (var collider in colliders)
        {
            if (!collider.CompareTag("Peaton") && !collider.CompareTag("Coche"))
            {
                return false;
            }
        }
        return true;
    }

    private float CalculatePathWeight(Vector3 start, Vector3 end, List<Vector3> waypoints)
    {
        float weight = 0;
        Vector3 previousPoint = start;

        foreach (var point in waypoints)
        {
            weight += Vector3.Distance(previousPoint, point);
            previousPoint = point;
        }

        weight += Vector3.Distance(previousPoint, end);
        return weight;
    }

    private List<Vector3> CheckForObstacles(Vector3 start, Vector3 end)
    {
        List<Vector3> detourPoints = new List<Vector3>();
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        direction.Normalize();

        // Comprobar obstáculos a lo largo de la línea
        int numChecks = Mathf.CeilToInt(distance / RAYCAST_INTERVAL);
        bool needsDetour = false;
        Vector3 obstaclePosition = Vector3.zero;
        Vector3 obstacleNormal = Vector3.zero;

        for (int i = 0; i <= numChecks; i++)
        {
            float t = i * RAYCAST_INTERVAL;
            if (t > distance) t = distance;

            Vector3 checkPoint = start + direction * t;

            // Realizar un check esférico para detectar obstáculos
            Collider[] obstacles = Physics.OverlapSphere(checkPoint, 0.5f, LayerMask.GetMask("Obstacle"));

            if (obstacles.Length > 0)
            {
                needsDetour = true;
                obstaclePosition = obstacles[0].transform.position;
                // Calcular la normal aproximada del obstáculo
                obstacleNormal = (checkPoint - obstacles[0].transform.position).normalized;
                break;
            }
        }

        if (needsDetour)
        {
            // Crear puntos de desvío alrededor del obstáculo
            Vector3 detourDirection = Vector3.Cross(direction, Vector3.up).normalized;

            // Punto antes del obstáculo
            Vector3 beforeObstacle = obstaclePosition - direction * DETOUR_OFFSET;
            detourPoints.Add(beforeObstacle);

            // Punto de desvío
            Vector3 detourPoint = obstaclePosition + detourDirection * DETOUR_OFFSET;
            detourPoints.Add(detourPoint);

            // Punto después del obstáculo
            Vector3 afterObstacle = obstaclePosition + direction * DETOUR_OFFSET;
            detourPoints.Add(afterObstacle);
        }

        return detourPoints;
    }

    public List<Transform> FindShortestPath(Transform start, Transform end)
    {
        if (!adjacencyList.ContainsKey(start) || !adjacencyList.ContainsKey(end))
        {
            Debug.LogError($"Nodo inicial o final no existe en el grafo: {start?.name} -> {end?.name}");
            return new List<Transform>();
        }

        var distances = new Dictionary<Transform, float>();
        var previous = new Dictionary<Transform, Transform>();
        var unvisited = new HashSet<Transform>();

        // Inicializar distancias
        foreach (var node in adjacencyList.Keys)
        {
            distances[node] = float.MaxValue;
            unvisited.Add(node);
        }
        distances[start] = 0;

        while (unvisited.Count > 0)
        {
            // Encontrar el nodo no visitado con la menor distancia
            Transform current = null;
            float minDistance = float.MaxValue;
            foreach (var node in unvisited)
            {
                if (distances[node] < minDistance)
                {
                    minDistance = distances[node];
                    current = node;
                }
            }

            if (current == null) break;
            if (current == end) break;

            unvisited.Remove(current);

            foreach (var edge in adjacencyList[current])
            {
                if (!unvisited.Contains(edge.node)) continue;

                float distance = distances[current] + edge.weight;
                if (distance < distances[edge.node])
                {
                    distances[edge.node] = distance;
                    previous[edge.node] = current;
                }
            }
        }

        return ReconstructPath(previous, start, end);
    }

    private List<Transform> ReconstructPath(Dictionary<Transform, Transform> previous, Transform start, Transform end)
    {
        var path = new List<Transform>();
        Transform current = end;

        while (current != null)
        {
            path.Add(current);
            if (current == start) break;
            if (!previous.TryGetValue(current, out current))
            {
                Debug.LogError($"No se pudo reconstruir el camino completo de {start.name} a {end.name}");
                return new List<Transform>();
            }
        }

        path.Reverse();
        return path;
    }

    public void PrintGraph()
    {
        foreach (var kvp in adjacencyList)
        {
            string connections = string.Join(", ",
                kvp.Value.Select(e => $"{e.node.name}({e.weight:F2}{(e.isCrossing ? ",Cross" : "")})"));
            Debug.Log($"Nodo {kvp.Key.name} conectado a: {connections}");
        }
    }

    public (List<Transform> nodes, List<Vector3> waypoints) GetFullPath(Transform start, Transform end)
    {
        List<Transform> nodePath = FindShortestPath(start, end);
        List<Vector3> allPoints = new List<Vector3>();

        // Agregar puntos intermedios entre cada par de nodos
        for (int i = 0; i < nodePath.Count - 1; i++)
        {
            Transform current = nodePath[i];
            Transform next = nodePath[i + 1];

            // Encontrar la EdgeInfo correspondiente
            EdgeInfo edge = adjacencyList[current].Find(e => e.node == next);
            if (edge != null && edge.waypoints.Count > 0)  // Cambiar intermediatePoints por waypoints
            {
                allPoints.AddRange(edge.waypoints);
            }
        }

        return (nodePath, allPoints);
    }

    public void DrawGizmoConnections(Color safeColor, Color dangerousColor)
    {
        foreach (var kvp in adjacencyList)
        {
            Transform from = kvp.Key;
            foreach (var edge in kvp.Value)
            {
                Gizmos.color = edge.isCrossing ? dangerousColor : safeColor;

                if (edge.waypoints.Count > 0)
                {
                    Vector3 currentPoint = from.position;
                    foreach (var point in edge.waypoints)
                    {
                        Gizmos.DrawWireSphere(point, 0.2f); // Visualizar puntos intermedios
                        Gizmos.DrawLine(currentPoint, point);
                        currentPoint = point;
                    }
                    Gizmos.DrawLine(currentPoint, edge.node.position);
                }
                else
                {
                    Gizmos.DrawLine(from.position, edge.node.position);
                }
            }
        }
    }
}