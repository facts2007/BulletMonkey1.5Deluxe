using UnityEngine;
using TMPro;

public class PlayerPoints : MonoBehaviour
{
    public static PlayerPoints Instance { get; private set; }

    [Header("Points")]
    public int currentPoints;

    [Header("UI")]
    public TextMeshProUGUI pointsText;

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

    public void AddPoints(int amount)
    {
        currentPoints += amount;
        UpdateText();
    }

    public bool SpendPoints(int amount)
    {
        if (currentPoints < amount)
        {
            return false;
        }

        currentPoints -= amount;
        UpdateText();
        return true;
    }

    private void UpdateText()
    {
        if (pointsText != null)
        {
            pointsText.text = currentPoints.ToString();
        }
    }
}