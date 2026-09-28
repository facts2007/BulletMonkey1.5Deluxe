using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class FinalAnimationChecks
{
    static IEnumerator routine;static double until;static readonly List<string> results=new List<string>();
    public static string Report=>string.Join("\n",results);
    public static void Run(){EditorApplication.update-=Step;results.Clear();routine=Checks();until=0;EditorApplication.update+=Step;}
    static void Step(){if(EditorApplication.timeSinceStartup<until)return;try{if(routine.MoveNext()){until=EditorApplication.timeSinceStartup+Convert.ToDouble(routine.Current);return;}}catch(Exception e){results.Add("FAIL "+e);}EditorApplication.update-=Step;System.IO.Directory.CreateDirectory("Temp/CombatChecks");System.IO.File.WriteAllText("Temp/CombatChecks/final-animation-report.txt",Report);}
    static void Check(bool ok,string label){results.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(label);}
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static IEnumerator Checks()
    {
        Application.runInBackground=true;EditorApplication.isPaused=false;
        if(TutorialOverlay.Instance!=null&&TutorialOverlay.Instance.IsOpen)TutorialOverlay.Instance.Close();Time.timeScale=1;
        var hp=UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();var driver=hp.GetComponent<CharacterAnimationDriver>();var cheer=hp.GetComponent<PlayerCheer>();
        Check(cheer!=null && driver.animator.HasState(0,Animator.StringToHash("Cheer")),"Cheer component and animation wired");
        Check(driver.animator.HasState(0,Animator.StringToHash("CokeyRun"))&&driver.animator.HasState(0,Animator.StringToHash("Jump")),"Run and jump states ready");
        cheer.Begin(false);yield return .25;
        Check(cheer.IsCheering&&driver.animator.GetCurrentAnimatorStateInfo(0).IsName("Cheer"),"Manual cheer plays the new clip");
        Check(GameAudio.Instance.cheerPopup.picture.gameObject.activeSelf,"Cheer shows monkey picture");
        yield return 1.7;Check(!cheer.IsCheering&&hp.GetComponent<PlayerMovement>().enabled,"Manual cheer restores movement");
        var camera=Camera.main;Vector3 position=camera.transform.localPosition;Quaternion rotation=camera.transform.localRotation;
        cheer.Begin(true);yield return .8;
        Check(PlayerCheer.IsCutsceneActive&&PlayerCheer.ActiveCamera!=null&&!camera.enabled&&Vector3.Distance(PlayerCheer.ActiveCamera.transform.position,camera.transform.position)>.1f,"Boss cheer pans camera");
        int health=hp.currentHealth;hp.TakeDamage(10);Check(hp.currentHealth==health,"Boss cheer protects player during camera shot");
        yield return 2.4;Check(!PlayerCheer.IsCutsceneActive&&Vector3.Distance(camera.transform.localPosition,position)<.01f&&Quaternion.Angle(camera.transform.localRotation,rotation)<.1f,"Boss cheer returns camera and controls");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/SceneStuff/HealingBananaPickup.prefab");
        hp.TakeDamage(30);health=hp.currentHealth;
        var banana=UnityEngine.Object.Instantiate(prefab,hp.transform.position+Vector3.right*5,Quaternion.identity);banana.SendMessage("OnTriggerEnter",hp.GetComponent<Collider>());
        Check(hp.currentHealth==health+20,"Normal banana heals exactly 20 HP");yield return .1;
        hp.Heal(10000);Check(hp.currentHealth==hp.maxHealth,"Healing caps at maximum health");
        var enemyPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/SceneStuff/EnemyMelee.prefab");var enemy=UnityEngine.Object.Instantiate(enemyPrefab,hp.transform.position+Vector3.right*15,Quaternion.identity);enemy.GetComponent<Enemy>().healingBananaDropChance=1;enemy.GetComponent<EnemyHealth>().TakeDamage(10000);
        Check(UnityEngine.Object.FindObjectsByType<BananaPickup>(FindObjectsSortMode.None).Any(),"Enemy death can drop healing banana");
        int[] counts=new int[3];for(int i=0;i<10000;i++)counts[DungeonEncounter.ChooseAttack((i+.5f)/10000)]++;
        Check(counts[0]==3300&&counts[1]==3400&&counts[2]==3300,"Attack weights are 33 / 34 / 33 percent");
        GameSceneFlow.Load("DungeonLevel");yield return 1;
        var e=UnityEngine.Object.FindFirstObjectByType<DungeonEncounter>();e.StopAllCoroutines();hp=UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();hp.maxHealth=hp.currentHealth=10000;
        Check(e.boss.maxHealth==10000&&e.movesPerImp==6,"Boss retains 10k HP and imp interval six");
        Check(e.visual.GetComponentInChildren<SkinnedMeshRenderer>()!=null&&e.animator.HasState(0,Animator.StringToHash("Stomp")),"Final Evil Kabu model and stomp animation wired");
        Check(e.recoverySeconds<1.8f&&e.punchDamage>30,"Boss difficulty increased");
        e.windupSeconds=.1f;var invoke=typeof(DungeonEncounter).GetMethod("ThrowImp",Private);e.StartCoroutine((IEnumerator)invoke.Invoke(e,null));hp.transform.position+=Vector3.right*15;
        yield return 1.8;var imp=GameObject.Find("Kabu thrown imp — bonus ammo");Check(imp!=null&&imp.GetComponent<Enemy>().minAmmoAmount==75,"Imp reinforcement retains bonus ammo");
        e.boss.TakeDamage(100000);yield return .3;Check(e.BossDefeated&&PlayerCheer.IsCutsceneActive,"Evil Kabu defeat triggers boss celebration");
        yield return 3;Check(!PlayerCheer.IsCutsceneActive&&hp.GetComponent<PlayerMovement>().enabled,"Dungeon celebration finishes and restores control");
        results.Add("COMPLETE");
    }
}
