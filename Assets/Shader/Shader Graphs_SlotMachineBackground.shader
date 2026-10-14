Shader "Shader Graphs/SlotMachineBackground" {
	Properties {
		[HideInInspector] [NoScaleOffset] _MainTex ("_MainTex", 2D) = "white" {}
		_Emission ("Emission", Float) = 0
		_MainColor ("MainColor", Vector) = (1,1,1,1)
		_SecondColor ("SecondColor", Vector) = (1,1,1,1)
		_Tightness ("Tightness", Float) = 0.75
		_Tiling ("Tiling", Float) = 1
		_TilingSpeed ("TilingSpeed", Vector) = (0.1,0,0,0)
		_WaveSpeed ("WaveSpeed", Vector) = (3,0.1,0,0)
		_WaveAmount ("WaveAmount", Float) = 0
		_WaveOffset ("WaveOffset", Float) = 0
		_NoiseScale ("NoiseScale", Float) = 1
		_NoiseAmount ("NoiseAmount", Float) = 0.25
		_Tightness2 ("Tightness2", Float) = 0.75
		_Tiling2 ("Tiling2", Float) = 1
		_TilingSpeed2 ("TilingSpeed2", Vector) = (0.1,0,0,0)
		_WaveSpeed2 ("WaveSpeed2", Vector) = (2,0.1,0,0)
		_WaveAmount2 ("WaveAmount2", Float) = 0
		_WaveOffset2 ("WaveOffset2", Float) = 0
		_NoiseScale2 ("NoiseScale2", Float) = 1
		_NoiseAmount2 ("NoiseAmount2", Float) = 0.25
		_LineColor1 ("LineColor1", Vector) = (0,0,0,1)
		_LineColor2 ("LineColor2", Vector) = (0,0,0,1)
		_LineColor3 ("LineColor3", Vector) = (0,0,0,1)
		_LineColor4 ("LineColor4", Vector) = (0,0,0,1)
		_Pixels ("Pixels", Float) = 1
		_BackgroundSpeed ("BackgroundSpeed", Float) = 0
		_LineThinness ("LineThinness", Float) = 1
		[HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
		[HideInInspector] _Stencil ("Stencil ID", Float) = 0
		[HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
		[HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
		[HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
		[HideInInspector] _ColorMask ("ColorMask", Float) = 15
		[HideInInspector] _ClipRect ("ClipRect", Vector) = (0,0,0,0)
		[HideInInspector] _UIMaskSoftnessX ("UIMaskSoftnessX", Float) = 1
		[HideInInspector] _UIMaskSoftnessY ("UIMaskSoftnessY", Float) = 1
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