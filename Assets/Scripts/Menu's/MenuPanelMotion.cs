using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Short, unscaled menu fades that can be reversed without leaving input blocked.</summary>
public class MenuPanelMotion : MonoBehaviour
{
    private CanvasGroup group;
    private Vector3 restingScale;
    private Coroutine animation;
    private bool visible;
    private bool scalePanel;
    private bool pendingShow;
    private float pendingSeconds;

    private void Awake()
    {
        restingScale = transform.localScale;
        scalePanel = GetComponent<Button>() == null;
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        visible = gameObject.activeSelf;
    }

    private void OnEnable()
    {
        if (!pendingShow) return;
        pendingShow = false;
        SetVisible(true, pendingSeconds, true);
    }

    public static bool IsVisible(GameObject panel)
    {
        if (panel == null) return false;
        var motion = panel.GetComponent<MenuPanelMotion>();
        return panel.activeSelf && (motion == null || motion.visible);
    }

    public static void Show(GameObject panel, float seconds = .2f)
    {
        if (panel == null) return;
        bool wasActive = panel.activeSelf;
        panel.SetActive(true);
        var motion = panel.GetComponent<MenuPanelMotion>();
        if (motion == null) motion = panel.AddComponent<MenuPanelMotion>();
        if (!panel.activeInHierarchy)
        {
            motion.pendingShow = true;
            motion.pendingSeconds = seconds;
            motion.visible = true;
            return;
        }
        motion.pendingShow = false;
        motion.SetVisible(true, seconds, !wasActive);
    }

    public static void Hide(GameObject panel, bool immediate = false)
    {
        if (panel == null) return;
        var motion = panel.GetComponent<MenuPanelMotion>();
        if (immediate || !panel.activeInHierarchy || motion == null)
        {
            if (motion != null) { motion.visible = false; motion.pendingShow = false; }
            panel.SetActive(false);
            return;
        }
        motion.SetVisible(false, .12f, false);
    }

    private void SetVisible(bool value, float seconds, bool fresh)
    {
        if (animation != null) StopCoroutine(animation);
        visible = value;
        group.interactable = value;
        group.blocksRaycasts = value;
        if (fresh)
        {
            group.alpha = 0f;
            if (scalePanel) transform.localScale = restingScale * .96f;
        }
        animation = StartCoroutine(Animate(value, Mathf.Max(.01f, seconds)));
    }

    private IEnumerator Animate(bool show, float seconds)
    {
        float startAlpha = group.alpha;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = restingScale * (show ? 1f : .98f);
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / seconds);
            group.alpha = Mathf.Lerp(startAlpha, show ? 1f : 0f, t);
            if (scalePanel) transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        group.alpha = show ? 1f : 0f;
        if (scalePanel) transform.localScale = endScale;
        animation = null;
        if (!show) gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (animation != null) StopCoroutine(animation);
        animation = null;
        visible = false;
        if (scalePanel) transform.localScale = restingScale;
    }
}
