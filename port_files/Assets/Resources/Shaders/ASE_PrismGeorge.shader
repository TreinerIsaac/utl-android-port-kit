Shader "ASE/PrismGeorge"
{
    Properties
    {
        _TextureSample0 ("TextureSample0", 2D) = "white" {}
        _MainTex ("MainTex", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Fresnel_Color ("Fresnel_Color", Color) = (0.68, 0.94, 1.0, 1.0)
        _Fresnel_Power ("Fresnel_Power", Float) = 0.33
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
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float fres : TEXCOORD1; };
            sampler2D _TextureSample0; float4 _TextureSample0_ST;
            sampler2D _MainTex;
            fixed4 _Color; fixed4 _Fresnel_Color;
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _TextureSample0);
                float3 wn = UnityObjectToWorldNormal(v.normal);
                float3 vd = normalize(WorldSpaceViewDir(v.vertex));
                o.fres = pow(1.0 - saturate(dot(wn, vd)), 2.2);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                fixed4 tex = tex2D(_TextureSample0, i.uv) * tex2D(_MainTex, i.uv);
                fixed3 rgb = lerp(tex.rgb * _Color.rgb, _Fresnel_Color.rgb, saturate(i.fres * 0.45));
                return fixed4(rgb, 1.0);
            }
            ENDCG
        }
    }
}
