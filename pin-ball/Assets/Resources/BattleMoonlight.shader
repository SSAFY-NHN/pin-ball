Shader "Battle/MoonlightMask"
{
    Properties
    {
        _MainTex ("Mask", 2D) = "white" {}
        _Intensity ("Intensity", Range(0, 1)) = 0.65
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Blend One One
            Cull Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            half _Intensity;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 sample = tex2D(_MainTex, input.uv);
                half luminance = dot(
                    sample.rgb,
                    half3(0.2126, 0.7152, 0.0722));
                half strength = luminance * sample.a * input.color.a;
                half3 light = input.color.rgb * (_Intensity * strength);
                return half4(light, 0.0);
            }
            ENDCG
        }
    }
}
