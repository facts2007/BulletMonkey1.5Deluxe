using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class FinalAnimationSetup
{
    const string PlayerModel="Assets/Scene tweaks/Models/bulletmonkey(anims).fbx";
    const string BossModel="Assets/Scene tweaks/Models/evil kabu.fbx";
    static AnimationClip Clip(string model,string match){return AssetDatabase.LoadAllAssetsAtPath(model).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview")&&c.name.ToLower().Contains(match.ToLower()));}
    static void State(AnimatorController controller,string name,AnimationClip source,bool loop,float duration=0)
    {
        string path="Assets/animation/Combat/Final_"+controller.name+"_"+name+".anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(clip==null){clip=Object.Instantiate(source);AssetDatabase.CreateAsset(clip,path);}else EditorUtility.CopySerialized(source,clip);
        clip.name=System.IO.Path.GetFileNameWithoutExtension(path);
        if(controller.name=="BulletMonkey")
            foreach(var binding in AnimationUtility.GetCurveBindings(clip).Where(b=>b.path=="Bulletmonkey_rig" && b.propertyName.StartsWith("m_LocalScale")))
                AnimationUtility.SetEditorCurve(clip,binding,AnimationCurve.Constant(0,clip.length,1));
        if(controller.name=="BulletMonkey")
            foreach(var binding in AnimationUtility.GetCurveBindings(clip).Where(b=>b.path.StartsWith("Bulletmonkey_rig/") && b.propertyName.StartsWith("m_LocalPosition")))
            {
                var curve=AnimationUtility.GetEditorCurve(clip,binding);var keys=curve.keys;
                for(int i=0;i<keys.Length;i++){keys[i].value*=100;keys[i].inTangent*=100;keys[i].outTangent*=100;}curve.keys=keys;AnimationUtility.SetEditorCurve(clip,binding,curve);
            }
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
        var machine=controller.layers[0].stateMachine;
        var state=machine.states.FirstOrDefault(s=>s.state.name==name).state;if(state==null)state=machine.AddState(name);
        state.motion=clip;state.speed=duration>0?clip.length/duration:1;EditorUtility.SetDirty(clip);EditorUtility.SetDirty(controller);
    }
    static Bounds Bounds(GameObject go){var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/animation/Combat/BulletMonkey.controller");
        State(controller,"CokeyRun",Clip(PlayerModel,"Cokey run"),true);
        State(controller,"Jump",Clip(PlayerModel,"Bullet-jump"),false);
        State(controller,"Cheer",Clip(PlayerModel,"cheer"),false,1.4f);
        var bossController=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/animation/Combat/EvilKabuPlaceholder.controller");
        State(bossController,"Idle",Clip(BossModel,"Empty"),true);
        State(bossController,"Run",Clip(BossModel,"Run"),true);
        State(bossController,"MegaPunch",Clip(BossModel,"punch"),false,.85f);
        State(bossController,"Stomp",Clip(BossModel,"jump+stomp"),false,1.4f);
        State(bossController,"RockThrow",Clip(BossModel,"throw"),false,.85f);
        State(bossController,"ThrowImp",Clip(BossModel,"throw"),false,.85f);
        var banana=new GameObject("HealingBananaPickup");
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Prefabs/banaan.prefab"));visual.transform.SetParent(banana.transform,false);visual.transform.localPosition=Vector3.zero;
        var b=Bounds(visual);visual.transform.localScale*=1.2f/Mathf.Max(b.size.x,b.size.y,b.size.z);b=Bounds(visual);visual.transform.position-=b.center;
        foreach(var c in visual.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
        banana.AddComponent<SphereCollider>().isTrigger=true;banana.GetComponent<SphereCollider>().radius=.8f;
        var rb=banana.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
        banana.AddComponent<BananaPickup>().healAmount=20;banana.AddComponent<LootMotion>().visual=visual.transform;
        var prefab=PrefabUtility.SaveAsPrefabAsset(banana,"Assets/Assets/SceneStuff/HealingBananaPickup.prefab");Object.DestroyImmediate(banana);
        int enemies=0;
        foreach(string path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Assets/SceneStuff"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset.GetComponent<Enemy>()==null)continue;
            var root=PrefabUtility.LoadPrefabContents(path);root.GetComponent<Enemy>().healingBananaPrefab=prefab;root.GetComponent<Enemy>().healingBananaDropChance=.15f;PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);enemies++;
        }
        var active=SceneManager.GetActiveScene();
        foreach(string path in new[]{"Assets/MainScene.unity","Assets/DungeonLevel.unity"})
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var hp=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PlayerHealth>(true)).First();
            var cheer=hp.GetComponent<PlayerCheer>();if(cheer==null)cheer=hp.gameObject.AddComponent<PlayerCheer>();cheer.cheerSeconds=1.4f;
            foreach(var e in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Enemy>(true))){e.healingBananaPrefab=prefab;e.healingBananaDropChance=.15f;EditorUtility.SetDirty(e);PrefabUtility.RecordPrefabInstancePropertyModifications(e);}
            if(path.Contains("Dungeon"))
            {
                var e=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DungeonEncounter>(true)).First();
                e.windupSeconds=.85f;e.recoverySeconds=.8f;e.rockDamage=30;e.punchDamage=40;e.bellyDamage=45;e.movesPerImp=6;
                if(e.visual.name.Contains("replace"))e.visual.gameObject.SetActive(false);
                var existing=e.boss.transform.Find("Evil Kabu model");
                GameObject model=existing!=null?existing.gameObject:(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scene tweaks/Models/Prefabs/evil kabu.prefab"),e.boss.transform);
                model.name="Evil Kabu model";model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
                b=Bounds(model);model.transform.localScale*=10f/b.size.y;b=Bounds(model);model.transform.position+=e.boss.transform.position-new Vector3(b.center.x,b.min.y,b.center.z);
                foreach(var c in model.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
                var capsule=e.boss.GetComponent<CapsuleCollider>();if(capsule==null)capsule=e.boss.gameObject.AddComponent<CapsuleCollider>();capsule.center=Vector3.up*5;capsule.height=10;capsule.radius=3.5f;
                var old=e.boss.GetComponent<Animator>();if(old!=null)old.enabled=false;
                e.animator=model.GetComponentInChildren<Animator>();if(e.animator==null)e.animator=model.AddComponent<Animator>();e.animator.runtimeAnimatorController=bossController;e.animator.applyRootMotion=false;e.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;e.visual=model.transform;
                e.boss.name="Evil Kabu";EditorUtility.SetDirty(e);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();return "Final clips and boss model assigned; healing drops on "+enemies+" enemy prefabs";
    }
}
