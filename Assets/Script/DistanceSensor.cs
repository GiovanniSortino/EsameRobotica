using UnityEngine;

public class DistanceSensor : MonoBehaviour
{
    public float maxDistance = 10f; // Distanza massima del sensore
    public LayerMask detectionLayer; // Layer per gli ostacoli

    void Update()
    {
        RaycastHit hit;
        Vector3 forwardDirection = transform.forward;

        // Lancia un Raycast dal sensore in avanti
        if (Physics.Raycast(transform.position, forwardDirection, out hit, maxDistance, detectionLayer))
        {

            Debug.DrawRay(transform.position, forwardDirection * hit.distance, Color.red);
            Debug.Log("Ostacolo rilevato a distanza: " + hit.distance + " metri");
        }
        else
        {
            Debug.DrawRay(transform.position, forwardDirection * maxDistance, Color.green);
        }
    }
}
