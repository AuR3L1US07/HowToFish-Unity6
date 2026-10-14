Shader "Unify/Internal/Blur" {
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
