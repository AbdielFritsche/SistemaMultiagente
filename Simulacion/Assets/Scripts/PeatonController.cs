using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PeatonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float movementSpeed = 2f;
    public float rotationSpeed = 5f;
    public float minDistance = 1f;
    public float detectionRadius = 10f;
    public float obstacleAvoidanceStrength = 5f;
    public float pathNodeRadius = 0.1f;

    [Header("Layer Settings")]
    public LayerMask pedestrianLayer;
    public LayerMask obstacleLayer;
    public LayerMask carLayer;

    private List<Transform> pathNodes;
    private List<Transform> intermediatePoints;
    private List<Movimiento> movements;
    private Vector3 targetPosition;
    private Vector3 centerPoint;
    private Vector3 objectivePoint;
    private Transform currentTarget;
    private Vector3 currentVelocity;
    private Vector3 avoidanceDirection;
    private bool isStopped;
    private int currentNodeIndex = 0;
    private int currentMovementIndex = 0;
    private bool reachedCenter = false;
    private float waitTime;
    private const float COLLISION_CHECK_INTERVAL = 0.1f;


    public Transform topLeftCorner;
    public Transform topRightCorner;
    public Transform bottomLeftCorner;
    public Transform bottomRightCorner;

    private void Awake()
    {
        pathNodes = new List<Transform>();
        intermediatePoints = new List<Transform>();
        currentVelocity = Vector3.zero;
    }

    public void InitializePath(List<Transform> route)
    {
        if (route == null || route.Count == 0)
        {
            Debug.LogError($"[{gameObject.name}] Error: Ruta vacía o nula");
            return;
        }

        pathNodes = new List<Transform>(route);
        currentNodeIndex = 0;
        SetNextTarget();
    }

    private void Update()
    {
        waitTime += Time.deltaTime;
        if (waitTime >= COLLISION_CHECK_INTERVAL)
        {
            UpdateObstacleAvoidance();
            waitTime = 0f;
        }

        if (!isStopped)
        {
            MoveTowardsTarget();
        }

        DrawDebugRays();
    }

    private void UpdateObstacleAvoidance()
    {
        avoidanceDirection = Vector3.zero;
        isStopped = false;

        // Detección de obstáculos y coches
        Collider[] obstacles = Physics.OverlapSphere(transform.position, detectionRadius, obstacleLayer | carLayer);
        foreach (var obstacle in obstacles)
        {
            Vector3 directionToObstacle = obstacle.transform.position - transform.position;
            float distance = directionToObstacle.magnitude;

            if (distance < minDistance)
            {
                Vector3 avoidDir = Vector3.Cross(directionToObstacle, Vector3.up).normalized;

                if (Vector3.Dot(avoidDir, transform.right) < 0)
                {
                    avoidDir = -avoidDir;
                }

                avoidanceDirection += avoidDir * (obstacleAvoidanceStrength / distance);

                if (distance < minDistance * 0.5f)
                {
                    isStopped = true;
                }
            }
        }

        // Detección de otros peatones
        Collider[] pedestrians = Physics.OverlapSphere(transform.position, detectionRadius, pedestrianLayer);
        foreach (var other in pedestrians)
        {
            if (other.gameObject != gameObject)
            {
                Vector3 directionToOther = other.transform.position - transform.position;
                float distance = directionToOther.magnitude;

                if (distance < minDistance)
                {
                    avoidanceDirection -= directionToOther.normalized * (1f / distance);
                }
            }
        }

        if (avoidanceDirection != Vector3.zero)
        {
            avoidanceDirection.Normalize();
        }
    }



    private void MoveTowardsTarget()
    {
        if (currentTarget == null) return;

        Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;
        Vector3 finalDirection = directionToTarget;

        if (avoidanceDirection != Vector3.zero)
        {
            finalDirection = (directionToTarget + avoidanceDirection).normalized;
        }

        finalDirection.y = 0;
        Vector3 targetVelocity = finalDirection * movementSpeed;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * 5f);

        if (!isStopped)
        {
            transform.position += currentVelocity * Time.deltaTime;

            if (currentVelocity.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(currentVelocity);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }

        if (Vector3.Distance(transform.position, currentTarget.position) < pathNodeRadius)
        {
            SetNextTarget();
        }
    }

    private void SetNextTarget()
    {
        if (currentNodeIndex < pathNodes.Count)
        {
            currentTarget = pathNodes[currentNodeIndex];
            currentNodeIndex++;
            Debug.Log($"[{gameObject.name}] Nuevo objetivo: {currentTarget.name}");
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Llegó al destino final");
            Destroy(gameObject);
        }
    }


    private void DrawDebugRays()
    {
        // Dibujar radio de detección

        // Dibujar dirección hacia el objetivo
        if (currentTarget != null)
        {
            Debug.DrawLine(transform.position, currentTarget.position, Color.green);
        }

        // Dibujar dirección de evasión
        if (avoidanceDirection != Vector3.zero)
        {
            Debug.DrawRay(transform.position, avoidanceDirection * 2f, Color.red);
        }
    }

    private void OnDrawGizmos()
    {
        // Dibujar el radio de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Dibujar la dirección actual
        if (currentTarget != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }

    public void Initialize(List<Movimiento> movements, Vector3 centerPoint, Vector3 objectivePoint)
    {
        this.movements = movements;
        this.centerPoint = centerPoint;
        this.objectivePoint = objectivePoint;

        intermediatePoints = GetIntermediatePoints(centerPoint, objectivePoint);

        if (intermediatePoints.Count == 0)
        {
            Debug.LogWarning($"No se generaron puntos intermedios para el peatón desde {centerPoint} hasta {objectivePoint}.");
        }

        if (intermediatePoints.Count > 0)
        {
            targetPosition = intermediatePoints[0].position;
            intermediatePoints.RemoveAt(0);
        }
        else
        {
            targetPosition = objectivePoint;
        }

        reachedCenter = false;
        currentMovementIndex = 0;

        Debug.Log($"{gameObject.name} inicializado con {intermediatePoints.Count} puntos intermedios y destino final en {objectivePoint}");
    }
    private List<Transform> GetIntermediatePoints(Vector3 spawnPosition, Vector3 objectivePoint)
    {
        List<Transform> intermediatePoints = new List<Transform>();

        // Agrega lógica para conectar puntos centrales
        if (spawnPosition.x < 0 && spawnPosition.z < 0)
        {
            intermediatePoints.Add(bottomLeftCorner);
        }
        else if (spawnPosition.x > 0 && spawnPosition.z < 0)
        {
            intermediatePoints.Add(bottomRightCorner);
        }
        else if (spawnPosition.x < 0 && spawnPosition.z > 0)
        {
            intermediatePoints.Add(topLeftCorner);
        }
        else if (spawnPosition.x > 0 && spawnPosition.z > 0)
        {
            intermediatePoints.Add(topRightCorner);
        }

        // Si no se generan puntos, forzar un paso central predeterminado
        if (intermediatePoints.Count == 0)
        {
            Debug.LogWarning($"No se generaron puntos intermedios para el peatón desde {spawnPosition} hasta {objectivePoint}. Añadiendo un punto predeterminado.");
            intermediatePoints.Add(topRightCorner);
        }

        Debug.Log($"Puntos intermedios generados: {string.Join(", ", intermediatePoints.Select(p => p.name))}");
        return intermediatePoints;
    }

    private Dictionary<string, Transform> crossingPoints; // Almacena los cruces por nombre o lógica

    public void SetCrossingPoints(Dictionary<string, Transform> crossingPoints)
    {
        this.crossingPoints = crossingPoints;
        if (crossingPoints.ContainsKey("TopRight"))
        {
            topRightCorner = crossingPoints["TopRight"];
        }
        if (crossingPoints.ContainsKey("TopLeft"))
        {
            topLeftCorner = crossingPoints["TopLeft"];
        }
        if (crossingPoints.ContainsKey("BottomLeft"))
        {
            bottomLeftCorner = crossingPoints["BottomLeft"];
        }
        if (crossingPoints.ContainsKey("BottomRight"))
        {
            bottomRightCorner = crossingPoints["BottomRight"];
        }
    }

}
