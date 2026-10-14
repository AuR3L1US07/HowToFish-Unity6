Shader "Shader Graphs/CloudSphereShader" {
	Properties {
		_CloudHeigth ("CloudHeigth", Float) = 0
		_Pixels ("Pixels", Float) = 1
		[HDR] _Color ("Color", Vector) = (0.7490196,0.7490196,0.7490196,1)
		[HDR] _SSSColor ("SSSColor", Vector) = (1,0.5607843,0,1)
		_NormalInfluence ("NormalInfluence", Float) = 0
		_SSSPower ("SSSPower", Float) = 0
		_SSSIntensity ("SSSIntensity", Float) = 0
		_Thickness ("Thickness", Float) = 0
		_CloudScale ("CloudScale", Float) = 0
		_DetailScale ("DetailScale", Float) = 10
		_CloudSmoothStep ("CloudSmoothStep", Vector) = (0,1,0,0)
		_DetailSmoothStep ("DetailSmoothStep", Vector) = (0,0,0,0)
		_CombinedSmoothStep ("CombinedSmoothStep", Vector) = (0,0,0,0)
		_HeightSmoothStep ("HeightSmoothStep", Vector) = (0,0,0,0)
		_HeigthSmoothStep2 ("HeigthSmoothStep2", Vector) = (0,0,0,0)
		_CloudSpeed ("CloudSpeed", Vector) = (-0.0005,-0.0025,0,0)
		_DetailSpeed ("DetailSpeed", Vector) = (0.015,0.01,0,0)
		[HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
		[HideInInspector] _QueueControl ("_QueueControl", Float) = -1
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
	}
	
 // Recovery implementation for missing graph: transparent clouds / particle sprites.
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" }
 Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct A { float4 pos:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
 struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
 float4 _Color; float4 _CloudSpeed;
 float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
 float Noise(float2 p) { float2 cell=floor(p), f=frac(p); f=f*f*(3-2*f); return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y); }
 V vert(A v) { V o; o.pos=UnityObjectToClipPos(v.pos); o.uv=v.uv; o.color=v.color; return o; }
 half4 frag(V i):SV_Target {
 float2 p=floor((i.uv*float2(18,9)+_Time.y*_CloudSpeed.xy)*24)/24;
 float n=Noise(p)*0.65+Noise(p*2.1)*0.25+Noise(p*4.3)*0.1;
 float opacity=smoothstep(0.35,0.75,n)*0.25;
 return half4(half3(0.40,0.47,0.55),opacity);
 }
 ENDHLSL } } }
 
