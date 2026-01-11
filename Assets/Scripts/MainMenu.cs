using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    public GameObject CreditsMenuUI;
    public GameObject settingsMenuUI;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenCredits()
    {
        if (CreditsMenuUI == null)
        {
            return;
        }

        CreditsMenuUI.SetActive(true);
    }

    public void CloseCredits()
    {
        if (CreditsMenuUI == null)
        {
            return;
        }

        CreditsMenuUI.SetActive(false);
    }

    public void OpenSettings()
    {
        if (settingsMenuUI == null)
        {
            return;
        }

        settingsMenuUI.SetActive(true);
    }

public void CloseSettings()
    {
        if (settingsMenuUI == null)
        {
            return;
        }

        settingsMenuUI.SetActive(false);
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Game");
    }

    public void QuitGame()
    {
          // This works in a built application
        #if UNITY_STANDALONE
            Application.Quit();
        // This works in the Unity Editor
        #elif UNITY_EDITOR
            EditorApplication.isPlaying = false;
        #endif
    }




}
