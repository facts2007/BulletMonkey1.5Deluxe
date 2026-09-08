using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class MeleeEnemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float chaseRange = 6f;

    [Header("Contact Damage")]
    public int contactDamage = 5;
    public float damageInterval = 1f;   
    private float damageTimer;
    private bool isTouchingPlayer;

    [Header("Stomp")]
    [Tooltip("How far above the enemy's center a contact must be to count as a head stomp")]
    public float stompDetectionThreshold = 0.5f;
    public float stompBounceForce = 8f;

    [Header("Target")]
    public Transform player;            

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
        if (distance <= chaseRange)
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

        if (isTouchingPlayer)
        {
            damageTimer += Time.deltaTime;
            if (damageTimer >= damageInterval)
            {
                DamagePlayer();
                damageTimer = 0f;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            CheckStomp(collision);
            isTouchingPlayer = true;
            damageTimer = damageInterval; 
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isTouchingPlayer = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isTouchingPlayer = false;
            damageTimer = 0f;
        }
    }

    void CheckStomp(Collision collision)
    {
        if (enemyHealth != null && enemyHealth.currentHealth <= 0) return; 

        foreach (ContactPoint contact in collision.contacts)
        {
            
            if (contact.point.y > transform.position.y + stompDetectionThreshold)
            {
                
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(enemyHealth.currentHealth);
                }

                Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    Vector3 v = playerRb.linearVelocity;
                    v.y = stompBounceForce;
                    playerRb.linearVelocity = v;
                }
                return;
            }
        }
    }

    void DamagePlayer()
    {
        if (player == null) return;
        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(contactDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}