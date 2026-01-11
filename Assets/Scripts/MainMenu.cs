using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenu : MonoBehaviour
{

    public GameObject CreditsMenuUI;

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

    public void StartGame()
    {
        SceneManager.LoadScene("Game");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

}