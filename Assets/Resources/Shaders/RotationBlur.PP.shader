// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/Post Processing/Rotation Blur"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Size ("Size", Float) = 1.0
        _Offset ("Offset", Float) = 0.0
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
            #include "Assets/Nova/CGInc/Blur.cginc"

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
            float4 _MainTex_TexelSize;
            float _T, _Size, _Offset;
            float _GScale;

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uvShift = i.uv - 0.5;
                float4 col = tex2DMotionBlur(_MainTex, _MainTex_TexelSize * _GScale, i.uv, float2(uvShift.y, -uvShift.x) * _Size * _T);
                col *= i.color;
                col.rgb += _Offset * length(uvShift) * _T;
                col.rgb = saturate(col.rgb);

                return col;
            }
            ENDCG
        }
    }
}
