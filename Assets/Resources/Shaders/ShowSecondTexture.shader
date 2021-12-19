// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX/Show Second Texture"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        [NoScaleOffset] _SubTex ("Second Texture", 2D) = "black" {}
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

            sampler2D _SubTex;

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_SubTex, i.uv);
            }
            ENDCG
        }
    }
}
