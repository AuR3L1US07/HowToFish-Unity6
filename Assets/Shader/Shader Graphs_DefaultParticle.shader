Shader "Shader Graphs/DefaultParticle" {
	Properties {
		_Color ("Color", Vector) = (0,0,0,1)
		[NoScaleOffset] _MainTex ("_MainTex", 2D) = "white" {}
		_AlphaClipping ("AlphaClipping", Range(0, 1)) = 0
		_NoiseStrength ("NoiseStrength", Range(0, 1)) = 0
		_NoiseScale ("NoiseScale", Float) = 0.1
		[NoScaleOffset] _Noise ("Noise", 2D) = "white" {}
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
 float4 _Color; sampler2D _MainTex; float _AlphaClipping;
 V vert(A v) { V o; o.pos=UnityObjectToClipPos(v.pos); o.uv=v.uv; o.color=v.color; return o; }
 half4 frag(V i):SV_Target {
 half4 c=tex2D(_MainTex,i.uv)*i.color*_Color; clip(c.a-max(_AlphaClipping,0.01)); return c;
 }
 ENDHLSL } } }
 