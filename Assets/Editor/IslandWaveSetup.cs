using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class IslandWaveSetup
{
    public static void Configure()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
        var manager = GameObject.Find("WaveManager");
        var first = GameObject.Find("Island1").GetComponent<WaveArea>();
        var template = first.transform.Find("Mist wall").gameObject;
        var oldBlocker = first.pathBlocker;
        var ui = first.countdownText;
        var prefabs = new[] {
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/SceneStuff/EnemyMelee.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/SceneStuff/EnemyRanged.prefab") };
        Vector3[] centers = {new Vector3(-23,15,-70),new Vector3(38,23,-127),new Vector3(-11,13,-204),new Vector3(-84,11,-287),new Vector3(-80,23,-390)};
        Vector3[] bridges = {new Vector3(-18,14,-52),new Vector3(4,17,-95),new Vector3(19,15,-166),new Vector3(-50,10,-243),new Vector3(-73,25,-351),new Vector3(-133,22,-461)};
        var areas = new WaveArea[5];
        for (int i = 0; i < 5; i++)
        {
            WaveArea area = i == 0 ? first : new GameObject("Island" + (i+1)).AddComponent<WaveArea>();
            area.transform.SetParent(manager.transform, true);
            area.transform.position = centers[i];
            area.islandName = "Island " + (i+1);
            area.autoStart = false; area.startsUnlocked = i == 0;
            area.waveStarted = false; area.waveComplete = false;
            area.countdownSeconds = 3; area.countdownText = ui;
            area.waves = new[] {new WaveArea.IslandWave {enemyCount=4+i*2},new WaveArea.IslandWave {enemyCount=6+i*2}};
            area.enemyPrefabs = prefabs;
            var entry = new GameObject("Entry trigger"); entry.transform.SetParent(area.transform);
            Vector3 direction = (centers[i]-bridges[i]); direction.y=0; direction.Normalize();
            Vector3 entryFloor = Ground(bridges[i]+direction*5);
            entry.transform.position = entryFloor+Vector3.up*3;
            entry.transform.rotation = Quaternion.LookRotation(direction);
            var box=entry.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(18,8,3);
            entry.AddComponent<IslandEntryTrigger>().island=area;area.entryTrigger=box;
            // Keep the user's mist material/mesh, placing one solid gate across each exit bridge.
            GameObject wall=i==0?template:Object.Instantiate(template);
            wall.name="Mist wall";wall.transform.SetParent(area.transform,true);
            wall.transform.position=Ground(bridges[i+1])+Vector3.up*6;
            Vector3 exitDirection=bridges[i+1]-centers[i];exitDirection.y=0;
            wall.transform.rotation=Quaternion.LookRotation(exitDirection);
            wall.transform.localScale=new Vector3(i==4?48:22,16,2.4f);
            var collider=wall.GetComponent<BoxCollider>();collider.enabled=true;collider.isTrigger=false;
            var obstacle=wall.GetComponent<NavMeshObstacle>();if(obstacle==null)obstacle=wall.AddComponent<NavMeshObstacle>();
            obstacle.shape=NavMeshObstacleShape.Box;obstacle.size=Vector3.one;obstacle.carving=true;
            area.pathBlocker=wall;
            var oldPoints=area.spawnPoints;
            var points=new Transform[3];
            for(int j=0;j<3;j++)
            {
                Transform point=i==0 && oldPoints!=null && j<oldPoints.Length ? oldPoints[j] : new GameObject("Spawnpoint "+(j+1)).transform;
                point.SetParent(area.transform,true);
                Vector3 desired=centers[i]+new Vector3((j-1)*7,0,j==1?7:-5);
                if(i==4) desired=new[]{new Vector3(-80,23,-390),new Vector3(-65,24,-395),new Vector3(-80,15,-405)}[j];
                point.position=Ground(desired);points[j]=point;
            }
            area.spawnPoints=points;areas[i]=area;EditorUtility.SetDirty(area);
        }
        for(int i=0;i<4;i++)areas[i].nextArea=areas[i+1];
        if(oldBlocker!=null && oldBlocker!=template)oldBlocker.SetActive(false);
        EditorSceneManager.MarkSceneDirty(first.gameObject.scene);
        EditorSceneManager.SaveScene(first.gameObject.scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject=first.gameObject;
    }
    private static Vector3 Ground(Vector3 desired)
    {
        NavMeshHit hit;
        if(!NavMesh.SamplePosition(desired,out hit,12,NavMesh.AllAreas))throw new System.InvalidOperationException("No walkable ground near "+desired);
        return hit.position;
    }
}
