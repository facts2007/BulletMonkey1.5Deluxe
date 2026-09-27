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
    public int healthCostLevel4 = 200;

    [Header("Health Upgrade - HP Gained Per Level")]
    public int healthIncreaseLevel1 = 20;
    public int healthIncreaseLevel2 = 25;
    public int healthIncreaseLevel3 = 30;
    public int healthIncreaseLevel4 = 35;

    private int healthLevel = 0;

    [Header("Fire Rate Upgrade - Cost Per Level")]
    public int fireRateCostLevel1 = 50;
    public int fireRateCostLevel2 = 100;
    public int fireRateCostLevel3 = 150;
    public int fireRateCostLevel4 = 200;

    [Header("Fire Rate Upgrade - Fire Rate Gained Per Level")]
    public float fireRateIncreaseLevel1 = 2f;
    public float fireRateIncreaseLevel2 = 3f;
    public float fireRateIncreaseLevel3 = 4f;
    public float fireRateIncreaseLevel4 = 5;

    private int fireRateLevel = 0;

    [Header("Max Ammo Upgrade - Cost Per Level")]
    public int maxAmmoCostLevel1 = 50;
    public int maxAmmoCostLevel2 = 100;
    public int maxAmmoCostLevel3 = 150;
    public int maxAmmoCostLevel4 = 200;

    [Header("Max Ammo Upgrade - Ammo Gained Per Level")]
    public int maxAmmoIncreaseLevel1 = 10;
    public int maxAmmoIncreaseLevel2 = 15;
    public int maxAmmoIncreaseLevel3 = 20;
    public int maxAmmoIncreaseLevel4 = 25;

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
        if(PauseManager.GameIsPaused)return;

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
        var gate=GetComponent<KabuWaveShop>();
        if(gate!=null && !gate.TryInteract())return;
        shopOpen = true;
        if(GameAudio.Instance!=null)GameAudio.Instance.SetShopOpen(true);

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
        if(GameAudio.Instance!=null)GameAudio.Instance.SetShopOpen(false);

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
        if (healthLevel >= 4) return;

        int cost = GetLevelValue(healthLevel, healthCostLevel1, healthCostLevel2, healthCostLevel3, healthCostLevel4);

        if (SpendCredits(cost))
        {
            int increase = GetLevelValue(healthLevel, healthIncreaseLevel1, healthIncreaseLevel2, healthIncreaseLevel3, healthIncreaseLevel4);
            playerHealth.IncreaseMaxHealth(increase);
            healthLevel++;
            UpdateShopUI();
        }
    }

    public void UpgradeFireRate()
    {
        if (fireRateLevel >= 4) return;

        int cost = GetLevelValue(fireRateLevel, fireRateCostLevel1, fireRateCostLevel2, fireRateCostLevel3, fireRateCostLevel4);

        if (SpendCredits(cost))
        {
            float increase = GetLevelValue(fireRateLevel, fireRateIncreaseLevel1, fireRateIncreaseLevel2, fireRateIncreaseLevel3, fireRateIncreaseLevel4);
            playerGun.fireRate += increase;
            fireRateLevel++;
            UpdateShopUI();
        }
    }

    public void UpgradeMaxAmmo()
    {
        if (maxAmmoLevel >= 4) return;

        int cost = GetLevelValue(maxAmmoLevel, maxAmmoCostLevel1, maxAmmoCostLevel2, maxAmmoCostLevel3, maxAmmoCostLevel4);

        if (SpendCredits(cost))
        {
            int increase = GetLevelValue(maxAmmoLevel, maxAmmoIncreaseLevel1, maxAmmoIncreaseLevel2, maxAmmoIncreaseLevel3, maxAmmoIncreaseLevel4);
            playerGun.maxAmmo += increase;
            playerGun.Reload();
            maxAmmoLevel++;
            UpdateShopUI();
        }
    }

    private int GetLevelValue(int level, int v1, int v2, int v3, int v4)
    {
        if (level == 0) return v1;
        if (level == 1) return v2;
        if(level==2)return v3;
        return v4;
    }

    private float GetLevelValue(int level, float v1, float v2, float v3, float v4)
    {
        if (level == 0) return v1;
        if (level == 1) return v2;
        if(level==2)return v3;
        return v4;
    }


    private void UpdateShopUI()
    {
        SetUpgradeLabel(healthUpgradeButtonText, "HP", healthLevel, healthCostLevel1, healthCostLevel2, healthCostLevel3, healthCostLevel4);
        SetUpgradeLabel(fireRateUpgradeButtonText, "Fire Rate", fireRateLevel, fireRateCostLevel1, fireRateCostLevel2, fireRateCostLevel3, fireRateCostLevel4);
        SetUpgradeLabel(maxAmmoUpgradeButtonText, "Max Ammo", maxAmmoLevel, maxAmmoCostLevel1, maxAmmoCostLevel2, maxAmmoCostLevel3, maxAmmoCostLevel4);
    }

    private void SetUpgradeLabel(TextMeshProUGUI label, string upgradeName, int level, int cost1, int cost2, int cost3, int cost4)
    {
        if (label == null) return;

        if (level >= 4)
        {
            label.text = $"{upgradeName}\n4/4  |  MAXED";
            return;
        }

        int nextCost = GetLevelValue(level, cost1, cost2, cost3, cost4);
        label.text = $"{upgradeName}\n{level}/4  |  {nextCost} SCRAP";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}


