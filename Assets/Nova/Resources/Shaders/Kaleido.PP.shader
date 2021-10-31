// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/Post Processing/Kaleido"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Repeat ("Repeat", Float) = 8.0
        _Freq ("Frequency", Float) = 1.0
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
                float2 cosSinTime : TEXCOORD1;
            };

            float _Repeat;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                float t = _Time.y / _Repeat;
                o.cosSinTime = float2(cos(t), sin(t));
                return o;
            }

            sampler2D _MainTex;
            float _T, _Freq;

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);

                float2 uv = i.uv - 0.5;
                for (int repeat = 0; repeat < _Repeat; ++repeat)
                {
                    uv = abs(uv) - 0.25;
                    uv *= sign(uv + uv.yx);
                    uv = i.cosSinTime.x * uv + i.cosSinTime.y * uv.yx * float2(1.0, -1.0);
                }
                uv += 0.5;
                float4 col2 = tex2D(_MainTex, uv);

                col = lerp(col, col2, _T) * i.color;

                return col;
            }
            ENDCG
        }
    }
}
