using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    private Vector3 restingScale;
    private float scale = 1f;
    private bool hovered, selected, pressed;
    private Button button;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= Attach;
        SceneManager.sceneLoaded += Attach;
    }

    private static void Attach(Scene scene, LoadSceneMode mode)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var b in root.GetComponentsInChildren<Button>(true))
                if (b.GetComponent<MenuButtonMotion>() == null) b.gameObject.AddComponent<MenuButtonMotion>();
    }

    private void Awake() { restingScale = transform.localScale; button = GetComponent<Button>(); }
    private void OnEnable() { scale = .97f; hovered = selected = pressed = false; }
    private void Update()
    {
        bool interactive = button != null && button.IsInteractable();
        float target = !interactive ? 1f : pressed ? .97f : hovered || selected ? 1.025f : 1f;
        scale = Mathf.Lerp(scale, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        transform.localScale = restingScale * scale;
    }
    private void OnDisable() { transform.localScale = restingScale; hovered = selected = pressed = false; }
    public void OnPointerEnter(PointerEventData data) { hovered = true; }
    public void OnPointerExit(PointerEventData data) { hovered = pressed = false; }
    public void OnPointerDown(PointerEventData data) { pressed = true; }
    public void OnPointerUp(PointerEventData data) { pressed = false; }
    public void OnSelect(BaseEventData data) { selected = true; }
    public void OnDeselect(BaseEventData data) { selected = pressed = false; }
}
