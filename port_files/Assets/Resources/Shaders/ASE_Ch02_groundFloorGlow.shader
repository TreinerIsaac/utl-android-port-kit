Shader "ASE/Ch02_groundFloorGlow"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _TextureSample0 ("TextureSample0", 2D) = "white" {}
        _Color0 ("Color 0", Color) = (0.28, 0.96, 0.65, 1)
        _Color1 ("Color 1", Color) = (0.0, 1.0, 0.47, 1)
        _GlowEdgeCOlor ("GlowEdgeCOlor", Color) = (0.0, 1.0, 0.62, 1)
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
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _TextureSample0;
            fixed4 _Color0; fixed4 _Color1; fixed4 _GlowEdgeCOlor;
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.uv, _MainTex); return o; }
            fixed4 frag(v2f i) : SV_Target {
                fixed4 tex = tex2D(_MainTex, i.uv) * tex2D(_TextureSample0, i.uv);
                fixed lum = dot(tex.rgb, fixed3(0.3, 0.59, 0.11));
                fixed3 c0 = ((_Color0.r + _Color0.g + _Color0.b) > 0.05) ? _Color0.rgb : fixed3(0.05, 0.68, 0.18);
                fixed3 c1 = ((_Color1.r + _Color1.g + _Color1.b) > 0.05) ? _Color1.rgb : fixed3(0.0, 0.85, 0.30);
                return fixed4(lerp(c0 * 0.72, c1, lum), 1.0);
            }
            ENDCG
        }
    }
}
