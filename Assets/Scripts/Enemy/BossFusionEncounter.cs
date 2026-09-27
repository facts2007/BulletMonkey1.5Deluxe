using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BossFusionEncounter : MonoBehaviour
{
    [Header("Entry unlock and placed markers")]
    public WaveArea requiredIsland;
    public Transform[] impSpawns;
    public Transform mergePoint;
    public GameObject finalMistDoor;
    [Header("Fusion")]
    public GameObject impVisualPrefab;
    public GameObject bossPrefab;
    [Range(1,200)] public int impCount=120;
    public Material fusionFogMaterial;
    private FusionCloud fusionCloud;
    public int bossHealth=750;
    public float impSpeed=12;
    public float spawnSpacing=.08f;
    [Header("Cutscene — edit this camera's pose to frame the fusion")]
    public Camera fusionCamera;
    public float panSeconds=1.5f;
    public float revealSeconds=2;
    [Header("Cutscene audio")]
    public AudioClip mergingSound;
    [Range(0,1)] public float mergingGain=.7f;
    public AudioClip fusionRoar;
    public AudioClip bossTheme;
    [Range(0,1)] public float roarGain=1;
    [Range(0,1)] public float themeGain=1;
    public static bool IsEncounterActive {get;private set;}
    public static bool IsCutsceneActive {get;private set;}
    public bool Started {get;private set;}
    public bool Defeated {get;private set;}
    public EnemyHealth Boss {get;private set;}
    private readonly List<FusionImpRunner> runners=new List<FusionImpRunner>();
    private readonly List<Behaviour> disabled=new List<Behaviour>();
    private Camera playerCamera;
    private RetroCamera playerRetro;
    private bool cameraWasEnabled,retroWasEnabled,hudWasActive;
    private GameObject hud;
    private AudioSource roarSource,themeSource,mergeSource;
    private GameAudio audioManager;
    private Transform player;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState(){IsEncounterActive=IsCutsceneActive=false;}
    private void Awake()
    {
        if(fusionCamera!=null){fusionCamera.enabled=false;var retro=fusionCamera.GetComponent<RetroCamera>();if(retro!=null)retro.enabled=false;}
        mergeSource=gameObject.AddComponent<AudioSource>();mergeSource.playOnAwake=false;mergeSource.loop=true;
        roarSource=gameObject.AddComponent<AudioSource>();roarSource.playOnAwake=false;
        themeSource=gameObject.AddComponent<AudioSource>();themeSource.playOnAwake=false;themeSource.loop=true;
    }
    private void OnTriggerEnter(Collider other){TryEnter(other);}
    private void OnTriggerStay(Collider other){TryEnter(other);}
    public void TryEnter(Collider other)
    {
        if(Started || requiredIsland==null || !requiredIsland.waveComplete || other==null)return;
        var health=other.GetComponentInParent<PlayerHealth>();if(health==null)return;
        if(mergePoint==null || fusionCamera==null || impVisualPrefab==null || impSpawns==null || impSpawns.Length==0)return;
        if(health.GetComponent<PlayerUnstuck>()?.IsRecovering==true)return;
        Started=true;player=health.transform;StartCoroutine(Fuse());
    }
    private IEnumerator Fuse()
    {
        IsEncounterActive=IsCutsceneActive=true;
        audioManager=GameAudio.Instance;if(audioManager!=null){audioManager.SetShopOpen(false);audioManager.SetCinematicMusicDucked(true);}
        foreach(var b in player.GetComponentsInChildren<MonoBehaviour>())
            if(b.enabled && (b is PlayerMovement || b is MouseLook || b is Gun || b is PlayerUnstuck || b is ShootCameraShake)){disabled.Add(b);b.enabled=false;}
        foreach(var pause in FindObjectsByType<PauseManager>(FindObjectsSortMode.None))if(pause.enabled){disabled.Add(pause);pause.enabled=false;}
        hud=GameObject.Find("PlayerUI");if(hud!=null){hudWasActive=hud.activeSelf;hud.SetActive(false);}
        playerCamera=Camera.main;
        Vector3 shotPosition=fusionCamera.transform.position;Quaternion shotRotation=fusionCamera.transform.rotation;
        if(playerCamera!=null)
        {
            cameraWasEnabled=playerCamera.enabled;playerRetro=playerCamera.GetComponent<RetroCamera>();retroWasEnabled=playerRetro!=null&&playerRetro.enabled;
            if(playerRetro!=null)playerRetro.enabled=false;playerCamera.enabled=false;
            fusionCamera.transform.SetPositionAndRotation(playerCamera.transform.position,playerCamera.transform.rotation);
        }
        fusionCamera.enabled=true;var cutRetro=fusionCamera.GetComponent<RetroCamera>();if(cutRetro!=null)cutRetro.enabled=true;
        yield return Pan(shotPosition,shotRotation);
        mergeSource.clip=mergingSound;if(mergingSound!=null)mergeSource.Play();
        Vector3 target=Sample(mergePoint.position);
        fusionCloud=new GameObject("Growing fusion fog").AddComponent<FusionCloud>();fusionCloud.transform.position=target+Vector3.up*5;fusionCloud.Initialize(fusionFogMaterial);
        for(int i=0;i<impCount;i++)
        {
            var marker=impSpawns[i%impSpawns.Length];if(marker==null)continue;
            Vector2 jitter=Random.insideUnitCircle*1.5f;
            Vector3 position=Sample(marker.position+new Vector3(jitter.x,0,jitter.y));
            var go=Instantiate(impVisualPrefab,position,marker.rotation);go.name="Fusion imp "+(i+1);go.SetActive(true);
            var runner=go.AddComponent<FusionImpRunner>();runner.Begin(target,impSpeed);runners.Add(runner);SpawnFog.Poof(position);
            if(spawnSpacing>0)yield return new WaitForSeconds(spawnSpacing);
        }
        bool waiting=true;
        while(waiting){waiting=false;foreach(var runner in runners)if(runner!=null&&!runner.Arrived){waiting=true;break;}yield return null;}
        foreach(var runner in runners)if(runner!=null)Destroy(runner.gameObject);runners.Clear();
        if(fusionCloud!=null){fusionCloud.SetProgress(1);fusionCloud.Finish();fusionCloud=null;}
        mergeSource.Stop();
        SpawnFog.Poof(target,12);
        var body=Instantiate(bossPrefab,target,Quaternion.identity);body.SetActive(true);Boss=body.GetComponent<EnemyHealth>();if(Boss==null)Boss=body.AddComponent<EnemyHealth>();Boss.SetFullHealth(bossHealth);
        // The placeholder is invulnerable during its introduction; normal hits resume with control.
        foreach(var collider in body.GetComponentsInChildren<Collider>())collider.enabled=false;
        roarSource.clip=fusionRoar;roarSource.volume=roarGain*(audioManager!=null?audioManager.SfxVolume:.8f);if(fusionRoar!=null)roarSource.Play();
        themeSource.clip=bossTheme;if(bossTheme!=null)themeSource.Play();
        yield return new WaitForSeconds(Mathf.Max(revealSeconds,fusionRoar!=null?Mathf.Min(fusionRoar.length,8):0));
        if(playerCamera!=null)yield return Pan(playerCamera.transform.position,playerCamera.transform.rotation);
        RestoreControl();
        foreach(var collider in body.GetComponentsInChildren<Collider>())collider.enabled=true;
        IsCutsceneActive=false;
    }
    private Vector3 Sample(Vector3 point){NavMeshHit hit;return NavMesh.SamplePosition(point,out hit,8,NavMesh.AllAreas)?hit.position:point;}
    private IEnumerator Pan(Vector3 position,Quaternion rotation)
    {
        Vector3 from=fusionCamera.transform.position;Quaternion facing=fusionCamera.transform.rotation;
        for(float t=0;t<panSeconds;t+=Time.deltaTime){float f=Mathf.SmoothStep(0,1,t/Mathf.Max(.01f,panSeconds));fusionCamera.transform.SetPositionAndRotation(Vector3.Lerp(from,position,f),Quaternion.Slerp(facing,rotation,f));yield return null;}
        fusionCamera.transform.SetPositionAndRotation(position,rotation);
    }
    private void Update()
    {
        if(fusionCloud!=null){int arrived=0;foreach(var runner in runners)if(runner==null||runner.Arrived)arrived++;fusionCloud.SetProgress((float)arrived/Mathf.Max(1,impCount));}
        if(mergeSource!=null)mergeSource.volume=mergingGain*(audioManager!=null?audioManager.SfxVolume:.8f);
        if(themeSource!=null)themeSource.volume=themeGain*(audioManager!=null?audioManager.MusicVolume:.7f)*(Time.timeScale>0?1:0);
        if(!Started || Defeated || IsCutsceneActive || Boss==null)return;
        if(Boss.IsDead || !Boss.gameObject.activeInHierarchy)
        {
            Defeated=true;IsEncounterActive=false;if(finalMistDoor!=null)finalMistDoor.SetActive(false);
            themeSource.Stop();if(audioManager!=null)audioManager.SetCinematicMusicDucked(false);
        }
    }
    private void RestoreControl()
    {
        if(fusionCamera!=null){fusionCamera.enabled=false;var retro=fusionCamera.GetComponent<RetroCamera>();if(retro!=null)retro.enabled=false;}
        if(playerCamera!=null)playerCamera.enabled=cameraWasEnabled;if(playerRetro!=null)playerRetro.enabled=retroWasEnabled;
        foreach(var b in disabled)if(b!=null)b.enabled=true;disabled.Clear();
        if(hud!=null)hud.SetActive(hudWasActive);
    }
    private void OnDisable()
    {
        if(!Started)return;
        if(fusionCloud!=null)Destroy(fusionCloud.gameObject);
        StopAllCoroutines();RestoreControl();IsCutsceneActive=IsEncounterActive=false;
        foreach(var runner in runners)if(runner!=null)Destroy(runner.gameObject);runners.Clear();
        if(mergeSource!=null)mergeSource.Stop();
        if(themeSource!=null)themeSource.Stop();if(roarSource!=null)roarSource.Stop();
        if(audioManager!=null)audioManager.SetCinematicMusicDucked(false);
    }
}


