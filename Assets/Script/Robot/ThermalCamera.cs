using UnityEngine;

public class ThermalCamera : MonoBehaviour
{
    public RenderTexture thermalTexture; // Assegna la RenderTexture della camera termica
    public float redThreshold = 0.8f;    // Soglia per il rilevamento del rosso
    public int detectionRadius = 5;     // Raggio di pixel per cluster rosso

    void Update()
    {
        DetectRedSpot();
    }

    void DetectRedSpot()
    {
        // Converte la texture in formato leggibile
        Texture2D tex = new Texture2D(thermalTexture.width, thermalTexture.height, TextureFormat.RGB24, false);
        RenderTexture.active = thermalTexture;
        tex.ReadPixels(new Rect(0, 0, thermalTexture.width, thermalTexture.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        // Scansiona i pixel per rilevare macchie rosse
        for (int x = 0; x < tex.width; x++)
        {
            for (int y = 0; y < tex.height; y++)
            {
                Color pixelColor = tex.GetPixel(x, y);

                // Controlla se il pixel supera la soglia del rosso
                if (pixelColor.r > redThreshold && pixelColor.g < redThreshold / 2 && pixelColor.b < redThreshold / 2)
                {
                    Debug.Log($"Macchia rossa rilevata in posizione: ({x}, {y})");

                    // Opzionale: Mostra la posizione nel mondo reale
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(x, y, Camera.main.nearClipPlane));
                    Debug.Log($"Posizione nel mondo: {worldPos}");

                    Destroy(tex); // Libera memoria
                    return;       // Esce non appena trova un punto rosso
                }
            }
        }

        Destroy(tex); // Libera memoria se non trova nulla
    }
}
