Shader "Nova/UI/Histogram"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Scale ("Multiplier", Float) = 1.0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
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

            float4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            // Akima interpolation
            // f(0) = y2, f(1) = y3, f'(0) = (y3 - y1) / 2, f'(1) = (y4 - y2) / 2
            // x in [0, 1]
            float interp(float x, float y1, float y2, float y3, float y4)
            {
                return 0.5 * x * (x * (x * (-y1 + 3.0 * y2 - 3.0 * y3 + y4) + 2.0 * y1 - 5.0 * y2 + 4.0 * y3 - y4) - y1 + y3) + y2;
            }

            // Positivity preserving interpolation
            float posInterp(float x, float y1, float y2, float y3, float y4)
            {
                const float eps = 1e-6;
                return exp(interp(x, log(y1 + eps), log(y2 + eps), log(y3 + eps), log(y4 + eps))) - eps;
            }

            // [0, inf) -> [0, 1)
            float activate(float v)
            {
                return 1.0 - exp(-sqrt(v));
            }

            int _SegmentCount;
            float _Segments[256];
            float _Scale;

            float histogram(float2 uv)
            {
                float x = uv.x * (_SegmentCount + 1);
                int i = min(x, _SegmentCount);  // i in [0, _SegmentCount]
                x -= i;  // x in [0, 1]

                float h;
                if (i == 0)
                {
                    h = posInterp(x, _Segments[0], 0.0, _Segments[0], _Segments[1]);
                }
                else if (i == 1)
                {
                    h = posInterp(x, 0.0, _Segments[0], _Segments[1], _Segments[2]);
                }
                else if (i == _SegmentCount - 1)
                {
                    h = posInterp(x, _Segments[_SegmentCount - 3], _Segments[_SegmentCount - 2], _Segments[_SegmentCount - 1], 0.0);
                }
                else if (i == _SegmentCount)
                {
                    h = posInterp(x, _Segments[_SegmentCount - 2], _Segments[_SegmentCount - 1], 0.0, _Segments[_SegmentCount - 1]);
                }
                else
                {
                    h = posInterp(x, _Segments[i - 2], _Segments[i - 1], _Segments[i], _Segments[i + 1]);
                }

                h = activate(_Scale * h);

                // Output anti-aliasing
                return saturate(1.0 - 1000.0 * (uv.y - h));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = i.color;
                col.a *= histogram(i.uv);
                return col;
            }
            ENDCG
        }
    }
}
