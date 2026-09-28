Shader "Hidden/N64FX"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _HeatStr, _HeatScale, _HeatSpeed, _HeatMask;
            fixed4 frag(v2f_img i) : SV_Target {
                float2 uv = i.uv;
                float mask = saturate(1.0 - (uv.y - _HeatMask) / (1.0 - _HeatMask + 0.001));
                float t = _Time.y * _HeatSpeed;
                float wave = sin(uv.y * _HeatScale * 38.0 + t * 6.283)
                           + sin(uv.y * _HeatScale * 15.7 + t * 3.9 + 1.2) * 0.45
                           + sin(uv.y * _HeatScale * 71.3 + t * 9.1 + 2.7) * 0.2;
                uv.x += wave * _HeatStr * mask;
                return tex2D(_MainTex, uv);
            }
            ENDCG
        }

        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _PixelSize;
            fixed4 frag(v2f_img i) : SV_Target {
                float2 res = _MainTex_TexelSize.zw;
                float2 snapped = (floor(i.uv * res / _PixelSize) * _PixelSize + _PixelSize * 0.5) / res;
                return tex2D(_MainTex, snapped);
            }
            ENDCG
        }

        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _ChromaStr, _ScanlineInt, _NoiseStr, _RollSpeed;
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            fixed4 frag(v2f_img i) : SV_Target {
                float2 uv = i.uv;
                float2 dir = (uv - 0.5) * _ChromaStr;
                float r = tex2D(_MainTex, uv + dir).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv - dir).b;
                fixed4 col = fixed4(r, g, b, 1);
                float line = sin(uv.y * _MainTex_TexelSize.w * UNITY_PI) * 0.5 + 0.5;
                col.rgb *= lerp(1.0 - _ScanlineInt, 1.0, line);
                float n = hash(uv + frac(float2(_Time.y * _RollSpeed, _Time.y * _RollSpeed * 0.7))) * 2.0 - 1.0;
                col.rgb += n * _NoiseStr;
                float2 vig = uv - 0.5;
                col.rgb *= 1.0 - dot(vig, vig) * 0.4;
                return col;
            }
            ENDCG
        }

        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float2 _SunUV;
            float _FlareIntensity, _FlareRadius, _AnaStr, _SunVisible, _Aspect;
            float4 _FlareColor;
            float lens(float d, float r) { return pow(max(0.0, r - d) / r, 2.5); }
            fixed4 frag(v2f_img i) : SV_Target {
                fixed4 base = tex2D(_MainTex, i.uv);
                if (_SunVisible < 0.5) return base;
                float2 uv = i.uv;
                float2 toS = (uv - _SunUV) * float2(_Aspect, 1.0);
                float d = length(toS);
                float glow  = lens(d, _FlareRadius * 0.9);
                float halo  = lens(d, _FlareRadius * 2.2) * 0.25;
                float2 toSR = uv - _SunUV;
                float streak = exp(-abs(toSR.y) * 140.0)
                             * (1.0 - smoothstep(0.0, 0.9, abs(toSR.x)))
                             * _AnaStr;
                float core = exp(-abs(toSR.y) * 400.0) * exp(-abs(toSR.x) * 0.8) * _AnaStr * 0.4;
                float2 axis = float2(0.5, 0.5) - _SunUV;
                float2 g1 = _SunUV + axis * 1.35;
                float ghost1 = lens(length((uv - g1) * float2(_Aspect,1.0)), _FlareRadius * 0.55) * 0.28;
                float2 g2 = _SunUV + axis * 2.10;
                float ghost2 = lens(length((uv - g2) * float2(_Aspect,1.0)), _FlareRadius * 0.35) * 0.18;
                float2 g3 = _SunUV + axis * 0.60;
                float ghost3 = lens(length((uv - g3) * float2(_Aspect,1.0)), _FlareRadius * 0.70) * 0.12;
                float ang = atan2(toS.y, toS.x);
                float spike = pow(abs(sin(ang * 2.0)), 8.0) * lens(d, _FlareRadius * 0.5) * 0.3;
                float total = (glow + halo + streak + core + ghost1 + ghost2 + ghost3 + spike) * _FlareIntensity;
                return base + _FlareColor * fixed4(total, total * 0.85, total * 0.55, 1.0);
            }
            ENDCG
        }
    }
}