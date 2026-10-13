Shader "ANIMOL/Source Art Sweetie16"
{
    Properties
    {
        [PerRendererData] _MainTex ("Source Art", 2D) = "white" {}
        _AlphaCutoff ("Alpha cutoff", Range(0,1)) = 0.85
        _HasExclude ("Source neighbour exclusion", Float) = 0
        _ExcludeRect ("Excluded source UV rectangle", Vector) = (0,0,0,0)
        _PreviewOpacity ("Preview opacity after source cutoff", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _AlphaCutoff;
            float _HasExclude;
            float4 _ExcludeRect;
            float _PreviewOpacity;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            float3 palette(int i)
            {
                float3 p;
                if(i==0) p=float3(26,28,44);
                else if(i==1) p=float3(93,39,93);
                else if(i==2) p=float3(177,62,83);
                else if(i==3) p=float3(239,125,87);
                else if(i==4) p=float3(255,205,117);
                else if(i==5) p=float3(167,240,112);
                else if(i==6) p=float3(56,183,100);
                else if(i==7) p=float3(37,113,121);
                else if(i==8) p=float3(41,54,111);
                else if(i==9) p=float3(59,93,201);
                else if(i==10) p=float3(65,166,246);
                else if(i==11) p=float3(115,239,247);
                else if(i==12) p=float3(244,244,244);
                else if(i==13) p=float3(148,176,194);
                else if(i==14) p=float3(86,108,134);
                else p=float3(51,60,87);
                p/=255.0;
                #ifndef UNITY_COLORSPACE_GAMMA
                    p=GammaToLinearSpace(p);
                #endif
                return p;
            }
            float4 frag(v2f i):SV_Target
            {
                if(_HasExclude>0.5 && i.uv.x>=_ExcludeRect.x && i.uv.x<=_ExcludeRect.z && i.uv.y>=_ExcludeRect.y && i.uv.y<=_ExcludeRect.w) discard;
                float4 source=tex2D(_MainTex,i.uv)*i.color;
                clip(source.a-_AlphaCutoff);
                float best=1e10; float3 output=palette(0);
                [unroll] for(int n=0;n<16;n++)
                {
                    float3 p=palette(n); float3 delta=source.rgb-p; float d=dot(delta,delta);
                    if(d<best) { best=d; output=p; }
                }
                return float4(output * _PreviewOpacity, _PreviewOpacity);
            }
            ENDCG
        }
    }
    Fallback Off
}
