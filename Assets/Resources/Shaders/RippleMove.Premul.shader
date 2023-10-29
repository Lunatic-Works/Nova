// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/Premul/Ripple Move"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _Aspect ("Aspect Ratio", Float) = 1.77777778
        _Amp ("Amplitude", Float) = 0.01
        _RFreq ("Radial Frequency", Float) = 50.0
        _TFreq ("Time Frequency", Float) = 3.0
        _BlurSize ("Blur Size", Float) = 1.0
        _Width ("Peak Width", Float) = 0.1
        _Fade ("Fade Time", Float) = 0.1
    }
    SubShader
    {
        Cull Off ZWrite Off Blend One OneMinusSrcAlpha
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
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
                float2 tex1 : TEXCOORD1; // x: r max, y: fade factor
                float4 color : COLOR;
            };

            float _T, _Aspect, _Fade;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.tex1.x = 0.5 * sqrt(1.0 + _Aspect * _Aspect);
                o.tex1.y = saturate(_T / _Fade) * saturate((1.0 - _T) / _Fade);
                o.color = v.color;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Amp, _RFreq, _TFreq, _BlurSize, _Width;

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                uv -= 0.5;
                uv.x *= _Aspect;
                float r = length(uv);
                r = max(r, 1e-3);
                float dr = sin(_RFreq * r + _TFreq * _Time.y);

                float t = r / i.tex1.x;
                float dt = (_T - t) / _Width;
                dr /= 1.0 + dt * dt;
                dr *= i.tex1.y;

                dr = dr * dr * dr;
                uv *= (r + min(r, _Amp) * dr) / r;
                uv.x /= _Aspect;
                uv += 0.5;

                float4 col;
                if (_BlurSize > 1e-3)
                {
                    col = tex2DGaussianBlur(_MainTex, _MainTex_TexelSize * 1.0, uv, _BlurSize * abs(dr));
                }
                else
                {
                    col = tex2D(_MainTex, uv);
                }
                col *= i.color;

                return col;
            }
            ENDCG
        }
    }
}
