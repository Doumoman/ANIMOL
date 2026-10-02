Shader "Hidden/ANIMOL/FantasyBackgroundV6"
{
    Properties
    {
        _MainTex ("Current native frame", 2D) = "white" {}
        _Previous ("Previous native frame", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _Far, _Mid, _Platform, _Rabbit, _Near, _MainTex, _Previous;
        float4 _FarRect, _MidRect, _NearRect;
        float _RabbitX, _Frame, _Threshold;
        float4 SamplePlane(sampler2D tex,float2 pixel,float4 rect)
        {
            float2 uv=(pixel-rect.xy)/rect.zw;
            if(any(uv<0)||any(uv>=1)) return 0;
            // Resolve to a source texel center explicitly. Sampling on a texel edge
            // lets GPU rounding choose the adjacent row at exact scale ratios.
            float2 source=floor((pixel-rect.xy)*float2(352,704)/rect.zw+0.00001);
            uv=(source+.5)/float2(352,704);
            return tex2D(tex,float2(uv.x,1-uv.y));
        }
        float4 Over(float4 a,float4 b) { return b.a>.5 ? float4(b.rgb,1) : a; }
        float4 Scene(v2f_img i):SV_Target
        {
            float2 p=floor(float2(i.uv.x,1-i.uv.y)*float2(352,704))+.5;
            float3 baseColor=float3(26,28,44)/255.0;
            #ifndef UNITY_COLORSPACE_GAMMA
            baseColor=float3(GammaToLinearSpaceExact(baseColor.r),GammaToLinearSpaceExact(baseColor.g),GammaToLinearSpaceExact(baseColor.b));
            #endif
            float4 c=float4(baseColor,1);
            c=Over(c,SamplePlane(_Far,p,_FarRect));
            c=Over(c,SamplePlane(_Mid,p,_MidRect));
            c=Over(c,SamplePlane(_Platform,p,float4(0,0,352,704)));
            float2 r=p-float2(_RabbitX,436);
            if(all(r>=0)&&r.x<64&&r.y<96)
                c=Over(c,tex2D(_Rabbit,float2((r.x+_Frame*64)/512,1-r.y/96)));
            return Over(c,SamplePlane(_Near,p,_NearRect));
        }
        float4 Wipe(v2f_img i):SV_Target
        {
            float2 p=floor(float2(i.uv.x,1-i.uv.y)*float2(352,704)/8)*8;
            float d=.5*p.x/352+.5*p.y/704;
            return d<_Threshold ? tex2D(_MainTex,i.uv) : tex2D(_Previous,i.uv);
        }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Scene
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Wipe
            ENDCG
        }
    }
}
