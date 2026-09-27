using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathSceneLoader : MonoBehaviour
{
    [Header("Death Scene")]
    public string deathSceneName = "DeathScene";

    private PlayerHealth playerHealth;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.currentHealth <= 0 && !GameSceneFlow.IsLoading)
        {
            GameSceneFlow.Load(deathSceneName);
        }
    }
}
