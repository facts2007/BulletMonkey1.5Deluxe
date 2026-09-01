using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Death")]
    public GameObject explosionEffect;
    public float popDuration = 0.15f;

    public void Explode()
    {
        if (explosionEffect != null)
        {
            Instantiate(
                explosionEffect,
                transform.position,
                Quaternion.identity
            );
        }

        StartCoroutine(PopAndDestroy());
    }

    private IEnumerator PopAndDestroy()
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 1.5f;
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;

            transform.localScale = Vector3.Lerp(
                startScale,
                endScale,
                elapsed / popDuration
            );

            yield return null;
        }

        Destroy(gameObject);
    }
}