using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarMovement : MonoBehaviour
{
    public Transform llantaDelanteraIzquierda;
    public Transform llantaDelanteraDerecha;
    public Transform llantaTraseraIzquierda;
    public Transform llantaTraseraDerecha;

    public float velocidad = 10f;
    public float rotacionLlantas = 100f;

    void Update()
    {
        // Movimiento del coche hacia adelante/atrás
        float movimiento = Input.GetAxis("Vertical") * velocidad * Time.deltaTime;
        transform.Translate(Vector3.forward * movimiento);

        // Rotación de las llantas
        if (movimiento != 0)
        {
            RotarLlantas(movimiento);
        }

        // Rotación del coche (izquierda/derecha)
        float rotacion = Input.GetAxis("Horizontal") * velocidad * Time.deltaTime;
        transform.Rotate(Vector3.up, rotacion);
    }

    void RotarLlantas(float movimiento)
    {
        float rotacion = movimiento * rotacionLlantas;

        // Rotar las llantas alrededor de su eje X
        llantaDelanteraIzquierda.Rotate(Vector3.right, rotacion);
        llantaDelanteraDerecha.Rotate(Vector3.right, rotacion);
        llantaTraseraIzquierda.Rotate(Vector3.right, rotacion);
        llantaTraseraDerecha.Rotate(Vector3.right, rotacion);
    }
}
