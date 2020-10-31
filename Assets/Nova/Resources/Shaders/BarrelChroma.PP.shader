// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/Post Processing/Barrel Chroma"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Sigma ("Curvature", Range(0.0, 1.0)) = 0.2
        _Aspect ("Aspect Ratio", Float) = 1.77777778
        _Scale ("Scale", Float) = 1.0
        _Strength ("Chromatic Aberration Strength", Float) = 0.02
        _Offset ("Offset", Float) = 0.0
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

            float2 convertUV(float2 uv, float sigma, float aspect, float scale)
            {
                uv -= 0.5;
                uv /= scale;
                float r = sqrt(uv.x * uv.x * aspect * aspect + uv.y * uv.y);
                float ratio = 1.0 - r * sigma;
                uv *= ratio;
                uv += 0.5;
                return uv;
            }

            sampler2D _MainTex;
            float _T, _Sigma, _Aspect, _Scale, _Strength, _Offset;

            fixed4 frag(v2f i) : SV_Target
            {
                float sigma = _Sigma * _T;
                float r = tex2D(_MainTex, convertUV(i.uv, sigma * (1.0 - _Strength), _Aspect, _Scale)).r;
                float g = tex2D(_MainTex, convertUV(i.uv, sigma, _Aspect, _Scale)).g;
                float b = tex2D(_MainTex, convertUV(i.uv, sigma * (1.0 + _Strength), _Aspect, _Scale)).b;
                float a = tex2D(_MainTex, i.uv).a;
                float4 col = float4(r, g, b, a);
                col.rgb += _Offset * _T;
                col *= i.color;
                return col;
            }
            ENDCG
        }
    }
}
