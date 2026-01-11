using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject settingsMenuUI;
    [SerializeField] private Slider musicSlider;

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
}
