// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class NewBehaviourScript : MonoBehaviour{

//     public float velocità = 1f;
//     public float rotazioneVelocità = 10f; 
//     public float distanzaSicurezza = 5f;
//     public Transform target;
//     private Rigidbody rb;
//     public DistanceSensor distanceSensorCenter;
//     public DistanceSensor distanceSensorLeft;
//     public DistanceSensor distanceSensorRight;


//     void Start(){
//         rb = GetComponent<Rigidbody>();
//         if (rb == null){
//             Debug.Log("null");
//         }else{
//             Debug.Log("ok");
//         }
//     }

//     void FixedUpdate(){
//         Vector3 direzioneTarget = (target.position - transform.position).normalized;
//         float centerDistance = distanceSensorCenter.currentDistance;
//         float leftDistance = distanceSensorLeft.currentDistance;
//         float rightDistance = distanceSensorRight.currentDistance;

//         Debug.Log($"center {centerDistance}, left {leftDistance}, right{rightDistance}");

//         float distanzaTarget = Vector3.Distance(transform.position, target.position);

//         if(distanzaTarget > 1){
//             if(rightDistance < 3){
//                 Vector3 movimentoAvanti = transform.forward * 1 * Time.fixedDeltaTime;
//                 rb.MovePosition(rb.position + movimentoAvanti);

//                 Quaternion rotazioneAngolare = Quaternion.Euler(0f, -1 * Time.fixedDeltaTime, 0f);
//                 rb.MoveRotation(rb.rotation * rotazioneAngolare);
//             }else if(leftDistance < 3){
//                 Vector3 movimentoAvanti = transform.forward * 1 * Time.fixedDeltaTime;
//                 rb.MovePosition(rb.position + movimentoAvanti);

//                 Quaternion rotazioneAngolare = Quaternion.Euler(0f, 1 * Time.fixedDeltaTime, 0f);
//                 rb.MoveRotation(rb.rotation * rotazioneAngolare);
//             }else if (centerDistance < 1){
//                 Vector3 movimentoAvanti = transform.forward * -1 * Time.fixedDeltaTime;
//                 rb.MovePosition(rb.position + movimentoAvanti);
//             }else{
//                 rb.MovePosition(rb.position + direzioneTarget * velocità * Time.fixedDeltaTime);

//                 Quaternion targetRotazione = Quaternion.LookRotation(direzioneTarget);
//                 rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotazione, rotazioneVelocità * Time.fixedDeltaTime));
//             }
//         }
//     }

// }

using UnityEngine;

public class CarPathFollower : MonoBehaviour
{
    public Transform[] waypoints;         // Array di punti da seguire
    public float velocità = 5f;           // Velocità di movimento
    public float rotazioneVelocità = 5f;  // Velocità di rotazione
    public int waypointCorrente = 0;     // Indice del waypoint attuale
    public DistanceSensorCar distanceSensorCenter;
    public DistanceSensorCar distanceSensorLeft;
    public DistanceSensorCar distanceSensorRight;

    void Update(){
        if(distanceSensorLeft.currentDistance > 2 && distanceSensorCenter.currentDistance > 1 && distanceSensorRight.currentDistance > 2){
            Transform targetWaypoint = waypoints[waypointCorrente];
            Vector3 direzione = (targetWaypoint.position - transform.position).normalized;

            Quaternion rotazioneTarget = Quaternion.LookRotation(direzione);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotazioneTarget, rotazioneVelocità * Time.deltaTime);

            transform.position += transform.forward * velocità * Time.deltaTime;

            if (Vector3.Distance(transform.position, targetWaypoint.position) < 1f){
                waypointCorrente = (waypointCorrente + 1) % waypoints.Length;
            }
        }
    }
}

