// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX Multiply/Barrel"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Sigma ("Curvature", Float) = 0.2
        _Aspect ("Aspect Ratio", Float) = 1.77777778
        _Scale ("Scale", Float) = 1.0
        _Chroma ("Chromatic Aberration Strength", Float) = 0.0
        _Offset ("Offset", Float) = 0.0
        _BackColor ("Background Color", Color) = (0, 0, 0, 1)
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

            #define PI_HALF 1.5707963

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

                float ratio;
                float sinTheta = r * sigma * 4.0;
                if (sinTheta < 0.1)
                {
                    ratio = 1.0 + sinTheta * sinTheta / 6.0; // Taylor expansion
                }
                else if (sinTheta > 1.0)
                {
                    ratio = PI_HALF * sinTheta;
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
            float _Aspect, _Scale;
            float4 _BackColor;

            float4 getColor(float2 uv, float sigma)
            {
                uv = convertUV(uv, sigma, _Aspect, _Scale);
                if (uv.x >= 0.0 && uv.x <= 1.0 && uv.y >= 0.0 && uv.y <= 1.0)
                {
                    return tex2D(_MainTex, uv);
                }
                else
                {
                    return _BackColor;
                }
            }

            float _T, _Sigma, _Chroma, _Offset;

            fixed4 frag(v2f i) : SV_Target
            {
                float sigma = _Sigma * _T;

                float4 col;
                if (_Chroma > 0.0)
                {
                    float r = getColor(i.uv, sigma * (1.0 - _Chroma)).r;
                    float2 ga = getColor(i.uv, sigma).ga;
                    float b = getColor(i.uv, sigma * (1.0 + _Chroma)).b;
                    col = float4(r, ga.x, b, ga.y);
                }
                else
                {
                    col = getColor(i.uv, sigma);
                }
                col *= i.color;
                col.rgb += _Offset * _T;

                col.rgb = 1.0 - (1.0 - col.rgb) * col.a;
                col.a = 1.0;

                return col;
            }
            ENDCG
        }
    }
}
