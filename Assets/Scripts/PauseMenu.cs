using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{

    public static bool GameIsPaused = false;

    public GameObject pauseMenuUI;
    public GameObject settingsMenuUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        if (settingsMenuUI != null)
        {
            settingsMenuUI.SetActive(false);
        }
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    void Pause()
    {

        pauseMenuUI.SetActive(true);
        if (settingsMenuUI != null)
        {
            settingsMenuUI.SetActive(false);
        }
        Time.timeScale = 0f;
        GameIsPaused = true;
    }

    public void OpenSettings()
    {
        if (settingsMenuUI == null)
        {
            return;
        }

        settingsMenuUI.SetActive(true);
        pauseMenuUI.SetActive(false);
    }

    public void CloseSettings()
    {
        if (settingsMenuUI == null)
        {
            return;
        }

        settingsMenuUI.SetActive(false);
        pauseMenuUI.SetActive(true);
    }

    public void LoadMenu()
    {
        Debug.Log("Loading Menu...");

        GameIsPaused = true;
    }

    public void QuitGame()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
