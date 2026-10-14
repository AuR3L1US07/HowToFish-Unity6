import pathlib,shutil
project=pathlib.Path(__file__).resolve().parents[1]
folder=project/'Assets/Shader'; backup=project/'ReferenceOnly/BeforeRemainingShaderRepair'; backup.mkdir(exist_ok=True)
shared=r'''
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
'''
def recover(name,decl,body):
    file=folder/f'Shader Graphs_{name}.shader'; source=file.read_text(encoding='utf8')
    if 'DummyShaderTextExporter' not in source:return
    shutil.copyfile(file,backup/file.name)
    head=source[:source.index('//DummyShaderTextExporter')]
    result=head+'// Recovery implementation: original decal fragment logic plus URP screen-space projector compatibility.\nSubShader { Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }\n'
    result+='HLSLINCLUDE\n'+shared+decl+'\nhalf4 DecalColor(float2 uv) {\n'+body+'\n}\nENDHLSL\n'
    result+='''Pass { Name "DecalScreenSpaceProjector" Tags { "LightMode"="DecalScreenSpaceProjector" }
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
'''
    file.write_text(result,encoding='utf8')
recover('LaserDotDecal','float4 _Center,_SmoothStep,_Innercolor,_OuterColor;',r'''
float alpha=1-smoothstep(_SmoothStep.x,max(_SmoothStep.y,_SmoothStep.x+.0001),distance(uv,_Center.xy));
return half4(lerp(_OuterColor.rgb,_Innercolor.rgb,alpha),alpha);
''')
recover('URPDecal','TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap); TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap); float4 _BaseColor; float _NoiseScale,_NoiseStrength,_Alpha;',r'''
half4 texture=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv);
half3 noise=SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,uv*_NoiseScale).rgb;
half3 color=_BaseColor.rgb*lerp(texture.rgb,noise,_NoiseStrength);
return half4(color,step(.1,texture.a)*_Alpha);
''')
file=folder/'Unify_Internal_Blur.shader'; shutil.copyfile(file,backup/file.name)
file.write_text(r'''Shader "Unify/Internal/Blur" {
// Recovery implementation from original globalgamemanagers DXBC, four-tap Kawase pass.
SubShader { Tags { "RenderPipeline"="UniversalPipeline" }
ZWrite Off ZTest Always Cull Off
Pass { Name "Kawase"
HLSLPROGRAM
#pragma vertex Vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
int _Iteration; float4 _BlurParams;
half4 frag(Varyings input):SV_Target {
float2 offset=_BlitTexture_TexelSize.xy*(_BlurParams.x/max(_BlurParams.z,.0001))*(_Iteration*_BlurParams.w+.5);
half4 sum=SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_LinearClamp,input.texcoord+offset,_BlitMipLevel);
sum+=SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_LinearClamp,input.texcoord+float2(offset.x,-offset.y),_BlitMipLevel);
sum+=SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_LinearClamp,input.texcoord+float2(-offset.x,offset.y),_BlitMipLevel);
sum+=SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_LinearClamp,input.texcoord-offset,_BlitMipLevel);
return sum*.25;
} ENDHLSL } } }
''',encoding='utf8')
print('Recovered screen-space decal projectors and four-tap Kawase blur.')
