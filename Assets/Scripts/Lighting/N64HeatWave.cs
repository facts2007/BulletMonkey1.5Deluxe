// N64HeatWave.cs
// Place in: Assets/Scripts/
// Attach to your Main Camera OR let N64JungleLighting add it automatically via Apply.
// Simulates heat haze distortion using a screen-space sine wave displacement.
// No extra packages needed — uses OnRenderImage which is built into Unity.

using UnityEngine;

[RequireComponent(typeof(Camera))]
public class N64HeatWave : MonoBehaviour
{
    [Header("Heat Wave")]
    [Range(0f, 0.02f)]
    public float distortionStrength = 0.006f;   // how far pixels shift

    [Range(0.5f, 8f)]
    public float distortionScale = 3f;           // size of the wave pattern

    [Range(0.1f, 3f)]
    public float distortionSpeed = 0.8f;         // how fast the waves move

    [Range(0f, 1f)]
    public float heightMask = 0.5f;              // only distort above this screen height (0=all, 1=top only)

    Material _mat;

    static readonly string ShaderSource = @"
Shader ""Hidden/N64HeatWave""
{
    Properties
    {
        _MainTex (""Texture"", 2D) = ""white"" {}
    }
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
            float _Strength;
            float _Scale;
            float _Speed;
            float _HeightMask;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                // Only apply above heightMask threshold
                float mask = saturate((uv.y - _HeightMask) / (1.0 - _HeightMask + 0.0001));

                float t = _Time.y * _Speed;

                float offsetX = sin(uv.y * _Scale * 10.0 + t) * _Strength * mask;
                float offsetY = cos(uv.x * _Scale * 10.0 + t * 0.7) * _Strength * 0.5 * mask;

                float2 distortedUV = uv + float2(offsetX, offsetY);
                return tex2D(_MainTex, distortedUV);
            }
            ENDCG
        }
    }
}";

    void OnEnable()
    {
        BuildMaterial();
    }

    void BuildMaterial()
    {
        Shader shader = Shader.Find("Hidden/N64HeatWave");

        if (shader == null)
        {
            // Write shader to disk and import it
            #if UNITY_EDITOR
            WriteAndImportShader();
            shader = Shader.Find("Hidden/N64HeatWave");
            #endif
        }

        if (shader != null)
            _mat = new Material(shader);
        else
            Debug.LogWarning("[N64 HeatWave] Shader not found. See setup note below.");
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (_mat == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        _mat.SetFloat("_Strength", distortionStrength);
        _mat.SetFloat("_Scale", distortionScale);
        _mat.SetFloat("_Speed", distortionSpeed);
        _mat.SetFloat("_HeightMask", heightMask);

        Graphics.Blit(src, dest, _mat);
    }

    #if UNITY_EDITOR
    void WriteAndImportShader()
    {
        string path = "Assets/Shaders/N64HeatWave.shader";
        string dir  = System.IO.Path.GetDirectoryName(path);

        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        if (!System.IO.File.Exists(path))
        {
            System.IO.File.WriteAllText(path, ShaderSource);
            UnityEditor.AssetDatabase.ImportAsset(path);
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("[N64 HeatWave] Shader written to " + path);
        }
    }
    #endif
}