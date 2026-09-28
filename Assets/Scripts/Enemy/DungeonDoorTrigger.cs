using UnityEngine;
public class DungeonDoorTrigger : MonoBehaviour
{
    public DungeonEncounter encounter;
    private void OnTriggerEnter(Collider other){if(other.GetComponentInParent<PlayerHealth>()!=null)encounter.TryOpenDoor();}
    private void OnTriggerStay(Collider other){if(other.GetComponentInParent<PlayerHealth>()!=null && encounter.HasKey)encounter.TryOpenDoor();}
}
