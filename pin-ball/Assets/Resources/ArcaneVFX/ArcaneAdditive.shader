Shader "Pinball/ArcaneAdditive"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("HDR Intensity", Range(0, 4)) = 2.2
        _GlowSpread ("Glow Spread", Range(0, 4)) = 2.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode"="Universal2D" }
            Blend One One
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half _Intensity;
                half _GlowSpread;
            CBUFFER_END

            float4 _MainTex_TexelSize;

            half SampleMask(float2 uv)
            {
                half4 sample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return dot(sample.rgb, half3(0.2126, 0.7152, 0.0722)) * sample.a;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 offset = _MainTex_TexelSize.xy * _GlowSpread;
                half core = SampleMask(input.uv);
                half halo = 0;
                halo = max(halo, SampleMask(input.uv + float2(offset.x, 0)));
                halo = max(halo, SampleMask(input.uv - float2(offset.x, 0)));
                halo = max(halo, SampleMask(input.uv + float2(0, offset.y)));
                halo = max(halo, SampleMask(input.uv - float2(0, offset.y)));
                halo = max(halo, SampleMask(input.uv + offset));
                halo = max(halo, SampleMask(input.uv - offset));
                halo = max(halo, SampleMask(input.uv + float2(offset.x, -offset.y)));
                halo = max(halo, SampleMask(input.uv + float2(-offset.x, offset.y)));
                half strength = max(core, halo * 0.48h) * input.color.a;
                return half4(input.color.rgb * _Intensity * strength, strength);
            }
            ENDHLSL
        }
    }
}
