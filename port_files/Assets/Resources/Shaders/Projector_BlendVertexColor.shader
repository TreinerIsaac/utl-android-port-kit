Shader "Projector/BlendVertexColor"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _ShadowTex ("Cookie", 2D) = "white" {}
        _MainTex ("MainTex", 2D) = "white" {}
        _FalloffTex ("FallOff", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            ZWrite Off
            ColorMask RGB
            Blend SrcAlpha OneMinusSrcAlpha
            Offset -1, -1

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 uvShadow : TEXCOORD0;
                float4 uvFalloff : TEXCOORD1;
                float4 pos : SV_POSITION;
            };

            float4x4 unity_Projector;
            float4x4 unity_ProjectorClip;

            fixed4 _Color;
            sampler2D _ShadowTex;
            sampler2D _MainTex;
            sampler2D _FalloffTex;

            v2f vert(float4 vertex : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.uvShadow = mul(unity_Projector, vertex);
                o.uvFalloff = mul(unity_ProjectorClip, vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (i.uvShadow.w <= 0.0001) discard;
                float2 uv = i.uvShadow.xy / i.uvShadow.w;
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) discard;

                fixed4 texS = tex2D(_ShadowTex, uv);
                fixed4 texM = tex2D(_MainTex, uv);
                fixed4 tex = texS * texM;
                tex *= _Color;

                float falloffCoord = saturate(i.uvFalloff.x / max(0.0001, i.uvFalloff.w));
                fixed falloffA = tex2D(_FalloffTex, float2(falloffCoord, 0.5)).a;
                // Guard against default white texture having alpha=1 or black texture having alpha=0
                tex.a *= max(falloffA, 0.001) > 0.01 ? falloffA : 1.0;
                return tex;
            }
            ENDCG
        }
    }
}
