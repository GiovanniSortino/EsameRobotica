using UnityEngine;

public class DistanceSensor : MonoBehaviour
{
    [Header("Configurazione Sensore")]
    public float maxDistance = 10f;      
    public float fieldOfViewAngle = 30f;  
    public LayerMask detectionLayer;    
    public bool isSensorActive = true;    
    public float currentDistance = Mathf.Infinity;  

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

}

