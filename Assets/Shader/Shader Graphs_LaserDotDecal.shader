Shader "Shader Graphs/LaserDotDecal" {
	Properties {
		_Center ("Center", Vector) = (0.5,0.5,0,0)
		_SmoothStep ("SmoothStep", Vector) = (0,0,0,0)
		[HDR] _Innercolor ("Innercolor", Vector) = (1,1,1,1)
		[HDR] _OuterColor ("OuterColor", Vector) = (1,0,0,1)
		[HideInInspector] _DrawOrder ("Draw Order", Range(-50, 50)) = 0
		[Enum(Default, 0, Depth Bias, 0, View Bias, 1)] [HideInInspector] _DecalMeshBiasType ("DecalMesh BiasType", Float) = 0
		[HideInInspector] _DecalMeshDepthBias ("DecalMesh DepthBias", Float) = 0
		[HideInInspector] _DecalMeshViewBias ("DecalMesh ViewBias", Float) = 0
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
	}
	// Recovery implementation: original decal fragment logic plus URP screen-space projector compatibility.
SubShader { Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
HLSLINCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareRenderingLayerTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderVariablesDecal.hlsl"
struct A { float4 pos:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
V vert(A v) { V o; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.pos=TransformObjectToHClip(v.pos.xyz); o.uv=v.uv; return o; }
float2 ProjectorUV(V i,out float3 world,out float fade) {
 UNITY_SETUP_INSTANCE_ID(i); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 float2 screenUV=GetNormalizedScreenSpaceUV(i.pos);
 float depth=SampleSceneDepth(screenUV);
 #if !UNITY_REVERSED_Z
 depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
 #endif
 world=ComputeWorldSpacePosition(screenUV,depth,UNITY_MATRIX_I_VP);
 float3 local=TransformWorldToObject(world)*float3(1,-1,1);
 clip(.5-max(max(abs(local.x),abs(local.y)),abs(local.z)));
 #if defined(_DECAL_LAYERS)
 uint sceneLayer=LoadSceneRenderingLayer(uint2(i.pos.xy));
 uint decalLayer=asuint(UNITY_ACCESS_INSTANCED_PROP(Decal,_DecalLayerMaskFromDecal));
 clip((sceneLayer&decalLayer)!=0?1:-1);
 #endif
 half4x4 normalToWorld=UNITY_ACCESS_INSTANCED_PROP(Decal,_NormalToWorld);
 fade=saturate(normalToWorld[0][3]);
 float2 uv=local.xz+.5;
 return uv*float2(normalToWorld[3][0],normalToWorld[3][1])+float2(normalToWorld[3][2],normalToWorld[3][3]);
}
half3 DecalLight(float3 world) { float3 N=normalize(cross(ddy(world),ddx(world))); Light light=GetMainLight(); return max(SampleSH(N)+light.color*saturate(dot(N,light.direction)),.25); }
float4 _Center,_SmoothStep,_Innercolor,_OuterColor;
half4 DecalColor(float2 uv) {

float alpha=1-smoothstep(_SmoothStep.x,max(_SmoothStep.y,_SmoothStep.x+.0001),distance(uv,_Center.xy));
return half4(lerp(_OuterColor.rgb,_Innercolor.rgb,alpha),alpha);

}
ENDHLSL
Pass { Name "DecalScreenSpaceProjector" Tags { "LightMode"="DecalScreenSpaceProjector" }
Cull Front ZWrite Off ZTest Greater Blend SrcAlpha OneMinusSrcAlpha
HLSLPROGRAM
#pragma target 3.5
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#pragma multi_compile _ _DECAL_LAYERS
half4 frag(V i):SV_Target { float3 world; float fade; float2 uv=ProjectorUV(i,world,fade); half4 color=DecalColor(uv); color.a*=fade; color.rgb*=DecalLight(world); return color; }
ENDHLSL }
Pass { Name "DecalScreenSpaceMesh" Tags { "LightMode"="DecalScreenSpaceMesh" }
Cull Back ZWrite Off ZTest LEqual Offset -1,-1 Blend SrcAlpha OneMinusSrcAlpha
HLSLPROGRAM
#pragma target 3.5
#pragma vertex vert
#pragma fragment fragMesh
half4 fragMesh(V i):SV_Target { return DecalColor(i.uv); }
ENDHLSL }
} }
