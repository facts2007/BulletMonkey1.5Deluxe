using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class MeleeEnemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float chaseRange = 6f;
    public float stopDistance = 1.2f;

    [Header("Target")]
    public Transform player;

    private EnemyHealth enemyHealth;

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
        if (player == null) return;
        if (enemyHealth != null && enemyHealth.currentHealth <= 0) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= chaseRange && distance > stopDistance)
        {
            Chase();
        }
    }

    private void Chase()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.position += direction * moveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}