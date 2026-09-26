using UnityEngine;

[RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
public class CoceyBananaPickup : MonoBehaviour
{
    public float spinSpeed = 70f;
    public float bobHeight = 0.1f;
    public float lifetime = 90f;
    private Vector3 startPosition;
    private bool collected;

    private void Awake()
    {
        startPosition = transform.position;
        GetComponent<SphereCollider>().isTrigger = true;
        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * 3f) * bobHeight);
    }

    private void OnTriggerEnter(Collider other) { TryCollect(other); }
    private void OnTriggerStay(Collider other) { TryCollect(other); }

    private void TryCollect(Collider other)
    {
        if (collected || Time.timeScale <= 0f) return;
        SpeedBoostAbility boost = other.GetComponentInParent<SpeedBoostAbility>();
        if (boost == null || !boost.isActiveAndEnabled) return;
        collected = true;
        boost.Activate();
        Destroy(gameObject);
    }
}
