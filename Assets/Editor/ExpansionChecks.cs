using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExpansionChecks
{
    private static IEnumerator routine;private static double until;
    private static readonly List<string> results=new List<string>();
    public static string Report=>string.Join("\n",results);
    public static void Run(){results.Clear();routine=Checks();until=0;EditorApplication.update+=Step;}
    private static void Step()
    {
        if(EditorApplication.timeSinceStartup<until)return;
        try{if(routine.MoveNext()){until=EditorApplication.timeSinceStartup+Convert.ToDouble(routine.Current);return;}}
        catch(Exception ex){results.Add("FAIL "+ex);}
        EditorApplication.update-=Step;System.IO.Directory.CreateDirectory("Temp/CombatChecks");System.IO.File.WriteAllText("Temp/CombatChecks/expansion-report.txt",Report);
    }
    private static void Check(bool ok,string label){results.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(label);}
    private static IEnumerator WaitGame(float seconds){float target=Time.time+seconds;while(Time.time<target)yield return .1;}
    private static IEnumerator Checks()
    {
        Application.runInBackground=true;
        Check(new[]{"MainMenu","MainScene","DeathScene","VictoryScene"}.All(Application.CanStreamedLevelBeLoaded),"All four scenes loadable");
        var p=UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();var rescue=p.GetComponent<PlayerUnstuck>();
        int hp=p.currentHealth;rescue.Request();Check(!rescue.IsRecovering&&p.currentHealth==hp,"First T warns freely moving player without cost");
        typeof(PlayerUnstuck).GetField("lastBlocked",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(rescue,Time.time);
        Check(!rescue.IsRecovering&&rescue.prompt.text.Contains("-10 HP"),"First T presents warning without charging");
        rescue.Request();Check(rescue.IsRecovering&&p.currentHealth==hp-10,"Second T charges exactly 10 HP and begins rescue");
        p.TakeDamage(999);Check(p.currentHealth==hp-10,"Rescue protects player during flight");
        float end=Time.time+5;while(rescue.IsRecovering&&Time.time<end)yield return .1;
        Check(!rescue.IsRecovering&&p.GetComponent<CharacterController>().enabled&&p.GetComponent<PlayerMovement>().enabled,"Rescue restores controller and movement");
        Vector3 safe;rescue.FindDestination(out safe);Check(Vector3.Distance(p.transform.position,safe)<1,"Rescue lands at closest safe island center");
        p.GetComponent<PlayerMovement>().enabled=false;p.GetComponent<MouseLook>().enabled=false;p.GetComponentInChildren<Gun>().enabled=false;
        var ranged=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/SceneStuff/EnemyRanged.prefab");
        var boss=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/SceneStuff/GiantRoboMonkey.prefab");
        Check(boss.GetComponent<EnemyHealth>().maxHealth==ranged.GetComponent<EnemyHealth>().maxHealth*3,"Boss has triple ranged health");
        Check(boss.transform.localScale.x>ranged.transform.localScale.x*2,"Boss is visibly giant");
        var wave=UnityEngine.Object.FindObjectsByType<WaveArea>(FindObjectsSortMode.None).First(w=>w.name=="Island3");
        Check(wave.waves.Last().bossWave&&wave.bossPrefab==boss,"Island 3 ends in boss wave");
        var robot=UnityEngine.Object.Instantiate(ranged,wave.spawnPoints[0].position,Quaternion.identity);var ai=robot.GetComponent<RangedEnemy>();
        yield return .2;ai.enabled=false;ai.player=p.transform;
        Check(Mathf.Approximately(ai.noscopeChance,.1f),"Ranged noscope chance is 10 percent");
        typeof(RangedEnemy).GetMethod("FireProjectile",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(ai,new object[]{true});
        var projectile=UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).First(x=>x.damage==ai.projectileDamage*2);
        Check(projectile!=null,"Noscope projectile deals double damage");UnityEngine.Object.Destroy(projectile.gameObject);
        ai.noscopeSeconds=1;var visual=ai.characterAnimation.animator.transform;Vector3 original=visual.localPosition;Quaternion originalRotation=visual.localRotation;
        ai.StartCoroutine("JumpingNoscope");float trickTime=Time.time+.3f;while(Time.time<trickTime)yield return .02;
        Check(visual.localPosition.y>original.y+.3f&&Quaternion.Angle(visual.localRotation,originalRotation)>30,"Noscope visibly jumps and spins");
        trickTime=Time.time+1;while(Time.time<trickTime)yield return .05;
        Check(Vector3.Distance(visual.localPosition,original)<.01f&&Quaternion.Angle(visual.localRotation,originalRotation)<1,"Noscope lands and restores visual pose");
        UnityEngine.Object.Destroy(robot);
        var inventory=p.GetComponent<PlayerBucketInventory>();int before=inventory.buckets;var gate=UnityEngine.Object.FindFirstObjectByType<KabuWaveShop>();
        wave.waves=new[]{new WaveArea.IslandWave{bossWave=true,enemyCount=1}};wave.countdownSeconds=0;wave.spawnInterval=0;wave.Unlock();wave.StartWave();
        yield return .3;
        Check(WaveArea.AnyWaveActive&&gate.burning&&!gate.TryInteract(),"Active wave ignites Kabu and blocks shop");
        var spawned=UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).First(e=>e.name.Contains("Giant"));
        Check(spawned.maxHealth==Mathf.RoundToInt(boss.GetComponent<EnemyHealth>().maxHealth*(1+(wave.islandNumber-1)*wave.healthIncreasePerIsland)),"Wave spawns configured boss");spawned.TakeDamage(9999);
        end=Time.time+3;while(!wave.waveComplete&&Time.time<end)yield return .1;
        Check(wave.waveComplete&&!WaveArea.AnyWaveActive&&inventory.buckets==before,"Cleared island releases shop lock without automatic bucket credit");
        var pickup=UnityEngine.Object.FindFirstObjectByType<WaterBucketPickup>();Check(pickup!=null,"Cleared island spawns physical bucket");pickup.SendMessage("OnTriggerEnter",p.GetComponent<Collider>());
        gate.TryInteract();Check(!gate.burning&&inventory.buckets==before&&gate.TryInteract(),"Bucket extinguishes Kabu and unlocks shop");
        var shop=gate.GetComponent<ShopManager>();typeof(ShopManager).GetMethod("OpenShop",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(shop,null);
        Check(Time.timeScale==0&&shop.shopPanel.activeSelf,"Shop still opens upgrades and pauses gameplay");shop.CloseShop();Check(Time.timeScale==1,"Closing shop restores time");
        var camera=p.GetComponentInChildren<Camera>();Check(camera.targetTexture!=null&&camera.targetTexture.height==400&&camera.targetTexture.filterMode==FilterMode.Point,"Retro camera renders point-filtered 400-line image");
        Check(p.healthText.font.name.Contains("Electronic"),"HUD uses retro font");
        // Verify the real scene routes, including loading out of a paused game.
        Time.timeScale=0;p.Die();yield return 1;
        Check(SceneManager.GetActiveScene().name=="DeathScene"&&Time.timeScale==1&&Cursor.visible,"Ordinary death enters DeathScene with working time and cursor");
        var death=UnityEngine.Object.FindFirstObjectByType<DeathButtons>();death.Retry();yield return 1;
        Check(SceneManager.GetActiveScene().name=="MainScene"&&UnityEngine.Object.FindFirstObjectByType<PlayerHealth>().currentHealth>0,"Death Retry restarts gameplay");
        var loader=UnityEngine.Object.FindFirstObjectByType<ModelSceneLoader>();var player=UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
        loader.SendMessage("OnTriggerEnter",player.GetComponent<Collider>());yield return 1;
        Check(SceneManager.GetActiveScene().name=="VictoryScene"&&Cursor.visible,"Boat player contact loads VictoryScene");
        UnityEngine.Object.FindFirstObjectByType<VictoryButtons>().BackToMainMenu();yield return 1;
        Check(SceneManager.GetActiveScene().name=="MainMenu"&&Time.timeScale==1,"Victory returns to MainMenu");
        UnityEngine.Object.FindFirstObjectByType<VideoIntro>().OnContinue();yield return 1;
        Check(SceneManager.GetActiveScene().name=="MainScene","MainMenu Start loads gameplay");
        var drowning=UnityEngine.Object.FindFirstObjectByType<DrowningSequence>();drowning.heavenlyVoice=null;drowning.minimumMessageSeconds=1;drowning.ragdollDelay=0;drowning.ragdollRestSeconds=.1f;drowning.whiteFadeSeconds=.3f;
        drowning.Begin(UnityEngine.Object.FindFirstObjectByType<PlayerHealth>());yield return 2;
        Check(SceneManager.GetActiveScene().name=="DeathScene","Heavenly ascent and whiteout also enter DeathScene");
        results.Add("COMPLETE");
    }
}

