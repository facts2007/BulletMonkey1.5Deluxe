
Shader "Hidden/N64HeatWave"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
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
}