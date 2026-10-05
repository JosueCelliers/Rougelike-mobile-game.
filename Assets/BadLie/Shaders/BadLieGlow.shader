Shader "BadLie/Glow"
{
    // Self-lit accents: lantern glass, luminous lilies, machine lights. Kept sparse.
    Properties
    {
        [HDR] _Color ("Colour", Color) = (1.6, 2.6, 2.5, 1)
        _Pulse ("Pulse", Range(0,1)) = 0.15
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "BadLieCore.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Pulse;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.color = v.color;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 view = normalize(_WorldSpaceCameraPos - i.positionWS);
                half core = 0.7h + 0.3h * saturate(dot(normalize(i.normalWS), view));
                half pulse = 1.0h - _Pulse + _Pulse * sin(_BL_Time.x * 1.6 + i.positionWS.x * 0.7 + i.positionWS.z);
                half3 c = _Color.rgb * i.color.rgb * core * pulse;
                c = lerp(c, _BL_FogColor.rgb, BL_FogAmount(i.positionWS) * 0.7h);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
