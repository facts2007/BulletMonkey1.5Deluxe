using UnityEngine;

/// <summary>One bounded cloud grows with the number of imps absorbed.</summary>
public class FusionCloud : MonoBehaviour
{
    public ParticleSystem particles;
    public float Progress {get;private set;}
    public void Initialize(Material material)
    {
        particles=gameObject.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=particles.main;main.loop=true;main.startLifetime=1.4f;main.startSpeed=.35f;main.startSize=new ParticleSystem.MinMaxCurve(.5f,1);main.maxParticles=100;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.startColor=new Color(.8f,.86f,.92f,.8f);
        var emission=particles.emission;emission.rateOverTime=55;
        var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.7f;
        var color=particles.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(0,1)});color.color=gradient;
        var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        SetProgress(0);particles.Play();
    }
    public void SetProgress(float value){Progress=Mathf.Clamp01(value);transform.localScale=Vector3.one*Mathf.Lerp(.5f,12,Mathf.SmoothStep(0,1,Progress));}
    public void Finish(){particles.Stop(true,ParticleSystemStopBehavior.StopEmitting);Destroy(gameObject,2);}
}
