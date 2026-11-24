Shader "Photoshop/Overlay"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,0.35)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color;
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 col = tex * _Color;
                if ((i.color.r + i.color.g + i.color.b) > 0.02) col.rgb *= i.color.rgb;
                col.a *= i.color.a * 0.35;
                return col;
            }
            ENDCG
        }
    }
}
