using UnityEngine;

public class HeatLevelController : MonoBehaviour
{
    public Material thermalMaterial; // Riferimento al materiale termico
    [Range(0, 1)] public float heatLevel = 0.5f; // Livello di calore

    void Update()
    {
        // Modifica il valore di HeatLevel dinamicamente
        if (thermalMaterial != null)
        {
            thermalMaterial.SetFloat("_HeatLevel", heatLevel);
        }

        // Test: Cambia calore con tasti
        if (Input.GetKey(KeyCode.UpArrow))
        {
            heatLevel = Mathf.Clamp01(heatLevel + Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            heatLevel = Mathf.Clamp01(heatLevel - Time.deltaTime);
        }
    }
}
