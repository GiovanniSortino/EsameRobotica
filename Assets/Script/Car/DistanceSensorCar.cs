using UnityEngine;

public class DistanceSensorCar : MonoBehaviour
{
    [Header("Configurazione Sensore")]
    public float maxDistance = 10f; // Distanza massima di rilevamento
    public float fieldOfViewAngle = 30f; // Angolo totale del cono visivo
    public LayerMask detectionLayer; // Layer da rilevare (es. "Obstacles")
    public bool isSensorActive = true; // Stato attivo/disattivo del sensore
    public float currentDistance = Mathf.Infinity; // Variabile pubblica per la distanza

    void Update()
    {
        // Attiva il sensore solo se lo stato è attivo
        if (isSensorActive)
        {
            DetectObjects();
        }
    }

    // Metodo per rilevare oggetti con un Raycast
    void DetectObjects(){
        // Stato del rilevamento
        bool isRobotDetected = false;

        Collider[] hits = Physics.OverlapSphere(transform.position, maxDistance);

        foreach (var hit in hits){
            if (hit.CompareTag("Robot")){
                Vector3 targetDirection = hit.transform.position - transform.position;

                float angleToTarget = Vector3.Angle(transform.forward, targetDirection);

                if (angleToTarget <= fieldOfViewAngle / 2){
                    float distance = Vector3.Distance(transform.position, hit.transform.position);

                    Debug.DrawRay(transform.position, targetDirection.normalized * distance, Color.red);

                    currentDistance = distance;
                    isRobotDetected = true;

                    break;
                }
            }
        }

        if (!isRobotDetected){
            currentDistance = Mathf.Infinity;
        }
    }


    // void DetectObjects(){

    //     bool isRobotDetected;
    //     Collider[] hits = Physics.OverlapSphere(transform.position, maxDistance);

    //     foreach (var hit in hits){
    //         Vector3 targetDirection = hit.transform.position - transform.position;

    //         float angleToTarget = Vector3.Angle(transform.forward, targetDirection);

    //         // Verifica se è dentro il Field of View
    //         if (angleToTarget <= fieldOfViewAngle / 2){
    //             // Controlla se il tag è "Robot"
    //             if (hit.CompareTag("Robot")){
    //                 Debug.DrawLine(transform.position, hit.transform.position, Color.red);
    //                 Debug.Log("Robot rilevato dentro il FOV!");
    //             }
    //         }
    //     }
    // }


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