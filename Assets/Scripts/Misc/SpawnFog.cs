using UnityEngine;

/// <summary>Bounded particle pool shared by enemy arrivals and the fusion scene.</summary>
public class SpawnFog : MonoBehaviour
{
    public Material material;
    private static SpawnFog instance;
    private readonly ParticleSystem[] pool = new ParticleSystem[8];
    private int cursor;
    private void Awake(){instance=this;}
    public static void Poof(Vector3 position, float size=1f)
    {
        if(instance!=null)instance.Emit(position,size);
    }
    private void Emit(Vector3 position,float size)
    {
        int slot=cursor++%pool.Length;
        var ps=pool[slot];
        if(ps==null)
        {
            var go=new GameObject("Pooled arrival cloud");go.transform.SetParent(transform);
            ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=false;main.playOnAwake=false;main.duration=1.2f;main.startLifetime=1.1f;main.maxParticles=48;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission;emission.enabled=false;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.6f;
            var color=ps.colorOverLifetime;color.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.6f,.7f,.8f),1)},new[]{new GradientAlphaKey(.7f,0),new GradientAlphaKey(0,1)});color.color=gradient;
            var scale=ps.sizeOverLifetime;scale.enabled=true;scale.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.5f,1,1.7f));
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            pool[slot]=ps;
        }
        ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.transform.position=position+Vector3.up*.6f;
        var settings=ps.main;settings.startSize=new ParticleSystem.MinMaxCurve(.7f*size,1.6f*size);settings.startSpeed=new ParticleSystem.MinMaxCurve(.5f*size,2*size);
        ps.Play();ps.Emit(size>2?40:16);
    }
    private void OnDestroy(){if(instance==this)instance=null;}
}
