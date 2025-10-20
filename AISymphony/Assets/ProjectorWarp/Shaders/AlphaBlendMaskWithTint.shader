// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Unlit alpha-blended shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "Unlit/Qoobit/Alpha Blend Mask With Tint" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_TintColor("Tint", Color) = (1,1,1,1)
		_TopFadeRange("Top Fade Range", Range(0,1.0)) = 0.0
		_TopFadeChoke("Top Fade Choke", Range(0,0.999)) = 0.0
		_BottomFadeRange("Bottom Fade Range", Range(0,1.0)) = 0.0
		_BottomFadeChoke("Bottom Fade Choke", Range(0,0.999)) = 0.0
		_LeftFadeRange("Left Fade Range", Range(0,1.0)) = 0.0
		_LeftFadeChoke("Left Fade Choke", Range(0,0.999)) = 0.0
		_RightFadeRange("Right Fade Range", Range(0,1.0)) = 0.0
		_RightFadeChoke("Right Fade Choke", Range(0,0.999)) = 0.0
	}

		SubShader{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 100

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

	struct appdata_t {
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
	};

	struct v2f {
		float4 vertex : SV_POSITION;
		half2 texcoord : TEXCOORD0;
	};

	sampler2D _MainTex;
	sampler2D _AlphaTex;
	float4 _TintColor;
	float4 _MainTex_ST;
	float _TopFadeRange;
	float _TopFadeChoke;
	float _BottomFadeRange;
	float _BottomFadeChoke;
	float _LeftFadeRange;
	float _LeftFadeChoke;
	float _RightFadeRange;
	float _RightFadeChoke;

	v2f vert(appdata_t v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		fixed4 col = tex2D(_MainTex, i.texcoord);
		col*=_TintColor;
		//top controls
		float inverseInTopRange = 1-_TopFadeRange;
		float inverseInTopChoke = (1-_TopFadeChoke);		
		float topY = (2 * (clamp(i.texcoord.y, 0.5, 1.0) - 0.5));
		float topRange = (clamp(topY,inverseInTopRange,1)-inverseInTopRange) / _TopFadeRange;
		float t = (1 - (clamp(topRange,0,inverseInTopChoke)/inverseInTopChoke));

		//bottom controls
		float inverseInBottomRange = 1-_BottomFadeRange;
		float inverseInBottomChoke = 1-_BottomFadeChoke;
		float bottomY = 2 * (0.5 - clamp(i.texcoord.y, 0, 0.50001));
		float bottomRange = (clamp(bottomY,inverseInBottomRange,1)-inverseInBottomRange) / _BottomFadeRange;
		float b = (1 - (clamp(bottomRange,0,inverseInBottomChoke)/inverseInBottomChoke));
		
		//right controls
		float inverseInRightRange = 1-_RightFadeRange;
		float inverseInRightChoke = 1-_RightFadeChoke;
		float rightX = (2 * (clamp(i.texcoord.x, 0.5, 1.0) - 0.5));
		float rightRange = (clamp(rightX,inverseInRightRange,1)-inverseInRightRange) / _RightFadeRange;
		float r = (1 - (clamp(rightRange,0,inverseInRightChoke)/inverseInRightChoke));

		//left controls
		float inverseInLeftRange = 1-_LeftFadeRange;
		float inverseInLeftChoke = 1-_LeftFadeChoke;
		float leftX = 2 * (0.5 - clamp(i.texcoord.x, 0, 0.50001));
		float leftRange = (clamp(leftX,inverseInLeftRange,1)-inverseInLeftRange) / _LeftFadeRange;
		float l = (1 - (clamp(leftRange,0,inverseInLeftChoke)/inverseInLeftChoke));
		
		
		float a = t * b * r * l;
		
		
		return fixed4(col.r, col.g, col.b, a);
	}
		ENDCG
	}
	}

}