// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX Multiply/Mix Add"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Mask ("Mask", 2D) = "white" {}
        _ColorMul ("Color Multiplier", Color) = (1, 1, 1, 1)
        _ColorAdd ("Color Offset", Vector) = (0, 0, 0, 0)
        _InvertMask ("Invert Mask", Float) = 0.0
        _AlphaFactor ("Alpha Factor", Float) = 0.0
    }
    SubShader
    {
        Cull Off ZWrite Off Blend DstColor Zero
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            float4 _ColorMul, _ColorAdd;
            float _T, _InvertMask, _AlphaFactor;

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv) * i.color;
                float mask = tex2D(_Mask, i.uvMask).r;
                mask = _InvertMask + mask - 2 * _InvertMask * mask;
                float4 maskColor = mask * _ColorMul + _ColorAdd;
                maskColor.a *= _AlphaFactor;
                col = saturate(col + _T * maskColor);

                col.rgb = 1.0 - (1.0 - col.rgb) * col.a;
                col.a = 1.0;

                return col;
            }
            ENDCG
        }
    }
}
