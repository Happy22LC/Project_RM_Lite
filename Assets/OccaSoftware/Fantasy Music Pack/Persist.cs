using UnityEngine;

public class Persist : MonoBehaviour
{
    private static Persist instance;
    private AudioSource _src;

    private void Awake()
    {
        // Keep only one Music object across scenes
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Auto-play if not already playing
        _src = GetComponent<AudioSource>();
        if (_src != null && !_src.isPlaying) _src.Play();
    }
}
