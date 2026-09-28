// N64JungleLighting.cs
// Place in: Assets/Scripts/
// Requires: Post Processing package (Window -> Package Manager -> Post Processing -> Install)
// Right-click component header -> "Apply N64 Lighting"

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class N64JungleLighting : MonoBehaviour
{
    [Header("Sun")]
    public Color sunColor = new Color(1.00f, 0.62f, 0.18f);
    public float sunIntensity = 2.4f;
    public Vector3 sunRotation = new Vector3(52f, -30f, 0f);

    [Header("Sky Fill Light")]
    public Color skyFillColor = new Color(0.290f, 0.420f, 0.541f);
    public float skyFillIntensity = 0.12f;
    public Vector3 skyFillRotation = new Vector3(-40f, 150f, 0f);

    [Header("Ground Fill Light")]
    public Color groundFillColor = new Color(0.180f, 0.290f, 0.102f);
    public float groundFillIntensity = 0.08f;
    public Vector3 groundFillRotation = new Vector3(90f, 0f, 0f);

    [Header("Shadows")]
    public LightShadows shadowType = LightShadows.Hard;
    public float shadowStrength = 0.85f;
    public float shadowBias = 0.02f;
    public float shadowNormalBias = 0.2f;

    [Header("Ambient")]
    public Color ambientSky = new Color(0.15f, 0.20f, 0.28f);
    public Color ambientEquator = new Color(0.06f, 0.12f, 0.04f);
    public Color ambientGround = new Color(0.02f, 0.05f, 0.01f);
    public float ambientIntensity = 0.25f;

    [Header("Fog")]
    public Color fogColor = new Color(0.52f, 0.58f, 0.22f);
    public float fogDensity = 0.004f;

    [Header("Camera")]
    public float cameraFarClip = 350f;

    [Header("Bloom")]
    public float bloomIntensity = 2.0f;
    public float bloomThreshold = 0.6f;
    public float bloomSoftKnee = 0.5f;
    public float bloomDiffusion = 7f;

    [Header("Vignette")]
    public float vignetteIntensity = 0.4f;
    public float vignetteSmoothness = 0.45f;
    public Color vignetteColor = new Color(0.02f, 0.05f, 0.01f);

    [Header("Color Grading (Camera Filter)")]
    public float colorGradingTemperature = 22f;   // strong warm orange push
    public float colorGradingTint = 14f;           // visible green jungle tint
    public float colorGradingSaturation = 28f;     // vivid N64-style punch
    public float colorGradingContrast = 22f;       // crunch the mids hard
    public Color colorGradingColorFilter = new Color(1.0f, 0.78f, 0.42f); // deep orange amber filter

    [Header("Heat Wave")]
    public float heatWaveStrength = 0.006f;
    public float heatWaveScale = 3f;
    public float heatWaveSpeed = 0.8f;
    public float heatWaveHeightMask = 0.4f;

    [Header("Light Probes")]
    public Vector3 probeAreaSize = new Vector3(40f, 6f, 40f);
    public Vector3 probeSpacing = new Vector3(5f, 3f, 5f);

    // The layer index we assign to the volume object
    const string PPLayerName = "PostProcessing";

    [ContextMenu("Apply N64 Lighting")]
    public void Apply()
    {
        Debug.Log("[N64 Lighting] Applying...");

        CreateSun();
        CreateLight("N64_SkyFill", skyFillColor, skyFillIntensity, skyFillRotation);
        CreateLight("N64_GroundFill", groundFillColor, groundFillIntensity, groundFillRotation);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;
        RenderSettings.ambientIntensity = ambientIntensity;

        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = fogDensity;

        SetupCamera();
        BuildProbes();
        SetupPostProcessing();
        SetupHeatWave();

#if UNITY_EDITOR
        EditorUtility.SetDirty(gameObject);
#endif

        Debug.Log("[N64 Lighting] Done.");
    }

    // ── Sun ──────────────────────────────────────────────────────────────────

    void CreateSun()
    {
        GameObject existing = GameObject.Find("N64_Sun");
        if (existing != null) DestroyImmediate(existing);

        GameObject go = new GameObject("N64_Sun");
        go.transform.SetParent(this.transform);
        go.transform.eulerAngles = sunRotation;

        Light l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = sunColor;
        l.intensity = sunIntensity;
        l.shadows = LightShadows.Hard;
        l.shadowStrength = shadowStrength;
        l.shadowBias = shadowBias;
        l.shadowNormalBias = shadowNormalBias;
        l.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;

        // Force Quality Settings to actually render shadows
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
        QualitySettings.shadowDistance = 150f;
        QualitySettings.shadowCascades = 2;
        QualitySettings.shadowProjection = ShadowProjection.CloseFit;

        Debug.Log("[N64 Lighting] Sun created with hard shadows, distance 150.");
    }

    void CreateLight(string goName, Color color, float intensity, Vector3 rotation)
    {
        GameObject existing = GameObject.Find(goName);
        if (existing != null) DestroyImmediate(existing);

        GameObject go = new GameObject(goName);
        go.transform.SetParent(this.transform);
        go.transform.eulerAngles = rotation;

        Light l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = color;
        l.intensity = intensity;
        l.shadows = LightShadows.None;
    }

    // ── Camera ───────────────────────────────────────────────────────────────

    void SetupCamera()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("[N64 Lighting] No Main Camera found. Tag your camera as MainCamera.");
            return;
        }
        Camera.main.farClipPlane = cameraFarClip;
        Camera.main.allowHDR = true;
    }

    // ── Post Processing ──────────────────────────────────────────────────────

    void SetupPostProcessing()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("[N64 Lighting] No Main Camera — skipping post processing.");
            return;
        }

        // ── Step 1: ensure PostProcessing layer exists ────────────────────────
        int ppLayer = EnsureLayer(PPLayerName);

        // ── Step 2: volume object on that layer ───────────────────────────────
        GameObject volObj = GameObject.Find("N64_PostProcessVolume");
        if (volObj == null)
        {
            volObj = new GameObject("N64_PostProcessVolume");
            volObj.transform.SetParent(this.transform);
        }
        volObj.layer = ppLayer;

        PostProcessVolume volume = volObj.GetComponent<PostProcessVolume>();
        if (volume == null)
            volume = volObj.AddComponent<PostProcessVolume>();

        volume.isGlobal = true;
        volume.priority = 10;

        // ── Step 3: build profile — always fresh so bloom actually applies ────
        PostProcessProfile profile = ScriptableObject.CreateInstance<PostProcessProfile>();

