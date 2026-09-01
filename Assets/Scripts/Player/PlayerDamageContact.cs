using UnityEngine;

public class PlayerDamageContact : MonoBehaviour
{
    public float damageCooldown = 1f;

    private PlayerHealth playerHealth;
    private float lastDamageTime = -999f;

    private void Awake()
    {
        playerHealth = GetComponentInParent<PlayerHealth>();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        DamageOnTouch damageSource = hit.gameObject.GetComponent<DamageOnTouch>();

        if (damageSource != null && Time.time >= lastDamageTime + damageCooldown)
        {
            playerHealth.TakeDamage(damageSource.damageAmount);
            lastDamageTime = Time.time;
        }
    }
}
