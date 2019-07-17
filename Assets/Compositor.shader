Shader "Hidden/Compositor"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

    TEXTURE2D(_ColorTexture);
    TEXTURE2D(_NormalTexture);
    TEXTURE2D(_DepthTexture);

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord : TEXCOORD0;
    };

    Varyings Vertex(uint vertexID : SV_VertexID)
    {
        Varyings output;
        output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(vertexID);
        return output;
    }

    float4 Fragment(Varyings input) : SV_Target
    {
        uint2 positionSS = input.texcoord * _ScreenSize.xy;
        float4 pn = LOAD_TEXTURE2D(_NormalTexture, positionSS);
        float2 octNormalWS = Unpack888ToFloat2(pn.rgb);
        float3 n = UnpackNormalOctQuadEncode(octNormalWS * 2.0 - 1.0);
        return float4(n, 1);
    }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            ENDHLSL
        }
    }
}
