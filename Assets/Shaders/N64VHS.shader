
Shader "Hidden/N64VHS"
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
}