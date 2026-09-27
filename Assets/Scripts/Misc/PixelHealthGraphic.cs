using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class PixelHealthGraphic : MaskableGraphic
{
    public PlayerHealth player;
    public EnemyHealth enemy;
    public bool playerDecoration;
    private float shown=1;
    public float DisplayedFraction=>shown;
    protected override void OnEnable(){base.OnEnable();raycastTarget=false;shown=Fraction();SetVerticesDirty();}
    private float Fraction(){if(player!=null)return Mathf.Clamp01((float)player.currentHealth/Mathf.Max(1,player.maxHealth));if(enemy!=null)return Mathf.Clamp01((float)enemy.currentHealth/Mathf.Max(1,enemy.maxHealth));return 1;}
    private void Update(){float target=Application.isPlaying?Fraction():1;float next=Application.isPlaying?Mathf.MoveTowards(shown,target,Time.deltaTime*3):target;if(!Mathf.Approximately(next,shown)){shown=next;SetVerticesDirty();}}
    private void Quad(VertexHelper vh,float x,float y,float w,float h,Color tint)
    {
        if(w<=0||h<=0)return;var r=rectTransform.rect;float sx=r.width/82,sy=r.height/(playerDecoration?30:14);int i=vh.currentVertCount;
        vh.AddVert(new Vector3(r.x+x*sx,r.y+y*sy),tint,Vector2.zero);vh.AddVert(new Vector3(r.x+x*sx,r.y+(y+h)*sy),tint,Vector2.zero);vh.AddVert(new Vector3(r.x+(x+w)*sx,r.y+(y+h)*sy),tint,Vector2.zero);vh.AddVert(new Vector3(r.x+(x+w)*sx,r.y+y*sy),tint,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
    }
    private void Bar(VertexHelper vh,float x,float y,float width)
    {
        Quad(vh,x,y+2,width,10,Color.black);Quad(vh,x+2,y,width-5,14,Color.black);
        Quad(vh,x+3,y+3,width-7,8,new Color(.23f,.025f,.035f));
        float fill=Mathf.Round((width-7)*shown);Quad(vh,x+3,y+3,fill,8,new Color(.82f,.025f,.045f));Quad(vh,x+3,y+8,fill,2,new Color(1,.24f,.25f));Quad(vh,x+3,y+3,fill,2,new Color(.48f,0,.02f));
        for(float s=12;s<fill;s+=12)Quad(vh,x+3+s,y+3,Mathf.Min(2,fill-s),8,new Color(.4f,.015f,.025f));
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();if(!playerDecoration){Bar(vh,0,0,82);return;}
        Bar(vh,18,2,64);
        string[] heart={"..BBB...BBB...",".BRRRB.BRRRB..","BRRRRRBRRRRRB.","BRRWWRRRRRRRB.","BRRWRRRRRRRRB.","BRRRRRRRRRRRB.",".BRRRRRRRRRB..","..BRRRRRRRB...","...BRRRRRB....","....BRRRB.....",".....BRB......","......B......."};
        for(int row=0;row<heart.Length;row++)for(int col=0;col<heart[row].Length;col++){char c=heart[row][col];if(c=='.')continue;Color tint=c=='B'?Color.black:c=='W'?new Color(1,.7f,.65f):row>7?new Color(.65f,0,.025f):new Color(.96f,.025f,.04f);Quad(vh,col*1.7f,(12-row)*1.7f,1.7f,1.7f,tint);}
        Quad(vh,23,18,23,12,Color.black);
        string[] hp={"10101110","10101001","11101110","10101000","10101000"};
        for(int row=0;row<5;row++)for(int col=0;col<8;col++)if(hp[row][col]=='1')Quad(vh,25+col*2.3f,19+(4-row)*2,2.3f,2,new Color(.78f,.8f,.79f));
    }
}
