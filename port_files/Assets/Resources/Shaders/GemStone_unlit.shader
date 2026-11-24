Shader "Shader Forge/GemStone_unlit"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Texture ("Texture", 2D) = "white" {}
        _TextureSample0 ("TextureSample0", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _TintColor ("TintColor", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        LOD 100
        Cull Back
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; fixed shade : TEXCOORD1; };
            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _Texture;
            sampler2D _TextureSample0;
            fixed4 _Color; fixed4 _TintColor;
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                float3 wn = UnityObjectToWorldNormal(v.normal);
                o.shade = lerp(0.88, 1.06, saturate(wn.y * 0.5 + 0.5));
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                fixed4 tex = tex2D(_MainTex, i.uv) * tex2D(_Texture, i.uv) * tex2D(_TextureSample0, i.uv);
                fixed4 tint = ((_Color.r + _Color.g + _Color.b) > 0.05) ? _Color : _TintColor;
                fixed3 rgb = tex.rgb * tint.rgb * i.shade;
                return fixed4(rgb, 1.0);
            }
            ENDCG
        }
    }
}
