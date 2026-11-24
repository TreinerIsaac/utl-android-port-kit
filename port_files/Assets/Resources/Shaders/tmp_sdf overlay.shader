Shader "TextMeshPro/Distance Field Overlay" {
    Properties {
        _FaceTex ("Face Texture", 2D) = "white" {}
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _FaceDilate ("Face Dilate", Range(-1,1)) = 0
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Thickness", Range(0,1)) = 0
        _OutlineSoftness ("Outline Softness", Range(0,1)) = 0
        _MainTex ("Font Atlas", 2D) = "white" {}
        _TextureWidth ("Texture Width", float) = 512
        _TextureHeight ("Texture Height", float) = 512
        _GradientScale ("Gradient Scale", float) = 5.0
        _ScaleX ("Scale X", float) = 1.0
        _ScaleY ("Scale Y", float) = 1.0
        _ScaleRatioA ("Scale Ratio A", float) = 1
        _WeightNormal ("Weight Normal", float) = 0
        _WeightBold ("Weight Bold", float) = 0.5
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Stencil {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        ZWrite Off
        Lighting Off
        Fog { Mode Off }
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 param : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _FaceColor;
            fixed4 _OutlineColor;
            float _FaceDilate;
            float _OutlineWidth;
            float _OutlineSoftness;
            float _GradientScale;
            float _ScaleRatioA;
            float _WeightNormal;
            float _WeightBold;

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord0;
                o.color = v.color * _FaceColor;
                float bold = step(v.texcoord1.y, 0.0) * _WeightBold + (1.0 - step(v.texcoord1.y, 0.0)) * _WeightNormal;
                o.param.x = 0.5 - (_FaceDilate * _ScaleRatioA * 0.5) - (bold * 0.25);
                o.param.y = max(0.05, _OutlineSoftness * _ScaleRatioA * 0.25 + 0.06);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                half d = tex2D(_MainTex, i.uv).a;
                half edge = i.param.x;
                half soft = i.param.y;
                half alpha = smoothstep(edge - soft, edge + soft, d);
                fixed4 col = i.color;
                col.a *= alpha;
                col.rgb *= col.a;
                return col;
            }
            ENDCG
        }
    }
}
