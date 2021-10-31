// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX Screen/Glitch"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
    }
    SubShader
    {
        Cull Off ZWrite Off Blend OneMinusDstColor One
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
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

            float blockNoise(float2 uv, float freqX, float freqY, float freqT)
            {
                float a = floor(noise(_Time.y * freqT) * 100.0);
                a = floor(noise(uv.y * freqY + a) * 100.0);
                a = floor(noise(uv.x * freqX + a) * 100.0);
                a = floor(noise(uv.y * freqY + a) * 100.0);
                a = rand(a);
                return a;
            }

            float power(float x)
            {
                x += 1.0;
                x = x * x;
                x = x * x;
                x -= 1.0;
                return x;
            }

            float blockOffsetPass(float2 uv, float freqX, float freqY, float freqT, float threshold, float value)
            {
                float blockOffsetOn = blockNoise(uv, freqX, freqY, freqT);
                if (blockOffsetOn < threshold)
                {
                    return value;
                }
                return 0.0;
            }

            sampler2D _MainTex;
            float _T;

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float powerT = power(_T);

                float uvOffsetOn = blockNoise(uv, 0.2, 0.2, 0.05);
                float2 uvOffset = float2(0.0, 0.0);
                if (uvOffsetOn < _T)
                {
                    uvOffset.x = srand(uvOffsetOn) * 0.1;
                    uvOffset.y = srand(uvOffset.x) * 0.1;
                }
                float2 uv2 = uv + uvOffset;

                float rbOffsetOn = blockNoise(uv, 0.1, 0.1, 0.5);
                float2 rbOffset = float2(0.0, 0.0);
                if (rbOffsetOn < 0.2 * _T)
                {
                    rbOffset.x = srand(rbOffsetOn) * 0.1;
                    rbOffset.y = srand(rbOffset.x) * 0.1;
                }
                float r = tex2D(_MainTex, uv2 - rbOffset).r;
                float g = tex2D(_MainTex, uv2).g;
                float b = tex2D(_MainTex, uv2 + rbOffset).b;
                float a = tex2D(_MainTex, uv2).a;
                float4 col = float4(r, g, b, a);
                col *= i.color;

                float blockOffset = 0.0;
                blockOffset += blockOffsetPass(uv, 0.5, 5.0, 0.1, 0.06 * powerT, -0.5);
                blockOffset += blockOffsetPass(uv, 2.0, 4.0, 0.1, 0.002 * powerT, 1.0);
                col.rgb += blockOffset;

                col.rgb *= col.a;
                col.a = 1.0;

                return col;
            }
            ENDCG
        }
    }
}
