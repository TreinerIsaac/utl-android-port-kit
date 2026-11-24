Shader "Unlit/Unlit Texture Tint"
{
    Properties
    {
        _Texture ("Texture", 2D) = "white" {}
        _MainTex ("MainTex", 2D) = "white" {}
        _TextureSample0 ("TextureSample0", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _TintColor ("TintColor", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Cull Back
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 uv2 : TEXCOORD1; };
            sampler2D _Texture; float4 _Texture_ST;
            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _TextureSample0;
            fixed4 _Color; fixed4 _TintColor;
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _Texture);
                o.uv2 = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                fixed4 t1 = tex2D(_Texture, i.uv);
                fixed4 t2 = tex2D(_MainTex, i.uv2);
                fixed4 t3 = tex2D(_TextureSample0, i.uv);
                fixed3 rgb = t1.rgb * t2.rgb * t3.rgb;
                fixed3 tint = ((_Color.r + _Color.g + _Color.b) > 0.05) ? _Color.rgb : fixed3(1,1,1);
                return fixed4(rgb * tint, 1.0);
            }
            ENDCG
        }
    }
}
