Shader "Custom/Fog"
{
    Properties
    {
        [MainColor] _BaseColor("Fog Color", Color) = (0.75, 0.82, 0.86, 0.55)
        [MainTexture] _BaseMap("Fog Noise", 2D) = "gray" {}
        _NoiseScale("Noise Scale", Range(0.1, 10)) = 1.5
        _NoiseSpeed("Noise Speed", Vector) = (0.035, 0.012, 0, 0)
        _NoiseContrast("Noise Contrast", Range(0.1, 4)) = 1.4
        _Opacity("Opacity", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPosition : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float _NoiseScale;
                float4 _NoiseSpeed;
                float _NoiseContrast;
                float _Opacity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.screenPosition = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y;
                float2 screenUV = IN.screenPosition.xy / IN.screenPosition.w;
                float2 noiseUV = IN.uv * _NoiseScale;
                float2 drift = _NoiseSpeed.xy * time;

                float noiseA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, noiseUV + drift).r;
                float noiseB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap,
                    noiseUV * 1.7 - drift * 0.65 + screenUV * 0.08).g;
                float fog = saturate((noiseA * 0.65 + noiseB * 0.35 - 0.35) * _NoiseContrast);
                fog = smoothstep(0.08, 0.92, fog);

                half4 color = _BaseColor;
                color.a *= fog * _Opacity;
                return color;
            }
            ENDHLSL
        }
    }
}
