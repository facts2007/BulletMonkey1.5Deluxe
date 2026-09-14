using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    public int amount = 10;

    private void OnTriggerEnter(Collider other)
    {
        bool isPlayer = other.GetComponentInParent<PlayerHealth>() != null;

        if (isPlayer && Gun.Instance != null)
        {
            Gun.Instance.AddAmmo(amount);
            Destroy(gameObject);
        }
    }
}