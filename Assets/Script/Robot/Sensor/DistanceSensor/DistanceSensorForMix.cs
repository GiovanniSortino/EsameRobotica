using UnityEngine;

public class DistanceSensorForMix : MonoBehaviour{
    [Header("Configurazione Sensore")]
    public float maxDistance = 5;
    private int layerMask;

    void Start(){
        layerMask = ~LayerMask.GetMask("Robot");
    }

    void Update(){
        RaycastHit hit;
        if (Physics.Raycast(transform.position + transform.forward * 0.01f, transform.forward, out hit, maxDistance, layerMask) && !hit.collider.CompareTag("Person")){
            Debug.DrawRay(transform.position, transform.forward * hit.distance, Color.red);
        }else{
            Debug.DrawRay(transform.position, transform.forward * maxDistance, Color.green);
        }
    }

    public float DetectObjects(){
        RaycastHit hit;
        if (Physics.Raycast(transform.position + transform.forward * 0.01f, transform.forward, out hit, maxDistance, layerMask) && !hit.collider.CompareTag("Person")){
            return hit.distance;
        }else{
            Debug.DrawRay(transform.position, transform.forward * maxDistance, Color.green);
            return Mathf.Infinity;
        }
    }

    void OnDrawGizmos(){
        Gizmos.color = new Color(0, 1, 1, 0.3f);

        int segments = 20;
        float fov = 30;
        float halfFov = fov / 2;
        Vector3 startPoint = transform.forward.normalized * maxDistance;

        for (int i = 0; i <= segments; i++)
        {
            float step = -halfFov + i / (float)segments * fov;
            Quaternion rotation = Quaternion.Euler(0, step, 0);

            Vector3 rayDirection = rotation * startPoint;

            Gizmos.DrawLine(transform.position, transform.position + rayDirection);
        }
    }
}