Shader "ASE/Additive_Transparent"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _TextureSample0 ("TextureSample0", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Color0 ("Color0", Color) = (1,1,1,1)
        _TintColor ("TintColor", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _TextureSample0;
            float4 _TextureSample0_ST;
            fixed4 _Color;
            fixed4 _TintColor;
            fixed _Opacity;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv0 : TEXCOORD1;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv0 = TRANSFORM_TEX(v.uv, _TextureSample0);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv) * tex2D(_TextureSample0, i.uv0) * _Color * _TintColor * i.color;
                col.a *= _Opacity;
                return col;
            }
            ENDCG
        }
    }
}
