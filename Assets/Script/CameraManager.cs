using UnityEngine;
using System;

public class CameraManager : MonoBehaviour
{
    public Camera mainCamera;           // Telecamera principale
    public Camera thermalCamera;        // Telecamera termica

    // Evento per notificare il cambio della telecamera
    public static event Action<bool> OnThermalCameraToggled;

    private bool isThermalActive = false;

    void Start()
    {
        // Inizializza le telecamere
        mainCamera.enabled = true;
        thermalCamera.enabled = false;
    }

    void Update()
    {
        // Premendo "T", cambia telecamera
        if (Input.GetKeyDown(KeyCode.T))
        {
            isThermalActive = !isThermalActive;

            mainCamera.enabled = !isThermalActive;
            thermalCamera.enabled = isThermalActive;

            // Lancia l'evento per notificare lo stato
            OnThermalCameraToggled?.Invoke(isThermalActive);
            Debug.Log(isThermalActive ? "Telecamera Termica Attivata" : "Telecamera Principale Attivata");
        }
    }
}
