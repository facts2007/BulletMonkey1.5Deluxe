using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    public Vector3 direction;
    public float lifeTime = 5f;
    public bool isNoscope;
    private bool hit;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(hit)return;
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            hit=true;
            int previous=playerHealth.currentHealth;
            playerHealth.TakeDamage(damage);
            if(isNoscope && playerHealth!=null && playerHealth.currentHealth<previous && GameAudio.Instance!=null)
                GameAudio.Instance.PlayEffect(GameAudio.Instance.enemyNoscopeHit);
            Destroy(gameObject);
            return;
        }

        if (other.GetComponent<EnemyHealth>() != null || other.isTrigger)
        {
            return;
        }

        Destroy(gameObject);
    }
}
