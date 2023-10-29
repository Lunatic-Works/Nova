// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX Screen/Broken TV"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Roll ("Y Rolling", Range(0.0, 1.0)) = 0.07
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

            float power(float x)
            {
                x += 1.0;
                x = x * x;
                x = x * x;
                x -= 1.0;
                return x;
            }

            sampler2D _MainTex;
            float _T, _Roll;

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float powerT = power(_T);

                float2 ty = float2(_Time.y, uv.y * 20.0);
                float xOffset = srand(ty) * 0.01;
                xOffset += snoise(ty) * 0.05;
                float x = uv.x + xOffset * _T;

                float yOffset = (_Time.y + xOffset) * powerT;
                yOffset *= step(noise(_Time.y), powerT * _Roll);
                float y = frac(uv.y + yOffset);

                // Chromatic aberration
                float rbOffset = xOffset * 0.1 * powerT;
                float r = tex2D(_MainTex, float2(x - rbOffset, y)).r;
                float g = tex2D(_MainTex, float2(x, y)).g;
                float b = tex2D(_MainTex, float2(x + rbOffset, y)).b;
                float a = tex2D(_MainTex, float2(x, y)).a;
                float4 col = float4(r, g, b, a);
                col *= i.color;

                // Scan lines
                col.rgb -= sin((uv.y + _Time.x) * 500.0) * 0.1 * _T * col.a;

                // Snow noise
                col.rgb += srand(uv * 100.0 + _Time.y) * 0.04 * powerT * col.a;

                col.rgb *= col.a;
                col.a = 0.0;

                return col;
            }
            ENDCG
        }
    }
}
