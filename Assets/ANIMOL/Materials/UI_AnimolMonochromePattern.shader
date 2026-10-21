Shader "UI/ANIMOL/Monochrome Diagonal Pattern"
{
    Properties
    {
        [PerRendererData] _MainTex ("Pattern Texture", 2D) = "white" {}
        _Color ("UI Tint", Color) = (1,1,1,1)
        _BackgroundColor ("Background Color", Color) = (0.97,0.97,0.99,1)
        _PatternTint ("Pattern Tint", Color) = (1,1,1,1)
        _PatternOpacity ("Pattern Opacity", Range(0,1)) = 0.20
        _PatternScale ("Pattern Repeats XY", Vector) = (4,7,0,0)
        _PatternOffset ("Pattern Offset XY", Vector) = (0,0,0,0)
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
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="False"
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
            Name "DiagonalPatternUI"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 mask : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            fixed4 _Color;
            fixed4 _BackgroundColor;
            fixed4 _PatternTint;
            float _PatternOpacity;
            float4 _PatternScale;
            float4 _PatternOffset;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.uv = v.uv;
                o.mask = 0;

                #ifdef UNITY_UI_CLIP_RECT
                float2 pixelSize = o.vertex.w;
                pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 clipRect = clamp(_ClipRect, -2e10, 2e10);
                o.mask = float4(v.vertex.xy * 2 - clipRect.xy - clipRect.zw,
                    0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY)
                        + abs(pixelSize)));
                #endif
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Positive offset moves the VISIBLE pattern right/up.
                // Repeat is handled by the texture sampler, including bilinear seams.
                // Do not apply frac() here: it disrupts texture derivatives for mipmaps.
                float2 patternUV = i.uv * _PatternScale.xy - _PatternOffset.xy;
                half coverage = saturate(tex2D(_MainTex, patternUV).a + _TextureSampleAdd.a);
                half4 pattern = half4(1, 1, 1, coverage) * _PatternTint;
                half patternAlpha = saturate(pattern.a * _PatternOpacity);

                // Straight-alpha output, also correct when background alpha is zero.
                half outputAlpha = patternAlpha + _BackgroundColor.a * (1 - patternAlpha);
                half3 premultiplied = pattern.rgb * patternAlpha
                    + _BackgroundColor.rgb * _BackgroundColor.a * (1 - patternAlpha);
                half4 color = half4(premultiplied / max(outputAlpha, 0.0001), outputAlpha);
                color *= i.color;

                #ifdef UNITY_UI_CLIP_RECT
                half2 mask = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
                color.a *= mask.x * mask.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
