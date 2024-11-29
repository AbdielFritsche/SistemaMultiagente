using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SemaforoController : MonoBehaviour
{
    public GameObject[] semaforos; // Array de sem�foros (deben ser 4)
    public Vector3[] posicionesRojas; // Posiciones individuales cuando los sem�foros est�n en rojo
    public Vector3[] posicionesVerdes; // Posiciones individuales cuando los sem�foros est�n en verde
    public float tiempoVerde = 25f; // Tiempo en segundos que un sem�foro est� en verde (libre)
    public float tiempoTodosRojos = 60f; // Tiempo en segundos que todos los sem�foros est�n en rojo

    private int semaforoLibreActual = 0; // �ndice del sem�foro que est� en verde (libre)
    private float tiempoActual = 0f; // Temporizador
    private bool enPausa = false; // Indica si estamos en la fase de todos en rojo

    void Start()
    {
        // Validaci�n: Verifica que las posiciones rojas y verdes coincidan con el n�mero de sem�foros
        if (posicionesRojas.Length != semaforos.Length || posicionesVerdes.Length != semaforos.Length)
        {
            Debug.LogError("El n�mero de posiciones rojas/verdes no coincide con el n�mero de sem�foros.");
            return;
        }

        // Configura todos los sem�foros en estado rojo al inicio
        for (int i = 0; i < semaforos.Length; i++)
        {
            semaforos[i].transform.position = posicionesRojas[i];
        }

        // Activa el primer sem�foro como verde (libre)
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
                // Vuelve el sem�foro libre actual a rojo
                semaforos[semaforoLibreActual].transform.position = posicionesRojas[semaforoLibreActual];

                // Avanza al siguiente sem�foro
                semaforoLibreActual++;

                if (semaforoLibreActual >= semaforos.Length)
                {
                    // Entra en la fase de pausa (todos en rojo)
                    enPausa = true;
                    tiempoActual = tiempoTodosRojos;

                    // Aseg�rate de que todos est�n en rojo
                    for (int i = 0; i < semaforos.Length; i++)
                    {
                        semaforos[i].transform.position = posicionesRojas[i];
                    }
                }
                else
                {
                    // Activa el siguiente sem�foro como verde (libre)
                    semaforos[semaforoLibreActual].transform.position = posicionesVerdes[semaforoLibreActual];
                    tiempoActual = tiempoVerde;
                }
            }
        }
    }
}
