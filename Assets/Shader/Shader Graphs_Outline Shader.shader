Shader "Shader Graphs/Outline Shader" {
	Properties {
		[NoScaleOffset] _Colors ("Colors", 2D) = "white" {}
		_Emission ("Emission", Float) = 0
		_Outline_Color ("Outline Color", Vector) = (0,0,0,0)
		_Outline_Thickness ("Outline Thickness", Float) = 0.1
		_Animation_Multiplier ("Animation Multiplier", Float) = 0.5
		_Animation_Speed ("Animation Speed", Float) = 1
		[HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
		[HideInInspector] _QueueControl ("_QueueControl", Float) = -1
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
	}
	// Recovery fallback: original graph source was not present in the shipped game.

SubShader {
 Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
 Pass { Tags { "LightMode"="UniversalForward" }
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma shader_feature_local _SKIN_TYPE_GRADIENT_NOISE _SKIN_TYPE_CHECKERBOARD _SKIN_TYPE_VORONOI
 #pragma shader_feature_local _SKIN_AFFECTS_ONLY_METALLIC _SKIN_AFFECTS_ONLY_METALLIC_NOISE _SKIN_AFFECTS_EVERYTHING
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 struct A { float4 pos:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
 struct V { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float2 uv:TEXCOORD1; };
 TEXTURE2D(_Colors); SAMPLER(sampler_Colors);
 V vert(A v) { V o; o.pos=TransformObjectToHClip(v.pos.xyz); o.normal=TransformObjectToWorldNormal(v.normal); o.uv=v.uv; return o; }
 half4 frag(V i):SV_Target { Light light=GetMainLight(); half3 color=SAMPLE_TEXTURE2D(_Colors,sampler_Colors,i.uv).rgb;
 half3 illumination=SampleSH(normalize(i.normal))+light.color*(0.2+0.8*saturate(dot(normalize(i.normal),light.direction)));
 return half4(color*max(illumination,0.25),1); }
 ENDHLSL }
} }