using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PeatonController : MonoBehaviour
{
    public List<Movimiento> movements;
    public float movementSpeed = 2f;         // Slower than cars
    public float minDistance = 1f;           // Minimum distance between pedestrians
    public float detectionRadius = 2f;       // Radius to detect other pedestrians
    public LayerMask pedestrianLayer;        // Layer for pedestrian detection

    private Vector3 targetPosition;
    private Vector3 centerPoint;
    private int currentMovementIndex = 0;
    private bool reachedCenter = false;
    private bool isStopped = false;

    // Parameters for crowd behavior
    private float waitTime = 0f;
    private const float COLLISION_CHECK_INTERVAL = 0.25f;
    private Vector3 avoidanceDirection = Vector3.zero;

    public void Initialize(List<Movimiento> movements, Vector3 centerPoint)
    {
        this.movements = movements;
        this.centerPoint = centerPoint;
        targetPosition = centerPoint;
    }

    void Update()
    {
        if (movements == null || movements.Count == 0)
            return;

        waitTime += Time.deltaTime;
        if (waitTime >= COLLISION_CHECK_INTERVAL)
        {
            CheckForOtherPedestrians();
            waitTime = 0f;
        }

        if (!isStopped)
        {
            MoveTowardsTarget();
            CheckTargetReached();
        }
    }

    private void CheckForOtherPedestrians()
    {
        Collider[] nearbyPedestrians = Physics.OverlapSphere(transform.position, detectionRadius, pedestrianLayer);

        avoidanceDirection = Vector3.zero;
        bool shouldStop = false;

        foreach (Collider other in nearbyPedestrians)
        {
            if (other.gameObject != gameObject)
            {
                Vector3 directionToOther = other.transform.position - transform.position;
                float distance = directionToOther.magnitude;

                if (distance < minDistance)
                {
                    shouldStop = true;
                    // Add avoidance force in opposite direction
                    avoidanceDirection += -directionToOther.normalized;
                }
            }
        }

        isStopped = shouldStop;

        if (avoidanceDirection != Vector3.zero)
        {
            avoidanceDirection.Normalize();
        }
    }

    private void MoveTowardsTarget()
    {
        Vector3 direction = targetPosition - transform.position;

        if (direction.magnitude > 0.1f)
        {
            // Combine target direction with avoidance
            Vector3 moveDirection = direction.normalized;
            if (avoidanceDirection != Vector3.zero)
            {
                moveDirection = (moveDirection + avoidanceDirection).normalized;
            }

            // Calculate movement with smooth acceleration/deceleration
            float currentSpeed = movementSpeed;
            if (direction.magnitude < minDistance)
            {
                currentSpeed *= direction.magnitude / minDistance;
            }

            // Move the pedestrian
            transform.position = Vector3.MoveTowards(
                transform.position,
                transform.position + moveDirection,
                currentSpeed * Time.deltaTime
            );

            // Rotate towards movement direction
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * 5f
                );
            }
        }
    }

    private void CheckTargetReached()
    {
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            if (!reachedCenter)
            {
                // After reaching center, move to next waypoint
                reachedCenter = true;
                if (currentMovementIndex < movements.Count)
                {
                    SetNextTarget();
                }
            }
            else if (currentMovementIndex < movements.Count - 1)
            {
                SetNextTarget();
            }
            else
            {
                // Reached final destination
                Debug.Log($"Pedestrian {gameObject.name} has reached final destination.");
                Destroy(gameObject);
            }
        }
    }

    private void SetNextTarget()
    {
        if (currentMovementIndex < movements.Count)
        {
            Movimiento nextMove = movements[currentMovementIndex];
            targetPosition = new Vector3(nextMove.x, transform.position.y, nextMove.y);
            currentMovementIndex++;
            Debug.Log($"Pedestrian {gameObject.name} moving to next target: {targetPosition}");
        }
    }

    private void OnDestroy()
    {
        // Cleanup when pedestrian is destroyed
        Debug.Log($"Pedestrian {gameObject.name} destroyed and cleaned up");
    }
}
