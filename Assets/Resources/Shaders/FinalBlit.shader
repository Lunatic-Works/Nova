// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX/Final Blit"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Margin Color", Color) = (0, 0, 0, 1)
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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _Color;
            float _GW, _GH;

            fixed4 frag(v2f i) : SV_Target
            {
                float2 pivot = float2(0.5, 0.5);
                float2 uv = (i.uv - pivot) / float2(_GW, _GH) * _ScreenParams.xy + pivot;
                float eps = 1e-6;
                float4 col;

                if (uv.x > -eps && uv.x < 1 + eps && uv.y > -eps && uv.y < 1 + eps)
                {
                    col = tex2D(_MainTex, clamp(uv, 0, 1));
                }
                else
                {
                    col = _Color;
                }

                return col;
            }
            ENDCG
        }
    }
}
