using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarAgentController : MonoBehaviour
{
    public List<Movimiento> movements;
    public float movementSpeed = 5f;
    public LayerMask carLayer;
    public float minDistance = 2f;
    public float stopDistance = 1f;
    public float raycastLength = 5f;

    private Vector3 targetPosition;
    private Vector3 centerPoint;
    private Vector3 finalObjective;
    private int currentMovementIndex = 0;
    private bool reachedCenter = false;
    private bool isStopped = false;
    private Transform trafficLightPosition;
    private float waitTimer = 0f;
    private const float CHECK_INTERVAL = 0.25f;

    private float minimumCarSpacing = 20f;
    private float detectionWidth = 10f;
    private float detectionHeight = 15f;
    private float safetyBuffer = 10f;

    // New variables to track stop reasons
    private bool isStoppedByTrafficLight = false;
    private bool isStoppedByCar = false;

    public void Initialize(List<Movimiento> movements, Vector3 centerPoint, Vector3 finalObjective, Transform trafficLightPosition)
    {
        this.movements = movements;
        this.centerPoint = centerPoint;
        this.finalObjective = finalObjective;
        this.trafficLightPosition = trafficLightPosition;
        targetPosition = centerPoint;
    }
    
    void Update()
    {
        if (ShouldStopForCar())
        {
            isStopped = true;
            return;
        }

        if (movements == null || movements.Count == 0)
            return;
        
        if (Input.GetKeyDown(KeyCode.D)) // Press D to debug
        {
            LogCurrentState();
        }
        // Update wait timer and check traffic light state
        waitTimer += Time.deltaTime;
        if (waitTimer >= CHECK_INTERVAL)
        {
            CheckTrafficLightState();
            waitTimer = 0f;
        }

        // Check for car obstacles
        isStoppedByCar = CheckForCarAhead();

        // Update overall stopped state
        isStopped = isStoppedByTrafficLight || isStoppedByCar;

        // If not stopped, proceed with movement
        if (!isStopped)
        {
            MoveTowardsTarget();
            CheckTargetReached();
            AvoidOverlap();
        }
        else
        {
            // Debug information about why the car is stopped
            if (isStoppedByTrafficLight)
                Debug.Log($"Car {gameObject.name} stopped by traffic light");
            if (isStoppedByCar)
                Debug.Log($"Car {gameObject.name} stopped by car ahead");
        }
    }

    private void CheckTrafficLightState()
    {
        if (trafficLightPosition == null || currentMovementIndex >= movements.Count)
        {
            isStoppedByTrafficLight = false;
            return;
        }

        Movimiento currentMove = movements[currentMovementIndex];
        Vector3 directionToLight = trafficLightPosition.position - transform.position;
        float distanceToLight = Vector3.Distance(transform.position, trafficLightPosition.position);

        bool isNearTrafficLight = distanceToLight <= stopDistance;
        bool isApproachingTrafficLight = Vector3.Dot(transform.forward, directionToLight.normalized) > 0.5f;

        // Update traffic light stop state
        if ((currentMove.state == "red_light_near" || currentMove.state == "red_light") && isNearTrafficLight && isApproachingTrafficLight)
        {
            isStoppedByTrafficLight = true;
            isStopped = true;
            Debug.Log($"Car {gameObject.name} stopping at traffic light. Distance: {distanceToLight}, State: {currentMove.state}");
        }
        else
        {
            isStoppedByTrafficLight = false;
            isStopped = false;
            Debug.Log($"Car {gameObject.name} can proceed through traffic light. State: {currentMove.state}");
        }
    }


    private bool CheckForCarAhead()
    {
        Vector3 rayStart = transform.position + Vector3.up * detectionHeight;

        // Raycast check
        RaycastHit hit;
        if (Physics.Raycast(rayStart, transform.forward, out hit, minDistance + safetyBuffer, carLayer))
        {
            if (hit.collider.CompareTag("Coche") || hit.collider.CompareTag("Peaton"))
            {
                Debug.Log($"Car {gameObject.name} detected car ahead: {hit.collider.name} at distance {hit.distance}");
                return true;
            }
        }

        // BoxCast check
        Vector3 boxExtents = new Vector3(detectionWidth , detectionHeight , minDistance );
        if (Physics.BoxCast(rayStart, boxExtents, transform.forward, out hit, transform.rotation, minDistance + safetyBuffer, carLayer))
        {
            if (hit.collider.CompareTag("Coche") || hit.collider.CompareTag("Peaton"))
            {
                Debug.Log($"Car {gameObject.name} detected potential collision with: {hit.collider.name}");
                return true;
            }
        }

        return false;
    }

    private void MoveTowardsTarget()
    {
        Vector3 direction = targetPosition - transform.position;

        if (direction.magnitude > 0.1f)
        {
            float currentSpeed = movementSpeed;

            // Only check for speed reduction if we're not already stopped
            if (!isStopped)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, transform.forward, out hit, raycastLength))
                {
                    if (hit.collider.CompareTag("Coche") || hit.collider.CompareTag("Semaforo") || hit.collider.CompareTag("Peaton") )
                    {
                        // Instead of setting speed to 0, reduce it based on distance
                        float distanceRatio = hit.distance / raycastLength*10;
                        currentSpeed *= Mathf.Clamp01(distanceRatio);
                    }
                }

                transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);

                // Smooth rotation
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
    }

    private void CheckTargetReached()
    {
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            if (!reachedCenter)
            {
                reachedCenter = true;
                targetPosition = finalObjective;
                Debug.Log($"Car {gameObject.name} now heading to final objective: {finalObjective}");
            }
            else if (currentMovementIndex < movements.Count - 1)
            {
                SetNextTarget();
            }
            else
            {
                Debug.Log($"Car {gameObject.name} has reached final destination.");
                enabled = false;
            }
        }
    }

    private void SetNextTarget()
    {
        currentMovementIndex++;
        if (currentMovementIndex < movements.Count)
        {
            Movimiento nextMove = movements[currentMovementIndex];
            targetPosition = new Vector3(nextMove.x, 0.5f, nextMove.y);
            Debug.Log($"Car {gameObject.name} moving to next target: {targetPosition}");
        }
    }

    private void AvoidOverlap()
    {
        Vector3 boxSize = new Vector3(3.5f, 1.5f, minDistance / 2); // Ajusta las dimensiones según tu Box Collider
        Collider[] collisions = Physics.OverlapBox(transform.position, boxSize, Quaternion.identity, carLayer);

        foreach (Collider collider in collisions)
        {
            if (collider.gameObject != this.gameObject) // Evitar auto-detección
            {
                Vector3 direction = transform.position - collider.transform.position;
                direction = direction.normalized;

                // Reposicionar el coche
                transform.position += direction * 0.1f; // Ajusta este valor según el nivel de separación deseado
                Debug.Log($"Adjusting position to avoid overlap with {collider.gameObject.name}");
            }
        }
    }

    private bool ShouldStopForCar()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.5f; // Eleva el inicio del raycast para evitar el suelo
        float rayLength = 4f; // Longitud del raycast
        float rayWidth = 2f; // Amplitud del raycast (para lanzar varios)

        // Lanza varios rayos para detectar coches
        for (float offset = -rayWidth; offset <= rayWidth; offset += 0.2f)
        {
            Vector3 startPosition = rayStart + transform.right * offset; // Desplaza lateralmente el inicio del raycast
            Debug.DrawRay(startPosition, transform.forward * rayLength, Color.red);

            if (Physics.Raycast(startPosition, transform.forward, out RaycastHit hit, rayLength))
            {
                if (hit.collider.CompareTag("Coche") || hit.collider.CompareTag("Peaton"))
                {
                    Debug.Log($"Coche {gameObject.name} detectó otro coche adelante: {hit.collider.name}");
                    return true; // Detén el coche
                }
            }
        }

        return false; // No hay coches adelante
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Coche") || collision.collider.CompareTag("Coche") )
        {
            Vector3 separationForce = (transform.position - collision.transform.position).normalized;
            separationForce.y = 0; // Evitar movimientos verticales
            GetComponent<Rigidbody>().AddForce(separationForce * 10f, ForceMode.Force);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Coche") || other.CompareTag("Peaton"))
        {
            isStopped = false;
            Debug.Log($"Coche {gameObject.name} ya no detecta {other.name}");
        }
    }

    private void LogCurrentState()
    {
        if (movements != null && currentMovementIndex < movements.Count)
        {
            string currentState = movements[currentMovementIndex].state;
            Debug.Log($"Car {gameObject.name} - Current State:" +
                $"\nTraffic Light Stopped: {isStoppedByTrafficLight}" +
                $"\nCar Ahead Stopped: {isStoppedByCar}" +
                $"\nOverall Stopped: {isStopped}" +
                $"\nCurrent Movement State: {currentState}" +
                $"\nTarget Position: {targetPosition}");
        }
    }
}