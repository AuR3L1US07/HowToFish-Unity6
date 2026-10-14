Shader "Shader Graphs/WaterShader" {
	Properties {
		[ToggleUI] _Testing ("Testing", Float) = 0
		_DeepColor ("Deep Color", Vector) = (0.03921569,0.09803922,0.2745098,0.7058824)
		_DeepColor_1 ("Deep Color 2", Vector) = (0.03921569,0.09803922,0.2745098,0.7058824)
		Color_93e06cd551a5449091bcde90b46765a0 ("Shallow Color", Vector) = (0.03804734,0.5490196,0.5490196,0.1960784)
		Color_1 ("Shallow Color 2", Vector) = (0.03804734,0.5490196,0.5490196,0.1960784)
		_Pixels ("Pixels", Float) = 0
		_Color_Noise_Scale ("Color Noise Scale", Float) = 0
		Vector1_6f56a0970372485390c6587863c2374e ("Depth", Float) = -3.5
		Vector1_6c82dffdd68049bcb019d3a9c64c92a0 ("Depth Strenght", Range(0, 2)) = 0.2
		Vector1_6269b1025b26473ca8bc61634f34b537 ("Smoothness", Range(0, 1)) = 0.95
		_Wave_Frequency_1 ("Wave Frequency 1", Float) = 1
		_Gradient_Speed_1 ("Gradient Speed 1", Float) = 1
		_Wave_Direction_1 ("Wave Direction 1", Vector) = (1,0,0,0)
		_Wave_Height_1 ("Wave Height 1", Float) = 0
		_Peak_1 ("Peak 1", Float) = 1
		_Wave_Frequency_2 ("Wave Frequency 2", Float) = 1
		_Gradient_Speed_2 ("Gradient Speed 2", Float) = 1
		_Wave_Direction_2 ("Wave Direction 2", Vector) = (1,0,0,0)
		_Wave_Height_2 ("Wave Height 2", Float) = 0
		_Peak_2 ("Peak 2", Float) = 1
		_Foam_Amount ("Foam Amount", Float) = 40
		_Foam_Threshold ("Foam Threshold", Float) = 50
		_Foam_Speed ("Foam Speed", Float) = 0.05
		_Foam_Scale ("Foam Scale", Float) = 200
		_Foam_Color ("Foam Color", Vector) = (1,1,1,1)
		[NoScaleOffset] _Normal_Bumps_Map ("Normal Bumps Map", 2D) = "white" {}
		_Normal_Bumps_Strength ("Normal Bumps Strength", Range(0, 1)) = 0.2
		_Normal_Bumps_Fade_Speed ("Normal Bumps Fade Speed", Float) = 1
		_Normal_Bumps_Scale ("Normal Bumps Scale", Vector) = (1,2,0,0)
		_Normal_Bumps_2_Offset ("Normal Bumps 2 Offset", Float) = 100
		_Specular_Smoothness ("Specular Smoothness", Range(0, 1)) = 0
		[HDR] _Specular_Color ("Specular Color", Vector) = (0,0,0,1)
		_Specular_SmoothStep ("Specular SmoothStep", Vector) = (0,1,0,0)
		[HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
		[HideInInspector] _QueueControl ("_QueueControl", Float) = -1
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
	}
	// Recovery fallback: basic environment rendering, not the original graph.

 SubShader { Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"  }
 
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 struct A { float4 pos:POSITION; };
 struct V { float4 pos:SV_POSITION; float3 direction:TEXCOORD0; float3 world:TEXCOORD1; };
 float4 _DeepColor, _DeepColor_1;
 float _Wave_Frequency_1, _Wave_Frequency_2, _Gradient_Speed_1, _Gradient_Speed_2, _Wave_Height_1, _Wave_Height_2, _Wave_Offset;
 float4 _Wave_Direction_1, _Wave_Direction_2;
 float WaterHash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
 V vert(A v) { V o; float3 world=TransformObjectToWorld(v.pos.xyz);
 world.y += sin(dot(normalize(_Wave_Direction_1.xyz),world)*_Wave_Frequency_1+(_Time.y+_Wave_Offset)*_Gradient_Speed_1)*_Wave_Height_1;
 world.y += sin(dot(normalize(_Wave_Direction_2.xyz),world)*_Wave_Frequency_2+(_Time.y+_Wave_Offset)*_Gradient_Speed_2)*_Wave_Height_2;
 o.pos=TransformWorldToHClip(world); o.direction=v.pos.xyz; o.world=world; return o; }
 half4 frag(V i):SV_Target {
 float3 normal=normalize(cross(ddy(i.world),ddx(i.world)));
 normal*=normal.y<0?-1:1;
 float2 facetUV=i.world.xz*0.18+_Time.y*float2(0.018,0.011);
 float2 cell=floor(facetUV);
 float side=step(1,frac(facetUV.x)+frac(facetUV.y));
 float waveShade=WaterHash(cell+side*float2(17.3,9.7));
 normal=normalize(normal+float3((waveShade-0.5)*0.45,0,(WaterHash(cell+13.7)-0.5)*0.4));
 float3 view=normalize(GetCameraPositionWS()-i.world);
 float fresnel=pow(1-saturate(dot(view,normal)),3);
 float t=saturate(0.5+normal.x*1.3+normal.z*0.8);
 float3 water=lerp(float3(0.13,0.28,0.43),float3(0.26,0.43,0.59),t);
 float3 sky=lerp(float3(0.25,0.40,0.56),float3(0.38,0.55,0.70),saturate(reflect(-view,normal).y));
 water=lerp(water,sky,0.35+fresnel*0.45);
 water*=0.82+0.35*saturate(dot(normal,GetMainLight().direction));
 water*=0.88+waveShade*0.24;
 float distanceToCamera=distance(GetCameraPositionWS(),i.world);
 water=lerp(water,float3(0.36,0.54,0.68),1-exp(-distanceToCamera*0.0025));
 return half4(water,1); }
 ENDHLSL }
 } }
 
