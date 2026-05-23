Shader "Custom/RadioactiveLiquid"
{
    Properties
    {
        _Color              ("Liquid Color",        Color)        = (0.2, 1.0, 0.1, 0.85)
        _GlowColor          ("Glow Color",          Color)        = (0.5, 1.0, 0.2, 1.0)
        _GlassColor         ("Glass Color",         Color)        = (0.8, 1.0, 0.9, 0.08)
        _FillLevel          ("Fill Level",          Range(-1, 1)) = 0.0
        _WaveAmplitude      ("Wave Amplitude",      Range(0, 0.1))= 0.02
        _WaveFrequency      ("Wave Frequency",      Range(0, 20)) = 8.0
        _WaveSpeed          ("Wave Speed",          Range(0, 5))  = 1.5
        _SecondaryWaveScale ("Secondary Wave Scale",Range(0, 1))  = 0.4
        _NoiseScale         ("Noise Scale",         Range(0, 5))  = 2.0
        _NoiseStrength      ("Noise Strength",      Range(0, 1))  = 0.5
        _FoamThreshold      ("Foam Threshold",      Range(0, 0.1))= 0.015
        _EmissionIntensity  ("Emission Intensity",  Range(0, 3))  = 1.2
        _FresnelPower       ("Fresnel Power",       Range(0.1, 5))= 2.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Name "Glass"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _GlassColor;
            float  _FresnelPower;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 view   : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos    = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.view   = WorldSpaceViewDir(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.normal);
                float3 V = normalize(i.view);

                float fresnel = pow(1.0 - saturate(dot(N, V)), _FresnelPower);

                fixed4 col = _GlassColor;
                col.a      = lerp(_GlassColor.a, 0.35, fresnel);
                return col;
            }
            ENDCG
        }

        Pass
        {
            Name "StencilWrite"
            ColorMask 0
            ZWrite Off
            Cull Front

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f     { float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target { return 0; }
            ENDCG
        }

        Pass
        {
            Name "Liquid"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            Stencil
            {
                Ref 1
                Comp Equal
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            fixed4 _GlowColor;
            float  _FillLevel;
            float  _WaveAmplitude;
            float  _WaveFrequency;
            float  _WaveSpeed;
            float  _SecondaryWaveScale;
            float  _NoiseScale;
            float  _NoiseStrength;
            float  _FoamThreshold;
            float  _EmissionIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float smoothNoise(float2 p)
            {
                float2 i = floor(p);        
                float2 f = frac(p);         

                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(a, b, u.x),
                    lerp(c, d, u.x),
                    u.y
                );
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv       = v.uv;
                o.pos      = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _WaveSpeed;

                float primaryWave   = sin(i.worldPos.x * _WaveFrequency + t)
                                      * _WaveAmplitude;
                float secondaryWave = cos(i.worldPos.z * _WaveFrequency * 0.75 + t * 1.3)
                                      * _WaveAmplitude * _SecondaryWaveScale;
                float turbulence    = sin(i.worldPos.x * _WaveFrequency * 2.1 + t * 0.7)
                                      * cos(i.worldPos.z * _WaveFrequency * 1.8 + t * 1.1)
                                      * _WaveAmplitude * 0.3;

                float noiseWave = smoothNoise(
                    float2(i.worldPos.x, i.worldPos.z) * _NoiseScale + _Time.y * 0.5
                ) * _WaveAmplitude * 2.0 * _NoiseStrength;

                float totalWave = primaryWave + secondaryWave + turbulence + noiseWave;

                float objectCenterY = unity_ObjectToWorld[1][3];
                float objectRadius  = length(float3(
                                        unity_ObjectToWorld[0][1],
                                        unity_ObjectToWorld[1][1],
                                        unity_ObjectToWorld[2][1]));

                float surfaceWorldY = objectCenterY
                                    + (_FillLevel * objectRadius)
                                    + totalWave;

                clip(surfaceWorldY - i.worldPos.y);

                float belowSurface = surfaceWorldY - i.worldPos.y;

                float foam      = 1.0 - smoothstep(0.0, _FoamThreshold, belowSurface);

                float depth     = saturate(belowSurface * 8.0);
                float alpha     = lerp(0.55, _Color.a, depth);

                float glowPulse = sin(_Time.y * 2.0 + totalWave * 50.0) * 0.5 + 0.5;
                fixed4 emissive = _GlowColor * glowPulse * _EmissionIntensity * foam;

                fixed4 col  = _Color + emissive;
                col.a       = alpha + foam * 0.4;
                return col;
            }
            ENDCG
        }
    }
}