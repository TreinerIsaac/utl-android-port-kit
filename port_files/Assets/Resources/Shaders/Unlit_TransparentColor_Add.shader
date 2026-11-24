Shader "Unlit/TransparentColor_Add"
{
    Properties
    {
        _TextureSample0 ("TextureSample0", 2D) = "white" {}
        _MainTex ("MainTex", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _FillColor ("FillColor", Color) = (0,0,0,0)
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
            sampler2D _TextureSample0; float4 _TextureSample0_ST;
            sampler2D _MainTex;
            fixed4 _Color; fixed4 _FillColor;
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _TextureSample0);
                o.color = v.color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                fixed4 tex = tex2D(_TextureSample0, i.uv) * tex2D(_MainTex, i.uv);
                fixed4 col = tex * _Color;
                col.rgb = lerp(col.rgb, _FillColor.rgb, _FillColor.a);
                col.a *= i.color.a;
                return col;
            }
            ENDCG
        }
    }
}
