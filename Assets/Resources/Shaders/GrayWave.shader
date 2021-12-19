// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX/Gray Wave"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Freq ("Frequency", Float) = 0.5
        _Size ("Size", Float) = 50.0
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

            sampler2D _MainTex;
            float _T, _Freq, _Size;

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv) * i.color;

                float n = noise(_Freq * _Time.y);
                float3 delta = col.rgb - n;
                col.rgb /= (1.0 + _Size * delta * delta);

                return col;
            }
            ENDCG
        }
    }
}
