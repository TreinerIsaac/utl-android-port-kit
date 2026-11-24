Shader "TextMeshPro/Distance Field" {
    Properties {
        _FaceTex ("Face Texture", 2D) = "white" {}
        _FaceUVSpeedX ("Face UV Speed X", Range(-5, 5)) = 0.0
        _FaceUVSpeedY ("Face UV Speed Y", Range(-5, 5)) = 0.0
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _FaceDilate ("Face Dilate", Range(-1,1)) = 0
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Thickness", Range(0,1)) = 0
        _OutlineSoftness ("Outline Softness", Range(0,1)) = 0
        _UnderlayColor ("Border Color", Color) = (0,0,0,0.5)
        _UnderlayOffsetX ("Border OffsetX", Range(-1,1)) = 0
        _UnderlayOffsetY ("Border OffsetY", Range(-1,1)) = 0
        _UnderlayDilate ("Border Dilate", Range(-1,1)) = 0
        _UnderlaySoftness ("Border Softness", Range(0,1)) = 0
        _MainTex ("Font Atlas", 2D) = "white" {}
        _TextureWidth ("Texture Width", float) = 512
        _TextureHeight ("Texture Height", float) = 512
        _GradientScale ("Gradient Scale", float) = 5.0
        _ScaleX ("Scale X", float) = 1.0
        _ScaleY ("Scale Y", float) = 1.0
        _ScaleRatioA ("Scale Ratio A", float) = 1
        _ScaleRatioC ("Scale Ratio C", float) = 1
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
                float2 underlayUV : TEXCOORD1;
                float4 param : TEXCOORD2;
                float2 faceUV : TEXCOORD3;
            };

            sampler2D _MainTex;
            sampler2D _FaceTex;
            float4 _FaceTex_ST;
            fixed4 _FaceColor;
            fixed4 _OutlineColor;
            fixed4 _UnderlayColor;
            float _FaceDilate;
            float _OutlineWidth;
            float _OutlineSoftness;
            float _UnderlayOffsetX;
            float _UnderlayOffsetY;
            float _UnderlayDilate;
            float _UnderlaySoftness;
            float _TextureWidth;
            float _TextureHeight;
            float _GradientScale;
            float _ScaleRatioA;
            float _ScaleRatioC;
            float _WeightNormal;
            float _WeightBold;

            float2 UnpackUV(float uv) {
                float2 output;
                output.x = floor(uv / 4096.0);
                output.y = uv - 4096.0 * output.x;
                return output * 0.001953125;
            }

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord0;
                o.faceUV = TRANSFORM_TEX(UnpackUV(v.texcoord1.x), _FaceTex);
                o.color = v.color * _FaceColor;

                float bold = step(v.texcoord1.y, 0.0) * _WeightBold + (1.0 - step(v.texcoord1.y, 0.0)) * _WeightNormal;
                float faceEdge = 0.5 - (_FaceDilate * _ScaleRatioA * 0.5) - (bold * 0.25);
                float soft = max(0.035, _OutlineSoftness * _ScaleRatioA * 0.25 + 0.045);

                float texW = max(128.0, _TextureWidth);
                float texH = max(128.0, _TextureHeight);
                float gScale = max(1.0, _GradientScale);
                float2 uOff = float2(_UnderlayOffsetX * _ScaleRatioC * gScale / texW,
                                     _UnderlayOffsetY * _ScaleRatioC * gScale / texH);
                o.underlayUV = v.texcoord0 - uOff;
                o.param = float4(faceEdge, soft, v.color.a, _UnderlayDilate * _ScaleRatioC * 0.5);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                half d = tex2D(_MainTex, i.uv).a;
                half faceEdge = i.param.x;
                half soft = i.param.y;
                half vAlpha = i.param.z;
                half outlineHalf = _OutlineWidth * _ScaleRatioA * 0.55;

                half faceAlpha = smoothstep(faceEdge - soft, faceEdge + soft, d);
                fixed4 faceTexCol = tex2D(_FaceTex, i.faceUV);
                fixed4 fgCol = i.color * faceTexCol;
                half fgAlpha = faceAlpha * fgCol.a;
                fixed3 fgRgb = fgCol.rgb;

                if (outlineHalf > 0.001) {
                    half outerEdge = max(0.01, faceEdge - outlineHalf);
                    half outerAlpha = smoothstep(outerEdge - soft, outerEdge + soft, d);
                    fixed4 outCol = _OutlineColor;
                    outCol.a *= vAlpha;

                    fgAlpha = max(outerAlpha * outCol.a, faceAlpha * fgCol.a);
                    fgRgb = lerp(outCol.rgb, fgCol.rgb, faceAlpha);
                }

                fixed4 fg = fixed4(fgRgb * fgAlpha, fgAlpha);

                if (_UnderlayColor.a > 0.01) {
                    half ud = tex2D(_MainTex, i.underlayUV).a;
                    half uSoft = max(soft, _UnderlaySoftness * _ScaleRatioC * 0.5 + 0.05);
                    half uEdge = max(0.01, faceEdge - i.param.w - outlineHalf * 0.5);
                    half uAlpha = smoothstep(uEdge - uSoft, uEdge + uSoft, ud) * _UnderlayColor.a * vAlpha;
                    fixed4 bg = fixed4(_UnderlayColor.rgb * uAlpha, uAlpha);
                    return fg + bg * (1.0 - fg.a);
                }

                return fg;
            }
            ENDCG
        }
    }
}
