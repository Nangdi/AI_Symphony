Shader "Custom/ExhibitionNeonPillarURP"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _Ambient("Ambient Add", Range(0,1)) = 0.16

        _EmissionBoost("Emission Boost", Range(0,8)) = 0.6
        _HeightEmission("Height Emission", Range(0,8)) = 0.45

        _RimStrength("Rim Strength", Range(0,3)) = 1.2
        _RimPower("Rim Power", Range(0.5,8)) = 2.0

        _RingPos("Ring Pos (0..1)", Range(0,1)) = 0.72
        _RingWidth("Ring Width", Range(0.01,0.5)) = 0.12
        _RingBoost("Ring Emission", Range(0,8)) = 1.2

        // Pulse FX
        _PulseEmission("Pulse Emission", Range(0,8)) = 0.8
        _BandUpSpeed("Band Up Speed", Range(0.1,10)) = 2.4   // ▲
        _BandDownSpeed("Band Down Speed", Range(0.1,10)) = 1.6 // ▼
        _BandWidth("Pulse Band Width", Range(0.02,0.5)) = 0.14
        _BandBoost("Pulse Band Emission", Range(0,10)) = 4.0

        _ToneMap("ToneMap", Range(0,2)) = 0.8
    }
    SubShader
    {
        Tags{ "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardNeon"
            Tags{ "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings   { float4 positionHCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float3 positionOS:TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID };

            UNITY_INSTANCING_BUFFER_START(PerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float,  _Pulse)
                UNITY_DEFINE_INSTANCED_PROP(float,  _PulseTime)
            UNITY_INSTANCING_BUFFER_END(PerInstance)

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _Ambient, _EmissionBoost, _HeightEmission;
                float  _RimStrength, _RimPower;
                float  _RingPos, _RingWidth, _RingBoost;
                float  _PulseEmission, _BandUpSpeed, _BandDownSpeed, _BandWidth, _BandBoost;
                float  _ToneMap;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT; UNITY_SETUP_INSTANCE_ID(IN);
                OUT.positionOS = IN.positionOS.xyz;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionHCS= TransformWorldToHClip(OUT.positionWS);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                return OUT;
            }

            half4 frag(Varyings IN):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 N = normalize(IN.normalWS);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);

                Light mainLight = GetMainLight();
                float3 L = normalize(-mainLight.direction);
                float ndotl = saturate(dot(N, L));

                float3 instCol = UNITY_ACCESS_INSTANCED_PROP(PerInstance,_Color).rgb;
                float  pulse   = UNITY_ACCESS_INSTANCED_PROP(PerInstance,_Pulse);
                float  tStart  = UNITY_ACCESS_INSTANCED_PROP(PerInstance,_PulseTime);

                float3 albedo  = _BaseColor.rgb * instCol;

                // 기본 조명 + 림
                float3 lit = albedo * (_Ambient + ndotl * mainLight.color.rgb);
                float rim = pow(saturate(1.0 - dot(N,V)), _RimPower) * _RimStrength;

                // 로컬 Y(0..1) & 링
                float y01 = saturate(IN.positionOS.y * 0.5 + 0.5);
                float ring = smoothstep(_RingPos - _RingWidth, _RingPos, y01) *
                             (1.0 - smoothstep(_RingPos, _RingPos + _RingWidth, y01));

                // ── 밴드 위치: 위로 갔다가 내려오는 비대칭 속도 ──
                float elapsed = max(0.0, _Time.y - tStart);
                float upT = max(1e-4, 1.0 / _BandUpSpeed);
                float dnT = max(1e-4, 1.0 / _BandDownSpeed);
                float cycle = upT + dnT;
                float tc = fmod(elapsed, cycle);
                float bandPos = (tc < upT) ? (tc / upT) : (1.0 - (tc - upT) / dnT);

                float d = abs(y01 - bandPos);
                float band = exp(- (d*d) / max(1e-4, _BandWidth*_BandWidth));

                // 최종 에미션
                float emission = _EmissionBoost
                               + _HeightEmission * y01
                               + _RingBoost * ring
                               + rim
                               + _PulseEmission * saturate(pulse)
                               + _BandBoost * band * saturate(pulse);

                float3 colHDR = lit + albedo * emission;
                colHDR = colHDR / (1.0 + _ToneMap * colHDR); // 소프트 톤매핑

                return half4(colHDR, 1);
            }
            ENDHLSL
        }
    }
}
