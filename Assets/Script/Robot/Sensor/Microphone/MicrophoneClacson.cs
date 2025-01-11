using UnityEngine;

public class MicrophoneClacson : MonoBehaviour
{
    public AudioSource audioSource;
    public Transform microphoneTransform;
    public float detectionRadius = 10f;
    public LayerMask soundLayer;
    public float detectionThreshold = 0.01f;
    public static bool detectClacson = false;

    void Update()
    {
        DetectSounds();
    }

    void DetectSounds()
    {
        detectClacson = false;

        Collider[] colliders = Physics.OverlapSphere(microphoneTransform.position, detectionRadius, soundLayer);

        foreach (var collider in colliders)
        {
            // Filtra solo gli oggetti con un tag specifico
            if (collider.CompareTag("Clacson"))
            {
                AudioSource source = collider.GetComponent<AudioSource>();
                if (source != null && source.isPlaying)
                {
                    float distance = Vector3.Distance(microphoneTransform.position, source.transform.position);
                    float perceivedVolume = source.volume / (distance * distance);

                    if (perceivedVolume > detectionThreshold)
                    {
                        detectClacson = true;
                        return;
                    }
                }
            }
        }
    }
}
