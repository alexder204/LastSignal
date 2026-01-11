using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float defaultMusicVolume = 1f;

    private const string MusicVolumeKey = "MusicVolume";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        var volume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
        ApplyVolume(Mathf.Clamp01(volume));
        TryPlayMusic();
    }

    public float MusicVolume
    {
        get
        {
            if (musicSource != null)
            {
                return musicSource.volume;
            }

            return AudioListener.volume;
        }
    }

    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        ApplyVolume(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, volume);
    }

    private void ApplyVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = volume;
            return;
        }

        AudioListener.volume = volume;
    }

    private void TryPlayMusic()
    {
        if (musicSource == null || musicSource.clip == null)
        {
            return;
        }

        if (!musicSource.isPlaying)
        {
            musicSource.loop = true;
            musicSource.Play();
        }
    }
}
