using System.Collections.Generic;
using UnityEngine;

// Samples the warning once when placed; no per-frame terrain raycasts.
public class GroundAttackIndicator : MonoBehaviour
{
    private Mesh ownedMesh;
    private static readonly RaycastHit[] hits=new RaycastHit[64];
    public static GameObject Create(bool circle,Vector3 center,Quaternion rotation,float radius,Vector2 size,Material material,Transform owner,float lift)
    {
        var go=new GameObject(circle?"BossIndicator_Circle":"BossIndicator_Box");
        go.transform.SetPositionAndRotation(center,rotation);
        var component=go.AddComponent<GroundAttackIndicator>();
        var vertices=new List<Vector3>();var valid=new List<bool>();var triangles=new List<int>();
        System.Action<float,float> add=(x,z)=>
        {
            Vector3 world=center+rotation*new Vector3(x,0,z);
            int count=Physics.RaycastNonAlloc(world+Vector3.up*30,Vector3.down,hits,230,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            float closest=float.PositiveInfinity;Vector3 point=world;bool found=false;
            for(int i=0;i<count;i++)
            {
                var collider=hits[i].collider;
                if(collider.transform.IsChildOf(owner)||collider.GetComponentInParent<PlayerHealth>()!=null||collider.GetComponentInParent<EnemyHealth>()!=null||collider.GetComponentInParent<LootMotion>()!=null)continue;
                if(hits[i].normal.y<.2f||hits[i].distance>=closest)continue;
                closest=hits[i].distance;point=hits[i].point+Vector3.up*Mathf.Max(.12f,lift);found=true;
            }
            vertices.Add(go.transform.InverseTransformPoint(point));valid.Add(found);
        };
        System.Action<int,int,int> tri=(a,b,c)=>
        {
            if(!valid[a]||!valid[b]||!valid[c])return;
            // Do not stretch a warning down cliff faces or across missing ground.
            float low=Mathf.Min(vertices[a].y,Mathf.Min(vertices[b].y,vertices[c].y));
            float high=Mathf.Max(vertices[a].y,Mathf.Max(vertices[b].y,vertices[c].y));
            if(high-low>3)return;
            triangles.Add(a);triangles.Add(b);triangles.Add(c);
        };
        if(circle)
        {
            int rings=Mathf.Clamp(Mathf.CeilToInt(radius/.65f),1,40),segments=Mathf.Clamp(Mathf.CeilToInt(2*Mathf.PI*radius/.65f),16,128);
            add(0,0);
            for(int r=1;r<=rings;r++)for(int s=0;s<segments;s++){float angle=s*2*Mathf.PI/segments;add(Mathf.Cos(angle)*radius*r/rings,Mathf.Sin(angle)*radius*r/rings);}
            for(int s=0;s<segments;s++)tri(0,1+(s+1)%segments,1+s);
            for(int r=1;r<rings;r++)for(int s=0;s<segments;s++)
            {
                int a=1+(r-1)*segments+s,b=1+(r-1)*segments+(s+1)%segments,c=1+r*segments+s,d=1+r*segments+(s+1)%segments;
                tri(a,b,c);tri(b,d,c);
            }
        }
        else
        {
            int nx=Mathf.Clamp(Mathf.CeilToInt(size.x/.65f),1,128),nz=Mathf.Clamp(Mathf.CeilToInt(size.y/.65f),1,192);
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)add((float)x/nx*size.x-size.x*.5f,(float)z/nz*size.y-size.y*.5f);
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int a=z*(nx+1)+x,b=a+nx+1;tri(a,b,a+1);tri(a+1,b,b+1);}
        }
        component.ownedMesh=new Mesh{name="Ground conforming attack warning"};
        component.ownedMesh.SetVertices(vertices);component.ownedMesh.SetTriangles(triangles,0);component.ownedMesh.RecalculateNormals();component.ownedMesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh=component.ownedMesh;
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        return go;
    }
    private void OnDestroy(){if(ownedMesh!=null)Destroy(ownedMesh);}
}
