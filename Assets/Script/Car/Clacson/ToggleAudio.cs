using UnityEngine;

public class ToggleAudio : MonoBehaviour
{
    public AudioSource audioSource;

    void Update()
    {
        if (CarPathFollower.isPlaying)
        {
            if (!audioSource.enabled) 
            {
                audioSource.enabled = true; 
                audioSource.Play();
            }
        }else 
        {
            audioSource.Stop();
            audioSource.enabled = false;
        }
    }
}
