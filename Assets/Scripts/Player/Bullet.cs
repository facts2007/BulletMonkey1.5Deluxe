using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 80f;
    public float lifetime = 2f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }
}