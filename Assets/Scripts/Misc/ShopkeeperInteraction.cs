using UnityEngine;

public class ShopkeeperInteraction : MonoBehaviour
{
    [Header("Detection")]
    public Transform player;
    public float interactionRange = 3.5f;
    public KeyCode interactKey = KeyCode.E;

    [Header("UI")]
    public GameObject interactionPrompt;
    public GameObject shopPanel;       

    [Header("Player References (disabled while shop is open)")]
    public Gun playerGun;

    private bool playerInRange;
    private bool shopOpen;

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
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
    }

    private void CloseShop()
    {
        shopOpen = false;

        if (shopPanel != null) shopPanel.SetActive(false);

        if (playerGun != null) playerGun.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Time.timeScale = 1f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}