Shader "Recovery/VfxParticles"
{
    // Recovery implementation: procedural particle discs; not an original VFX graph.
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=input.uv;
                output.color=input.color;
                return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                float radius=length(input.uv*2-1);
                half4 color=input.color;
                color.a*=1-smoothstep(.45,1,radius);
                clip(color.a-.003);
                return color;
            }
            ENDHLSL
        }
    }
}
