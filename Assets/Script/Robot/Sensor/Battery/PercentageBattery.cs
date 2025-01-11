using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Per usare UI

public class PercentageBattery : MonoBehaviour
{
    private DatabaseManager databaseManager;

    public Text batteryText; // Riferimento al componente Text per la batteria

    public static float percentage = 100f; // Percentuale della batteria
    public float x_base;
    public float z_base;
    public float h_base;
    public float w_base;
    private float timer = 0f;

    void Start()
    {
        databaseManager = DatabaseManager.GetDatabaseManager();

        // Inizializza il testo della batteria se non è stato assegnato
        if (batteryText != null)
        {
            batteryText.text = "Batteria: " + percentage + "%";
        }
        else
        {
            Debug.LogError("BatteryText non è stato assegnato nello script!");
        }
    }

    // Update viene chiamato una volta per frame
    void Update()
    {
        // Timer per scalare la batteria ogni secondo
        timer += Time.deltaTime; // Incrementa il timer
        if (timer >= 1f) // Controlla se è passato 1 secondo
        {
            if (percentage <= 0f)
            {
                timer = 0f; // Resetta il timer
                Debug.Log("Batteria scarica"); // Stampa lo stato della batteria
            }
            else
            {
                percentage -= 1f; // Riduce la batteria di 1
                timer = 0f; // Resetta il timer

                // Aggiorna il testo nella UI
                UpdateBatteryText();

                // Aggiorna il database
                _ = databaseManager.SetBatteryLevelAsync((int)percentage);
            }
        }

        // Verifica se il robot è all'interno della piattaforma
        if (IsInsidePlatform())
        {
            percentage = 100f; // Ricarica completa
            UpdateBatteryText(); // Aggiorna il testo
        }
    }

    // Metodo per aggiornare il testo nella UI
    void UpdateBatteryText()
    {
        if (batteryText != null)
        {
            batteryText.text = "Batteria: " + Mathf.Clamp(percentage, 0f, 100f) + "%";
        }
    }

    // Metodo per controllare se il robot è sulla piattaforma
    bool IsInsidePlatform()
    {
        // Ottieni la posizione del robot
        Vector3 robotPosition = transform.position;

        // Controlla se si trova nei limiti della piattaforma
        bool isInsideX = robotPosition.x >= x_base - w_base && robotPosition.x <= x_base + w_base;
        bool isInsideZ = robotPosition.z >= z_base - h_base && robotPosition.z <= z_base + h_base;

        // Restituisce vero se è nei limiti sia su X che su Z
        return isInsideX && isInsideZ;
    }
}