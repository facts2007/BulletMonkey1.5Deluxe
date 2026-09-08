using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class HybridEnemy : MonoBehaviour
{
    [Header("Ranged Attack")]
    public GameObject projectilePrefab;   
    public Transform firePoint;
    public float fireRate = 2f;
    public int projectileDamage = 10;
    public float detectionRange = 12f;    

    [Header("Melee / Proximity Attack")]
    public float meleeRange = 2.5f;      
    public int meleeDamage = 15;
    public float meleeDamageInterval = 1f;
    private float meleeDamageTimer;

    [Header("Movement")]
    public float moveSpeed = 2.5f;

    [Header("Target")]
    public Transform player;             

    private float fireTimer;
    private Rigidbody rb;
    private EnemyHealth enemyHealth;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
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

        if (distance <= meleeRange)
        {
           
            meleeDamageTimer += Time.deltaTime;
            if (meleeDamageTimer >= meleeDamageInterval)
            {
                DamagePlayerMelee();
                meleeDamageTimer = 0f;
            }
        }
        else if (distance <= detectionRange)
        {
            
            ChasePlayer();
            meleeDamageTimer = 0f;

            fireTimer += Time.deltaTime;
            if (fireTimer >= fireRate)
            {
                Shoot();
                fireTimer = 0f;
            }
        }
        else
        {
            
            meleeDamageTimer = 0f;
        }
    }

    void ChasePlayer()
    {
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f; 
        dir.Normalize();

        Vector3 move = dir * moveSpeed * Time.deltaTime;

        if (rb != null)
        {
            rb.MovePosition(transform.position + move);
        }
        else
        {
            transform.position += move;
        }

        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;

        Vector3 targetPoint = player.position + Vector3.up * 1f;
        Vector3 direction = (targetPoint - firePoint.position).normalized;

        GameObject ball = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));

        Projectile projScript = ball.GetComponent<Projectile>();
        if (projScript == null)
        {
            projScript = ball.AddComponent<Projectile>();
        }

        projScript.damage = projectileDamage;
        projScript.direction = direction;
    }

    void DamagePlayerMelee()
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}