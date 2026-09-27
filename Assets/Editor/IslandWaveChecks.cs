using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class IslandWaveChecks
{
    private static IEnumerator routine;
    private static double until;
    private static readonly List<string> results = new List<string>();
    public static string Report => string.Join("\n",results);
    public static void Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Enter Play mode.");
        results.Clear();routine=Checks();until=0;EditorApplication.update+=Step;
    }
    private static void Step()
    {
        if(Time.time<until)return;
        try
        {
            if(routine.MoveNext()){until=Time.time+Convert.ToDouble(routine.Current);return;}
        }
        catch(Exception ex){results.Add("FAIL "+ex);}
        EditorApplication.update-=Step;
        System.IO.Directory.CreateDirectory("Temp/CombatChecks");
        System.IO.File.WriteAllText("Temp/CombatChecks/island-report.txt",Report);
        routine=null;
    }
    private static void Check(bool ok,string label)
    {
        results.Add((ok?"PASS ":"FAIL ")+label);
        if(!ok)throw new Exception(label);
    }
    private static IEnumerator Checks()
    {
        Application.runInBackground=true;
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
        player.GetComponent<PlayerMovement>().enabled=false;
        player.GetComponent<MouseLook>().enabled=false;
        player.GetComponentInChildren<Gun>().enabled=false;
        var areas=UnityEngine.Object.FindObjectsByType<WaveArea>(FindObjectsSortMode.None).OrderBy(a=>a.name).ToArray();
        Check(areas.Length==5,"Five combat islands configured");
        Check(areas.All(a=>!a.waveStarted && a.pathBlocker.activeSelf),"No wave autostarts; all exit gates closed");
        Check(areas[0].IsUnlocked && areas.Skip(1).All(a=>!a.IsUnlocked),"Only first island entry unlocked");
        foreach(var a in areas){a.waves=new[]{new WaveArea.IslandWave{enemyCount=2},new WaveArea.IslandWave{enemyCount=1}};a.countdownSeconds=1;a.spawnInterval=.1f;a.intermissionSeconds=0;}
        foreach(var e in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Freeze(e);
        Collider playerCollider=player.GetComponent<Collider>();
        areas[1].TryEnter(playerCollider);
        Check(!areas[1].waveStarted,"Locked island ignores player entry");
        var dummy=new GameObject("Non-player entry check");var other=dummy.AddComponent<BoxCollider>();
        areas[0].TryEnter(other);
        Check(!areas[0].waveStarted,"Non-player collider cannot start wave");
        UnityEngine.Object.Destroy(dummy);
        var controller=player.GetComponent<CharacterController>();
        for(int i=0;i<5;i++)
        {
            var a=areas[i];
            controller.enabled=false;player.transform.position=a.entryTrigger.bounds.center-Vector3.up*2;controller.enabled=true;
            Physics.SyncTransforms();controller.Move(Vector3.down*.05f);
            yield return .4;
            Check(a.waveStarted && a.CurrentWave==1,"Island "+(i+1)+" physical entry starts countdown");
            Check(a.EnemiesRemaining==0 && a.pathBlocker.activeSelf,"Countdown delays spawn and keeps mist closed");
            yield return 1.2;
            var enemies=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Where(e=>e.name.Contains("Clone")&&!e.GetComponent<EnemyHealth>().IsDead).ToArray();
            foreach(var e in enemies)Freeze(e);
            Check(enemies.Length==2 && a.EnemiesRemaining==2,"Island "+(i+1)+" spawns configured first-wave count");
            if(i==0)
            {
                var unrelated=new GameObject("Unrelated enemy");var health=unrelated.AddComponent<EnemyHealth>();var enemy=unrelated.AddComponent<Enemy>();enemy.Explode();
                yield return .1;
                Check(a.EnemiesRemaining==2,"Unrelated enemy death cannot clear island");
            }
            foreach(var e in enemies)e.GetComponent<EnemyHealth>().TakeDamage(9999);
            yield return .2;
            Check(!a.waveComplete && a.pathBlocker.activeSelf,"Mist stays closed between waves");
            yield return 1.2;
            enemies=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Where(e=>e.name.Contains("Clone")&&!e.GetComponent<EnemyHealth>().IsDead).ToArray();
            foreach(var e in enemies)Freeze(e);
            Check(a.CurrentWave==2 && enemies.Length==1,"Configured second wave starts after first is cleared");
            enemies[0].GetComponent<EnemyHealth>().TakeDamage(9999);
            yield return .3;
            Check(a.waveComplete && !a.pathBlocker.activeSelf,"Island "+(i+1)+" final kill opens mist gate");
            if(i<4)Check(areas[i+1].IsUnlocked,"Next island unlocked after completion");
            a.TryEnter(playerCollider);Check(a.waveComplete,"Cleared island does not restart");
        }
        var audio=GameAudio.Instance;
        var clip=AudioClip.Create("Test impact",2205,1,22050,false);
        var field=typeof(GameAudio).GetField("nextVoice",BindingFlags.NonPublic|BindingFlags.Instance);
        var victim=new GameObject("Audio routing victim");var hp=victim.AddComponent<EnemyHealth>();victim.AddComponent<Enemy>();
        audio.enemyDamaged=null;audio.enemyStompDamaged=clip;audio.enemyStomped=clip;
        int before=(int)field.GetValue(audio);hp.TakeDamage(10);
        Check((int)field.GetValue(audio)==before,"Bullet damage never uses either stomp sound");
        audio.enemyDamaged=clip;hp.TakeDamage(10);
        Check((int)field.GetValue(audio)==(before+1)%24,"Bullet damage uses its own clip");
        audio.enemyDamaged=null;hp.TakeDamage(10,true);
        Check((int)field.GetValue(audio)==(before+2)%24,"Nonlethal stomp uses stomp damage clip");
        hp.TakeDamage(999,true);
        Check((int)field.GetValue(audio)==(before+3)%24,"Lethal stomp plays only final stomp clip");
        results.Add("COMPLETE");
    }
    private static void Freeze(Enemy e)
    {
        foreach(var b in e.GetComponents<MonoBehaviour>())if(b is RangedEnemy || b is MeleeEnemy || b is MeleeAttack || b is DamageOnTouch)b.enabled=false;
        var agent=e.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent!=null)agent.enabled=false;
        e.dropChance=0;e.ammoDropChance=0;e.superBananaDropChance=0;e.coceyBananaDropChance=0;
    }
}
