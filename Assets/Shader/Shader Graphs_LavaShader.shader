Shader "Shader Graphs/LavaShader" {
	Properties {
		_Color_Speed ("Color Speed", Vector) = (0,0,0,0)
		_Color_Speed_2 ("Color Speed 2", Vector) = (0,0,0,0)
		_Noise_Scale_2 ("Noise Scale 2", Float) = 0
		[HDR] _DeepColor ("Color 1", Vector) = (1,0.3686275,0,1)
		[HDR] Color_93e06cd551a5449091bcde90b46765a0 ("Color 2", Vector) = (1,0.3686275,0,1)
		_Pixelate ("Pixelate", Float) = 5
		_Noise_Scale ("Noise Scale", Float) = 1
		_Color_Smoothstep ("Color Smoothstep", Vector) = (0,1,0,0)
		Vector1_6269b1025b26473ca8bc61634f34b537 ("Smoothness", Range(0, 1)) = 0.95
		Vector1_1 ("Metallic", Range(0, 1)) = 0.95
		_Vertex_Speed ("Vertex Speed", Vector) = (0,0,0,0)
		_Vertex_Height ("Vertex Height", Float) = 1
		_Vertex_Noise_Scale ("Vertex Noise Scale", Float) = 0
		[NoScaleOffset] _Normal_Bumps_Map ("Normal Bumps Map", 2D) = "white" {}
		_Normal_Bumps_Strength ("Normal Bumps Strength", Range(0, 1)) = 0.2
		_Normal_Bumps_Scale ("Normal Bumps Scale", Vector) = (1,2,0,0)
		[HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
		[HideInInspector] _QueueControl ("_QueueControl", Float) = -1
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
	}
	// Recovery implementation based on original DXBC and render state; lighting remains reconstructed.
SubShader {
Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
Blend One Zero
ZWrite On
Cull Back
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
float4 _Vertex_Speed,_Color_Speed,_Color_Speed_2,_DeepColor,Color_93e06cd551a5449091bcde90b46765a0,_Color_Smoothstep; float _Vertex_Noise_Scale,_Vertex_Height,_Pixelate,_Noise_Scale,_Noise_Scale_2,_WorldPositionNoiseMulti;
V vert(A v) { V o; float3 world=TransformObjectToWorld(v.pos.xyz);
v.pos.xyz=float3(v.pos.x,RecoveryGradient((v.uv+_Time.y*_Vertex_Speed.xy)*_Vertex_Noise_Scale)*_Vertex_Height,v.pos.z);
o.world=TransformObjectToWorld(v.pos.xyz); o.objectPos=v.pos.xyz; o.pos=TransformWorldToHClip(o.world); o.normal=TransformObjectToWorldNormal(v.normal); o.uv=v.uv; o.color=v.color; o.fog=ComputeFogFactor(o.pos.z); return o; }
half4 frag(V i):SV_Target {

float2 uv=_Pixelate>0?floor(i.uv*_Pixelate)/_Pixelate:i.uv;
float n1=RecoverySimpleNoise((uv+_Time.y*_Color_Speed.xy)*_Noise_Scale);
float n2=RecoverySimpleNoise((uv+_Time.y*_Color_Speed_2.xy)*_Noise_Scale_2);
float n=smoothstep(_Color_Smoothstep.x,max(_Color_Smoothstep.y,_Color_Smoothstep.x+.0001),(n1+n2)*.5);
half3 color=lerp(_DeepColor.rgb,Color_93e06cd551a5449091bcde90b46765a0.rgb,n);
return half4(MixFog(color*RecoveryLight(i.normal),i.fog),1);

}
ENDHLSL
} } }
