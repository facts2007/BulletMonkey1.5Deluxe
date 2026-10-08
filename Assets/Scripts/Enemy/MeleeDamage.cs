using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    public float attackRange = 1.5f;
    public int damage = 10;
    public float attackInterval = 1f;

    public Transform player;

    private EnemyHealth enemyHealth;
    private MeleeEnemy movement;
    private Collider body;
    private float attackTimer;
    private float nextAttackTime;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        movement = GetComponent<MeleeEnemy>();
        body = GetComponent<Collider>();
    }

    private void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update()
    {
        if (player == null || Time.timeScale <= 0f || GameSceneFlow.IsLoading) return;
        if (enemyHealth != null && enemyHealth.currentHealth <= 0) return;

        var health = player.GetComponentInParent<PlayerHealth>();
        if (!CanAttack(health) || Time.time < nextAttackTime) { attackTimer = 0; return; }
        Vector3 offset = player.position - transform.position;
        offset.y = 0;
        float distance = offset.magnitude;
        var playerBody = health.GetComponent<CharacterController>();
        bool sameHeight = body == null || playerBody == null ||
            (playerBody.bounds.min.y < body.bounds.max.y && playerBody.bounds.max.y > body.bounds.min.y + .12f);

        if (distance <= attackRange && sameHeight)
        {
            attackTimer += Time.deltaTime;
            if (nextAttackTime > 0 || attackTimer >= attackInterval)
            {
                TryContactAttack(health);
                attackTimer = 0f;
            }
        }
        else
        {
            attackTimer = 0f;
        }
    }

    private bool CanAttack(PlayerHealth health)
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f || GameSceneFlow.IsLoading || health == null ||
            health.currentHealth <= 0 || health.HasEscaped || health.IsDying ||
            (enemyHealth != null && enemyHealth.IsDead) || (movement != null && movement.IsDodging)) return false;
        var playerBody = health.GetComponent<CharacterController>();
        // Feet on/above the imp's head are a stomp, never a melee hit.
        return body == null || playerBody == null || playerBody.bounds.min.y < body.bounds.max.y - .12f;
    }

    public bool TryContactAttack(PlayerHealth health)
    {
        if (!CanAttack(health) || Time.time < nextAttackTime) return false;
        health.TakeDamage(damage);
        nextAttackTime = Time.time + Mathf.Max(.1f, attackInterval);
        attackTimer = 0;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
