using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathButtons : MonoBehaviour
{
    public string mainSceneName = "MainScene";
    public string mainMenuName = "MainMenu";

    public void Retry()
    {
        SceneManager.LoadScene(mainSceneName);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuName);
    }
}