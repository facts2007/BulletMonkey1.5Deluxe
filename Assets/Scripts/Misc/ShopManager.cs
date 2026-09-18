using UnityEngine;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Player References")]
    public Transform player;          
    public PlayerHealth playerHealth;
    public Gun playerGun;

    [Header("Interaction")]
    public float interactionRange = 3.5f;
    public KeyCode interactKey = KeyCode.E;
    public GameObject interactionPrompt;   
    public GameObject shopPanel;           

    private bool playerInRange;
    private bool shopOpen;

    [Header("Health Upgrade - Cost Per Level")]
    public int healthCostLevel1 = 50;
    public int healthCostLevel2 = 100;
    public int healthCostLevel3 = 150;

    [Header("Health Upgrade - HP Gained Per Level")]
    public int healthIncreaseLevel1 = 20;
    public int healthIncreaseLevel2 = 25;
    public int healthIncreaseLevel3 = 30;

    private int healthLevel = 0;

    [Header("Fire Rate Upgrade - Cost Per Level")]
    public int fireRateCostLevel1 = 50;
    public int fireRateCostLevel2 = 100;
    public int fireRateCostLevel3 = 150;

    [Header("Fire Rate Upgrade - Fire Rate Gained Per Level")]
    public float fireRateIncreaseLevel1 = 2f;
    public float fireRateIncreaseLevel2 = 3f;
    public float fireRateIncreaseLevel3 = 4f;

    private int fireRateLevel = 0;

    [Header("Max Ammo Upgrade - Cost Per Level")]
    public int maxAmmoCostLevel1 = 50;
    public int maxAmmoCostLevel2 = 100;
    public int maxAmmoCostLevel3 = 150;

    [Header("Max Ammo Upgrade - Ammo Gained Per Level")]
    public int maxAmmoIncreaseLevel1 = 10;
    public int maxAmmoIncreaseLevel2 = 15;
    public int maxAmmoIncreaseLevel3 = 20;

    private int maxAmmoLevel = 0;

    [Header("Shop UI Labels")]
    public TextMeshProUGUI healthUpgradeButtonText;
    public TextMeshProUGUI fireRateUpgradeButtonText;
    public TextMeshProUGUI maxAmmoUpgradeButtonText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (playerGun == null)
        {
            playerGun = Gun.Instance;
        }

        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);

        UpdateShopUI();
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactionRange;

        if (!shopOpen)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(playerInRange);
            }

            if (playerInRange && Input.GetKeyDown(interactKey))
            {
                OpenShop();
            }
        }
        else
        {
            if (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.Escape) || !playerInRange)
            {
                CloseShop();
            }
        }
    }


    private void OpenShop()
    {
        shopOpen = true;

        if (shopPanel != null) shopPanel.SetActive(true);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);

        if (playerGun != null) playerGun.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;

        UpdateShopUI();
    }

    public void CloseShop()
    {
        shopOpen = false;

        if (shopPanel != null) shopPanel.SetActive(false);

        if (playerGun != null) playerGun.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Time.timeScale = 1f;
    }

    private bool SpendCredits(int amount)
    {
        if (PlayerParts.Instance == null) return false;
        return PlayerParts.Instance.SpendParts(amount);
    }


    public void UpgradeHealth()
    {
        if (healthLevel >= 3) return;

        int cost = GetLevelValue(healthLevel, healthCostLevel1, healthCostLevel2, healthCostLevel3);

        if (SpendCredits(cost))
        {
            int increase = GetLevelValue(healthLevel, healthIncreaseLevel1, healthIncreaseLevel2, healthIncreaseLevel3);
            playerHealth.IncreaseMaxHealth(increase);
            healthLevel++;
            UpdateShopUI();
        }
    }

    public void UpgradeFireRate()
    {
        if (fireRateLevel >= 3) return;

        int cost = GetLevelValue(fireRateLevel, fireRateCostLevel1, fireRateCostLevel2, fireRateCostLevel3);

        if (SpendCredits(cost))
        {
            float increase = GetLevelValue(fireRateLevel, fireRateIncreaseLevel1, fireRateIncreaseLevel2, fireRateIncreaseLevel3);
            playerGun.fireRate += increase;
            fireRateLevel++;
            UpdateShopUI();
        }
    }

    public void UpgradeMaxAmmo()
    {
        if (maxAmmoLevel >= 3) return;

        int cost = GetLevelValue(maxAmmoLevel, maxAmmoCostLevel1, maxAmmoCostLevel2, maxAmmoCostLevel3);

        if (SpendCredits(cost))
        {
            int increase = GetLevelValue(maxAmmoLevel, maxAmmoIncreaseLevel1, maxAmmoIncreaseLevel2, maxAmmoIncreaseLevel3);
            playerGun.maxAmmo += increase;
            playerGun.Reload();
            maxAmmoLevel++;
            UpdateShopUI();
        }
    }

    private int GetLevelValue(int level, int v1, int v2, int v3)
    {
        if (level == 0) return v1;
        if (level == 1) return v2;
        return v3;
    }

    private float GetLevelValue(int level, float v1, float v2, float v3)
    {
        if (level == 0) return v1;
        if (level == 1) return v2;
        return v3;
    }


    private void UpdateShopUI()
    {
        SetUpgradeLabel(healthUpgradeButtonText, "HP", healthLevel, healthCostLevel1, healthCostLevel2, healthCostLevel3);
        SetUpgradeLabel(fireRateUpgradeButtonText, "Fire Rate", fireRateLevel, fireRateCostLevel1, fireRateCostLevel2, fireRateCostLevel3);
        SetUpgradeLabel(maxAmmoUpgradeButtonText, "Max Ammo", maxAmmoLevel, maxAmmoCostLevel1, maxAmmoCostLevel2, maxAmmoCostLevel3);
    }

    private void SetUpgradeLabel(TextMeshProUGUI label, string upgradeName, int level, int cost1, int cost2, int cost3)
    {
        if (label == null) return;

        if (level >= 3)
        {
            label.text = $"{upgradeName}\nLevel 3/3 - MAXED";
            return;
        }

        int nextCost = GetLevelValue(level, cost1, cost2, cost3);
        label.text = $"{upgradeName}\nLevel {level}/3 - Upgrade: {nextCost} pts";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}