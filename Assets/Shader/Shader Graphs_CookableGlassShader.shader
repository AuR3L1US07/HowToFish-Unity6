Shader "Shader Graphs/CookableGlassShader" {
	Properties {
		_Smooth_Step ("Smooth Step", Vector) = (0,0,0,0)
		_Distance_Min_Max ("Distance Min Max", Vector) = (1,5,0,0)
		[NoScaleOffset] _Colors ("Colors", 2D) = "white" {}
		_AlphaMax ("AlphaMax", Float) = 0
		_Normal_Scale ("Normal Scale", Float) = 0.3
		_Normal_Strength ("Normal Strength", Range(0, 1)) = 0.2
		[NoScaleOffset] _Normal_Texture ("Normal Texture", 2D) = "white" {}
		[HideInInspector] _Cookness ("Cookness", Float) = 0
		_CookColor ("CookColor", Vector) = (0,0,0,1)
		_BurntColor ("BurntColor", Vector) = (0,0,0,1)
		[HideInInspector] _WorkflowMode ("_WorkflowMode", Float) = 1
		[HideInInspector] _CastShadows ("_CastShadows", Float) = 1
		[HideInInspector] _ReceiveShadows ("_ReceiveShadows", Float) = 1
		[HideInInspector] _Surface ("_Surface", Float) = 1
		[HideInInspector] _Blend ("_Blend", Float) = 0
		[HideInInspector] _AlphaClip ("_AlphaClip", Float) = 0
		[HideInInspector] _BlendModePreserveSpecular ("_BlendModePreserveSpecular", Float) = 1
		[HideInInspector] _SrcBlend ("_SrcBlend", Float) = 1
		[HideInInspector] _DstBlend ("_DstBlend", Float) = 0
		[HideInInspector] _SrcBlendAlpha ("_SrcBlendAlpha", Float) = 1
		[HideInInspector] _DstBlendAlpha ("_DstBlendAlpha", Float) = 0
		[ToggleUI] [HideInInspector] _ZWrite ("_ZWrite", Float) = 0
		[HideInInspector] _ZWriteControl ("_ZWriteControl", Float) = 0
		[HideInInspector] _ZTest ("_ZTest", Float) = 4
		[HideInInspector] _Cull ("_Cull", Float) = 2
		[HideInInspector] _AlphaToMask ("_AlphaToMask", Float) = 0
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

 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
 struct A { float4 pos:POSITION; float3 normal:NORMAL; float4 tangent:TANGENT; float2 uv:TEXCOORD0; float2 normalUV:TEXCOORD1; float2 materialMask:TEXCOORD2; };
 struct V { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float2 uv:TEXCOORD1; float3 world:TEXCOORD2; float2 normalUV:TEXCOORD3; float2 materialMask:TEXCOORD4; float4 tangent:TEXCOORD5; };
 TEXTURE2D(_Colors); SAMPLER(sampler_Colors);
 TEXTURE2D(_Normal_Texture); SAMPLER(sampler_Normal_Texture);
 float _Cookness, _Normal_Scale, _Normal_Strength; float4 _CookColor, _BurntColor, _Distance_Min_Max;
 V vert(A v) { V o; o.world=TransformObjectToWorld(v.pos.xyz); o.pos=TransformWorldToHClip(o.world); o.normal=TransformObjectToWorldNormal(v.normal); o.uv=v.uv; o.normalUV=v.normalUV; o.materialMask=v.materialMask; o.tangent=float4(TransformObjectToWorldDir(v.tangent.xyz),v.tangent.w*GetOddNegativeScale()); return o; }
 half4 frag(V i):SV_Target {
 half3 color=SAMPLE_TEXTURE2D(_Colors,sampler_Colors,i.uv).rgb;
 // Original DXBC fragment-0: palette -> CookColor -> BurntColor, alpha always one.
 color=lerp(color,_CookColor.rgb,saturate(_Cookness));
 color=lerp(color,_BurntColor.rgb,saturate(_Cookness-1));
 float3 N=normalize(i.normal), T=normalize(i.tangent.xyz), B=cross(N,T)*i.tangent.w;
 float3 view=GetCameraPositionWS()-i.world;
 float attenuation=1-saturate((length(view)-_Distance_Min_Max.x)/max(_Distance_Min_Max.y-_Distance_Min_Max.x,.0001));
 float3 normalTS=UnpackNormal(SAMPLE_TEXTURE2D(_Normal_Texture,sampler_Normal_Texture,i.normalUV*_Normal_Scale));
 N=normalize(lerp(N,normalize(T*normalTS.x+B*normalTS.y+N*normalTS.z),_Normal_Strength*attenuation));
 Light light=GetMainLight();
 half3 illumination=SampleSH(N)+light.color*(.2+.8*saturate(dot(N,light.direction)));
 float2 material=saturate(i.materialMask-_Cookness);
 half specular=pow(saturate(dot(N,SafeNormalize(light.direction+SafeNormalize(view)))),lerp(8,256,material.y))*material.y;
 return half4(color*max(illumination,.25)+lerp(.04,color,material.x)*light.color*specular,1);
 }
 ENDHLSL }
} }