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

    public void Explode()
    {
        OnEnemyDied?.Invoke();

        TryDropParts();
        TryDropAmmo();

        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        StartCoroutine(PopAndDestroy());
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