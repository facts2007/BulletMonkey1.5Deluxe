using UnityEngine;
using TMPro;

public class ShopUpgradeManager : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public Gun playerGun;

    [Header("Health Upgrade - Cost Per Level")]
    public int healthCostLevel1 = 50;
    public int healthCostLevel2 = 100;
    public int healthCostLevel3 = 150;
    public int healthIncreasePerTier = 20;
    private int healthTier = 0;

    [Header("Fire Rate Upgrade - Cost Per Level")]
    public int fireRateCostLevel1 = 50;
    public int fireRateCostLevel2 = 100;
    public int fireRateCostLevel3 = 150;
    public float fireRateIncreasePerTier = 2f;
    private int fireRateTier = 0;

    [Header("Max Ammo Upgrade - Cost Per Level")]
    public int maxAmmoCostLevel1 = 50;
    public int maxAmmoCostLevel2 = 100;
    public int maxAmmoCostLevel3 = 150;
    public int maxAmmoIncreasePerTier = 10;
    private int maxAmmoTier = 0;

    [Header("Button Labels (optional)")]
    public TextMeshProUGUI healthUpgradeButtonText;
    public TextMeshProUGUI fireRateUpgradeButtonText;
    public TextMeshProUGUI maxAmmoUpgradeButtonText;

    private void Start()
    {
        UpdateButtonLabels();
    }

    private int[] HealthCosts => new int[] { healthCostLevel1, healthCostLevel2, healthCostLevel3 };
    private int[] FireRateCosts => new int[] { fireRateCostLevel1, fireRateCostLevel2, fireRateCostLevel3 };
    private int[] MaxAmmoCosts => new int[] { maxAmmoCostLevel1, maxAmmoCostLevel2, maxAmmoCostLevel3 };

    
    public void UpgradeHealth()
    {
        if (healthTier >= HealthCosts.Length) return;

        int cost = HealthCosts[healthTier];

        if (PlayerPoints.Instance != null && PlayerPoints.Instance.SpendPoints(cost))
        {
            playerHealth.IncreaseMaxHealth(healthIncreasePerTier);
            healthTier++;
            UpdateButtonLabels();
        }
    }

    
    public void UpgradeFireRate()
    {
        if (fireRateTier >= FireRateCosts.Length) return;

        int cost = FireRateCosts[fireRateTier];

        if (PlayerPoints.Instance != null && PlayerPoints.Instance.SpendPoints(cost))
        {
            playerGun.fireRate += fireRateIncreasePerTier;
            fireRateTier++;
            UpdateButtonLabels();
        }
    }

    
    public void UpgradeMaxAmmo()
    {
        if (playerGun == null) return;
        if (maxAmmoTier >= MaxAmmoCosts.Length) return;

        int cost = MaxAmmoCosts[maxAmmoTier];

        if (PlayerPoints.Instance != null && PlayerPoints.Instance.SpendPoints(cost))
        {
            playerGun.maxAmmo += maxAmmoIncreasePerTier;
            playerGun.Reload();
            maxAmmoTier++;
            UpdateButtonLabels();
        }
    }

    private void UpdateButtonLabels()
    {
        if (healthUpgradeButtonText != null)
        {
            healthUpgradeButtonText.text = healthTier >= HealthCosts.Length
                ? "HP MAXED"
                : $"Upgrade HP ({HealthCosts[healthTier]} pts)";
        }

        if (fireRateUpgradeButtonText != null)
        {
            fireRateUpgradeButtonText.text = fireRateTier >= FireRateCosts.Length
                ? "Fire Rate MAXED"
                : $"Upgrade Fire Rate ({FireRateCosts[fireRateTier]} pts)";
        }

        if (maxAmmoUpgradeButtonText != null)
        {
            maxAmmoUpgradeButtonText.text = maxAmmoTier >= MaxAmmoCosts.Length
                ? "Max Ammo MAXED"
                : $"Upgrade Max Ammo ({MaxAmmoCosts[maxAmmoTier]} pts)";
        }
    }
}