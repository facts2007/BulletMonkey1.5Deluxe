using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Editor-only wiring for the existing gameplay prefabs and MainScene.</summary>
public static class CombatPresentationSetup
{
    private const string Output = "Assets/Animation/Combat";
    private const string PlayerModel = "Assets/Models/Prefabs/bulletmoneky.prefab";
    private const string RobotModel = "Assets/Models/Prefabs/robomonkey 1.prefab";
    private const string DroidModel = "Assets/Models/Prefabs/MiniDroid.prefab";
    private const string BananaPath = "Assets/Assets/SceneStuff/SuperBananaPickup.prefab";

    [MenuItem("Tools/BulletMonkey/Set up combat presentation")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before setup.");
        if (SceneManager.GetActiveScene().path != "Assets/MainScene.unity")
            throw new InvalidOperationException("Open MainScene first.");
        var sceneEnemyVisibility = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .ToDictionary(enemy => enemy, enemy => enemy.gameObject.activeSelf);
        EnsureFolder("Assets/Animation");
        EnsureFolder(Output);
        BuildController("BulletMonkey", "Assets/Models/bulletmoneky.fbx", false, true);
        BuildController("RoboMonkey", "Assets/Models/robomonkey 1.fbx", false, false);
        BuildController("MiniDroid", "Assets/Models/MiniDroid.fbx", true, false);
        BuildBanana();
        EditPrefab("Assets/Scene tweaks/Player.prefab", ConfigurePlayer);
        EditPrefab("Assets/Assets/SceneStuff/EnemyRanged.prefab", go => ConfigureEnemy(go, false));
        EditPrefab("Assets/Assets/SceneStuff/EnemyMelee.prefab", go => ConfigureEnemy(go, true));

        foreach (PlayerMovement player in UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            ConfigurePlayer(player.gameObject);
        foreach (Enemy enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (enemy.GetComponent<MeleeEnemy>() != null) ConfigureEnemy(enemy.gameObject, true);
            else if (enemy.GetComponent<RangedEnemy>() != null) ConfigureEnemy(enemy.gameObject, false);
        }
        foreach (var entry in sceneEnemyVisibility)
        {
            if (entry.Key == null) continue;
            entry.Key.gameObject.SetActive(entry.Value);
            PrefabUtility.RecordPrefabInstancePropertyModifications(entry.Key.gameObject);
        }
        ConfigureUI();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Combat presentation, super banana, and audio settings configured.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    private static void EditPrefab(string path, Action<GameObject> configure)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            configure(root);
            // Scene examples may stay hidden, but wave-spawned prefab roots must be active.
            if (root.GetComponent<Enemy>() != null) root.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void BuildController(string name, string modelPath, bool walkOnly, bool super)
    {
        string path = Output + "/" + name + ".controller";
        // Do not overwrite hand-tuned controllers on subsequent setup runs.
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) return;
        AnimationClip[] source = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToArray();
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        string[] states = walkOnly ? new[] { "Walk" } : super
            ? new[] { "Idle", "Walk", "Shoot", "Empty", "SuperShoot" }
            : new[] { "Idle", "Walk", "Shoot", "Empty" };
        foreach (string stateName in states)
        {
            string search = stateName == "SuperShoot" ? "super shooting" : stateName == "Shoot" ? "- shooting" : stateName.ToLowerInvariant();
            AnimationClip original = source.FirstOrDefault(c => c.name.ToLowerInvariant().Contains(search));
            if (original == null) throw new InvalidOperationException("Missing " + stateName + " clip in " + modelPath);
            AnimationClip clip = UnityEngine.Object.Instantiate(original);
            clip.name = name + " " + stateName;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, Output + "/" + name + "_" + stateName + ".anim");
            AnimatorState state = controller.layers[0].stateMachine.AddState(stateName);
            state.motion = clip;
            if (stateName == states[0]) controller.layers[0].stateMachine.defaultState = state;
        }
    }

