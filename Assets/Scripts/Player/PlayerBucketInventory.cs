using UnityEngine;
public class PlayerBucketInventory : MonoBehaviour
{
    [Min(0)] public int buckets;
    public void AddBucket(){buckets++;}
    public bool Consume(){if(buckets<=0)return false;buckets--;return true;}
}
