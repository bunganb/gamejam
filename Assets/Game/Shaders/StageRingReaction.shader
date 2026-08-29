Shader "GameJam/Stage Ring Reaction"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.05, 0.01, 0.08, 1)
        [HDR] _EmissionColor("Emission Color", Color) = (1, 0.05, 1, 1)
        _ReactionEnergy("Reaction Energy", Range(0, 1)) = 0
        _StageProgress("Stage Progress", Range(0, 1)) = 0
        _SegmentFill("Segment Fill", Range(0, 1)) = 0
        _BeatPulse("Beat Pulse", Range(0, 1)) = 0
        _RowCompletePulse("Row Complete Pulse", Range(0, 1)) = 0
        _FailurePulse("Failure Pulse", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float _ReactionEnergy;
                float _StageProgress;
                float _SegmentFill;
                float _BeatPulse;
                float _RowCompletePulse;
                float _FailurePulse;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float reveal = 1.0 - smoothstep(_SegmentFill, _SegmentFill + 0.08, input.uv.x);
                reveal *= step(0.001, _SegmentFill);
                float pulse = _BeatPulse * 0.65 + _RowCompletePulse * 1.2;
                float failureFlash = _FailurePulse * (0.5 + 0.5 * sin(_Time.y * 35.0));
                float3 dim = _BaseColor.rgb * 0.18;
                float3 active = _EmissionColor.rgb * (1.0 + _ReactionEnergy + pulse);
                float3 color = lerp(dim, active, reveal);
                color = lerp(color, float3(0.35, 0.02, 0.55), failureFlash);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
