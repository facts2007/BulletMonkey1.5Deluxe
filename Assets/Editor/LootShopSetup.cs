using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class LootShopSetup
{
    static T Get<T>(GameObject go) where T:Component{var c=go.GetComponent<T>();return c!=null?c:go.AddComponent<T>();}
    static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position){var go=new GameObject(name,typeof(RectTransform));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.sizeDelta=size;rt.anchoredPosition=position;return rt;}
    static Sprite ImportSprite(string path,int resolution){var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.maxTextureSize=resolution;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);}
    static TMP_FontAsset Font=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset");
    static TextMeshProUGUI Text(string name,Transform parent,string value,Vector2 size,Vector2 pos,int fontSize){var rt=Rect(name,parent,size,pos);var text=rt.gameObject.AddComponent<TextMeshProUGUI>();text.text=value;text.font=Font;text.fontSize=fontSize;text.color=new Color(1,.94f,.75f);text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;}
    static RetroPanelGraphic Panel(Transform parent)
    {
        var old=parent.Find("Retro frame");if(old!=null){Get<CanvasRenderer>(old.gameObject);return old.GetComponent<RetroPanelGraphic>();}
        var rt=Rect("Retro frame",parent,Vector2.zero,Vector2.zero);rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;rt.SetAsFirstSibling();Get<CanvasRenderer>(rt.gameObject);var panel=rt.gameObject.AddComponent<RetroPanelGraphic>();panel.raycastTarget=false;return panel;
    }
    static void Style(Scene scene)
    {
        foreach(var root in scene.GetRootGameObjects())
        {
            foreach(var button in root.GetComponentsInChildren<Button>(true))
            {
                var image=button.GetComponent<Image>();if(image!=null)image.color=Color.clear;
                foreach(var old in button.GetComponents<Shadow>())old.enabled=false;
                button.targetGraphic=Panel(button.transform);var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.35f,1.25f,1);colors.pressedColor=new Color(.7f,.7f,.75f);colors.disabledColor=new Color(.5f,.5f,.5f);button.colors=colors;
                foreach(var text in button.GetComponentsInChildren<TMP_Text>(true)){text.font=Font;text.color=new Color(1,.94f,.75f);text.fontSize=Mathf.Min(text.fontSize,26);EditorUtility.SetDirty(text);PrefabUtility.RecordPrefabInstancePropertyModifications(text);}
                EditorUtility.SetDirty(button);PrefabUtility.RecordPrefabInstancePropertyModifications(button);
            }
            foreach(var slider in root.GetComponentsInChildren<Slider>(true))
            {
                foreach(var img in slider.GetComponentsInChildren<Image>(true)){img.sprite=null;img.color=img.name.Contains("Fill")?new Color(.85f,.05f,.07f):img.name.Contains("Handle")?new Color(1,.8f,.3f):new Color(.1f,.04f,.07f);EditorUtility.SetDirty(img);PrefabUtility.RecordPrefabInstancePropertyModifications(img);}
            }
        }
    }
    public static void Configure()
    {
        if(Application.isPlaying)throw new System.Exception("Stop Play first");var scene=SceneManager.GetActiveScene();
        var player=Object.FindFirstObjectByType<PlayerHealth>();var hud=GameObject.Find("PlayerUI").GetComponent<Canvas>();var audio=Object.FindFirstObjectByType<GameAudio>();
        var monkey=ImportSprite("Assets/UI_images/Monkey cheer.png",128);var banana=ImportSprite("Assets/UI_images/white-banana-fruit-seeds-for-planting-01.jpg",64);
        var popup=Get<CheerPopup>(audio.gameObject);if(popup.picture==null){var rt=Rect("Monkey cheer popup",hud.transform,new Vector2(105,145),Vector2.zero);var img=rt.gameObject.AddComponent<Image>();img.sprite=monkey;img.preserveAspect=true;img.raycastTarget=false;Get<CanvasGroup>(rt.gameObject);popup.picture=rt;rt.gameObject.SetActive(false);}popup.player=player.transform;popup.canvas=hud;audio.cheerPopup=popup;
        var boost=player.GetComponent<SpeedBoostAbility>();
        if(boost.inventoryText==null)
        {
            var rt=Rect("Stored Cokey bananas",hud.transform,new Vector2(110,125),new Vector2(-22,0));rt.anchorMin=rt.anchorMax=new Vector2(1,.37f);rt.pivot=new Vector2(1,.5f);Panel(rt);
            var icon=Rect("White banana icon",rt,new Vector2(72,72),new Vector2(0,15)).gameObject.AddComponent<Image>();icon.sprite=banana;icon.preserveAspect=true;icon.raycastTarget=false;
            boost.inventoryText=Text("Banana count",rt,"[R]  x0",new Vector2(100,30),new Vector2(0,-40),20);
        }
        EditorUtility.SetDirty(boost);PrefabUtility.RecordPrefabInstancePropertyModifications(boost);EditorUtility.SetDirty(audio);
        foreach(var path in new[]{"Assets/Assets/SceneStuff/Parts.prefab","Assets/Assets/SceneStuff/Ammo.prefab","Assets/Assets/SceneStuff/SuperBananaPickup.prefab","Assets/Models/Prefabs/Cocey banan Variant.prefab"})
        {
            var root=PrefabUtility.LoadPrefabContents(path);var motion=Get<LootMotion>(root);
            if(motion.visual==null)
            {
                if(path.Contains("Parts.prefab"))
                {
                    foreach(var renderer in root.GetComponentsInChildren<Renderer>())renderer.enabled=false;
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Prefabs/ScrapMetal.prefab"),root.transform);model.name="Scrap metal visual";model.transform.localPosition=Vector3.zero;
                    var bounds=new Bounds(model.transform.position,Vector3.zero);foreach(var renderer in model.GetComponentsInChildren<Renderer>())bounds.Encapsulate(renderer.bounds);model.transform.localScale*=.8f/Mathf.Max(.01f,Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)));motion.visual=model.transform;
                }
                else if(root.GetComponent<SuperBananaPickup>()!=null)motion.visual=root.GetComponent<SuperBananaPickup>().visual;
                else if(root.GetComponent<MeshRenderer>()!=null)
                {
                    var go=new GameObject("Spinning visual");go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=root.GetComponent<MeshFilter>().sharedMesh;go.AddComponent<MeshRenderer>().sharedMaterials=root.GetComponent<MeshRenderer>().sharedMaterials;root.GetComponent<MeshRenderer>().enabled=false;motion.visual=go.transform;
                }
                else if(root.transform.childCount>0)motion.visual=root.transform.GetChild(0);
            }
            foreach(var light in root.GetComponentsInChildren<Light>())light.enabled=false;
            var body=Get<Rigidbody>(root);body.isKinematic=true;body.useGravity=false;
            foreach(var col in root.GetComponentsInChildren<Collider>())col.isTrigger=true;
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        var shop=Object.FindFirstObjectByType<ShopManager>();var shopRect=shop.shopPanel.GetComponent<RectTransform>();shopRect.anchorMin=shopRect.anchorMax=new Vector2(.5f,.5f);shopRect.pivot=new Vector2(.5f,.5f);shopRect.anchoredPosition=Vector2.zero;shopRect.sizeDelta=new Vector2(700,440);
        foreach(var raw in shop.shopPanel.GetComponentsInChildren<RawImage>(true))raw.enabled=false;Panel(shopRect);
        if(shopRect.Find("Shop title")==null){Text("Shop title",shopRect,"KABU'S SHOP",new Vector2(640,55),new Vector2(0,160),38);Text("Shop hint",shopRect,"[E] / [ESC]  BACK TO ISLAND",new Vector2(640,35),new Vector2(0,-175),20);}
        var labels=new[]{shop.healthUpgradeButtonText,shop.fireRateUpgradeButtonText,shop.maxAmmoUpgradeButtonText};string[] names={"ButtonHp","ButtonFirerate","ButtonMaxAmmo"};
        for(int i=0;i<3;i++){var label=labels[i];label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(.5f,.5f);label.rectTransform.anchoredPosition=new Vector2(-115,75-i*92);label.rectTransform.sizeDelta=new Vector2(390,78);label.font=Font;label.fontSize=24;label.color=new Color(1,.94f,.75f);label.alignment=TextAlignmentOptions.MidlineLeft;var button=shopRect.GetComponentsInChildren<Button>(true).First(b=>b.name==names[i]);var rt=button.GetComponent<RectTransform>();rt.anchoredPosition=new Vector2(225,75-i*92);rt.sizeDelta=new Vector2(160,68);}
        var pause=Object.FindFirstObjectByType<PauseManager>();if(pause!=null){if(pause.pausePanel!=null)Panel(pause.pausePanel.transform);if(pause.settingsPanel!=null)Panel(pause.settingsPanel.transform);}
        Style(scene);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        foreach(var name in new[]{"MainMenu","DeathScene","VictoryScene"}){var menu=EditorSceneManager.OpenScene("Assets/"+name+".unity",OpenSceneMode.Additive);Style(menu);EditorSceneManager.MarkSceneDirty(menu);EditorSceneManager.SaveScene(menu);EditorSceneManager.CloseScene(menu,true);}
        SceneManager.SetActiveScene(scene);AssetDatabase.SaveAssets();
    }
}

