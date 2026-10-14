Shader "Shader Graphs/UnderwaterShader" {
	Properties {
		[HideInInspector] [NoScaleOffset] _MainTex ("_MainTex", 2D) = "white" {}
		_UnderwaterColor ("UnderwaterColor", Vector) = (1,1,1,1)
		_Distortion1 ("Distortion1", Range(0, 1)) = 0.1
		_Distortion2 ("Distortion2", Range(0, 1)) = 0.1
		_WaveScale1 ("WaveScale1", Float) = 1
		_WaveScale2 ("WaveScale2", Float) = 1
		_WaveSpeed1 ("WaveSpeed1", Vector) = (0,0,0,0)
		_WaveSpeed2 ("WaveSpeed2", Vector) = (0,0,0,0)
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