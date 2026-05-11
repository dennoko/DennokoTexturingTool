Shader "Dennoko/TilingOffset"
{
    Properties
    {
        _MainTex      ("Texture",       2D)     = "white" {}
        _TilingScale  ("Tiling Scale",  Vector) = (1,1,0,0)
        _TilingOffset ("Tiling Offset", Vector) = (0,0,0,0)
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
            float4 _TilingScale;
            float4 _TilingOffset;

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata_base v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv * _TilingScale.xy + _TilingOffset.xy;
                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}
