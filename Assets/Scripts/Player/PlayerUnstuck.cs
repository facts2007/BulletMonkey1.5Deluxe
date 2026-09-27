using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class PlayerUnstuck : MonoBehaviour
{
    public Transform[] islandCenters;
    public TMP_Text prompt;
    public int healthCost=10;
    public float blockedSeconds=1.5f;
    public float flightSeconds=3;
    public Material haloMaterial;
    public bool IsRecovering {get;private set;}
    public bool IsStuck => blockedFor>=blockedSeconds || Time.time-lastBlocked<4;
    private float blockedFor,lastBlocked=-100,confirmUntil,messageUntil;
    private Vector3 lastPosition;
    private PlayerMovement movement;
    private PlayerHealth health;
    private void Awake(){movement=GetComponent<PlayerMovement>();health=GetComponent<PlayerHealth>();lastPosition=transform.position;}
    private void Update()
    {
        Vector3 delta=transform.position-lastPosition;lastPosition=transform.position;delta.y=0;
        if(IsRecovering || Time.timeScale<=0 || !movement.enabled){blockedFor=0;return;}
        bool trying=Mathf.Abs(Input.GetAxisRaw("Horizontal"))+Mathf.Abs(Input.GetAxisRaw("Vertical"))>.1f;
        blockedFor=trying && delta.magnitude<Time.deltaTime*.15f ? blockedFor+Time.deltaTime:0;
        if(blockedFor>=blockedSeconds)lastBlocked=Time.time;
        if(Input.GetKeyDown(KeyCode.T))Request();
        if(prompt!=null && Time.time>messageUntil){prompt.text="[T] unstuck?";prompt.gameObject.SetActive(true);}
    }
    public void Request()
    {
        if(IsRecovering || !movement.enabled || Time.timeScale<=0)return;

        if(health.currentHealth<=healthCost){Show("Unstuck needs more than "+healthCost+" HP.");return;}
        if(Time.time<confirmUntil){confirmUntil=0;StartCoroutine(Recover());return;}
        confirmUntil=Time.time+4;Show("Unstuck?  -10 HP  [T] confirm");
    }
    private void Show(string text){if(prompt!=null){prompt.text=text;prompt.gameObject.SetActive(true);}messageUntil=Time.time+4;}
    public bool FindDestination(out Vector3 destination)
    {
        destination=transform.position;float nearest=float.PositiveInfinity;bool found=false;
        if(islandCenters==null)return false;
        foreach(var center in islandCenters)
        {
            if(center==null)continue;
            var area=center.GetComponentInParent<WaveArea>();if(area!=null && !area.IsUnlocked && !area.waveComplete)continue;
            NavMeshHit hit;if(!NavMesh.SamplePosition(center.position,out hit,8,NavMesh.AllAreas))continue;
            float d=(hit.position-transform.position).sqrMagnitude;if(d>=nearest)continue;
            nearest=d;destination=hit.position;found=true;
        }
        return found;
    }
    private IEnumerator Recover()
    {
        Vector3 destination;if(!FindDestination(out destination)){Show("No safe island landing found.");yield break;}
        health.TakeDamage(healthCost);IsRecovering=true;Show("HELICOPTER RESCUE  -10 HP");
        var disabled=new List<Behaviour>();
        foreach(var b in GetComponentsInChildren<MonoBehaviour>())
            if(b.enabled && (b is PlayerMovement || b is MouseLook || b is Gun || b is CharacterAnimationDriver || b is ShootCameraShake)){disabled.Add(b);b.enabled=false;}
        var cc=GetComponent<CharacterController>();cc.enabled=false;
        var visual=transform.Find("BulletMonkeyVisual");
        var copy=Instantiate(visual.gameObject,visual.position,visual.rotation);copy.transform.localScale=visual.lossyScale;
        foreach(var a in copy.GetComponentsInChildren<Animator>())a.enabled=false;
        var rag=copy.AddComponent<HeavenlyRagdoll>();rag.haloMaterial=haloMaterial;rag.Flop();visual.gameObject.SetActive(false);
        yield return new WaitForSeconds(.8f);
        Vector3 start=transform.position;rag.PrepareHelicopter();
        for(float tilt=0;tilt<.4f;tilt+=Time.deltaTime){rag.HelicopterPose(Vector3.zero,0,65*Mathf.SmoothStep(0,1,tilt/.4f));yield return null;}
        for(float t=0;t<flightSeconds;t+=Time.deltaTime)
        {
            float f=Mathf.Clamp01(t/flightSeconds);Vector3 p=Vector3.Lerp(start,destination,Mathf.SmoothStep(0,1,f))+Vector3.up*Mathf.Sin(f*Mathf.PI)*6;
            rag.HelicopterPose(p-start,1080*f,65);transform.position=p;yield return null;
        }
        transform.position=destination+Vector3.up*.15f;rag.HidePuppet();Destroy(copy);visual.gameObject.SetActive(true);
        cc.enabled=true;movement.ResetAfterRecovery();foreach(var b in disabled)if(b!=null)b.enabled=true;
        lastPosition=transform.position;blockedFor=0;lastBlocked=-100;IsRecovering=false;
        if(prompt!=null)prompt.gameObject.SetActive(false);
    }
}

