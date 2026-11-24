Shader "ASE/RosesRoom_GroundClouds"
{
    Properties
    {
        _NoiseTex ("NoiseTex", 2D) = "white" {}
        _Color ("Color", Color) = (1, 0.71, 0.87, 1)
        _Color0 ("Color0", Color) = (0.88, 0.61, 0.75, 1)
        _Speed01 ("Speed01", Vector) = (0.36, 0.18, 0, 0)
        _Speed02 ("Speed02", Vector) = (0.13, 0.30, 0, 0)
        _Speed03 ("Speed03", Vector) = (0.25, 0.24, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "IgnoreProjector"="True" }
        LOD 100
        Cull Back
        Lighting Off
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            fixed4 _Color;
            fixed4 _Color0;
            float4 _Speed01;
            float4 _Speed02;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _NoiseTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv1 = i.uv + _Speed01.xy * (_Time.y * 0.08);
                float2 uv2 = i.uv * 1.35 + _Speed02.xy * (_Time.y * 0.06);
                fixed n1 = tex2D(_NoiseTex, uv1).r;
                fixed n2 = tex2D(_NoiseTex, uv2).g;
                fixed mask = smoothstep(0.25, 0.78, saturate(n1 * 0.65 + n2 * 0.35));
                fixed3 col = lerp(_Color0.rgb, _Color.rgb, mask);
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}
