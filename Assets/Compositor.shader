Shader "Hidden/Compositor"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _NormalTex;
            sampler2D _DepthTex;

            fixed4 frag (v2f i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
                float4 n = tex2D(_NormalTex, i.uv);
                float4 d = tex2D(_DepthTex, i.uv);
                return float4(c.r, n.r, d.r, 1);
            }

            ENDCG
        }
    }
}
