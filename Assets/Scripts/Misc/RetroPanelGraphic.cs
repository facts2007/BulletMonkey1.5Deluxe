using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(CanvasRenderer))]
public class RetroPanelGraphic : MaskableGraphic
{
    public float border=4;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=rectTransform.rect;
        Box(vh,r,Color.black);Box(vh,new Rect(r.x+border,r.y+border,r.width-2*border,r.height-2*border),new Color(.72f,.37f,.16f)*color);
        Box(vh,new Rect(r.x+2*border,r.y+2*border,r.width-4*border,r.height-4*border),new Color(.13f,.085f,.12f)*color);
        Box(vh,new Rect(r.x+border,r.yMax-2*border,r.width-2*border,border),new Color(1,.76f,.35f)*color);
    }
    void Box(VertexHelper vh,Rect r,Color c){if(r.width<=0||r.height<=0)return;int i=vh.currentVertCount;vh.AddVert(new Vector3(r.xMin,r.yMin),c,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),c,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMax),c,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMin),c,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
}

