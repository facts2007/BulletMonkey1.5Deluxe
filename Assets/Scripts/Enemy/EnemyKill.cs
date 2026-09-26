using System;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static event Action OnEnemyDied;

    public GameObject explosionEffect;
    public float popDuration = 0.15f;
    public bool canBeStomped = true;
    public bool stompInstantKills = true;

    [Header("Parts Drop")]
    public GameObject partsDropPrefab;
    public float dropChance = 0.5f;
    public int minDropAmount = 10;
    public int maxDropAmount = 15;

    [Header("Ammo Drop")]
    public GameObject ammoDropPrefab;
    public float ammoDropChance = 0.35f;
    public int minAmmoAmount = 5;
    public int maxAmmoAmount = 10;

    [Header("Rare super banana")]
    public GameObject superBananaPrefab;
    [Range(0f, 1f)] public float superBananaDropChance = 0.01f;

    [Header("Stomp presentation")]
    public bool flattenOnStomp;
    public Transform visualRoot;
    public GameObject healthBarObject;
    public float flattenSeconds = 0.12f;
    public float flattenedLifetime = 2f;
    private bool dying;

    public void Stomp(int damage)
    {
        if (dying || !canBeStomped) return;
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.TakeDamage(stompInstantKills ? health.currentHealth : damage, true);
            if (health.currentHealth > 0 && GameAudio.Instance != null)
                GameAudio.Instance.PlayEffect(GameAudio.Instance.enemyStomped);
        }
        else DieFromStomp();
    }

    public void Explode()
    {
        BeginDeath(false);
    }

    public void DieFromStomp()
    {
        BeginDeath(true);
    }

    private void BeginDeath(bool stomp)
    {
        if (dying) return;
        dying = true;
        foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;
        foreach (Animator animator in GetComponentsInChildren<Animator>()) animator.enabled = false;
        var animationDriver = GetComponent<CharacterAnimationDriver>();
        if (animationDriver != null) animationDriver.enabled = false;
        if (healthBarObject != null) healthBarObject.SetActive(false);
        OnEnemyDied?.Invoke();

        TryDropParts();
        TryDropAmmo();

        if (superBananaPrefab != null && UnityEngine.Random.value < superBananaDropChance)
            Instantiate(superBananaPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayEffect(stomp ? GameAudio.Instance.enemyStomped : GameAudio.Instance.enemyExplode);

        if (stomp && flattenOnStomp && visualRoot != null)
        {
            StartCoroutine(FlattenAndDestroy());
            return;
        }

        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        StartCoroutine(PopAndDestroy());
    }

    private IEnumerator FlattenAndDestroy()
    {
        Vector3 startScale = visualRoot.localScale;
        Vector3 endScale = Vector3.Scale(startScale, new Vector3(1.5f, 0.08f, 1.5f));
        float elapsed = 0f;
        while (elapsed < Mathf.Max(0.01f, flattenSeconds))
        {
            elapsed += Time.deltaTime;
            visualRoot.localScale = Vector3.Lerp(startScale, endScale, elapsed / Mathf.Max(0.01f, flattenSeconds));
            yield return null;
        }
        visualRoot.localScale = endScale;
        yield return new WaitForSeconds(flattenedLifetime);
        Destroy(gameObject);
    }

    private void TryDropParts()
    {
        if (partsDropPrefab == null) return;
        if (UnityEngine.Random.value > dropChance) return;

        int amount = UnityEngine.Random.Range(minDropAmount, maxDropAmount + 1);
        GameObject drop = Instantiate(partsDropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        PartsPickup pickup = drop.GetComponent<PartsPickup>();
        if (pickup != null)
        {
            pickup.amount = amount;
        }
    }

    private void TryDropAmmo()
    {
        if (ammoDropPrefab == null) return;
        if (UnityEngine.Random.value > ammoDropChance) return;

        int amount = UnityEngine.Random.Range(minAmmoAmount, maxAmmoAmount + 1);
        GameObject drop = Instantiate(ammoDropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        AmmoPickup pickup = drop.GetComponent<AmmoPickup>();
        if (pickup != null)
        {
            pickup.amount = amount;
        }
    }

    private IEnumerator PopAndDestroy()
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 1.5f;
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, elapsed / popDuration);
            yield return null;
        }

        Destroy(gameObject);
    }
}
