using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public List<Camera> cameras; // Lista de cámaras que deseas alternar
    private int currentCameraIndex = 0; // Índice de la cámara activa actualmente

    void Start()
    {
        // Asegúrate de que solo una cámara esté activa al inicio
        if (cameras.Count > 0)
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                cameras[i].gameObject.SetActive(i == currentCameraIndex);
            }
        }
        else
        {
            Debug.LogError("No se han asignado cámaras al script.");
        }
    }

    void Update()
    {
        // Detecta el clic del mouse o cualquier otra entrada que desees
        if (Input.GetMouseButtonDown(0)) // 0 es el clic izquierdo
        {
            SwitchCamera();
        }
    }

    void SwitchCamera()
    {
        if (cameras.Count == 0) return;

        // Desactiva la cámara actual
        cameras[currentCameraIndex].gameObject.SetActive(false);

        // Cambia al siguiente índice
        currentCameraIndex = (currentCameraIndex + 1) % cameras.Count;

        // Activa la nueva cámara
        cameras[currentCameraIndex].gameObject.SetActive(true);

        Debug.Log($"Cambiando a la cámara: {cameras[currentCameraIndex].name}");
    }
}