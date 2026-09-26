using UnityEngine;

[RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
public class SuperBananaPickup : MonoBehaviour
{
    public Transform visual;
    public float spinSpeed = 90f;
    public float bobHeight = 0.12f;
    public float lifetime = 90f;
    private Vector3 visualStart;
    private bool collected;

    private void Awake()
    {
        GetComponent<SphereCollider>().isTrigger = true;
        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        if (visual != null) visualStart = visual.localPosition;
        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (visual == null) return;
        visual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        visual.localPosition = visualStart + Vector3.up * (Mathf.Sin(Time.time * 3f) * bobHeight);
    }

    private void OnTriggerEnter(Collider other) { TryCollect(other); }
    private void OnTriggerStay(Collider other) { TryCollect(other); }

    private void TryCollect(Collider other)
    {
        if (collected || Time.timeScale <= 0f) return;
        SuperShootAbility ability = other.GetComponentInParent<SuperShootAbility>();
        if (ability == null || !ability.isActiveAndEnabled || !ability.TryCollectBanana()) return;
        collected = true;
        Destroy(gameObject);
    }
}
