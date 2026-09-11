using UnityEngine;
using TMPro;

public class ShopUpgradeManager : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public Gun playerGun;

    [Header("Health Upgrade (3 tiers)")]
    public int[] healthUpgradeCosts = { 50, 100, 150 };
    public int healthIncreasePerTier = 20;
    private int healthTier = 0;

    [Header("Fire Rate Upgrade (3 tiers)")]
    public int[] fireRateUpgradeCosts = { 50, 100, 150 };
    public float fireRateIncreasePerTier = 2f;
    private int fireRateTier = 0;

    [Header("Button Labels (optional)")]
    public TextMeshProUGUI healthUpgradeButtonText;
    public TextMeshProUGUI fireRateUpgradeButtonText;

    private void Start()
    {
        UpdateButtonLabels();
    }

    
    public void UpgradeHealth()
    {
        if (healthTier >= healthUpgradeCosts.Length) return;

        int cost = healthUpgradeCosts[healthTier];

        if (PlayerPoints.Instance != null && PlayerPoints.Instance.SpendPoints(cost))
        {
            playerHealth.IncreaseMaxHealth(healthIncreasePerTier);
            healthTier++;
            UpdateButtonLabels();
        }
    }


    public void UpgradeFireRate()
    {
        if (fireRateTier >= fireRateUpgradeCosts.Length) return;

        int cost = fireRateUpgradeCosts[fireRateTier];

        if (PlayerPoints.Instance != null && PlayerPoints.Instance.SpendPoints(cost))
        {
            playerGun.fireRate += fireRateIncreasePerTier;
            fireRateTier++;
            UpdateButtonLabels();
        }
    }

    private void UpdateButtonLabels()
    {
        if (healthUpgradeButtonText != null)
        {
            healthUpgradeButtonText.text = healthTier >= healthUpgradeCosts.Length
                ? "HP MAXED"
                : $"Upgrade HP ({healthUpgradeCosts[healthTier]} pts)";
        }

        if (fireRateUpgradeButtonText != null)
        {
            fireRateUpgradeButtonText.text = fireRateTier >= fireRateUpgradeCosts.Length
                ? "Fire Rate MAXED"
                : $"Upgrade Fire Rate ({fireRateUpgradeCosts[fireRateTier]} pts)";
        }
    }
}