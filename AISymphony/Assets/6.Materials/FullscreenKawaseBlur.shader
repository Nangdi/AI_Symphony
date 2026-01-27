Shader "Hidden/FullscreenKawaseBlur"
{
    Properties
    {
        _BlurRadius ("Blur Radius", Range(0, 3)) = 1
        _Intensity  ("Blur Intensity", Range(0, 1)) = 0.25
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {

            Name "FullscreenKawaseBlur"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float4 _BlitTexture_TexelSize; // x=1/w, y=1/h
            float _BlurRadius;             // 0~3 Á¤µµ ÃßÃµ (»ìÂ¦ ºí·¯)
            float _Intensity;              // 0~1

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS: SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 Sample(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float2 texel = _BlitTexture_TexelSize.xy;
                float r = max(_BlurRadius, 0.0);

                // Kawase 4-tap (°¡º±°í ±ò²û)
                float2 o1 = texel * (r + 0.5);
                float2 o2 = texel * (r + 1.5);

                half4 c =
                    Sample(uv + float2( o1.x,  o1.y)) +
                    Sample(uv + float2(-o1.x,  o1.y)) +
                    Sample(uv + float2( o1.x, -o1.y)) +
                    Sample(uv + float2(-o1.x, -o1.y));

                c *= 0.25;

                // ¿øº»°ú ºí·¯¸¦ ¼¯¾î¼­ "»ìÂ¦"¸¸ Èå¸®°Ô
                half4 src = Sample(uv);
                return lerp(src, c, saturate(_Intensity));
            }
            ENDHLSL
        }
    }
}
