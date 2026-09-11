using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(NavMeshAgent))]
public class RangedEnemy : MonoBehaviour
{
    [Header("Combat")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 2f;
    public int projectileDamage = 10;
    public float aimHeightOffset = 0f;

    [Header("Range")]
    public float detectionRange = 12f;
    public float attackRange = 8f;
    public float retreatRange = 4f;

    [Header("Target")]
    public Transform player;

    private float fireTimer;
    private EnemyHealth enemyHealth;
    private NavMeshAgent agent;

    private void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        
        GameObject gevondenSpeler = GameObject.Find("Speler");

        
        if (gevondenSpeler == null)
        {
            gevondenSpeler = GameObject.FindGameObjectWithTag("Player");
        }

       
        if (gevondenSpeler != null)
        {
            player = gevondenSpeler.transform;
        }
        else
        {
            Debug.LogError("Oeps! De speler kon met geen mogelijkheid worden gevonden in de scene. Controleer de naam of tag van je Player object!");
        }
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update()
    {
        if (player == null) return;
        if (enemyHealth != null && enemyHealth.currentHealth <= 0) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > detectionRange)
        {
            agent.ResetPath();
            return;
        }

        FaceTarget();

        if (distance < retreatRange)
        {
            Retreat();
            HandleFiring();
        }
        else if (distance > attackRange)
        {
            ChasePlayer();
        }
        else
        {
            agent.ResetPath();
            HandleFiring();
        }
    }

    private void HandleFiring()
    {
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate)
        {
            Shoot();
            fireTimer = 0f;
        }
    }

    private void ChasePlayer()
    {
        agent.SetDestination(player.position);
    }

    private void FaceTarget()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void Retreat()
    {
        Vector3 directionAwayFromPlayer = (transform.position - player.position).normalized;
        Vector3 retreatTarget = transform.position + directionAwayFromPlayer * retreatRange;

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(retreatTarget, out navHit, retreatRange, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;

        Vector3 targetPoint = player.position + Vector3.up * aimHeightOffset;
        Vector3 direction = (targetPoint - firePoint.position).normalized;

        GameObject projectileObject = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        Projectile projectile = projectileObject.GetComponent<Projectile>();
        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<Projectile>();
        }

        projectile.damage = projectileDamage;
        projectile.direction = direction;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, retreatRange);
    }
}