using UnityEngine;
using UnityEngine.UI;

public class CheerPopup : MonoBehaviour
{
    public RectTransform picture;
    public Canvas canvas;
    public Transform player;
    public float duration=1.4f;
    private float remaining;
    private CanvasGroup group;
    private SkinnedMeshRenderer body;
    private void Awake(){body=player.GetComponentInChildren<SkinnedMeshRenderer>();group=picture.GetComponent<CanvasGroup>();picture.gameObject.SetActive(false);}
    public void Show(){remaining=duration;picture.gameObject.SetActive(true);}
    private void LateUpdate()
    {
        if(remaining<=0)return;remaining-=Time.deltaTime;
        if(remaining<=0){picture.gameObject.SetActive(false);return;}
        var camera=Camera.main;if(camera==null)return;
        Vector3 anchor=body!=null?body.bounds.center+Vector3.up*(body.bounds.extents.y*.35f)+camera.transform.right*(body.bounds.extents.magnitude+.5f):player.position+Vector3.up*3+camera.transform.right*3;
        Vector3 screen=camera.WorldToScreenPoint(anchor);
        if(screen.z<=0){group.alpha=0;return;}
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var point);
        float t=1-remaining/duration;picture.anchoredPosition=point+new Vector2(180,70)+Vector2.up*(30*t+Mathf.Sin(t*14)*8);
        picture.localScale=Vector3.one*(t<.18f?Mathf.Lerp(.15f,1.15f,t/.18f):1+.1f*Mathf.Sin(t*22));picture.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*28)*12);group.alpha=Mathf.Clamp01(remaining/.3f);
    }
}


