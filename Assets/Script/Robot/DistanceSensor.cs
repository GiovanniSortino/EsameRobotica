using UnityEngine;

public class DistanceSensor : MonoBehaviour
{
    [Header("Configurazione Sensore")]
    public float maxDistance = 10f; // Distanza massima di rilevamento
    public float fieldOfViewAngle = 30f; // Angolo totale del cono visivo
    public LayerMask detectionLayer; // Layer da rilevare (es. "Obstacles")
    public bool isSensorActive = true; // Stato attivo/disattivo del sensore

    void Update()
    {
        // Attiva il sensore solo se lo stato � attivo
        if (isSensorActive)
        {
            DetectObjects();
        }
    }

    // Metodo per rilevare oggetti con un Raycast
    void DetectObjects()
    {
        RaycastHit hit;

        // Lancia un Raycast nella direzione forward
        if (Physics.Raycast(transform.position, transform.forward, out hit, maxDistance))
        {
            Debug.DrawRay(transform.position, transform.forward * hit.distance, Color.red);
            Debug.Log("Ostacolo: " + hit.collider.name +  " rilevato a distanza: " + hit.distance + " metri");
        }

        else
        {
            Debug.Log("Nessun Ostacolo rilevato");
        }
    }

    // Metodo per visualizzare il campo visivo con un cono di Gizmos
    void OnDrawGizmos()
    {
        if (isSensorActive)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f); // Colore azzurro trasparente
            DrawViewCone(transform.position, transform.forward, fieldOfViewAngle, maxDistance);
        }
    }

    // Metodo per disegnare il cono visivo
    void DrawViewCone(Vector3 position, Vector3 direction, float angle, float distance)
    {
        int segments = 20; // Numero di linee per il cono
        float halfAngle = angle / 2;

        Vector3 startPoint = direction.normalized * distance; // Punto iniziale della direzione principale

        for (int i = 0; i <= segments; i++)
        {
            // Calcola l'angolo per ciascun segmento del cono
            float step = -halfAngle + (i / (float)segments) * angle;
            Quaternion rotation = Quaternion.Euler(0, step, 0);

            // Calcola la direzione del raggio e il punto finale
            Vector3 rayDirection = rotation * startPoint;

            // Disegna una linea dal punto di partenza alla fine del cono
            Gizmos.DrawLine(position, position + rayDirection);
        }

        // Disegna una linea centrale per riferimento
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(position, position + direction.normalized * distance);
    }
}
