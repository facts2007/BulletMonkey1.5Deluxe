using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathSceneLoader : MonoBehaviour
{
    [Header("Death Scene")]
    public string deathSceneName = "GameOver";

    private PlayerHealth playerHealth;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (playerHealth.currentHealth <= 0)
        {
            SceneManager.LoadScene(deathSceneName);
        }
    }
}