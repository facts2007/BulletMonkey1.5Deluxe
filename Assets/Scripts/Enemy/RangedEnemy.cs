using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class RangedEnemy : MonoBehaviour
{
    [Header("Combat")]
    public GameObject projectilePrefab;   
    public Transform firePoint;
    public float fireRate = 2f;        
    public int projectileDamage = 10;
    public float detectionRange = 8f;

    [Header("Target")]
    public Transform player;          

    private float fireTimer;
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
        if (distance <= detectionRange)
        {
            fireTimer += Time.deltaTime;
            if (fireTimer >= fireRate)
            {
                Shoot();
                fireTimer = 0f;
            }
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    public Vector3 direction;
    public float lifeTime = 5f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
        else if (!other.isTrigger && !other.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}
