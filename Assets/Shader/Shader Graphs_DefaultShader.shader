Shader "Shader Graphs/DefaultShader" {
	Properties {
		[NoScaleOffset] _Colors ("Colors", 2D) = "white" {}
		_Emission ("Emission", Float) = 0
		_MetallicAngleSmoothStep ("MetallicAngleSmoothStep", Vector) = (0.4,0.75,0,0)
		[NoScaleOffset] _Normal_Map ("Normal Map", 2D) = "white" {}
		_Normal_Strength ("Normal Strength", Range(0, 1)) = 1
		_Normal_Scale ("Normal Scale", Float) = 0.2
		_Cookness ("Cookness", Range(0, 2)) = 0
		_CookColor ("CookColor", Vector) = (1,0.4874805,0,1)
		_BurntColor ("BurntColor", Vector) = (0,0,0,1)
		_Cooked_Normal_Strength ("Cooked Normal Strength", Range(0, 2)) = 0.7
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
 #pragma target 3.5
 #pragma vertex vert
 #pragma fragment frag
 #pragma shader_feature_local _SKIN_TYPE_GRADIENT_NOISE _SKIN_TYPE_CHECKERBOARD _SKIN_TYPE_VORONOI
 #pragma shader_feature_local _SKIN_AFFECTS_ONLY_METALLIC _SKIN_AFFECTS_ONLY_METALLIC_NOISE _SKIN_AFFECTS_EVERYTHING
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
 #define RECOVERY_COOKABLE 1
 #include "RecoveryItemSurface.hlsl"

 struct A { float4 pos:POSITION; float3 normal:NORMAL; float4 tangent:TANGENT; float2 uv:TEXCOORD0; float2 materialMask:TEXCOORD1; float2 secondaryPalette:TEXCOORD2; float2 patternUV:TEXCOORD3; };
 struct V { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float2 uv:TEXCOORD1; float3 world:TEXCOORD2; float2 materialMask:TEXCOORD3; float2 secondaryPalette:TEXCOORD4; float2 patternUV:TEXCOORD5; float4 tangent:TEXCOORD6; };
 V vert(A v) { V o; o.world=TransformObjectToWorld(v.pos.xyz); o.pos=TransformWorldToHClip(o.world); o.normal=TransformObjectToWorldNormal(v.normal); o.uv=v.uv; o.materialMask=v.materialMask; o.secondaryPalette=v.secondaryPalette; o.patternUV=v.patternUV; o.tangent=float4(TransformObjectToWorldDir(v.tangent.xyz),v.tangent.w*GetOddNegativeScale()); return o; }
 half4 frag(V i):SV_Target {
 half3 color=RecoveryItemColor(i.uv,i.materialMask,i.patternUV);
 float3 N=normalize(i.normal), T=normalize(i.tangent.xyz), B=cross(N,T)*i.tangent.w;
 float3 normalTS=UnpackNormal(SAMPLE_TEXTURE2D(_Normal_Map,sampler_Normal_Map,i.patternUV*_Normal_Scale));
 float normalStrength=_Normal_Strength;
 #ifdef RECOVERY_COOKABLE
 normalStrength=lerp(normalStrength,_Cooked_Normal_Strength,saturate(_Cookness));
 #endif
 float3 perturbed=normalize(T*normalTS.x+B*normalTS.y+N*normalTS.z);
 N=normalize(lerp(N,perturbed,normalStrength));
 float metallicRegion=step(_Metallic_Cutoff,i.materialMask.x);
 float metallic=lerp(_PlasticMetallicness,_MetallicMetallicness,metallicRegion);
 float smoothness=lerp(_PlasticSmoothness,_MetallicSmoothness,step(.9,i.materialMask.x));
 if(metallic<.01) metallic=i.materialMask.x;
 if(smoothness<.01) smoothness=clamp(i.materialMask.y,0,.7);
 #ifdef RECOVERY_COOKABLE
 metallic=saturate(metallic-_Cookness); smoothness=clamp(smoothness-_Cookness,0,.7);
 #endif
 if(i.materialMask.x+i.materialMask.y>=1.8) { metallic=1; smoothness=.7; }
 // Reconstructed URP lighting. Original compiled lighting/graph source is not recovered.
 Light light=GetMainLight(); float3 view=SafeNormalize(GetCameraPositionWS()-i.world);
 float3 halfDirection=SafeNormalize(light.direction+view);
 half3 illumination=SampleSH(N)+light.color*(.2+.8*saturate(dot(N,light.direction)));
 half specular=pow(saturate(dot(N,halfDirection)),lerp(8,256,smoothness))*smoothness;
 half3 reflection=lerp(.04,color,metallic)*light.color*specular;
 half3 emission=SAMPLE_TEXTURE2D(_Colors,sampler_Colors,i.secondaryPalette).rgb*_Emission;
 return half4(color*max(illumination,.25)+reflection+emission,1);
 }
 ENDHLSL }
} }