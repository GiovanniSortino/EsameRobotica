using UnityEngine;

public class MaterialSwitcher : MonoBehaviour
{
    public Material normalMaterial;     // Materiale per la telecamera principale
    public Material thermalMaterial;    // Materiale per la telecamera termica

    private Renderer objRenderer;

    void OnEnable()
    {
        // Iscriviti all'evento di CameraManager
        CameraManager.OnThermalCameraToggled += SwitchMaterial;
    }

    void OnDisable()
    {
        // Rimuovi l'iscrizione all'evento
        CameraManager.OnThermalCameraToggled -= SwitchMaterial;
    }

    void Start()
    {
        // Ottieni il Renderer dell'oggetto
        objRenderer = GetComponent<Renderer>();

        // Imposta il materiale normale all'inizio
        if (objRenderer != null)
        {
            objRenderer.material = normalMaterial;
        }
    }

    void SwitchMaterial(bool isThermalActive)
    {
        // Cambia il materiale in base allo stato della telecamera termica
        if (objRenderer != null)
        {
            objRenderer.material = isThermalActive ? thermalMaterial : normalMaterial;
        }
    }
}
