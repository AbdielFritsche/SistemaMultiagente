using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public List<Camera> cameras; // Lista de c�maras que deseas alternar
    private int currentCameraIndex = 0; // �ndice de la c�mara activa actualmente

    void Start()
    {
        // Aseg�rate de que solo una c�mara est� activa al inicio
        if (cameras.Count > 0)
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                cameras[i].gameObject.SetActive(i == currentCameraIndex);
            }
        }
        else
        {
            Debug.LogError("No se han asignado c�maras al script.");
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

        // Desactiva la c�mara actual
        cameras[currentCameraIndex].gameObject.SetActive(false);

        // Cambia al siguiente �ndice
        currentCameraIndex = (currentCameraIndex + 1) % cameras.Count;

        // Activa la nueva c�mara
        cameras[currentCameraIndex].gameObject.SetActive(true);

        Debug.Log($"Cambiando a la c�mara: {cameras[currentCameraIndex].name}");
    }
}