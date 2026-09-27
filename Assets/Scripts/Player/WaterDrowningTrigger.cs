using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class WaterDrowningTrigger : MonoBehaviour
{
    public DrowningSequence sequence;
    private PlayerHealth player;
    private Collider playerCollider;
    private BoxCollider volume;
    private void Start(){player=FindFirstObjectByType<PlayerHealth>();if(player!=null)playerCollider=player.GetComponent<Collider>();volume=GetComponent<BoxCollider>();}
    private void FixedUpdate()
    {
        // CharacterController contacts can be missed while resting on the thin water mesh.
        if(sequence!=null && !sequence.IsRunning && playerCollider!=null && playerCollider.enabled && volume.bounds.Intersects(playerCollider.bounds))sequence.Begin(player);
    }
    private void OnTriggerEnter(Collider other){Touch(other);}
    private void OnTriggerStay(Collider other){Touch(other);}
    private void Touch(Collider other)
    {
        var player=other.GetComponentInParent<PlayerHealth>();
        if(player!=null && sequence!=null)sequence.Begin(player);
    }
}

