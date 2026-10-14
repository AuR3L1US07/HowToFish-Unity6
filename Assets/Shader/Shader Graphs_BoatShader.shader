Shader "Shader Graphs/BoatShader" {
	Properties {
		[NoScaleOffset] _Colors ("Colors", 2D) = "white" {}
		_Emission ("Emission", Float) = 0
		[NoScaleOffset] _Normal_Map ("Normal Map", 2D) = "white" {}
		_Normal_Strength ("Normal Strength", Range(0, 1)) = 1
		_Normal_Scale ("Normal Scale", Float) = 0.2
		[ToggleUI] _Use_Skin ("Use Skin", Float) = 0
		[ToggleUI] _Rainbow_Skin ("Rainbow Skin", Float) = 0
		[KeywordEnum(Gradient Noise, Checkerboard, Voronoi)] _SKIN_TYPE ("Skin Type", Float) = 0
		[KeywordEnum(Only Metallic, Only Metallic Noise, Everything)] _SKIN_AFFECTS ("Skin Affects", Float) = 0
		_Metallic_Cutoff ("Metallic Cutoff", Range(0, 1)) = 0.5
		_Color_Offset ("Color Offset", Vector) = (0,0,0,0)
		_Color_Offset_2 ("Color Offset 2", Vector) = (0,0,0,0)
		_Skin_Smooth_Step ("Skin Smooth Step", Vector) = (0.5,0.5,0,0)
		_Skin_Noise_Scale ("Skin Noise Scale", Float) = 10
		_Noise_Rotation ("Noise Rotation", Float) = 45
		_UV_Rotation ("UV Rotation", Float) = 0
		_Skin_UV_Scale ("Skin UV Scale", Vector) = (1,1,0,0)
		_MetallicMetallicness ("MetallicMetallicness", Range(0, 1)) = 0
		_MetallicSmoothness ("MetallicSmoothness", Range(0, 1)) = 0
		_PlasticMetallicness ("PlasticMetallicness", Range(0, 1)) = 0
		_PlasticSmoothness ("PlasticSmoothness", Range(0, 1)) = 0
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