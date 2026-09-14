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

    [Header("Ammo")]
    public int ammoCost = 25; 

    [Header("Button Labels (optional)")]
    public TextMeshProUGUI healthUpgradeButtonText;
    public TextMeshProUGUI fireRateUpgradeButtonText;
    public TextMeshProUGUI ammoButtonText;

    private void Start()
    {
        UpdateButtonLabels();
    }

    private int[] HealthCosts => new int[] { healthCostLevel1, healthCostLevel2, healthCostLevel3 };
    private int[] FireRateCosts => new int[] { fireRateCostLevel1, fireRateCostLevel2, fireRateCostLevel3 };

    
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

  
    public void BuyAmmo()
    {
        if (playerGun == null) return;
        if (playerGun.currentAmmo >= playerGun.maxAmmo) return;

        if (PlayerPoints.Instance != null && PlayerPoints.Instance.SpendPoints(ammoCost))
        {
            playerGun.Reload();
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

        if (ammoButtonText != null)
        {
            ammoButtonText.text = $"Buy Ammo ({ammoCost} pts)";
        }
    }
}