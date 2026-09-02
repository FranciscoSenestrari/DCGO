Shader "DCGO/CyberpunkPlayMat"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "black" {}
        _MainTexOpacity ("Main Texture Opacity", Range(0.0, 1.0)) = 1.0
        _MatColor ("Deep Charcoal Base", Color) = (0.039, 0.055, 0.090, 1.0)        // #0A0E17
        _GridColor ("Cyber Grid Pulse Color", Color) = (0.0, 0.823, 1.0, 1.0)       // #00D2FF (You) or #FF6B00 (Opponent)
        _AccentColor ("Card Zone Outline Color", Color) = (1.0, 0.164, 0.294, 1.0)   // #FF2A4B
        _CarbonColor ("Carbon Fiber Weave", Color) = (0.164, 0.180, 0.239, 1.0)      // #2A2E3D

        _GridScale ("Grid Scale", Float) = 25.0
        _PulseSpeed ("Energy Pulse Speed", Float) = 1.0
        _GlowIntensity ("Emissive Glow Intensity", Float) = 1.6
        _ScanSpeed ("Holographic Glare Speed", Float) = 0.8
        _GlareIntensity ("Holographic Glare Intensity", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque" 
            "Queue"="Geometry" 
        }
        LOD 100

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _MatColor;
                half4 _GridColor;
                half4 _AccentColor;
                half4 _CarbonColor;
                float _MainTexOpacity;
                float _GridScale;
                float _PulseSpeed;
                float _GlowIntensity;
                float _ScanSpeed;
                float _GlareIntensity;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                return o;
            }

            float drawBoxBorder(float2 uv, float2 center, float2 halfSize, float borderWidth)
            {
                float2 d = abs(uv - center) - halfSize;
                float outside = length(max(d, 0.0));
                float inside = min(max(d.x, d.y), 0.0);
                float dist = outside + inside;
                return smoothstep(borderWidth, 0.0, abs(dist));
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y;

                // 1. Solid Charcoal Base (#0A0E17)
                half4 col = _MatColor;

                // Subtle carbon texture
                float carbonWeave = sin(uv.x * 250.0) * sin(uv.y * 250.0) * 0.5 + 0.5;
                col.rgb = lerp(col.rgb, _CarbonColor.rgb, carbonWeave * 0.05);

                // Texture sample
                half4 mainTexCol = _MainTex.Sample(sampler_MainTex, uv);
                float blendAlpha = saturate(mainTexCol.a * _MainTexOpacity);
                col.rgb = lerp(col.rgb, mainTexCol.rgb, blendAlpha);

                // 2. Outer Border Frame
                float outerBorder = drawBoxBorder(uv, float2(0.5, 0.5), float2(0.492, 0.492), 0.003);
                col.rgb += _GridColor.rgb * outerBorder * 2.0;

                // 3. Cyber Matrix Grid
                if (_GridScale > 1.0)
                {
                    float2 gridUV = frac(uv * _GridScale);
                    float gridLineX = smoothstep(0.02, 0.0, abs(gridUV.x - 0.5));
                    float gridLineY = smoothstep(0.02, 0.0, abs(gridUV.y - 0.5));
                    float gridPattern = max(gridLineX, gridLineY);

                    float pulseWave = sin(uv.y * 10.0 - time * _PulseSpeed * 2.0) * 0.5 + 0.5;
                    pulseWave = pow(max(0.0, pulseWave), 4.0);
                    half3 gridGlow = _GridColor.rgb * (gridPattern * (0.15 + pulseWave * 0.35) * _GlowIntensity);
                    col.rgb += gridGlow;
                }

                // 4. Procedural Card Zones (Deck, Trash, Security, Breeding, Battle Area)
                float cardZones = 0.0;

                [unroll]
                for (int slot = 0; slot < 5; ++slot)
                {
                    float posX = 0.18 + (float)slot * 0.16;
                    cardZones += drawBoxBorder(uv, float2(posX, 0.5), float2(0.065, 0.14), 0.004);
                }

                cardZones += drawBoxBorder(uv, float2(0.08, 0.25), float2(0.05, 0.12), 0.004);
                cardZones += drawBoxBorder(uv, float2(0.08, 0.75), float2(0.05, 0.12), 0.004);
                cardZones += drawBoxBorder(uv, float2(0.92, 0.25), float2(0.05, 0.12), 0.004);
                cardZones += drawBoxBorder(uv, float2(0.92, 0.75), float2(0.05, 0.12), 0.004);

                half3 zoneGlow = lerp(_AccentColor.rgb, _GridColor.rgb, 0.5) * cardZones * 2.2;
                col.rgb += zoneGlow;

                // 5. Holographic Glare
                if (_GlareIntensity > 0.001)
                {
                    float glare = sin((uv.x + uv.y) * 4.0 - time * _ScanSpeed * 1.5) * 0.5 + 0.5;
                    glare = pow(max(0.0, glare), 8.0) * _GlareIntensity;
                    col.rgb += glare * _GridColor.rgb;
                }

                col.a = 1.0;
                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}