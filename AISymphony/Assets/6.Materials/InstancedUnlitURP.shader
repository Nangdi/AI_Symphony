Shader "Custom/InstancedUnlitURP"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _EmissionBoost("Emission Boost", Float) = 0   // Bloom 세게 하고 싶으면 올리기(0~5)
    }
    SubShader
    {
        Tags{ "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardUnlit"
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(PerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)   // 인스턴스별 색상 (HDR 가능)
            UNITY_INSTANCING_BUFFER_END(PerInstance)

            float4 _BaseColor;
            float  _EmissionBoost;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(posWS);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 c = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color).rgb;
                // 베이스 × 인스턴스 컬러 + 약간의 발광 부스트
                float3 col = _BaseColor.rgb * c + c * _EmissionBoost;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
