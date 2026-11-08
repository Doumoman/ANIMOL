Shader "UI/ANIMOL/CrayonFrameBoil"
{
    Properties
    {
        [PerRendererData] _MainTex ("Prepainted Frame A (UI Sprite)", 2D) = "white" {}
        [NoScaleOffset] _FrameB ("Prepainted Frame B", 2D) = "white" {}
        [NoScaleOffset] _FrameC ("Prepainted Frame C", 2D) = "white" {}
        _Color ("UI Tint", Color) = (1,1,1,1)
        _FrameRate ("Frame Changes Per Second", Float) = 1.666667
        _Motion ("Line Boil Enabled", Float) = 1

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
            "CanUseSpriteAtlas" = "False"
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
            Name "PrepaintedCrayonUI"
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
            sampler2D _FrameB;
            sampler2D _FrameC;
            fixed4 _TextureSampleAdd;
            fixed4 _Color;
            float _FrameRate;
            float _Motion;
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
                // All three PNGs share dimensions, FullRect UVs and 9-slice borders.
                // The shader selects prepainted pixels; it never draws or randomizes a line.
                float frame = 0;
                if (_Motion > 0.5 && _FrameRate > 0)
                    frame = floor(fmod(_Time.y * _FrameRate, 3.0));

                fixed4 sampled;
                if (frame < 0.5)
                    sampled = tex2D(_MainTex, i.uv);
                else if (frame < 1.5)
                    sampled = tex2D(_FrameB, i.uv);
                else
                    sampled = tex2D(_FrameC, i.uv);

                fixed4 color = (sampled + _TextureSampleAdd) * i.color;

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
