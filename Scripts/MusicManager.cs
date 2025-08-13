// MusicManager.cs
using UnityEngine;

[DisallowMultipleComponent]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource; // Asigna en el inspector
    public AudioSource Audio => audioSource;

    const string VOL_KEY = "music_volume";
    const float  DEFAULT_VOL = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        float v = PlayerPrefs.GetFloat(VOL_KEY, DEFAULT_VOL);
        if (audioSource) audioSource.volume = v;
    }

    public void SetVolume(float v)
    {
        if (audioSource) audioSource.volume = v;
        PlayerPrefs.SetFloat(VOL_KEY, v);
        PlayerPrefs.Save();
    }
}