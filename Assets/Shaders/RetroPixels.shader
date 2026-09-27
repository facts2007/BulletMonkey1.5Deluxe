Shader "BulletMonkey/RetroPixels"
{
 Properties { _MainTex ("Texture", 2D) = "white" {} }
 SubShader {
 Tags { "Queue"="Overlay" "RenderType"="Transparent" }
 Cull Off ZWrite Off ZTest Always
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0; };
 struct v2f { float4 pos:SV_POSITION;float2 uv:TEXCOORD0; };
 sampler2D _MainTex;float4 _MainTex_TexelSize;
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
 fixed4 frag(v2f i):SV_Target {
 float3 c=tex2D(_MainTex,i.uv).rgb;
 float2 pixel=floor(i.uv*_MainTex_TexelSize.zw);
 float d=(fmod(pixel.x,2)*2+fmod(pixel.y,2)-1.5)/124;
 return float4(floor(saturate(c+d)*31+.5)/31,1);
 }
 ENDCG
 }
 }
}
