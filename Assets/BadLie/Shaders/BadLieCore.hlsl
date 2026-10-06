#ifndef BADLIE_CORE_INCLUDED
#define BADLIE_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// ---------------------------------------------------------------------------------------
// Global look parameters, written by the Atmosphere component (one place to art-direct).
// ---------------------------------------------------------------------------------------
float4 _BL_SunColor;       // rgb: lit colour multiplier (includes intensity)
float4 _BL_ShadowColor;    // rgb: colour multiplier in shadow (violet-brown)
float4 _BL_AmbientSky;     // rgb: fill from above
float4 _BL_AmbientGround;  // rgb: fill from below (teal)
float4 _BL_AOColor;        // rgb: tint of contact darkening
float4 _BL_FogColor;       // rgb: distance haze colour
float4 _BL_DepthColor;     // rgb: colour of the depths (height fog below the courses)
float4 _BL_FogHeight;      // x: fog top (y), y: fog bottom (y), z: max amount
float4 _BL_FogDistance;    // x: start, y: end, z: max amount
float4 _BL_Reveal;         // xy: ball viewport uv, z: radius (viewport height units), w: ball eye depth
float4 _BL_Reveal2;        // xy: aim end viewport uv, z: aim reveal on (0/1), w: screen aspect (w/h)
float4 _BL_Wind;           // xy: direction, z: strength, w: unused
float4 _BL_Time;           // x: seconds

// ---------------------------------------------------------------------------------------
half3 BL_Ambient(float3 n)
{
    return lerp(_BL_AmbientGround.rgb, _BL_AmbientSky.rgb, saturate(n.y * 0.5h + 0.5h));
}

float BL_MainShadow(float3 positionWS)
{
    float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
    return MainLightRealtimeShadow(shadowCoord);
}

// Stylised lighting: wrapped diffuse, coloured shadows, coloured ambient and AO.
half3 BL_Shade(half3 albedo, float3 n, float3 positionWS, half ao, half wrap, out half lit)
{
    float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
    Light light = GetMainLight(shadowCoord);
    half ndl = dot(n, light.direction);
    half diffuse = saturate((ndl + wrap) / (1.0h + wrap));
    // Soften the terminator so forms read as sculpted rather than toon.
    diffuse = smoothstep(0.0h, 1.0h, diffuse);
    lit = diffuse * light.shadowAttenuation;
    half3 direct = lerp(_BL_ShadowColor.rgb, _BL_SunColor.rgb, lit);
    half3 aoTint = lerp(_BL_AOColor.rgb, half3(1, 1, 1), ao);
    half3 c = albedo * (direct + BL_Ambient(n)) * aoTint;
    return c;
}

half BL_DepthFog(float3 positionWS)
{
    return (half)(saturate((_BL_FogHeight.x - positionWS.y) / max(_BL_FogHeight.x - _BL_FogHeight.y, 0.001)) * _BL_FogHeight.z);
}

half BL_DistanceFog(float3 positionWS)
{
    float d = distance(positionWS, _WorldSpaceCameraPos);
    return (half)(saturate((d - _BL_FogDistance.x) / max(_BL_FogDistance.y - _BL_FogDistance.x, 0.001)) * _BL_FogDistance.z);
}

// Total fog cover (for effects that fade rather than tint).
half BL_FogAmount(float3 positionWS)
{
    return 1.0h - (1.0h - BL_DepthFog(positionWS)) * (1.0h - BL_DistanceFog(positionWS));
}

// The lower estate sinks into cool depth; the far distance into warm haze.
half3 BL_ApplyFog(half3 c, float3 positionWS)
{
    c = lerp(c, _BL_DepthColor.rgb, BL_DepthFog(positionWS));
    return lerp(c, _BL_FogColor.rgb, BL_DistanceFog(positionWS));
}

// ---------------------------------------------------------------------------------------
// Reveal: geometry between the camera and the ball (or the aim line) dithers away so the
// ball and the shot path are never hidden.
// ---------------------------------------------------------------------------------------
static const float BL_BayerM[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

float BL_Bayer4(float2 pixel)
{
    int2 p = int2(fmod(floor(pixel), 4.0));
    return (BL_BayerM[p.y * 4 + p.x] + 0.5) / 16.0;
}

float BL_SegmentDistance(float2 p, float2 a, float2 b)
{
    float2 ab = b - a;
    float t = saturate(dot(p - a, ab) / max(dot(ab, ab), 1e-6));
    return length(p - (a + ab * t));
}

void BL_RevealClip(float4 positionCS, float3 positionWS)
{
    if (_BL_Reveal.z <= 0.0) return;
    float2 uv = GetNormalizedScreenSpaceUV(positionCS);
    float aspect = _BL_Reveal2.w;
    float2 p = float2(uv.x * aspect, uv.y);
    float2 ball = float2(_BL_Reveal.x * aspect, _BL_Reveal.y);
    float d = length(p - ball);
    if (_BL_Reveal2.z > 0.5)
    {
        float2 aimEnd = float2(_BL_Reveal2.x * aspect, _BL_Reveal2.y);
        d = min(d, BL_SegmentDistance(p, ball, aimEnd) * 1.25);
    }
    float eyeDepth = -TransformWorldToView(positionWS).z;
    // Only fragments clearly in front of the ball fade.
    float inFront = saturate((_BL_Reveal.w - eyeDepth - 0.6) * 0.5);
    float fade = saturate(1.0 - d / _BL_Reveal.z) * inFront;
    fade = smoothstep(0.0, 0.55, fade) * 0.82;
    clip(BL_Bayer4(positionCS.xy) - fade);
}

// ---------------------------------------------------------------------------------------
float BL_Hash12(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float BL_ValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = BL_Hash12(i), b = BL_Hash12(i + float2(1, 0));
    float c = BL_Hash12(i + float2(0, 1)), d = BL_Hash12(i + float2(1, 1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

#endif
