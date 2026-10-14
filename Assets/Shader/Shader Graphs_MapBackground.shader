Shader "Shader Graphs/MapBackground" {
	Properties {
		[HideInInspector] [NoScaleOffset] _MainTex ("_MainTex", 2D) = "white" {}
		[HideInInspector] _Stencil ("_Stencil", Float) = 0
		[HideInInspector] _StencilComp ("_StencilComp", Float) = 8
		[HideInInspector] _StencilOp ("_StencilOp", Float) = 0
		[HideInInspector] _StencilWriteMask ("_StencilWriteMask", Float) = 255
		[HideInInspector] _StencilReadMask ("_StencilReadMask", Float) = 255
		[HideInInspector] _ColorMask ("_ColorMask", Float) = 15
		_GridColor ("GridColor", Vector) = (0.01013041,1,0,1)
		_Pixels ("Pixels", Float) = 1
		_BackgroundColor ("BackgroundColor", Vector) = (0,0,0,1)
		_GridThickness ("GridThickness", Float) = 0.12
		_PlayerOffsetMultiplier ("PlayerOffsetMultiplier", Float) = 4
		_CircleDistanceCheck ("CircleDistanceCheck", Float) = 1
		_CircleSmoothstep ("CircleSmoothstep", Vector) = (0.5,1,0,0)
		[HideInInspector] White ("Color", Vector) = (1,1,1,1)
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
	}
	// Recovery fallback: original graph source was not present in the shipped game.

SubShader {
 Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
 Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct A { float4 pos:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
 struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
 sampler2D _MainTex; float4 _TextureSampleAdd;
 
 V vert(A v) { V o; o.pos=UnityObjectToClipPos(v.pos); o.uv=v.uv; o.color=v.color; return o; }
 half4 frag(V i):SV_Target { half4 c=(tex2D(_MainTex,i.uv)+_TextureSampleAdd)*i.color;
 
 return c; }
 ENDHLSL }
} }