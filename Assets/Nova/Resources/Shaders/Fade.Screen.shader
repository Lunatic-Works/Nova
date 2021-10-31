// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX Screen/Fade"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        [NoScaleOffset] _SubTex ("Second Texture", 2D) = "black" {}
        _SubColor ("Second Texture Color", Color) = (1, 1, 1, 1)
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Mask ("Mask", 2D) = "white" {}
        _Vague ("Vagueness", Range(0.0, 0.5)) = 0.25
        _Offset ("Main Texture Luminosity Offset", Float) = 0.0
        _InvertMask ("Invert Mask", Float) = 0.0
    }
    SubShader
    {
        Cull Off ZWrite Off Blend OneMinusDstColor One
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define smoothStep(x) ((x) * (x) * (3.0 - 2.0 * (x)))

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uvMask : TEXCOORD1;
                float4 color : COLOR;
            };

            sampler2D _Mask;
            float4 _Mask_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uvMask = TRANSFORM_TEX(v.uv, _Mask);
                o.color = v.color;
                return o;
            }

            sampler2D _MainTex, _SubTex;
            float4 _MainTex_TexelSize, _SubColor;
            float _T, _Vague, _Offset, _InvertMask;

            fixed4 frag(v2f i) : SV_Target
            {
                float t0 = tex2D(_Mask, i.uvMask).r;
                t0 = _InvertMask + t0 - 2 * _InvertMask * t0;
                t0 = t0 * (1.0 - 2.0 * _Vague) + _Vague;
                float slope = 0.5 / (_Vague + 0.001);
                float mask = smoothStep(saturate(0.5 + slope * (_T - t0)));

                float4 col = tex2D(_MainTex, i.uv) * i.color;
                col.rgb += _Offset * _T;
                float4 col2 = tex2D(_SubTex, i.uv) * _SubColor;
                col = lerp(col, col2, mask);

                col.rgb *= col.a;
                col.a = 1.0;

                return col;
            }
            ENDCG
        }
    }
}
