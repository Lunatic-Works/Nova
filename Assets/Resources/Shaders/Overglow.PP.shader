// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/Post Processing/Overglow"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Zoom ("Zoom", Float) = 0.5
        _Mul ("Multiplier", Float) = 0.5
        _CenterX ("Center X", Float) = 0.5
        _CenterY ("Center Y", Float) = 0.5
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            sampler2D _MainTex;
            float _T, _Zoom, _Mul, _CenterX, _CenterY;

            fixed4 frag(v2f i) : SV_Target
            {
                float2 center = float2(_CenterX, _CenterY);
                float2 uv2 = (i.uv - center) * (1.0 - _Zoom * _T) + center;
                float4 col = tex2D(_MainTex, i.uv) * i.color;
                float4 col2 = tex2D(_MainTex, uv2) * i.color;
                col.rgb += col2.rgb * _Mul * (1.0 - _T);
                col.rgb = saturate(col.rgb);

                return col;
            }
            ENDCG
        }
    }
}
