
Shader "Hidden/N64HeatWave"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
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
}