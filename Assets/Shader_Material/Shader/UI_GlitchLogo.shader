Shader "UI/DCGO/GlitchLogo"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Main Tint", Color) = (0.0, 0.823, 1.0, 1.0)                        // #00D2FF
        _CrimsonGlow ("Crimson Inner Glow", Color) = (1.0, 0.164, 0.294, 1.0)       // #FF2A4B
        _CarbonPattern ("Carbon Fiber Texture Color", Color) = (0.164, 0.180, 0.239, 1.0) // #2A2E3D
        _GlitchColor ("Glitch Offset Color", Color) = (1.0, 0.419, 0.0, 1.0)        // #FF6B00

        _GlitchFrequency ("Glitch Frequency", Float) = 4.0
        _GlitchAmount ("Glitch Horizontal Displacement", Float) = 0.04
        _BevelDepth ("3D Bevel Depth", Float) = 0.02

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local __ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local __ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _CrimsonGlow;
            fixed4 _CarbonPattern;
            fixed4 _GlitchColor;

            float _GlitchFrequency;
            float _GlitchAmount;
            float _BevelDepth;

            sampler2D _MainTex;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            float rand(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float time = _Time.y;

                // Horizontal Glitch Line Offset computation
                float glitchStep = floor(uv.y * 30.0);
                float glitchNoise = rand(glitchStep + floor(time * _GlitchFrequency));
                float isGlitch = step(0.88, glitchNoise);
                float offsetX = (glitchNoise - 0.5) * _GlitchAmount * isGlitch;

                float2 mainUV = float2(uv.x + offsetX, uv.y);

                // Sample texture with offset for glitch
                fixed4 col = tex2D(_MainTex, mainUV);

                // If no main texture loaded or texture is blank, generate procedural 3D block geometry effect
                if (col.a <= 0.05)
                {
                    // Procedural block bevel
                    float2 blockUV = frac(mainUV * 8.0) - 0.5;
                    float blockMask = step(abs(blockUV.x), 0.45) * step(abs(blockUV.y), 0.45);
                    col = lerp(_CarbonPattern, _Color, blockMask);
                    col.a = blockMask * 0.9;
                }

                // Apply Carbon Fiber Texture grid overlay
                float carbonGrid = sin(mainUV.x * 200.0) * sin(mainUV.y * 200.0) * 0.5 + 0.5;
                col.rgb = lerp(col.rgb, _CarbonPattern.rgb, carbonGrid * 0.25);

                // Top Crimson Inner Light glow
                float innerGlow = smoothstep(0.8, 0.1, mainUV.y) * 0.5;
                col.rgb += _CrimsonGlow.rgb * innerGlow;

                // Glitch RGB Chromatic Shift on glitch line
                if (isGlitch > 0.5)
                {
                    fixed4 glitchCol = tex2D(_MainTex, mainUV + float2(offsetX * 0.5, 0.0));
                    col.rgb = lerp(col.rgb, _GlitchColor.rgb + glitchCol.rgb, 0.6);
                }

                col.a *= IN.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
