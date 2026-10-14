Shader "Shader Graphs/OutlineBackgroundShader" {
	Properties {
		[HideInInspector] [NoScaleOffset] _MainTex ("_MainTex", 2D) = "white" {}
		_LineThinness ("LineThinness", Float) = 1
		_BackgroundSpeed ("BackgroundSpeed", Float) = 0
		_SmoothStep ("SmoothStep", Vector) = (0,1,0,0)
		_Pixels ("Pixels", Float) = 0
		_AlphaMinMax ("AlphaMinMax", Vector) = (0,0,0,0)
		_Emission ("Emission", Float) = 0
		_Step ("Step", Float) = 0.35
		_Alpha ("Alpha", Float) = 0.44
		_Space ("Space", Float) = 1
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
	// Recovery reconstruction of the original rarity-outline frame:
	// pixelated animated border band around the quad (tinted by vertex color,
	// e.g. rarity color) over a faint interior. Rendered inside UI Masks,
	// so the Stencil/ColorMask blocks are mandatory.

	SubShader {
	 Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
	 Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
	 Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
	 ColorMask [_ColorMask]
	 Pass { HLSLPROGRAM
	 #pragma vertex vert
	 #pragma fragment frag
	 #include "UnityCG.cginc"
	 #include "UnityUI.cginc"
	 struct A { float4 pos:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
	 struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; float4 world:TEXCOORD1; };
	 sampler2D _MainTex; float4 _TextureSampleAdd; float4 _ClipRect;

	 float _LineThinness,_BackgroundSpeed,_Pixels,_Step,_Alpha,_Space,_Emission;
	 float4 _SmoothStep,_AlphaMinMax,_Color;

	 V vert(A v) { V o; o.pos=UnityObjectToClipPos(v.pos); o.uv=v.uv; o.color=v.color; o.world=v.pos; return o; }

	 half4 frag(V i):SV_Target {
	  float t=_Time.y;
	  float2 uv=i.uv;

	  // pixelation
	  float px = max(_Pixels, 1.0);
	  float2 puv = floor(uv*px)/px;

	  // diagonal pixelated phase (x + y) with background speed
	  float ph = frac((puv.x + puv.y) * _Step + t*_BackgroundSpeed);

	  // triangular pulse -> band within SmoothStep window
	  float tri = max(frac(ph), frac(-ph));
	  float band = saturate((tri - _SmoothStep.x) / max(_SmoothStep.y - _SmoothStep.x, 0.0001));
	  band = band*band*(3.0 - 2.0*band); // smoothstep

	  // alpha ramps between AlphaMinMax.x..y
	  float a = lerp(_AlphaMinMax.x, _AlphaMinMax.y, band) * _Alpha;

	  // additive glow: white * (1 + shimmer*emission), tinted by vertex (rarity) color
	  float3 col = (1.0 + a*_Emission) * i.color.rgb;

	  float m = UnityGet2DClipping(i.world.xy, _ClipRect);
	  return half4(col, a*m*i.color.a);
	 }
	 ENDHLSL }
} }