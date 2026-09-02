Shader "UI/DCGO/CyberpunkBackground"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _DarkBgColor ("Deep Matrix Charcoal", Color) = (0.039, 0.055, 0.090, 1.0) // #0A0E17
        _CyanColor ("Cyber Cyan", Color) = (0.0, 0.823, 1.0, 1.0)                 // #00D2FF
        _OrangeColor ("Neon Orange", Color) = (1.0, 0.419, 0.0, 1.0)               // #FF6B00
        _CrimsonColor ("Crimson Red", Color) = (1.0, 0.164, 0.294, 1.0)            // #FF2A4B
        _CarbonColor ("Carbon Grey", Color) = (0.164, 0.180, 0.239, 1.0)            // #2A2E3D

        _GridScale ("Grid Scale", Float) = 40.0
        _GridSpeed ("Grid Speed", Float) = 0.5
        _BloomIntensity ("Bloom Intensity", Float) = 1.2
        _ScanlineSpeed ("Scanline Speed", Float) = 2.0

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
            fixed4 _DarkBgColor;
            fixed4 _CyanColor;
            fixed4 _OrangeColor;
            fixed4 _CrimsonColor;
            fixed4 _CarbonColor;

            float _GridScale;
            float _GridSpeed;
            float _BloomIntensity;
            float _ScanlineSpeed;

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

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float time = _Time.y;

                fixed4 mainTexCol = tex2D(_MainTex, uv);

                // Base Deep Matrix Charcoal background #0A0E17 multiplied by vertex color tint
                fixed4 col;
                col.rgb = _DarkBgColor.rgb * IN.color.rgb;

                // If mainTex contains an actual custom texture image (non-white sprite), blend it softly
                float isCustomTex = step(0.01, abs(mainTexCol.r - mainTexCol.g) + abs(mainTexCol.g - mainTexCol.b));
                col.rgb = lerp(col.rgb, mainTexCol.rgb * IN.color.rgb, isCustomTex * 0.4);

                // Central radial bloom
                float2 centerUV = uv - float2(0.5, 0.5);
                float dist = length(centerUV);
                float radialBloom = exp(-dist * 2.8) * _BloomIntensity;

                // Dual-tone chromatic gradient (Cyan on left, Orange on right)
                float leftCyan = smoothstep(0.7, 0.0, uv.x + centerUV.y * 0.3);
                float rightOrange = smoothstep(0.3, 1.0, uv.x - centerUV.y * 0.3);
                float crimsonCore = smoothstep(0.3, 0.0, dist);

                fixed3 bloomColor = lerp(_CyanColor.rgb, _OrangeColor.rgb, rightOrange);
                bloomColor = lerp(bloomColor, _CrimsonColor.rgb, crimsonCore * 0.6);

                col.rgb += bloomColor * radialBloom * 0.35;

                // Binary Matrix / Grid lines
                float2 gridUV = uv * _GridScale;
                float2 fw = max(fwidth(gridUV), float2(0.0001, 0.0001));
                float2 gridLines = abs(frac(gridUV - 0.5) - 0.5) / fw;
                float lineVal = min(gridLines.x, gridLines.y);
                float gridPattern = 1.0 - saturate(min(lineVal, 1.0));

                // Animated scanlines
                float scanline = sin((uv.y * 120.0) + (time * _ScanlineSpeed)) * 0.5 + 0.5;
                scanline = pow(scanline, 3.0) * 0.1;

                // Matrix Nodes & Sparkle
                float2 cell = floor(gridUV + float2(0.0, time * _GridSpeed));
                float randVal = hash(cell);
                float nodeSparkle = step(0.92, randVal) * (sin(time * 5.0 + randVal * 10.0) * 0.5 + 0.5);

                // Combine grid & node glow
                fixed3 gridGlow = lerp(_CyanColor.rgb, _OrangeColor.rgb, step(0.5, uv.x)) * (gridPattern * 0.15 + nodeSparkle * 0.5);
                col.rgb += gridGlow + scanline * _CyanColor.rgb;

                col.a = mainTexCol.a * IN.color.a;

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
