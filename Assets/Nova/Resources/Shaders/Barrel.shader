// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX/Barrel"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _Sigma ("Curvature", Range(0.0, 1.0)) = 0.2
        _Aspect ("Aspect Ratio", Float) = 1.77777778
        _Scale ("Scale", Float) = 1.0
        _BackColor ("Background Color", Color) = (0, 0, 0, 1)
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
                float sinTheta = r * sigma * 4.0;
                float ratio;
                if (sigma < 0.1)
                {
                    ratio = 1.0 + sinTheta * sinTheta / 6.0; // Taylor expansion
                }
                else if (sinTheta < 0.001)
                {
                    ratio = 1.0;
                }
                else
                {
                    ratio = asin(sinTheta) / sinTheta;
                }
                uv *= ratio;
                uv += 0.5;
                return uv;
            }

            sampler2D _MainTex;
            float _Sigma, _Aspect, _Scale;
            fixed4 _BackColor;

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = convertUV(i.uv, _Sigma, _Aspect, _Scale);
                if (uv.x >= 0.0 && uv.y >= 0.0 && uv.x <= 1.0 && uv.y <= 1.0)
                {
                    return tex2D(_MainTex, uv) * i.color;
                }
                else
                {
                    return _BackColor;
                }
            }
            ENDCG
        }
    }
}
