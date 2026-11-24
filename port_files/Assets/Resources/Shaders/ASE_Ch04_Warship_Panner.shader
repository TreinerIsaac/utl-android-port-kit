Shader "ASE/Ch04_Warship_Panner"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _TextureSample0 ("TextureSample0", 2D) = "white" {}
        _Color ("Color", Color) = (0.28, 0.22, 0.46, 1)
        _Color0 ("Color 0", Color) = (0.28, 0.22, 0.46, 1)
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
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed shade : TEXCOORD1; };
            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _TextureSample0;
            fixed4 _Color; fixed4 _Color0;
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex) + float2(0, _Time.y * 0.25);
                float3 wn = UnityObjectToWorldNormal(v.normal);
                o.shade = lerp(0.75, 1.08, saturate(wn.y * 0.5 + 0.5));
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                fixed4 tex = tex2D(_MainTex, i.uv) * tex2D(_TextureSample0, i.uv);
                fixed3 tint = ((_Color.r + _Color.g + _Color.b) > 0.05 && (_Color.r + _Color.g + _Color.b) < 2.8)
                    ? _Color.rgb
                    : (((_Color0.r + _Color0.g + _Color0.b) > 0.05 && (_Color0.r + _Color0.g + _Color0.b) < 2.8)
                        ? _Color0.rgb : fixed3(0.28, 0.20, 0.46));
                return fixed4(tex.rgb * tint * i.shade, 1.0);
            }
            ENDCG
        }
    }
}
