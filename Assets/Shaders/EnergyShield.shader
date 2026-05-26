Shader "Custom/EnergyShield"
{
    Properties
    {
        _Color("Shield Color", Color) = (0.2, 0.8, 1.0, 0.3)
        _RippleColor("Ripple Color", Color) = (1.0, 0.5, 0.1, 1.0)
        _FresnelEffect("Rim Power", Range(0.5, 6)) = 2.5
        _ImpactPos("Impact World Pos", Vector) = (0, 0, 0, 0)
        _ImpactTime("Impact Timer (0-1)", Range(0, 1)) = 0.0
        _MaxRadius("Max Ripple Radius", Float) = 3.0
        _RippleWidth("Ripple Width", Float) = 0.12
    }

    SubShader
    {
        Tags{"RenderType" = "Transparent" "Queue" = "Transparent+5"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.0
            #include "UnityCG.cginc"

            float4 _Color;
            float4 _RippleColor;
            float _FresnelEffect;
            float4 _ImpactPos;
            float _ImpactTime;
            float _MaxRadius;
            float _RippleWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 vertex : POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            v2g vert(appdata v)
            {
                v2g o;
                o.vertex = v.vertex;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream < g2f> stream)
            {
                float3 center = (IN[0].worldPos + IN[1].worldPos + IN[2].worldPos) / 3.0;
                float3 faceNormal = normalize(IN[0].normal + IN[1].normal + IN[2].normal);

                float currentRadius = _ImpactTime * _MaxRadius;

                float dist = length(center - _ImpactPos.xyz);

                float onWave = smoothstep(_RippleWidth, 0.0, abs(dist - currentRadius));

                float active = step(0.001, _ImpactTime);
                float expand = onWave * 0.04 * (1.0 - _ImpactTime) * active;

                for (int i = 0;
                i < 3; i++)
                {
                    float3 newWorldPos = IN[i].worldPos + faceNormal * expand;

                    float4 objPos = mul(unity_WorldToObject, float4(newWorldPos, 1.0));

                    g2f o;
                    o.pos = UnityObjectToClipPos(objPos);
                    o.worldPos = newWorldPos;
                    o.normal = IN[i].normal;
                    o.viewDir = normalize(WorldSpaceViewDir(IN[i].vertex));
                    stream.Append(o);
                }
                stream.RestartStrip();
            }

            fixed4 frag(g2f i) : SV_Target
            {
                float3 N = normalize(i.normal);
                float3 V = normalize(i.viewDir);

                float fresnel = pow(1.0 - saturate(dot(N, V)), _FresnelEffect);
                float dist = length(i.worldPos - _ImpactPos.xyz);
                float currentRadius = _ImpactTime * _MaxRadius;
                float ripple = 1.0 - smoothstep(0.0, _RippleWidth, abs(dist - currentRadius));
                float rippleFade = ripple * (1.0 - _ImpactTime) * step(0.001, _ImpactTime);

                fixed4 col;
                col.rgb = _Color.rgb;
                col.rgb += fresnel * _Color.rgb;
                col.rgb = lerp(col.rgb, _RippleColor.rgb, rippleFade);
                col.a = _Color.a + fresnel * 0.3 + rippleFade * 0.5;

                return saturate(col);
            }
            ENDCG
        }
    }
    FallBack Off
}
