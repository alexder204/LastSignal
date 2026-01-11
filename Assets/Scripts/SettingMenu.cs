using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject settingsMenuUI;
    [SerializeField] private Slider musicSlider;

    void OnEnable()
    {
        SyncMusicSlider();
    }

    public void BackToPauseMenu()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
            settingsMenuUI.SetActive(false);
        }
    }

    public void QuitGame()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
            settingsMenuUI.SetActive(false);
        }

        SceneManager.LoadScene("MainMenu");
    }

    public void SetMusicVolume(float volume)
    {
        var manager = AudioManager.Instance;
        if (manager != null)
        {
            manager.SetMusicVolume(volume);
            return;
        }

        AudioListener.volume = volume;
    }

    private void SyncMusicSlider()
    {
        if (musicSlider == null)
        {
            return;
        }

        var manager = AudioManager.Instance;
        if (manager != null)
        {
            musicSlider.SetValueWithoutNotify(manager.MusicVolume);
            return;
        }

        musicSlider.SetValueWithoutNotify(AudioListener.volume);
    }
}
