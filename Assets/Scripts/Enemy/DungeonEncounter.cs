using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DungeonEncounter : MonoBehaviour
{
    [Header("Evil Kabu placeholder — replace Visual and animation state clips later")]
    public EnemyHealth boss;
    public Transform visual;
    public Animator animator;
    public int bossMaxHealth=10000;
    [Header("Attacks and imp reinforcements")]
    public GameObject impPrefab;
    public int impAmmoDrop=75;
    [Header("Sky supplies — independent roll after each attack")]
    public GameObject skyAmmoPrefab, skySuperBananaPrefab;
    [Range(0,1)] public float skyAmmoChance=.1f, skySuperBananaChance=.05f;
    public int skyAmmoAmount=50;
    public float approachSeconds=.4f;
    public float facingDegreesPerSecond=540;
    private bool facingLocked;
    private GameObject airborneImp;
    public float windupSeconds=1.2f;
    public float recoverySeconds=1.8f;
    public int rockDamage=20,punchDamage=30,bellyDamage=35;
    public float rockRadius=4,punchLength=14,punchWidth=7,bellyRadius=9;
    public Transform circleIndicator,rectangleIndicator;
    public Material rockMaterial;
    public GameObject boulderPrefab;
    public int movesPerImp=6;
    public static int ChooseAttack(float roll){return roll<.33f?0:roll<.67f?1:2;}
    [Header("Exit sequence")]
    public GameObject keyPickup;
    public Transform door;
    public Camera doorCamera;
    public AudioClip bossTheme,heavenlyDoorMusic;
    public TMP_Text statusText;
    public Transform rescueCenter;
    public bool BossDefeated {get;private set;}
    public bool HasKey {get;private set;}
    public bool DoorOpened {get;private set;}
    public int LastAttack {get;private set;}=-1;
    public static bool IsCutsceneActive {get;private set;}
    private PlayerHealth player;
    private AudioSource heavenlySource;
    private Coroutine fight;
    private GameObject activeRock;
    private Vector3 originalVisualScale;
    private Camera mainCamera;
    private RetroCamera mainRetro;
    private bool mainWasEnabled,retroWasEnabled;
    private readonly List<Behaviour> suspended=new List<Behaviour>();
    private bool ownsMusic;
    private static bool saved;
    private static int savedHP,savedAmmo,savedParts,savedBananas;
    private static float savedFireRate;
    public static void ResetLoadout(){saved=false;}
    public static void CaptureLoadout(PlayerHealth hp,Gun gun){saved=true;savedHP=hp.maxHealth;savedAmmo=gun.maxAmmo;savedFireRate=gun.fireRate;savedParts=hp.GetComponent<PlayerParts>().currentParts;savedBananas=hp.GetComponent<SpeedBoostAbility>().storedBananas;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState(){saved=false;IsCutsceneActive=false;}
    private void Start()
    {
        player=FindFirstObjectByType<PlayerHealth>();
        if(saved){player.IncreaseMaxHealth(savedHP-player.maxHealth);player.currentHealth=savedHP;var gun=FindFirstObjectByType<Gun>();gun.maxAmmo=savedAmmo;gun.fireRate=savedFireRate;gun.Reload();player.GetComponent<PlayerParts>().AddParts(savedParts-player.GetComponent<PlayerParts>().currentParts);player.GetComponent<SpeedBoostAbility>().storedBananas=savedBananas;}
        boss.SetFullHealth(bossMaxHealth);originalVisualScale=visual.localScale;
        circleIndicator.gameObject.SetActive(false);rectangleIndicator.gameObject.SetActive(false);keyPickup.SetActive(false);
        doorCamera.enabled=false;var retro=doorCamera.GetComponent<RetroCamera>();if(retro!=null)retro.enabled=false;
        heavenlySource=gameObject.AddComponent<AudioSource>();heavenlySource.playOnAwake=false;
        if(GameAudio.Instance!=null)ownsMusic=GameAudio.Instance.BeginMinibossMusic(bossTheme);
        if(statusText!=null)statusText.text="EVIL KABU  —  DODGE THE RED MARKERS";
        fight=StartCoroutine(Fight());
    }
    private void Update()
    {
        if(!BossDefeated && boss!=null && !boss.IsDead && player!=null && !facingLocked)
        {
            Vector3 direction=player.transform.position-boss.transform.position;direction.y=0;
            if(direction.sqrMagnitude>.01f)boss.transform.rotation=Quaternion.RotateTowards(boss.transform.rotation,Quaternion.LookRotation(direction),facingDegreesPerSecond*Time.deltaTime);
        }
        if(heavenlySource!=null&&GameAudio.Instance!=null)heavenlySource.volume=GameAudio.Instance.MusicVolume;
        if(!BossDefeated && boss!=null && (boss.IsDead||!boss.gameObject.activeInHierarchy))DefeatBoss();
    }
    private IEnumerator Fight()
    {
        yield return new WaitForSeconds(2);
        int moves=0;
        while(!BossDefeated)
        {
            if(boss==null||boss.IsDead)yield break;
            int attack=ChooseAttack(Random.value);
            LastAttack=attack;Vector3 target=player.transform.position;target.y=0;target.x=Mathf.Clamp(target.x,-29,29);target.z=Mathf.Clamp(target.z,-43,42);
            if(attack==0)yield return RockThrow(target);
            else if(attack==1)yield return MegaPunch(target);
            else yield return Stomp(target);
            HideIndicators();Animate("Idle");if(visual!=null)visual.localScale=originalVisualScale;
            RollSkySupplies();moves++;yield return new WaitForSeconds(recoverySeconds);
            if(moves%Mathf.Max(1,movesPerImp)==0 && !BossDefeated){LastAttack=3;yield return ThrowImp();RollSkySupplies();HideIndicators();Animate("Idle");yield return new WaitForSeconds(recoverySeconds);}
        }
    }
    private void Animate(string state){if(animator!=null)animator.CrossFadeInFixedTime(state,.1f);}
    private void ShowCircle(Vector3 point,float radius){circleIndicator.position=point+Vector3.up*.08f;circleIndicator.localScale=new Vector3(radius*2,.035f,radius*2);circleIndicator.gameObject.SetActive(true);}
    private void HideIndicators(){circleIndicator.gameObject.SetActive(false);rectangleIndicator.gameObject.SetActive(false);}
    private IEnumerator RockThrow(Vector3 target)
    {
        Animate("RockThrow");ShowCircle(target,rockRadius);yield return new WaitForSeconds(windupSeconds);
        if(boulderPrefab!=null)activeRock=Instantiate(boulderPrefab);
        else {activeRock=GameObject.CreatePrimitive(PrimitiveType.Sphere);activeRock.GetComponent<Renderer>().sharedMaterial=rockMaterial;activeRock.transform.localScale=Vector3.one*2;}
        activeRock.name="Evil Kabu thrown rock";
        foreach(var c in activeRock.GetComponentsInChildren<Collider>())c.enabled=false;
        foreach(var rb in activeRock.GetComponentsInChildren<Rigidbody>()){rb.isKinematic=true;rb.useGravity=false;}
        Vector3 start=boss.transform.position+Vector3.up*9;
        for(float t=0;t<1;t+=Time.deltaTime){activeRock.transform.position=Vector3.Lerp(start,target,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*7;activeRock.transform.Rotate(new Vector3(80,140,40)*Time.deltaTime);yield return null;}
        DamageCircle(target,rockRadius,rockDamage);SpawnFog.Poof(target,3);Destroy(activeRock);activeRock=null;
    }
    private IEnumerator MegaPunch(Vector3 target)
    {
        // Keep the committed punch aligned with its red ground indicator.
        facingLocked=true;
        // Approach first so a melee attack remains a threat across the arena.
        Vector3 direction=target-boss.transform.position;direction.y=0;if(direction.sqrMagnitude<.01f)direction=Vector3.forward;direction.Normalize();
        Vector3 destination=target-direction*8;destination.x=Mathf.Clamp(destination.x,-28,28);destination.z=Mathf.Clamp(destination.z,-40,38);
        Animate("Run");boss.transform.rotation=Quaternion.LookRotation(direction);Vector3 from=boss.transform.position;for(float t=0;t<Mathf.Max(.05f,approachSeconds);t+=Time.deltaTime){boss.transform.position=Vector3.Lerp(from,destination,t/Mathf.Max(.05f,approachSeconds));yield return null;}
        boss.transform.position=destination;boss.transform.rotation=Quaternion.LookRotation(direction);
        Vector3 center=destination+direction*(punchLength*.5f);rectangleIndicator.SetPositionAndRotation(center+Vector3.up*.08f,Quaternion.LookRotation(direction));rectangleIndicator.localScale=new Vector3(punchWidth,.05f,punchLength);rectangleIndicator.gameObject.SetActive(true);
        Animate("MegaPunch");yield return new WaitForSeconds(windupSeconds);
        // The final model supplies the punch motion.
        Vector3 local=Quaternion.Inverse(rectangleIndicator.rotation)*(player.transform.position-center);
        if(Mathf.Abs(local.x)<=punchWidth*.5f&&Mathf.Abs(local.z)<=punchLength*.5f&&player.transform.position.y<5)player.TakeDamage(punchDamage);
        SpawnFog.Poof(center,3);yield return new WaitForSeconds(.2f);facingLocked=false;
    }
    private IEnumerator Stomp(Vector3 target)
    {
        Animate("Idle");ShowCircle(target,bellyRadius);yield return new WaitForSeconds(windupSeconds);
        Vector3 start=boss.transform.position;
        Animate("Stomp");for(float t=0;t<1.1f;t+=Time.deltaTime){float f=t/1.1f;boss.transform.position=Vector3.Lerp(start,target,f)+Vector3.up*Mathf.Sin(f*Mathf.PI)*8;yield return null;}
        boss.transform.position=target;DamageCircle(target,bellyRadius,bellyDamage);SpawnFog.Poof(target,6);yield return new WaitForSeconds(.3f);
    }
    private void RollSkySupplies()
    {
        if(BossDefeated || boss==null || boss.IsDead)return;
        if(Random.value<skyAmmoChance)DropSkySupply(skyAmmoPrefab,true);
        if(Random.value<skySuperBananaChance)DropSkySupply(skySuperBananaPrefab,false);
    }
    private void DropSkySupply(GameObject prefab,bool ammo)
    {
        if(prefab==null)return;
        Vector2 offset=Random.insideUnitCircle*9;
        Vector3 position=player.transform.position+new Vector3(offset.x,0,offset.y);
        position.x=Mathf.Clamp(position.x,-27,27);position.z=Mathf.Clamp(position.z,-39,39);position.y=18;
        var drop=Instantiate(prefab,position,prefab.transform.rotation);
        var pickup=drop.GetComponent<AmmoPickup>();if(ammo&&pickup!=null)pickup.amount=skyAmmoAmount;
        var motion=drop.GetComponent<LootMotion>();if(motion==null)motion=drop.AddComponent<LootMotion>();
        motion.flightSeconds=2;motion.jumpHeight=0;motion.scatterRadius=0;motion.Launch();
    }
    private IEnumerator ThrowImp()
    {
        if(impPrefab==null)yield break;
        Vector3 target=player.transform.position;target.y=0;
        target.x=Mathf.Clamp(target.x,-28,28);target.z=Mathf.Clamp(target.z,-40,40);
        Animate("ThrowImp");ShowCircle(target,2);
        yield return new WaitForSeconds(windupSeconds);
        var staging=new GameObject("Imp launch staging");staging.SetActive(false);
        airborneImp=Instantiate(impPrefab,boss.transform.position+Vector3.up*9,Quaternion.identity,staging.transform);
        var nav=airborneImp.GetComponent<UnityEngine.AI.NavMeshAgent>();if(nav!=null)nav.enabled=false;
        airborneImp.name="Kabu thrown imp — bonus ammo";
        var behaviours=new List<Behaviour>();
        foreach(var b in airborneImp.GetComponentsInChildren<Behaviour>())
            if(b.enabled && (b is MeleeEnemy || b is MeleeAttack || b is UnityEngine.AI.NavMeshAgent)){behaviours.Add(b);b.enabled=false;}
        var colliders=new List<Collider>();
        foreach(var c in airborneImp.GetComponentsInChildren<Collider>())if(c.enabled){colliders.Add(c);c.enabled=false;}
        var enemy=airborneImp.GetComponent<Enemy>();
        if(enemy!=null){enemy.ammoDropChance=1;enemy.minAmmoAmount=enemy.maxAmmoAmount=impAmmoDrop;}
        var melee=airborneImp.GetComponent<MeleeEnemy>();if(melee!=null)melee.chaseRange=150;
        airborneImp.transform.SetParent(null,true);Destroy(staging);
        Vector3 start=airborneImp.transform.position;
        for(float t=0;t<1.2f;t+=Time.deltaTime){float f=t/1.2f;airborneImp.transform.position=Vector3.Lerp(start,target,f)+Vector3.up*Mathf.Sin(f*Mathf.PI)*8;yield return null;}
        airborneImp.transform.position=target;
        foreach(var c in colliders)if(c!=null)c.enabled=true;
        foreach(var b in behaviours)if(b!=null)b.enabled=true;
        SpawnFog.Poof(target,2);airborneImp=null;
    }
    public void DamageCircle(Vector3 center,float radius,int damage){Vector3 delta=player.transform.position-center;delta.y=0;if(delta.sqrMagnitude<=radius*radius&&player.transform.position.y<center.y+5)player.TakeDamage(damage);}
    private void DefeatBoss()
    {
        BossDefeated=true;PlayerCheer.CelebrateBoss();if(fight!=null)StopCoroutine(fight);HideIndicators();if(activeRock!=null)Destroy(activeRock);if(airborneImp!=null)Destroy(airborneImp);
        if(ownsMusic&&GameAudio.Instance!=null){GameAudio.Instance.EndMinibossMusic();ownsMusic=false;}
        keyPickup.transform.position=new Vector3(Mathf.Clamp(boss.transform.position.x,-28,28),1,Mathf.Clamp(boss.transform.position.z,-40,40));keyPickup.SetActive(true);var motion=keyPickup.GetComponent<LootMotion>();if(motion!=null)motion.Launch();
        if(statusText!=null)statusText.text="EVIL KABU DEFEATED — PICK UP HIS KEY";
    }
    public void CollectKey(){if(!BossDefeated)return;HasKey=true;if(statusText!=null)statusText.text="KEY FOUND — UNLOCK THE FAR DOOR";}
    public void TryOpenDoor()
    {
        if(DoorOpened||IsCutsceneActive||PlayerCheer.IsCutsceneActive)return;
        if(!HasKey){if(statusText!=null)statusText.text="LOCKED — DEFEAT EVIL KABU AND FIND HIS KEY";return;}
        StartCoroutine(OpenDoor());
    }
    private IEnumerator OpenDoor()
    {
        IsCutsceneActive=true;
        foreach(var b in player.GetComponentsInChildren<MonoBehaviour>())if(b.enabled&&(b is PlayerMovement||b is MouseLook||b is Gun||b is ShootCameraShake||b is PlayerUnstuck)){suspended.Add(b);b.enabled=false;}
        var pause=FindFirstObjectByType<PauseManager>();if(pause!=null&&pause.enabled){suspended.Add(pause);pause.enabled=false;}
        if(GameAudio.Instance!=null)GameAudio.Instance.SetCinematicMusicDucked(true);
        heavenlySource.clip=heavenlyDoorMusic;if(heavenlyDoorMusic!=null)heavenlySource.Play();
        mainCamera=Camera.main;mainWasEnabled=mainCamera.enabled;mainRetro=mainCamera.GetComponent<RetroCamera>();retroWasEnabled=mainRetro!=null&&mainRetro.enabled;
        if(mainRetro!=null)mainRetro.enabled=false;mainCamera.enabled=false;
        Vector3 shot=doorCamera.transform.position;Quaternion rotation=doorCamera.transform.rotation;
        doorCamera.transform.SetPositionAndRotation(mainCamera.transform.position,mainCamera.transform.rotation);doorCamera.enabled=true;var retro=doorCamera.GetComponent<RetroCamera>();if(retro!=null)retro.enabled=true;
        if(statusText!=null)statusText.text="THE WAY OUT OPENS...";
        yield return Pan(shot,rotation,1.2f);Vector3 closed=door.position;
        for(float t=0;t<2.5f;t+=Time.deltaTime){door.position=closed+Vector3.up*13*Mathf.SmoothStep(0,1,t/2.5f);yield return null;}
        door.position=closed+Vector3.up*13;foreach(var c in door.GetComponentsInChildren<Collider>())c.enabled=false;DoorOpened=true;
        yield return Pan(mainCamera.transform.position,mainCamera.transform.rotation,1.2f);RestoreControls();
        heavenlySource.Stop();if(GameAudio.Instance!=null)GameAudio.Instance.SetCinematicMusicDucked(false);IsCutsceneActive=false;
        if(statusText!=null)statusText.text="TAKE THE BOAT OUTSIDE — YOU EARNED IT";
    }
    private IEnumerator Pan(Vector3 position,Quaternion rotation,float seconds){Vector3 start=doorCamera.transform.position;Quaternion facing=doorCamera.transform.rotation;for(float t=0;t<seconds;t+=Time.deltaTime){float f=Mathf.SmoothStep(0,1,t/seconds);doorCamera.transform.SetPositionAndRotation(Vector3.Lerp(start,position,f),Quaternion.Slerp(facing,rotation,f));yield return null;}doorCamera.transform.SetPositionAndRotation(position,rotation);}
    private void RestoreControls(){if(doorCamera!=null){doorCamera.enabled=false;var retro=doorCamera.GetComponent<RetroCamera>();if(retro!=null)retro.enabled=false;}if(mainCamera!=null)mainCamera.enabled=mainWasEnabled;if(mainRetro!=null)mainRetro.enabled=retroWasEnabled;foreach(var b in suspended)if(b!=null)b.enabled=true;suspended.Clear();}
    private void OnDisable(){StopAllCoroutines();RestoreControls();IsCutsceneActive=false;if(activeRock!=null)Destroy(activeRock);if(airborneImp!=null)Destroy(airborneImp);if(heavenlySource!=null)heavenlySource.Stop();if(GameAudio.Instance!=null){if(ownsMusic)GameAudio.Instance.EndMinibossMusic();GameAudio.Instance.SetCinematicMusicDucked(false);}}
}
