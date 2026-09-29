// N64JungleLighting.cs
// ONE SCRIPT — place in Assets/Scripts/
// Requires: Post Processing package (Window -> Package Manager -> Post Processing -> Install)
// Right-click component in Inspector -> "Apply N64 Lighting"
// Everything: lights, shadows, fog, bloom, vignette, flares, heatwaves, VHS

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class N64JungleLighting : MonoBehaviour
{
    // ── Sun ───────────────────────────────────────────────────────────────────
    [Header("Sun")]
    public Color  sunColor     = new Color(1.00f, 0.52f, 0.08f);
    public float  sunIntensity = 1.4f;
    public Vector3 sunRotation = new Vector3(52f, -30f, 0f);

    // ── Fill Lights ───────────────────────────────────────────────────────────
    [Header("Sky Fill")]
    public Color  skyFillColor    = new Color(0.29f, 0.42f, 0.54f);
    public float  skyFillIntensity = 0.12f;
    public Vector3 skyFillRotation = new Vector3(-40f, 150f, 0f);

    [Header("Ground Fill")]
    public Color  groundFillColor    = new Color(0.18f, 0.29f, 0.10f);
    public float  groundFillIntensity = 0.08f;
    public Vector3 groundFillRotation = new Vector3(90f, 0f, 0f);

    // ── Shadows ───────────────────────────────────────────────────────────────
    [Header("Shadows")]
    public float shadowStrength    = 0.85f;
    public float shadowBias        = 0.02f;
    public float shadowNormalBias  = 0.2f;

    // ── Ambient ───────────────────────────────────────────────────────────────
    [Header("Ambient")]
    public Color ambientSky     = new Color(0.15f, 0.20f, 0.28f);
    public Color ambientEquator = new Color(0.06f, 0.12f, 0.04f);
    public Color ambientGround  = new Color(0.02f, 0.05f, 0.01f);
    public float ambientIntensity = 0.25f;

    // ── Fog ───────────────────────────────────────────────────────────────────
    [Header("Fog")]
    public Color fogColor   = new Color(0.52f, 0.58f, 0.22f);
    public float fogDensity = 0.004f;

    // ── Camera ────────────────────────────────────────────────────────────────
    [Header("Camera")]
    public float cameraFarClip = 350f;

    // ── Bloom ─────────────────────────────────────────────────────────────────
    [Header("Bloom")]
    public float bloomIntensity  = 2.0f;
    public float bloomThreshold  = 0.6f;
    public float bloomSoftKnee   = 0.5f;
    public float bloomDiffusion  = 7f;

    // ── Vignette ──────────────────────────────────────────────────────────────
    [Header("Vignette")]
    public float vignetteIntensity  = 0.4f;
    public float vignetteSmoothness = 0.45f;
    public Color vignetteColor      = new Color(0.02f, 0.05f, 0.01f);

    // ── Color Grading ─────────────────────────────────────────────────────────
    [Header("Color Grading")]
    public float colorTemperature = 32f;
    public float colorTint        = 14f;
    public float colorSaturation  = 32f;
    public float colorContrast    = 22f;
    public Color colorFilter      = new Color(1.0f, 0.65f, 0.22f);

    // ── Lens Flare ────────────────────────────────────────────────────────────
    [Header("Lens Flare")]
    public bool  enableFlare     = true;
    public Flare sunFlare        = null;
    public float flareBrightness = 0.6f;
    public Color flareColor      = new Color(1.0f, 0.7f, 0.3f);

    // ── Heat Wave ─────────────────────────────────────────────────────────────
    [Header("Heat Wave")]
    public bool  enableHeatWave      = true;
    public float heatWaveStrength    = 0.007f;
    public float heatWaveScale       = 3f;
    public float heatWaveSpeed       = 0.9f;
    public float heatWaveHeightMask  = 0.35f;

    // ── VHS ───────────────────────────────────────────────────────────────────
    [Header("VHS")]
    public bool  enableVHS          = true;
    public float vhsScanlineStrength = 0.08f;
    public float vhsNoiseStrength    = 0.04f;
    public float vhsRGBShift         = 0.003f;

    // ── Light Probes ──────────────────────────────────────────────────────────
    [Header("Light Probes")]
    public Vector3 probeAreaSize = new Vector3(40f, 6f, 40f);
    public Vector3 probeSpacing  = new Vector3(5f, 3f, 5f);

    const string PPLayerName = "PostProcessing";

    // ─────────────────────────────────────────────────────────────────────────
    // APPLY
    // ─────────────────────────────────────────────────────────────────────────

    [ContextMenu("Apply N64 Lighting")]
    public void Apply()
    {
        Debug.Log("[N64 Lighting] Applying...");

        CreateSun();
        CreateFillLight("N64_SkyFill",    skyFillColor,    skyFillIntensity,    skyFillRotation);
        CreateFillLight("N64_GroundFill", groundFillColor, groundFillIntensity, groundFillRotation);

        RenderSettings.ambientMode         = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor  = ambientGround;
        RenderSettings.ambientIntensity    = ambientIntensity;

        RenderSettings.fog        = true;
        RenderSettings.fogColor   = fogColor;
        RenderSettings.fogMode    = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = fogDensity;

        SetupCamera();
        BuildProbes();
        SetupPostProcessing();
        WriteAndApplyShaders();

#if UNITY_EDITOR
        EditorUtility.SetDirty(gameObject);
#endif
        Debug.Log("[N64 Lighting] Done. Check Game view for bloom/vignette/VHS/heatwaves.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LIGHTS
    // ─────────────────────────────────────────────────────────────────────────

    void CreateSun()
    {
        Kill("N64_Sun");
        GameObject go = new GameObject("N64_Sun");
        go.transform.SetParent(transform);
        go.transform.eulerAngles = sunRotation;

        Light l = go.AddComponent<Light>();
        l.type             = LightType.Directional;
        l.color            = sunColor;
        l.intensity        = sunIntensity;
        l.shadows          = LightShadows.Hard;
        l.shadowStrength   = shadowStrength;
        l.shadowBias       = shadowBias;
        l.shadowNormalBias = shadowNormalBias;
        l.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;

        // Lens flare on the sun
        if (enableFlare)
        {
            LensFlare lf = go.AddComponent<LensFlare>();
            lf.brightness = flareBrightness;
            lf.color      = flareColor;
            lf.fadeSpeed  = 5f;
            if (sunFlare != null)
                lf.flare = sunFlare;
            else
                Debug.LogWarning("[N64 Lighting] No flare asset assigned. Drag any Flare asset into the Sun Flare slot on the N64JungleLighting component.");
        }

        QualitySettings.shadows           = ShadowQuality.All;
        QualitySettings.shadowResolution  = ShadowResolution.VeryHigh;
        QualitySettings.shadowDistance    = 150f;
        QualitySettings.shadowCascades    = 2;
        QualitySettings.shadowProjection  = ShadowProjection.CloseFit;
    }

    void CreateFillLight(string goName, Color color, float intensity, Vector3 rotation)
    {
        Kill(goName);
        GameObject go = new GameObject(goName);
        go.transform.SetParent(transform);
        go.transform.eulerAngles = rotation;

        Light l = go.AddComponent<Light>();
        l.type      = LightType.Directional;
        l.color     = color;
        l.intensity = intensity;
        l.shadows   = LightShadows.None;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CAMERA
    // ─────────────────────────────────────────────────────────────────────────

    void SetupCamera()
    {
        if (Camera.main == null) { Debug.LogWarning("[N64 Lighting] No Main Camera."); return; }
        Camera.main.farClipPlane = cameraFarClip;
        Camera.main.allowHDR     = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST PROCESSING — bloom, vignette, color grading
    // ─────────────────────────────────────────────────────────────────────────

    void SetupPostProcessing()
    {
        if (Camera.main == null) return;

        int ppLayer = EnsureLayer(PPLayerName);

        Kill("N64_PostProcessVolume");
        GameObject volObj = new GameObject("N64_PostProcessVolume");
        volObj.transform.SetParent(transform);
        volObj.layer = ppLayer;

        PostProcessVolume volume = volObj.AddComponent<PostProcessVolume>();
        volume.isGlobal = true;
        volume.priority = 10;

        PostProcessProfile profile = ScriptableObject.CreateInstance<PostProcessProfile>();

#if UNITY_EDITOR
        string path = "Assets/N64_PostProcessProfile.asset";
        if (System.IO.File.Exists(path)) { AssetDatabase.DeleteAsset(path); AssetDatabase.Refresh(); }
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();
        profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(path);
#endif
        volume.sharedProfile = profile;

        // Bloom
        Bloom bloom = profile.AddSettings<Bloom>();
        bloom.enabled.Override(true);
        bloom.intensity.Override(bloomIntensity);
        bloom.threshold.Override(bloomThreshold);
        bloom.softKnee.Override(bloomSoftKnee);
        bloom.diffusion.Override(bloomDiffusion);
        bloom.fastMode.Override(false);

        // Vignette
        Vignette vignette = profile.AddSettings<Vignette>();
        vignette.enabled.Override(true);
        vignette.mode.Override(VignetteMode.Classic);
        vignette.color.Override(vignetteColor);
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(vignetteSmoothness);
        vignette.rounded.Override(true);

        // Color grading
        ColorGrading cg = profile.AddSettings<ColorGrading>();
        cg.enabled.Override(true);
        cg.temperature.Override(colorTemperature);
        cg.tint.Override(colorTint);
        cg.saturation.Override(colorSaturation);
        cg.contrast.Override(colorContrast);
        cg.colorFilter.Override(colorFilter);

        // PostProcessLayer on camera
        PostProcessLayer ppl = Camera.main.GetComponent<PostProcessLayer>()
                            ?? Camera.main.gameObject.AddComponent<PostProcessLayer>();
        ppl.volumeLayer      = 1 << ppLayer;
        ppl.enabled          = true;
        ppl.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;

#if UNITY_EDITOR
        EditorUtility.SetDirty(Camera.main.gameObject);
#endif
        Debug.Log("[N64 Lighting] Post processing wired to layer: " + PPLayerName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SHADERS — heat wave + VHS written to disk, applied via OnRenderImage
    // ─────────────────────────────────────────────────────────────────────────

    void WriteAndApplyShaders()
    {
        if (Camera.main == null) return;

#if UNITY_EDITOR
        EnsureShaderDir();
        WriteShader("Assets/Shaders/N64HeatWave.shader",  HeatWaveShader());
        WriteShader("Assets/Shaders/N64VHS.shader",       VHSShader());
        AssetDatabase.Refresh();
#endif
        // Add or update the camera effect component
        N64CameraFX fx = Camera.main.GetComponent<N64CameraFX>()
                      ?? Camera.main.gameObject.AddComponent<N64CameraFX>();

        fx.enableHeatWave       = enableHeatWave;
        fx.heatWaveStrength     = heatWaveStrength;
        fx.heatWaveScale        = heatWaveScale;
        fx.heatWaveSpeed        = heatWaveSpeed;
        fx.heatWaveHeightMask   = heatWaveHeightMask;

        fx.enableVHS            = enableVHS;
        fx.scanlineStrength     = vhsScanlineStrength;
        fx.noiseStrength        = vhsNoiseStrength;
        fx.rgbShift             = vhsRGBShift;

        Debug.Log("[N64 Lighting] Heat wave + VHS applied to camera.");
    }

#if UNITY_EDITOR
    void EnsureShaderDir()
    {
        if (!System.IO.Directory.Exists("Assets/Shaders"))
            System.IO.Directory.CreateDirectory("Assets/Shaders");
    }

    void WriteShader(string path, string src)
    {
        System.IO.File.WriteAllText(path, src);
        Debug.Log("[N64 Lighting] Shader written: " + path);
    }
#endif

    string HeatWaveShader() => @"
Shader ""Hidden/N64HeatWave""
{
    Properties { _MainTex (""Texture"", 2D) = ""white"" {} }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include ""UnityCG.cginc""
            sampler2D _MainTex;
            float _Strength, _Scale, _Speed, _HeightMask;
            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float mask = saturate((uv.y - _HeightMask) / (1.0 - _HeightMask + 0.0001));
                float t = _Time.y * _Speed;
                float ox = sin(uv.y * _Scale * 10.0 + t) * _Strength * mask;
                float oy = cos(uv.x * _Scale * 10.0 + t * 0.7) * _Strength * 0.5 * mask;
                return tex2D(_MainTex, uv + float2(ox, oy));
            }
            ENDCG
        }
    }
}";

    string VHSShader() => @"
Shader ""Hidden/N64VHS""
{
    Properties { _MainTex (""Texture"", 2D) = ""white"" {} }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include ""UnityCG.cginc""
            sampler2D _MainTex;
            float _Scanline, _Noise, _RGBShift;

            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float t   = _Time.y;

                // RGB shift
                float r = tex2D(_MainTex, uv + float2(_RGBShift, 0)).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv - float2(_RGBShift, 0)).b;
                float a = tex2D(_MainTex, uv).a;
                fixed4 col = fixed4(r, g, b, a);

                // Scanlines
                float scanVal = sin(uv.y * _ScreenParams.y * 1.5) * _Scanline;
                col.rgb -= scanVal;

                // Noise
                float n = rand(float2(uv.x + t * 0.1, uv.y + t * 0.07)) * _Noise;
                col.rgb += n - _Noise * 0.5;

                return col;
            }
            ENDCG
        }
    }
}";

    // ─────────────────────────────────────────────────────────────────────────
    // LAYER UTIL
    // ─────────────────────────────────────────────────────────────────────────

    int EnsureLayer(string layerName)
    {
        int idx = LayerMask.NameToLayer(layerName);
        if (idx != -1) return idx;

#if UNITY_EDITOR
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
                return i;
            }
        }
        Debug.LogWarning("[N64 Lighting] No free layer slots.");
