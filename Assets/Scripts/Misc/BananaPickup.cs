using UnityEngine;

public class BananaPickup : MonoBehaviour
{
    public int healAmount=20;
    private bool collected;
    private void OnTriggerEnter(Collider other){Collect(other);}
    private void OnTriggerStay(Collider other){Collect(other);}
    private void Collect(Collider other)
    {
        if(collected || Time.timeScale<=0)return;
        var motion=GetComponent<LootMotion>();if(motion!=null && motion.IsFlying)return;
        var health=other.GetComponentInParent<PlayerHealth>();
        if(health==null || health.currentHealth<=0 || health.currentHealth>=health.maxHealth)return;
        health.Heal(healAmount);collected=true;Destroy(gameObject);
    }
}
