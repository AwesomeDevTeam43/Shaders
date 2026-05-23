Shader "Custom/EnergyShield"
{
    Properties
    {
        _Color          ("Shield Color",       Color)        = (0.2, 0.8, 1.0, 0.3)
        _RippleColor    ("Ripple Color",       Color)        = (1.0, 0.5, 0.1, 1.0)
        _RimPower       ("Rim Power",          Range(0.5,6)) = 2.5
        _PulseSpeed     ("Pulse Speed",        Float)        = 3.0
        _ImpactPos      ("Impact World Pos",   Vector)       = (0,0,0,0)
        _ImpactTime     ("Impact Timer (0-1)", Range(0,1))   = 0.0
        _MaxRadius      ("Max Ripple Radius",  Float)        = 3.0
        _RippleWidth    ("Ripple Width",       Float)        = 0.12
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+5" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.0
            #include "UnityCG.cginc"

            float4 _Color;
            float4 _RippleColor;
            float  _RimPower;
            float  _PulseSpeed;
            float4 _ImpactPos;
            float  _ImpactTime;
            float  _MaxRadius;
            float  _RippleWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 vertex   : POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal   : TEXCOORD1;
            };

            struct g2f
            {
                float4 pos      : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal   : TEXCOORD1;
                float3 viewDir  : TEXCOORD2;
            };

            v2g vert(appdata v)
            {
                v2g o;
                o.vertex   = v.vertex;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal   = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            // ── Geometry ──────────────────────────────────────────────────
            // O geometry shader recebe cada triângulo e re-emite-o
            // ligeiramente expandido ao longo da normal na zona do ripple.
            // Isto dá um ligeiro "relevo" 3D à onda enquanto passa.
            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> stream)
            {
                // Centróide e normal do triângulo
                float3 center     = (IN[0].worldPos + IN[1].worldPos + IN[2].worldPos) / 3.0;
                float3 faceNormal = normalize(IN[0].normal + IN[1].normal + IN[2].normal);

                // Raio atual do ripple neste frame
                float currentRadius = _ImpactTime * _MaxRadius;

                // Distância do centróide ao centro do impacto
                float dist = length(center - _ImpactPos.xyz);

                // O triângulo está dentro da frente de onda?
                // smoothstep cria uma transição suave em vez de um degrau abrupto
                float onWave = smoothstep(_RippleWidth, 0.0, abs(dist - currentRadius));

                // Expansão ao longo da normal proporcional à intensidade da onda
                // e que diminui conforme _ImpactTime avança (a onda "aplana" ao expandir)
                float expand = onWave * 0.04 * (1.0 - _ImpactTime);

                for (int i = 0; i < 3; i++)
                {
                    // Desloca o vértice ao longo da normal world-space
                    float3 newWorldPos = IN[i].worldPos + faceNormal * expand;

                    // Converte de volta para object space para o clip transform
                    float4 objPos = mul(unity_WorldToObject, float4(newWorldPos, 1.0));

                    g2f o;
                    o.pos      = UnityObjectToClipPos(objPos);
                    o.worldPos = newWorldPos;
                    o.normal   = IN[i].normal;
                    o.viewDir  = normalize(WorldSpaceViewDir(IN[i].vertex));
                    stream.Append(o);
                }
                stream.RestartStrip();
            }

            fixed4 frag(g2f i) : SV_Target
            {
                float3 N = normalize(i.normal);
                float3 V = normalize(i.viewDir);

                // ── Fresnel: bordas mais opacas ────────────────────────────
                float fresnel = pow(1.0 - saturate(dot(N, V)), _RimPower);

                // ── Pulso base do escudo ───────────────────────────────────
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;

                // ── Ripple ─────────────────────────────────────────────────
                // Distância deste fragmento ao ponto de impacto
                float dist = length(i.worldPos - _ImpactPos.xyz);

                // Raio atual da onda (cresce de 0 até _MaxRadius)
                float currentRadius = _ImpactTime * _MaxRadius;

                // Intensidade do ripple neste fragmento:
                // máxima quando dist ≈ currentRadius, zero fora da largura do anel
                float ripple = 1.0 - smoothstep(0.0, _RippleWidth, abs(dist - currentRadius));

                // A onda vai ficando mais fraca conforme se expande
                float rippleFade = ripple * (1.0 - _ImpactTime);

                // ── Cor final ──────────────────────────────────────────────
                fixed4 col;
                col.rgb  = _Color.rgb;
                col.rgb += fresnel  * _Color.rgb;           // brilho nas bordas
                col.rgb += pulse    * _Color.rgb * 0.1;     // pulso suave
                col.rgb  = lerp(col.rgb, _RippleColor.rgb, rippleFade); // onda

                col.a    = _Color.a + fresnel * 0.3 + rippleFade * 0.5;

                return saturate(col);
            }
            ENDCG
        }
    }
    FallBack Off
}
