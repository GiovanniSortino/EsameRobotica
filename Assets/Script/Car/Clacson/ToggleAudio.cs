using UnityEngine;

public class ToggleAudio : MonoBehaviour
{
    public AudioSource audioSource;

    void Update()
    {
        if (CarPathFollower.isPlaying)
        {
            if (!audioSource.enabled) // Se l'AudioSource è disattivato
            {
                audioSource.enabled = true; // Attivalo
                audioSource.Play(); // Riproduci l'audio
            }
        }else 
        {
            audioSource.Stop(); // Ferma l'audio
            audioSource.enabled = false; // Disattivalo
        }
    }
}
