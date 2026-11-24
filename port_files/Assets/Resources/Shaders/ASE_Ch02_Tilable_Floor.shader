Shader "ASE/Ch02_Tilable_Floor"
{
    Properties
    {
        _ColorHigh ("ColorHigh", Vector) = (0.30, 0.16, 0.44, 1)
        _ColorHighMulti ("ColorHighMulti", Range(0, 2)) = 1
        _ColorHighPower ("ColorHighPower", Float) = 1
        _ColorLow ("ColorLow", Vector) = (0.14, 0.07, 0.24, 1)
        _ColorLowMulti ("ColorLowMulti", Range(0, 2)) = 1
        _ColorLowPower ("ColorLowPower", Float) = 1
        _MainTex ("MainTex", 2D) = "white" {}
        _MainTexScale ("MainTexScale", Float) = 1
        _MainTexOffset ("MainTexOffset", Vector) = (0,0,0,0)
        _MainTex_Rotation ("MainTex_Rotation", Float) = 0
        [HideInInspector] __dirty ("", Float) = 1
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
            float4 _ColorHigh; float _ColorHighMulti;
            float4 _ColorLow; float _ColorLowMulti;
            float _MainTexScale; float4 _MainTexOffset; float _MainTex_Rotation;
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float sc = (abs(_MainTexScale) > 0.001) ? _MainTexScale : 1.0;
                float2 uv = TRANSFORM_TEX(v.uv, _MainTex) * sc + _MainTexOffset.xy;
                o.uv = uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed mask = dot(tex.rgb, fixed3(0.3, 0.59, 0.11));
                float lowM = (_ColorLowMulti > 0.001) ? _ColorLowMulti : 1.0;
                float highM = (_ColorHighMulti > 0.001) ? _ColorHighMulti : 1.0;
                fixed3 cLow = saturate(_ColorLow.rgb * lowM);
                fixed3 cHigh = saturate(_ColorHigh.rgb * highM);
                float sL = cLow.r + cLow.g + cLow.b;
                float sH = cHigh.r + cHigh.g + cHigh.b;
                if ((sL + sH) < 0.02 || (sL > 2.75 && sH > 2.75)) {
                    cLow = fixed3(0.14, 0.07, 0.24);
                    cHigh = fixed3(0.30, 0.16, 0.44);
                }
                return fixed4(lerp(cLow, cHigh, mask), 1.0);
            }
            ENDCG
        }
    }
}
