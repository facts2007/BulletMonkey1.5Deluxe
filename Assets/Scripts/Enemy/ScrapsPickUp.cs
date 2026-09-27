using UnityEngine;

public class PartsPickup : MonoBehaviour
{
    public int amount = 10;
    private bool collected;
    private void OnTriggerStay(Collider other){OnTriggerEnter(other);}

    private void OnTriggerEnter(Collider other)
    {
        if(collected || (GetComponent<LootMotion>()!=null && GetComponent<LootMotion>().IsFlying))return;
        PlayerParts playerParts = other.GetComponentInParent<PlayerParts>();
        if (playerParts != null)
        {
            playerParts.AddParts(amount);
            collected=true;Destroy(gameObject);
        }
    }
}

