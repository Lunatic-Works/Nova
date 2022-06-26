// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX Multiply/Water"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Size ("Size", Float) = 10.0
        _Aspect ("Aspect Ratio", Float) = 1.77777778
        _Freq ("Frequency", Float) = 50.0
        _Distort ("Distort", Float) = 5.0
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
            #include "Assets/Nova/CGInc/Rand.cginc"

            #define PI 3.1415927

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
                float2 uvEffect : TEXCOORD1;
                float4 color : COLOR;
            };

            float _Size, _Aspect;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uvEffect = float2(v.uv.x * _Size * _Aspect, v.uv.y * _Size);
                o.color = v.color;
                return o;
            }

            float _T, _Freq;

            float water(float2 uv)
            {
                const float n = 3.1;
                float h = 0.0;
                for (float i = 1.0; i <= n; i += 0.7)
                {
                    float2 p = uv * i * i;
                    p.y += _Freq / i * _Time.y;
                    h += cos(2.0 * PI * noise(p) * _T) / (i * i * i);
                }
                return h;
            }

            float2 waterNormal(float2 uv)
            {
                float2 e = float2(1e-3, 0.0);
                float h0 = water(uv);
                float h1 = water(uv + e);
                float h2 = water(uv + e.yx);
                return float2(h1 - h0, h2 - h0);
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Distort;

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uvDelta = _Distort * waterNormal(i.uvEffect);
                float4 col = tex2D(_MainTex, i.uv + uvDelta) * i.color;

                col.rgb = 1.0 - (1.0 - col.rgb) * col.a;
                col.a = 1.0;

                return col;
            }
            ENDCG
        }
    }
}
