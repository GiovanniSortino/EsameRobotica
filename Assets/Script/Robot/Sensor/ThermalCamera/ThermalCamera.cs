using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
 
public class ThermalCamera : MonoBehaviour
{
    public Camera cam;                   // La fotocamera da cui acquisire i frame
    public RawImage display;             // (Opzionale) Visualizza il frame su UI
 
    public Color targetColor;            // Colore del materiale da rilevare
    public float colorTolerance = 0.1f;  // Tolleranza per il confronto dei colori
    public float minPercentage = 0.1f;   // Percentuale minima (10%) per considerare il materiale presente
 
    public int detectedPixelCount = 0;   // Numero di pixel rilevati
    public float detectedPercentage = 0; // Percentuale rilevata
 
    private Texture2D texture;           // Per memorizzare i dati dei pixel
 
    void Start()
    {
        // Controlli iniziali
        if (cam == null)
        {
            Debug.LogError("Camera non assegnata!");
            return;
        }
 
        // Inizializza la texture basata sulla dimensione dello schermo
        texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
 
        // Debug delle impostazioni della telecamera
        Debug.Log($"Telecamera attiva: {cam.name}");
        Debug.Log($"Clear Flags: {cam.clearFlags}");
        Debug.Log($"Culling Mask: {cam.cullingMask}");
    }
 
    void Update()
    {
        // Analizza il frame ad ogni aggiornamento
        StartCoroutine(AnalyzeFrame());
    }
 
    // Coroutine per attendere il rendering completo
    System.Collections.IEnumerator AnalyzeFrame()
    {
        // Aspetta la fine del frame per assicurarsi che il rendering sia completo
        yield return new WaitForEndOfFrame();
 
        // 1. Legge i dati dalla schermata attiva
        texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        texture.Apply();
 
        // 2. Mostra l'immagine nella UI (opzionale)
        if (display != null)
        {
            display.texture = texture;
        }
 
        // 3. Analizza i pixel per trovare il colore dominante
        Color dominantColor;
        int pixelCount = CountDominantColor(texture, out dominantColor);
        Debug.Log($"Colore dominante: {dominantColor}, Pixel: {pixelCount}");
 
        // 4. Controlla se il colore specifico è presente almeno al 10%
        bool isTargetPresent = CheckMaterialPresence(texture, targetColor, colorTolerance, minPercentage, out detectedPixelCount, out detectedPercentage);
        Debug.Log($"Materiale rilevato: {isTargetPresent}");
        Debug.Log($"Pixel rilevati: {detectedPixelCount}, Percentuale: {detectedPercentage * 100.0f}%");
    }
 
    // Funzione per trovare il colore dominante
    int CountDominantColor(Texture2D tex, out Color dominantColor)
    {
        // Memorizza la frequenza dei colori
        Dictionary<Color, int> colorCount = new Dictionary<Color, int>();
 
        // Scansiona ogni pixel
        Color[] pixels = tex.GetPixels();
        foreach (Color pixel in pixels)
        {
            // Arrotonda i valori per ridurre variazioni minime nei colori
            Color roundedColor = new Color(
                Mathf.Round(pixel.r * 10) / 10,
                Mathf.Round(pixel.g * 10) / 10,
                Mathf.Round(pixel.b * 10) / 10
            );
 
            if (colorCount.ContainsKey(roundedColor))
            {
                colorCount[roundedColor]++;
            }
            else
            {
                colorCount[roundedColor] = 1;
            }
        }
 
        // Trova il colore più frequente
        KeyValuePair<Color, int> mostFrequent = colorCount.OrderByDescending(c => c.Value).First();
        dominantColor = mostFrequent.Key;
        return mostFrequent.Value;
    }
 
    // Funzione per verificare la presenza del materiale specifico
    bool CheckMaterialPresence(Texture2D tex, Color targetColor, float tolerance, float minPercentage, out int matchingPixels, out float percentage)
    {
        matchingPixels = 0;
        int totalPixels = tex.width * tex.height;
 
        // Scansiona i pixel
        Color[] pixels = tex.GetPixels();
        foreach (Color pixel in pixels)
        {
            // Controlla se il colore è entro la tolleranza
            if (IsColorSimilar(pixel, targetColor, tolerance))
            {
                matchingPixels++;
            }
        }
 
        // Calcola la percentuale di pixel corrispondenti
        percentage = (float)matchingPixels / totalPixels;
 
        // Ritorna true se è almeno il 10%
        return percentage >= minPercentage;
    }
 
    // Funzione per confrontare i colori con una tolleranza
    bool IsColorSimilar(Color a, Color b, float tolerance)
    {
        return Mathf.Abs(a.r - b.r) <= tolerance &&
               Mathf.Abs(a.g - b.g) <= tolerance &&
               Mathf.Abs(a.b - b.b) <= tolerance;
    }
}