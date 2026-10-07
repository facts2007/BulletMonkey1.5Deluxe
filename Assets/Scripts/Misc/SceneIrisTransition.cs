using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneIrisTransition : MonoBehaviour
{
    private static SceneIrisTransition instance;
    private GameObject overlay;
    private IrisTransitionGraphic iris;
    private const float CloseSeconds = .55f;
    private const float OpenSeconds = .55f;

    public static void Load(string scene, Action finished)
    {
        if (instance == null)
        {
            var root = new GameObject("Scene circle transition");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<SceneIrisTransition>();
            instance.CreateOverlay();
        }
        instance.StartCoroutine(instance.ChangeScene(scene, finished));
    }

    private void CreateOverlay()
    {
        overlay = new GameObject("Black iris", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        overlay.transform.SetParent(transform, false);
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        var image = new GameObject("Circular opening", typeof(RectTransform), typeof(CanvasRenderer), typeof(IrisTransitionGraphic));
        image.transform.SetParent(overlay.transform, false);
        var rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        iris = image.GetComponent<IrisTransitionGraphic>();
        iris.raycastTarget = true;
        overlay.SetActive(false);
    }

    private IEnumerator ChangeScene(string scene, Action finished)
    {
        overlay.SetActive(true);
        Time.timeScale = 0f;
        yield return Animate(false);
        // Keep the old scene hidden until the new scene has rendered its first frame.
        var operation = SceneManager.LoadSceneAsync(scene);
        while (!operation.isDone) yield return null;
        yield return null;
        yield return Animate(true);
        overlay.SetActive(false);
        finished();
    }

    private IEnumerator Animate(bool opening)
    {
        float seconds = opening ? OpenSeconds : CloseSeconds;
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / seconds);
            float value = opening ? t : 1f - t;
            iris.SetOpening(value, Mathf.InverseLerp(.25f, 0f, value));
            yield return null;
        }
        iris.SetOpening(opening ? 1f : 0f, opening ? 0f : 1f);
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}
