// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX Multiply/Masked Mosaic"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Mask ("Mask", 2D) = "white" {}
        _Size ("Size", Float) = 4.0
        _Strength ("Mosaic Strength", Float) = 8.0
    }
    SubShader
    {
        Cull Off ZWrite Off Blend DstColor Zero
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

            sampler2D _MainTex, _Mask;
            float4 _MainTex_TexelSize;
            float _T, _Size, _Strength;

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);

                float4 screenPos = ComputeScreenPos(floor(i.vertex / _Size) * _Size);
                float2 delta = srand2(float3(screenPos.xy, _Time.y));
                float2 uv2 = i.uv + delta * _MainTex_TexelSize.xy * 1.0 * _Strength;
                float4 col2 = tex2D(_MainTex, uv2);

                float mask = tex2D(_Mask, i.uv).r * _T;
                col = lerp(col, col2, mask) * i.color;

                col.rgb = 1.0 - (1.0 - col.rgb) * col.a;
                col.a = 1.0;

                return col;
            }
            ENDCG
        }
    }
}
