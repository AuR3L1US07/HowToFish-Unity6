Shader "Shader Graphs/SkyboxShader" {
	Properties {
		_SkyPixels ("SkyPixels", Vector) = (1,1,0,0)
		[HDR] _TopDayColor ("TopDayColor", Vector) = (0,0.4660978,1,1)
		[HDR] _TopNightColor ("TopNightColor", Vector) = (0,0.4660978,1,1)
		[HDR] _BottomDayColor ("BottomDayColor", Vector) = (0.2627451,0.5764706,1,1)
		[HDR] _BottomSunriseColor ("BottomSunriseColor", Vector) = (2.996079,1.050501,0,1)
		[HDR] _BottomNightColor ("BottomNightColor", Vector) = (0.01960784,0.01960784,0.01960784,1)
		_SkyNoiseStrength ("SkyNoiseStrength", Range(0, 1)) = 1
		_SkyBands ("SkyBands", Float) = 6
		_SkyNoiseScale ("SkyNoiseScale", Vector) = (10,10,0,0)
		_SunStep ("SunStep", Range(0, 1)) = 0.5
		_SunPixels ("SunPixels", Float) = 4
		[HDR] _SunColor ("SunColor", Vector) = (1,0.9787216,0.6226415,1)
		_SunIntensity ("SunIntensity", Float) = 8
		_MoonRadius ("MoonRadius", Range(0, 100)) = 0
		[HDR] _MoonColor ("MoonColor", Vector) = (0.5660378,0.5660378,0.5660378,1)
		_MoonIntensity ("MoonIntensity", Float) = 1
		[HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
		[HideInInspector] _QueueControl ("_QueueControl", Float) = -1
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
	}
	// Recovery fallback: basic environment rendering, not the original graph.

 SubShader { Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Background" }
 Cull Off ZWrite Off
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A { float4 pos:POSITION; };
 struct V { float4 pos:SV_POSITION; float3 direction:TEXCOORD0; float3 world:TEXCOORD1; };
 float4 _TopDayColor, _BottomDayColor;
 V vert(A v) { V o; float3 world=TransformObjectToWorld(v.pos.xyz);
 
 o.pos=TransformWorldToHClip(world); o.direction=v.pos.xyz; o.world=world; return o; }
 half4 frag(V i):SV_Target {
 float height=saturate(normalize(i.direction).y);
 return half4(lerp(half3(0.29,0.46,0.61),half3(0.13,0.23,0.35),height),1);
 }
 ENDHLSL }
 } }
 
