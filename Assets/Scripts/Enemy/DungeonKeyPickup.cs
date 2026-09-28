using UnityEngine;
public class DungeonKeyPickup : MonoBehaviour
{
    public DungeonEncounter encounter;
    private bool collected;
    private void OnTriggerEnter(Collider other){TryCollect(other);}
    private void OnTriggerStay(Collider other){TryCollect(other);}
    private void TryCollect(Collider other)
    {
        if(collected || other.GetComponentInParent<PlayerHealth>()==null)return;
        var flight=GetComponent<LootMotion>();if(flight!=null&&flight.IsFlying)return;
        collected=true;encounter.CollectKey();gameObject.SetActive(false);
    }
}
