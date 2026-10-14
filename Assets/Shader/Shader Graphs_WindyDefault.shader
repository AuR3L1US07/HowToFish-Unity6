Shader "Shader Graphs/WindyDefault" {
	Properties {
		[NoScaleOffset] _Texture ("Texture", 2D) = "white" {}
		_Smoothness ("Smoothness", Float) = 0
		_Metallic ("Metallic", Float) = 0
		_WindFromOrigoSmoothstep ("WindFromOrigoSmoothstep", Vector) = (0,1,0,0)
		_WindSpeed ("WindSpeed", Float) = 2.8
		_WindSpeed2 ("WindSpeed2", Float) = 0.5
		_WindScale ("WindScale", Float) = 0.15
		_WindDensity ("WindDensity", Float) = 0.2
		_NormalInfluence ("NormalInfluence", Float) = 2
		_SSSPower ("SSSPower", Float) = 1.5
		_SSSIntensity ("SSSIntensity", Float) = 2
		_Thickness ("Thickness", Float) = 1
		[NoScaleOffset] _Normal_Map ("Normal Map", 2D) = "white" {}
		_Normal_Strength ("Normal Strength", Range(0, 1)) = 0.146
		_Normal_Scale ("Normal Scale", Float) = 1
		[HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
		[HideInInspector] _QueueControl ("_QueueControl", Float) = -1
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
	}
	// Recovery implementation based on original DXBC and render state; lighting remains reconstructed.
SubShader {
Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="AlphaTest" }
Blend One Zero
ZWrite On
Cull Off
Pass { Tags { "LightMode"="UniversalForward" }
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fog

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
struct A { float4 pos:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float2 uv1:TEXCOORD1; float4 color:COLOR; };
struct V { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float2 uv:TEXCOORD1; float3 world:TEXCOORD2; float3 objectPos:TEXCOORD3; float4 color:TEXCOORD4; float fog:TEXCOORD5; };
half3 RecoveryLight(float3 normal) { normal=normalize(normal); Light light=GetMainLight(); return max(SampleSH(normal)+light.color*(.2+.8*saturate(dot(normal,light.direction))),.25); }

float2 RecoveryNoiseDir(float2 p) { float x; Hash_Tchou_2_1_float(p,x); return normalize(float2(x-floor(x+.5),abs(x)-.5)); }
float RecoveryGradient(float2 p) { float2 ip=floor(p),fp=frac(p),f=fp*fp*fp*(fp*(fp*6-15)+10);
float a=dot(RecoveryNoiseDir(ip),fp),b=dot(RecoveryNoiseDir(ip+float2(0,1)),fp-float2(0,1));
float c=dot(RecoveryNoiseDir(ip+float2(1,0)),fp-float2(1,0)),d=dot(RecoveryNoiseDir(ip+1),fp-1);
return lerp(lerp(a,b,f.y),lerp(c,d,f.y),f.x)+.5; }
float RecoveryValue(float2 p) { float2 ip=floor(p),f=frac(p); f=f*f*(3-2*f); float a,b,c,d;
Hash_Tchou_2_1_float(ip,a); Hash_Tchou_2_1_float(ip+float2(1,0),b); Hash_Tchou_2_1_float(ip+float2(0,1),c); Hash_Tchou_2_1_float(ip+1,d);
return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y); }
float RecoverySimpleNoise(float2 uv) { return RecoveryValue(uv)*.125+RecoveryValue(uv*.5)*.25+RecoveryValue(uv*.25)*.5; }
TEXTURE2D(_Texture); SAMPLER(sampler_Texture); float _WindSpeed,_WindSpeed2,_WindDensity,_WindScale; float4 _WindFromOrigoSmoothstep;
V vert(A v) { V o; float3 world=TransformObjectToWorld(v.pos.xyz);

float noise=RecoveryGradient((v.pos.xy+_Time.y*_WindSpeed2)*_WindDensity)+RecoveryGradient((v.pos.xy-_Time.y*_WindSpeed)*_WindDensity)-.5;
float weight=smoothstep(_WindFromOrigoSmoothstep.x,max(_WindFromOrigoSmoothstep.y,_WindFromOrigoSmoothstep.x+.0001),length(v.pos.xy));
v.pos.xy+=noise*_WindScale*weight;

o.world=TransformObjectToWorld(v.pos.xyz); o.objectPos=v.pos.xyz; o.pos=TransformWorldToHClip(o.world); o.normal=TransformObjectToWorldNormal(v.normal); o.uv=v.uv; o.color=v.color; o.fog=ComputeFogFactor(o.pos.z); return o; }
half4 frag(V i):SV_Target {
return half4(MixFog(SAMPLE_TEXTURE2D(_Texture,sampler_Texture,i.uv).rgb*RecoveryLight(i.normal),i.fog),1);
}
ENDHLSL
} } }
