Shader "ANIMOL/DevicePatch01/VividWindRoute"
{
    Properties
    {
        _Outline ("Navy Outline", Color) = (0.035, 0.045, 0.13, 1)
        _IdleColor ("Idle Range Teal", Color) = (0.02, 0.59, 0.64, 0.86)
        _ActiveColor ("Active Cyan", Color) = (0.02, 0.88, 0.92, 0.96)
        _ArrowColor ("Cream Chevrons", Color) = (1, 0.96, 0.70, 1)
        _EndColor ("Gold Range End", Color) = (1, 0.70, 0.12, 1)
        [HideInInspector] _PathLength ("Path Length", Float) = 1
        [HideInInspector] _FlowOffset ("Host Flow Offset", Float) = 0
        [HideInInspector] _Active ("Committed Action Active", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            // Deliberately no LightMode tag: the generic unlit pass can be selected by
            // Built-in and URP's SRPDefaultUnlit path. No external package include.
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            fixed4 _Outline, _IdleColor, _ActiveColor, _ArrowColor, _EndColor;
            float _PathLength, _FlowOffset, _Active;
            v2f vert(appdata v)
            {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float across = i.uv.y * 2.0 - 1.0;
                float side = abs(across);
                float remaining = _PathLength - i.uv.x;
                // Crisp full-width cap makes the exact final edge readable at rest.
                if (side > 0.84 || remaining < 0.045 || i.uv.x < 0.045) return _Outline;
                if (remaining < 0.16) return _EndColor;
                fixed4 fill = _Active > 0.5 ? _ActiveColor : _IdleColor;
                float offset = _Active > 0.5 ? _FlowOffset : 0.0;
                float phase = frac((i.uv.x - offset) / 0.70) * 0.70;
                float chevron = 0.45 - side * 0.23;
                if (side < 0.72 && abs(phase - chevron) < 0.055) return _ArrowColor;
                return fill;
            }
            ENDCG
        }
    }
    Fallback Off
}
