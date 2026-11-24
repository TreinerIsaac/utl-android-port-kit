Shader "ASE/Ch04_Warship_Hallway_FloorLight"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Texture ("Texture", 2D) = "white" {}
        _TextureSample0 ("TextureSample0", 2D) = "white" {}
        _Color ("Color", Color) = (0.18, 0.10, 0.32, 1)
        _Color0 ("Color 0", Color) = (0.14, 0.08, 0.26, 1)
        _Color1 ("Color 1", Color) = (0.26, 0.16, 0.42, 1)
        _ColorLow ("ColorLow", Color) = (0.14, 0.08, 0.26, 1)
        _ColorHigh ("ColorHigh", Color) = (0.26, 0.16, 0.42, 1)
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

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _Texture;
            sampler2D _TextureSample0;
            fixed4 _Color; fixed4 _Color0; fixed4 _Color1; fixed4 _ColorLow; fixed4 _ColorHigh;

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed4 tex = tex2D(_MainTex, i.uv) * tex2D(_Texture, i.uv) * tex2D(_TextureSample0, i.uv);
                float sat = max(tex.r, max(tex.g, tex.b)) - min(tex.r, min(tex.g, tex.b));
                if (sat > 0.08) {
                    fixed3 t = ((_Color.r + _Color.g + _Color.b) > 0.05) ? _Color.rgb : fixed3(1,1,1);
                    return fixed4(tex.rgb * t, 1.0);
                }
                fixed lum = dot(tex.rgb, fixed3(0.3, 0.59, 0.11));
                fixed3 c0 = ((_Color0.r + _Color0.g + _Color0.b) > 0.05 && (_Color0.r + _Color0.g + _Color0.b) < 2.75)
                    ? _Color0.rgb
                    : (((_ColorLow.r + _ColorLow.g + _ColorLow.b) > 0.05 && (_ColorLow.r + _ColorLow.g + _ColorLow.b) < 2.75)
                        ? _ColorLow.rgb : fixed3(0.15, 0.08, 0.28));
                fixed3 c1 = ((_Color1.r + _Color1.g + _Color1.b) > 0.05 && (_Color1.r + _Color1.g + _Color1.b) < 2.75)
                    ? _Color1.rgb
                    : (((_ColorHigh.r + _ColorHigh.g + _ColorHigh.b) > 0.05 && (_ColorHigh.r + _ColorHigh.g + _ColorHigh.b) < 2.75)
                        ? _ColorHigh.rgb : fixed3(0.26, 0.16, 0.42));
                return fixed4(lerp(c0, c1, lum), 1.0);
            }
            ENDCG
        }
    }
}
