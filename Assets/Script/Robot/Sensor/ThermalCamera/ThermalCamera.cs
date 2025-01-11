using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
    public string idPerson;
    public RaycastHit person;
    private DatabaseManager databaseManager;
    
    private float offsetX;
    private float offsetZ;

    private Vector2Int cella = new Vector2Int(0, 0);

    public Transform plane;

    public static bool detectPerson = false;

    private float timer = 0f;
    private int count;
    private int countFrame = 0;

    void Start(){
        countFrame = 0;
        databaseManager = DatabaseManager.GetDatabaseManager();
        offsetX = plane.localScale.x * 10f / 2f;
        offsetZ = plane.localScale.z * 10f / 2f;

        if (cam == null){
            Debug.LogError("Camera non assegnata!");
            return;
        }

        texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        if (redPercentageText == null)
        {
            Debug.LogError("Nessun testo assegnato per la percentuale di rosso!");
        }
    }

    void Update(){
        timer += Time.deltaTime; // Incrementa il timer
        countFrame++;
        if(countFrame >= 5){
            countFrame = 0;
            StartCoroutine(AnalyzeFrame());
        }
    }

    System.Collections.IEnumerator AnalyzeFrame()
    {
        yield return new WaitForEndOfFrame();

        texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        texture.Apply();

        if (display != null)
        {
            display.texture = texture;
        }

        Color dominantColor;
        int pixelCount = CountDominantColor(texture, out dominantColor);

        bool isTargetPresent = CheckMaterialPresence(texture, targetColor, colorTolerance, minPercentage, out detectedPixelCount, out detectedPercentage);

        UpdateRedPercentageText();
    }

    async void UpdateRedPercentageText()
    {
        if (redPercentageText != null)
        {
            redPercentageText.text = $"Percentuale di calore {(detectedPercentage * 100f):F2}%";
            detectedPercentage = detectedPercentage * 100f;

            if (detectedPercentage > 2.0f) // Se supera la soglia
            {
                person = DistanceSensor.hit; // Ottieni il RaycastHit

                if (person.collider != null) // Controlla se c'è un collider
                {
                    // Verifica se l'oggetto colpito ha il componente "Person"
                    var personComponent = person.collider.GetComponent<PersonComponent>();
                    if (personComponent != null)
                    {
                        idPerson = personComponent.GetID(); // Ottieni l'ID della persona

                        cella = RealToGrid(transform.position);

                        personDetect.text = $"Disperso {idPerson} \r\nCella {cella.x}, {cella.y}";
                        detectPerson = true;

                        if (count >= 20)
                        {
                            personDetect.text = "";
                            person.collider.transform.root.gameObject.SetActive(false);
                            //person.collider.gameObject.SetActive(false); // Disattiva l'oggetto della persona
                            _ = databaseManager.UpdateMissingPersonNodeAsync(idPerson, cella);

                            await databaseManager.ComunicaConPersonaleAsync("P1", $"Ho trovato la persona con ID {idPerson} nella cella {cella.x}, {cella.y}");
                            detectPerson = false;
                            count = 0;
                        }
                        else
                        {
                            count += 1;

                        }

                        //StartCoroutine(DeactivatePersonAfterDelay(person.collider.transform.root.gameObject, 5f));

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

    private Vector2Int RealToGrid(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x + offsetX);
        int y = Mathf.FloorToInt(position.z + offsetZ);
        x = x > 0 ? x : 0;
        y = y > 0 ? y : 0;
        return new Vector2Int(x, y);
    }
}