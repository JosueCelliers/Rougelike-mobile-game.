Shader "BadLie/Lit"
{
    // Stonework, machinery and props. Albedo comes from vertex colours; TEXCOORD0.y masks
    // self-lit details (lamp glass, machine slot lights).
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _Wrap ("Light Wrap", Range(0,1)) = 0.35
        _Spec ("Specular", Range(0,1)) = 0.08
        _Gloss ("Gloss", Range(1,128)) = 24
        [HDR] _GlowColor ("Glow Colour", Color) = (1.4, 2.2, 2.1, 1)
        _NoiseTex ("Noise", 2D) = "gray" {}
        _NoiseScale ("Noise Scale", Float) = 0.6
        _NoiseAmount ("Noise Amount", Range(0,0.5)) = 0.12
        _LichenColor ("Lichen", Color) = (0.79, 0.65, 0.29, 1)
        _Reveal ("Reveal Fade", Float) = 1
        _WindAmount ("Wind (unused)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "BadLieVertexColor.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            half _Wrap;
            half _Spec;
            half _Gloss;
            half4 _GlowColor;
            float4 _NoiseTex_ST;
            float _NoiseScale;
            half _NoiseAmount;
            half4 _LichenColor;
            half _Reveal;
            half _WindAmount;
        CBUFFER_END
        TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex BLVert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_instancing

            half4 Frag(BLVaryings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                if (_Reveal > 0.5) BL_RevealClip(i.positionCS, i.positionWS);
                float3 n = normalize(i.normalWS);
                // Large-scale world noise breaks up flat vertex colour (weathering).
                float3 wp = i.positionWS * _NoiseScale;
                half nA = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, wp.xz + wp.y * 0.37).r;
                half nB = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, wp.zy * 1.7 + 0.5).a;
                half3 albedo = i.color.rgb * _Tint.rgb * (1.0h + (nA - 0.5h) * _NoiseAmount * 2.0h + (nB - 0.5h) * _NoiseAmount);
                // Ochre lichen: vertex alpha below 1 marks surfaces that can carry it.
                half lichenAmt = saturate((1.0h - i.color.a) * 2.0h);
                half lichenNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.positionWS.xz * 1.3 + i.positionWS.y * 0.7).b;
                half lich = smoothstep(0.42h, 0.6h, lichenNoise) * lichenAmt * saturate(n.y * 0.8h + 0.35h);
                albedo = lerp(albedo, _LichenColor.rgb * (0.85h + 0.3h * nA), lich);
                half ao = i.data.x;
                half lit;
                half3 c = BL_Shade(albedo, n, i.positionWS, ao, _Wrap, lit);
                // Soft specular for worn stone / metal.
                float3 v = normalize(_WorldSpaceCameraPos - i.positionWS);
                Light light = GetMainLight();
                float3 h = normalize(light.direction + v);
                half spec = pow(saturate(dot(n, h)), _Gloss) * _Spec * lit;
                c += spec * _BL_SunColor.rgb;
                // Self-lit details.
                c = lerp(c, _GlowColor.rgb * (0.75h + 0.25h * i.color.rgb), saturate(i.data.y));
                c = BL_ApplyFog(c, i.positionWS);
                return half4(c, 1);
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
            #pragma vertex BLShadowVert
            #pragma fragment BLShadowFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex BLDepthVert
            #pragma fragment BLDepthFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
