// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/Premul/Rain"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Size ("Size", Float) = 2.0
        _Aspect ("Aspect Ratio", Float) = 1.77777778
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
            #include "Assets/Nova/CGInc/Rand.cginc"

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

            float S(float a, float b, float t)
            {
                t = saturate((t - a) / (b - a));
                return t * t * (3.0 - 2.0 * t);
            }

            float saw(float b, float t)
            {
                return S(0.0, b, t) * S(1.0, b, t);
            }

            float staticDrops(float2 uv, float t)
            {
                float3 n = rand3(floor(uv));
                float d = length(frac(uv) - n.xy);
                float fade = saw(0.5, frac(t + n.z));
                float c = S(0.3, 0.0, d) * frac(n.z * 1000.0) * fade;
                return c;
            }

            float2 dropLayer(float2 uv, float t)
            {
                float2 UV = uv;

                float2 grid = float2(12.0, 2.0);
                float colShift = rand(floor(uv.x * grid.x));
                uv.y += t + colShift;

                float2 uvGrid = uv * grid;
                float3 n = rand3(floor(uvGrid));
                float2 st = frac(uvGrid) - float2(0.5, 0.0);

                float x = n.x - 0.5;
                float wiggle = UV.y * 20.0;
                wiggle = sin(wiggle + sin(wiggle));
                x += wiggle * (0.5 - abs(x)) * (n.y - 0.5);
                x *= 0.7;
                float y = saw(0.8, frac(t + n.z));
                float d = length((st - float2(x, y)) * grid.yx);
                float mainDrop = S(0.4, 0.0, d);

                float r = S(1.0, y, st.y);
                float trail = S(0.2, 0.0, abs(st.x - x));
                float trailFront = S(-0.02, 0.02, st.y - y);
                trail *= trailFront * r;

                y = frac(UV.y * 10.0) + (st.y - 0.5);
                float dd = length(st - float2(x, y));
                float droplets = S(0.3, 0.0, dd);
                float c = mainDrop + trailFront * droplets * r;

                return float2(c, trail);
            }

            float2 drops(float2 uv, float t, float l0, float l1, float l2)
            {
                float s = staticDrops(uv * 40.0, t) * l0;
                float2 m1 = dropLayer(uv * 0.5, t) * l1;
                float2 m2 = dropLayer(uv, t) * l2;
                float c = s + m1.x + m2.x;
                c = S(0.0, 1.0, c);
                float trail = m1.y + m2.y;
                return float2(c, trail);
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _T;

            fixed4 frag(v2f i) : SV_Target
            {
                float2 UV = i.uv;
                float2 uv = i.uvEffect;
                float t = _Time.y * 0.2;

                float l0 = S(0.0, 1.0, _T) * 2.0;
                float l1 = S(0.25, 0.75, _T);
                float l2 = S(0.0, 0.5, _T);

                float2 c = drops(uv, t, l0, l1, l2);
                float2 e = float2(1e-3, 0.0);
                float cx = drops(uv + e, t, l0, l1, l2).x;
                float cy = drops(uv + e.yx, t, l0, l1, l2).x;
                float2 n = float2(cx - c.x, cy - c.x);

                float4 col = tex2D(_MainTex, UV + n);
                float4 col2 = tex2DGaussianBlur(_MainTex, _MainTex_TexelSize * 1.0, UV, 3.0 * _T) * (1.0 - 0.1 * _T);
                col = lerp(col2, col, saturate(c.x + c.y)) * i.color;

                return col;
            }
            ENDCG
        }
    }
}
