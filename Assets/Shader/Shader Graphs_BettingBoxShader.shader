Shader "Shader Graphs/BettingBoxShader" {
	Properties {
		[HDR] _Base_Color ("Base Color", Vector) = (0,0,0,1)
		_Smoothstep ("Smoothstep", Vector) = (0,0,0,0)
		_Alpha_Speed ("Alpha Speed", Float) = 5
		_AlphaMap ("AlphaMap", Vector) = (0,1,0,0)
		_WaveDensity ("WaveDensity", Float) = 1
		_Alpha_Clip ("Alpha Clip", Float) = 0
		_WidthPixelation ("WidthPixelation", Float) = 10
		_HeightPixelation ("HeightPixelation", Float) = 0
		[HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
		[HideInInspector] _QueueControl ("_QueueControl", Float) = -1
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
	}
	// Recovery implementation based on original DXBC and render state; lighting remains reconstructed.
SubShader {
Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
Blend SrcAlpha OneMinusSrcAlpha
ZWrite Off
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
float4 _Base_Color,_Smoothstep,_AlphaMap; float _Alpha_Speed,_WaveDensity,_Alpha_Clip,_WidthPixelation,_HeightPixelation;
V vert(A v) { V o; float3 world=TransformObjectToWorld(v.pos.xyz);

o.world=TransformObjectToWorld(v.pos.xyz); o.objectPos=v.pos.xyz; o.pos=TransformWorldToHClip(o.world); o.normal=TransformObjectToWorldNormal(v.normal); o.uv=v.uv; o.color=v.color; o.fog=ComputeFogFactor(o.pos.z); return o; }
half4 frag(V i):SV_Target {

float2 quantize=max(float2(_WidthPixelation,_HeightPixelation),.0001);
float2 coordinates=floor(float2((i.objectPos.x+i.objectPos.z)*_WaveDensity,(i.objectPos.y+1)*.5)*quantize)/quantize;
float wave=lerp(_AlphaMap.x,_AlphaMap.y,sin(coordinates.x+_Time.y*_Alpha_Speed));
float alpha=1-smoothstep(_Smoothstep.x,max(_Smoothstep.y,_Smoothstep.x+.0001),coordinates.y*wave);
clip(alpha-_Alpha_Clip); return half4(_Base_Color.rgb,alpha);

}
ENDHLSL
} } }
