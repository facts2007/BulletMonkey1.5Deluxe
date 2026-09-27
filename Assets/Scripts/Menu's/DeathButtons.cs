using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathButtons : MonoBehaviour
{
    public string mainSceneName = "MainScene";
    public string mainMenuName = "MainMenu";

    public void Retry()
    {
        GameSceneFlow.Load(mainSceneName);
    }

    public void BackToMainMenu()
    {
        GameSceneFlow.Load(mainMenuName);
    }
}
