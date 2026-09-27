using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class IslandEntryTrigger : MonoBehaviour
{
    public WaveArea island;
    private void OnTriggerEnter(Collider other) { if (island != null) island.TryEnter(other); }
    private void OnTriggerStay(Collider other) { if (island != null) island.TryEnter(other); }
    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = new Color(1f, .75f, .1f, .7f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
