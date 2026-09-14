using UnityEngine;
using TMPro;

public class PlayerParts : MonoBehaviour
{
    public int currentParts;
    public TextMeshProUGUI partsText;

    private void Awake()
    {
        UpdateText();
    }

    public void AddParts(int amount)
    {
        currentParts += amount;
        UpdateText();
    }

    private void UpdateText()
    {
        partsText.text = currentParts.ToString();
    }
}