    private static T Ensure<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static GameObject AttachModel(GameObject root, string path, string name, float height, float feetY)
    {
        Transform existing = root.transform.Find(name);
        if (existing != null) return existing.gameObject;
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform);
        model.name = name;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
        Bounds bounds = LocalBounds(model, root.transform);
        model.transform.localScale *= height / Mathf.Max(0.01f, bounds.size.y);
        bounds = LocalBounds(model, root.transform);
        model.transform.localPosition = new Vector3(-bounds.center.x, feetY - bounds.min.y, -bounds.center.z);
        foreach (Transform child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = root.layer;
        return model;
    }

    private static Bounds LocalBounds(GameObject model, Transform space)
    {
        Bounds result = new Bounds();
        bool first = true;
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            Bounds b = renderer.bounds;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 point = space.InverseTransformPoint(b.center + Vector3.Scale(b.extents, new Vector3(x, y, z)));
                        if (first) { result = new Bounds(point, Vector3.zero); first = false; }
                        else result.Encapsulate(point);
                    }
        }
        return result;
    }

    private static CharacterAnimationDriver ConfigureAnimation(GameObject root, GameObject model, string controller, bool walkOnly)
    {
        Animator animator = Ensure<Animator>(model);
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(Output + "/" + controller + ".controller");
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        CharacterAnimationDriver driver = Ensure<CharacterAnimationDriver>(root);
        driver.animator = animator;
        driver.movementRoot = root.transform;
        driver.walkOnly = walkOnly;
        return driver;
    }

    private static void ConfigurePlayer(GameObject root)
    {
        // Keep the controller, camera, weapon logic, and all existing gameplay references.
        Transform old = root.transform.Find("bulletmonkey (1)");
        if (old != null) old.gameObject.SetActive(false);
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            if (renderer.name == "Model" || renderer.name == "Gun") renderer.enabled = false;
        GameObject model = AttachModel(root, PlayerModel, "BulletMonkeyVisual", 2.2f, 0f);
        CharacterAnimationDriver driver = ConfigureAnimation(root, model, "BulletMonkey", false);
        Gun gun = root.GetComponentInChildren<Gun>(true);
        SuperShootAbility ability = Ensure<SuperShootAbility>(root);
        ability.gun = gun;
        driver.playerGun = gun;
        driver.superAbility = ability;
        if (gun != null)
        {
            gun.characterAnimation = driver;
            gun.superAbility = ability;
            Camera camera = root.GetComponentInChildren<Camera>(true);
            if (camera != null) { gun.aimCamera = camera; gun.cameraShake = Ensure<ShootCameraShake>(camera.gameObject); }
            gun.muzzlePoint = MakeMuzzle(model);
        }
        EditorUtility.SetDirty(root);
    }

    private static Transform MakeMuzzle(GameObject model)
    {
        Transform existing = model.transform.Find("MuzzlePoint");
        if (existing != null) return existing;
        Transform muzzle = new GameObject("MuzzlePoint").transform;
        muzzle.SetParent(model.transform, false);
        // The AK barrel points forward; position at its forward bound in model space.
        Renderer weapon = model.GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.name.StartsWith("AK-47"));
        if (weapon != null)
        {
            Bounds b = weapon.bounds;
            muzzle.position = new Vector3(b.center.x, b.center.y, b.max.z);
        }
        else muzzle.localPosition = new Vector3(0.4f, 1.5f, 0.9f);
        return muzzle;
    }

    private static void ConfigureEnemy(GameObject root, bool melee)
    {
        MeshRenderer placeholder = root.GetComponent<MeshRenderer>();
        if (placeholder != null) placeholder.enabled = false;
        Transform old = root.transform.Find("robomonkey");
        if (old != null) old.gameObject.SetActive(false);
        GameObject model = AttachModel(root, melee ? DroidModel : RobotModel,
            melee ? "MiniDroidVisual" : "RoboMonkeyVisual", melee ? 1.35f : 2.2f, -0.5f);
        CharacterAnimationDriver driver = ConfigureAnimation(root, model, melee ? "MiniDroid" : "RoboMonkey", melee);
        Enemy enemy = root.GetComponent<Enemy>();
        enemy.visualRoot = model.transform;
        enemy.flattenOnStomp = melee;
        enemy.superBananaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BananaPath);
        enemy.superBananaDropChance = 0.01f;
        Transform bar = root.transform.Find("Healthbar");
        if (bar != null)
        {
            enemy.healthBarObject = bar.gameObject;
            Vector3 position = bar.localPosition;
            position.y = melee ? 1.1f : 2f;
            bar.localPosition = position;
        }
        BoxCollider box = root.GetComponent<BoxCollider>();
        if (box != null)
        {
            box.center = new Vector3(0f, melee ? 0.175f : 0.6f, 0f);
            box.size = new Vector3(melee ? 0.8f : 1f, melee ? 1.35f : 2.2f, 0.8f);
        }
        if (!melee)
        {
            RangedEnemy ranged = root.GetComponent<RangedEnemy>();
            ranged.characterAnimation = driver;
            ranged.firePoint = MakeMuzzle(model);
            ranged.aimHeightOffset = 1f;
        }
        EditorUtility.SetDirty(root);
    }

    private static void BuildBanana()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(BananaPath) != null) return;
        GameObject root = new GameObject("SuperBananaPickup");
        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Prefabs/superbanana Variant.prefab"), root.transform);
        visual.transform.localPosition = Vector3.zero;
        SuperBananaPickup pickup = root.AddComponent<SuperBananaPickup>();
        pickup.visual = visual.transform;
        SphereCollider collider = root.GetComponent<SphereCollider>();
        collider.radius = 0.65f;
        collider.isTrigger = true;
        Rigidbody body = root.GetComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        PrefabUtility.SaveAsPrefabAsset(root, BananaPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void ConfigureUI()
    {
        PauseManager pause = UnityEngine.Object.FindFirstObjectByType<PauseManager>();
        if (pause == null || pause.pausePanel == null) throw new InvalidOperationException("Pause manager not found.");
        GameAudio audio = UnityEngine.Object.FindFirstObjectByType<GameAudio>();
        if (audio == null) audio = new GameObject("Game Audio - assign sound clips here").AddComponent<GameAudio>();
        pause.gameAudio = audio;
        Transform menu = pause.pausePanel.transform;
        Button continueButton = menu.Find("ContinueButton").GetComponent<Button>();
        Transform settingsButton = menu.Find("SettingsButton");
        if (settingsButton == null)
        {
            GameObject go = UnityEngine.Object.Instantiate(continueButton.gameObject, menu);
            go.name = "SettingsButton";
            settingsButton = go.transform;
            Button button = go.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, pause.OpenSettings);
            go.GetComponentInChildren<TMP_Text>(true).text = "Settings";
        }
        string[] buttons = { "ContinueButton", "SettingsButton", "BackToMenuButton", "QuitGameButton" };
        pause.menuButtons = buttons.Select(n => menu.Find(n).gameObject).ToArray();
        for (int i = 0; i < buttons.Length; i++)
            ((RectTransform)pause.menuButtons[i].transform).anchoredPosition = new Vector2(0f, 75f - i * 50f);

        if (menu.Find("AudioSettings") == null)
        {
            RectTransform panel = Rect("AudioSettings", menu, new Vector2(440, 310), Vector2.zero);
            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.055f, 0.07f, 0.09f, 0.98f);
            Label("Title", panel, "AUDIO SETTINGS", new Vector2(0, 112), new Vector2(400, 40), 26);
            Label("MusicLabel", panel, "Music", new Vector2(-135, 57), new Vector2(100, 30), 20);
            Label("SfxLabel", panel, "Sound effects", new Vector2(-105, -20), new Vector2(160, 30), 20);
            pause.musicValue = Label("MusicValue", panel, "70%", new Vector2(160, 57), new Vector2(70, 30), 20);
            pause.sfxValue = Label("SfxValue", panel, "80%", new Vector2(160, -20), new Vector2(70, 30), 20);
            pause.musicSlider = MakeSlider("MusicVolume", panel, new Vector2(0, 25), 0.7f);
            pause.sfxSlider = MakeSlider("SfxVolume", panel, new Vector2(0, -52), 0.8f);
            GameObject back = UnityEngine.Object.Instantiate(continueButton.gameObject, panel);
            back.name = "BackButton";
            ((RectTransform)back.transform).anchoredPosition = new Vector2(0, -112);
            back.GetComponentInChildren<TMP_Text>(true).text = "Back";
            Button backButton = back.GetComponent<Button>();
            backButton.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(backButton.onClick, pause.CloseSettings);
            pause.settingsPanel = panel.gameObject;
            panel.gameObject.SetActive(false);
        }
        RepairSlider(pause.musicSlider);
        RepairSlider(pause.sfxSlider);
        SuperShootAbility ability = UnityEngine.Object.FindFirstObjectByType<SuperShootAbility>();
        if (ability != null && ability.hud == null)
        {
            Canvas canvas = GameObject.Find("PlayerUI").GetComponent<Canvas>();
            RectTransform hud = Rect("SuperBananaHUD", canvas.transform, new Vector2(420, 70), new Vector2(0, 90));
            hud.anchorMin = hud.anchorMax = new Vector2(0.5f, 0f);
            Image backdrop = hud.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0.04f, 0.05f, 0.07f, 0.88f);
            backdrop.raycastTarget = false;
            ability.promptText = Label("Prompt", hud, "SUPER BANANA  •  Hold F to charge", new Vector2(0, 12), new Vector2(400, 35), 18);
            ability.promptText.color = new Color(1f, 0.86f, 0.2f);
            RectTransform track = Rect("ChargeTrack", hud, new Vector2(380, 8), new Vector2(0, -18));
            track.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.22f, 0.25f);
            RectTransform fill = Rect("ChargeFill", track, new Vector2(380, 8), Vector2.zero);
            Image image = fill.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 0f;
            image.color = new Color(1f, 0.8f, 0.05f);
            image.raycastTarget = false;
            ability.chargeFill = image;
            ability.hud = hud.gameObject;
            hud.gameObject.SetActive(false);
        }
        EditorUtility.SetDirty(pause);
        if (ability != null) EditorUtility.SetDirty(ability);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static TMP_Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size, float fontSize)
    {
        TextMeshProUGUI label = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static Slider MakeSlider(string name, Transform parent, Vector2 position, float value)
    {
        RectTransform root = Rect(name, parent, new Vector2(360, 28), position);
        Slider slider = root.gameObject.AddComponent<Slider>();
        Image track = Rect("Track", root, new Vector2(360, 6), Vector2.zero).gameObject.AddComponent<Image>();
        track.color = new Color(0.25f, 0.28f, 0.32f);
        RectTransform fillArea = Rect("FillArea", root, new Vector2(360, 6), Vector2.zero);
        RectTransform fill = Rect("Fill", fillArea, Vector2.zero, Vector2.zero);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.gameObject.AddComponent<Image>().color = new Color(1f, 0.8f, 0.05f);
        RectTransform handleArea = Rect("HandleArea", root, new Vector2(340, 28), Vector2.zero);
        RectTransform handle = Rect("Handle", handleArea, new Vector2(18, 0), Vector2.zero);
        handle.anchorMin = Vector2.zero;
        handle.anchorMax = new Vector2(0, 1);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = Color.white;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;
        return slider;
    }

    private static void RepairSlider(Slider slider)
    {
        if (slider == null) return;
        Transform area = slider.transform.Find("FillArea");
        if (area == null) area = Rect("FillArea", slider.transform, new Vector2(360, 6), Vector2.zero);
        slider.fillRect.SetParent(area, false);
        slider.fillRect.sizeDelta = Vector2.zero;
        slider.fillRect.anchoredPosition = Vector2.zero;
        slider.handleRect.sizeDelta = new Vector2(18, 0);
        slider.handleRect.anchoredPosition = Vector2.zero;
        // Reassigning refreshes Slider's driven anchors in edit mode.
        slider.direction = Slider.Direction.RightToLeft;
        slider.direction = Slider.Direction.LeftToRight;
    }
}
