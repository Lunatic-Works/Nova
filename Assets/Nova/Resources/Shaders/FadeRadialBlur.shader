// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX/Fade Radial Blur"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        [NoScaleOffset] _SubTex ("Second Texture", 2D) = "black" {}
        _SubColor ("Second Texture Color", Color) = (1, 1, 1, 1)
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Size ("Size", Float) = 1.0
        _Zoom ("Zoom", Float) = 0.5
        _Dir ("Direction", Float) = 0.0
    }
    SubShader
    {
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/Nova/CGInc/Blur.cginc"

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
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            sampler2D _MainTex, _SubTex;
            float4 _MainTex_TexelSize, _SubColor;
            float _T, _Size, _Zoom, _Dir;

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uvShift = i.uv - 0.5;
                float2 uvZoom = uvShift * (1.0 - _Zoom * _T * _Dir) + 0.5;
                float4 col = tex2DMotionBlur(_MainTex, _MainTex_TexelSize * 1.0, uvZoom, uvShift * _Size * _T) * i.color;
                float2 uvZoom2 = uvShift * (1.0 - _Zoom * (1.0 - _T) * (1.0 - _Dir)) + 0.5;
                float4 col2 = tex2DMotionBlur(_SubTex, _MainTex_TexelSize * 1.0, uvZoom2, uvShift * _Size * (1.0 - _T)) * _SubColor;
                col = lerp(col, col2, _T);

                return col;
            }
            ENDCG
        }
    }
}
