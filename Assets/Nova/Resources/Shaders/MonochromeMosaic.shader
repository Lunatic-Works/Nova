// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX/Monochrome Mosaic"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Size ("Size", Float) = 4.0
        _Strength ("Mosaic Strength", Float) = 8.0
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
            float4 _MainTex_TexelSize;
            float _T, _Size, _Strength;


            fixed4 frag(v2f i) : SV_Target
            {
                float rgb = tex2D(_MainTex, i.uv).g;
                float a = tex2D(_MainTex, i.uv).b;

                float4 screenPos = ComputeScreenPos(floor(i.vertex / _Size) * _Size);
                float2 delta = srand2(float3(screenPos.xy, _Time.y));
                float2 uv2 = i.uv + delta * _MainTex_TexelSize.xy * 1.0 * _Strength;
                float rgb2 = tex2D(_MainTex, uv2).g;
                float a2 = tex2D(_MainTex, uv2).b;

                float mask = tex2D(_MainTex, i.uv).r * _T;
                rgb = lerp(rgb, rgb2, mask);
                a = lerp(a, a2, mask);

                fixed4 col = float4(rgb, rgb, rgb, a) * i.color;
                return col;
            }
            ENDCG
        }
    }
}
