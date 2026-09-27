using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class IslandPolishChecks
{
    private static IEnumerator routine;private static double until;
    private static readonly List<string> results=new List<string>();
    public static string Report=>string.Join("\n",results);
    public static void Run(){EditorApplication.update-=Step;results.Clear();routine=Checks();until=0;EditorApplication.update+=Step;}
    private static void Step()
    {
        if(EditorApplication.timeSinceStartup<until)return;
        try{if(routine.MoveNext()){until=EditorApplication.timeSinceStartup+Convert.ToDouble(routine.Current);return;}}
        catch(Exception ex){results.Add("FAIL "+ex);}
        EditorApplication.update-=Step;System.IO.Directory.CreateDirectory("Temp/CombatChecks");System.IO.File.WriteAllText("Temp/CombatChecks/island-polish-report.txt",Report);
    }
    private static void Check(bool ok,string label){results.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(label);}
    private static IEnumerator Checks()
    {
        Application.runInBackground=true;
        var p=UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();var rescue=p.GetComponent<PlayerUnstuck>();int hp=p.currentHealth;
        Check(!rescue.IsStuck,"Player is freely moving at test start");
        rescue.Request();Check(!rescue.IsRecovering&&p.currentHealth==hp&&rescue.prompt.text.Contains("-10 HP"),"First T warns anywhere without charging");
        rescue.Request();Check(rescue.IsRecovering&&p.currentHealth==hp-10,"Second T starts rescue anywhere, costs 10 HP");
        p.TakeDamage(50);Check(p.currentHealth==hp-10,"Rescue blocks incidental damage");
        float end=Time.time+6;while(rescue.IsRecovering&&Time.time<end)yield return .1;
        Check(!rescue.IsRecovering&&p.GetComponent<CharacterController>().enabled&&p.GetComponent<PlayerMovement>().enabled,"Ragdoll/tilt/flight restores controls");
        Vector3 safe;rescue.FindDestination(out safe);Check(Vector3.Distance(p.transform.position,safe)<1,"Rescue lands at closest available center");
        var movement=p.GetComponent<PlayerMovement>();movement.enabled=false;
        var waves=UnityEngine.Object.FindObjectsByType<WaveArea>(FindObjectsSortMode.None).OrderBy(w=>w.islandNumber).ToArray();
        Check(waves.Length==5&&waves.All(w=>w.shopSpawn!=null&&w.rescueCenter!=null&&w.waterBucketPrefab!=null),"All five islands have shop and bucket markers");
        var ranged=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/SceneStuff/EnemyRanged.prefab");
        var boss=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/SceneStuff/GiantRoboMonkey.prefab");
        Check(boss.GetComponent<RangedEnemy>().fireRate==ranged.GetComponent<RangedEnemy>().fireRate*.5f,"Island 3 miniboss fires twice as fast");
        var robot=UnityEngine.Object.Instantiate(ranged,waves[4].spawnPoints[0].position,Quaternion.identity);var ai=robot.GetComponent<RangedEnemy>();ai.enabled=false;
        waves[4].ScaleEnemy(robot);Check(robot.GetComponent<EnemyHealth>().maxHealth==Mathf.RoundToInt(ranged.GetComponent<EnemyHealth>().maxHealth*1.6f)&&ai.projectileDamage==Mathf.RoundToInt(ranged.GetComponent<RangedEnemy>().projectileDamage*1.4f),"Island 5 scales HP +60% and damage +40%");
        ai.player=p.transform;typeof(RangedEnemy).GetMethod("FireProjectile",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ai,new object[]{true});
        var projectile=UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).First(x=>x.isNoscope);Check(projectile.damage==ai.projectileDamage*2,"Noscope marks projectile and retains double damage");
        var audio=GameAudio.Instance;var clip=AudioClip.Create("Test hit",4410,1,44100,false);audio.enemyNoscopeHit=clip;int beforeHP=p.currentHealth;
        int voiceBefore=(int)typeof(GameAudio).GetField("nextVoice",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(audio);
        projectile.SendMessage("OnTriggerEnter",p.GetComponent<Collider>());
        Check(p.currentHealth==beforeHP-projectile.damage&&(int)typeof(GameAudio).GetField("nextVoice",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(audio)==(voiceBefore+1)%24,"Special hit damages player and plays separate impact voice");
        UnityEngine.Object.Destroy(robot);
        var inventory=p.GetComponent<PlayerBucketInventory>();int buckets=inventory.buckets;var wave=waves[0];wave.waves=new[]{new WaveArea.IslandWave{enemyCount=1},new WaveArea.IslandWave{enemyCount=1}};wave.countdownSeconds=0;wave.spawnInterval=0;wave.intermissionSeconds=1;wave.StartWave();yield return .3;
        var spawned=UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Where(h=>h.name.EndsWith("(Clone)")).ToArray();foreach(var enemy in spawned)enemy.TakeDamage(99999);
        yield return .3;Check(inventory.buckets==buckets&&UnityEngine.Object.FindObjectsByType<WaterBucketPickup>(FindObjectsSortMode.None).Length==0,"Intermediate wave awards no bucket");
        yield return 1.2;foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Where(h=>h.name.EndsWith("(Clone)")))enemy.TakeDamage(99999);
        yield return .5;
        var pickups=UnityEngine.Object.FindObjectsByType<WaterBucketPickup>(FindObjectsSortMode.None);
        Check(wave.waveComplete&&pickups.Length==1&&inventory.buckets==buckets,"Final wave spawns one physical bucket, no automatic charge");
        var shop=UnityEngine.Object.FindFirstObjectByType<KabuWaveShop>();Check(Vector3.Distance(shop.transform.position,wave.shopSpawn.position)<.01f,"Kabu moves to cleared island Shopspawn");
        pickups[0].SendMessage("OnTriggerEnter",p.GetComponent<Collider>());pickups[0].SendMessage("OnTriggerEnter",p.GetComponent<Collider>());Check(inventory.buckets==buckets+1,"Bucket pickup grants exactly one charge");
        shop.TryInteract();Check(!shop.burning&&inventory.buckets==buckets,"Collected bucket extinguishes Kabu");
        Check(UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Any(ps=>ps.name=="Pooled arrival cloud"),"Enemy spawns emit pooled fog");
        var encounter=UnityEngine.Object.FindFirstObjectByType<BossFusionEncounter>();
        encounter.TryEnter(p.GetComponent<Collider>());Check(!encounter.Started,"Boss entry waits for island 5 completion");
        waves[4].waveComplete=true;Check(!encounter.Started,"Island 5 completion alone does not start cinematic");
        var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=encounter.transform.position+Vector3.up*2;cc.enabled=true;movement.enabled=true;
        encounter.fusionRoar=clip;encounter.bossTheme=clip;
        encounter.TryEnter(cc);Check(encounter.Started&&BossFusionEncounter.IsCutsceneActive&&!movement.enabled&&encounter.fusionCamera.enabled,"Boss entry starts separate cinematic camera and suspends controls");
        int protectedHP=p.currentHealth;p.TakeDamage(9999);Check(p.currentHealth==protectedHP,"Cutscene protects player");
        end=Time.time+40;int maximumRunners=0;
        while(BossFusionEncounter.IsCutsceneActive&&Time.time<end){maximumRunners=Mathf.Max(maximumRunners,UnityEngine.Object.FindObjectsByType<FusionImpRunner>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length);yield return .1;}
        Check(maximumRunners==encounter.impCount,"Fusion uses the configured lightweight runner count");
        Check(!BossFusionEncounter.IsCutsceneActive&&movement.enabled&&!encounter.fusionCamera.enabled&&Camera.main!=null&&Camera.main.targetTexture!=null,"Cutscene pans back and restores player/retro camera");
        Check(encounter.Boss!=null&&encounter.Boss.maxHealth==750&&encounter.Boss.GetComponent<RangedEnemy>()==null&&encounter.Boss.GetComponent<MeleeEnemy>()==null,"Fusion produces passive 750 HP placeholder");
        Check(encounter.GetComponents<AudioSource>().Any(s=>s.loop&&s.isPlaying)&&encounter.finalMistDoor.activeSelf,"Boss theme loops and final door stays closed until defeated");
        movement.enabled=false;encounter.Boss.TakeDamage(750);yield return .2;
        Check(encounter.Defeated&&!encounter.finalMistDoor.activeSelf&&!BossFusionEncounter.IsEncounterActive,"Boss death opens final mist door and ends boss music state");
        foreach(var name in new[]{"MainMenu","DeathScene","VictoryScene"})
        {
            GameSceneFlow.Load(name);yield return 1.2;
            var theme=UnityEngine.Object.FindFirstObjectByType<SceneThemeMusic>();var source=theme.GetComponent<AudioSource>();
            Check(source.clip!=null&&source.isPlaying&&source.loop,name+" assigned music plays and loops");
            theme.theme=null;theme.enabled=false;theme.enabled=true;yield return .1;
            Check(source.isPlaying&&theme.theme==source.clip,name+" AudioSource clip fallback and same-clip restart work");
        }
        results.Add("COMPLETE");
    }
}

