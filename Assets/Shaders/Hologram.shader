Shader "Custom/Hologram"
{
    Properties
    {
        [HDR] _Color ("Hologram Color", Color) = (0, 0.933, 1, 0.75)
        _RimPower ("Rim Power", Range(0.1, 8.0)) = 3.0
        
        [Header(Scanlines)]
        _ScanlineFreq ("Scanline Frequency", Float) = 50.0
        _ScanlineSpeed ("Scanline Speed", Float) = 5.0
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.5
        
        [Header(Breathing)]
        _BreathSpeed ("Breathing Speed", Float) = 2.0
        _BreathAmp ("Breathing Amplitude", Float) = 0.01
        
        [Header(Glitch Control)]
        _GlitchIntensity ("Glitch Intensity", Range(0, 1)) = 0.0
        _IsConstantGlitch ("Is Constant Glitch", Float) = 0.0 // 0 = Beat, 1 = Constant
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            float4 _Color;
            float _RimPower;
            float _ScanlineFreq;
            float _ScanlineSpeed;
            float _ScanlineIntensity;
            float _BreathSpeed;
            float _BreathAmp;
            float _GlitchIntensity;
            float _IsConstantGlitch;

            v2f vert (appdata v)
            {
                v2f o;

                float breath = (sin(_Time.y * _BreathSpeed) + cos(_Time.y * _BreathSpeed * 0.8)) * 0.5;
                v.vertex.xyz += v.normal * breath * _BreathAmp;

                float slice = sin(_Time.y * 50.0 + v.vertex.y * 20.0);
                
                float baseSnap = step(0.8, sin(_Time.y * 15.0)); 
                float glitchSnap = max(baseSnap, _IsConstantGlitch);
                
                v.vertex.x += slice * glitchSnap * _GlitchIntensity * 0.1;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
                float3 normal = normalize(i.normal);

                float rim = 1.0 - saturate(dot(normal, viewDir));
                float rimIntensity = pow(rim, _RimPower);

                float scanline = sin(i.worldPos.y * _ScanlineFreq - _Time.y * _ScanlineSpeed);
                scanline = scanline * 0.5 + 0.5; 

                fixed4 col = _Color;
                col.a = _Color.a * rimIntensity * lerp(1.0, scanline, _ScanlineIntensity);

                float baseSnap = step(0.8, sin(_Time.y * 15.0)); 
                float glitchSnap = max(baseSnap, _IsConstantGlitch);
                float flicker = lerp(1.0, sin(_Time.y * 40.0) * 0.5 + 0.5, _GlitchIntensity * glitchSnap);
                
                col.a *= flicker;

                return col;
            }
            ENDCG
        }
    }
}