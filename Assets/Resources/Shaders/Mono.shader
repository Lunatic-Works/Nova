// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX/Mono"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _ColorMul ("Color Multiplier", Color) = (1, 1, 1, 1)
        _ColorAdd ("Color Offset", Vector) = (0, 0, 0, 0)
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

            sampler2D _MainTex;
            float _T;
            float4 _ColorMul, _ColorAdd;

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                float gray = Luminance(col.rgb);
                float3 mono = gray * _ColorMul.rgb + _ColorAdd.rgb;
                col.rgb = lerp(col.rgb, mono, _T);
                col *= i.color;

                return col;
            }
            ENDCG
        }
    }
}
