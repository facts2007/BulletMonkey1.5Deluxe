using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One banana buys one charged, continuous stream. Early release keeps the banana.</summary>
public class SuperShootAbility : MonoBehaviour
{
    [Header("Super banana")]
    public KeyCode activationKey = KeyCode.F;
    [Min(0.1f)] public float chargeSeconds = 1.5f;
    [Min(0.1f)] public float shootingSeconds = 5f;
    [Min(1f)] public float shotsPerSecond = 40f;
    [SerializeField] private bool hasBanana;

    [Header("References")]
    public Gun gun;
    public TMP_Text promptText;
    public Image chargeFill;
    public GameObject hud;

    public bool HasBanana => hasBanana;
    public bool IsCharging => charge > 0f && !IsSuperShooting;
    public bool IsSuperShooting => remaining > 0f;
    public float ChargeFraction => Mathf.Clamp01(charge / Mathf.Max(0.1f, chargeSeconds));
    public float RemainingSeconds => remaining;

    private float charge;
    private float remaining;
    private bool requireRelease;

    private void Awake()
    {
        if (gun == null) gun = GetComponentInChildren<Gun>(true);
        RefreshHud();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        // Shop interaction disables the gun. Do not spend a banana while shopping.
        if (gun == null || !gun.isActiveAndEnabled) { CancelCharge(); return; }
        Tick(Input.GetKey(activationKey), Time.deltaTime);
    }

    public void Tick(bool held, float deltaTime)
    {
        if (deltaTime <= 0f) return;
        if (!held) requireRelease = false;
        if (IsSuperShooting)
        {
            remaining = Mathf.Max(0f, remaining - deltaTime);
        }
        else if (hasBanana && held && !requireRelease)
        {
            charge += deltaTime;
            if (charge >= Mathf.Max(0.1f, chargeSeconds))
            {
                hasBanana = false;
                charge = 0f;
                remaining = Mathf.Max(0.1f, shootingSeconds);
                requireRelease = true;
            }
        }
        else charge = 0f;
        RefreshHud();
    }

    public bool TryCollectBanana()
    {
        if (hasBanana || IsSuperShooting) return false;
        hasBanana = true;
        RefreshHud();
        return true;
    }

    public void CancelCharge()
    {
        charge = 0f;
        RefreshHud();
    }

    private void OnDisable()
    {
        charge = remaining = 0f;
        RefreshHud();
    }

    private void RefreshHud()
    {
        if (hud != null) hud.SetActive(isActiveAndEnabled && (hasBanana || IsSuperShooting));
        if (promptText != null)
        {
            promptText.text = IsSuperShooting ? $"SUPER SHOOT  {remaining:0.0}s"
                : IsCharging ? $"CHARGING  {ChargeFraction:P0}"
                : $"SUPER BANANA  •  Hold {activationKey} to charge";
        }
        if (chargeFill != null)
            chargeFill.fillAmount = IsSuperShooting ? remaining / Mathf.Max(0.1f, shootingSeconds) : ChargeFraction;
    }
}
