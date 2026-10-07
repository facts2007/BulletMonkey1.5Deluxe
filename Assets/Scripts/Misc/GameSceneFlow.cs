using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSceneFlow
{
    public static string RetryScene {get;private set;}="MainScene";
    public static int EndingNumber {get;private set;}=1;
    public static bool IsLoading {get;private set;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        IsLoading=false;
        SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;
    }
    private static void Loaded(Scene scene,LoadSceneMode mode)
    {
        Time.timeScale=1;
        ConfigureScene(scene);
    }
    private static void ConfigureScene(Scene scene)
    {
        bool gameplay=scene.name=="MainScene"||scene.name=="DungeonLevel";
        if(gameplay)RetryScene=scene.name;
        if(scene.name=="VictoryScene")
            foreach(var root in scene.GetRootGameObjects())
                foreach(var label in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    if(label.name=="EndingLabel")label.text="ENDING "+EndingNumber+"/2";
        bool lockCursor=gameplay&&!PauseManager.GameIsPaused;
        Cursor.lockState=lockCursor?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!lockCursor;
    }
    private static void FinishedTransition()
    {
        var scene=SceneManager.GetActiveScene();
        // Refresh labels after scene startup, and preserve a newly opened tutorial's pause.
        if(scene.name!="MainScene"&&scene.name!="DungeonLevel")Time.timeScale=1;
        ConfigureScene(scene);
        IsLoading=false;
    }
    public static void Load(string scene)
    {
        if(IsLoading)return;
        if(!Application.CanStreamedLevelBeLoaded(scene)){Debug.LogError("Scene is not in build settings: "+scene);return;}
        if(scene=="MainScene" && SceneManager.GetActiveScene().name=="MainMenu"){TutorialOverlay.ResetForNewGame();DungeonEncounter.ResetLoadout();}
        if(scene=="VictoryScene")EndingNumber=SceneManager.GetActiveScene().name=="DungeonLevel"?2:1;
        IsLoading=true;
        SceneIrisTransition.Load(scene,FinishedTransition);
    }
}

