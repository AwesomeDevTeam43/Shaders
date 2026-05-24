Shader "Hidden/Custom/ReadingSteiner"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlitchIntensity ("Glitch Intensity", Range(0, 1.5)) = 0.0
    }
    SubShader
    {
        // Required for Post-Processing to draw over the screen properly
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

            float rand(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
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

                // 1. HORIZONTAL SCREEN TEARING
                float tearFrequency = 50.0;
                float tearSpeed = 20.0;
                float tearBand = step(0.9, sin(uv.y * tearFrequency + _Time.y * tearSpeed));
                float tearOffset = tearBand * (_GlitchIntensity * 0.1);
                uv.x += tearOffset;

                // 2. CHROMATIC ABERRATION (RGB SPLIT)
                float splitAmount = _GlitchIntensity * 0.08;
                float r = tex2D(_MainTex, float2(uv.x + splitAmount, uv.y)).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, float2(uv.x - splitAmount, uv.y)).b;

                // 3. DIGITAL STATIC NOISE
                float noise = (rand(uv + _Time.y) - 0.5) * _GlitchIntensity * 0.8;

                // 4. COLOR INVERSION (The Climax of the shift)
                float invertTrigger = step(0.9, _GlitchIntensity);
                float3 finalColor = float3(r, g, b) + noise;
                
                // When intensity hits max, rapidly flash inverted colors
                finalColor = lerp(finalColor, 1.0 - finalColor, invertTrigger * step(0.5, rand(uv * _Time.x)));

                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}