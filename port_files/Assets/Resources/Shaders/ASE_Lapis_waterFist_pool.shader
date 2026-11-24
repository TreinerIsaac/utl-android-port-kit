Shader "ASE/Lapis_waterFist_pool"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Color_Base ("Color_Base", Color) = (0.47,0.87,1,1)
        _WaveColor ("WaveColor", Color) = (0.76,0.97,1,1)
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
            fixed4 _Color_Base;
            fixed4 _WaveColor;

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
                fixed3 rgb = lerp(_Color_Base.rgb, _WaveColor.rgb, t.r) * i.color.rgb;
                return fixed4(rgb, t.a * i.color.a);
            }
            ENDCG
        }
    }
}
