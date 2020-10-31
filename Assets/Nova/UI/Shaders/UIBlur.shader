Shader "Nova/UI/Blur"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _Size ("Size", Float) = 1.0
        _Offset ("Offset", Float) = 0.0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend Off
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

        GrabPass { "_UIBlur" }

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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 grabPos : TEXCOORD1;
            };

            sampler2D _UIBlur;
            float4 _UIBlur_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _UIBlur);
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }

            float4 _UIBlur_TexelSize;
            float _Size, _Offset;
            float _GScale;

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = tex2DProjGaussianBlur(_UIBlur, _UIBlur_TexelSize * _GScale, i.grabPos, _Size);
                col.rgb += _Offset;
                col.a = 1.0;
                return col;
            }
            ENDCG
        }
    }
}
