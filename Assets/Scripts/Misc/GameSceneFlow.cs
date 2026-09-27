using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSceneFlow
{
    public static bool IsLoading {get;private set;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        IsLoading=false;
        SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;
    }
    private static void Loaded(Scene scene,LoadSceneMode mode)
    {
        IsLoading=false;Time.timeScale=1;
        bool gameplay=scene.name=="MainScene";
        Cursor.lockState=gameplay?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!gameplay;
    }
    public static void Load(string scene)
    {
        if(IsLoading)return;
        if(!Application.CanStreamedLevelBeLoaded(scene)){Debug.LogError("Scene is not in build settings: "+scene);return;}
        IsLoading=true;Time.timeScale=1;SceneManager.LoadScene(scene);
    }
}
