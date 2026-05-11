Shader "Dennoko/LayerBlend"
{
    Properties
    {
        _MainTex  ("Layer Texture", 2D) = "white" {}
        _Base     ("Base Canvas",   2D) = "black" {}
        _Mask     ("ID Mask",       2D) = "white" {}
        _Mode     ("Blend Mode",    Float) = 0
        _Opacity  ("Opacity",       Range(0,1)) = 1
        _UseMask  ("Use ID Mask",   Float) = 0
    }

    SubShader
    {
        Pass
        {
            ZTest Always ZWrite Off Cull Off Fog { Mode Off }

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _Base;
            sampler2D _Mask;
            float _Mode;
            float _Opacity;
            float _UseMask;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.texcoord;
                return o;
            }

            fixed4 BlendNormal(fixed4 base, fixed4 layer)
            {
                float a = layer.a * _Opacity;
                return fixed4(lerp(base.rgb, layer.rgb, a), max(base.a, a));
            }

            fixed4 BlendAdd(fixed4 base, fixed4 layer)
            {
                return fixed4(saturate(base.rgb + layer.rgb * _Opacity), base.a);
            }

            fixed4 BlendMultiply(fixed4 base, fixed4 layer)
            {
                return fixed4(lerp(base.rgb, base.rgb * layer.rgb, _Opacity), base.a);
            }

            fixed4 BlendOverlay(fixed4 base, fixed4 layer)
            {
                fixed3 ov = lerp(
                    2.0 * base.rgb * layer.rgb,
                    1.0 - 2.0 * (1.0 - base.rgb) * (1.0 - layer.rgb),
                    step(0.5, base.rgb));
                return fixed4(lerp(base.rgb, ov, _Opacity), base.a);
            }

            fixed4 BlendScreen(fixed4 base, fixed4 layer)
            {
                fixed3 scr = 1.0 - (1.0 - base.rgb) * (1.0 - layer.rgb);
                return fixed4(lerp(base.rgb, scr, _Opacity), base.a);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 base  = tex2D(_Base,    i.uv);
                fixed4 layer = tex2D(_MainTex, i.uv);

                if (_UseMask > 0.5)
                    layer.a *= tex2D(_Mask, i.uv).r;

                if (_Mode < 0.5) return BlendNormal  (base, layer);
                if (_Mode < 1.5) return BlendAdd      (base, layer);
                if (_Mode < 2.5) return BlendMultiply (base, layer);
                if (_Mode < 3.5) return BlendOverlay  (base, layer);
                return                   BlendScreen  (base, layer);
            }
            ENDCG
        }
    }
}
