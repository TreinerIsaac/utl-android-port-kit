Shader "Unlit/Unlit_Texture_Fill_Tint"
{
    Properties
    {
        _node_104 ("Texture", 2D) = "white" {}
        _MainTex ("MainTex", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _TintColor ("TintColor", Color) = (1,1,1,1)
        _node_693 ("FillAmount", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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

            sampler2D _node_104;
            float4 _node_104_ST;
            fixed4 _Color;
            fixed4 _TintColor;
            float _node_693;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _node_104);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_node_104, i.uv);
                fixed3 baseCol = tex.rgb * i.color.rgb * _Color.rgb;
                fixed3 rgb = lerp(baseCol, _TintColor.rgb, saturate(_node_693));
                return fixed4(rgb, tex.a * i.color.a * _Color.a);
            }
            ENDCG
        }
    }
}
