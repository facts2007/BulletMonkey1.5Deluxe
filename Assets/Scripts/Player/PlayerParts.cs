using UnityEngine;
using TMPro;

public class PlayerParts : MonoBehaviour
{
    public static PlayerParts Instance { get; private set; }

    public int currentParts;
    public TextMeshProUGUI partsText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        UpdateText();
    }

    public void AddParts(int amount)
    {
        currentParts += amount;
        UpdateText();
    }

    public bool SpendParts(int amount)
    {
        if (currentParts < amount)
        {
            return false;
        }

        currentParts -= amount;
        UpdateText();
        return true;
    }

    private void UpdateText()
    {
        partsText.text = currentParts.ToString();
    }
}