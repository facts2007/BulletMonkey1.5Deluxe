using TMPro;
using UnityEngine;
[DefaultExecutionOrder(100)]
public class KabuWaveShop : MonoBehaviour
{
    public Animator animator;
    public GameObject fireVfx;
    public bool burning;
    private ShopManager shop;
    private string state;
    private void Awake(){shop=GetComponent<ShopManager>();}
    public void Ignite(){burning=true;}
    public bool TryInteract()
    {
        if(WaveArea.AnyWaveActive)return false;
        if(burning)
        {
            var buckets=shop.player!=null?shop.player.GetComponent<PlayerBucketInventory>():null;
            if(buckets!=null && buckets.Consume())burning=false;
            return false;
        }
        return true;
    }
    private void LateUpdate()
    {
        bool active=WaveArea.AnyWaveActive;
        if(active)burning=true;
        if(fireVfx!=null && fireVfx.activeSelf!=burning)fireVfx.SetActive(burning);
        string next=burning?"Fire":"Idle";
        if(animator!=null && next!=state){animator.CrossFadeInFixedTime(next,.15f);state=next;}
        if(shop==null || shop.interactionPrompt==null || !shop.interactionPrompt.activeSelf)return;
        var text=shop.interactionPrompt.GetComponentInChildren<TMP_Text>();
        var inventory=shop.player!=null?shop.player.GetComponent<PlayerBucketInventory>():null;
        if(text!=null)text.text=active?"KABU IS BUSY BURNING! Finish the wave":burning?
            "[E] Put out Kabu · 1 bucket ("+(inventory!=null?inventory.buckets:0)+")":"[E] Open Kabu's shop";
    }
}
