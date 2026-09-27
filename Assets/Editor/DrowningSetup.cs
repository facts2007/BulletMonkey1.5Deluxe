using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class DrowningSetup
{
    public static void Configure()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Stop Play mode first.");
        if(Object.FindFirstObjectByType<DrowningSequence>()!=null)throw new System.InvalidOperationException("Drowning setup already exists; edit its Inspector settings.");
        var root=new GameObject("Drowning — heavenly box");var sequence=root.AddComponent<DrowningSequence>();
        var room=new GameObject("Heavenly room");room.transform.SetParent(root.transform);room.transform.position=new Vector3(2500,100,0);sequence.heavenlyRoom=room;
        var white=Material("HeavenlyIvory",new Color(.92f,.89f,.78f));var gold=Material("HeavenlyGold",new Color(1,.72f,.21f));
        Cube("Cloud floor",room.transform,new Vector3(0,-.5f,0),new Vector3(24,1,24),white);
        Cube("Back wall",room.transform,new Vector3(0,7,10),new Vector3(24,15,1),white);
        Cube("Left wall",room.transform,new Vector3(-12,7,0),new Vector3(1,15,24),white);
        Cube("Right wall",room.transform,new Vector3(12,7,0),new Vector3(1,15,24),white);
        foreach(float x in new[]{-6f,6f})foreach(float z in new[]{-3f,6f})
        {
            var column=GameObject.CreatePrimitive(PrimitiveType.Cylinder);column.name="Pearly column";column.transform.SetParent(room.transform,false);column.transform.localPosition=new Vector3(x,5,z);column.transform.localScale=new Vector3(.8f,5,.8f);column.GetComponent<Renderer>().sharedMaterial=white;
            Cube("Golden capital",room.transform,new Vector3(x,10,z),new Vector3(1.5f,.3f,1.5f),gold);
        }
        var halo=new GameObject("Suspiciously official halo");halo.transform.SetParent(room.transform,false);halo.transform.localPosition=new Vector3(0,6,1);
        var line=halo.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=96;line.widthMultiplier=.12f;line.sharedMaterial=gold;
        for(int i=0;i<96;i++){float a=i*Mathf.PI*2/96;line.SetPosition(i,new Vector3(Mathf.Cos(a)*2.2f,0,Mathf.Sin(a)*2.2f));}
        var lightGo=new GameObject("Heavenly spotlight");lightGo.transform.SetParent(room.transform,false);lightGo.transform.localPosition=new Vector3(0,9,0);lightGo.transform.localRotation=Quaternion.Euler(90,0,0);
        var light=lightGo.AddComponent<Light>();light.type=LightType.Spot;light.color=new Color(1,.91f,.68f);light.intensity=14;light.range=20;light.spotAngle=65;light.shadows=LightShadows.Soft;
        var fill=new GameObject("Soft heavenly fill");fill.transform.SetParent(room.transform,false);fill.transform.localPosition=new Vector3(0,4,-5);var f=fill.AddComponent<Light>();f.type=LightType.Point;f.range=22;f.intensity=5;f.color=new Color(.78f,.88f,1);
        var arrival=new GameObject("Player arrival");arrival.transform.SetParent(room.transform,false);arrival.transform.localPosition=new Vector3(0,.8f,0);arrival.transform.localRotation=Quaternion.Euler(0,180,0);sequence.arrival=arrival.transform;
        var cameraObject=new GameObject("Afterlife camera");cameraObject.transform.SetParent(root.transform);cameraObject.transform.position=room.transform.position+new Vector3(0,3.5f,-8);cameraObject.transform.LookAt(room.transform.position+new Vector3(0,1.6f,0));
        var camera=cameraObject.AddComponent<Camera>();camera.fieldOfView=55;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.95f,.95f,1);cameraObject.AddComponent<AudioListener>();sequence.heavenlyCamera=camera;
        var ui=new GameObject("Drowning presentation",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));ui.transform.SetParent(root.transform);var canvas=ui.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=300;sequence.presentation=canvas;
        var scaler=ui.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var caption=Text("Heavenly message",ui.transform,sequence.message,42,new Color(1,.96f,.76f));Rect(caption.rectTransform,new Vector2(.12f,.73f),new Vector2(.88f,.95f));caption.fontStyle=FontStyles.Bold;sequence.caption=caption;
        var fade=new GameObject("Whiteout",typeof(RectTransform),typeof(Image));fade.transform.SetParent(ui.transform,false);Rect(fade.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);sequence.whiteFade=fade.GetComponent<Image>();sequence.whiteFade.color=new Color(1,1,1,0);sequence.whiteFade.raycastTarget=false;
        var panel=new GameObject("Death choices",typeof(RectTransform));panel.transform.SetParent(ui.transform,false);Rect(panel.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);sequence.deathPanel=panel;
        var title=Text("Ascended",panel.transform,"ASCENDED",76,new Color(.31f,.26f,.13f));Rect(title.rectTransform,new Vector2(.2f,.57f),new Vector2(.8f,.73f));title.fontStyle=FontStyles.Bold;
        var subtitle=Text("Epitaph",panel.transform,"Swimming lessons were not included.",27,new Color(.4f,.36f,.26f));Rect(subtitle.rectTransform,new Vector2(.2f,.47f),new Vector2(.8f,.57f));
        var retry=Button("Try again  [R]",panel.transform,new Vector2(.34f,.33f),new Vector2(.66f,.42f));UnityEventTools.AddPersistentListener(retry.onClick,sequence.Retry);
        var menu=Button("Main menu",panel.transform,new Vector2(.34f,.22f),new Vector2(.66f,.30f));UnityEventTools.AddPersistentListener(menu.onClick,sequence.MainMenu);
        var water=GameObject.Find("mappy/water")??GameObject.Find("water");var bounds=water.GetComponent<Collider>().bounds;
        var trigger=new GameObject("Drowning water volume");trigger.transform.SetParent(water.transform,true);trigger.transform.position=new Vector3(bounds.center.x,bounds.max.y-25,bounds.center.z);trigger.transform.rotation=Quaternion.identity;
        // Use world dimensions even if the imported water mesh has a scaled/rotated parent.
        trigger.transform.SetParent(root.transform,true);
        var box=trigger.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(bounds.size.x,50.4f,bounds.size.z);
        var rb=trigger.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
        trigger.AddComponent<WaterDrowningTrigger>().sequence=sequence;
        room.SetActive(false);cameraObject.SetActive(false);ui.SetActive(false);panel.SetActive(false);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);Selection.activeGameObject=root;
    }
    private static Material Material(string name,Color color)
    {
        string path="Assets/Assets/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=color;AssetDatabase.CreateAsset(mat,path);}return mat;
    }
    private static void Cube(string name,Transform parent,Vector3 position,Vector3 scale,Material mat)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;
    }
    private static void Rect(RectTransform r,Vector2 min,Vector2 max){r.anchorMin=min;r.anchorMax=max;r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;}
    private static TextMeshProUGUI Text(string name,Transform parent,string value,int size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TextMeshProUGUI>();text.text=value;text.fontSize=size;text.color=color;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;
    }
    private static Button Button(string title,Transform parent,Vector2 min,Vector2 max)
    {
        var go=new GameObject(title,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);Rect(go.GetComponent<RectTransform>(),min,max);go.GetComponent<Image>().color=new Color(.85f,.79f,.61f);var button=go.GetComponent<Button>();button.targetGraphic=go.GetComponent<Image>();var t=Text("Label",go.transform,title,30,new Color(.22f,.19f,.12f));Rect(t.rectTransform,Vector2.zero,Vector2.one);return button;
    }
}
