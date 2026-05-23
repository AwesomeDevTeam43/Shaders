Shader "Hidden/Custom/CCTV_Glitch"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlitchIntensity ("Glitch Intensity", Range(0, 1)) = 0.5
        _ChromAberration ("Chromatic Aberration", Range(0, 0.05)) = 0.01
        _LinesFrequency ("Scan Lines Frequency", Range(0, 100)) = 50
        _GraoIntensity ("Grain Intensity", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _GlitchIntensity;
            float _ChromAberration;
            float _LinesFrequency;
            float _GraoIntensity;

            float random(float2 st)
            {
                return frac(sin(dot(st, float2(12.9898, 78.233))) * 43758.5453123);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float glitchTime = random(_Time.y * 10) * random(_Time.y * 100);
                if (glitchTime > 0.5 * (1.0 - _GlitchIntensity))
                {
                    uv.x += random(uv.y * 10 + _Time.y) * _GlitchIntensity * 0.05;
                }

                float4 colR = tex2D(_MainTex, uv - float2(_ChromAberration * _GlitchIntensity, 0));
                float4 colG = tex2D(_MainTex, uv);
                float4 colB = tex2D(_MainTex, uv + float2(_ChromAberration * _GlitchIntensity, 0));

                float4 col = float4(colR.r, colG.g, colB.b, 1.0);

                float scanLine = random(uv.y * _LinesFrequency * 100 + _Time.y * 2.0);
                col.rgb -= saturate(scanLine * 0.1);

                float noise = random(uv + _Time.y);
                float grain = (noise - 0.5) * _GraoIntensity;
                col.rgb += grain * _GlitchIntensity * 0.5;

                float luma = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(col.rgb, float3(luma, luma, luma), 0.4);

                return col;
            }
            ENDCG
        }
    }
}
