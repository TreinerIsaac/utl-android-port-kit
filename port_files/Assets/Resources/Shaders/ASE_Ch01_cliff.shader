Shader "ASE/Ch01_cliff"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Texture ("Texture", 2D) = "white" {}
        _TextureSample0 ("TextureSample0", 2D) = "white" {}
        _Color ("Color", Color) = (0.82, 0.36, 0.84, 1)
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

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float topMask : TEXCOORD1;
            };

            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _Texture;
            sampler2D _TextureSample0;
            fixed4 _Color;

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                float3 wn = UnityObjectToWorldNormal(v.normal);
                o.topMask = saturate(max(smoothstep(0.35, 0.75, wn.y), v.color.g));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed4 tex = tex2D(_MainTex, i.uv) * tex2D(_Texture, i.uv) * tex2D(_TextureSample0, i.uv);
                fixed3 sidePink = fixed3(0.76, 0.30, 0.78);
                fixed3 topPink  = fixed3(0.95, 0.64, 0.98);
                fixed3 baseCol  = lerp(sidePink, topPink, i.topMask);
                fixed lum = dot(tex.rgb, fixed3(0.3, 0.59, 0.11));
                return fixed4(baseCol * lerp(0.84, 1.08, lum), 1.0);
            }
            ENDCG
        }
    }
}
