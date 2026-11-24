Shader "ASE/RosesRoom_DoorGlow"
{
    Properties
    {
        _TextureSample0 ("TextureSample0", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _TextureSample0;
            float4 _TextureSample0_ST;
            fixed4 _Color;
            float _Opacity;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _TextureSample0);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_TextureSample0, i.uv);
                fixed op = saturate(_Opacity);
                fixed3 rgb = tex.rgb * _Color.rgb * i.color.rgb;
                fixed a = tex.a * _Color.a * i.color.a * op;
                return fixed4(rgb, a);
            }
            ENDCG
        }
    }
}
