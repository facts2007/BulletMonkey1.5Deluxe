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
    public string      gameSceneName = "GameScene";
    public float       fadeDuration  = 1.5f;

    void Start()
    {
        continueButton.gameObject.SetActive(false);
        quitButton.gameObject.SetActive(false);

        videoPlayer.loopPointReached += OnVideoFinished;
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        continueButton.gameObject.SetActive(true);
        quitButton.gameObject.SetActive(true);

        StartCoroutine(FadeIn(continueButton));
        StartCoroutine(FadeIn(quitButton));
    }

    IEnumerator FadeIn(Button btn)
    {
        CanvasGroup cg = btn.gameObject.AddComponent<CanvasGroup>();
        cg.alpha       = 0f;
        float elapsed  = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed  += Time.deltaTime;
            cg.alpha  = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        cg.alpha = 1f;
    }

    public void OnContinue()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}