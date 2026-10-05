Shader "BadLie/Foliage"
{
    // Sculpted vegetation built from leaf flakes. Albedo from vertex colour, TEXCOORD0.y is the
    // sway weight, TEXCOORD0.z a per-flake random used for wind phase and colour jitter.
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _Wrap ("Light Wrap", Range(0,1)) = 0.55
        _Translucency ("Back-light", Range(0,2)) = 0.6
        _Jitter ("Colour Jitter", Range(0,0.4)) = 0.04
        _WindAmount ("Wind", Range(0,2)) = 1
        _Reveal ("Reveal Fade", Float) = 1
        _RimLight ("Rim", Range(0,1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        HLSLINCLUDE
        #define BL_WIND 1
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            half _Wrap;
            half _Translucency;
            half _Jitter;
            half _WindAmount;
            half _Reveal;
            half _RimLight;
        CBUFFER_END
        #include "BadLieVertexColor.hlsl"
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex BLVert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_instancing

            half4 Frag(BLVaryings i, bool front : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                if (_Reveal > 0.5) BL_RevealClip(i.positionCS, i.positionWS);
                float3 n = normalize(i.normalWS);
                n = front ? n : -n;
                half rnd = i.data.z;
                half3 albedo = i.color.rgb * _Tint.rgb * (1.0h + (rnd - 0.5h) * _Jitter * 2.0h);
                half ao = i.data.x;
                half lit;
                half3 c = BL_Shade(albedo, n, i.positionWS, ao, _Wrap, lit);
                float3 v = normalize(_WorldSpaceCameraPos - i.positionWS);
                Light light = GetMainLight();
                // Sun shining through leaf edges.
                half back = pow(saturate(dot(v, -light.direction) * 0.5h + 0.5h), 3.0h);
                half shadow = BL_MainShadow(i.positionWS);
                c += albedo * _BL_SunColor.rgb * back * _Translucency * 0.35h * shadow * ao;
                // Warm rim on lit silhouettes keeps masses separated.
                half rim = pow(1.0h - saturate(dot(n, v)), 3.0h) * _RimLight * lit;
                c += rim * _BL_SunColor.rgb * albedo;
                c = BL_ApplyFog(c, i.positionWS);
                return half4(c, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex BLShadowVert
            #pragma fragment BLShadowFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex BLDepthVert
            #pragma fragment BLDepthFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
