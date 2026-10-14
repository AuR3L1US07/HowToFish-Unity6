Shader "Shader Graphs/WaterShader" {
	Properties {
		[ToggleUI] _Testing ("Testing", Float) = 0
		[HideInInspector] _Fake_Time ("Recovery simulation time", Float) = 0
		[HideInInspector] _Wave_Offset ("Simulation wave offset", Float) = 0
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
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
 struct A { float4 pos:POSITION; float2 uv:TEXCOORD0; };
 struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; };
 float4 _DeepColor, _DeepColor_1, Color_1, Color_93e06cd551a5449091bcde90b46765a0, _Foam_Color;
 float _Testing,_Fake_Time,_Peak_1,_Peak_2,_Pixels,_Color_Noise_Scale,_Foam_Amount,_Foam_Threshold,_Foam_Speed,_Foam_Scale;
 float Vector1_6f56a0970372485390c6587863c2374e, Vector1_6c82dffdd68049bcb019d3a9c64c92a0;
 float _Wave_Frequency_1, _Wave_Frequency_2, _Gradient_Speed_1, _Gradient_Speed_2, _Wave_Height_1, _Wave_Height_2, _Wave_Offset;
 float4 _Wave_Direction_1, _Wave_Direction_2;
 float WaterHash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
 float WaterValueNoise(float2 p) {
 float2 ip=floor(p),f=frac(p); f=f*f*(3-2*f); float a,b,c,d;
 Hash_Tchou_2_1_float(ip,a); Hash_Tchou_2_1_float(ip+float2(1,0),b);
 Hash_Tchou_2_1_float(ip+float2(0,1),c); Hash_Tchou_2_1_float(ip+1,d);
 return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y); }
 float2 WaterNoiseDirection(float2 p) { float x; Hash_Tchou_2_1_float(p,x); return normalize(float2(x-floor(x+.5),abs(x)-.5)); }
 float WaterGradientNoise(float2 p) { float2 ip=floor(p),fp=frac(p),f=fp*fp*fp*(fp*(fp*6-15)+10);
 float a=dot(WaterNoiseDirection(ip),fp),b=dot(WaterNoiseDirection(ip+float2(0,1)),fp-float2(0,1));
 float c=dot(WaterNoiseDirection(ip+float2(1,0)),fp-float2(1,0)),d=dot(WaterNoiseDirection(ip+1),fp-1);
 return lerp(lerp(a,b,f.y),lerp(c,d,f.y),f.x)+.5; }
 float3 WaterWave(float3 world,float3 rawDirection,float frequency,float speed,float height,float peak,float time) {
 float3 direction=rawDirection*rsqrt(max(dot(rawDirection,rawDirection),.000001));
 float phase=dot(direction,world)*frequency+time*speed;
 return float3(rawDirection.x*peak*height*cos(phase),height*sin(phase),rawDirection.z*peak*height*cos(phase)); }
 V vert(A v) { V o; float3 world=TransformObjectToWorld(v.pos.xyz);
 float time=(_Testing!=0?_Time.y:_Fake_Time)+_Wave_Offset;
 // Original globalgamemanagers.assets vertex DXBC program-0: world phase,
 // Gerstner horizontal peak offsets then displacement in object coordinates.
 float3 displaced=float3(v.pos.x,0,v.pos.z)+WaterWave(world,_Wave_Direction_1.xyz,_Wave_Frequency_1,_Gradient_Speed_1,_Wave_Height_1,_Peak_1,time)
 +WaterWave(world,_Wave_Direction_2.xyz,_Wave_Frequency_2,_Gradient_Speed_2,_Wave_Height_2,_Peak_2,time);
 o.world=TransformObjectToWorld(displaced); o.pos=TransformWorldToHClip(o.world); o.uv=v.uv; return o; }
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
 float2 pixelUV=_Pixels>0?floor(i.uv*_Pixels)/_Pixels:i.uv;
 float2 noiseUV=pixelUV*_Color_Noise_Scale;
 float colorNoise=WaterValueNoise(noiseUV)*.125+WaterValueNoise(noiseUV*.5)*.25+WaterValueNoise(noiseUV*.25)*.5;
 float sceneEye=LinearEyeDepth(SampleSceneDepth(GetNormalizedScreenSpaceUV(i.pos)),_ZBufferParams);
 float surfaceEye=-TransformWorldToView(i.world).z;
 float waterDepth=max(0,sceneEye-surfaceEye);
 float depthBlend=saturate((waterDepth-Vector1_6f56a0970372485390c6587863c2374e)*Vector1_6c82dffdd68049bcb019d3a9c64c92a0);
 float3 shallow=lerp(Color_93e06cd551a5449091bcde90b46765a0.rgb,Color_1.rgb,colorNoise);
 float3 deep=lerp(_DeepColor.rgb,_DeepColor_1.rgb,colorNoise);
 float3 water=lerp(shallow,deep,depthBlend);
 float foamNoise=WaterGradientNoise(i.uv*_Foam_Scale+_Time.y*_Foam_Speed);
 float foamThreshold=saturate(max(waterDepth-.01,0)/max(_Foam_Amount,.0001))*_Foam_Threshold;
 float foam=step(foamThreshold,foamNoise);
 water=lerp(water,_Foam_Color.rgb,foam);
 water*=0.82+0.35*saturate(dot(normal,GetMainLight().direction));
 water*=0.88+waveShade*0.24;
 float distanceToCamera=distance(GetCameraPositionWS(),i.world);
 water=lerp(water,float3(0.36,0.54,0.68),1-exp(-distanceToCamera*0.0025));
 return half4(water,1); }
 ENDHLSL }
 } }
 
