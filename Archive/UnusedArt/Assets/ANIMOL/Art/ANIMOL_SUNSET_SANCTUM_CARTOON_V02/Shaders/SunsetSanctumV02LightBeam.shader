Shader "ANIMOL/SunsetSanctumV02/LightBeam"
{
    Properties
    {
        _Outline("Navy edge",Color)=(.035,.055,.13,1)
        _LightColor("Flat cyan beam",Color)=(.15,.85,.95,1)
        _CoreColor("Cream core",Color)=(1,.97,.78,1)
        _PulseColor("Committed action pulse",Color)=(.65,1,1,1)
        [HideInInspector]_PathLength("Path length",Float)=1
        [HideInInspector]_PulseDistance("Host pulse",Float)=0
        [HideInInspector]_Active("Committed action",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            // No LightMode tag: generic unlit/SRPDefaultUnlit. Unity pipeline compile NOT_RUN.
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
            fixed4 _Outline,_LightColor,_CoreColor,_PulseColor;
            float _PathLength,_PulseDistance,_Active;
            v2f vert(appdata v) {v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float side=abs(i.uv.y*2-1);
                if(side>.82 || i.uv.x<.012 || _PathLength-i.uv.x<.012)return _Outline;
                if(_Active>.5 && abs(i.uv.x-_PulseDistance)<.18)return _PulseColor;
                return side>.4?_LightColor:_CoreColor;
            }
            ENDCG
        }
    }
    Fallback Off
}
