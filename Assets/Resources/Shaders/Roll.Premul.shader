// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/Premul/Roll"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _XSpeed ("X Speed", Float) = 0.0
        _YSpeed ("Y Speed", Float) = 0.0
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

            float _T, _XSpeed, _YSpeed;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv + float2(_XSpeed, _YSpeed) * _Time.y * _T;
                o.color = v.color;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, frac(i.uv)) * i.color;

                return col;
            }
            ENDCG
        }
    }
}
