Shader "ASE/LapisWings_Opaque"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _TextureSample2 ("TextureSample2", 2D) = "white" {}
        _Noise ("Noise", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Color0 ("Color0", Color) = (0.28,0.74,1,1)
        _EmissionColor ("EmissionColor", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _TextureSample2;
            float4 _TextureSample2_ST;
            sampler2D _Noise;
            float4 _Noise_ST;
            fixed4 _Color;
            fixed4 _Color0;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv2 : TEXCOORD0; float2 uvN : TEXCOORD1; };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv2 = TRANSFORM_TEX(v.uv, _TextureSample2);
                o.uvN = TRANSFORM_TEX(v.uv, _Noise) + float2(0.0, _Time.y * 0.5);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed baseL = tex2D(_TextureSample2, i.uv2).r;
                fixed noiseL = tex2D(_Noise, i.uvN).r;
                fixed3 deepWater = fixed3(0.16, 0.56, 0.96);
                fixed3 midWater = (_Color0.r + _Color0.g + _Color0.b > 0.05) ? _Color0.rgb : fixed3(0.32, 0.78, 1.0);
                fixed3 foam = fixed3(0.86, 0.97, 1.0);
                fixed waveMix = saturate(baseL * 0.65 + noiseL * 0.35);
                fixed3 col = lerp(deepWater, midWater, saturate(waveMix * 1.4));
                col = lerp(col, foam, smoothstep(0.68, 0.92, waveMix));
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}
