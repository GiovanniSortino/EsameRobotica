using UnityEngine;

public class DualMaterialSwitcher : MonoBehaviour
{
    public Material normalMaterial;    // Materiale per la telecamera principale
    public Material thermalMaterial;   // Materiale per la telecamera termica

    private Renderer objRenderer;      // Renderer dell'oggetto
    private Material[] originalMaterials; // Per gestire il ripristino dei materiali originali

    void Start()
    {
        // Ottieni il renderer dell'oggetto e salva i materiali originali
        objRenderer = GetComponent<Renderer>();

        // Salva il materiale originale per eventuali ripristini
        if (objRenderer != null)
        {
            originalMaterials = objRenderer.materials;
        }
    }

    void Update()
    {
        // Controlla se la telecamera corrente è la telecamera termica
        if (Camera.current != null && Camera.current.CompareTag("ThermalCamera"))
        {
            // Imposta il materiale termico
            SetMaterial(thermalMaterial);
        }
        else
        {
            // Ripristina il materiale originale se non è la telecamera termica
            ResetMaterial();
        }
    }

    void SetMaterial(Material mat)
    {
        // Imposta il materiale termico
        if (objRenderer != null && mat != null)
        {
            objRenderer.material = mat;
        }
    }

    void ResetMaterial()
    {
        // Ripristina i materiali originali
        if (objRenderer != null && originalMaterials != null)
        {
            objRenderer.materials = originalMaterials;
        }
    }
}
