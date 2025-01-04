using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PercentageBattery : MonoBehaviour
{
    private float percentage = 100f;
    public float x_base;
    public float z_base;
    public float h_base;
    public float w_base;
    private float timer = 0f;

    // Update is called once per frame
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
                Debug.Log("Batteria: " + percentage + "%"); // Stampa lo stato della batteria
            }
        }

        // Verifica se il robot è all'interno della piattaforma
        if (IsInsidePlatform())
        {
            percentage = 100f;
            Debug.Log("Il robot si trova sulla piattaforma, si è ricaricato Batteria: 100%");
        }
        else
        {
            //Debug.Log("Il robot è fuori dalla piattaforma.");
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
