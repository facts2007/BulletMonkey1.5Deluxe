using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathButtons : MonoBehaviour
{
    public string mainSceneName = "MainScene";
    public string mainMenuName = "MainMenu";

    private void Start()
    {
        foreach (var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
        {
            MenuPanelMotion.Hide(button.gameObject, true);
            MenuPanelMotion.Show(button.gameObject, .3f);
        }
    }

    public void Retry()
    {
        GameSceneFlow.Load(GameSceneFlow.RetryScene=="DungeonLevel"?"DungeonLevel":mainSceneName);
    }

    public void BackToMainMenu()
    {
        GameSceneFlow.Load(mainMenuName);
    }
}

