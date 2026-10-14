Shader "Shader Graphs/UIBlur" {
	Properties {
		[HideInInspector] [NoScaleOffset] _MainTex ("_MainTex", 2D) = "white" {}
		_Pixel_Ratio ("Pixel Ratio", Float) = 1
		_Gradient_Scale ("Gradient Scale", Range(0, 20)) = 0
		_Gradient_Strength ("Gradient Strength", Range(0, 0.1)) = 0
		[HideInInspector] _Mouse_Position_Smoothstep ("Mouse Position Smoothstep", Vector) = (0,0.15,0,0)
		[HideInInspector] _Mouse_Strength ("Mouse Strength", Float) = 1
		_Color ("Color", Vector) = (0,0,0,1)
		_SurfaceOpacity ("Surface Opacity", Range(0, 1)) = 0.12
		_PausedColor ("PausedColor", Vector) = (1,1,1,1)
		[HideInInspector] _Mouse_Position_Offset ("Mouse Position Offset", Float) = 0
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
 Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
 ColorMask [_ColorMask]
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct A { float4 pos:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
 struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
 sampler2D _MainTex; float4 _TextureSampleAdd;
 float _SurfaceOpacity;
 
 V vert(A v) { V o; o.pos=UnityObjectToClipPos(v.pos); o.uv=v.uv; o.color=v.color; return o; }
 half4 frag(V i):SV_Target {
 half4 mask=tex2D(_MainTex,i.uv)+_TextureSampleAdd;
 // The original sprite outline remains a separate white Image above this surface.
 return half4(i.color.rgb*half3(0.05,0.08,0.12),mask.a*i.color.a*_SurfaceOpacity);
 }
 ENDHLSL }
} }