#endif
        return 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LIGHT PROBES
    // ─────────────────────────────────────────────────────────────────────────

    void BuildProbes()
    {
        Kill("N64_LightProbes");
        GameObject go = new GameObject("N64_LightProbes");
        go.transform.SetParent(transform);

        LightProbeGroup lpg = go.AddComponent<LightProbeGroup>();
        var positions = new List<Vector3>();
        Vector3 origin = transform.position - probeAreaSize * 0.5f;

        for (float x = 0; x <= probeAreaSize.x; x += probeSpacing.x)
            for (float y = 0; y <= probeAreaSize.y; y += probeSpacing.y)
                for (float z = 0; z <= probeAreaSize.z; z += probeSpacing.z)
                    positions.Add(origin + new Vector3(x, y, z));

        lpg.probePositions = positions.ToArray();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UTIL
    // ─────────────────────────────────────────────────────────────────────────

    void Kill(string goName)
    {
        GameObject existing = GameObject.Find(goName);
        if (existing != null) DestroyImmediate(existing);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// N64CameraFX — attached to camera automatically by Apply()
// Runs heat wave and VHS as OnRenderImage passes
// ─────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(Camera))]
public class N64CameraFX : MonoBehaviour
{
    [Header("Heat Wave")]
    public bool  enableHeatWave    = true;
    public float heatWaveStrength  = 0.007f;
    public float heatWaveScale     = 3f;
    public float heatWaveSpeed     = 0.9f;
    public float heatWaveHeightMask = 0.35f;

    [Header("VHS")]
    public bool  enableVHS         = true;
    public float scanlineStrength  = 0.08f;
    public float noiseStrength     = 0.04f;
    public float rgbShift          = 0.003f;

    Material _heatMat;
    Material _vhsMat;

    void OnEnable()
    {
        _heatMat = LoadMat("Hidden/N64HeatWave");
        _vhsMat  = LoadMat("Hidden/N64VHS");
    }

    Material LoadMat(string shaderName)
    {
        Shader s = Shader.Find(shaderName);
        if (s == null) { Debug.LogWarning("[N64 FX] Shader not found: " + shaderName + " — hit Apply again after Unity reimports."); return null; }
        return new Material(s);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        RenderTexture current = src;
        RenderTexture tmp     = null;

        if (enableHeatWave && _heatMat != null)
        {
            _heatMat.SetFloat("_Strength",   heatWaveStrength);
            _heatMat.SetFloat("_Scale",      heatWaveScale);
            _heatMat.SetFloat("_Speed",      heatWaveSpeed);
            _heatMat.SetFloat("_HeightMask", heatWaveHeightMask);

            tmp = RenderTexture.GetTemporary(src.descriptor);
            Graphics.Blit(current, tmp, _heatMat);
            current = tmp;
        }

        if (enableVHS && _vhsMat != null)
        {
            _vhsMat.SetFloat("_Scanline", scanlineStrength);
            _vhsMat.SetFloat("_Noise",    noiseStrength);
            _vhsMat.SetFloat("_RGBShift", rgbShift);

            RenderTexture tmp2 = RenderTexture.GetTemporary(src.descriptor);
            Graphics.Blit(current, tmp2, _vhsMat);
            if (tmp != null) RenderTexture.ReleaseTemporary(tmp);
            tmp = tmp2;
            current = tmp;
        }

        Graphics.Blit(current, dest);
        if (tmp != null) RenderTexture.ReleaseTemporary(tmp);
    }
}