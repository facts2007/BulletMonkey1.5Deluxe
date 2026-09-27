using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class HealthStyleSetup
{
    static void Style(PlayerHealth player,EnemyHealth enemy)
    {
        var bar=player!=null?player.healthBar:enemy.healthBar;if(bar==null)return;
        var parent=bar.parent;
        foreach(var graphic in parent.GetComponentsInChildren<Graphic>(true))if(!(graphic is PixelHealthGraphic)){graphic.enabled=false;EditorUtility.SetDirty(graphic);PrefabUtility.RecordPrefabInstancePropertyModifications(graphic);}
        var pixel=(player!=null?parent.parent:parent).GetComponentInChildren<PixelHealthGraphic>(true);
        if(pixel==null){var go=new GameObject("Pixel health bar",typeof(RectTransform),typeof(CanvasRenderer));go.transform.SetParent(parent,false);pixel=go.AddComponent<PixelHealthGraphic>();}
        pixel.player=player;pixel.enemy=enemy;pixel.playerDecoration=player!=null;pixel.raycastTarget=false;
        var rect=pixel.rectTransform;
        if(player!=null){rect.SetParent(parent.parent,false);rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(20,-18);rect.sizeDelta=new Vector2(290,106);}
        else{rect.anchorMin=bar.anchorMin;rect.anchorMax=bar.anchorMax;rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=bar.anchoredPosition;rect.sizeDelta=new Vector2(bar.rect.width,Mathf.Max(30,bar.rect.width*14/82));}
        pixel.SetAllDirty();EditorUtility.SetDirty(pixel);PrefabUtility.RecordPrefabInstancePropertyModifications(pixel);
    }
    public static void Configure()
    {
        if(Application.isPlaying)throw new System.Exception("Stop Play first");
        foreach(var path in new[]{"Assets/Assets/SceneStuff/EnemyMelee.prefab","Assets/Assets/SceneStuff/EnemyRanged.prefab","Assets/Assets/SceneStuff/GiantRoboMonkey.prefab","Assets/Assets/SceneStuff/ImptronPlaceholder.prefab"})
        {var root=PrefabUtility.LoadPrefabContents(path);Style(null,root.GetComponent<EnemyHealth>());PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);}
        foreach(var enemy in Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include,FindObjectsSortMode.None))Style(null,enemy);
        Style(Object.FindFirstObjectByType<PlayerHealth>(),null);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
    }
}

