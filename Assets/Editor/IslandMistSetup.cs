using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class IslandMistSetup
{
    public static void Configure()
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Assets/Materials/IslandMist.mat");
        if(material==null)
        {
            material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Assets/Materials/ParticleDark.mat"));
            material.name="IslandMist";material.DisableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);material.SetColor("_BaseColor",Color.white);
            AssetDatabase.CreateAsset(material,"Assets/Assets/Materials/IslandMist.mat");
        }
        foreach(var area in Object.FindObjectsByType<WaveArea>(FindObjectsSortMode.None))
        {
            var wall=area.pathBlocker.transform;
            if(wall.Find("Mist particles")!=null)continue;
            var go=new GameObject("Mist particles");go.transform.SetParent(wall,false);
            go.transform.localScale=new Vector3(1/wall.lossyScale.x,1/wall.lossyScale.y,1/wall.lossyScale.z);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=true;main.prewarm=true;main.startLifetime=3;main.startSpeed=.4f;
            main.startSize=new ParticleSystem.MinMaxCurve(3,5);main.startColor=new Color(.65f,.85f,.92f,.4f);
            main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.maxParticles=250;main.simulationSpace=ParticleSystemSimulationSpace.Local;
            var emission=ps.emission;emission.rateOverTime=area.name=="Island5"?65:35;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(wall.lossyScale.x,11,1);
            var color=ps.colorOverLifetime;color.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.2f),new GradientAlphaKey(1,.7f),new GradientAlphaKey(0,1)});color.color=gradient;
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
            ps.Play();
        }
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}

