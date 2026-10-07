using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCheer : MonoBehaviour
{
    public float cheerSeconds=1.4f;
    public float panSeconds=.65f;
    public Camera cheerCamera;
    public bool IsCheering {get;private set;}
    public static bool IsCutsceneActive {get;private set;}
    public static Camera ActiveCamera {get;private set;}
    private CharacterAnimationDriver driver;
    private Camera gameplayCamera;
    private RetroCamera gameplayRetro;
    private bool cameraWasEnabled,retroWasEnabled;
    private readonly List<Behaviour> suspended=new List<Behaviour>();
    private float nextCheer;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState(){IsCutsceneActive=false;ActiveCamera=null;}
    private void Awake(){driver=GetComponent<CharacterAnimationDriver>();if(cheerCamera!=null)cheerCamera.enabled=false;}
    private void Update(){if(Input.GetKeyDown(KeyCode.Q) && Time.time>=nextCheer)Begin(false);}
    public void Begin(bool cinematic)
    {
        if(IsCheering && cinematic && !IsCutsceneActive){StopAllCoroutines();Restore();}
        if(IsCheering || Time.timeScale<=0 || GameSceneFlow.IsLoading || BossFusionEncounter.IsCutsceneActive || DungeonEncounter.IsCutsceneActive)return;
        var hp=GetComponent<PlayerHealth>();if(hp==null || hp.currentHealth<=0)return;
        var movement=GetComponent<PlayerMovement>();if(movement==null || !movement.enabled)return;
        StartCoroutine(Cheer(cinematic));
    }
    public static void CelebrateBoss(){var cheer=FindFirstObjectByType<PlayerCheer>();if(cheer!=null)cheer.Begin(true);}
    private IEnumerator Cheer(bool cinematic)
    {
        IsCheering=true;IsCutsceneActive=cinematic;nextCheer=Time.time+cheerSeconds+1;
        if(cinematic)
        {
            foreach(var b in GetComponentsInChildren<MonoBehaviour>())
                if(b!=null && b.enabled && (b is PlayerMovement || b is MouseLook || b is Gun || b is PlayerUnstuck || b is ShootCameraShake)){suspended.Add(b);b.enabled=false;}
            var pause=FindFirstObjectByType<PauseManager>();if(pause!=null && pause.enabled){suspended.Add(pause);pause.enabled=false;}
            gameplayCamera=Camera.main;
            if(gameplayCamera!=null)
            {
                if(cheerCamera==null){var go=new GameObject("Cheer Camera");cheerCamera=go.AddComponent<Camera>();}
                cheerCamera.CopyFrom(gameplayCamera);cheerCamera.targetTexture=null;cheerCamera.tag="MainCamera";
                cheerCamera.transform.SetPositionAndRotation(gameplayCamera.transform.position,gameplayCamera.transform.rotation);
                cameraWasEnabled=gameplayCamera.enabled;gameplayRetro=gameplayCamera.GetComponent<RetroCamera>();retroWasEnabled=gameplayRetro!=null&&gameplayRetro.enabled;
                if(gameplayRetro!=null)gameplayRetro.enabled=false;gameplayCamera.enabled=false;
                var retro=cheerCamera.GetComponent<RetroCamera>();
                if(gameplayRetro!=null){if(retro==null)retro=cheerCamera.gameObject.AddComponent<RetroCamera>();retro.verticalResolution=gameplayRetro.verticalResolution;retro.pixelMaterial=gameplayRetro.pixelMaterial;retro.enabled=true;}
                cheerCamera.enabled=true;ActiveCamera=cheerCamera;
                Vector3 center=transform.position+Vector3.up*1.2f;
                var meshes=GetComponentsInChildren<SkinnedMeshRenderer>();
                float size=0;foreach(var mesh in meshes)if(mesh.bounds.size.y>size){size=mesh.bounds.size.y;center=mesh.bounds.center;}
                Vector3 shot=center+transform.forward*Mathf.Max(4,size*2)+Vector3.up*.6f;
                foreach(var hit in Physics.RaycastAll(center,(shot-center).normalized,Vector3.Distance(center,shot),~0,QueryTriggerInteraction.Ignore))
                    if(hit.collider.GetComponentInParent<PlayerHealth>()==null&&Vector3.Distance(center,hit.point)<Vector3.Distance(center,shot))shot=hit.point+hit.normal*.3f;
                yield return Pan(shot,Quaternion.LookRotation(center-shot),panSeconds);
            }
        }
        if(driver!=null)driver.Cheer(cheerSeconds);
        if(GameAudio.Instance!=null)GameAudio.Instance.PlayPlayerCheer();
        yield return new WaitForSeconds(cheerSeconds);
        if(cinematic && gameplayCamera!=null && cheerCamera!=null)
            yield return Pan(gameplayCamera.transform.position,gameplayCamera.transform.rotation,panSeconds);
        Restore();
    }
    private IEnumerator Pan(Vector3 point,Quaternion rotation,float seconds)
    {
        Vector3 from=cheerCamera.transform.position;Quaternion facing=cheerCamera.transform.rotation;
        for(float t=0;t<seconds;t+=Time.deltaTime){float f=Mathf.SmoothStep(0,1,t/Mathf.Max(.01f,seconds));cheerCamera.transform.SetPositionAndRotation(Vector3.Lerp(from,point,f),Quaternion.Slerp(facing,rotation,f));yield return null;}
        cheerCamera.transform.SetPositionAndRotation(point,rotation);
    }
    private void Restore()
    {
        if(cheerCamera!=null){cheerCamera.enabled=false;var retro=cheerCamera.GetComponent<RetroCamera>();if(retro!=null)retro.enabled=false;}
        if(gameplayCamera!=null)gameplayCamera.enabled=cameraWasEnabled;
        if(gameplayRetro!=null)gameplayRetro.enabled=retroWasEnabled;
        gameplayCamera=null;gameplayRetro=null;ActiveCamera=null;
        if(driver!=null)driver.EndCheer();
        foreach(var b in suspended)if(b!=null)b.enabled=true;suspended.Clear();
        IsCheering=false;IsCutsceneActive=false;
    }
    private void OnDisable(){StopAllCoroutines();Restore();}
}
