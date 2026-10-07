using System.Collections;
using UnityEngine;
public class TutorialOverlay : MonoBehaviour
{
    public static TutorialOverlay Instance {get;private set;}
    private static bool seenThisRun;
    public PauseManager pause;
    public GameObject panel;
    public bool autoOpen=true;
    public bool IsOpen=>MenuPanelMotion.IsVisible(panel);
    private bool openedFromPause;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetForNewGame(){seenThisRun=false;}
    private void Awake(){Instance=this;if(panel!=null)panel.SetActive(false);}
    private IEnumerator Start(){yield return null;if(autoOpen&&!seenThisRun)Open();}
    public void Open()
    {
        if(IsOpen || DungeonEncounter.IsCutsceneActive || BossFusionEncounter.IsCutsceneActive)return;
        openedFromPause=PauseManager.GameIsPaused;if(!openedFromPause)pause.Pause();
        seenThisRun=true;pause.CloseSettings();MenuPanelMotion.Hide(pause.pausePanel,true);MenuPanelMotion.Show(panel);
    }
    public void Close(){if(!IsOpen)return;MenuPanelMotion.Hide(panel);if(openedFromPause){MenuPanelMotion.Show(pause.pausePanel);pause.CloseSettings();}else pause.Resume();}
    private void OnDestroy(){if(Instance==this)Instance=null;}
}
