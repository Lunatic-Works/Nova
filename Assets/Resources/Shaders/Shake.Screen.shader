// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX Screen/Shake"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _XAmp ("X Amplitude", Float) = 1.0
        _YAmp ("Y Amplitude", Float) = 1.0
        _Freq ("Frequency", Float) = 10.0
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

            float _T, _XAmp, _YAmp, _Freq;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv + float2(_XAmp, _YAmp) * rand2(floor(_Freq * _Time.y)) * _T;
                o.color = v.color;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv) * i.color;

                col.rgb *= col.a;
                col.a = 0.0;

                return col;
            }
            ENDCG
        }
    }
}
