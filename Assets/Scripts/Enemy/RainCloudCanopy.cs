using UnityEngine;

/// <summary>A small, fixed particle budget makes broad, simple storm-cloud clumps.</summary>
public class RainCloudCanopy : MonoBehaviour
{
    public ParticleSystem particles;

    public void Initialize(Material material, Vector2 coverage, float puffSize)
    {
        particles = gameObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = false;
        main.startLifetime = 36000f;
        main.startSpeed = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 300;
        var emission = particles.emission; emission.enabled = false;
        var shape = particles.shape; shape.enabled = false;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        float size = Mathf.Max(20f, puffSize);
        int columns = Mathf.Clamp(Mathf.CeilToInt(coverage.x / (size * 1.7f)), 2, 10);
        int rows = Mathf.Clamp(Mathf.CeilToInt(coverage.y / (size * 1.7f)), 2, 10);
        var random = new System.Random(731);
        particles.Play();
        for (int z = 0; z < rows; z++)
        for (int x = 0; x < columns; x++)
        for (int lobe = 0; lobe < 3; lobe++)
        {
            float u = (x + .5f) / columns - .5f;
            float v = (z + .5f) / rows - .5f;
            float jitterX = ((float)random.NextDouble() - .5f) * size * .5f;
            float jitterZ = ((float)random.NextDouble() - .5f) * size * .5f;
            var puff = new ParticleSystem.EmitParams();
            puff.position = new Vector3(u * coverage.x + jitterX, (float)random.NextDouble() * 12f, v * coverage.y + jitterZ);
            puff.startSize = size * Mathf.Lerp(.8f, 1.2f, (float)random.NextDouble());
            puff.startColor = new Color(.24f, .28f, .33f, .8f);
            puff.startLifetime = 36000f;
            puff.velocity = Vector3.zero;
            particles.Emit(puff, 1);
        }
    }
}
