Shader "BadLie/Ball"
{
    // The ball: brightest object on screen, with an ink rim for contrast on pale ground and a
    // silhouette pass that shows it through anything in front of it.
    Properties
    {
        _Color ("Colour", Color) = (0.98, 0.96, 0.92, 1)
        _Rim ("Ink Rim", Color) = (0.16, 0.10, 0.16, 1)
        _RimWidth ("Rim Width", Range(0,1)) = 0.38
        [HDR] _Silhouette ("Hidden Silhouette", Color) = (1.0, 0.85, 0.55, 0.85)
        _Lift ("Shadow Lift", Range(0,1)) = 0.45
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+50" }

        HLSLINCLUDE
        #include "BadLieCore.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Color, _Rim, _Silhouette;
            half _RimWidth, _Lift;
        CBUFFER_END
        float3 _LightDirection;

        struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
        struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };

        Varyings Vert(Attributes v)
        {
            Varyings o;
            o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
            o.positionCS = TransformWorldToHClip(o.positionWS);
            o.normalWS = TransformObjectToWorldNormal(v.normalOS);
            return o;
        }
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

            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 view = normalize(_WorldSpaceCameraPos - i.positionWS);
                float4 sc = TransformWorldToShadowCoord(i.positionWS);
                Light light = GetMainLight(sc);
                half ndl = saturate(dot(n, light.direction) * 0.6h + 0.4h);
                half sh = lerp(_Lift, 1.0h, light.shadowAttenuation);
                half3 c = _Color.rgb * lerp(half3(0.62, 0.62, 0.78), half3(1.18, 1.12, 1.02), ndl * sh);
                float3 h = normalize(light.direction + view);
                c += pow(saturate(dot(n, h)), 60.0h) * 0.6h * sh;
                half rim = smoothstep(1.0h - _RimWidth, 1.0h, 1.0h - saturate(dot(n, view)));
                c = lerp(c, _Rim.rgb, rim);
                return half4(c, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Hidden"
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZTest Greater
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 view = normalize(_WorldSpaceCameraPos - i.positionWS);
                half edge = 1.0h - saturate(dot(n, view));
                return half4(_Silhouette.rgb * (0.6h + edge), _Silhouette.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            float4 ShadowVert(Attributes v) : SV_POSITION
            {
                float3 p = TransformObjectToWorld(v.positionOS.xyz);
                float3 n = TransformObjectToWorldNormal(v.normalOS);
                return ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(p, n, _LightDirection)));
            }
            half4 ShadowFrag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