#if UNITY_EDITOR
        string profilePath = "Assets/N64_PostProcessProfile.asset";
        // Delete stale profile so settings are never silently skipped
        if (System.IO.File.Exists(profilePath))
        {
            AssetDatabase.DeleteAsset(profilePath);
            AssetDatabase.Refresh();
        }
        AssetDatabase.CreateAsset(profile, profilePath);
        AssetDatabase.SaveAssets();
        profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(profilePath);
#endif

        volume.sharedProfile = profile;

        // ── Bloom ─────────────────────────────────────────────────────────────
        Bloom bloom = profile.AddSettings<Bloom>();
        bloom.enabled.Override(true);
        bloom.intensity.Override(bloomIntensity);
        bloom.threshold.Override(bloomThreshold);
        bloom.softKnee.Override(bloomSoftKnee);
        bloom.diffusion.Override(bloomDiffusion);
        bloom.fastMode.Override(false);

        // ── Vignette ──────────────────────────────────────────────────────────
        Vignette vignette = profile.AddSettings<Vignette>();
        vignette.enabled.Override(true);
        vignette.mode.Override(VignetteMode.Classic);
        vignette.color.Override(vignetteColor);
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(vignetteSmoothness);
        vignette.rounded.Override(true);

        // ── Color Grading (Camera Filter) ─────────────────────────────────────
        ColorGrading cg = profile.AddSettings<ColorGrading>();
        cg.enabled.Override(true);

        cg.temperature.Override(colorGradingTemperature);
        cg.tint.Override(colorGradingTint);
        cg.saturation.Override(colorGradingSaturation);
        cg.contrast.Override(colorGradingContrast);
        cg.colorFilter.Override(colorGradingColorFilter);

        // ── Step 4: PostProcessLayer on camera pointing at our layer ──────────
        PostProcessLayer ppLayerComponent = Camera.main.GetComponent<PostProcessLayer>();
        if (ppLayerComponent == null)
            ppLayerComponent = Camera.main.gameObject.AddComponent<PostProcessLayer>();

        ppLayerComponent.volumeLayer = 1 << ppLayer;   // <-- THIS is what makes it actually render
        ppLayerComponent.enabled = true;
        ppLayerComponent.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;

#if UNITY_EDITOR
        EditorUtility.SetDirty(Camera.main.gameObject);
#endif

        Debug.Log("[N64 Lighting] Post processing active on layer: " + PPLayerName);
    }

    // ── Ensure a layer exists, return its index ───────────────────────────────

    int EnsureLayer(string layerName)
    {
        // Check if it already exists
        int idx = LayerMask.NameToLayer(layerName);
        if (idx != -1) return idx;

#if UNITY_EDITOR
        // Add it to TagManager
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log("[N64 Lighting] Created layer: " + layerName + " at index " + i);
                return i;
            }
        }
        Debug.LogWarning("[N64 Lighting] No free layer slots. Delete an unused layer and re-apply.");
        return 0;
#else
        return 0;
#endif
    }

    // ── Heat Wave ─────────────────────────────────────────────────────────────

    void SetupHeatWave()
    {
        if (Camera.main == null) return;

        N64HeatWave hw = Camera.main.GetComponent<N64HeatWave>();
        if (hw == null)
            hw = Camera.main.gameObject.AddComponent<N64HeatWave>();

        hw.distortionStrength = heatWaveStrength;
        hw.distortionScale    = heatWaveScale;
        hw.distortionSpeed    = heatWaveSpeed;
        hw.heightMask         = heatWaveHeightMask;

        Debug.Log("[N64 Lighting] Heat wave added to camera.");
    }

    // ── Light Probes ──────────────────────────────────────────────────────────

    void BuildProbes()
    {
        GameObject existing = GameObject.Find("N64_LightProbes");
        if (existing != null) DestroyImmediate(existing);

        GameObject go = new GameObject("N64_LightProbes");
        go.transform.SetParent(this.transform);

        LightProbeGroup lpg = go.AddComponent<LightProbeGroup>();
        List<Vector3> positions = new List<Vector3>();
        Vector3 origin = transform.position - probeAreaSize * 0.5f;

        for (float x = 0; x <= probeAreaSize.x; x += probeSpacing.x)
            for (float y = 0; y <= probeAreaSize.y; y += probeSpacing.y)
                for (float z = 0; z <= probeAreaSize.z; z += probeSpacing.z)
                    positions.Add(origin + new Vector3(x, y, z));

        lpg.probePositions = positions.ToArray();
    }
}