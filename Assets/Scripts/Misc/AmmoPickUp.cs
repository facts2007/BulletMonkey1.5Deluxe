using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    public int amount = 10;
    private bool collected;
    private void OnTriggerStay(Collider other){OnTriggerEnter(other);}

    private void OnTriggerEnter(Collider other)
    {
        if(collected || (GetComponent<LootMotion>()!=null && GetComponent<LootMotion>().IsFlying))return;
        bool isPlayer = other.GetComponentInParent<PlayerHealth>() != null;

        if (isPlayer && Gun.Instance != null)
        {
            Gun.Instance.AddAmmo(amount);
            collected=true;Destroy(gameObject);
        }
    }
}
