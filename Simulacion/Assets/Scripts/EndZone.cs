using UnityEngine;

public class EndZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coche")) // Aseg�rate de que tus coches tengan la etiqueta "Car"
        {
            Destroy(other.gameObject); // Elimina el coche cuando toca la pared
            Debug.Log("Coche complet� su recorrido y fue eliminado.");
        }
    }
}