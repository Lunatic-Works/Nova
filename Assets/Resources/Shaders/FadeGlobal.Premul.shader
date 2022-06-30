// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/Premul/Fade Global"
{
    Properties
    {
        [HideInInspector] _MainTex ("Dummy Texture Providing Size", 2D) = "black" {}
        _Offsets ("Offsets (MainX, MainY, SecX, SecY)", Vector) = (0, 0, 0, 0)
        _Color ("Primary Texture Color", Color) = (1, 1, 1, 1)
        _SubColor ("Secondary Texture Color", Color) = (1, 1, 1, 1)
        _T ("Time", Range(0.0, 1.0)) = 0.0
    }
    SubShader
    {
        Cull Off ZWrite Off Blend One OneMinusSrcAlpha
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define clamped2D(tex, uv) tex2D((tex), clamp((uv), 0, 1))

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _PrimaryTex, _SubTex;
            float4 _Offsets, _Color, _SubColor;
            float _T;

            fixed4 frag(v2f i) : SV_Target
            {
                return lerp(
                    clamped2D(_PrimaryTex, i.uv - _Offsets.xy) * _Color,
                    clamped2D(_SubTex, i.uv - _Offsets.zw) * _SubColor,
                    _T
                );
            }
            ENDCG
        }
    }
}
