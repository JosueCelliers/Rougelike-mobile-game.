#ifndef BADLIE_VERTEXCOLOR_INCLUDED
#define BADLIE_VERTEXCOLOR_INCLUDED

// Shared forward / shadow / depth passes for the vertex-coloured environment kit.
// COLOR.rgb = albedo (linear), TEXCOORD0: x = baked AO, y = sway (foliage) or glow mask (lit),
// z = per-element random, w = unused.

#include "BadLieCore.hlsl"

float3 _LightDirection;

struct BLAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 color      : COLOR;
    float4 uv0        : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct BLVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS   : TEXCOORD1;
    half4 color       : TEXCOORD2;
    float4 data       : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float3 BL_WindOffset(float3 positionWS, float sway, float rnd)
{
#if defined(BL_WIND)
    float t = _BL_Time.x;
    float phase = dot(positionWS.xz, float2(0.31, 0.47)) + rnd * 6.2831;
    float gust = 0.6 + 0.4 * sin(t * 0.37 + positionWS.x * 0.05);
    float s = sin(t * 1.7 + phase) * 0.6 + sin(t * 3.1 + phase * 1.7) * 0.25;
    float2 dir = _BL_Wind.xy;
    float amount = sway * _BL_Wind.z * gust * _WindAmount;
    return float3(dir.x * s, 0.15 * s, dir.y * s) * amount;
#else
    return 0;
#endif
}

BLVaryings BLVert(BLAttributes input)
{
    BLVaryings o = (BLVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    positionWS += BL_WindOffset(positionWS, input.uv0.y, input.uv0.z);
    o.positionWS = positionWS;
    o.positionCS = TransformWorldToHClip(positionWS);
    o.normalWS = TransformObjectToWorldNormal(input.normalOS);
    o.color = input.color;
    o.data = input.uv0;
    return o;
}

struct BLShadowVaryings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

BLShadowVaryings BLShadowVert(BLAttributes input)
{
    BLShadowVaryings o = (BLShadowVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o);
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    positionWS += BL_WindOffset(positionWS, input.uv0.y, input.uv0.z);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
    o.positionCS = ApplyShadowClamping(positionCS);
    return o;
}

half4 BLShadowFrag(BLShadowVaryings input) : SV_Target
{
    return 0;
}

BLShadowVaryings BLDepthVert(BLAttributes input)
{
    BLShadowVaryings o = (BLShadowVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o);
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    positionWS += BL_WindOffset(positionWS, input.uv0.y, input.uv0.z);
    o.positionCS = TransformWorldToHClip(positionWS);
    return o;
}

half4 BLDepthFrag(BLShadowVaryings input) : SV_Target
{
    return input.positionCS.z;
}

#endif
