using UnityEngine;

public class BossTestShortcut : MonoBehaviour
{
    public BossFusionEncounter encounter;
    private bool used;
    private void OnTriggerEnter(Collider other){Touch(other);}
    private void OnTriggerStay(Collider other){Touch(other);}
    private void Touch(Collider other)
    {
        if(used || encounter==null)return;
        var player=other.GetComponentInParent<PlayerHealth>();
        if(player!=null)used=encounter.StartFromTestCube(player);
    }
}
