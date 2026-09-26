using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Run in Play mode; all spawned test objects are discarded when Play mode stops.</summary>
public static class CombatPresentationChecks
{
    private static IEnumerator routine;
    private static double resumeAt;
    private static readonly List<string> results = new List<string>();
    public static string Report => string.Join("\n", results);

    [MenuItem("Tools/BulletMonkey/Run combat checks (Play mode)")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
        if (routine != null) throw new InvalidOperationException("Checks already running.");
        results.Clear();
        routine = CheckGameplay();
        resumeAt = 0;
        EditorApplication.update += Step;
    }

    private static void Step()
    {
        if (EditorApplication.timeSinceStartup < resumeAt) return;
        try
        {
            if (!EditorApplication.isPlaying || !routine.MoveNext()) { Finish(); return; }
            resumeAt = EditorApplication.timeSinceStartup + (routine.Current is float delay ? delay : 0.05f);
        }
        catch (Exception exception)
        {
            results.Add("FAIL: " + exception);
            Finish();
        }
    }

    private static void Finish()
    {
        EditorApplication.update -= Step;
        routine = null;
        Directory.CreateDirectory("Temp/CombatChecks");
        File.WriteAllText("Temp/CombatChecks/report.txt", Report);
        Debug.Log("Combat checks finished:\n" + Report);
    }

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        results.Add("PASS: " + description);
    }

    private static IEnumerator CheckGameplay()
    {
        Application.runInBackground = true;
        foreach (WaveArea wave in UnityEngine.Object.FindObjectsByType<WaveArea>(FindObjectsSortMode.None))
        { wave.StopAllCoroutines(); wave.enabled = false; }
        foreach (Enemy enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) FreezeEnemy(enemy.gameObject);
        foreach (Projectile projectile in UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(projectile.gameObject);
        SuperShootAbility ability = UnityEngine.Object.FindFirstObjectByType<SuperShootAbility>();
        Gun gun = ability.gun;
        PlayerMovement movement = ability.GetComponent<PlayerMovement>();
        movement.enabled = false;
        ability.GetComponent<MouseLook>().enabled = false;
        ability.enabled = false;
        gun.enabled = false;
        Check(ability.TryCollectBanana(), "First banana is stored");
        Check(!ability.TryCollectBanana(), "Second banana cannot overwrite stored use");
        ability.Tick(true, 0.75f);
        Check(Mathf.Approximately(ability.ChargeFraction, 0.5f), "1.5 second charge reaches halfway at 0.75 seconds");
        ability.Tick(false, 0.01f);
        Check(ability.HasBanana && ability.ChargeFraction == 0, "Early release cancels charge without consuming banana");

        foreach (Bullet bullet in UnityEngine.Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(bullet.gameObject);
        yield return 0.1f;
        int ammo = gun.currentAmmo;
        ability.Tick(true, 1.5f);
        Check(ability.IsSuperShooting && !ability.HasBanana, "Full charge consumes one banana and starts stream");
        float streamStart = Time.time;
        gun.enabled = true;
        yield return 0.5f;
        gun.enabled = false;
        // The existing bullet prefab also has a Bullet script on its light child.
        int bullets = UnityEngine.Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None).Count(b => b.transform.parent == null);
        float streamElapsed = Time.time - streamStart;
        Check(Mathf.Abs(bullets - streamElapsed * ability.shotsPerSecond) <= 5f,
            "Continuous stream fires near 40 shots/sec (" + bullets + " shots in " + streamElapsed.ToString("0.00") + " game seconds)");
        Check(gun.currentAmmo == ammo, "Super stream preserves regular ammo");
        Check(ability.GetComponent<CharacterAnimationDriver>().animator.GetCurrentAnimatorStateInfo(0).IsName("SuperShoot"), "Super stream uses super animation");
        ability.Tick(false, 5f);
        Check(!ability.IsSuperShooting && !ability.HasBanana, "Stream expires after 5 seconds");

        ability.enabled = true;
        GameObject bananaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/SceneStuff/SuperBananaPickup.prefab");
        GameObject banana = UnityEngine.Object.Instantiate(bananaPrefab, ability.transform.position + Vector3.up * 0.5f, Quaternion.identity);
        yield return 0.25f;
        Check(ability.HasBanana && banana == null, "Physical trigger collects banana and removes pickup");

        GameObject meleePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/SceneStuff/EnemyMelee.prefab");
        GameObject victim = UnityEngine.Object.Instantiate(meleePrefab, ability.transform.position + Vector3.forward * 4 + Vector3.up * 0.5f, Quaternion.identity);
        FreezeEnemy(victim);
        Enemy enemyComponent = victim.GetComponent<Enemy>();
        EnemyHealth health = victim.GetComponent<EnemyHealth>();
        enemyComponent.partsDropPrefab = enemyComponent.ammoDropPrefab = null;
        enemyComponent.superBananaDropChance = 1f;
        int parts = PlayerParts.Instance.currentParts;
        int deathEvents = 0;
        Action onDeath = () => deathEvents++;
        Enemy.OnEnemyDied += onDeath;
        try
        {
            enemyComponent.Stomp(25);
            Check(health.currentHealth == 75 && !health.IsDead, "Nonlethal stomp keeps MiniDroid alive");
            Vector3 before = enemyComponent.visualRoot.localScale;
            enemyComponent.Stomp(75);
            health.TakeDamage(100); health.Die(); enemyComponent.Explode();
            Check(health.IsDead && !victim.GetComponent<Collider>().enabled, "Lethal stomp disables collisions");
            Check(deathEvents == 1 && PlayerParts.Instance.currentParts - parts == health.pointValue, "Death event and points awarded exactly once");
            Check(UnityEngine.Object.FindObjectsByType<SuperBananaPickup>(FindObjectsSortMode.None).Length == 1, "Guaranteed test drop creates one super banana");
            yield return 0.3f;
            Check(victim != null && enemyComponent.visualRoot.localScale.y <= before.y * 0.09f, "MiniDroid flattens to pancake and stays visible");
        }
        finally { Enemy.OnEnemyDied -= onDeath; }

        // Check actual descending CharacterController contact, not just the damage method.
        GameObject contactVictim = UnityEngine.Object.Instantiate(meleePrefab, ability.transform.position + Vector3.right * 3 + Vector3.up * 0.5f, Quaternion.identity);
        FreezeEnemy(contactVictim);
        CharacterController character = ability.GetComponent<CharacterController>();
        character.enabled = false;
        ability.transform.position = contactVictim.transform.position + Vector3.up * 3f;
        character.enabled = true;
        typeof(PlayerMovement).GetField("velocity", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, Vector3.zero);
        movement.enabled = true;
        yield return 1f;
        Check(contactVictim.GetComponent<EnemyHealth>().currentHealth == 75, "Landing from above deals one stomp, without repeated standing damage");
        movement.enabled = false;

        PauseManager pause = UnityEngine.Object.FindFirstObjectByType<PauseManager>();
        float oldMusic = pause.musicSlider.value, oldSfx = pause.sfxSlider.value;
        pause.Pause(); pause.OpenSettings();
        Check(Time.timeScale == 0 && pause.settingsPanel.activeInHierarchy, "Pause opens audio settings while gameplay is frozen");
        pause.musicSlider.value = 0.25f; pause.sfxSlider.value = 0.4f;
        Check(Mathf.Approximately(pause.gameAudio.MusicVolume, 0.25f) && Mathf.Approximately(pause.gameAudio.SfxVolume, 0.4f), "Independent music and SFX sliders update audio");
        Check(Mathf.Approximately(PlayerPrefs.GetFloat("BM.MusicVolume"), 0.25f) && Mathf.Approximately(PlayerPrefs.GetFloat("BM.SfxVolume"), 0.4f), "Volume settings persist");
        pause.musicSlider.value = oldMusic; pause.sfxSlider.value = oldSfx;
        pause.CloseSettings(); pause.Resume();
        Check(Time.timeScale == 1 && !pause.pausePanel.activeSelf, "Back and Resume restore gameplay");
        results.Add("ALL CHECKS PASSED. Stop Play mode to discard the test setup.");
    }

    private static void FreezeEnemy(GameObject go)
    {
        foreach (MonoBehaviour component in go.GetComponents<MonoBehaviour>())
            if (component is MeleeEnemy || component is MeleeAttack || component is RangedEnemy || component is DamageOnTouch) component.enabled = false;
        var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;
    }
}
