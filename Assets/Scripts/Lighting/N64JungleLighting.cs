using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class N64JungleLighting : MonoBehaviour
{
    [Header("Sun")]
    public Color sunColor = new Color(1.0f, 0.62f, 0.18f);
    public float sunIntensity = 2.4f;
    public Vector3 sunRotation = new Vector3(52f, -30f, 0f);

    [Header("Lens Flare")]
    public UnityEngine.Object lensFlareData;
    public float lensFlareIntensity = 1.5f;

    [Header("Sky Fill")]
    public Color skyFillColor = new Color(0.29f, 0.42f, 0.54f);
    public float skyFillIntensity = 0.12f;
    public Vector3 skyFillRotation = new Vector3(-40f, 150f, 0f);

    [Header("Ground Fill")]
    public Color groundFillColor = new Color(0.18f, 0.29f, 0.10f);
    public float groundFillIntensity = 0.08f;
    public Vector3 groundFillRotation = new Vector3(90f, 0f, 0f);

    [Header("Shadows")]
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
    public float bloomScatter = 0.7f;

    [Header("Vignette")]
    public float vignetteIntensity = 0.4f;
    public float vignetteSmoothness = 0.45f;
    public Color vignetteColor = new Color(0.02f, 0.05f, 0.01f);

    [Header("Color")]
    public float temperature = 22f;
    public float tint = 14f;
    public float saturation = 28f;
    public float contrast = 22f;
    public Color colorFilter = new Color(1.0f, 0.78f, 0.42f);

    [Header("Light Probes")]
    public Vector3 probeAreaSize = new Vector3(40f, 6f, 40f);
    public Vector3 probeSpacing = new Vector3(5f, 3f, 5f);

    private const string SUN = "N64_Sun";
    private const string SKY = "N64_SkyFill";
    private const string GROUND = "N64_GroundFill";
    private const string VOLUME = "N64_GlobalVolume";
    private const string PROBES = "N64_LightProbes";

    [ContextMenu("Apply N64 Lighting")]
    public void Apply()
    {
        CreateSun();
        CreateFillLight(SKY, skyFillColor, skyFillIntensity, skyFillRotation);
        CreateFillLight(GROUND, groundFillColor, groundFillIntensity, groundFillRotation);

        SetupAmbient();
        SetupFog();
        SetupCamera();
        SetupVolume();
        SetupProbes();

        Debug.Log("N64 Jungle Lighting applied.");
    }

    private void CreateSun()
    {
        DeleteObject(SUN);

        GameObject obj = new GameObject(SUN);
        obj.transform.SetParent(transform);
        obj.transform.position = transform.position;
        obj.transform.eulerAngles = sunRotation;

        Light light = obj.AddComponent<Light>();

        light.type = LightType.Directional;
        light.color = sunColor;
        light.intensity = sunIntensity;

        light.shadows = LightShadows.Soft;
        light.shadowStrength = shadowStrength;
        light.shadowBias = shadowBias;
        light.shadowNormalBias = shadowNormalBias;

        AddLensFlare(obj);
    }

    private void AddLensFlare(GameObject sun)
    {
        if (lensFlareData == null)
        {
            Debug.LogWarning(
                "N64 Jungle Lighting: No Lens Flare Data assigned. " +
                "The lighting will still work."
            );

            return;
        }

        Type flareType = Type.GetType(
            "UnityEngine.Rendering.Universal.LensFlareComponentSRP, Unity.RenderPipelines.Universal.Runtime"
        );

        if (flareType == null)
        {
            Debug.LogWarning(
                "N64 Jungle Lighting: Lens Flare SRP component was not found in this URP installation."
            );

            return;
        }

        Component flare = sun.GetComponent(flareType);

        if (flare == null)
            flare = sun.AddComponent(flareType);

        SetProperty(flare, "lensFlareData", lensFlareData);
        SetProperty(flare, "intensity", lensFlareIntensity);

        Debug.Log("N64 Jungle Lighting: Lens flare component added.");
    }

    private void SetProperty(Component component, string propertyName, object value)
    {
        var property = component.GetType().GetProperty(propertyName);

        if (property != null && property.CanWrite)
        {
            try
            {
                property.SetValue(component, value);
            }
            catch
            {
                Debug.LogWarning(
                    "Could not set lens flare property: " + propertyName
                );
            }
        }
    }

    private void CreateFillLight(
        string objectName,
        Color color,
        float intensity,
        Vector3 rotation)
    {
        DeleteObject(objectName);

        GameObject obj = new GameObject(objectName);

        obj.transform.SetParent(transform);
        obj.transform.position = transform.position;
        obj.transform.eulerAngles = rotation;

        Light light = obj.AddComponent<Light>();

        light.type = LightType.Directional;
        light.color = color;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
    }

    private void SetupAmbient()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;

        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;
        RenderSettings.ambientIntensity = ambientIntensity;
    }

    private void SetupFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = fogDensity;
    }

    private void SetupCamera()
    {
        Camera cam = Camera.main;

        if (cam != null)
            cam.farClipPlane = cameraFarClip;
    }

    private void SetupVolume()
    {
        GameObject obj = GameObject.Find(VOLUME);

        if (obj == null)
        {
            obj = new GameObject(VOLUME);
            obj.transform.SetParent(transform);
        }

        Volume volume = obj.GetComponent<Volume>();

        if (volume == null)
            volume = obj.AddComponent<Volume>();

        volume.isGlobal = true;
        volume.priority = 10f;

        VolumeProfile profile = volume.sharedProfile;

        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;
        }

        Bloom bloom;

        if (!profile.TryGet(out bloom))
            bloom = profile.Add<Bloom>(true);

        bloom.active = true;
        bloom.intensity.Override(bloomIntensity);
        bloom.threshold.Override(bloomThreshold);
        bloom.scatter.Override(bloomScatter);

        Vignette vignette;

        if (!profile.TryGet(out vignette))
            vignette = profile.Add<Vignette>(true);

        vignette.active = true;
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(vignetteSmoothness);
        vignette.color.Override(vignetteColor);

        ColorAdjustments color;

        if (!profile.TryGet(out color))
            color = profile.Add<ColorAdjustments>(true);

        color.active = true;
        color.saturation.Override(saturation);
        color.contrast.Override(contrast);
        color.colorFilter.Override(colorFilter);

        WhiteBalance whiteBalance;

        if (!profile.TryGet(out whiteBalance))
            whiteBalance = profile.Add<WhiteBalance>(true);

        whiteBalance.active = true;
        whiteBalance.temperature.Override(temperature);
        whiteBalance.tint.Override(tint);
    }

    private void SetupProbes()
    {
        DeleteObject(PROBES);

        GameObject obj = new GameObject(PROBES);
        obj.transform.SetParent(transform);

        LightProbeGroup group = obj.AddComponent<LightProbeGroup>();

        var positions = new System.Collections.Generic.List<Vector3>();

        Vector3 start =
            transform.position -
            probeAreaSize * 0.5f;

        for (float x = 0; x <= probeAreaSize.x; x += probeSpacing.x)
        {
            for (float y = 0; y <= probeAreaSize.y; y += probeSpacing.y)
            {
                for (float z = 0; z <= probeAreaSize.z; z += probeSpacing.z)
                {
                    positions.Add(
                        start + new Vector3(x, y, z)
                    );
                }
            }
        }

        group.probePositions = positions.ToArray();
    }

    private void DeleteObject(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);

        if (obj != null)
        {
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }
    }
}