using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class ThermalCamera : MonoBehaviour
{
    public Camera cam;                   // La fotocamera da cui acquisire i frame
    public RawImage display;             // (Opzionale) Visualizza il frame su UI

    public Text redPercentageText;       // Testo UI per mostrare la percentuale di rosso
    public Text personDetect;       // Testo UI per mostrare la percentuale di rosso

    public Color targetColor;            // Colore del materiale da rilevare
    public float colorTolerance = 0.1f;  // Tolleranza per il confronto dei colori
    public float minPercentage = 0.1f;   // Percentuale minima (10%) per considerare il materiale presente

    public int detectedPixelCount = 0;   // Numero di pixel rilevati
    public float detectedPercentage = 0; // Percentuale rilevata

    public Texture2D texture;           // Per memorizzare i dati dei pixel
    private float percentage;
    private int countFrame;
    public string idPerson;
    public RaycastHit person;

    void Start(){
        countFrame = 0;
        if (cam == null){
            Debug.LogError("Camera non assegnata!");
            return;
        }

        // Inizializza la texture basata sulla dimensione dello schermo
        texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        // Debug delle impostazioni della telecamera
        //Debug.Log($"Telecamera attiva: {cam.name}");
        //Debug.Log($"Clear Flags: {cam.clearFlags}");
        //Debug.Log($"Culling Mask: {cam.cullingMask}");

        // Controllo del testo UI
        if (redPercentageText == null)
        {
            Debug.LogError("Nessun testo assegnato per la percentuale di rosso!");
        }
    }

    void Update(){
        countFrame++;
        if(countFrame >= 5){
            countFrame = 0;
            StartCoroutine(AnalyzeFrame());
        }
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
        //Debug.Log($"Colore dominante: {dominantColor}, Pixel: {pixelCount}");

        // 4. Controlla la percentuale di rosso
        bool isTargetPresent = CheckMaterialPresence(texture, targetColor, colorTolerance, minPercentage, out detectedPixelCount, out detectedPercentage);
        //Debug.Log($"Materiale rilevato: {isTargetPresent}");
        //Debug.Log($"Pixel rilevati: {detectedPixelCount}, Percentuale: {detectedPercentage * 100.0f}%");

        // 5. Aggiorna la UI con la percentuale di rosso
        UpdateRedPercentageText();
    }

    // Funzione per aggiornare il testo della percentuale di rosso
    void UpdateRedPercentageText()
    {
        if (redPercentageText != null)
        {
            // Aggiorna la percentuale di calore
            redPercentageText.text = $"Percentuale di calore {(detectedPercentage * 100f):F2}%";
            detectedPercentage = detectedPercentage * 100f;

            if (detectedPercentage > 2.0f) // Se supera la soglia
            {
                personDetect.text = "Disperso Rilevato";
                person = DistanceSensor.hit; // Ottieni il RaycastHit

                if (person.collider != null) // Controlla se c'è un collider
                {
                    // Verifica se l'oggetto colpito ha il componente "Person"
                    var personComponent = person.collider.GetComponent<PersonComponent>();
                    if (personComponent != null)
                    {
                        idPerson = personComponent.GetID(); // Ottieni l'ID della persona
                        Debug.Log($"ID Persona rilevata: {idPerson}");
                    }
                    else
                    {
                        Debug.LogError("Il componente Person non è presente sull'oggetto colpito.");
                    }
                }
            }
            else
            {
                personDetect.text = ""; // Nessun disperso rilevato
            }
        }
    }


    // Funzione per trovare il colore dominante
    int CountDominantColor(Texture2D tex, out Color dominantColor)
    {
        Dictionary<Color, int> colorCount = new Dictionary<Color, int>();

        Color[] pixels = tex.GetPixels();
        foreach (Color pixel in pixels)
        {
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

        KeyValuePair<Color, int> mostFrequent = colorCount.OrderByDescending(c => c.Value).First();
        dominantColor = mostFrequent.Key;
        return mostFrequent.Value;
    }

    public bool CheckMaterialPresence(Texture2D tex, Color targetColor, float tolerance, float minPercentage, out int matchingPixels, out float percentage)
    {
        matchingPixels = 0;
        int totalPixels = tex.width * tex.height;

        Color[] pixels = tex.GetPixels();
        foreach (Color pixel in pixels)
        {
            if (IsColorSimilar(pixel, targetColor, tolerance))
            {
                matchingPixels++;
            }
        }

        percentage = (float)matchingPixels / totalPixels;
        
        return percentage >= minPercentage;
    }

    bool IsColorSimilar(Color a, Color b, float tolerance)
    {
        return Mathf.Abs(a.r - b.r) <= tolerance &&
               Mathf.Abs(a.g - b.g) <= tolerance &&
               Mathf.Abs(a.b - b.b) <= tolerance;
    }
}