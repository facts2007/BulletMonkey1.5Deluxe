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
    [Header("Custom fusion VFX — leave empty for existing fog")]
    public GameObject impSpawnVfx;
    [Tooltip("Optional second effect, played together with Imp Spawn Vfx.")]
    public GameObject impSpawnVfx2;
    public GameObject mergeVfxPrefab, revealVfx;
    [Header("Transformation electricity")]
    public GameObject electricitySphereVfx;
    public float electricityHeight=7;
    public float electricityFinalScale=8;
    public float vfxLifetime=6;
    private GameObject customMergeVfx;
    private GameObject electricityInstance;
    [Header("Boss island weather")]
    public GameObject rainVfx;
    public GameObject lightningVfx;
    [Tooltip("Optional custom clouds. Empty uses simple cloud puffs over the rain area.")]
    public GameObject rainCloudVfx;
    public Material rainCloudMaterial;
    public float rainCloudHeight = 12;
    [Min(20)] public float rainCloudPuffSize = 100;
    [Tooltip("The boss island mesh. Its bounds determine the full rain coverage.")]
    public Transform rainArea;
    public Vector2 rainCoverage=new Vector2(200,200);
    public float rainHeight=70;
    [Min(0)] public float islandRainEmission=700;
    [Min(1)] public int islandRainMaxParticles=7000;
    public Vector3 lightningOffset=new Vector3(25,8,12);
    private GameObject rainInstance,lightningInstance,cloudInstance;
    [Header("Boss death explosion cutscene")]
    public GameObject deathBurstVfx, deathExplosionVfx;
    public AudioClip deathExplosionSound;
    public Transform deathCameraShot;
    public float deathBuildSeconds=2;
    public float deathExplosionHold=1.2f;
    public float deathShake=.45f;
    [Range(0,1)] public float deathFlashAlpha=.45f;
    private GameObject deathBody,deathFlash;
    private bool deathSequenceRunning;
    public float deathExplosionScale=6;
    public float deathBurstScale=1.5f;
    private FusionCloud fusionCloud;
    public int bossHealth=750;
    public float impSpeed=12;
    public float spawnSpacing=.08f;
    [Header("Cutscene — edit this camera's pose to frame the fusion")]
    public Camera fusionCamera;
    public float panSeconds=1.5f;
    public float revealSeconds=2;
    [Header("Fusion camera drama")]
    [Min(0)] public float mergeShake=.1f;
    [Min(0)] public float revealShake=.22f;
    [Range(0,10)] public float mergeZoomDegrees=3;
    private bool cameraDrama;
    private Vector3 dramaPosition;
    private Quaternion dramaRotation;
    private float originalFov,mergeProgress,revealTime=-1;
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
        if(fusionCamera!=null){originalFov=fusionCamera.fieldOfView;fusionCamera.enabled=false;var retro=fusionCamera.GetComponent<RetroCamera>();if(retro!=null)retro.enabled=false;}
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
    // Developer shortcut: bypass island progression without marking any waves complete.
    public bool StartFromTestCube(PlayerHealth health)
    {
        if(Started || !isActiveAndEnabled || health==null || health.currentHealth<=0 || Time.timeScale<=0)return false;
        if(mergePoint==null || fusionCamera==null || impVisualPrefab==null || bossPrefab==null || impSpawns==null || impSpawns.Length==0)return false;
        if(health.GetComponent<PlayerUnstuck>()?.IsRecovering==true)return false;
        Vector3 direction=mergePoint.position-transform.position;direction.y=0;
        NavMeshHit landing;
        if(!NavMesh.SamplePosition(transform.position+direction.normalized*4,out landing,8,NavMesh.AllAreas))return false;
        var controller=health.GetComponent<CharacterController>();bool wasEnabled=controller!=null&&controller.enabled;
        if(wasEnabled)controller.enabled=false;
        health.transform.position=landing.position+Vector3.up*.2f;
        if(direction.sqrMagnitude>.01f)health.transform.rotation=Quaternion.LookRotation(direction);
        if(wasEnabled)controller.enabled=true;
        var movement=health.GetComponent<PlayerMovement>();if(movement!=null)movement.ResetAfterRecovery();
        Started=true;player=health.transform;StartCoroutine(Fuse());return true;
    }
    private IEnumerator Fuse()
    {
        IsEncounterActive=IsCutsceneActive=true;
        audioManager=GameAudio.Instance;if(audioManager!=null){audioManager.SetShopOpen(false);audioManager.SetCinematicMusicDucked(true);}
        foreach(var b in player.GetComponentsInChildren<MonoBehaviour>())
            if(b!=null && b.enabled && (b is PlayerMovement || b is MouseLook || b is Gun || b is PlayerUnstuck || b is ShootCameraShake)){disabled.Add(b);b.enabled=false;}
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
        dramaPosition=fusionCamera.transform.position;dramaRotation=fusionCamera.transform.rotation;cameraDrama=true;mergeProgress=0;revealTime=-1;
        mergeSource.clip=mergingSound;if(mergingSound!=null)mergeSource.Play();
        Vector3 target=Sample(mergePoint.position);
        StartWeather(target);
        fusionCloud=new GameObject("Growing fusion fog").AddComponent<FusionCloud>();fusionCloud.transform.position=target+Vector3.up*5;fusionCloud.Initialize(fusionFogMaterial);
        if(mergeVfxPrefab!=null){Destroy(fusionCloud.gameObject);fusionCloud=null;customMergeVfx=Instantiate(mergeVfxPrefab,target,Quaternion.identity);}
        if(electricitySphereVfx!=null){electricityInstance=Instantiate(electricitySphereVfx,target+Vector3.up*electricityHeight,Quaternion.identity);electricityInstance.name="Fusion electricity";electricityInstance.transform.localScale=Vector3.one*2;}
        for(int i=0;i<impCount;i++)
        {
            var marker=impSpawns[i%impSpawns.Length];if(marker==null)continue;
            Vector2 jitter=Random.insideUnitCircle*1.5f;
            Vector3 position=Sample(marker.position+new Vector3(jitter.x,0,jitter.y));
            var go=Instantiate(impVisualPrefab,position,marker.rotation);go.name="Fusion imp "+(i+1);go.SetActive(true);
            var runner=go.AddComponent<FusionImpRunner>();runner.Begin(target,impSpeed);runners.Add(runner);PlayImpSpawnVfx(position);
            if(spawnSpacing>0)yield return new WaitForSeconds(spawnSpacing);
        }
        bool waiting=true;
        while(waiting){waiting=false;foreach(var runner in runners)if(runner!=null&&!runner.Arrived){waiting=true;break;}yield return null;}
        foreach(var runner in runners)if(runner!=null)Destroy(runner.gameObject);runners.Clear();
        if(fusionCloud!=null){fusionCloud.SetProgress(1);fusionCloud.Finish();fusionCloud=null;}
        if(customMergeVfx!=null){Destroy(customMergeVfx);customMergeVfx=null;}
        if(electricityInstance!=null){Destroy(electricityInstance);electricityInstance=null;}
        mergeSource.Stop();
        PlayVfx(revealVfx,target,12);mergeProgress=1;revealTime=Time.time;
        var body=Instantiate(bossPrefab,target,Quaternion.identity);body.SetActive(true);Boss=body.GetComponent<EnemyHealth>();if(Boss==null)Boss=body.AddComponent<EnemyHealth>();Boss.SetFullHealth(bossHealth);
        // The placeholder is invulnerable during its introduction; normal hits resume with control.
        foreach(var collider in body.GetComponentsInChildren<Collider>())collider.enabled=false;
        roarSource.clip=fusionRoar;roarSource.volume=roarGain*(audioManager!=null?audioManager.SfxVolume:.8f);if(fusionRoar!=null)roarSource.Play();
        themeSource.clip=bossTheme;if(bossTheme!=null)themeSource.Play();
        yield return new WaitForSeconds(Mathf.Max(revealSeconds,fusionRoar!=null?Mathf.Min(fusionRoar.length,8):0));
        StopCameraDrama();
        if(playerCamera!=null)yield return Pan(playerCamera.transform.position,playerCamera.transform.rotation);
        RestoreControl();
        foreach(var collider in body.GetComponentsInChildren<Collider>())collider.enabled=true;
        IsCutsceneActive=false;
    }
    private Vector3 Sample(Vector3 point){NavMeshHit hit;return NavMesh.SamplePosition(point,out hit,8,NavMesh.AllAreas)?hit.position:point;}
    private IEnumerator Pan(Vector3 position,Quaternion rotation)
    {
        Vector3 from=fusionCamera.transform.position;Quaternion facing=fusionCamera.transform.rotation;float startFov=fusionCamera.fieldOfView;
        for(float t=0;t<panSeconds;t+=Time.deltaTime){float f=Mathf.SmoothStep(0,1,t/Mathf.Max(.01f,panSeconds));fusionCamera.transform.SetPositionAndRotation(Vector3.Lerp(from,position,f),Quaternion.Slerp(facing,rotation,f));fusionCamera.fieldOfView=Mathf.Lerp(startFov,originalFov,f);yield return null;}
        fusionCamera.transform.SetPositionAndRotation(position,rotation);
    }
    private void Update()
    {
        if(IsCutsceneActive && runners.Count>0){int arrived=0;foreach(var runner in runners)if(runner==null||runner.Arrived)arrived++;mergeProgress=(float)arrived/Mathf.Max(1,impCount);if(fusionCloud!=null)fusionCloud.SetProgress(mergeProgress);}
        if(electricityInstance!=null)electricityInstance.transform.localScale=Vector3.one*Mathf.Lerp(2,Mathf.Max(2,electricityFinalScale),mergeProgress);
        if(mergeSource!=null)mergeSource.volume=mergingGain*(audioManager!=null?audioManager.SfxVolume:.8f);
        if(themeSource!=null)themeSource.volume=themeGain*(audioManager!=null?audioManager.MusicVolume:.7f)*(Time.timeScale>0?1:0);
        if(!Started || Defeated || IsCutsceneActive || Boss==null)return;
        if(Boss.IsDead || !Boss.gameObject.activeInHierarchy)
        {
            Defeated=true;StartCoroutine(DeathExplosion());
        }
    }
    private void PlayImpSpawnVfx(Vector3 position)
    {
        if(impSpawnVfx==null && impSpawnVfx2==null){SpawnFog.Poof(position,1);return;}
        if(impSpawnVfx!=null)PlayVfx(impSpawnVfx,position,1);
        if(impSpawnVfx2!=null)PlayVfx(impSpawnVfx2,position,1);
    }
    private void StartWeather(Vector3 center)
    {
        if(rainVfx!=null)
        {
            Vector3 position=center+Vector3.up*rainHeight;
            Vector2 coverage=rainCoverage;
            var areaRenderer=rainArea!=null?rainArea.GetComponent<Renderer>():null;
            if(areaRenderer!=null)
            {
                var bounds=areaRenderer.bounds;
                position.x=bounds.center.x;position.z=bounds.center.z;
                coverage=new Vector2(bounds.size.x+20,bounds.size.z+20);
            }
            Vector3 cloudPosition = position + Vector3.up * rainCloudHeight;
            if (rainCloudVfx != null)
                cloudInstance = Instantiate(rainCloudVfx, cloudPosition, Quaternion.identity);
            else if (rainCloudMaterial != null || fusionFogMaterial != null)
            {
                cloudInstance = new GameObject("Boss rain clouds");
                cloudInstance.transform.position = cloudPosition;
                cloudInstance.AddComponent<RainCloudCanopy>().Initialize(
                    rainCloudMaterial != null ? rainCloudMaterial : fusionFogMaterial, coverage, rainCloudPuffSize);
            }
            if (cloudInstance != null) cloudInstance.name = "Boss rain clouds";
            rainInstance=Instantiate(rainVfx,position,Quaternion.identity);rainInstance.name="Boss island rain";
            var rain=rainInstance.GetComponent<ParticleSystem>();
            if(rain!=null)
            {
                rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var shape=rain.shape;var size=shape.scale;
                shape.scale=new Vector3(Mathf.Max(1,coverage.x),size.y,Mathf.Max(1,coverage.y));
                var emission=rain.emission;emission.rateOverTime=islandRainEmission;
                var main=rain.main;main.maxParticles=islandRainMaxParticles;main.startLifetime=15;main.simulationSpace=ParticleSystemSimulationSpace.World;
                var collision=rain.collision;collision.quality=ParticleSystemCollisionQuality.Medium;collision.enableDynamicColliders=false;
                rain.Play(true);
            }
        }
        if(lightningVfx!=null){lightningInstance=Instantiate(lightningVfx,center+lightningOffset,Quaternion.identity);lightningInstance.name="Boss island lightning";}
    }
    private void StopWeather()
    {
        if(rainInstance!=null){Destroy(rainInstance);rainInstance=null;}
        if(cloudInstance!=null){Destroy(cloudInstance);cloudInstance=null;}
        if(lightningInstance!=null){Destroy(lightningInstance);lightningInstance=null;}
    }
    private void PlayVfx(GameObject prefab,Vector3 position,float fallbackSize,float scale=1)
    {
        if(prefab==null){SpawnFog.Poof(position,fallbackSize);return;}
        var effect=Instantiate(prefab,position,Quaternion.identity);effect.transform.localScale*=scale;Destroy(effect,Mathf.Max(.1f,vfxLifetime));
    }
    private IEnumerator DeathExplosion()
    {
        IsCutsceneActive=true;deathSequenceRunning=true;StopCameraDrama();
        StopWeather();
        themeSource.Stop();if(audioManager!=null)audioManager.SetCinematicMusicDucked(true);
        Vector3 center=Boss.transform.position+Vector3.up*9;
        var renderers=Boss.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);center=bounds.center;}
        // Keep only the defeated boss's appearance during the explosion buildup.
        deathBody=Instantiate(Boss.gameObject,Boss.transform.position,Boss.transform.rotation);deathBody.SetActive(false);deathBody.name="Defeated boss cinematic visual";
        foreach(var b in deathBody.GetComponentsInChildren<MonoBehaviour>(true))Destroy(b);
        foreach(var c in deathBody.GetComponentsInChildren<Collider>(true))c.enabled=false;
        foreach(var a in deathBody.GetComponentsInChildren<NavMeshAgent>(true))a.enabled=false;
        foreach(var a in deathBody.GetComponentsInChildren<Animator>(true))a.enabled=false;
        var bar=deathBody.transform.Find("Healthbar");if(bar!=null)bar.gameObject.SetActive(false);
        foreach(var b in player.GetComponentsInChildren<MonoBehaviour>())
            if(b!=null && b.enabled&&(b is PlayerMovement||b is MouseLook||b is Gun||b is PlayerUnstuck||b is ShootCameraShake)){disabled.Add(b);b.enabled=false;}
        foreach(var pause in FindObjectsByType<PauseManager>(FindObjectsSortMode.None))if(pause.enabled){disabled.Add(pause);pause.enabled=false;}
        hud=GameObject.Find("PlayerUI");if(hud!=null){hudWasActive=hud.activeSelf;hud.SetActive(false);}
        playerCamera=Camera.main;
        if(playerCamera!=null){cameraWasEnabled=playerCamera.enabled;playerRetro=playerCamera.GetComponent<RetroCamera>();retroWasEnabled=playerRetro!=null&&playerRetro.enabled;if(playerRetro!=null)playerRetro.enabled=false;playerCamera.enabled=false;fusionCamera.transform.SetPositionAndRotation(playerCamera.transform.position,playerCamera.transform.rotation);}
        fusionCamera.enabled=true;var retro=fusionCamera.GetComponent<RetroCamera>();if(retro!=null)retro.enabled=true;
        yield return null;deathBody.SetActive(true);
        Vector3 towardPlayer=player.position-center;towardPlayer.y=0;if(towardPlayer.sqrMagnitude<1)towardPlayer=Vector3.back;
        Vector3 shot=deathCameraShot!=null?deathCameraShot.position:center+towardPlayer.normalized*30+Vector3.up*8;
        Quaternion facing=deathCameraShot!=null?deathCameraShot.rotation:Quaternion.LookRotation(center-shot);
        yield return Pan(shot,facing);
        Vector3 bodyScale=deathBody.transform.localScale;float nextBurst=0;
        for(float t=0;t<Mathf.Max(.1f,deathBuildSeconds);t+=Time.deltaTime)
        {
            float f=t/Mathf.Max(.1f,deathBuildSeconds);
            deathBody.transform.localScale=bodyScale*(1+.12f*f);
            if(t>=nextBurst){PlayVfx(deathBurstVfx,center+Random.insideUnitSphere*5,2,deathBurstScale);nextBurst=t+.3f;}
            fusionCamera.transform.SetPositionAndRotation(shot+Random.insideUnitSphere*deathShake*.3f*f,facing);
            fusionCamera.fieldOfView=originalFov-4*f;yield return null;
        }
        PlayVfx(deathExplosionVfx,center,16,deathExplosionScale);Destroy(deathBody);deathBody=null;
        if(deathExplosionSound!=null){roarSource.volume=audioManager!=null?audioManager.SfxVolume:1;roarSource.PlayOneShot(deathExplosionSound);}
        deathFlash=new GameObject("Boss explosion flash",typeof(Canvas));var canvas=deathFlash.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=2000;
        var flashObject=new GameObject("Flash",typeof(RectTransform),typeof(UnityEngine.UI.Image));flashObject.transform.SetParent(deathFlash.transform,false);var flash=flashObject.GetComponent<UnityEngine.UI.Image>();flash.raycastTarget=false;
        flash.rectTransform.anchorMin=Vector2.zero;flash.rectTransform.anchorMax=Vector2.one;flash.rectTransform.offsetMin=flash.rectTransform.offsetMax=Vector2.zero;
        for(float t=0;t<Mathf.Max(.3f,deathExplosionHold);t+=Time.deltaTime)
        {
            float strength=Mathf.Clamp01(1-t/.8f);fusionCamera.transform.SetPositionAndRotation(shot+Random.insideUnitSphere*deathShake*strength,facing*Quaternion.Euler(0,0,Mathf.Sin(t*55)*strength));
            flash.color=new Color(1,.85f,.55f,deathFlashAlpha*Mathf.Clamp01(1-t/.3f));yield return null;
        }
        Destroy(deathFlash);deathFlash=null;deathSequenceRunning=false;
        if(playerCamera!=null)yield return Pan(playerCamera.transform.position,playerCamera.transform.rotation);
        RestoreControl();IsCutsceneActive=false;IsEncounterActive=false;
        if(finalMistDoor!=null)finalMistDoor.SetActive(false);
        if(audioManager!=null)audioManager.SetCinematicMusicDucked(false);
        PlayerCheer.CelebrateBoss();
    }
    private void LateUpdate()
    {
        if(deathSequenceRunning)return;
        if(!cameraDrama || fusionCamera==null || Time.timeScale<=0)return;
        float kick=revealTime<0?0:Mathf.Clamp01(1-(Time.time-revealTime)/.8f);
        float amount=revealTime<0?mergeShake*Mathf.Lerp(.15f,1,mergeProgress):revealShake*kick;
        float t=Time.time*18;
        float x=(Mathf.PerlinNoise(t,3)-.5f)*2,y=(Mathf.PerlinNoise(7,t)-.5f)*2;
        fusionCamera.transform.SetPositionAndRotation(dramaPosition+dramaRotation*new Vector3(x,y*.65f,0)*amount,dramaRotation*Quaternion.Euler(y*amount*2,x*amount, x*amount*4));
        float zoom=originalFov-mergeZoomDegrees*Mathf.SmoothStep(0,1,mergeProgress);
        fusionCamera.fieldOfView=Mathf.Lerp(fusionCamera.fieldOfView,zoom,1-Mathf.Exp(-4*Time.deltaTime));
    }
    private void StopCameraDrama()
    {
        if(cameraDrama && fusionCamera!=null)fusionCamera.transform.SetPositionAndRotation(dramaPosition,dramaRotation);
        cameraDrama=false;
    }
    private void RestoreControl()
    {
        StopCameraDrama();
        if(fusionCamera!=null){fusionCamera.fieldOfView=originalFov;fusionCamera.enabled=false;var retro=fusionCamera.GetComponent<RetroCamera>();if(retro!=null)retro.enabled=false;}
        if(playerCamera!=null)playerCamera.enabled=cameraWasEnabled;if(playerRetro!=null)playerRetro.enabled=retroWasEnabled;
        foreach(var b in disabled)if(b!=null)b.enabled=true;disabled.Clear();
        if(hud!=null)hud.SetActive(hudWasActive);
    }
    private void OnDisable()
    {
        if(!Started)return;
        if(customMergeVfx!=null)Destroy(customMergeVfx);
        if(electricityInstance!=null)Destroy(electricityInstance);
        StopWeather();
        if(deathBody!=null)Destroy(deathBody);if(deathFlash!=null)Destroy(deathFlash);
        deathSequenceRunning=false;
        if(fusionCloud!=null)Destroy(fusionCloud.gameObject);
        StopAllCoroutines();RestoreControl();IsCutsceneActive=IsEncounterActive=false;
        foreach(var runner in runners)if(runner!=null)Destroy(runner.gameObject);runners.Clear();
        if(mergeSource!=null)mergeSource.Stop();
        if(themeSource!=null)themeSource.Stop();if(roarSource!=null)roarSource.Stop();
        if(audioManager!=null)audioManager.SetCinematicMusicDucked(false);
    }
}


