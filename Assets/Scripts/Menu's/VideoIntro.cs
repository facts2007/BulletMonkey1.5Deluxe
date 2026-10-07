using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class VideoIntro : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Button      continueButton;
    public Button      quitButton;
    public string      gameSceneName = "MainScene";
    public float       fadeDuration  = 1.5f;
    private bool buttonsShown;

    void Start()
    {
        continueButton.gameObject.SetActive(false);
        quitButton.gameObject.SetActive(false);

        if(videoPlayer!=null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.prepareCompleted += Prepared;
            videoPlayer.errorReceived += VideoError;
            videoPlayer.isLooping=false;
            videoPlayer.Prepare();
        }
        else ShowButtons();
        StartCoroutine(MenuFallback());
    }
    private void Prepared(VideoPlayer vp){if(!buttonsShown)vp.Play();}
    private void VideoError(VideoPlayer vp,string error){ShowButtons();}
    private IEnumerator MenuFallback()
    {
        yield return new WaitForSecondsRealtime(8);
        if(videoPlayer==null || !videoPlayer.isPlaying)ShowButtons();
    }
    private void Update()
    {
        if(!buttonsShown && (Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Escape)))
        {if(videoPlayer!=null)videoPlayer.Stop();ShowButtons();}
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        ShowButtons();
    }
    private void ShowButtons()
    {
        if(buttonsShown)return;buttonsShown=true;
        MenuPanelMotion.Show(continueButton.gameObject, fadeDuration);
        MenuPanelMotion.Show(quitButton.gameObject, fadeDuration);
    }

    public void OnContinue()
    {
        GameSceneFlow.Load(gameSceneName);
    }
    private void OnDestroy()
    {
        if(videoPlayer==null)return;
        videoPlayer.loopPointReached-=OnVideoFinished;videoPlayer.prepareCompleted-=Prepared;videoPlayer.errorReceived-=VideoError;
    }

    public void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
