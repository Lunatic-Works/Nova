// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/Premul/Overlay"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off Blend One OneMinusSrcAlpha
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define clamped2D(tex, uv) tex2D((tex), clamp((uv), 0, 1))

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
                // render directly to clip space
                o.vertex = v.vertex;
                o.uv = v.uv;
                if (_ProjectionParams.x < 0)
                    o.uv.y = 1 - o.uv.y;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f i) : SV_Target
            {
                return clamped2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
