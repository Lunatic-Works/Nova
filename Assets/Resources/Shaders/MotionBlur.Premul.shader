// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/Premul/Motion Blur"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Size ("Size", Float) = 1.0
        _Theta ("Theta", Float) = 0.0
        _Offset ("Offset", Float) = 0.0
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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _T, _Size, _Theta, _Offset;

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = tex2DMotionBlur(_MainTex, _MainTex_TexelSize * 1.0, i.uv, float2(cos(_Theta), sin(_Theta)) * _Size * _T);
                col *= i.color;
                col.rgb += _Offset * _T;
                col.rgb = saturate(col.rgb);

                return col;
            }
            ENDCG
        }
    }
}
