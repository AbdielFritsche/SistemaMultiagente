using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarMovement : MonoBehaviour
{
    public Transform llantaDelanteraIzquierda;
    public Transform llantaDelanteraDerecha;
    public Transform llantaTraseraIzquierda;
    public Transform llantaTraseraDerecha;

    public float velocidad = 1000f; // Incrementa la fuerza aplicada
    public float velocidadRotacion = 10f;
    public float rotacionLlantas = 50f;
    public float limiteVelocidad = 20f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0); // Centro de masa ajustado
    }

    void FixedUpdate()
    {
        // Movimiento hacia adelante o atr�s
        float movimientoInput = Input.GetAxis("Vertical");
        Vector3 fuerzaMovimiento = transform.forward * movimientoInput * velocidad;
        rb.AddForce(fuerzaMovimiento, ForceMode.Force);

        // Limitar la velocidad m�xima
        if (rb.velocity.magnitude > limiteVelocidad)
        {
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, limiteVelocidad);
        }

        // Rotaci�n del coche
        float rotacionInput = Input.GetAxis("Horizontal");
        float giro = rotacionInput * velocidadRotacion * Time.fixedDeltaTime;
        transform.Rotate(0, giro, 0);
    }
}