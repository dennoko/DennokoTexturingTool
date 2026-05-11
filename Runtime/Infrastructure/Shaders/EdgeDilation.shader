Shader "Dennoko/EdgeDilation"
{
    Properties
    {
        _MainTex        ("Texture",         2D)    = "white" {}
        _Radius         ("Radius",          Int)   = 4
        _AlphaThreshold ("Alpha Threshold", Float) = 0.01
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
            float4    _MainTex_TexelSize;
            int       _Radius;
            float     _AlphaThreshold;

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata_base v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 center = tex2D(_MainTex, i.uv);
                if (center.a > _AlphaThreshold) return center;

                float  bestDist  = 1e9;
                fixed4 bestColor = center;

                for (int dy = -_Radius; dy <= _Radius; dy++)
                {
                    for (int dx = -_Radius; dx <= _Radius; dx++)
                    {
                        float2 offset   = float2(dx, dy) * _MainTex_TexelSize.xy;
                        fixed4 neighbor = tex2D(_MainTex, i.uv + offset);
                        if (neighbor.a > _AlphaThreshold)
                        {
                            float dist = (float)(dx*dx + dy*dy);
                            if (dist < bestDist)
                            {
                                bestDist  = dist;
                                bestColor = fixed4(neighbor.rgb, center.a);
                            }
                        }
                    }
                }
                return bestColor;
            }
            ENDCG
        }
    }
}
