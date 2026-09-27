using UnityEngine;

/// <summary>Enemy drops follow a randomized arc, bounce once, then hover and spin.</summary>
public class LootMotion : MonoBehaviour
{
    public Transform visual;
    public GameObject visualPrefab;
    public float spinSpeed=100;
    public float flightSeconds=.75f;
    public float jumpHeight=1.8f;
    public float scatterRadius=1.4f;
    public bool IsFlying {get;private set;}
    private Vector3 start,landing,visualStart;
    private float age,phase,baseY;
    private Quaternion visualRotation;
    private void Awake()
    {
        var body=GetComponent<Rigidbody>();if(body!=null){body.isKinematic=true;body.useGravity=false;}
        if(visualPrefab!=null){if(visual!=null)visual.gameObject.SetActive(false);visual=Instantiate(visualPrefab,transform).transform;visual.localPosition=Vector3.zero;}
        if(visual!=null){visualStart=visual.localPosition;visualRotation=visual.localRotation;}
        phase=Random.value*Mathf.PI*2;baseY=transform.position.y;
    }
    public void Launch()
    {
        IsFlying=true;age=0;start=transform.position;Vector2 scatter=Random.insideUnitCircle*scatterRadius;landing=start+new Vector3(scatter.x,0,scatter.y);
        float best=float.PositiveInfinity;
        foreach(var hit in Physics.RaycastAll(landing+Vector3.up*3,Vector3.down,40,~0,QueryTriggerInteraction.Ignore))
        {
            if(hit.collider.transform.IsChildOf(transform)||hit.collider.GetComponentInParent<PlayerHealth>()!=null||hit.collider.GetComponentInParent<EnemyHealth>()!=null||hit.collider.GetComponentInParent<LootMotion>()!=null)continue;
            if(hit.distance<best){best=hit.distance;landing=hit.point+Vector3.up*.3f;}
        }
        jumpHeight*=Random.Range(.8f,1.3f);flightSeconds*=Random.Range(.85f,1.15f);
    }
    private void Update()
    {
        age+=Time.deltaTime;
        if(IsFlying)
        {
            float f=Mathf.Clamp01(age/Mathf.Max(.1f,flightSeconds));float ease=1-Mathf.Pow(1-f,2);
            transform.position=Vector3.Lerp(start,landing,ease)+Vector3.up*(4*f*(1-f)*jumpHeight);
            if(visual!=null)visual.Rotate(new Vector3(150,260,70)*Time.deltaTime,Space.Self);
            if(f>=1){IsFlying=false;baseY=landing.y;age=0;if(visual!=null)visual.localRotation=visualRotation;}
        }
        else
        {
            float bounce=age<.4f?Mathf.Sin(age/.4f*Mathf.PI)*.3f:0;
            var pos=transform.position;pos.y=baseY+bounce;transform.position=pos;
            if(visual!=null){visual.Rotate(Vector3.up,spinSpeed*Time.deltaTime,Space.World);visual.localPosition=visualStart+Vector3.up*Mathf.Sin(Time.time*3+phase)*.08f;}
        }
    }
    public static GameObject Drop(GameObject prefab,Vector3 position,Quaternion rotation)
    {
        var drop=Instantiate(prefab,position,rotation);var motion=drop.GetComponent<LootMotion>();if(motion==null)motion=drop.AddComponent<LootMotion>();motion.Launch();return drop;
    }
}
