Shader "Custom/InstancedRimLitURP"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _Ambient("Ambient Add", Range(0,1)) = 0.25
        _EmissionBoost("Emission Boost", Float) = 1.0
        _RimStrength("Rim Strength", Range(0,3)) = 1.2
        _RimPower("Rim Power", Range(0.5,8)) = 2.0
    }
    SubShader
    {
        Tags{ "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardRimLit"
            Tags{ "LightMode"="UniversalForward" }  // �� URP�� �� �н��� �����ϵ���!

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS // (�׸��� �� ���� ���� ����)
            #pragma multi_compile _ _ADDITIONAL_LIGHTS   // (�߰� ����Ʈ ���� ���ϸ� ����)

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(PerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)   // per-instance color
            UNITY_INSTANCING_BUFFER_END(PerInstance)

            CBUFFER_START(UnityPerMaterial)   // SRP Batcher ģȭ
                float4 _BaseColor;
                float  _Ambient;
                float  _EmissionBoost;
                float  _RimStrength;
                float  _RimPower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nWS   = TransformObjectToWorldNormal(IN.normalOS);

                OUT.positionWS  = posWS;
                OUT.normalWS    = nWS;
                OUT.positionHCS = TransformWorldToHClip(posWS);

                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 N = normalize(IN.normalWS);

                // ���� ����Ʈ
                Light mainLight = GetMainLight();
                float3 L = normalize(-mainLight.direction);  // URP�� direction�� "���� ���� ����"�� �ݴ�
                float  ndotl = saturate(dot(N, L));

                // per-instance ���� ���̽�
                float3 instCol = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color).rgb;
                float3 albedo  = _BaseColor.rgb * instCol;

                // �⺻ ���� = �ں��Ʈ + ���� ����Ʈ(����Ʈ)
                float3 lit = albedo * (_Ambient + ndotl * mainLight.color.rgb);

                // ������Ʈ(���� ����)
                float3 V   = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                float  rim = pow(saturate(1.0 - dot(N, V)), _RimPower) * _RimStrength;

                // ���̼�(��ȯ/���)
                float3 col = lit + albedo * _EmissionBoost + rim * albedo;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
