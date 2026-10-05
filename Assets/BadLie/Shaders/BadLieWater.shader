Shader "BadLie/Water"
{
    // Opaque stylised water (no depth texture needed on mobile).
    // COLOR.r = depth factor (0 shore .. 1 deep), COLOR.g = flow speed
    // TEXCOORD0 = (flow dir x, flow dir z, distance to shore in metres, unused)
    Properties
    {
        _Deep ("Deep", Color) = (0.07, 0.15, 0.16, 1)
        _Shallow ("Shallow", Color) = (0.17, 0.30, 0.28, 1)
        _Reflect ("Reflection Haze", Color) = (0.62, 0.50, 0.50, 1)
        _Foam ("Foam", Color) = (0.86, 0.82, 0.72, 1)
        _NoiseTex ("Noise (RGBA)", 2D) = "gray" {}
        _RippleScale ("Ripple Scale", Float) = 0.35
        _RippleStrength ("Ripple Strength", Range(0,1)) = 0.25
        _Glint ("Sun Glint", Range(0,4)) = 1.2
        _FoamWidth ("Foam Width (m)", Float) = 0.14
        _ReflectStrength ("Reflection", Range(0,1)) = 0.45
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10" }

        HLSLINCLUDE
        #include "BadLieCore.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Deep, _Shallow, _Reflect, _Foam;
            float4 _NoiseTex_ST;
            float _RippleScale;
            half _RippleStrength, _Glint, _FoamWidth, _ReflectStrength;
        CBUFFER_END
        TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float4 uv0 : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half4 color : TEXCOORD1;
                float4 d0 : TEXCOORD2;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 p = TransformObjectToWorld(v.positionOS.xyz);
                // Barely-there swell.
                p.y += sin(_BL_Time.x * 0.9 + p.x * 0.6 + p.z * 0.4) * 0.006 * v.color.r;
                o.positionWS = p;
                o.positionCS = TransformWorldToHClip(p);
                o.color = v.color;
                o.d0 = v.uv0;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 xz = i.positionWS.xz;
                float t = _BL_Time.x;
                float2 flow = i.d0.xy * i.color.g;
                float2 uvA = xz * _RippleScale + float2(t * 0.021, t * 0.013) - flow * t * 0.6;
                float2 uvB = xz * _RippleScale * 1.9 + float2(-t * 0.017, t * 0.027) - flow * t * 1.1;
                half4 a = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uvA);
                half4 b = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uvB);
                float2 ripple = (float2(a.g, b.r) - 0.5) * 2.0 * _RippleStrength;
                float3 n = normalize(float3(ripple.x, 1.0, ripple.y));

                float3 view = normalize(_WorldSpaceCameraPos - i.positionWS);
                half fres = pow(1.0h - saturate(dot(n, view)), 3.0h);
                half depth = saturate(i.color.r);
                half3 body = lerp(_Shallow.rgb, _Deep.rgb, depth);

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light light = GetMainLight(shadowCoord);
                half shadow = light.shadowAttenuation;

                // Soft reflection of the warm haze, broken by ripples.
                half refl = saturate(_ReflectStrength * (0.35h + fres) * (0.8h + 0.4h * a.b));
                half3 c = lerp(body, _Reflect.rgb, refl);
                c *= lerp(_BL_ShadowColor.rgb * 0.9h + 0.1h, half3(1, 1, 1), shadow);
                c += BL_Ambient(float3(0, 1, 0)) * body * 0.5h;

                float3 h = normalize(light.direction + view);
                half glint = pow(saturate(dot(n, h)), 220.0h) * _Glint * shadow;
                c += glint * _BL_SunColor.rgb;

                // Lapping foam line at the banks.
                half shore = i.d0.z;
                half foamNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, xz * 1.6 + float2(t * 0.05, -t * 0.04)).b;
                half foam = 1.0h - smoothstep(0.0h, _FoamWidth * (0.6h + 0.8h * foamNoise), shore);
                foam *= 0.55h + 0.45h * sin(t * 1.3 + shore * 18.0);
                c = lerp(c, _Foam.rgb * lerp(_BL_ShadowColor.rgb, _BL_SunColor.rgb, shadow) * 0.85h, saturate(foam) * 0.65h);

                c = BL_ApplyFog(c, i.positionWS);
                return half4(c, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            float4 DepthVert(float4 positionOS : POSITION) : SV_POSITION { return TransformObjectToHClip(positionOS.xyz); }
            half4 DepthFrag(float4 pos : SV_POSITION) : SV_Target { return pos.z; }
            ENDHLSL
        }
    }
}
