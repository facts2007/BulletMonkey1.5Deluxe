using System.Collections;
using UnityEngine;
using TMPro;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("UI References")]
    public RectTransform healthBar;
    public RectTransform redBar;
    public TextMeshProUGUI healthText;

    [Header("Bar Settings")]
    public float healthBarSpeed = 8f;
    public float redBarSpeed = 4f;
    public float redBarDelay = 0.5f;

    private float targetHealthScale = 1f;
    private float targetRedScale = 1f;
    private Coroutine redBarRoutine;

    private void Awake()
    {
        currentHealth = maxHealth;

        targetHealthScale = 1f;
        targetRedScale = 1f;

        UpdateText();
    }

    private void Update()
    {
        AnimateBar(healthBar, targetHealthScale, healthBarSpeed);
        AnimateBar(redBar, targetRedScale, redBarSpeed);
    }

    private void AnimateBar(RectTransform bar, float target, float speed)
    {
        if (bar == null) return;

        Vector3 scale = bar.localScale;
        scale.x = Mathf.Lerp(scale.x, target, speed * Time.deltaTime);
        bar.localScale = scale;
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);

        targetHealthScale = (float)currentHealth / maxHealth;

        UpdateText();

        if (redBarRoutine != null)
        {
            StopCoroutine(redBarRoutine);
        }

        redBarRoutine = StartCoroutine(DelayRedBar());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator DelayRedBar()
    {
        yield return new WaitForSeconds(redBarDelay);

        targetRedScale = targetHealthScale;
    }

    private void UpdateText()
    {
        if (healthText != null)
        {
            healthText.text = currentHealth + "/" + maxHealth;
        }
    }

    public void Die()
    {
        Enemy enemy = GetComponent<Enemy>();

        if (enemy != null)
        {
            enemy.Explode();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
