Shader "Custom/SpriteShine"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShineWidth ("Shine Width", Range(0.0, 1.0)) = 0.2
        _ShineOffset ("Shine Offset", Range(-1, 2)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _ShineColor;
            float _ShineWidth;
            float _ShineOffset;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Generate shine mask
                float shine = smoothstep(_ShineOffset, _ShineOffset + _ShineWidth, i.uv.x) * 
                              smoothstep(_ShineOffset + _ShineWidth, _ShineOffset, i.uv.x);

                col.rgb += _ShineColor.rgb * shine * col.a;
                return col;
            }
            ENDCG
        }
    }
}
