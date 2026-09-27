using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class FusionExpansionChecks
{
    static IEnumerator routine;static double until;static readonly List<string> results=new List<string>();
    public static string Report=>string.Join("\n",results);
    public static void Run(){results.Clear();routine=Checks();until=0;EditorApplication.update+=Step;}
    static void Step(){if(EditorApplication.timeSinceStartup<until)return;try{if(routine.MoveNext()){until=EditorApplication.timeSinceStartup+Convert.ToDouble(routine.Current);return;}}catch(Exception e){results.Add("FAIL "+e);}EditorApplication.update-=Step;System.IO.File.WriteAllText("Temp/CombatChecks/fusion-expansion-report.txt",Report);}
    static void Check(bool ok,string label){results.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(label);}
    static AudioSource Source(GameAudio audio,string field)=>(AudioSource)typeof(GameAudio).GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(audio);
    static IEnumerator Checks()
    {
        Application.runInBackground=true;
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();var movement=player.GetComponent<PlayerMovement>();movement.enabled=false;
        Check(Camera.main.targetTexture.height==400,"Gameplay retro camera renders at 400 lines");
        var rescue=player.GetComponent<PlayerUnstuck>();var ammo=UnityEngine.Object.FindFirstObjectByType<Gun>().ammoText;
        Check(rescue.prompt.text=="[T] unstuck?"&&rescue.prompt.gameObject.activeSelf&&rescue.prompt.transform.parent==ammo.transform.parent&&rescue.prompt.rectTransform.anchoredPosition.y>ammo.rectTransform.anchoredPosition.y,"Small unstuck hint appears above ammo");
        var audio=GameAudio.Instance;var main=Source(audio,"mainMusicSource");var mini=Source(audio,"minibossSource");
        var clip=AudioClip.Create("Verification music",44100*30,1,44100,false);main.clip=clip;main.Play();main.time=7;yield return .2;
        Check(!audio.BeginMinibossMusic(null)&&main.isPlaying,"Empty miniboss slot leaves normal music playing");
        var wave=UnityEngine.Object.FindObjectsByType<WaveArea>(FindObjectsSortMode.None).First(w=>w.islandNumber==3);wave.minibossTheme=clip;wave.waves=new[]{new WaveArea.IslandWave{bossWave=true}};wave.countdownSeconds=0;wave.spawnInterval=0;wave.Unlock();wave.StartWave();yield return .2;
        float held=main.time;Check(!main.isPlaying&&mini.isPlaying&&mini.loop,"Island 3 boss wave pauses normal track and loops miniboss theme");
        audio.SetPaused(true);yield return .2;Check(!mini.isPlaying&&!main.isPlaying,"Pause suspends miniboss without resuming normal music");audio.SetPaused(false);yield return .2;
        Check(mini.isPlaying&&!main.isPlaying&&Mathf.Abs(main.time-held)<.1f,"Resume keeps normal track paused at its saved position");
        foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Where(h=>h.name.Contains("Giant")))enemy.TakeDamage(99999);
        yield return .5;Check(wave.waveComplete&&main.isPlaying&&!mini.isPlaying&&main.time>=held&&main.time<held+1,"Miniboss defeat resumes normal music from saved position, not zero");
        var encounter=UnityEngine.Object.FindFirstObjectByType<BossFusionEncounter>();Check(encounter.impCount==120&&encounter.impSpawns.Length==5,"Fusion configured for 120 imps from five angles");
        encounter.requiredIsland.waveComplete=true;var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=encounter.transform.position+Vector3.up*2;cc.enabled=true;movement.enabled=true;encounter.TryEnter(cc);
        float end=Time.time+50,peakProgress=0;int maxRunners=0,maxCloudParticles=0;
        while(BossFusionEncounter.IsCutsceneActive&&Time.time<end){maxRunners=Mathf.Max(maxRunners,UnityEngine.Object.FindObjectsByType<FusionImpRunner>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length);var cloud=UnityEngine.Object.FindFirstObjectByType<FusionCloud>();if(cloud!=null){peakProgress=Mathf.Max(peakProgress,cloud.Progress);maxCloudParticles=Mathf.Max(maxCloudParticles,cloud.particles.particleCount);}yield return .1;}
        Check(!BossFusionEncounter.IsCutsceneActive&&movement.enabled,"Larger fusion finishes and restores player controls");
        Check(maxRunners==120,"All 120 imps were spawned");
        Check(peakProgress>.8f&&maxCloudParticles>0&&maxCloudParticles<=100,"Merge fog grows with arrivals and stays within particle budget");
        Check(encounter.Boss!=null&&encounter.Boss.transform.localScale==Vector3.one*3&&encounter.Boss.maxHealth==750,"Imptron is three times larger and keeps 750 HP");
        Check(Camera.main.targetTexture.height==400,"Camera returns with sharper retro rendering intact");
        encounter.Boss.TakeDamage(750);yield return .2;Check(encounter.Defeated&&!encounter.finalMistDoor.activeSelf,"Larger boss still unlocks final door on death");results.Add("COMPLETE");
    }
}
