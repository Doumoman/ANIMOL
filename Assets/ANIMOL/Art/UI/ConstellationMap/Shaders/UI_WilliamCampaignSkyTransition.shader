Shader "UI/WilliamCampaignSkyTransition"
{
    Properties
    {
        [PerRendererData] _MainTex ("UI Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SourceTex ("Preserved Main Illustration", 2D) = "black" {}
        _DestinationTex ("Preserved Night Sky", 2D) = "black" {}
        _SourceUvRect ("Source UV xywh", Vector) = (0,0,1,1)
        _DestinationUvRect ("Destination UV xywh", Vector) = (0,0,1,1)
        _SourceAspect ("Source Aspect", Float) = 0.666667
        _DestinationAspect ("Destination Aspect", Float) = 0.666667
        _ViewAspect ("Physical View Aspect", Float) = 0.5625
        _Progress ("Sequence Progress", Range(0,1)) = 0
        _Link0 ("Entry Link 0", Vector) = (-0.42,0.15,-0.25,0.34)
        _Link1 ("Entry Link 1", Vector) = (-0.25,0.34,-0.05,0.25)
        _Link2 ("Entry Link 2", Vector) = (-0.05,0.25,0.13,0.42)
        _Link3 ("Entry Link 3", Vector) = (0.13,0.42,0.32,0.31)
        _Link4 ("Entry Link 4", Vector) = (-0.24,-0.35,0.02,-0.19)
        _Link5 ("Entry Link 5", Vector) = (0.02,-0.19,0.23,-0.3)
        _Link6 ("Entry Link 6", Vector) = (0.23,-0.3,0.42,-0.05)
        _Link7 ("Entry Link 7", Vector) = (3.97,0.49,-0.51,0.4)
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            Name "CampaignSky"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 worldPosition:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _SourceTex, _DestinationTex;
            float4 _SourceUvRect, _DestinationUvRect, _ClipRect;
            float4 _Link0, _Link1, _Link2, _Link3, _Link4, _Link5, _Link6, _Link7;
            fixed4 _Color;
            float _Progress, _SourceAspect, _DestinationAspect, _ViewAspect;
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            float outCubic(float x) { x=saturate(x); return 1-(1-x)*(1-x)*(1-x); }
            float2 coverUv(float2 uv, float textureAspect)
            {
                float aspect=max(.01,_ViewAspect), image=max(.01,textureAspect);
                float2 scale=float2(min(1,aspect/image),min(1,image/aspect));
                return (uv-.5)*scale+.5;
            }
            // Preserved authored stars. A new set of short links is supplied once per entry.
            float2 starPoint(int i)
            {
                if(i==0)return float2(-.42,.15); if(i==1)return float2(-.25,.34); if(i==2)return float2(-.05,.25);
                if(i==3)return float2(.13,.42); if(i==4)return float2(.32,.31); if(i==5)return float2(-.24,-.35);
                if(i==6)return float2(.02,-.19); if(i==7)return float2(.23,-.30); if(i==8)return float2(.42,-.05);
                if(i==9)return float2(-.48,.62); if(i==10)return float2(.12,.70); if(i==11)return float2(.44,.52);
                if(i==12)return float2(-.34,-.58); if(i==13)return float2(.39,-.55); if(i==14)return float2(-.08,.06);
                if(i==15)return float2(.27,.03); if(i==16)return float2(-.50,-.06); if(i==17)return float2(.03,-.73);
                if(i==18)return float2(-.18,.78); if(i==19)return float2(.52,.82); if(i==20)return float2(-.48,-.80);
                if(i==21)return float2(.49,-.86); if(i==22)return float2(-.03,.49); if(i==23)return float2(.53,.13);
                if(i==24)return float2(-.51,.40); if(i==25)return float2(.04,-.47); return float2(.26,.91);
            }
            float depth(int i) { return .90+.46*floor((float)i/9); }
            float2 projectPoint(int i, float travel, float gaze)
            {
                float z=max(.38,depth(i)-travel*.43);
                float2 q=starPoint(i)*float2(max(.01,_ViewAspect),1)/z;
                q.y+=.12-.15*gaze; return q;
            }
            float segment(float2 p,float2 a,float2 b)
            {
                float2 ab=b-a; float t=saturate(dot(p-a,ab)/max(dot(ab,ab),.000001)); return length(p-a-ab*t);
            }
            float link(float2 p,float4 edge,float travel,float gaze,float aa)
            {
                // CPU stores the common depth layer in the first x coordinate (x + 2*layer).
                // Decode it without per-pixel indexed star lookups or additional uniforms.
                float layer=floor((edge.x+1)*.5), z=max(.38,.90+.46*layer-travel*.43);
                float2 scale=float2(max(.01,_ViewAspect),1)/z;
                float2 a=float2(edge.x-2*layer,edge.y)*scale, b=edge.zw*scale;
                a.y+=.12-.15*gaze; b.y+=.12-.15*gaze;
                return 1-smoothstep(.00065,.00065+aa,segment(p,a,b));
            }
            fixed4 frag(v2f i):SV_Target
            {
                float p=saturate(_Progress), gaze=outCubic(p/.68), travel=smoothstep(.06,.73,p);
                float sourceZoom=lerp(1,1.65,gaze);
                float sourcePan=min(.15*gaze,.4*(1-1/sourceZoom));
                float2 sourceUv=coverUv((i.uv-.5)/sourceZoom+.5+float2(0,sourcePan),_SourceAspect);
                float settle=outCubic(saturate((p-.40)/.40));
                float destinationZoom=lerp(1.26,1,settle);
                float destinationPan=min(.065*(1-settle),.4*(1-1/destinationZoom));
                float2 destinationUv=coverUv((i.uv-.5)/destinationZoom+.5+float2(0,destinationPan),_DestinationAspect);
                sourceUv=_SourceUvRect.xy+saturate(sourceUv)*_SourceUvRect.zw;
                destinationUv=_DestinationUvRect.xy+saturate(destinationUv)*_DestinationUvRect.zw;
                float3 source=tex2D(_SourceTex,sourceUv).rgb;
                float3 destination=tex2D(_DestinationTex,destinationUv).rgb;
                float3 color=lerp(source,destination,smoothstep(.16,.55,p));
                float envelope=smoothstep(.04,.18,p)*(1-smoothstep(.66,.90,p));
                float2 view=(i.uv-.5)*float2(max(.01,_ViewAspect),1);
                float aa=1.15/max(1,_ScreenParams.y);
                float lines=0;
                lines+=link(view,_Link0,travel,gaze,aa); lines+=link(view,_Link1,travel,gaze,aa);
                lines+=link(view,_Link2,travel,gaze,aa); lines+=link(view,_Link3,travel,gaze,aa);
                lines+=link(view,_Link4,travel,gaze,aa); lines+=link(view,_Link5,travel,gaze,aa);
                lines+=link(view,_Link6,travel,gaze,aa); lines+=link(view,_Link7,travel,gaze,aa);
                color=lerp(color,float3(.74,.77,.94),saturate(lines)*envelope*.46);
                [unroll] for(int n=0;n<27;n++)
                {
                    float z=max(.38,depth(n)-travel*.43);
                    float radius=(.0047+.0008*fmod((float)n,3))/z;
                    float2 delta=abs(view-projectPoint(n,travel,gaze));
                    float2 normalized=delta/float2(max(radius,.0001),max(radius*1.22,.0001));
                    float shape=max(normalized.x,normalized.y)+3*min(normalized.x,normalized.y);
                    float edge=aa/max(radius,.0001);
                    float core=1-smoothstep(1-edge,1+edge,shape);
                    float halo=1-smoothstep(.70,1.75,shape);
                    float star=core*.96+halo*.12;
                    float3 tint=n<9?float3(1,.94,.80):float3(.85,.87,1);
                    color=lerp(color,tint,saturate(star)*envelope);
                }
                float alpha=smoothstep(0,.16,p)*(1-smoothstep(.78,1,p))*i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                alpha*=UnityGet2DClipping(i.worldPosition.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha-.001);
                #endif
                return fixed4(saturate(color)*i.color.rgb,alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
