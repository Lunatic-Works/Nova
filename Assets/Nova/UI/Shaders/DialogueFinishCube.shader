Shader "Nova/UI/Dialogue Finish Cube"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Back ZWrite On ZTest LEqual Blend Off
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float c : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.c = abs(dot(v.normal, normalize(ObjSpaceViewDir(v.vertex))));
                o.c = o.c * 0.6 + 0.3;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(i.c, i.c, i.c, 1.0);
            }
            ENDCG
        }
    }
}
