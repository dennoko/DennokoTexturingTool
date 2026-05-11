Shader "Dennoko/ColorAdjust"
{
    Properties
    {
        _MainTex    ("Texture",      2D)    = "white" {}
        _Mode       ("Mode",         Float) = 0
        _SrcColor   ("Source Color", Color) = (1,1,1,1)
        _DstColor   ("Target Color", Color) = (1,1,1,1)
        _Threshold  ("Threshold",    Float) = 0.1
        _Hue        ("Hue",          Float) = 0
        _Saturation ("Saturation",   Float) = 0
        _Value      ("Value",        Float) = 0
        _InMin      ("In Min",       Float) = 0
        _InMax      ("In Max",       Float) = 1
        _OutMin     ("Out Min",      Float) = 0
        _OutMax     ("Out Max",      Float) = 1
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
            float  _Mode;
            fixed4 _SrcColor, _DstColor;
            float  _Threshold;
            float  _Hue, _Saturation, _Value;
            float  _InMin, _InMax, _OutMin, _OutMax;

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata_base v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o;
            }

            float3 RgbToHsv(float3 c)
            {
                float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float  d = q.x - min(q.w, q.y);
                return float3(abs(q.z + (q.w - q.y) / (6.0*d + 1e-10)), d / (q.x + 1e-10), q.x);
            }

            float3 HsvToRgb(float3 c)
            {
                float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                if (_Mode < 0.5) // ColorReplace
                {
                    float3 d = col.rgb - _SrcColor.rgb;
                    if (dot(d, d) <= _Threshold * _Threshold)
                        col.rgb = _DstColor.rgb;
                }
                else if (_Mode < 1.5) // HSV
                {
                    float3 hsv = RgbToHsv(col.rgb);
                    hsv.x = frac(hsv.x + _Hue);
                    hsv.y = saturate(hsv.y + _Saturation);
                    hsv.z = saturate(hsv.z + _Value);
                    col.rgb = HsvToRgb(hsv);
                }
                else // Levels
                {
                    float range = max(_InMax - _InMin, 1e-5);
                    col.rgb = saturate((col.rgb - _InMin) / range);
                    col.rgb = _OutMin + col.rgb * (_OutMax - _OutMin);
                }
                return col;
            }
            ENDCG
        }
    }
}
