using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class IslandPolishSetup
{
    private static T Get<T>(GameObject go) where T:Component {var component=go.GetComponent<T>();return component!=null?component:go.AddComponent<T>();}
    public static string Configure()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Stop Play first");
        var scene=SceneManager.GetActiveScene();if(scene.name!="MainScene")throw new System.InvalidOperationException("Open MainScene first");
        var areas=Object.FindObjectsByType<WaveArea>(FindObjectsSortMode.None).OrderBy(a=>a.name).ToArray();
        var bucketPath="Assets/Models/Prefabs/WaterBucket.prefab";
        var bucket=PrefabUtility.LoadPrefabContents(bucketPath);
        Get<WaterBucketPickup>(bucket);
        foreach(var col in bucket.GetComponentsInChildren<Collider>())col.isTrigger=true;
        var bounds=new Bounds(bucket.transform.position,Vector3.zero);foreach(var r in bucket.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
        var trigger=Get<BoxCollider>(bucket);trigger.isTrigger=true;trigger.center=bucket.transform.InverseTransformPoint(bounds.center);
        trigger.size=new Vector3(Mathf.Max(.8f,bounds.size.x)/bucket.transform.lossyScale.x,Mathf.Max(1,bounds.size.y)/bucket.transform.lossyScale.y,Mathf.Max(.8f,bounds.size.z)/bucket.transform.lossyScale.z);
        var rb=Get<Rigidbody>(bucket);rb.isKinematic=true;rb.useGravity=false;
        var bucketAsset=PrefabUtility.SaveAsPrefabAsset(bucket,bucketPath);PrefabUtility.UnloadPrefabContents(bucket);
        for(int i=0;i<areas.Length;i++)
        {
            var area=areas[i];area.islandNumber=i+1;area.shopSpawn=area.transform.Find("Shopspawn");area.rescueCenter=area.transform.Find("Rescue center");area.waterBucketPrefab=bucketAsset;EditorUtility.SetDirty(area);
        }
        string giantPath="Assets/Assets/SceneStuff/GiantRoboMonkey.prefab";
        var giant=PrefabUtility.LoadPrefabContents(giantPath);giant.GetComponent<RangedEnemy>().fireRate=1;PrefabUtility.SaveAsPrefabAsset(giant,giantPath);PrefabUtility.UnloadPrefabContents(giant);
        var cloud=CloudMaterial();
        var fog=Object.FindFirstObjectByType<SpawnFog>();if(fog==null)fog=new GameObject("Shared spawn fog").AddComponent<SpawnFog>();fog.material=cloud;
        foreach(var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.name=="Mist particles"))
        {
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.startLifetime=4;main.startSpeed=.15f;main.startSize=new ParticleSystem.MinMaxCurve(5,8);main.startColor=new Color(.72f,.82f,.87f,.8f);main.maxParticles=120;main.prewarm=true;
            var emission=ps.emission;emission.rateOverTime=28;
            var shape=ps.shape;var scale=shape.scale;scale.z=2;shape.scale=scale;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=cloud;ps.Play();EditorUtility.SetDirty(ps);
        }
        var encounterRoot=GameObject.Find("WaveManager/Boss island");
        var encounter=Get<BossFusionEncounter>(encounterRoot);encounter.requiredIsland=areas.Last();
        var transforms=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        encounter.impSpawns=transforms.Where(t=>t.name.StartsWith("imp spawn")).OrderBy(t=>t.name).ToArray();encounter.mergePoint=transforms.First(t=>t.name=="Merge point");
        if(encounter.finalMistDoor==null)encounter.finalMistDoor=areas.Last().GetComponentsInChildren<Transform>(true).First(t=>t.name=="Mist wall"&&t.gameObject!=areas.Last().pathBlocker).gameObject;
        encounter.finalMistDoor.name="Last mist door";
        var entry=Get<BoxCollider>(encounterRoot);entry.isTrigger=true;entry.size=new Vector3(42,12,10);entry.center=Vector3.up*4;
        var entryBody=Get<Rigidbody>(encounterRoot);entryBody.isKinematic=true;entryBody.useGravity=false;
        string runnerPath="Assets/Assets/SceneStuff/FusionImpVisual.prefab";
        var runnerAsset=AssetDatabase.LoadAssetAtPath<GameObject>(runnerPath);
        if(runnerAsset==null)
        {
            var melee=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/SceneStuff/EnemyMelee.prefab");
            var runner=new GameObject("Fusion imp visual");var visual=Object.Instantiate(melee.transform.Find("MiniDroidVisual").gameObject,runner.transform);visual.transform.localPosition=Vector3.zero;
            foreach(var behaviour in runner.GetComponentsInChildren<MonoBehaviour>())Object.DestroyImmediate(behaviour);
            foreach(var collider in runner.GetComponentsInChildren<Collider>())Object.DestroyImmediate(collider);
            foreach(var body in runner.GetComponentsInChildren<Rigidbody>())Object.DestroyImmediate(body);
            runnerAsset=PrefabUtility.SaveAsPrefabAsset(runner,runnerPath);Object.DestroyImmediate(runner);
        }
        encounter.impVisualPrefab=runnerAsset;
        string bossPath="Assets/Assets/SceneStuff/ImptronPlaceholder.prefab";
        var bossAsset=AssetDatabase.LoadAssetAtPath<GameObject>(bossPath);
        if(bossAsset==null)
        {
            var boss=new GameObject("Imptron placeholder");var cylinder=GameObject.CreatePrimitive(PrimitiveType.Cylinder);cylinder.transform.SetParent(boss.transform,false);cylinder.transform.localPosition=Vector3.up*3;cylinder.transform.localScale=new Vector3(5,3,5);
            var health=boss.AddComponent<EnemyHealth>();health.maxHealth=750;health.currentHealth=750;health.pointValue=0;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/SceneStuff/EnemyMelee.prefab");var bar=Object.Instantiate(source.transform.Find("Healthbar").gameObject,boss.transform);bar.name="Healthbar";bar.transform.localPosition=Vector3.up*7;bar.transform.localScale*=3;
            health.healthBar=bar.transform.Find("Health") as RectTransform;health.redBar=bar.transform.Find("Red") as RectTransform;health.healthText=bar.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            bossAsset=PrefabUtility.SaveAsPrefabAsset(boss,bossPath);Object.DestroyImmediate(boss);
        }
        if(encounter.bossPrefab==null)encounter.bossPrefab=bossAsset;
        if(encounter.fusionCamera==null)
        {
            var cameraObject=new GameObject("Imp fusion cutscene camera");cameraObject.transform.SetParent(encounterRoot.transform,true);
            cameraObject.transform.position=encounter.mergePoint.position+new Vector3(0,30,48);cameraObject.transform.LookAt(encounter.mergePoint.position+Vector3.up*3);
            var camera=cameraObject.AddComponent<Camera>();camera.fieldOfView=72;camera.farClipPlane=300;camera.enabled=false;encounter.fusionCamera=camera;
            var retro=cameraObject.AddComponent<RetroCamera>();retro.pixelMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Assets/Materials/RetroPixels.mat");retro.enabled=false;
        }
        EditorUtility.SetDirty(encounter);
        var pipeline=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/urp.asset");
        if(pipeline!=null){var so=new SerializedObject(pipeline);var resolution=so.FindProperty("m_MainLightShadowmapResolution");if(resolution!=null)resolution.intValue=2048;so.ApplyModifiedProperties();}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        foreach(var name in new[]{"MainMenu","DeathScene","VictoryScene"})
        {
            var other=EditorSceneManager.OpenScene("Assets/"+name+".unity",OpenSceneMode.Additive);
            foreach(var theme in other.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SceneThemeMusic>(true)))
            {
                if(theme.theme==null)theme.theme=theme.GetComponent<AudioSource>().clip;EditorUtility.SetDirty(theme);PrefabUtility.RecordPrefabInstancePropertyModifications(theme);
            }
            EditorSceneManager.MarkSceneDirty(other);EditorSceneManager.SaveScene(other);EditorSceneManager.CloseScene(other,true);
        }
        SceneManager.SetActiveScene(scene);AssetDatabase.SaveAssets();return "Configured 5 islands, bucket reward, cloud pool, denser mist, fusion entry/camera/placeholder, music and shadow cost.";
    }
    private static Material CloudMaterial()
    {
        const string texturePath="Assets/Assets/Materials/SoftFog.asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if(texture==null)
        {
            texture=new Texture2D(64,64,TextureFormat.RGBA32,false);texture.name="Soft fog cloud";texture.wrapMode=TextureWrapMode.Clamp;
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                float u=(x-31.5f)/31.5f,v=(y-31.5f)/31.5f;float radius=Mathf.Sqrt(u*u+v*v);float alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01((1-radius)*3));
                alpha*=.75f+.25f*Mathf.PerlinNoise(x*.12f,y*.12f);texture.SetPixel(x,y,new Color(1,1,1,alpha));
            }
            texture.Apply();AssetDatabase.CreateAsset(texture,texturePath);
        }
        const string path="Assets/Assets/Materials/SoftFog.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Assets/Materials/IslandMist.mat"));AssetDatabase.CreateAsset(material,path);}
        material.SetTexture("_BaseMap",texture);material.mainTexture=texture;material.SetColor("_BaseColor",Color.white);EditorUtility.SetDirty(material);return material;
    }
}

