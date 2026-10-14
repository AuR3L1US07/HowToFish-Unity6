Shader "Shader Graphs/GlassShader" {
	Properties {
		_Smooth_Step ("Smooth Step", Vector) = (0,0,0,0)
		_Distance_Min_Max ("Distance Min Max", Vector) = (1,5,0,0)
		[NoScaleOffset] _Colors ("Colors", 2D) = "white" {}
		_AlphaMax ("AlphaMax", Float) = 0
		_Normal_Scale ("Normal Scale", Float) = 0.3
		_Normal_Strength ("Normal Strength", Range(0, 1)) = 0.2
		[NoScaleOffset] _Normal_Texture ("Normal Texture", 2D) = "white" {}
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
 Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
 Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
 ZWrite [_ZWrite]
 ZTest [_ZTest]
 Cull [_Cull]
 Pass { Tags { "LightMode"="UniversalForward" }
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma shader_feature_local _SKIN_TYPE_GRADIENT_NOISE _SKIN_TYPE_CHECKERBOARD _SKIN_TYPE_VORONOI
 #pragma shader_feature_local _SKIN_AFFECTS_ONLY_METALLIC _SKIN_AFFECTS_ONLY_METALLIC_NOISE _SKIN_AFFECTS_EVERYTHING
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
 struct A { float4 pos:POSITION; float3 normal:NORMAL; float4 tangent:TANGENT; float2 uv:TEXCOORD0; float2 normalUV:TEXCOORD1; float2 alphaUV:TEXCOORD3; };
 struct V { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float2 uv:TEXCOORD1; float3 world:TEXCOORD2; float2 normalUV:TEXCOORD3; float alphaFloor:TEXCOORD4; float4 tangent:TEXCOORD5; };
 TEXTURE2D(_Colors); SAMPLER(sampler_Colors);
 TEXTURE2D(_Normal_Texture); SAMPLER(sampler_Normal_Texture);
 float4 _Smooth_Step, _Distance_Min_Max; float _AlphaMax,_Normal_Scale,_Normal_Strength;
 V vert(A v) { V o; o.world=TransformObjectToWorld(v.pos.xyz); o.pos=TransformWorldToHClip(o.world); o.normal=TransformObjectToWorldNormal(v.normal); o.uv=v.uv; o.normalUV=v.normalUV; o.alphaFloor=v.alphaUV.y; o.tangent=float4(TransformObjectToWorldDir(v.tangent.xyz),v.tangent.w*GetOddNegativeScale()); return o; }
 half4 frag(V i):SV_Target {
 Light light=GetMainLight(); half3 color=SAMPLE_TEXTURE2D(_Colors,sampler_Colors,i.uv).rgb;
 float3 N=normalize(i.normal); float3 view=GetCameraPositionWS()-i.world;
 float distanceRamp=saturate((length(view)-_Distance_Min_Max.x)/max(_Distance_Min_Max.y-_Distance_Min_Max.x,0.0001));
 // DXBC program-100: grazing-angle smoothstep + distance ramp, UV3.y alpha floor.
 float grazing=1-abs(dot(N,normalize(view)));
 float alpha=saturate(min(max(smoothstep(_Smooth_Step.x,max(_Smooth_Step.y,_Smooth_Step.x+0.0001),grazing)+distanceRamp,i.alphaFloor),_AlphaMax));
 float3 tangentNormal=UnpackNormal(SAMPLE_TEXTURE2D(_Normal_Texture,sampler_Normal_Texture,i.normalUV*_Normal_Scale));
 float3 T=normalize(i.tangent.xyz); float3 B=cross(N,T)*i.tangent.w;
 float3 bump=normalize(T*tangentNormal.x+B*tangentNormal.y+N*tangentNormal.z);
 N=normalize(lerp(N,bump,_Normal_Strength*(1-distanceRamp)));
 half3 illumination=SampleSH(N)+light.color*(0.2+0.8*saturate(dot(N,light.direction)));
 return half4(color*max(illumination,0.25),alpha); }
 ENDHLSL }
} }
