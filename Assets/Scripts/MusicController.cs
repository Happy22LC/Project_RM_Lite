using UnityEngine;

public class MusicController : MonoBehaviour
{
    private AudioSource audioSource;
    private bool isMuted = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Hook this to the button
    public void ToggleMute()
    {
        if (!audioSource) return;

        if (isMuted)
        {
            audioSource.Play();   // resume
            isMuted = false;
        }
        else
        {
            audioSource.Pause();  // pause
            isMuted = true;
        }
    }
    // Did not work properly , it did not allow me to add music controller component
}
