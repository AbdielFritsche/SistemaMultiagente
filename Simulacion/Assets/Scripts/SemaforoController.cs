using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SemaforoController : MonoBehaviour
{
    public GameObject[] semaforos; // Array de semáforos (deben ser 4)
    public Vector3[] posicionesRojas; // Posiciones individuales cuando los semáforos están en rojo
    public Vector3[] posicionesVerdes; // Posiciones individuales cuando los semáforos están en verde
    public float tiempoVerde = 25f; // Tiempo en segundos que un semáforo está en verde (libre)
    public float tiempoTodosRojos = 60f; // Tiempo en segundos que todos los semáforos están en rojo

    private int semaforoLibreActual = 0; // Índice del semáforo que está en verde (libre)
    private float tiempoActual = 0f; // Temporizador
    private bool enPausa = false; // Indica si estamos en la fase de todos en rojo

    void Start()
    {
        // Validación: Verifica que las posiciones rojas y verdes coincidan con el número de semáforos
        if (posicionesRojas.Length != semaforos.Length || posicionesVerdes.Length != semaforos.Length)
        {
            Debug.LogError("El número de posiciones rojas/verdes no coincide con el número de semáforos.");
            return;
        }

        // Configura todos los semáforos en estado rojo al inicio
        for (int i = 0; i < semaforos.Length; i++)
        {
            semaforos[i].transform.position = posicionesRojas[i];
        }

        // Activa el primer semáforo como verde (libre)
        semaforos[semaforoLibreActual].transform.position = posicionesVerdes[semaforoLibreActual];
        tiempoActual = tiempoVerde;
    }

    void Update()
    {
        // Reduce el temporizador
        tiempoActual -= Time.deltaTime;

        if (tiempoActual <= 0f)
        {
            if (enPausa)
            {
                // Salimos de la fase de pausa, reiniciamos el ciclo
                enPausa = false;
                semaforoLibreActual = 0;
                semaforos[semaforoLibreActual].transform.position = posicionesVerdes[semaforoLibreActual];
                tiempoActual = tiempoVerde;
            }
            else
            {
                // Vuelve el semáforo libre actual a rojo
                semaforos[semaforoLibreActual].transform.position = posicionesRojas[semaforoLibreActual];

                // Avanza al siguiente semáforo
                semaforoLibreActual++;

                if (semaforoLibreActual >= semaforos.Length)
                {
                    // Entra en la fase de pausa (todos en rojo)
                    enPausa = true;
                    tiempoActual = tiempoTodosRojos;

                    // Asegúrate de que todos están en rojo
                    for (int i = 0; i < semaforos.Length; i++)
                    {
                        semaforos[i].transform.position = posicionesRojas[i];
                    }
                }
                else
                {
                    // Activa el siguiente semáforo como verde (libre)
                    semaforos[semaforoLibreActual].transform.position = posicionesVerdes[semaforoLibreActual];
                    tiempoActual = tiempoVerde;
                }
            }
        }
    }
}
