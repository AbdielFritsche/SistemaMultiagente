using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarAgentController : MonoBehaviour
{
    public List<Movimiento> movements;  // Lista de movimientos del agente
    public float movementSpeed = 5f;    // Velocidad del agente
    public float scaleFactor = 5f;      // Factor de escala (10x10 -> 50x50)

    private int currentMovementIndex = 0; // Índice del movimiento actual
    private Vector3 targetPosition;      // Posición objetivo actual

    void Start()
    {
        if (movements == null || movements.Count == 0)
        {
            return;
        }

        // Inicializar la posición del agente al primer movimiento escalado
        transform.position = ScalePosition(movements[0]);

        // Configurar el siguiente objetivo
        SetNextTarget();
    }

    void Update()
    {
        if (movements == null || currentMovementIndex >= movements.Count)
            return;

        // Moverse hacia la posición objetivo
        MoveTowardsTarget();

        // Verificar si hemos llegado a la posición objetivo
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            // Configurar el siguiente objetivo
            SetNextTarget();
        }
    }

   
    private Vector3 ScalePosition(Movimiento movement)
    {
        float x = movement.x * scaleFactor;
        float z = movement.y * scaleFactor;
        return new Vector3(x, 0.5f, z); // Y se mantiene en 0.5 para un offset visual
    }
    private void SetNextTarget()
    {
        if (currentMovementIndex < movements.Count)
        {
            targetPosition = ScalePosition(movements[currentMovementIndex]);
            currentMovementIndex++;
        }
        else
        {
            // Si no hay más movimientos, detener el agente
            Debug.Log($"Agente {gameObject.name} ha completado su ruta.");
            enabled = false;
        }
    }

    private void MoveTowardsTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, movementSpeed * Time.deltaTime);

        // Opcional: Rotar el agente hacia el objetivo
        Vector3 direction = targetPosition - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    public void GetMovements(List<Movimiento> movements)
    {
        this.movements = movements;
    }
}
