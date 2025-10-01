Shader "Custom/DiagonalRotatingSkybox"
{
    Properties
    {
        _Tex ("Cubemap", CUBE) = "" {}
        _RotationSpeed ("Rotation Speed", Float) = 0.02
        _Axis ("Rotation Axis", Vector) = (1,-1,0,0) // �⺻�� XY �밢��
    }
    SubShader
    {
        Tags { "Queue"="Background" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            samplerCUBE _Tex;
            float _RotationSpeed;
            float4 _Axis; // ȸ���� (x,y,z)

            struct v2f {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ȸ�� ����
                float angle = _Time.y * _RotationSpeed;

                // ȸ���� ����ȭ
                float3 axis = normalize(_Axis.xyz);

                // Rodrigues' rotation formula ��� ȸ�����
                float s = sin(angle);
                float c = cos(angle);
                float oc = 1.0 - c;

                float3x3 rotationMatrix = float3x3(
                    oc*axis.x*axis.x + c,        oc*axis.x*axis.y - axis.z*s,  oc*axis.z*axis.x + axis.y*s,
                    oc*axis.x*axis.y + axis.z*s, oc*axis.y*axis.y + c,        oc*axis.y*axis.z - axis.x*s,
                    oc*axis.z*axis.x - axis.y*s, oc*axis.y*axis.z + axis.x*s, oc*axis.z*axis.z + c
                );

                float3 dir = mul(rotationMatrix, i.dir);
                return texCUBE(_Tex, dir);
            }
            ENDCG
        }
    }
}
