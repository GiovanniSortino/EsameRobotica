using UnityEngine;
 
public class DoubleMaterialSwitcher : MonoBehaviour
{
    public Material normalMaterial1;     // Materiale 1 per la telecamera principale
    public Material normalMaterial2;     // Materiale 2 per la telecamera principale
    public Material thermalMaterial1;    // Materiale 1 per la telecamera termica
    public Material thermalMaterial2;    // Materiale 2 per la telecamera termica
 
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
 
        // Imposta i materiali normali all'inizio
        if (objRenderer != null)
        {
            objRenderer.materials = new Material[] { normalMaterial1, normalMaterial2 };
        }
    }
 
    void SwitchMaterial(bool isThermalActive)
    {
        // Cambia i materiali in base allo stato della telecamera termica
        if (objRenderer != null)
        {
            objRenderer.materials = isThermalActive
                ? new Material[] { thermalMaterial1, thermalMaterial2 }
                : new Material[] { normalMaterial1, normalMaterial2 };
        }
    }
}