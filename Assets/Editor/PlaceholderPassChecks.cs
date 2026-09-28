using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class PlaceholderPassChecks
{
    static IEnumerator routine;static double until;static readonly List<string> results=new List<string>();
    public static string Report=>string.Join("\n",results);
    public static void Run(){EditorApplication.update-=Step;results.Clear();routine=Checks();until=0;EditorApplication.update+=Step;}
    static void Step(){if(EditorApplication.timeSinceStartup<until)return;try{if(routine.MoveNext()){until=EditorApplication.timeSinceStartup+Convert.ToDouble(routine.Current);return;}}catch(Exception e){results.Add("FAIL "+e);}EditorApplication.update-=Step;System.IO.Directory.CreateDirectory("Temp/CombatChecks");System.IO.File.WriteAllText("Temp/CombatChecks/placeholder-pass-report.txt",Report);}
    static void Check(bool ok,string label){results.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(label);}
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static IEnumerator Checks()
    {
        Application.runInBackground=true;
        var tutorial=TutorialOverlay.Instance;var pause=UnityEngine.Object.FindFirstObjectByType<PauseManager>();
        Check(tutorial.IsOpen&&Time.timeScale==0,"Tutorial opens on first spawn and pauses gameplay");tutorial.Close();Check(!tutorial.IsOpen&&Time.timeScale==1,"Tutorial X restores gameplay");
        pause.Pause();tutorial.Open();Check(tutorial.IsOpen&&!pause.pausePanel.activeSelf,"Pause menu can reopen tutorial");tutorial.Close();Check(PauseManager.GameIsPaused&&pause.pausePanel.activeSelf&&Time.timeScale==0,"Closing reopened tutorial returns to pause menu");pause.Resume();
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();var movement=player.GetComponent<PlayerMovement>();var gun=UnityEngine.Object.FindFirstObjectByType<Gun>();
        Check(gun.range==300&&gun.bulletPrefab.GetComponent<Bullet>().lifetime==6,"Damage range and visible bullet lifetime are tripled");
        var camObject=new GameObject("Range check camera");var cam=camObject.AddComponent<Camera>();cam.enabled=false;cam.transform.position=new Vector3(1000,100,1000);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=cam.transform.position+Vector3.forward*250;target.transform.localScale=Vector3.one*4;var health=target.AddComponent<EnemyHealth>();
        var originalCamera=gun.aimCamera;var originalBullet=gun.bulletPrefab;var originalImpact=gun.impactEffect;gun.aimCamera=cam;gun.bulletPrefab=null;gun.impactEffect=null;Physics.SyncTransforms();
        typeof(Gun).GetMethod("Shoot",Private).Invoke(gun,new object[]{false});Check(health.currentHealth==100-gun.damage,"Player can damage an enemy 250 units away");gun.aimCamera=originalCamera;gun.bulletPrefab=originalBullet;gun.impactEffect=originalImpact;UnityEngine.Object.Destroy(camObject);UnityEngine.Object.Destroy(target);
        movement.enabled=false;typeof(PlayerMovement).GetField("isGrounded",Private).SetValue(movement,true);var driver=player.GetComponent<CharacterAnimationDriver>();typeof(CharacterAnimationDriver).GetField("previousPosition",Private).SetValue(driver,player.transform.position);gun.currentAmmo=0;typeof(CharacterAnimationDriver).GetField("shotUntil",Private).SetValue(driver,-100f);
        typeof(CharacterAnimationDriver).GetMethod("LateUpdate",Private).Invoke(driver,null);Check((string)typeof(CharacterAnimationDriver).GetField("currentState",Private).GetValue(driver)=="Idle","Empty ammo still uses normal idle animation");
        Check(driver.animator.HasState(0,Animator.StringToHash("Jump"))&&driver.animator.HasState(0,Animator.StringToHash("CokeyRun")),"Jump and CokeyRun placeholder states exist");
        var boost=player.GetComponent<SpeedBoostAbility>();boost.Activate();Check(Mathf.Approximately(movement.MotionDelta,Time.deltaTime*2),"Cokey doubles movement simulation time, including ascent and gravity");
        driver.NotifyShot();typeof(CharacterAnimationDriver).GetMethod("LateUpdate",Private).Invoke(driver,null);Check(driver.animator.speed==1,"Shooting animation stays normal speed during Cokey");boost.Tick(100);movement.enabled=true;gun.Reload();
        var shop=UnityEngine.Object.FindFirstObjectByType<ShopManager>();PlayerParts.Instance.AddParts(10000);int credits=PlayerParts.Instance.currentParts;shop.BuyAmmo();Check(PlayerParts.Instance.currentParts==credits,"Full ammo cannot waste shop currency");gun.currentAmmo=0;shop.BuyAmmo();Check(gun.currentAmmo==Mathf.Min(shop.ammoPurchaseAmount,gun.maxAmmo)&&PlayerParts.Instance.currentParts==credits-shop.ammoPurchaseCost,"Shop buys ammo for the configured price");
        int before=PlayerParts.Instance.currentParts;gun.currentAmmo=0;PlayerParts.Instance.currentParts=0;shop.BuyAmmo();Check(gun.currentAmmo==0,"Ammo purchase rejects insufficient scrap");PlayerParts.Instance.currentParts=before;
        Check(!shop.AllUpgradesPurchased&&!shop.secretDungeonButton.activeSelf,"Secret dungeon is hidden before all upgrades");
        var wave=UnityEngine.Object.FindObjectsByType<WaveArea>(FindObjectsSortMode.None).First(w=>w.islandNumber==1);wave.waves=new[]{new WaveArea.IslandWave{enemyCount=1},new WaveArea.IslandWave{enemyCount=1}};wave.countdownSeconds=0;wave.spawnInterval=0;wave.intermissionSeconds=3;wave.StartWave();yield return .2;
        foreach(var h in UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Where(h=>h.name.EndsWith("(Clone)")))h.TakeDamage(99999);
        yield return .3;Check(wave.countdownText.text.Contains("Next wave in"),"Between-wave UI shows an actual countdown");
        for(int i=0;i<4;i++){shop.UpgradeHealth();shop.UpgradeFireRate();shop.UpgradeMaxAmmo();}
        Check(shop.AllUpgradesPurchased&&shop.secretDungeonButton.activeSelf,"All twelve upgrade purchases reveal dungeon button");int expectedHP=player.maxHealth,expectedAmmo=gun.maxAmmo;float expectedRate=gun.fireRate;
        shop.EnterSecretDungeon();yield return 1.5;
        Check(SceneManager.GetActiveScene().name=="DungeonLevel"&&Time.timeScale==1,"Secret button loads playable DungeonLevel");
        player=UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();gun=UnityEngine.Object.FindFirstObjectByType<Gun>();movement=player.GetComponent<PlayerMovement>();
        Check(player.maxHealth==expectedHP&&gun.maxAmmo==expectedAmmo&&gun.fireRate==expectedRate,"Dungeon carries purchased player upgrades");
        Check(!TutorialOverlay.Instance.IsOpen,"Dungeon does not repeat first-spawn tutorial");
        var encounter=UnityEngine.Object.FindFirstObjectByType<DungeonEncounter>();Check(encounter.boss.maxHealth==2000&&encounter.keyPickup.activeSelf==false,"Evil Kabu starts with configured health and key hidden");
        encounter.TryOpenDoor();Check(!encounter.DoorOpened&&!DungeonEncounter.IsCutsceneActive,"Door stays locked without boss key");
        // Observe a complete attack cycle while keeping the test player safe.
        player.maxHealth=10000;player.currentHealth=10000;var attacks=new HashSet<int>();bool circle=false,rectangle=false;float deadline=Time.time+20;
        while(attacks.Count<3&&Time.time<deadline){attacks.Add(encounter.LastAttack);attacks.Remove(-1);circle|=encounter.circleIndicator.gameObject.activeSelf;rectangle|=encounter.rectangleIndicator.gameObject.activeSelf;yield return .1;}
        Check(attacks.Count==3&&circle&&rectangle,"Rock, punch and belly attacks all run with red indicators");
        int hp=player.currentHealth;Vector3 center=player.transform.position;encounter.DamageCircle(center+Vector3.right*100,4,20);Check(player.currentHealth==hp,"Dodging outside red impact area avoids damage");encounter.DamageCircle(center,4,20);Check(player.currentHealth==hp-20,"Standing in red impact area takes damage");
        encounter.boss.TakeDamage(99999);yield return .2;Check(encounter.BossDefeated&&encounter.keyPickup.activeSelf&&!encounter.circleIndicator.gameObject.activeSelf&&!encounter.rectangleIndicator.gameObject.activeSelf,"Boss death stops attacks and drops the key");
        yield return 1.5;encounter.keyPickup.SendMessage("OnTriggerEnter",player.GetComponent<Collider>());Check(encounter.HasKey&&!encounter.keyPickup.activeSelf,"Key pickup is collected");
        encounter.heavenlyDoorMusic=AudioClip.Create("Heavenly door test",44100*8,1,44100,false);encounter.TryOpenDoor();Check(DungeonEncounter.IsCutsceneActive&&!movement.enabled,"Key starts door cutscene and locks player controls");
        hp=player.currentHealth;player.TakeDamage(100);Check(player.currentHealth==hp,"Door cinematic protects player from damage");
        deadline=Time.time+8;while(DungeonEncounter.IsCutsceneActive&&Time.time<deadline)yield return .1;
        Check(encounter.DoorOpened&&!DungeonEncounter.IsCutsceneActive&&movement.enabled&&Camera.main.targetTexture!=null,"Door rises, camera returns and controls restore");
        Check(!encounter.door.GetComponent<Collider>().enabled,"Unlocked doorway is traversable");
        // Dungeon death retries the same level with the entrance loadout.
        player.Die();yield return 1;Check(SceneManager.GetActiveScene().name=="DeathScene"&&GameSceneFlow.RetryScene=="DungeonLevel","Dungeon death remembers correct retry scene");
        UnityEngine.Object.FindFirstObjectByType<DeathButtons>().Retry();yield return 1;
        Check(SceneManager.GetActiveScene().name=="DungeonLevel"&&UnityEngine.Object.FindFirstObjectByType<PlayerHealth>().maxHealth==expectedHP,"Retry restarts dungeon with purchased upgrades");
        var boat=UnityEngine.Object.FindFirstObjectByType<ModelSceneLoader>();boat.SendMessage("OnTriggerEnter",UnityEngine.Object.FindFirstObjectByType<PlayerHealth>().GetComponent<Collider>());yield return 1;
        Check(SceneManager.GetActiveScene().name=="VictoryScene","Outside boat routes to VictoryScene");results.Add("COMPLETE");
    }
}

