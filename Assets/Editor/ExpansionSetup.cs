using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ExpansionSetup
{
    public static void Configure()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Stop Play mode first.");
        var main=SceneManager.GetActiveScene();if(main.path!="Assets/MainScene.unity")throw new System.InvalidOperationException("Open MainScene.");
        EditorBuildSettings.scenes=new[]{"MainMenu","MainScene","DeathScene","VictoryScene"}.Select(n=>new EditorBuildSettingsScene("Assets/"+n+".unity",true)).ToArray();
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Assets/Materials/RetroPixels.mat");
        if(material==null){material=new Material(Shader.Find("BulletMonkey/RetroPixels"));AssetDatabase.CreateAsset(material,"Assets/Assets/Materials/RetroPixels.mat");}
        var player=Object.FindFirstObjectByType<PlayerHealth>();
        var inventory=Get<PlayerBucketInventory>(player.gameObject);
        var unstuck=Get<PlayerUnstuck>(player.gameObject);unstuck.haloMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Assets/Materials/MonkeyHalo.mat");
        var areas=Object.FindObjectsByType<WaveArea>(FindObjectsSortMode.None).OrderBy(w=>w.name).ToArray();
        var centers=new System.Collections.Generic.List<Transform>();
        var start=new GameObject("Starting island rescue center").transform;start.SetParent(GameObject.Find("WaveManager").transform);start.position=player.transform.position;centers.Add(start);
        foreach(var area in areas)
        {
            var center=area.transform.Find("Rescue center");if(center==null){center=new GameObject("Rescue center").transform;center.SetParent(area.transform);}
            var pos=area.spawnPoints.Where(p=>p!=null).Aggregate(Vector3.zero,(sum,p)=>sum+p.position)/area.spawnPoints.Length;
            NavMeshHit hit;if(NavMesh.SamplePosition(pos,out hit,15,-1))pos=hit.position;center.position=pos;centers.Add(center);
        }
        unstuck.islandCenters=centers.ToArray();
        var hud=GameObject.Find("PlayerUI");var prompt=new GameObject("Unstuck warning",typeof(RectTransform),typeof(TextMeshProUGUI));prompt.transform.SetParent(hud.transform,false);
        var rt=prompt.GetComponent<RectTransform>();rt.anchorMin=new Vector2(.025f,.10f);rt.anchorMax=new Vector2(.57f,.19f);rt.offsetMin=rt.offsetMax=Vector2.zero;
        var text=prompt.GetComponent<TextMeshProUGUI>();text.fontSize=22;text.color=new Color(1,.8f,.2f);text.raycastTarget=false;text.alignment=TextAlignmentOptions.BottomLeft;unstuck.prompt=text;prompt.SetActive(false);
        var rangedPath="Assets/Assets/SceneStuff/EnemyRanged.prefab";
        var ranged=PrefabUtility.LoadPrefabContents(rangedPath);ranged.GetComponent<RangedEnemy>().noscopeChance=.1f;ranged.GetComponent<RangedEnemy>().noscopeDamageMultiplier=2;
        PrefabUtility.SaveAsPrefabAsset(ranged,rangedPath);PrefabUtility.UnloadPrefabContents(ranged);
        var boss=PrefabUtility.LoadPrefabContents(rangedPath);boss.name="Giant RoboMonkey";boss.transform.localScale*=2.5f;
        var health=boss.GetComponent<EnemyHealth>();health.maxHealth*=3;health.currentHealth=health.maxHealth;
        boss.GetComponent<RangedEnemy>().detectionRange=45;boss.GetComponent<RangedEnemy>().attackRange=18;
        var bossAsset=PrefabUtility.SaveAsPrefabAsset(boss,"Assets/Assets/SceneStuff/GiantRoboMonkey.prefab");PrefabUtility.UnloadPrefabContents(boss);
        var third=areas.First(w=>w.name=="Island3");third.bossPrefab=bossAsset;
        if(!third.waves.Any(w=>w.bossWave))third.waves=third.waves.Concat(new[]{new WaveArea.IslandWave{enemyCount=1,bossWave=true}}).ToArray();
        foreach(var boat in Object.FindObjectsByType<ModelSceneLoader>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            boat.targetSceneName="VictoryScene";var col=boat.GetComponent<BoxCollider>();if(col!=null)col.isTrigger=true;
            var body=Get<Rigidbody>(boat.gameObject);body.isKinematic=true;body.useGravity=false;
        }
        foreach(var loader in Object.FindObjectsByType<DeathSceneLoader>(FindObjectsInactive.Include,FindObjectsSortMode.None))loader.deathSceneName="DeathScene";
        ConfigureKabu();
        Style(main,material);EditorSceneManager.MarkSceneDirty(main);EditorSceneManager.SaveScene(main);
        foreach(var name in new[]{"MainMenu","DeathScene","VictoryScene"})
        {
            var scene=EditorSceneManager.OpenScene("Assets/"+name+".unity",OpenSceneMode.Additive);
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var intro in root.GetComponentsInChildren<VideoIntro>(true))intro.gameSceneName="MainScene";
                foreach(var death in root.GetComponentsInChildren<DeathButtons>(true)){death.mainSceneName="MainScene";death.mainMenuName="MainMenu";}
                foreach(var victory in root.GetComponentsInChildren<VictoryButtons>(true))victory.mainMenuName="MainMenu";
            }
            Style(scene,material);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);EditorSceneManager.CloseScene(scene,true);
        }
        SceneManager.SetActiveScene(main);AssetDatabase.SaveAssets();Selection.activeGameObject=player.gameObject;
    }
    private static T Get<T>(GameObject go) where T:Component {var c=go.GetComponent<T>();return c!=null?c:go.AddComponent<T>();}
    private static void ConfigureKabu()
    {
        var shop=Object.FindFirstObjectByType<ShopManager>();var gate=Get<KabuWaveShop>(shop.gameObject);gate.animator=shop.GetComponentInChildren<Animator>();
        string path="Assets/animation/Combat/KabuWave.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(controller==null)
        {
            controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            foreach(var pair in new[]{new[]{"Idle","Kabou|idle"},new[]{"Fire","Kabou|fire"},new[]{"Empty","Kabou|empty"}})
            {
                var clip=Object.Instantiate(AssetDatabase.LoadAllAssetsAtPath("Assets/Models/kabu.fbx").OfType<AnimationClip>().First(c=>c.name==pair[1]));clip.name="Kabu"+pair[0];
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
                AssetDatabase.CreateAsset(clip,"Assets/animation/Combat/Kabu"+pair[0]+".anim");controller.layers[0].stateMachine.AddState(pair[0]).motion=clip;
            }
        }
        gate.animator.runtimeAnimatorController=controller;
        foreach(var idle in shop.GetComponentsInChildren<ShopkeeperIdle>())idle.enabled=false;
        if(gate.fireVfx==null)
        {
            var go=new GameObject("Kabu fire VFX");go.transform.position=shop.transform.position+Vector3.up*.5f;go.transform.SetParent(shop.transform,true);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var m=ps.main;m.loop=true;m.startLifetime=.8f;m.startSpeed=2;m.startSize=.6f;m.startColor=new Color(1,.4f,.03f,.8f);m.scalingMode=ParticleSystemScalingMode.Hierarchy;
            var e=ps.emission;e.rateOverTime=45;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.radius=.65f;shape.angle=12;shape.rotation=new Vector3(-90,0,0);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Assets/Materials/ParticleOrange.mat");ps.Play();gate.fireVfx=go;go.SetActive(false);
        }
    }
    private static void Style(Scene scene,Material pixels)
    {
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset");
        foreach(var root in scene.GetRootGameObjects())
        {
            foreach(var camera in root.GetComponentsInChildren<Camera>(true)){var retro=Get<RetroCamera>(camera.gameObject);retro.pixelMaterial=pixels;retro.verticalResolution=240;}
            foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                t.font=font;t.fontStyle=FontStyles.Normal;
                if(t is TextMeshProUGUI){var shadow=Get<Shadow>(t.gameObject);shadow.effectColor=new Color(.025f,.02f,.06f,.9f);shadow.effectDistance=new Vector2(2,-2);}
            }
            foreach(var b in root.GetComponentsInChildren<Button>(true))
            {
                var img=b.GetComponent<Image>();if(img!=null){img.sprite=null;img.color=new Color(.12f,.15f,.28f,.96f);var outline=Get<Outline>(img.gameObject);outline.effectColor=new Color(.95f,.74f,.23f);outline.effectDistance=new Vector2(3,-3);}
                foreach(var t in b.GetComponentsInChildren<TMP_Text>(true))t.color=new Color(1,.92f,.61f);
                var colors=b.colors;colors.highlightedColor=new Color(1,.84f,.45f);colors.pressedColor=new Color(.6f,.55f,.9f);colors.fadeDuration=0;b.colors=colors;
            }
            if(scene.name!="MainScene")
            {
                foreach(var canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    var scaler=Get<CanvasScaler>(canvas.gameObject);scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
                    int index=0;
                    foreach(var button in canvas.GetComponentsInChildren<Button>(true))
                    {
                        var rect=button.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.22f-index*.115f);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(370,66);
                        foreach(var label in button.GetComponentsInChildren<TMP_Text>(true)){label.enableAutoSizing=true;label.fontSizeMin=20;label.fontSizeMax=30;}
                        index++;
                    }
                }
            }
            foreach(var slider in root.GetComponentsInChildren<Slider>(true))
            {
                if(slider.fillRect!=null){var fill=slider.fillRect.GetComponent<Image>();if(fill!=null)fill.color=new Color(.95f,.7f,.18f);}
                if(slider.handleRect!=null){var handle=slider.handleRect.GetComponent<Image>();if(handle!=null){handle.sprite=null;handle.color=new Color(1,.95f,.65f);}}
            }
            foreach(var component in root.GetComponentsInChildren<Component>(true))
            {
                if(component==null)continue;
                EditorUtility.SetDirty(component);
                if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }
    }
    public static void Restyle()
    {
        var main=SceneManager.GetActiveScene();var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Assets/Materials/RetroPixels.mat");
        Style(main,mat);EditorSceneManager.MarkSceneDirty(main);EditorSceneManager.SaveScene(main);
        foreach(var name in new[]{"MainMenu","DeathScene","VictoryScene"})
        {
            var s=EditorSceneManager.OpenScene("Assets/"+name+".unity",OpenSceneMode.Additive);Style(s,mat);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);EditorSceneManager.CloseScene(s,true);
        }
    }
}
