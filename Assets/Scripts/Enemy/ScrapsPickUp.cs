using System.Collections.Generic;
using UnityEngine;

public class PartsPickup : MonoBehaviour
{
    public int amount = 10;
    public bool reusableOnTouch;
    private bool collected;
    private readonly HashSet<Collider> touchingPlayer = new HashSet<Collider>();
    private void OnTriggerStay(Collider other){OnTriggerEnter(other);}

    private void OnTriggerEnter(Collider other)
    {
        if(collected || (GetComponent<LootMotion>()!=null && GetComponent<LootMotion>().IsFlying))return;
        PlayerParts playerParts = other.GetComponentInParent<PlayerParts>();
        if (playerParts == null) return;
        if (reusableOnTouch)
        {
            touchingPlayer.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
            bool firstTouch = touchingPlayer.Count == 0;
            touchingPlayer.Add(other);
            if(firstTouch) playerParts.AddParts(amount);
            return;
        }
        playerParts.AddParts(amount);
        collected=true;
        Destroy(gameObject);
    }

    private void OnTriggerExit(Collider other){touchingPlayer.Remove(other);}
    private void OnDisable(){touchingPlayer.Clear();}
}
