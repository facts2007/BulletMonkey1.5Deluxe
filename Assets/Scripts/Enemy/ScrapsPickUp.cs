using UnityEngine;

public class PartsPickup : MonoBehaviour
{
    public int amount = 10;

    private void OnTriggerEnter(Collider other)
    {
        PlayerParts playerParts = other.GetComponentInParent<PlayerParts>();
        if (playerParts != null)
        {
            playerParts.AddParts(amount);
            Destroy(gameObject);
        }
    }
}
