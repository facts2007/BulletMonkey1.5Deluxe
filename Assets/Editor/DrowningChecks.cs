using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DrowningChecks
{
    private static IEnumerator routine;
    private static double until;
    private static readonly List<string> results=new List<string>();
    public static string Report=>string.Join("\n",results);
    public static void Run(){results.Clear();routine=Checks();until=0;EditorApplication.update+=Step;}
    private static void Step()
    {
        if(Time.time<until)return;
        try{if(routine.MoveNext()){until=Time.time+Convert.ToDouble(routine.Current);return;}}
        catch(Exception ex){results.Add("FAIL "+ex);}
        EditorApplication.update-=Step;System.IO.Directory.CreateDirectory("Temp/CombatChecks");System.IO.File.WriteAllText("Temp/CombatChecks/drowning-report.txt",Report);
    }
    private static void Check(bool value,string label){results.Add((value?"PASS ":"FAIL ")+label);if(!value)throw new Exception(label);}
    private static IEnumerator Checks()
    {
        Application.runInBackground=true;
        var sequence=UnityEngine.Object.FindFirstObjectByType<DrowningSequence>();
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
        var volume=UnityEngine.Object.FindFirstObjectByType<WaterDrowningTrigger>().GetComponent<BoxCollider>();
        Check(!sequence.IsRunning,"Sequence does not start on dry land");
        Check(volume.bounds.size.x>1900 && volume.bounds.size.z>1900,"Water contact volume covers entire visible ocean");
        sequence.minimumMessageSeconds=1;sequence.ragdollDelay=.2f;sequence.whiteFadeSeconds=7;sequence.ragdollRestSeconds=2;
        sequence.heavenlyVoice=AudioClip.Create("Temporary five-second voice test",22050*5,1,22050,false);
        sequence.heavenlyChoir=AudioClip.Create("Temporary choir check",22050,1,22050,false);
        player.GetComponent<PlayerMovement>().enabled=false;
        var cc=player.GetComponent<CharacterController>();cc.enabled=false;
        player.transform.position=new Vector3(volume.bounds.max.x-20,volume.bounds.max.y-.3f,volume.bounds.min.z+20);
        cc.enabled=true;Physics.SyncTransforms();
        yield return .3;
        Check(sequence.IsRunning,"Water at far corner triggers drowning");
        Check(Vector3.Distance(player.transform.position,sequence.arrival.position)<.1f,"Player teleports to heavenly box");
        Check(!cc.enabled && !player.GetComponent<MouseLook>().enabled && !player.GetComponentInChildren<Gun>(true).enabled,"Movement, collision, aiming and shooting disabled");
        Check(sequence.heavenlyCamera.gameObject.activeInHierarchy && sequence.caption.gameObject.activeInHierarchy,"Heavenly camera and requested caption visible");
        sequence.Begin(player);
        Check(UnityEngine.Object.FindObjectsByType<HeavenlyRagdoll>(FindObjectsSortMode.None).Length==1,"Repeated water contacts create only one sequence");
        yield return .4;
        Check(sequence.Ragdoll!=null && sequence.Ragdoll.IsRagdoll,"Monkey becomes a physics ragdoll");
        var choir=sequence.GetComponents<AudioSource>().First(a=>a.clip==sequence.heavenlyChoir);
        Check(choir.isPlaying && choir.loop,"Separate choir source plays and loops beneath voice");
        Check(Mathf.Abs(choir.volume-sequence.choirVolume*GameAudio.Instance.MusicVolume)<.001f,"Choir respects music volume");
        Check(sequence.Ragdoll.Halo!=null && sequence.Ragdoll.Halo.GetComponent<LineRenderer>().sharedMaterial!=null,"Golden halo created on ragdoll");
        Check(GameObject.Find("Drowning physics puppet").GetComponentsInChildren<Rigidbody>().Length==15,"All 15 ragdoll bodies created");
        yield return 1.0;
        Check(!sequence.Ragdoll.IsAscending && sequence.whiteFade.color.a==0,"Ragdoll rests for two seconds before lift and fade");
        var bodies=GameObject.Find("Drowning physics puppet").GetComponentsInChildren<Rigidbody>();
        Check(bodies.All(b=>!float.IsNaN(b.position.x)&&Vector3.Distance(b.position,sequence.arrival.position)<10),"Ragdoll remains stable inside room");
        var head=bodies.First(b=>b.name=="head.x");
        Check(Vector3.Distance(sequence.Ragdoll.Halo.position,head.position+Vector3.up*sequence.haloHeight)<.05f,"Halo follows fallen head");
        Check(Quaternion.Angle(sequence.Ragdoll.Halo.rotation,Quaternion.identity)<.01f,"Halo remains upright");
        yield return 1.5;
        Check(sequence.Ragdoll.IsAscending && sequence.whiteFade.color.a>0 && sequence.whiteFade.color.a<1,"Torso ascent and gradual whiteout run together");
        var torso=bodies.First(b=>b.name=="spine_03.x");float liftStart=torso.position.y;
        Check(torso.isKinematic && bodies.Count(b=>b.isKinematic)==1,"Only torso is lifted; all other ragdoll bodies stay dynamic");
        yield return 2;
        Check(torso.position.y>liftStart+.4f,"Torso gradually rises upward");
        Check(bodies.All(b=>!float.IsNaN(b.position.x)&&Vector3.Distance(b.position,torso.position)<4),"Limbs remain attached and stable during ascent");
        yield return 6;
        Check(sequence.IsFinished && sequence.whiteFade.color.a==1,"Whiteout completes after voice and fade");
        Check(!choir.isPlaying,"Choir stops when whiteout completes");
        Check(player.currentHealth==0 && !player.gameObject.activeSelf,"Existing player death runs after whiteout");
        Check(sequence.deathPanel.activeInHierarchy && Cursor.visible,"Retry/main-menu choices appear with unlocked cursor");
        Check(Camera.allCameras.Length==1 && Camera.allCameras[0]==sequence.heavenlyCamera,"Death retains a working camera");
        results.Add("COMPLETE");
    }
}
