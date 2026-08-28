Shader "GameJam/Audience Reaction"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.72, 0.82, 1, 1)
        _ReactionEnergy("Reaction Energy", Range(0, 1)) = 0
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

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionHCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _ReactionEnergy;
                float _BeatPulse;
                float _RowCompletePulse;
                float _FailurePulse;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float facing = saturate(dot(normalize(input.normalWS), normalize(float3(0.35, 0.8, 0.25)))) * 0.45 + 0.55;
                float pulse = _BeatPulse * 0.3 + _RowCompletePulse * 0.55;
                float3 grooveTint = lerp(float3(0.25, 0.12, 0.45), float3(1.0, 0.25, 0.8), _ReactionEnergy);
                float3 color = _BaseColor.rgb * facing + grooveTint * (_ReactionEnergy * 0.5 + pulse);
                color = lerp(color, float3(0.25, 0.03, 0.35), _FailurePulse * 0.8);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
