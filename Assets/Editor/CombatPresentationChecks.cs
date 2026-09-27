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

    [MenuItem("Tools/BulletMonkey/Run camera feedback checks (Play mode)")]
    public static void RunCameraFeedback()
    {
        if (!EditorApplication.isPlaying || routine != null) throw new InvalidOperationException("Enter Play mode and finish other checks first.");
        results.Clear();
        routine = CheckCameraFeedback();
        resumeAt = 0;
        EditorApplication.update += Step;
    }

    private static IEnumerator CheckCameraFeedback()
    {
        Application.runInBackground = true;
        foreach (WaveArea wave in UnityEngine.Object.FindObjectsByType<WaveArea>(FindObjectsSortMode.None))
        { wave.StopAllCoroutines(); wave.enabled = false; }
        foreach (Enemy enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) FreezeEnemy(enemy.gameObject);
        var ability = UnityEngine.Object.FindFirstObjectByType<SuperShootAbility>();
        ability.GetComponent<PlayerMovement>().enabled = false;
        ability.GetComponent<MouseLook>().enabled = false;
        ability.gun.enabled = false;
        ability.enabled = false;
        var shake = ability.gun.cameraShake;
        shake.enabled = false; shake.enabled = true;
        Camera camera = ability.gun.aimCamera;
        float originalFov = camera.fieldOfView;
        Vector3 originalPosition = camera.transform.localPosition;
        Quaternion originalRotation = camera.transform.localRotation;
        ability.TryCollectBanana(); ability.Tick(true, 1.2f);
        yield return 0.5f;
        Check(camera.fieldOfView < originalFov - 2f && camera.fieldOfView >= originalFov - 5.1f, "Charging smoothly zooms in within the five-degree limit");
        ability.CancelCharge();
        yield return 0.8f;
        Check(Mathf.Abs(camera.fieldOfView - originalFov) < 0.05f, "Cancelling charge restores normal zoom");
        ability.Tick(true, 1.2f);
        yield return 0.3f;
        ability.Tick(true, 0.3f);
        yield return 0.8f;
        Check(ability.IsSuperShooting && Mathf.Abs(camera.fieldOfView - originalFov) < 0.05f, "Completing charge restores normal zoom for shooting");
        ability.Tick(false, 5f);

        var boost = ability.GetComponent<SpeedBoostAbility>();
        boost.Activate();
        yield return 0.3f;
        Vector3 first = camera.transform.localPosition;
        yield return 0.12f;
        Check(Vector3.Distance(first, camera.transform.localPosition) > 0.0001f, "Cocey produces continuous gentle camera motion");
        Check(Vector3.Distance(originalPosition, camera.transform.localPosition) < 0.04f, "Cocey shake stays subtle");
        boost.Tick(10f);
        yield return 0.3f;
        Check(Vector3.Distance(originalPosition, camera.transform.localPosition) < 0.0001f && Quaternion.Angle(originalRotation, camera.transform.localRotation) < 0.01f,
            "Cocey expiry restores camera pose without drift");

        camera.transform.localRotation = Quaternion.Euler(-70f, 0f, 0f);
        var oldBullets = new HashSet<int>(UnityEngine.Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None).Select(b => b.GetInstanceID()));
        var shoot = typeof(Gun).GetMethod("Shoot", BindingFlags.Instance | BindingFlags.NonPublic);
        int ammo = ability.gun.currentAmmo;
        for (int i = 0; i < 20; i++) shoot.Invoke(ability.gun, new object[] { true });
        var directions = UnityEngine.Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None)
            .Where(b => b.transform.parent == null && !oldBullets.Contains(b.GetInstanceID())).Select(b => b.transform.forward).ToArray();
        Check(directions.Length == 20 && directions.Any(d => Vector3.Angle(d, directions[0]) > 1f), "Super shots vary independently even within one frame");
        Check(ability.gun.currentAmmo == ammo, "Unpredictable super shots still preserve regular ammo");
        var recoil = (Vector2)typeof(ShootCameraShake).GetField("recoil", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(shake);
        Check(Mathf.Abs(recoil.x) <= 18f && Mathf.Abs(recoil.y) <= 7.2f, "Stronger recoil stays bounded");
        shake.enabled = false;
        Check(Vector3.Distance(camera.transform.localPosition, originalPosition) < 0.0001f && Mathf.Abs(camera.fieldOfView - originalFov) < 0.001f,
            "Disabling camera feedback cleans up position and zoom");
        results.Add("ALL CAMERA FEEDBACK CHECKS PASSED. Stop Play mode to discard the test setup.");
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
        int bananasBefore = UnityEngine.Object.FindObjectsByType<SuperBananaPickup>(FindObjectsSortMode.None).Length;
        GameObject victim = UnityEngine.Object.Instantiate(meleePrefab, ability.transform.position + Vector3.forward * 4 + Vector3.up * 0.5f, Quaternion.identity);
        FreezeEnemy(victim);
        Enemy enemyComponent = victim.GetComponent<Enemy>();
        EnemyHealth health = victim.GetComponent<EnemyHealth>();
        enemyComponent.partsDropPrefab = enemyComponent.ammoDropPrefab = null;
        enemyComponent.superBananaDropChance = 1f;
        enemyComponent.coceyBananaDropChance = 0f;
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
            Check(UnityEngine.Object.FindObjectsByType<SuperBananaPickup>(FindObjectsSortMode.None).Length == bananasBefore + 1, "Guaranteed test drop creates one super banana");
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

        SpeedBoostAbility speedBoost = ability.GetComponent<SpeedBoostAbility>();
        float baseSpeed = movement.walkSpeed;
        AudioClip savedAngry = pause.gameAudio.angryMonkey;
        AudioClip angryTest = AudioClip.Create("Temporary boost audio check", 22050, 1, 22050, false);
        pause.gameAudio.angryMonkey = angryTest;
        var angrySource = (AudioSource)typeof(GameAudio).GetField("angryMonkeySource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pause.gameAudio);
        var coceyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Prefabs/Cocey banan Variant.prefab");
        var cocey = UnityEngine.Object.Instantiate(coceyPrefab, ability.transform.position + Vector3.up * 0.5f, coceyPrefab.transform.rotation);
        yield return 0.25f;
        Check(cocey == null && speedBoost.IsActive && speedBoost.CurrentMultiplier == 2f, "Cocey physical pickup activates double speed");
        Check(angrySource.isPlaying && angrySource.loop, "Cocey starts the angry-monkey SFX loop");
        Check(ability.GetComponent<CharacterAnimationDriver>().animator.speed == 2f, "Cocey doubles player animation speed");
        Check(speedBoost.screenTint.color.a > 0f && speedBoost.screenTint.color.a <= 0.15f, "Cocey applies subtle red tint");
        speedBoost.Tick(4f);
        speedBoost.Activate();
        Check(Mathf.Approximately(speedBoost.RemainingSeconds, 10f) && speedBoost.CurrentMultiplier == 2f, "Repeated pickup refreshes 10 seconds without stacking speed");
        pause.Pause();
        float remaining = speedBoost.RemainingSeconds;
        yield return 0.2f;
        Check(Mathf.Approximately(remaining, speedBoost.RemainingSeconds), "Pause freezes the speed boost timer");
        Check(!angrySource.isPlaying, "Pause suspends angry-monkey audio");
        pause.Resume();
        Check(angrySource.isPlaying, "Resume restores angry-monkey audio");
        speedBoost.Tick(10f);
        yield return 0.1f;
        Check(!speedBoost.IsActive && speedBoost.CurrentMultiplier == 1f && movement.walkSpeed == baseSpeed && ability.GetComponent<CharacterAnimationDriver>().animator.speed == 1f,
            "Boost expiry restores movement and animation speed");
        Check(speedBoost.screenTint.color.a == 0 && !speedBoost.hud.activeSelf, "Boost expiry clears tint and status");
        Check(!angrySource.isPlaying, "Boost expiry stops angry-monkey audio");
        pause.gameAudio.angryMonkey = savedAngry;
        UnityEngine.Object.Destroy(angryTest);

        GameObject robotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/SceneStuff/EnemyRanged.prefab");
        GameObject robot = UnityEngine.Object.Instantiate(robotPrefab, ability.transform.position + Vector3.forward * 5f, Quaternion.identity);
        FreezeEnemy(robot);
        var robotAnimation = robot.GetComponent<CharacterAnimationDriver>();
        yield return 0.1f;
        robot.transform.position += Vector3.right * 0.2f;
        robotAnimation.SendMessage("LateUpdate");
        robotAnimation.animator.Update(0.1f);
        Check(robotAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"), "RoboMonkey movement selects Walk state");
        var ranged = robot.GetComponent<RangedEnemy>();
        ranged.player = ability.transform;
        ranged.SendMessage("Shoot");
        robotAnimation.SendMessage("LateUpdate");
        robotAnimation.animator.Update(0.1f);
        Check(robotAnimation.animator.GetCurrentAnimatorStateInfo(0).IsName("Shoot"), "RoboMonkey firing selects Shoot state");
        UnityEngine.Object.Destroy(robot);

        // Isolate the two sound slots using a silent temporary clip and the voice cursor.
        GameAudio audio = pause.gameAudio;
        AudioClip savedDamage = audio.enemyStompDamaged, savedStomp = audio.enemyStomped;
        AudioClip silent = AudioClip.Create("Temporary stomp sound check", 2205, 1, 22050, false);
        FieldInfo cursor = typeof(GameAudio).GetField("nextVoice", BindingFlags.Instance | BindingFlags.NonPublic);
        audio.enemyStompDamaged = null; audio.enemyStomped = silent;
        var soundVictim = UnityEngine.Object.Instantiate(meleePrefab, ability.transform.position + Vector3.left * 4f, Quaternion.identity);
        FreezeEnemy(soundVictim);
        var soundEnemy = soundVictim.GetComponent<Enemy>();
        int beforeSound = (int)cursor.GetValue(audio);
        soundEnemy.Stomp(25);
        Check((int)cursor.GetValue(audio) == beforeSound, "Nonlethal stomp does not play final-stomp sound");
        audio.enemyStompDamaged = silent;
        soundEnemy.Stomp(25);
        Check((int)cursor.GetValue(audio) == (beforeSound + 1) % 24, "Nonlethal stomp plays separate damage sound");
        soundEnemy.Stomp(50);
        Check((int)cursor.GetValue(audio) == (beforeSound + 2) % 24, "Lethal stomp plays exactly one final-stomp sound");
        audio.enemyStompDamaged = savedDamage; audio.enemyStomped = savedStomp;
        UnityEngine.Object.Destroy(silent);

        ShootCameraShake shake = gun.cameraShake;
        Quaternion startRotation = shake.transform.localRotation;
        shake.Kick(true); shake.SendMessage("LateUpdate");
        Check(Quaternion.Angle(startRotation, shake.transform.localRotation) > 0.05f, "Super recoil rotates the actual aiming camera");
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
