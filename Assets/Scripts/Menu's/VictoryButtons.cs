using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VictoryButtons : MonoBehaviour
{
    public string mainMenuName = "MainMenu";

    [Header("Fade")]
    public float fadeDuration = 1f;
    public Graphic[] buttons;

    private void Start()
    {
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        foreach (Graphic g in buttons)
        {
            Color c = g.color;
            c.a = 0f;
            g.color = c;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            foreach (Graphic g in buttons)
            {
                Color c = g.color;
                c.a = alpha;
                g.color = c;
            }
            yield return null;
        }
    }

    public void BackToMainMenu()
    {
        GameSceneFlow.Load(mainMenuName);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
