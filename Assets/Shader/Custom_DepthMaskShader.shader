Shader "Custom/DepthMaskShader" {
	Properties {
	}
	// Recovery implementation: original serialized pass state, sharedassets0 pathID 525.
	SubShader{
		Tags { "Queue" = "Geometry-1" "RenderPipeline"="UniversalPipeline" }
		LOD 200

		Pass
		{
			ColorMask 0
			ZWrite Off
			ZTest LEqual
			Cull Back
			Stencil { Ref 64 ReadMask 64 WriteMask 64 Comp Always Pass Replace }
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return 0; // Original pass writes stencil only, never color or depth.
			}

			ENDHLSL
		}
	}
}
