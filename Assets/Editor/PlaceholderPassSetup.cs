using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

public static class PlaceholderPassSetup
{
    static TMP_FontAsset Font=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset");
    static T Get<T>(GameObject go)where T:Component{var c=go.GetComponent<T>();return c!=null?c:go.AddComponent<T>();}
    static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 pos){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.sizeDelta=size;rt.anchoredPosition=pos;return rt;}
    static TextMeshProUGUI Text(string name,Transform parent,string content,Vector2 size,Vector2 pos,int fontSize){var rt=Rect(name,parent,size,pos);var t=rt.gameObject.AddComponent<TextMeshProUGUI>();t.font=Font;t.fontSize=fontSize;t.text=content;t.color=new Color(1,.94f,.75f);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
    static Button Button(string name,Transform parent,string text,Vector2 size,Vector2 pos,UnityEngine.Events.UnityAction action){var rt=Rect(name,parent,size,pos);var panel=rt.gameObject.AddComponent<RetroPanelGraphic>();panel.raycastTarget=true;var b=rt.gameObject.AddComponent<Button>();b.targetGraphic=panel;var label=Text("Label",rt,text,size-Vector2.one*12,Vector2.zero,20);UnityEventTools.AddPersistentListener(b.onClick,action);return b;}
    static Material Material(string name,Color color,bool unlit=false){string path="Assets/Assets/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;return m;}
    static GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 pos,Vector3 scale,Material mat,bool collider=true){var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
    static void PlaceholderState(AnimatorController controller,string name,Motion fallback){var machine=controller.layers[0].stateMachine;if(machine.states.Any(s=>s.state.name==name))return;machine.AddState(name).motion=fallback;EditorUtility.SetDirty(controller);}
    public static string ConfigureMain()
    {
        if(Application.isPlaying)throw new System.Exception("Stop Play first");var scene=SceneManager.GetActiveScene();if(scene.name!="MainScene")throw new System.Exception("Open MainScene");
        var player=Object.FindFirstObjectByType<PlayerHealth>();var gun=Object.FindFirstObjectByType<Gun>();gun.range=300;EditorUtility.SetDirty(gun);PrefabUtility.RecordPrefabInstancePropertyModifications(gun);
        var bulletPath=AssetDatabase.GetAssetPath(gun.bulletPrefab);var bullet=PrefabUtility.LoadPrefabContents(bulletPath);bullet.GetComponent<Bullet>().lifetime=6;PrefabUtility.SaveAsPrefabAsset(bullet,bulletPath);PrefabUtility.UnloadPrefabContents(bullet);
        var controller=(AnimatorController)player.GetComponent<CharacterAnimationDriver>().animator.runtimeAnimatorController;
        var states=controller.layers[0].stateMachine.states;PlaceholderState(controller,"Jump",states.First(s=>s.state.name=="Idle").state.motion);PlaceholderState(controller,"CokeyRun",states.First(s=>s.state.name=="Walk").state.motion);
        var pause=Object.FindFirstObjectByType<PauseManager>();var tutorial=Get<TutorialOverlay>(pause.gameObject);tutorial.pause=pause;
        if(tutorial.panel==null)
        {
            var go=new GameObject("Tutorial overlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=300;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,608);scaler.matchWidthOrHeight=.5f;
            var board=Rect("Tutorial placeholder",go.transform,new Vector2(820,520),Vector2.zero);board.gameObject.AddComponent<RetroPanelGraphic>();
            Text("Title",board,"HOW TO MONKEY",new Vector2(680,50),new Vector2(0,205),30);
            var instructions=Text("Instructions — replace text later",board,"WASD  MOVE     MOUSE  AIM     SPACE  JUMP\nLEFT CLICK  SHOOT — STOMP SMALL ENEMIES BY LANDING ON THEM\n\nCOLLECT SCRAP, AMMO AND BANANAS FROM ENEMIES.\nF  HOLD TO CHARGE A SUPER BANANA, THEN UNLEASH IT.\nR  USE A STORED COKEY BANANA: DOUBLE MOVEMENT FOR 10s.\nT, THEN T AGAIN  RESCUE TO AN ISLAND CENTRE (-10 HP).\n\nENTER EACH ISLAND TO START ITS WAVES. CLEAR THEM TO OPEN MIST.\nPICK UP THE BUCKET AFTER AN ISLAND; E PUTS OUT KABU'S FIRE.\nE AGAIN OPENS HIS SHOP. BUY AMMO AND FOUR TIERS OF UPGRADES.\nALL UPGRADES UNLOCK A SECRET DUNGEON BUTTON IN THE SHOP.\n\nESC  PAUSE / SETTINGS / REOPEN THIS GUIDE\nTHE FINAL BOAT TAKES YOU TO VICTORY. DON'T TOUCH THE WATER!",new Vector2(750,360),new Vector2(0,-10),18);instructions.alignment=TextAlignmentOptions.MidlineLeft;
            Button("Close tutorial X",board,"X",new Vector2(48,44),new Vector2(365,218),tutorial.Close);
            tutorial.panel=go;go.SetActive(false);
        }
        if(pause.pausePanel.transform.Find("TutorialButton")==null){var b=Button("TutorialButton",pause.pausePanel.transform,"HOW TO PLAY",new Vector2(400,50),new Vector2(0,35),tutorial.Open);pause.menuButtons=pause.menuButtons.Concat(new[]{b.gameObject}).ToArray();}
        string[] buttons={"ContinueButton","TutorialButton","SettingsButton","BackToMenuButton","QuitGameButton"};for(int i=0;i<buttons.Length;i++){var rt=pause.pausePanel.transform.Find(buttons[i]) as RectTransform;rt.anchoredPosition=new Vector2(0,110-i*65);rt.sizeDelta=new Vector2(400,50);}
        (pause.pausePanel.transform.Find("Retro frame") as RectTransform).sizeDelta=new Vector2(640,510);(pause.pauseTitle.transform as RectTransform).anchoredPosition=new Vector2(0,200);
        var shop=Object.FindFirstObjectByType<ShopManager>();var root=shop.shopPanel.transform;(root.Find("Retro frame") as RectTransform).sizeDelta=new Vector2(700,580);(root.Find("Shop title") as RectTransform).anchoredPosition=new Vector2(0,240);(root.Find("Shop hint") as RectTransform).anchoredPosition=new Vector2(0,-260);
        var labels=new[]{shop.healthUpgradeButtonText,shop.fireRateUpgradeButtonText,shop.maxAmmoUpgradeButtonText};string[] buy={"ButtonHp","ButtonFirerate","ButtonMaxAmmo"};for(int i=0;i<3;i++){labels[i].rectTransform.anchoredPosition=new Vector2(-115,140-i*80);(root.GetComponentsInChildren<Button>(true).First(b=>b.name==buy[i]).transform as RectTransform).anchoredPosition=new Vector2(225,140-i*80);}
        if(shop.ammoPurchaseLabel==null){shop.ammoPurchaseLabel=Text("Ammo purchase label",root,"AMMO",new Vector2(390,60),new Vector2(-115,-105),18);shop.ammoPurchaseLabel.alignment=TextAlignmentOptions.MidlineLeft;Button("BuyAmmo",root,"BUY AMMO",new Vector2(160,60),new Vector2(225,-105),shop.BuyAmmo);}
        if(shop.secretDungeonButton==null){shop.secretDungeonButton=Button("SecretDungeon",root,"??? MEGA BONUS DUNGEON ???",new Vector2(600,50),new Vector2(0,-190),shop.EnterSecretDungeon).gameObject;shop.secretDungeonButton.SetActive(false);}
        EditorUtility.SetDirty(shop);EditorUtility.SetDirty(pause);EditorUtility.SetDirty(tutorial);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return "Main gameplay, shop and tutorial placeholders saved";
    }
    public static string CreateDungeon()
    {
        if(Application.isPlaying)throw new System.Exception("Stop Play first");
        const string path="Assets/DungeonLevel.unity";if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)!=null)throw new System.Exception("Dungeon scene already exists; edit it instead of overwriting.");
        var main=SceneManager.GetActiveScene();EditorSceneManager.SaveScene(main,path,true);var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
        string[] keep={"Player","PlayerUI","Game Audio ","EventSystem","PauseManager","PauseMenu","Directional Light","Shared spawn fog","Tutorial overlay"};
        foreach(var root in scene.GetRootGameObjects())if(!keep.Contains(root.name))Object.DestroyImmediate(root);
        var player=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PlayerHealth>(true)).First();var audio=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameAudio>(true)).First();
        var tutorial=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TutorialOverlay>(true)).First();tutorial.autoOpen=false;
        player.transform.SetPositionAndRotation(new Vector3(0,2,-36),Quaternion.identity);
        var arena=new GameObject("DUNGEON — roofless arena placeholder");var encounter=arena.AddComponent<DungeonEncounter>();
        var stone=Material("DungeonStone",new Color(.22f,.2f,.27f));var floor=Material("DungeonFloor",new Color(.32f,.29f,.35f));var evil=Material("EvilKabuPlaceholder",new Color(.24f,.035f,.09f));var gold=Material("DungeonKeyGold",new Color(1,.7f,.08f));var danger=Material("DungeonAttackRed",new Color(1,.03f,.04f),true);
        Shape("Floor",PrimitiveType.Cube,arena.transform,new Vector3(0,-1,0),new Vector3(72,2,100),floor);
        Shape("Left wall",PrimitiveType.Cube,arena.transform,new Vector3(-37,6,0),new Vector3(2,12,102),stone);Shape("Right wall",PrimitiveType.Cube,arena.transform,new Vector3(37,6,0),new Vector3(2,12,102),stone);Shape("Entry wall",PrimitiveType.Cube,arena.transform,new Vector3(0,6,-51),new Vector3(76,12,2),stone);
        Shape("Exit wall left",PrimitiveType.Cube,arena.transform,new Vector3(-22,6,51),new Vector3(32,12,2),stone);Shape("Exit wall right",PrimitiveType.Cube,arena.transform,new Vector3(22,6,51),new Vector3(32,12,2),stone);
        Shape("Outside landing",PrimitiveType.Cube,arena.transform,new Vector3(0,-1,72),new Vector3(36,2,44),floor);Shape("Outside left rail",PrimitiveType.Cube,arena.transform,new Vector3(-18,3,72),new Vector3(1,6,44),stone);Shape("Outside right rail",PrimitiveType.Cube,arena.transform,new Vector3(18,3,72),new Vector3(1,6,44),stone);Shape("Outside far rail",PrimitiveType.Cube,arena.transform,new Vector3(0,3,94),new Vector3(36,6,1),stone);
        var rescue=new GameObject("Dungeon rescue center").transform;rescue.SetParent(arena.transform);rescue.position=new Vector3(0,.1f,-35);encounter.rescueCenter=rescue;player.GetComponent<PlayerUnstuck>().islandCenters=new[]{rescue};
        // Bake only walkable arena geometry before adding temporary boss/door colliders.
        var surface=arena.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.BuildNavMesh();
        var boss=new GameObject("Evil Kabu — cylinder placeholder");boss.transform.SetParent(arena.transform);boss.transform.position=new Vector3(0,0,18);
        encounter.boss=boss.AddComponent<EnemyHealth>();encounter.boss.maxHealth=encounter.boss.currentHealth=2000;encounter.boss.pointValue=500;
        encounter.visual=Shape("Visual — replace with Evil Kabu model",PrimitiveType.Cylinder,boss.transform,Vector3.up*5,new Vector3(8,5,8),evil).transform;
        var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/SceneStuff/ImptronPlaceholder.prefab");var bar=Object.Instantiate(original.transform.Find("Healthbar").gameObject,boss.transform);bar.name="Healthbar";bar.transform.localPosition=Vector3.up*12;
        var graphic=bar.GetComponentInChildren<PixelHealthGraphic>(true);graphic.enemy=encounter.boss;graphic.player=null;graphic.playerDecoration=false;
        encounter.boss.healthBar=bar.transform.Find("Health") as RectTransform;encounter.boss.redBar=bar.transform.Find("Red") as RectTransform;encounter.boss.healthText=bar.GetComponentInChildren<TextMeshProUGUI>(true);
        string controllerPath="Assets/animation/Combat/EvilKabuPlaceholder.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);if(controller==null){controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);foreach(var name in new[]{"Idle","RockThrow","MegaPunch","BellyBonk","Defeated"}){var clip=new AnimationClip();clip.name="Evil Kabu "+name+" PLACEHOLDER";clip.frameRate=30;clip.SetCurve("Visual — replace with Evil Kabu model",typeof(Transform),"m_LocalPosition.y",AnimationCurve.Constant(0,1,5));AssetDatabase.CreateAsset(clip,"Assets/animation/Combat/EvilKabu"+name+"Placeholder.anim");controller.layers[0].stateMachine.AddState(name).motion=clip;}}
        encounter.animator=boss.AddComponent<Animator>();encounter.animator.runtimeAnimatorController=controller;
        encounter.circleIndicator=Shape("RED circle attack telegraph",PrimitiveType.Cylinder,arena.transform,Vector3.zero,new Vector3(8,.035f,8),danger,false).transform;encounter.rectangleIndicator=Shape("RED punch attack telegraph",PrimitiveType.Cube,arena.transform,Vector3.zero,new Vector3(7,.05f,14),danger,false).transform;encounter.rockMaterial=stone;
        encounter.circleIndicator.gameObject.SetActive(false);encounter.rectangleIndicator.gameObject.SetActive(false);
        var key=new GameObject("Boss key pickup placeholder");key.transform.SetParent(arena.transform);var keyVisual=new GameObject("Spinning key visual").transform;keyVisual.SetParent(key.transform,false);
        Shape("Key head",PrimitiveType.Sphere,keyVisual,new Vector3(0,.7f,0),new Vector3(.7f,.7f,.25f),gold,false);Shape("Key shaft",PrimitiveType.Cube,keyVisual,Vector3.zero,new Vector3(.16f,1.1f,.2f),gold,false);Shape("Key tooth",PrimitiveType.Cube,keyVisual,new Vector3(.2f,-.4f,0),new Vector3(.5f,.15f,.2f),gold,false);
        var trigger=key.AddComponent<SphereCollider>();trigger.radius=1.4f;trigger.isTrigger=true;var rb=key.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;key.AddComponent<DungeonKeyPickup>().encounter=encounter;key.AddComponent<LootMotion>().visual=keyVisual;encounter.keyPickup=key;key.SetActive(false);
        encounter.door=Shape("Locked heavenly exit door",PrimitiveType.Cube,arena.transform,new Vector3(0,5,51),new Vector3(12,10,2),gold).transform;
        var doorway=new GameObject("Key door approach trigger");doorway.transform.SetParent(arena.transform);doorway.transform.position=new Vector3(0,2,46);var box=doorway.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(12,6,7);var doorBody=doorway.AddComponent<Rigidbody>();doorBody.isKinematic=true;doorBody.useGravity=false;doorway.AddComponent<DungeonDoorTrigger>().encounter=encounter;
        var cameraGo=new GameObject("Dungeon door cinematic camera");cameraGo.transform.SetParent(arena.transform);cameraGo.transform.position=new Vector3(16,13,32);cameraGo.transform.LookAt(encounter.door.position);encounter.doorCamera=cameraGo.AddComponent<Camera>();encounter.doorCamera.enabled=false;var retro=cameraGo.AddComponent<RetroCamera>();retro.verticalResolution=400;retro.pixelMaterial=player.GetComponentInChildren<RetroCamera>().pixelMaterial;retro.enabled=false;
        var boat=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Prefabs/boat 1.prefab"));boat.name="Dungeon victory boat";boat.transform.SetParent(arena.transform);boat.transform.position=new Vector3(0,1,82);var loader=Get<ModelSceneLoader>(boat);loader.targetSceneName="VictoryScene";var boatBox=Get<BoxCollider>(boat);boatBox.isTrigger=true;boatBox.size=new Vector3(5,4,7);Get<Rigidbody>(boat).isKinematic=true;
        var ui=scene.GetRootGameObjects().First(r=>r.name=="PlayerUI");encounter.statusText=Text("Dungeon status",ui.transform,"EVIL KABU",new Vector2(900,50),Vector2.zero,18);encounter.statusText.rectTransform.anchorMin=encounter.statusText.rectTransform.anchorMax=new Vector2(.5f,.14f);
        foreach(var t in ui.GetComponentsInChildren<TMP_Text>(true))if(t.name.ToLower().Contains("countdown"))t.text="";
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(main);
        if(!EditorBuildSettings.scenes.Any(s=>s.path==path))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(path,true)}).ToArray();AssetDatabase.SaveAssets();return "DungeonLevel created with arena, 3-attack boss, key, door cinematic and victory boat";
    }
}
