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
		_FillAmount ("FillAmount", Float) = 1
		_RepeatAmount ("RepeatAmount", Float) = 1
		_Gap ("Gap", Float) = 0.5
		_BackgroundColor ("BackgroundColor", Vector) = (0,0,0,1)
		_BackgroundColor1 ("BackgroundColor1", Vector) = (1,1,1,1)
		_BackgroundColor2 ("BackgroundColor2", Vector) = (1,0,0,1)
		_BackgroundColor3 ("BackgroundColor3", Vector) = (1,0.8,0,1)
		_BackgroundColor4 ("BackgroundColor4", Vector) = (0,0,0,1)
		_TopColor ("TopColor", Vector) = (1,0,1,1)
		_BottomColor ("BottomColor", Vector) = (1,1,0,1)
		_Color1 ("Color1", Vector) = (0,1,0,1)
		_Color2 ("Color2", Vector) = (0,0.2,0,1)
		_Color3 ("Color3", Vector) = (0,0,0,1)
		_Color4 ("Color4", Vector) = (0,0,0,1)
		_DollarTightness ("DollarTightness", Float) = 0.75
		_DollarTightness2 ("DollarTightness2", Float) = 0.8
		_NoiseSpeed ("NoiseSpeed", Vector) = (0,0,0,0)
		_NoiseSpeed2 ("NoiseSpeed2", Vector) = (0,0,0,0)
		_BackgroundNoiseAmount ("BackgroundNoiseAmount", Float) = 0.05
		_BackgroundNoiseScale ("BackgroundNoiseScale", Float) = 55
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
	// Recovery reconstruction of the original slot-machine screen background:
	// pixelated, animated grid of dollar bills in wavy color bands with colored
	// grid lines, vertically clipped to the display area by _FillAmount.

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

	 float4 _MainColor,_SecondColor,_TopColor,_BottomColor,_Color1,_Color2,_Color3,_Color4;
	 float4 _LineColor1,_LineColor2,_LineColor3,_LineColor4;
	 float4 _BackgroundColor,_BackgroundColor1,_BackgroundColor2,_BackgroundColor3,_BackgroundColor4;
	 float4 _TilingSpeed,_TilingSpeed2,_WaveSpeed,_WaveSpeed2,_NoiseSpeed,_NoiseSpeed2;
	 float _Tightness,_Tiling,_WaveAmount,_WaveOffset,_NoiseScale,_NoiseAmount;
	 float _Tightness2,_Tiling2,_WaveAmount2,_WaveOffset2,_NoiseScale2,_NoiseAmount2;
	 float _Pixels,_BackgroundSpeed,_LineThinness,_FillAmount,_RepeatAmount,_Gap;
	 float _DollarTightness,_DollarTightness2,_BackgroundNoiseAmount,_BackgroundNoiseScale,_Emission;

	 // Value noise exactly matching Shader Graph "Gradient Noise" hash
	 // (multipliers 0x41c64e6d / 0x27d4eb2d as seen in the original DXBC).
	 float SGHash(float2 p) {
	  uint2 q = uint2(int2(floor(p.x), floor(p.y)));
	  uint n = q.x * 0x41c64e6du;
	  n ^= q.y * 0x41c64e6du;
	  n = (n >> 5) ^ n;
	  n *= 0x27d4eb2du;
	  n = (n >> 8);
	  return float(n) * (1.0/255.0);
	 }
	 float SGNoise(float2 p) {
	  float2 i = floor(p), f = frac(p);
	  f = f*f*(3.0-2.0*f);
	  float a = SGHash(i);
	  float b = SGHash(i + float2(1,0));
	  float c = SGHash(i + float2(0,1));
	  float d = SGHash(i + float2(1,1));
	  return lerp(lerp(a,b,f.x), lerp(c,d,f.x), f.y);
	 }
	 // 3-octave FBM with 0.125/0.25/0.5 weights, as decoded from the original DXBC
	 float SGFbm(float2 p) {
	  return SGNoise(p) * 0.125 + SGNoise(p*0.5) * 0.25 + SGNoise(p*0.25) * 0.5;
	 }

	 V vert(A v) { V o; o.pos=UnityObjectToClipPos(v.pos); o.uv=v.uv; o.color=v.color; o.world=v.pos; return o; }

	 half4 frag(V i):SV_Target {
	  float t=_Time.y;
	  float2 uv=i.uv;

	  // --- vertical fill clip (screen area of the machine display) ---
	  float fillMask = step(abs(uv.y-0.5), _FillAmount*0.5);

	  // --- pixelation (stripe step only) ---
	  float2 puv = floor(uv*_Pixels)/_Pixels;

	  // --- diagonal color stripes: pixelated x+y phase, 4-way step ramp ---
	  //    phase < 0.25 -> _LineColor2, < 0.5 -> _LineColor1,
	  //             < 0.75 -> _BottomColor, else _TopColor
	  float phase = frac((puv.x + puv.y) * _RepeatAmount + t*_BackgroundSpeed);
	  float3 stripe;
	  if (phase < 0.25) stripe = _LineColor2.rgb;
	  else if (phase < 0.50) stripe = _LineColor1.rgb;
	  else if (phase < 0.75) stripe = _BottomColor.rgb;
	  else stripe = _TopColor.rgb;
	  float3 col = stripe;

	  // --- dollar bill layer 1: noise + wave distorted UV, tinted by MainColor ---
	  // original: wave = sincos((uv.x*_WaveOffset+t)*_WaveSpeed.xy)*_WaveAmount
	  //                  + uv*_RepeatAmount + t*_TilingSpeed.xy;  duv = frac(wave)/_Tightness
	  float n1 = SGFbm(uv * _NoiseScale + _NoiseSpeed.xy*t);
	  float ang1 = (uv.x * _WaveOffset + t) * 6.2831853;
	  float2 wave1 = float2(sin(ang1), cos(ang1)) * _WaveSpeed.x * _WaveAmount
	   + uv * _RepeatAmount + t * _TilingSpeed.xy;
	  float2 duv1 = frac(wave1 + _NoiseAmount*(n1.xx - wave1)) / max(_Tightness, 0.001);
	  half4 bill1 = tex2D(_MainTex, duv1) + _TextureSampleAdd;
	  col = lerp(col, bill1.rgb * _MainColor.rgb, bill1.a);

	  // --- dollar bill layer 2: smaller/faster, tinted by Color1 ---
	  float n2 = SGFbm(uv * _NoiseScale2 + _NoiseSpeed2.xy*t);
	  float ang2 = (uv.x * _WaveOffset2 + t) * 6.2831853;
	  float2 wave2 = float2(sin(ang2), cos(ang2)) * _WaveSpeed2.x * _WaveAmount2
	   + uv * _RepeatAmount * 2.0 + t * _TilingSpeed2.xy;
	  float2 duv2 = frac(wave2 + _NoiseAmount2*(n2.xx - wave2)) / max(_Tightness2, 0.001);
	  half4 bill2 = tex2D(_MainTex, duv2) + _TextureSampleAdd;
	  col = lerp(col, bill2.rgb * _Color1.rgb, bill2.a);

	  // --- emission + UI clip rect + fill + vertex color ---
	  col *= _Emission;
	  float m = UnityGet2DClipping(i.world.xy, _ClipRect);
	  fillMask *= m;
	  col *= i.color.rgb;
	  float alpha = fillMask*i.color.a;
	  return half4(col, alpha);
	 }
	 ENDHLSL }
} }