using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Physics puppet drives the imported rig's deform and helper bones without modifying the model asset.</summary>
public class HeavenlyRagdoll : MonoBehaviour
{
    public Material haloMaterial;
    public float haloHeight = .6f;
    public float haloRadius = .36f;
    public bool showHalo = true;
    public Transform Halo {get;private set;}
    public bool IsRagdoll {get;private set;}
    public bool IsAscending {get;private set;}
    private Rigidbody liftedTorso;
    private Vector3 ascentStart;
    private float ascentElapsed, ascentDuration, ascentHeight;
    public Vector3 TorsoPosition => bodies.TryGetValue("spine_03.x",out var torso) ? torso.position : transform.position+Vector3.up;
    public void Explode(float force=9f)
    {
        Vector3 center=TorsoPosition-Vector3.up*.4f;
        foreach(var body in bodies.Values)
        {
            body.AddExplosionForce(force,center,5f,1f,ForceMode.VelocityChange);
            body.AddTorque(Random.insideUnitSphere*force,ForceMode.VelocityChange);
        }
    }
    public void BeginAscent(float duration, float height)
    {
        if(!IsRagdoll || IsAscending || !bodies.TryGetValue("spine_03.x",out liftedTorso))return;
        IsAscending=true;ascentStart=liftedTorso.position;ascentElapsed=0;
        ascentDuration=Mathf.Max(.1f,duration);ascentHeight=Mathf.Max(0,height);
        // Pull only the chest; the connected limbs remain simulated and dangle beneath it.
        liftedTorso.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        liftedTorso.isKinematic=true;
    }
    private void FixedUpdate()
    {
        if(!IsAscending)return;
        ascentElapsed+=Time.fixedDeltaTime;
        float fraction=Mathf.Clamp01(ascentElapsed/ascentDuration);
        liftedTorso.MovePosition(ascentStart+Vector3.up*ascentHeight*Mathf.SmoothStep(0,1,fraction));
    }
    private class Follow {public Transform bone,body;public Vector3 position;public Quaternion rotation;}
    private readonly List<Follow> followers=new List<Follow>();
    private GameObject puppet;
    private readonly Dictionary<string,Rigidbody> bodies=new Dictionary<string,Rigidbody>();
    private readonly Dictionary<Rigidbody,Pose> helicopterPose=new Dictionary<Rigidbody,Pose>();
    private Vector3 helicopterPivot;
    public void PrepareHelicopter()
    {
        helicopterPivot=bodies["root.x"].position;
        foreach(var body in bodies.Values){helicopterPose[body]=new Pose(body.position,body.rotation);body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.isKinematic=true;}
    }
    public void HelicopterPose(Vector3 offset,float yaw,float tilt=0)
    {
        Quaternion spin=Quaternion.Euler(0,yaw,0)*Quaternion.Euler(tilt,0,0);
        foreach(var entry in helicopterPose){entry.Key.position=helicopterPivot+offset+spin*(entry.Value.position-helicopterPivot);entry.Key.rotation=spin*entry.Value.rotation;}
    }
    public void HidePuppet(){if(puppet!=null)puppet.SetActive(false);}
    public void Flop()
    {
        if(IsRagdoll)return;IsRagdoll=true;
        var bones=GetComponentsInChildren<Transform>();
        puppet=new GameObject("Drowning physics puppet");
        string[] names={"root.x","spine_03.x","head.x","thigh.l","leg.l","foot.l","thigh.r","leg.r","foot.r","arm.l","forearm.l","hand.l","arm.r","forearm.r","hand.r"};
        string[] parents={null,"root.x","spine_03.x","root.x","thigh.l","leg.l","root.x","thigh.r","leg.r","spine_03.x","arm.l","forearm.l","spine_03.x","arm.r","forearm.r"};
        for(int i=0;i<names.Length;i++)
        {
            var bone=bones.FirstOrDefault(b=>b.name==names[i]);if(bone==null)continue;
            var go=new GameObject(names[i]);go.transform.SetParent(puppet.transform);go.transform.SetPositionAndRotation(bone.position,bone.rotation);
            var rb=go.AddComponent<Rigidbody>();rb.mass=i==0?4:i==1?3:i==2?2:1;rb.linearDamping=.3f;rb.angularDamping=.5f;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            var col=go.AddComponent<SphereCollider>();col.radius=i==0?.2f:i==1?.23f:i==2?.3f:.11f;
            bodies[names[i]]=rb;
            if(parents[i]!=null && bodies.ContainsKey(parents[i]))
            {
                var joint=go.AddComponent<CharacterJoint>();joint.connectedBody=bodies[parents[i]];
                joint.lowTwistLimit=new SoftJointLimit{limit=-35};joint.highTwistLimit=new SoftJointLimit{limit=35};
                joint.swing1Limit=new SoftJointLimit{limit=55};joint.swing2Limit=new SoftJointLimit{limit=40};joint.enableProjection=true;
            }
        }
        var colliders=puppet.GetComponentsInChildren<Collider>();
        for(int i=0;i<colliders.Length;i++)for(int j=i+1;j<colliders.Length;j++)Physics.IgnoreCollision(colliders[i],colliders[j]);
        foreach(var bone in bones)
        {
            if(bone==transform)continue;
            string n=bone.name.ToLowerInvariant();string side=n.EndsWith(".l")?".l":n.EndsWith(".r")?".r":"";
            string key="root.x";
            if(side!="")
            {
                if(n.Contains("thumb")||n.Contains("index")||n.Contains("middle")||n.Contains("ring")||n.Contains("pinky")||n.Contains("hand"))key="hand"+side;
                else if(n.Contains("forearm"))key="forearm"+side;
                else if(n.Contains("arm") && !n.Contains("arms_pole"))key="arm"+side;
                else if(n.Contains("foot")||n.Contains("toe"))key="foot"+side;
                else if(n.Contains("thigh"))key="thigh"+side;
                else if(n.Contains("leg"))key="leg"+side;
                else if(n.Contains("shoulder"))key="spine_03.x";
            }
            else if(n.Contains("head")||n.Contains("neck"))key="head.x";
            else if(n.Contains("spine"))key="spine_03.x";
            else if(n=="ak")key="hand.r";
            var body=bodies.ContainsKey(key)?bodies[key].transform:bodies["root.x"].transform;
            followers.Add(new Follow{bone=bone,body=body,position=body.InverseTransformPoint(bone.position),rotation=Quaternion.Inverse(body.rotation)*bone.rotation});
        }
        foreach(var skin in GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
        if(showHalo)
        {
        var halo=new GameObject("Monkey halo");halo.transform.SetParent(puppet.transform,false);Halo=halo.transform;
        var ring=halo.AddComponent<LineRenderer>();ring.useWorldSpace=false;ring.loop=true;
        ring.positionCount=64;ring.widthMultiplier=.045f;ring.numCornerVertices=3;ring.sharedMaterial=haloMaterial;
        ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;ring.receiveShadows=false;
        for(int i=0;i<64;i++){float angle=i*Mathf.PI*2/64;ring.SetPosition(i,new Vector3(Mathf.Cos(angle)*haloRadius,0,Mathf.Sin(angle)*haloRadius));}
        UpdateHalo();
        }
        bodies["root.x"].AddForce(Vector3.up*1.2f+transform.forward*1.5f,ForceMode.VelocityChange);
        bodies["spine_03.x"].AddTorque(transform.right*7,ForceMode.VelocityChange);
    }
    private void LateUpdate()
    {
        foreach(var f in followers)f.bone.SetPositionAndRotation(f.body.TransformPoint(f.position),f.body.rotation*f.rotation);
        UpdateHalo();
    }
    private void UpdateHalo()
    {
        // Hover above the moving head, even when the monkey lands sideways.
        if(Halo!=null && bodies.TryGetValue("head.x",out var head))
            Halo.SetPositionAndRotation(head.position+Vector3.up*haloHeight,Quaternion.identity);
    }
    private void OnDestroy(){if(puppet!=null)Destroy(puppet);}
}
