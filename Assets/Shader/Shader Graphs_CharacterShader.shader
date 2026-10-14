Shader "Shader Graphs/CharacterShader" {
	Properties {
		[NoScaleOffset] _Colors ("Colors", 2D) = "white" {}
		[NoScaleOffset] _Normal_Map ("Normal Map", 2D) = "white" {}
		_Normal_Strength ("Normal Strength", Range(0, 1)) = 1
		_Normal_Scale ("Normal Scale", Float) = 0.2
		_SkinColor ("SkinColor", Vector) = (0,0,0,0)
		_PrimaryColor ("PrimaryColor", Vector) = (0,0,0,0)
		_SecondColor ("SecondColor", Vector) = (0,0,0,0)
		_ThirdColor ("ThirdColor", Vector) = (0,0,0,0)
		_NormalInfluence ("NormalInfluence", Float) = 0
		_SSSPower ("SSSPower", Float) = 0
		_SSSIntensity ("SSSIntensity", Float) = 0
		_SSSColor ("SSSColor", Vector) = (0,0,0,1)
		_Thickness ("Thickness", Float) = 0
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
 struct A { float4 pos:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float2 colorMask:TEXCOORD2; };
 struct V { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float2 uv:TEXCOORD1; float mask:TEXCOORD2; };
 TEXTURE2D(_Colors); SAMPLER(sampler_Colors);
 float4 _SkinColor, _PrimaryColor, _SecondColor, _ThirdColor;
 V vert(A v) { V o; o.pos=TransformObjectToHClip(v.pos.xyz); o.normal=TransformObjectToWorldNormal(v.normal); o.uv=v.uv; o.mask=v.colorMask.x; return o; }
 // Original DXBC program-10 evidence: TEXCOORD2.x identifies four editable
 // ranges; each parameter is (paletteU,paletteV,enabled), not RGB tint.
 float2 CharacterPaletteUV(V i) {
   float2 uv=i.uv;
   if(i.mask>=0.75 && i.mask<=0.85 && _ThirdColor.z>=0.5) uv=_ThirdColor.xy;
   if(i.mask>=0.55 && i.mask<=0.65 && _SecondColor.z>=0.5) uv=_SecondColor.xy;
   if(i.mask>=0.35 && i.mask<=0.45 && _PrimaryColor.z>=0.5) uv=_PrimaryColor.xy;
   if(i.mask>=0.15 && i.mask<=0.25 && _SkinColor.z>=0.5) uv=_SkinColor.xy;
   return uv;
 }
 half4 frag(V i):SV_Target { Light light=GetMainLight(); half3 color=SAMPLE_TEXTURE2D(_Colors,sampler_Colors,CharacterPaletteUV(i)).rgb;
 half3 illumination=SampleSH(normalize(i.normal))+light.color*(0.2+0.8*saturate(dot(normalize(i.normal),light.direction)));
 return half4(color*max(illumination,0.25),1); }
 ENDHLSL }
} }
