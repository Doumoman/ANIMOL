Shader "ANIMOL/QA/MoonAtlasPalette"
{
    Properties { [PerRendererData] _MainTex ("Sprite", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 position:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex; fixed4 _Color;
            v2f vert(appdata v) { v2f o; o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 src=tex2D(_MainTex,i.uv); float value=max(src.r,max(src.g,src.b));
                float3 color;
                if(value<.065) color=float3(.06,.11,.17);
                else if(value<.22) color=float3(.14,.25,.33);
                else if(src.r>src.g*1.3 && src.r>src.b*1.2) color=float3(.72,.53,.28);
                else if(value>.72) color=float3(.78,.9,.80);
                else if(src.b>src.g*1.25) color=float3(.25,.40,.49);
                else color=float3(.36,.66,.61);
                #ifndef UNITY_COLORSPACE_GAMMA
                color=GammaToLinearSpace(color);
                #endif
                return fixed4(color*i.color.rgb,src.a*i.color.a);
            }
            ENDCG
        }
    }
}
