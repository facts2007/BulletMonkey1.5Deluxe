using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class StationaryHeavyEnemy : MonoBehaviour
{
    [Header("Melee / Proximity Attack")]
    public float attackRange = 2.5f;   
    public int meleeDamage = 20;
    public float damageInterval = 1f;
    private float damageTimer;

    [Header("Target")]
    public Transform player;           

    private EnemyHealth enemyHealth;

    void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (player == null) return;
        if (enemyHealth != null && enemyHealth.currentHealth <= 0) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            damageTimer += Time.deltaTime;
            if (damageTimer >= damageInterval)
            {
                DamagePlayer();
                damageTimer = 0f;
            }
        }
        else
        {
            damageTimer = 0f;
        }
    }

    void DamagePlayer()
    {
        if (player == null) return;
        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(meleeDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}