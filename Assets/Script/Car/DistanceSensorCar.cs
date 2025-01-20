using UnityEngine;

public class DistanceSensorCar : MonoBehaviour
{
    [Header("Configurazione Sensore")]
    public float maxDistance = 5f; 
    public float fieldOfViewAngle = 30f;
    public LayerMask detectionLayer; 
    public bool isSensorActive = true; 
    public float currentDistance = Mathf.Infinity; 

    void Update()
    {
        if (isSensorActive)
        {
            DetectObjects();
        }
    }

    void DetectObjects(){
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


   
    void OnDrawGizmos()
    {
        if (isSensorActive)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f); 
            DrawViewCone(transform.position, transform.forward, fieldOfViewAngle, maxDistance);
        }
    }

    void DrawViewCone(Vector3 position, Vector3 direction, float angle, float distance)
    {
        int segments = 20; 
        float halfAngle = angle / 2;

        Vector3 startPoint = direction.normalized * distance; 

        for (int i = 0; i <= segments; i++)
        {
            float step = -halfAngle + (i / (float)segments) * angle;
            Quaternion rotation = Quaternion.Euler(0, step, 0);

            Vector3 rayDirection = rotation * startPoint;

            Gizmos.DrawLine(position, position + rayDirection);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(position, position + direction.normalized * distance);
    }
}