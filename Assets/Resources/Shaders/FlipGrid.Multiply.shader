// This file is generated. Do not edit it manually. Please edit .shaderproto files.

Shader "Nova/VFX Multiply/Flip Grid"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        [NoScaleOffset] _SubTex ("Second Texture", 2D) = "black" {}
        _SubColor ("Second Texture Color", Color) = (1, 1, 1, 1)
        _T ("Time", Range(0.0, 1.0)) = 0.0
        _BackColor ("Background Color", Color) = (0, 0, 0, 1)
        _GridSize ("Grid Size", Float) = 64.0
        _FlipDuration ("Flip Duration", Range(0.0, 1.0)) = 0.5
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

            #define PI 3.1415927
            #define smoothStepIn(x) ((x) * (x) * (2.0 - x))

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
                float3 gridInfo : TEXCOORD1; // x: max diag num; y: flip start time interval; z: half duration
            };

            sampler2D _MainTex, _SubTex;
            float4 _MainTex_TexelSize, _SubColor, _BackColor;
            float _T, _GridSize, _FlipDuration;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                float2 gridIndex = _MainTex_TexelSize.zw / _GridSize;
                o.gridInfo.x = round(gridIndex.x) + round(gridIndex.y);
                o.gridInfo.y = (1.0 - _FlipDuration) / o.gridInfo.x;
                o.gridInfo.z = _FlipDuration / 2.0;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 pixelPos = i.uv * _MainTex_TexelSize.zw / _GridSize;
                float2 gridIndex = floor(pixelPos); // Left bottom point of grid
                float gridDiagIndex = gridIndex.x + gridIndex.y;
                float flipStartTime = gridDiagIndex * i.gridInfo.y;
                float flipEndTime = flipStartTime + _FlipDuration;

                float4 col = _BackColor;

                if (flipStartTime > _T)
                {
                    // Transition has not arrive here
                    col = tex2D(_MainTex, i.uv) * i.color;
                }
                else if (flipEndTime < _T)
                {
                    // Transition finished
                    col = tex2D(_SubTex, i.uv) * _SubColor;
                }
                else
                {
                    float2 p = frac(pixelPos);
                    float2 intersect = float2(1 + p.x - p.y, 1 - p.x + p.y) / 2.0;
                    float deltaTime = _T - flipStartTime;
                    if (deltaTime < i.gridInfo.z)
                    {
                        float theta = PI / 2.0 * smoothStepIn(deltaTime / i.gridInfo.z);
                        float dis = (1 - p.x - p.y) * 0.5 / (cos(theta) + 0.001);
                        float2 q = intersect - dis;
                        if (q.x < 0.0 || q.y < 0.0 || q.x > 1.0 || q.y > 1.0)
                        {
                            return col;
                        }
                        float2 realUV = (gridIndex + q) * _GridSize * _MainTex_TexelSize.xy;
                        col = tex2D(_MainTex, realUV) * i.color;
                    }
                    else
                    {
                        float theta = PI / 2.0 * smoothStepIn((_FlipDuration - deltaTime) / i.gridInfo.z);
                        float dis = (1 - p.x - p.y) * 0.5 / (cos(theta) + 0.001);
                        float2 q = intersect - dis;
                        if (q.x < 0.0 || q.y < 0.0 || q.x > 1.0 || q.y > 1.0)
                        {
                            return col;
                        }
                        float2 realUV = (gridIndex + q) * _GridSize * _MainTex_TexelSize.xy;
                        col = tex2D(_SubTex, realUV) * _SubColor;
                    }
                }

                col.rgb = 1.0 - (1.0 - col.rgb) * col.a;
                col.a = 1.0;

                return col;
            }
            ENDCG
        }
    }
}
