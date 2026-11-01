Shader "ANIMOL/DiceReceiverClipV04"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ClipRect ("World Clip Rect", Vector) = (-10000,-10000,10000,10000)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Lighting Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct Varying { float4 position : SV_POSITION; float2 uv : TEXCOORD0; float2 world : TEXCOORD1; fixed4 color : COLOR; };
            sampler2D _MainTex; fixed4 _Color; float4 _ClipRect;
            Varying vert(Input v)
            {
                Varying o; o.position = UnityObjectToClipPos(v.vertex); o.uv = v.uv;
                o.world = mul(unity_ObjectToWorld, v.vertex).xy; o.color = v.color * _Color; return o;
            }
            fixed4 frag(Varying v) : SV_Target
            {
                clip(v.world.x - _ClipRect.x); clip(v.world.y - _ClipRect.y);
                clip(_ClipRect.z - v.world.x); clip(_ClipRect.w - v.world.y);
                fixed4 color = tex2D(_MainTex, v.uv) * v.color;
                color.rgb *= color.a; return color;
            }
            ENDCG
        }
    }
}
