using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Camera))]
public class RetroCamera : MonoBehaviour
{
    [Range(120,720)] public int verticalResolution=240;
    public Material pixelMaterial;
    private Camera source,output;
    private RenderTexture pixels;
    private GameObject presentation;
    private RawImage image;
    private void OnEnable()
    {
        source=GetComponent<Camera>();
        if(presentation==null)
        {
            presentation=new GameObject("Retro camera presentation",typeof(Canvas));
            var canvas=presentation.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=-1000;
            var go=new GameObject("Point sampled pixels",typeof(RectTransform),typeof(RawImage));go.transform.SetParent(presentation.transform,false);
            image=go.GetComponent<RawImage>();image.raycastTarget=false;image.material=pixelMaterial;
            image.rectTransform.anchorMin=Vector2.zero;image.rectTransform.anchorMax=Vector2.one;image.rectTransform.offsetMin=image.rectTransform.offsetMax=Vector2.zero;
            var cameraGo=new GameObject("Retro display camera");output=cameraGo.AddComponent<Camera>();output.cullingMask=0;output.clearFlags=CameraClearFlags.SolidColor;output.backgroundColor=Color.black;output.depth=source.depth-1;
        }
        presentation.SetActive(true);output.gameObject.SetActive(true);Resize();
    }
    private void Resize()
    {
        int h=Mathf.Max(120,verticalResolution),w=Mathf.RoundToInt(h*(float)Screen.width/Mathf.Max(1,Screen.height));
        if(pixels!=null && pixels.width==w && pixels.height==h)return;
        if(pixels!=null){source.targetTexture=null;pixels.Release();Destroy(pixels);}
        pixels=new RenderTexture(w,h,24){filterMode=FilterMode.Point,antiAliasing=1};pixels.Create();source.targetTexture=pixels;image.texture=pixels;
    }
    private void LateUpdate(){Resize();}
    private void OnDisable(){if(source!=null)source.targetTexture=null;if(presentation!=null)presentation.SetActive(false);if(output!=null)output.gameObject.SetActive(false);}
    private void OnDestroy(){if(pixels!=null){pixels.Release();Destroy(pixels);}if(presentation!=null)Destroy(presentation);if(output!=null)Destroy(output.gameObject);}
}
