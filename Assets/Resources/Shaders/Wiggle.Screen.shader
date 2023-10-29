// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX Screen/Wiggle"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Mul ("Multiplier", Float) = 1.0
        _Offset ("Offset", Float) = 0.0

        [Space] _XAmp ("X Amplitude", Float) = 0.0
        _YAmp ("Y Amplitude", Float) = 0.0
        _AAmp ("Alpha Amplitude", Float) = 0.0
        _BlinkAmp ("Blink Amplitude", Float) = 0.0

        [Space] _XFreq ("X Frequency", Float) = 0.0
        _YFreq ("Y Frequency", Float) = 0.0
        _TFreq ("T Frequency", Float) = 0.0
        _BlinkFreq ("Blink Frequency", Float) = 0.0

        [Space] _Mono ("Monochrome", Float) = 0.0
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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            sampler2D _MainTex;
            float _T, _Mul, _Offset, _XAmp, _YAmp, _AAmp, _BlinkAmp, _XFreq, _YFreq, _TFreq, _BlinkFreq, _Mono;

            fixed4 frag(v2f i) : SV_Target
            {
                float3 xyt = float3(_XFreq * i.uv.x, _YFreq * i.uv.y, _TFreq * _Time.y);
                float2 deltaUV = float2(_XAmp, _YAmp) * snoise2(xyt) * _T;

                float4 col = tex2D(_MainTex, i.uv + deltaUV);
                float gray = Luminance(col.rgb);
                col.rgb = lerp(col.rgb, gray, _Mono);
                col *= i.color;

                col.rgb *= _Mul;
                col.rgb += _Offset;
                float noiseBlink = noise(_BlinkFreq * _Time.y);
                col.rgb += _BlinkAmp * noiseBlink * noiseBlink * _T;

                float noiseA = _AAmp * noise(xyt);
                col.rgb *= 1.0 - noiseA;
                col.rgb = saturate(col.rgb);

                col.rgb *= col.a;
                col.a = 0.0;

                return col;
            }
            ENDCG
        }
    }
}
