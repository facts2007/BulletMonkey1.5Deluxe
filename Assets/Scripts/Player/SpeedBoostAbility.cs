using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpeedBoostAbility : MonoBehaviour
{
    [Min(1f)] public float speedMultiplier = 2f;
    [Min(0.1f)] public float duration = 10f;
    [Range(0f, 0.3f)] public float tintOpacity = 0.1f;
    public Image screenTint;
    public TMP_Text statusText;
    public GameObject hud;
    public bool IsActive => remaining > 0f;
    public float CurrentMultiplier => IsActive ? speedMultiplier : 1f;
    public float RemainingSeconds => remaining;
    private float remaining;
    [Min(0)] public int storedBananas;
    public TMP_Text inventoryText;
    public void Collect(){storedBananas++;RefreshPresentation();}
    public bool UseStored(){if(storedBananas<=0 || Time.timeScale<=0 || !isActiveAndEnabled || BossFusionEncounter.IsCutsceneActive)return false;var movement=GetComponent<PlayerMovement>();if(movement!=null&&!movement.enabled)return false;storedBananas--;Activate();return true;}

    public void Activate()
    {
        bool wasActive = IsActive;
        remaining = Mathf.Max(0.1f, duration);
        if (!wasActive && GameAudio.Instance != null) GameAudio.Instance.SetSpeedBoostActive(true);
        RefreshPresentation();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        if(Input.GetKeyDown(KeyCode.R))UseStored();
        Tick(Time.deltaTime);
    }

    public void Tick(float deltaTime)
    {
        if (!IsActive || deltaTime <= 0f) return;
        remaining = Mathf.Max(0f, remaining - deltaTime);
        if (!IsActive && GameAudio.Instance != null) GameAudio.Instance.SetSpeedBoostActive(false);
        RefreshPresentation();
    }

    private void OnEnable() { RefreshPresentation(); }

    private void OnDisable()
    {
        remaining = 0f;
        if (GameAudio.Instance != null) GameAudio.Instance.SetSpeedBoostActive(false);
        RefreshPresentation();
    }

    private void RefreshPresentation()
    {
        if(inventoryText!=null)inventoryText.text="[R]  x"+storedBananas;
        if (hud != null) hud.SetActive(IsActive);
        if (statusText != null) statusText.text = $"COCEY BANAN  ×{speedMultiplier:0.#}  ·  {remaining:0.0}s";
        if (screenTint != null)
        {
            float fade = Mathf.Min(1f, remaining);
            screenTint.color = new Color(0.85f, 0.015f, 0.025f, IsActive ? tintOpacity * fade : 0f);
            screenTint.raycastTarget = false;
        }
    }
}

