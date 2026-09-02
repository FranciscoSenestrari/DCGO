Shader "UI/DCGO/HolographicGlassShield"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Shield Tint", Color) = (0.0, 0.823, 1.0, 0.3) // #00D2FF @ 30% opacity default
        _BorderColor ("Emissive Border Color", Color) = (0.0, 0.823, 1.0, 1.0)
        _CarbonBgColor ("Carbon Grey Overlay", Color) = (0.164, 0.180, 0.239, 0.5)

        _GlassRefraction ("Glass Refraction Intensity", Float) = 0.35
        _BorderWidth ("Border Width", Float) = 0.05
        _ScanSpeed ("Code Stream Speed", Float) = 1.0

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
            fixed4 _BorderColor;
            fixed4 _CarbonBgColor;
            float _GlassRefraction;
            float _BorderWidth;
            float _ScanSpeed;

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
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float time = _Time.y;

                fixed4 mainTexCol = tex2D(_MainTex, uv);
                fixed4 col = _Color * IN.color;

                // Glass diagonal glare / refraction sheen
                float glare = sin((uv.x + uv.y) * 8.0 - time * 2.0) * 0.5 + 0.5;
                glare = pow(max(0.0, glare), 4.0) * _GlassRefraction;
                col.rgb += fixed3(glare, glare, glare);

                // Code stream scanline overlay
                float codeLine = sin(uv.y * 80.0 + time * _ScanSpeed * 10.0) * 0.5 + 0.5;
                col.rgb += _BorderColor.rgb * (codeLine * 0.08);

                // Emissive High-Contrast Border calculation
                float distToEdgeX = min(uv.x, 1.0 - uv.x);
                float distToEdgeY = min(uv.y, 1.0 - uv.y);
                float borderFactor = smoothstep(_BorderWidth, 0.0, min(distToEdgeX, distToEdgeY));

                // Border glow
                fixed3 borderGlow = _BorderColor.rgb * 1.5;
                col.rgb = lerp(col.rgb, borderGlow, borderFactor);
                col.a = max(col.a, borderFactor * 0.95) * mainTexCol.a * IN.color.a;

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
