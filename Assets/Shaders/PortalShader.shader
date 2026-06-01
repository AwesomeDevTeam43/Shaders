Shader "Custom/Stencil/FrankStonePortal"
{
    Properties
    {
        [Header(1. Colors)]
        [HDR] _CoreColor        ("Core Color", Color)               = (1.5, 2.0, 1.0, 1.0)
        [HDR] _MidColor         ("Mid Swirl Color", Color)          = (0.2, 1.5, 0.3, 1.0)
        [HDR] _DebrisColor      ("Debris Color", Color)             = (0.01, 0.05, 0.01, 1.0)

        [Header(2. Swirl Effect)]
        _SwirlSpeed             ("Swirl Rotation Speed", Range(0.0, 10.0)) = 0.5
        _InwardSpeed            ("Inward Suction Speed", Range(0.0, 10.0)) = 1.0
        _SwirlTwist             ("Swirl Twist Amount", Range(0.0, 20.0))   = 5.0
        _SwirlScale             ("Swirl Noise Scale", Range(0.1, 10.0))    = 3.0

        [Header(3. Core)]
        _CoreSize               ("Core Size", Range(0.0, 1.0))             = 0.3

        [Header(5. Fresnel)]
        [HDR] _FresnelColor     ("Fresnel Color", Color)           = (0.0, 1.0, 0.2, 1.0)
        _FresnelPower           ("Fresnel Power", Range(0.1, 10.0))= 3.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Geometry+2" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual

        Stencil
        {
            Ref 1
            Comp Always
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex       : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWorld  : TEXCOORD1;
                float3 viewDirWorld : TEXCOORD2;
            };

            float4 _CoreColor;
            float4 _MidColor;
            float4 _DebrisColor;
            float _SwirlSpeed;
            float _InwardSpeed;
            float _SwirlTwist;
            float _SwirlScale;
            float _CoreSize;
            float4 _FresnelColor;
            float _FresnelPower;

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                v += 0.5000 * valueNoise(p); p = p * 2.02;
                v += 0.2500 * valueNoise(p); p = p * 2.03;
                v += 0.1250 * valueNoise(p); p = p * 2.01;
                v += 0.0625 * valueNoise(p);
                return v;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normalWorld = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.viewDirWorld = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;
                float2 centered = i.uv - 0.5;
                float radius = length(centered);
                float angle = atan2(centered.y, centered.x);

                float twistAngle = radius * _SwirlTwist + t * _SwirlSpeed;
                float s_twist = sin(twistAngle);
                float c_twist = cos(twistAngle);
                float2x2 rotTwist = float2x2(c_twist, -s_twist, s_twist, c_twist);
                
                float2 swirlUV = mul(rotTwist, centered) * _SwirlScale;
                swirlUV += float2(t * _InwardSpeed * 0.5, t * _InwardSpeed * 0.7);
                
                float swirlNoise = fbm(swirlUV * _SwirlScale);
                swirlNoise = smoothstep(0.2, 0.8, swirlNoise);
                
                float s = sin(t * 0.5), c = cos(t * 0.5);
                float2x2 rot = float2x2(c, -s, s, c);
                float2 debrisUV = mul(rot, centered) * 15.0;
                debrisUV += float2(t * 2.0, t * 1.5);
                
                float debrisNoise = valueNoise(debrisUV);
                debrisNoise *= valueNoise(debrisUV + 5.5); 
                float debris = smoothstep(0.75, 0.8, debrisNoise);

                float coreGlow = 1.0 - smoothstep(0.0, _CoreSize, radius);
                coreGlow = pow(coreGlow, 2.0);

                float3 col = lerp(_MidColor.rgb * 0.5, _MidColor.rgb, swirlNoise);
                
                col += _CoreColor.rgb * coreGlow;
                
                col = lerp(col, _DebrisColor.rgb, debris * 0.8);

                float3 normal = normalize(i.normalWorld);
                float3 viewDir = normalize(i.viewDirWorld);
                float NdotV = saturate(dot(normal, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                col += _FresnelColor.rgb * fresnel;

                float alphaBorder = smoothstep(0.0, 0.02, i.uv.x) * smoothstep(1.0, 0.98, i.uv.x) *
                                    smoothstep(0.0, 0.02, i.uv.y) * smoothstep(1.0, 0.98, i.uv.y);
                
                float alpha = alphaBorder;

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}