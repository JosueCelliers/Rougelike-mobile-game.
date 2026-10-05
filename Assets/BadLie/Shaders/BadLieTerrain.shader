Shader "BadLie/Terrain"
{
    // Playing surfaces and the faces of the ground slabs.
    // COLOR      = coverage of (fairway, green, sand, stone); rough is the base layer
    // TEXCOORD0  = (runnel coverage, ao, green-edge distance, pad-edge distance)
    // TEXCOORD1  = (is side face, u along edge, depth below top, edge style)
    // TEXCOORD2  = (flow dir x, flow dir z, wetness, unused)
    Properties
    {
        _RoughA ("Rough A", Color) = (0.30, 0.34, 0.12, 1)
        _RoughB ("Rough B", Color) = (0.22, 0.26, 0.09, 1)
        _FairA ("Fairway A", Color) = (0.30, 0.52, 0.17, 1)
        _FairB ("Fairway B", Color) = (0.24, 0.45, 0.14, 1)
        _GreenA ("Green A", Color) = (0.38, 0.62, 0.20, 1)
        _GreenB ("Green B", Color) = (0.33, 0.57, 0.18, 1)
        _Collar ("Collar", Color) = (0.27, 0.48, 0.15, 1)
        _Sand ("Sand", Color) = (0.78, 0.60, 0.38, 1)
        _SandDark ("Sand Shade", Color) = (0.62, 0.45, 0.27, 1)
        _Stone0 ("Sett Sandstone", Color) = (0.72, 0.55, 0.38, 1)
        _Stone1 ("Sett Salmon", Color) = (0.70, 0.42, 0.33, 1)
        _Stone2 ("Sett Ochre", Color) = (0.70, 0.50, 0.20, 1)
        _Stone3 ("Sett Sage", Color) = (0.42, 0.45, 0.32, 1)
        _Stone4 ("Sett Grey", Color) = (0.52, 0.48, 0.44, 1)
        _Grout ("Grout", Color) = (0.58, 0.48, 0.36, 1)
        _Runnel ("Runnel Water", Color) = (0.20, 0.38, 0.40, 1)
        _Earth ("Earth", Color) = (0.30, 0.20, 0.15, 1)
        _EarthDark ("Earth Dark", Color) = (0.17, 0.11, 0.10, 1)
        _WallStone ("Wall Stone", Color) = (0.70, 0.54, 0.38, 1)
        _Brick ("Brick", Color) = (0.62, 0.28, 0.19, 1)
        _Metal ("Metal", Color) = (0.12, 0.11, 0.11, 1)
        _NoiseTex ("Noise (RGBA)", 2D) = "gray" {}
        _SettsTex ("Setts (RGBA)", 2D) = "gray" {}
        _SettsScale ("Sett Tile (m)", Float) = 3.0
        _StripeWidth ("Fairway Stripe (m)", Float) = 1.7
        _GreenStripe ("Green Stripe (m)", Float) = 0.9
        _Wrap ("Light Wrap", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "BadLieCore.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _RoughA, _RoughB, _FairA, _FairB, _GreenA, _GreenB, _Collar, _Sand, _SandDark;
            half4 _Stone0, _Stone1, _Stone2, _Stone3, _Stone4, _Grout, _Runnel;
            half4 _Earth, _EarthDark, _WallStone, _Brick, _Metal;
            float4 _NoiseTex_ST, _SettsTex_ST;
            float _SettsScale, _StripeWidth, _GreenStripe;
            half _Wrap;
        CBUFFER_END
        TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
        TEXTURE2D(_SettsTex); SAMPLER(sampler_SettsTex);
        float4 _BL_Stripe; // xy: stripe direction on the ground plane
        float3 _LightDirection;

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 color : COLOR;
            float4 uv0 : TEXCOORD0;
            float4 uv1 : TEXCOORD1;
            float4 uv2 : TEXCOORD2;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            half4 cover : TEXCOORD2;
            float4 d0 : TEXCOORD3;
            float4 d1 : TEXCOORD4;
            float4 d2 : TEXCOORD5;
        };
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

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.cover = v.color;
                o.d0 = v.uv0;
                o.d1 = v.uv1;
                o.d2 = v.uv2;
                return o;
            }

            half Sharpen(half c)
            {
                half w = max(fwidth(c), 0.002h);
                return saturate((c - 0.5h) / (w * 1.6h) + 0.5h);
            }

            half Band(float coord)
            {
                // Anti-aliased square wave, 0..1
                float tri = abs(frac(coord) * 2.0 - 1.0);
                float w = max(fwidth(tri), 1e-4);
                return saturate((tri - 0.5) / (w * 1.5) + 0.5);
            }

            half3 SettColor(half r)
            {
                half3 c = _Stone0.rgb;
                c = r > 0.30h ? _Stone1.rgb : c;
                c = r > 0.48h ? _Stone2.rgb : c;
                c = r > 0.64h ? _Stone3.rgb : c;
                c = r > 0.80h ? _Stone4.rgb : c;
                return c;
            }

            half3 TopSurface(Varyings i, float2 xz, inout float3 n, out half sheen)
            {
                half4 nLow = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, xz * 0.045);
                half4 nMid = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, xz * 0.23);
                half4 nHi = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, xz * 1.35);

                // Rough: long grass in soft clumps, darker olive than the fairway.
                half4 nClump = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, xz * 0.09);
                half clump = smoothstep(0.2h, 0.8h, nClump.b * 0.6h + nLow.g * 0.4h);
                half3 rough = lerp(_RoughB.rgb, _RoughA.rgb, clump);
                rough *= 0.9h + 0.18h * nLow.r;
                rough *= 0.93h + 0.1h * nHi.a;
                rough *= 0.95h + 0.08h * smoothstep(0.35h, 0.65h, nHi.b);

                // Fairway: wide mowing stripes across the line of play.
                float2 sd = _BL_Stripe.xy;
                float coord = dot(xz, sd) / _StripeWidth;
                half stripe = Band(coord + (nLow.g - 0.5) * 0.25);
                half3 fair = lerp(_FairA.rgb, _FairB.rgb, stripe);
                fair *= 0.93h + 0.12h * nLow.g;
                fair *= 0.97h + 0.06h * nHi.a;

                // Green: tight cross-cut stripes, smoother, a darker collar at the edge.
                float2 sd2 = float2(-sd.y, sd.x);
                half g1 = Band(dot(xz, sd) / _GreenStripe);
                half g2 = Band(dot(xz, sd2) / _GreenStripe);
                half3 green = lerp(_GreenA.rgb, _GreenB.rgb, g1 * 0.7h + g2 * 0.3h);
                green *= 0.96h + 0.06h * nLow.g;
                half collar = 1.0h - smoothstep(0.30h, 0.42h, -i.d0.z);
                collar *= step(i.d0.z, 0.02h);
                green = lerp(green, _Collar.rgb * (0.95h + 0.1h * nHi.a), saturate(collar));

                // Stone setts: per-stone palette colour, grout, bevel shading.
                half4 st = SAMPLE_TEXTURE2D(_SettsTex, sampler_SettsTex, xz / _SettsScale);
                half3 stone = lerp(SettColor(st.r), _Stone0.rgb, 0.38h) * (0.88h + 0.22h * st.g);
                stone *= 0.85h + 0.25h * st.b;
                stone = lerp(stone, _Grout.rgb * (0.85h + 0.2h * nHi.r), st.a);
                stone *= 0.92h + 0.12h * nLow.r;
                // Lichen and wear.
                stone = lerp(stone, stone * half3(1.08, 1.0, 0.72), smoothstep(0.62h, 0.8h, nLow.b) * 0.5h);
                const float e = 0.03;
                half hx = SAMPLE_TEXTURE2D(_SettsTex, sampler_SettsTex, (xz + float2(e, 0)) / _SettsScale).b;
                half hz = SAMPLE_TEXTURE2D(_SettsTex, sampler_SettsTex, (xz + float2(0, e)) / _SettsScale).b;
                float2 bevelGrad = float2(hx - st.b, hz - st.b) / e * 0.025;

                // Runnel: thin water sheet over stone with flowing streaks.
                float2 flow = i.d2.xy;
                float t = _BL_Time.x;
                float along = dot(xz, flow);
                float across = dot(xz, float2(-flow.y, flow.x));
                half streak = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, float2(across * 0.9, along * 0.25 - t * 0.45)).g;
                half streak2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, float2(across * 1.7 + 0.3, along * 0.5 - t * 0.8)).a;
                half3 runnel = lerp(stone * 0.55h, _Runnel.rgb, 0.65h);
                runnel += smoothstep(0.62h, 0.85h, streak * 0.6h + streak2 * 0.5h) * 0.16h;

                // Sand: raked lines, grain, damp and darker toward its rim.
                float rake = sin(dot(xz, sd2) * 9.0 + nLow.r * 6.0) * 0.5 + 0.5;
                half3 sand = lerp(_SandDark.rgb, _Sand.rgb, 0.72h + 0.28h * rake);
                sand *= 0.94h + 0.1h * nHi.a;

                half wF = Sharpen(i.cover.r);
                half wG = Sharpen(i.cover.g);
                half wSa = Sharpen(i.cover.b);
                half wSt = Sharpen(i.cover.a);
                half wR = Sharpen(i.d0.x);

                half3 c = rough;
                c = lerp(c, fair, wF);
                c = lerp(c, green, wG);
                c = lerp(c, runnel, wR);
                c = lerp(c, stone, wSt * (1.0h - wR));
                c = lerp(c, sand, wSa);

                // Bevelled setts catch the light.
                Light light = GetMainLight();
                n = normalize(n + float3(-bevelGrad.x, 0, -bevelGrad.y) * wSt * (1.0h - wR));
                // Wet dark band near water.
                c *= 1.0h - 0.28h * saturate(i.d2.z);
                // Slight darkening right at a slab's lip.
                c *= 1.0h - 0.18h * (1.0h - smoothstep(0.0h, 0.18h, i.d0.w));
                sheen = 0.08h * wG + 0.35h * wR + 0.06h * wSt;
                return c;
            }

            half3 SideFace(Varyings i, float2 xz, inout float3 n)
            {
                float u = i.d1.y, v = i.d1.z;
                half style = i.d1.w;
                half4 nLow = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, float2(u * 0.11, v * 0.3 + xz.x * 0.01));
                half4 nHi = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, float2(u * 0.9, v * 1.3));
                half3 c;
                if (style < 0.5h)
                {
                    // Earth bank: strata, roots, a turf lip.
                    half strata = sin(v * 13.0 + nLow.r * 5.0) * 0.5h + 0.5h;
                    c = lerp(_EarthDark.rgb, _Earth.rgb, strata * 0.6h + nHi.g * 0.4h);
                    half roots = smoothstep(0.78h, 0.86h, nHi.b) * (1.0h - smoothstep(0.1h, 0.8h, v));
                    c = lerp(c, _EarthDark.rgb * 0.6h, roots);
                    c = lerp(c, _RoughB.rgb, 1.0h - smoothstep(0.03h, 0.09h + nHi.r * 0.05h, v));
                }
                else if (style < 1.5h || style > 3.5h)
                {
                    // Coursed sandstone (style 1) or coping (style 4).
                    half course = style > 3.5h ? 0.34h : 0.27h;
                    float row = floor(v / course);
                    float blockLen = 0.62 + 0.18 * BL_Hash12(float2(row, 7));
                    float uu = u / blockLen + (fmod(row, 2.0) * 0.5);
                    float col = floor(uu);
                    float fu = frac(uu), fv = frac(v / course);
                    half joint = 1.0h - smoothstep(0.0h, 0.04h, min(min(fu, 1.0 - fu) * blockLen, min(fv, 1.0 - fv) * course));
                    half rnd = BL_Hash12(float2(col, row));
                    c = _WallStone.rgb * (0.82h + 0.3h * rnd) * (0.9h + 0.15h * nHi.g);
                    c = lerp(c, c * half3(1.12, 1.02, 0.66), smoothstep(0.55h, 0.75h, nLow.b) * 0.6h);
                    c = lerp(c, _EarthDark.rgb * 1.4h, joint * 0.85h);
                    // Top course is a lighter cap.
                    c = lerp(c * 1.12h, c, smoothstep(0.0h, 0.05h, v - course));
                }
                else if (style < 2.5h)
                {
                    float row = floor(v / 0.085);
                    float uu = u / 0.24 + fmod(row, 2.0) * 0.5;
                    float fu = frac(uu), fv = frac(v / 0.085);
                    half joint = 1.0h - smoothstep(0.0h, 0.05h, min(min(fu, 1.0 - fu) * 0.24, min(fv, 1.0 - fv) * 0.085) * 4.0);
                    half rnd = BL_Hash12(float2(floor(uu), row));
                    c = _Brick.rgb * (0.8h + 0.35h * rnd) * (0.9h + 0.15h * nHi.g);
                    c = lerp(c, _Grout.rgb * 0.8h, joint * 0.8h);
                }
                else
                {
                    half seam = 1.0h - smoothstep(0.0h, 0.02h, abs(frac(u / 1.6) - 0.5) - 0.48);
                    c = _Metal.rgb * (0.9h + 0.2h * nHi.g);
                    c += seam * 0.03h;
                }
                // Water stain toward the foot of the face.
                c *= lerp(1.0h, 0.75h, saturate(i.d2.z) * smoothstep(0.1h, 0.7h, v));
                return c;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 xz = i.positionWS.xz;
                float3 n = normalize(i.normalWS);
                half sheen = 0.0h;
                half3 albedo = (i.d1.x > 0.5) ? SideFace(i, xz, n) : TopSurface(i, xz, n, sheen);
                half ao = i.d0.y;
                half lit;
                half3 c = BL_Shade(albedo, n, i.positionWS, ao, _Wrap, lit);
                float3 view = normalize(_WorldSpaceCameraPos - i.positionWS);
                Light light = GetMainLight();
                float3 h = normalize(light.direction + view);
                c += pow(saturate(dot(n, h)), 40.0) * sheen * lit * _BL_SunColor.rgb;
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

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            float4 DepthVert(Attributes v) : SV_POSITION { return TransformObjectToHClip(v.positionOS.xyz); }
            half4 DepthFrag(float4 pos : SV_POSITION) : SV_Target { return pos.z; }
            ENDHLSL
        }
    }
}
