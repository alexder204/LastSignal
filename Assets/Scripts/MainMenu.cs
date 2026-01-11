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

    private void OpenCredits()
    {
        if (CreditsMenuUI == null)
        {
            return;
        }

        CreditsMenuUI.SetActive(true);
        //SceneManager.UnloadSceneAsync("MainMenu");
    }

    private void CloseCredits()
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





}
