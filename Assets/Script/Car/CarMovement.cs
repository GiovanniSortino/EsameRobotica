using Unity.VisualScripting;
using UnityEngine;

public class CarPathFollower : MonoBehaviour
{
    public Transform[] waypoints;
    public float velocità = 5f;
    public float rotazioneVelocità = 5f;
    public int waypointCorrente = 0;
    public DistanceSensorCar distanceSensor;

    public AudioSource audioSource;
    public AudioClip clip;
    private bool isPlaying = false;

    void Update(){
        if(distanceSensor.currentDistance > 5){
            Transform targetWaypoint = waypoints[waypointCorrente];
            Vector3 direzione = (targetWaypoint.position - transform.position).normalized;

            Quaternion rotazioneTarget = Quaternion.LookRotation(direzione);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotazioneTarget, rotazioneVelocità * Time.deltaTime);

            transform.position += transform.forward * velocità * Time.deltaTime;

            if (Vector3.Distance(transform.position, targetWaypoint.position) < 1f){
                waypointCorrente = (waypointCorrente + 1) % waypoints.Length;
            }
            isPlaying = false;
        }else if(!isPlaying){
            audioSource.PlayOneShot(clip);
            isPlaying = true;
        }
    }
}

