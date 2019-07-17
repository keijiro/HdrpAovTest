#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

TEXTURE2D(_ColorTexture);
TEXTURE2D(_NormalTexture);
TEXTURE2D(_DepthTexture);

float4 _EdgeColor;
float2 _EdgeThresholds;
float _FillOpacity;

float4 _ColorKey0;
float4 _ColorKey1;
float4 _ColorKey2;
float4 _ColorKey3;
float4 _ColorKey4;
float4 _ColorKey5;
float4 _ColorKey6;
float4 _ColorKey7;

float3 LoadNormal(uint2 positionSS)
{
    float4 pn = LOAD_TEXTURE2D(_NormalTexture, positionSS);
    float2 oct = Unpack888ToFloat2(pn.rgb);
    return UnpackNormalOctQuadEncode(oct * 2 - 1);
}

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

    // Source color
    float4 c0 = LOAD_TEXTURE2D(_ColorTexture, positionSS);

    // Four sample points of the roberts cross operator
    uint2 ps0 = positionSS;               // TL
    uint2 ps1 = positionSS + uint2(1, 1); // BR
    uint2 ps2 = positionSS + uint2(1, 0); // TR
    uint2 ps3 = positionSS + uint2(0, 1); // BL

    ps1 = min(ps1, _ScreenSize.xy - 1);
    ps2 = min(ps2, _ScreenSize.xy - 1);
    ps3 = min(ps3, _ScreenSize.xy - 1);

#ifdef RECOLOR_EDGE_COLOR

    // Color samples
    float3 c1 = LOAD_TEXTURE2D(_ColorTexture, ps1).rgb;
    float3 c2 = LOAD_TEXTURE2D(_ColorTexture, ps2).rgb;
    float3 c3 = LOAD_TEXTURE2D(_ColorTexture, ps3).rgb;

    // Roberts cross operator
    float3 g1 = c1 - c0.rgb;
    float3 g2 = c3 - c2;
    float g = sqrt(dot(g1, g1) + dot(g2, g2)) * 10;

#endif

#ifdef RECOLOR_EDGE_DEPTH

    // Depth samples
    float d0 = LOAD_TEXTURE2D(_DepthTexture, ps0).r;
    float d1 = LOAD_TEXTURE2D(_DepthTexture, ps1).r;
    float d2 = LOAD_TEXTURE2D(_DepthTexture, ps2).r;
    float d3 = LOAD_TEXTURE2D(_DepthTexture, ps3).r;

    // Roberts cross operator
    float g = length(float2(d1 - d0, d3 - d2)) * 100;

#endif

#ifdef RECOLOR_EDGE_NORMAL

    // Normal samples
    float3 n0 = LoadNormal(ps0);
    float3 n1 = LoadNormal(ps1);
    float3 n2 = LoadNormal(ps2);
    float3 n3 = LoadNormal(ps3);

    // Roberts cross operator
    float3 g1 = n1 - n0;
    float3 g2 = n3 - n2;
    float g = sqrt(dot(g1, g1) + dot(g2, g2));

#endif

    // Apply fill gradient.
    float3 fill = _ColorKey0.rgb;
    float lum = Luminance(LinearToSRGB(c0.rgb));
#ifdef RECOLOR_GRADIENT_LERP
    fill = lerp(fill, _ColorKey1.rgb, saturate((lum - _ColorKey0.w) / (_ColorKey1.w - _ColorKey0.w)));
    fill = lerp(fill, _ColorKey2.rgb, saturate((lum - _ColorKey1.w) / (_ColorKey2.w - _ColorKey1.w)));
    fill = lerp(fill, _ColorKey3.rgb, saturate((lum - _ColorKey2.w) / (_ColorKey3.w - _ColorKey2.w)));
    #ifdef RECOLOR_GRADIENT_EXT
    fill = lerp(fill, _ColorKey4.rgb, saturate((lum - _ColorKey3.w) / (_ColorKey4.w - _ColorKey3.w)));
    fill = lerp(fill, _ColorKey5.rgb, saturate((lum - _ColorKey4.w) / (_ColorKey5.w - _ColorKey4.w)));
    fill = lerp(fill, _ColorKey6.rgb, saturate((lum - _ColorKey5.w) / (_ColorKey6.w - _ColorKey5.w)));
    fill = lerp(fill, _ColorKey7.rgb, saturate((lum - _ColorKey6.w) / (_ColorKey7.w - _ColorKey6.w)));
    #endif
#else
    fill = lum > _ColorKey0.w ? _ColorKey1.rgb : fill;
    fill = lum > _ColorKey1.w ? _ColorKey2.rgb : fill;
    fill = lum > _ColorKey2.w ? _ColorKey3.rgb : fill;
    #ifdef RECOLOR_GRADIENT_EXT
    fill = lum > _ColorKey3.w ? _ColorKey4.rgb : fill;
    fill = lum > _ColorKey4.w ? _ColorKey5.rgb : fill;
    fill = lum > _ColorKey5.w ? _ColorKey6.rgb : fill;
    fill = lum > _ColorKey6.w ? _ColorKey7.rgb : fill;
    #endif
#endif

    float edge = smoothstep(_EdgeThresholds.x, _EdgeThresholds.y, g);
    float3 cb = lerp(c0.rgb, fill, _FillOpacity);
    float3 co = lerp(cb, _EdgeColor.rgb, edge * _EdgeColor.a);
    return float4(co, c0.a);
}
