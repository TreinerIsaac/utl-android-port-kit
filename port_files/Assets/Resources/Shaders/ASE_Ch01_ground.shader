Shader "ASE/Ch01_ground"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Texture ("Texture", 2D) = "white" {}
        _TextureSample0 ("TextureSample0", 2D) = "white" {}
        _Color ("Color", Color) = (0,0,0,0)
        _Color0 ("Color 0", Color) = (0,0,0,0)
        _Color1 ("Color 1", Color) = (0,0,0,0)
        _ColorLow ("ColorLow", Color) = (0,0,0,0)
        _ColorHigh ("ColorHigh", Color) = (0,0,0,0)
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

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * tex2D(_Texture, i.uv) * tex2D(_TextureSample0, i.uv);

                // Se a textura ja e colorida (nao branca/cinza), usa a cor da textura com tint opcional
                float sat = max(tex.r, max(tex.g, tex.b)) - min(tex.r, min(tex.g, tex.b));
                if (sat > 0.08)
                {
                    fixed3 t = ((_Color.r + _Color.g + _Color.b) > 0.05) ? _Color.rgb : fixed3(1, 1, 1);
                    return fixed4(tex.rgb * t, 1.0);
                }

                // Prioridade 1: _ColorLow / _ColorHigh (RosesRoom_Ground_Mat, Ch01_ground_mat, Ch02_ground_mat)
                float sumLow = _ColorLow.r + _ColorLow.g + _ColorLow.b;
                float sumHigh = _ColorHigh.r + _ColorHigh.g + _ColorHigh.b;
                float sum0 = _Color0.r + _Color0.g + _Color0.b;
                float sum1 = _Color1.r + _Color1.g + _Color1.b;
                float sumC = _Color.r + _Color.g + _Color.b;

                fixed3 cLow;
                fixed3 cHigh;
                if (sumLow > 0.02 || sumHigh > 0.02)
                {
                    cLow = (sumLow > 0.02) ? _ColorLow.rgb : _ColorHigh.rgb;
                    cHigh = (sumHigh > 0.02) ? _ColorHigh.rgb : _ColorLow.rgb;
                }
                else if (sum0 > 0.02 || sum1 > 0.02)
                {
                    cLow = (sum0 > 0.02) ? _Color0.rgb : _Color1.rgb;
                    cHigh = (sum1 > 0.02) ? _Color1.rgb : _Color0.rgb;
                }
                else if (sumC > 0.02)
                {
                    cLow = _Color.rgb;
                    cHigh = _Color.rgb;
                }
                else
                {
                    cLow = fixed3(0.14, 0.08, 0.26);
                    cHigh = fixed3(0.26, 0.16, 0.42);
                }

                fixed lum = dot(tex.rgb, fixed3(0.3, 0.59, 0.11));
                fixed vLum = dot(i.color.rgb, fixed3(0.3, 0.59, 0.11));
                fixed t = saturate(lum * vLum);
                fixed3 outCol = lerp(cLow, cHigh, t);
                return fixed4(outCol, 1.0);
            }
            ENDCG
        }
    }
}
