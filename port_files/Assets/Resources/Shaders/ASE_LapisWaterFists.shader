Shader "ASE/LapisWaterFists"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _TintColor ("TintColor", Color) = (0.5,0.9,1,1)
        _WaveDropColor ("WaveDropColor", Color) = (0.75,0.98,1,1)
        _FillColor ("FillColor", Color) = (1,1,1,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TintColor;
            fixed4 _WaveDropColor;
            fixed4 _FillColor;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed4 t = tex2D(_MainTex, i.uv);
                fixed3 rgb = lerp(t.rgb * _TintColor.rgb * _WaveDropColor.rgb, _FillColor.rgb, saturate(_FillColor.a));
                return fixed4(rgb * i.color.rgb, t.a * _TintColor.a * i.color.a);
            }
            ENDCG
        }
    }
}
