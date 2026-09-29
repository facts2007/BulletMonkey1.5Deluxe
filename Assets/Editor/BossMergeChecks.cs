using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class BossMergeChecks
{
 static IEnumerator routine;static double until;static readonly List<string> results=new List<string>();
 public static string Report=>string.Join("\n",results);
 const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
 public static void Run(){EditorApplication.update-=Step;results.Clear();routine=Checks();until=0;EditorApplication.update+=Step;}
 static void Step(){if(EditorApplication.timeSinceStartup<until)return;try{if(routine.MoveNext()){until=EditorApplication.timeSinceStartup+Convert.ToDouble(routine.Current);return;}}catch(Exception e){results.Add("FAIL "+e);}EditorApplication.update-=Step;System.IO.Directory.CreateDirectory("Temp/CombatChecks");System.IO.File.WriteAllText("Temp/CombatChecks/boss-merge-report.txt",Report);}
 static void Check(bool ok,string label){results.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(label);}
 static void Attack(BossController boss,BossAttack attack){boss.StartCoroutine((IEnumerator)typeof(BossController).GetMethod("PerformAttack",Private).Invoke(boss,new object[]{attack}));}
 static IEnumerator Checks()
 {
  Application.runInBackground=true;EditorApplication.isPaused=false;if(TutorialOverlay.Instance!=null&&TutorialOverlay.Instance.IsOpen)TutorialOverlay.Instance.Close();Time.timeScale=1;
  var player=UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();player.maxHealth=player.currentHealth=10000;
  var wave=UnityEngine.Object.FindObjectsByType<WaveArea>(FindObjectsSortMode.None).First(w=>w.islandNumber==3);
  Check(wave.minibossImpInterval==15,"Island 3 interval is 15 seconds");wave.waves=new[]{new WaveArea.IslandWave{bossWave=true,enemyCount=1}};wave.countdownSeconds=0;wave.spawnInterval=0;wave.Unlock();wave.StartWave();
  yield return 13;Check(GameObject.Find("Island 3 ammo reinforcement")==null,"No early miniboss reinforcement");yield return 3;
  var reinforcement=GameObject.Find("Island 3 ammo reinforcement");Check(reinforcement!=null,"Miniboss spawns reinforcement after 15 seconds");var drop=reinforcement.GetComponent<Enemy>();Check(drop.ammoDropChance==1&&drop.minAmmoAmount==30&&drop.maxAmmoAmount==30,"Miniboss reinforcement guarantees 30 ammo");wave.StopAllCoroutines();
  foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(enemy.gameObject);
  var fusion=UnityEngine.Object.FindFirstObjectByType<BossFusionEncounter>();var bossObject=UnityEngine.Object.Instantiate(fusion.bossPrefab,fusion.mergePoint.position,Quaternion.identity);var boss=bossObject.GetComponent<BossController>();boss.cinematicEnabled=false;boss.timeBetweenAttacks=999;
  Check(boss!=null&&boss.attacks.Length==3,"Fusion boss uses classmate controller with three attacks");
  var movement=player.GetComponent<PlayerMovement>();var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=fusion.mergePoint.position+Vector3.right*20;cc.enabled=true;movement.enabled=false;
  yield return .3;
  var slam=boss.attacks.First(a=>a.effect==BossAttackEffect.SlamImps);slam.minTimer=slam.maxTimer=.1f;Attack(boss,slam);yield return 1.3;
  var imps=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Where(e=>e.name.StartsWith("Boss supply imp")).ToArray();Check(imps.Length==3,"Slam launches exactly three walking imps");Check(imps.All(e=>e.minAmmoAmount==30&&e.maxAmmoAmount==30&&e.ammoDropChance==1),"All slam imps guarantee 30 ammo");
  foreach(var imp in imps)imp.GetComponent<EnemyHealth>().TakeDamage(100000);yield return .3;
  Check(UnityEngine.Object.FindObjectsByType<AmmoPickup>(FindObjectsSortMode.None).Count(a=>a.amount==30)>=3,"Slam imp deaths produce three ammo pickups");
  var scatter=boss.attacks.First(a=>a.effect==BossAttackEffect.ScatterImps);scatter.minTimer=scatter.maxTimer=.1f;Attack(boss,scatter);yield return .4;
  var balls=GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Explosive imp airborne").ToArray();Check(balls.Length==6,"Scattershot launches six explosive imps");Quaternion spin=balls[0].rotation;yield return .2;Check(Quaternion.Angle(spin,balls[0].rotation)>5,"Thrown imps spin in flight");yield return 1.6;Check(!GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name=="Explosive imp airborne"),"Scatter imps explode and disappear at landing");
  var push=boss.attacks.First(a=>a.effect==BossAttackEffect.Push);push.minTimer=push.maxTimer=.1f;movement.enabled=true;int hp=player.currentHealth;Vector3 before=player.transform.position;Attack(boss,push);yield return .7;
  Check(player.currentHealth==hp-5,"Push deals only five damage");Check(Vector3.Distance(before,player.transform.position)>1,"Push moves player backward");movement.enabled=false;
  boss.StartCoroutine((IEnumerator)typeof(BossController).GetMethod("PerformCinematicImpact",Private).Invoke(boss,null));yield return .2;Check(GameObject.Find("Explosive imp airborne")!=null,"Independent rain launches an explosive imp");
  boss.GetComponent<EnemyHealth>().TakeDamage(100000);yield return .3;Check(GameObject.Find("Explosive imp airborne")==null,"Boss death clears airborne projectiles");
  var boat=UnityEngine.Object.FindFirstObjectByType<ModelSceneLoader>();var water=UnityEngine.Object.FindFirstObjectByType<WaterDrowningTrigger>();var bounds=boat.GetComponent<Collider>().bounds;cc.enabled=false;player.transform.position=new Vector3(bounds.center.x,water.GetComponent<Collider>().bounds.max.y-.5f,bounds.center.z);cc.enabled=true;Physics.SyncTransforms();
  Check(boat.GetComponent<Collider>().bounds.Intersects(cc.bounds)&&water.GetComponent<Collider>().bounds.Intersects(cc.bounds),"Reproduced overlapping boat and drowning volumes");
  water.sequence.Begin(player);yield return .8;Check(SceneManager.GetActiveScene().name=="VictoryScene","Boat wins over water even when water is processed first");
  Check(GameSceneFlow.EndingNumber==1,"Regular boat keeps ending 1");results.Add("COMPLETE");
 }
}
