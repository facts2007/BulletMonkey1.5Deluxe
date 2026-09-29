using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class BossDeathVfxChecks
{
 static IEnumerator routine;static double until;static readonly List<string> results=new List<string>();
 public static string Report=>string.Join("\n",results);
 public static void Run(){EditorApplication.update-=Step;results.Clear();routine=Checks();until=0;EditorApplication.update+=Step;}
 static void Step(){if(EditorApplication.timeSinceStartup<until)return;try{if(routine.MoveNext()){until=EditorApplication.timeSinceStartup+Convert.ToDouble(routine.Current);return;}}catch(Exception e){results.Add("FAIL "+e);}EditorApplication.update-=Step;System.IO.File.WriteAllText("Temp/CombatChecks/boss-death-vfx-report.txt",Report);}
 static void Check(bool ok,string label){results.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(label);}
 static IEnumerator Checks()
 {
  Application.runInBackground=true;EditorApplication.isPaused=false;if(TutorialOverlay.Instance!=null&&TutorialOverlay.Instance.IsOpen)TutorialOverlay.Instance.Close();Time.timeScale=1;
  var player=UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();player.currentHealth=player.maxHealth=10000;
  var e=UnityEngine.Object.FindFirstObjectByType<BossFusionEncounter>();e.impCount=8;e.impSpeed=100;e.spawnSpacing=0;e.panSeconds=.2f;e.revealSeconds=.2f;e.fusionRoar=null;e.deathBuildSeconds=.8f;e.deathExplosionHold=.7f;
  Check(e.StartFromTestCube(player),"Test cube starts fusion");float deadline=Time.time+35;
  while(BossFusionEncounter.IsCutsceneActive && Time.time<deadline)yield return .2;
  Check(e.Boss!=null&&!BossFusionEncounter.IsCutsceneActive,"Fusion finishes normally");e.Boss.TakeDamage(100000);yield return .2;
  Check(BossFusionEncounter.IsCutsceneActive&&!player.GetComponent<PlayerCheer>().IsCheering,"Death cinematic starts before cheer");
  int hp=player.currentHealth;player.TakeDamage(999);Check(player.currentHealth==hp,"Player protected during explosion shot");
  Check(GameObject.Find("Defeated boss cinematic visual")!=null,"Boss visual remains for buildup");yield return 1;
  Check(GameObject.Find("Boss explosion flash")!=null,"Explosion produces brief flash");
  Check(GameObject.Find("Defeated boss cinematic visual")==null,"Boss visual disappears in explosion");yield return 1;
  Check(!BossFusionEncounter.IsCutsceneActive&&player.GetComponent<PlayerCheer>().IsCheering,"Player cheers after explosion completes");
  Check(e.finalMistDoor==null||!e.finalMistDoor.activeSelf,"Exit opens after death cinematic");yield return 3;
  Check(player.GetComponent<PlayerMovement>().enabled&&!PlayerCheer.IsCutsceneActive,"Camera and controls return after cheer");
  Check(GameObject.Find("Boss explosion flash")==null,"Flash is cleaned up");results.Add("COMPLETE");
 }
}
