Shader "Shader Graphs/FlagShader" {
	Properties {
		[NoScaleOffset] _Colors ("Colors", 2D) = "white" {}
		_WindStrength ("WindStrength", Float) = 0
		_NoiseScale ("NoiseScale", Float) = 1
		_WindNoiseAxis ("WindNoiseAxis", Vector) = (0,0,0,0)
		_WindSpeed ("WindSpeed", Vector) = (0,0,0,0)
		_WindAxis ("WindAxis", Vector) = (0,0,0,0)
		_WindAxis2 ("WindAxis2", Vector) = (0,0,0,0)
		[NoScaleOffset] _Normal_Bumps_Map ("Normal Bumps Map", 2D) = "white" {}
		_Normal_Bumps_Scale ("Normal Bumps Scale", Vector) = (1,2,0,0)
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
 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
 struct A { float4 pos:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
 struct V { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float2 uv:TEXCOORD1; };
 TEXTURE2D(_Colors); SAMPLER(sampler_Colors);
 float _WindStrength,_NoiseScale; float4 _WindAxis,_WindAxis2,_WindSpeed;
 float2 FlagNoiseDirection(float2 p) { float x; Hash_Tchou_2_1_float(p,x); return normalize(float2(x-floor(x+0.5),abs(x)-0.5)); }
 float FlagGradientNoise(float2 p) {
 float2 ip=floor(p),fp=frac(p);
 float d00=dot(FlagNoiseDirection(ip),fp), d01=dot(FlagNoiseDirection(ip+float2(0,1)),fp-float2(0,1));
 float d10=dot(FlagNoiseDirection(ip+float2(1,0)),fp-float2(1,0)), d11=dot(FlagNoiseDirection(ip+float2(1,1)),fp-float2(1,1));
 float2 f=fp*fp*fp*(fp*(fp*6-15)+10);
 return lerp(lerp(d00,d01,f.y),lerp(d10,d11,f.y),f.x)+0.5;
 }
 V vert(A v) { V o;
 // Original vertex program-0: noise(positionOS.xy + time*windSpeed.xy),
 // weighted by positionOS.z squared, signed axis plus absolute second axis.
 float displacement=(FlagGradientNoise((v.pos.xy+_Time.y*_WindSpeed.xy)*_NoiseScale)*2-1)*_WindStrength*v.pos.z*v.pos.z;
 v.pos.xyz+=displacement*_WindAxis.xyz+abs(displacement)*_WindAxis2.xyz;
 o.pos=TransformObjectToHClip(v.pos.xyz); o.normal=TransformObjectToWorldNormal(v.normal); o.uv=v.uv; return o; }
 half4 frag(V i):SV_Target { Light light=GetMainLight(); half3 color=SAMPLE_TEXTURE2D(_Colors,sampler_Colors,i.uv).rgb;
 half3 illumination=SampleSH(normalize(i.normal))+light.color*(0.2+0.8*saturate(dot(normalize(i.normal),light.direction)));
 return half4(color*max(illumination,0.25),1); }
 ENDHLSL }
} }
