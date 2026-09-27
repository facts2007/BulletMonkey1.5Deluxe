using UnityEngine;

public class WaterBucketPickup : MonoBehaviour
{
    private bool collected;
    private void OnTriggerEnter(Collider other)
    {
        var player=other.GetComponentInParent<PlayerBucketInventory>();
        if(collected || player==null)return;
        collected=true;player.AddBucket();SpawnFog.Poof(transform.position,.6f);Destroy(gameObject);
    }
}
