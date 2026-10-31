Shader "Unlit/Photoelasticity"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector]_Color("Color", Color) = (1, 0, 0, 1)
        _CMF("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _CMF;
            float4 _CMF_ST;
            float _Test;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                int n;
                float t = 0.5;          // mm
                float ds = i.color.r;   // Pa
                float C = 72;           // * 10^(-9) Pa
                float PI = 3.1415926535;
                float C2 = PI * t * C * ds * 20;
                float lambda = 380;
                float Intensity;
                fixed4 col = fixed4(0, 0, 0, 0);

                for (n = 0; n < 40; n++) {
                    Intensity = pow(sin(C2 / (lambda + n * 10)), 2);
                    float2 test_uv = ((float)n / 40.0, (float)n / 40.0);
                    col += tex2D(_CMF, test_uv) * Intensity * 0.5 * i.color.g * 2;
                }

                // sample the texture
                // *i.color.g;//3 * tex2D(_CMF, i.uv); //i.color;//tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
