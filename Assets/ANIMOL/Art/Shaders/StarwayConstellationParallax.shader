Shader "ANIMOL/UI/StarwayConstellationParallax"
{
    Properties
    {
        [PerRendererData] _MainTex ("Alpha mask (white for full panel)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cyan ("Far / near star color", Color) = (0.40,0.91,0.98,1)
        _Violet ("Middle star color", Color) = (0.72,0.62,0.98,1)
        _Aspect ("Panel width / height", Float) = 0.5625
        _Progress ("Parallax progress", Range(0,1)) = 0
        _Drift ("View shift (XY, panel height units)", Vector) = (0.05,-0.08,0,0)
        _RefractionPx ("Edge displacement at 1920 reference height", Range(0,20)) = 14
        _SealOpen ("Return blend (legacy property name)", Range(0,1)) = 0
        _Visibility ("Overlay visibility", Range(0,1)) = 1
        _ReducedMotion ("Reduced motion", Range(0,1)) = 0
        _LayerWeights ("Far / middle / near weights (XYZ)", Vector) = (1,1,1,0)

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector][Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "ConstellationUI"
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 position : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 softMask : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            half4 _TextureSampleAdd;
            half4 _Color, _Cyan, _Violet;
            float4 _ClipRect, _Drift, _LayerWeights;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;
            float _Aspect, _Progress, _RefractionPx, _SealOpen, _Visibility, _ReducedMotion;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv;
                output.color = input.color * _Color;
                output.softMask = float4(0.0, 0.0, 0.0, 0.0);
                #ifdef UNITY_UI_CLIP_RECT
                float4 rect = clamp(_ClipRect, -2e10, 2e10);
                float2 projection = abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float2 pixelSize = abs(output.position.w) / max(projection, float2(1e-4, 1e-4));
                float2 softness = float2(_UIMaskSoftnessX, _UIMaskSoftnessY);
                output.softMask = float4(input.position.xy * 2.0 - rect.xy - rect.zw,
                    0.25 / max(0.25 * softness + pixelSize, float2(1e-4, 1e-4)));
                #endif
                return output;
            }

            float DistanceToSegment(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                return length(p - a - ab * saturate(dot(p - a, ab) / max(dot(ab, ab), 1e-6)));
            }

            float FeatheredShape(float distanceToShape, float radius, float aa)
            {
                return 1.0 - smoothstep(max(0.0, radius - aa), radius + aa, distanceToShape);
            }

            float Chain(float2 p, float2 a, float2 b, float2 c, float2 d, float2 e,
                        float lineSize, float nodeSize, float aa)
            {
                float lineDistance = min(min(DistanceToSegment(p, a, b), DistanceToSegment(p, b, c)),
                                         min(DistanceToSegment(p, c, d), DistanceToSegment(p, d, e)));
                float2 da = abs(p - a);
                float2 db = abs(p - b);
                float2 dc = abs(p - c);
                float2 dd = abs(p - d);
                float2 de = abs(p - e);
                float nodeDistance = min(min(da.x + da.y, db.x + db.y),
                                         min(dc.x + dc.y, min(dd.x + dd.y, de.x + de.y)));
                return max(0.47 * FeatheredShape(lineDistance, lineSize, aa),
                           FeatheredShape(nodeDistance, nodeSize, aa));
            }

            float Hash(float2 p)
            {
                float3 a = frac(float3(p.xyx) * 0.1031);
                a += dot(a, a.yzx + 33.33);
                return frac((a.x + a.y) * a.z);
            }

            float SparseStars(float2 p, float aa)
            {
                float2 grid = p * 9.0;
                float2 cell = floor(grid);
                float seed = Hash(cell);
                float2 center = 0.2 + 0.6 * float2(Hash(cell + 4.32), Hash(cell + 12.65));
                float distanceToDot = length(frac(grid) - center) / 9.0;
                return step(0.58, seed) * FeatheredShape(distanceToDot, lerp(0.0008, 0.0024, seed), aa);
            }

            half4 Over(half4 back, half4 color, float alpha)
            {
                half a = saturate(alpha * color.a);
                return half4(color.rgb * a, a) + back * (1.0 - a);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 centered = input.uv - 0.5;
                float aspect = max(_Aspect, 0.01);
                float2 p = centered * float2(aspect, 1.0);
                float aa = max(0.00025, 0.62 * max(length(ddx(p)), length(ddy(p))));
                float motion = saturate(_Progress) * (1.0 - saturate(_ReducedMotion));
                float opening = saturate(_SealOpen);
                float edge = smoothstep(0.14, 0.70, length(centered));
                float2 direction = centered / max(length(centered), 1e-3);
                float2 bend = direction * edge * min(20.0, max(0.0, _RefractionPx))
                    * (motion * (1.0 - opening) / 1920.0);
                float2 farP  = (p - _Drift.xy * (0.12 * motion) + bend * 0.18) / (1.0 + 0.02 * motion);
                float2 midP  = (p - _Drift.xy * (0.38 * motion) + bend * 0.52) / (1.0 + 0.12 * motion);
                float2 nearP = (p - _Drift.xy * (1.00 * motion) + bend) / (1.0 + 0.28 * motion);

                float farStars = max(0.42 * SparseStars(farP, aa), Chain(farP,
                    float2(-0.24,0.22), float2(-0.18,0.31), float2(-0.09,0.27),
                    float2(-0.02,0.36), float2(0.09,0.32), 0.00075, 0.0037, aa));
                float midStars = Chain(midP, float2(0.07,0.13), float2(0.17,0.21),
                    float2(0.24,0.12), float2(0.17,0.03), float2(0.24,-0.08), 0.0010, 0.0048, aa);
                float nearStars = Chain(nearP, float2(-0.23,-0.28), float2(-0.14,-0.16),
                    float2(-0.06,-0.19), float2(0.03,-0.32), float2(0.15,-0.25), 0.0014, 0.0062, aa);

                half4 color = half4(0,0,0,0);
                color = Over(color, _Cyan, farStars * saturate(_LayerWeights.x) * 0.48);
                color = Over(color, _Violet, midStars * saturate(_LayerWeights.y) * 0.70);
                color = Over(color, _Cyan, nearStars * saturate(_LayerWeights.z) * 0.88);

                // Keep the legacy property binding, but return the entire spatial field
                // without a centered aperture that resembles an opening prison.
                float returnAlpha = 1.0 - opening;
                float alphaMask = saturate((tex2D(_MainTex, input.uv) + _TextureSampleAdd).a);
                float uiFactor = saturate(input.color.a) * alphaMask * saturate(_Visibility) * returnAlpha;
                #ifdef UNITY_UI_CLIP_RECT
                float4 rect = clamp(_ClipRect, -2e10, 2e10);
                float2 mask = saturate((rect.zw - rect.xy - abs(input.softMask.xy)) * input.softMask.zw);
                uiFactor *= mask.x * mask.y;
                #endif
                color *= uiFactor;
                color.rgb *= input.color.rgb;
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
