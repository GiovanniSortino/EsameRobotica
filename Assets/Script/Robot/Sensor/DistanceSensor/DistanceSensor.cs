using System;
using UnityEngine;

public class DistanceSensor : MonoBehaviour
{
    [Header("Configurazione Sensore")]
    public float maxDistance = 10f;       // Distanza massima di rilevamento
    public float fieldOfViewAngle = 30f;  // Angolo totale del cono visivo (usato per i Gizmos)
    public LayerMask detectionLayer;      // Layer da rilevare (es. "Obstacles")
    public bool isSensorActive = true;    // Stato attivo/disattivo del sensore
    public float currentDistance = Mathf.Infinity;  // Distanza rilevata (la più piccola fra i 3 raggi)

    // Se vuoi mantenere un singolo RaycastHit static, puoi tenerlo;
    // Ma per chiarezza, useremo RaycastHit locali nei 3 raggi
    public static RaycastHit hitLeft;
    public static RaycastHit hitCenter;
    public static RaycastHit hitRight;

    void Update()
    {
        if (isSensorActive)
        {
            DetectObjects3Rays();
        }
        else
        {
            currentDistance = Mathf.Infinity;
        }
    }

    void DetectObjects3Rays()
    {
        int layerMask = ~LayerMask.GetMask("Robot");

        Vector3 centerDir = transform.forward;

        Quaternion leftRot = Quaternion.Euler(0, -20f, 0);
        Vector3 leftDir = leftRot * centerDir;

        Quaternion rightRot = Quaternion.Euler(0, 20f, 0);
        Vector3 rightDir = rightRot * centerDir;

        float distCenter = Mathf.Infinity;
        float distLeft = Mathf.Infinity;
        float distRight = Mathf.Infinity;

        // Raggio centrale
        if (Physics.Raycast(transform.position + transform.forward * 0.01f, centerDir, out hitCenter, maxDistance, layerMask))
        {
            distCenter = hitCenter.distance;
            Debug.DrawRay(transform.position, centerDir * distCenter, Color.red);
        }
        else
        {
            Debug.DrawRay(transform.position + transform.forward * 0.01f, centerDir * maxDistance, Color.blue);
        }

        // Raggio sinistra
        if (Physics.Raycast(transform.position, leftDir, out hitLeft, maxDistance, layerMask))
        {
            distLeft = hitLeft.distance;
            Debug.DrawRay(transform.position + transform.forward * 0.01f, leftDir * distLeft, Color.red);
        }
        else
        {
            Debug.DrawRay(transform.position, leftDir * maxDistance, Color.blue);
        }

        // Raggio destra
        if (Physics.Raycast(transform.position, rightDir, out hitRight, maxDistance, layerMask))
        {
            distRight = hitRight.distance;
            Debug.DrawRay(transform.position, rightDir * distRight, Color.red);
        }
        else
        {
            Debug.DrawRay(transform.position, rightDir * maxDistance, Color.blue);
        }

    }

    // Metodo per visualizzare il campo visivo con un cono di Gizmos
    /*void OnDrawGizmos()
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
    }*/
}

