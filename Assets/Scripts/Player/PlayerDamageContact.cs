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
        bool isStompOnEnemy = hit.normal.y > 0.5f && hit.collider.GetComponentInParent<Enemy>() != null;
        if (isStompOnEnemy) return;

        // Melee contact and proximity attacks share one cooldown, rather than hitting twice.
        var melee = hit.collider.GetComponentInParent<MeleeAttack>();
        if (melee != null)
        {
            melee.TryContactAttack(playerHealth);
            return;
        }

        DamageOnTouch damageSource = hit.collider.GetComponentInParent<DamageOnTouch>();

        if (damageSource != null && Time.time >= lastDamageTime + damageCooldown)
        {
            playerHealth.TakeDamage(damageSource.damageAmount);
            lastDamageTime = Time.time;
        }
    }
}
