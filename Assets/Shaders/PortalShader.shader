Shader "Custom/Stencil/DimensionalPortal"
{
    Properties
    {
        // --- Borda ---
        _BorderColor        ("Border Color",            Color)        = (0.0, 1.0, 0.2, 1.0)
        _BorderThickness    ("Border Thickness",        Range(1, 12)) = 4.0
        _RimPower           ("Rim Power",               Range(0.5, 6))= 2.0

        // --- Vórtice ---
        _VortexColor        ("Vortex Color",            Color)        = (0.1, 0.9, 0.15, 1.0)
        _VortexSpeed        ("Vortex Spin Speed",       Range(0, 5))  = 1.2
        _VortexTex          ("Vortex Texture",          2D)           = "white" {}

        // --- Linhas de vento ---
        _WindColor          ("Wind Streak Color",       Color)        = (0.7, 1.0, 0.7, 1.0)
        _WindSpeed          ("Wind Speed",              Range(0, 4))  = 1.5
        _WindLineCount      ("Wind Line Count",         Range(8, 64)) = 28.0
        _WindIntensity      ("Wind Intensity",          Range(0, 1))  = 0.35

        // --- Glow geral ---
        _GlowIntensity      ("Glow Intensity",          Range(0, 3))  = 1.4
        _PulseSpeed         ("Pulse Speed",             Range(0, 6))  = 1.8
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Geometry+1" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        // ------------------------------------------------------------------
        // Pass 1 — escreve máscara no stencil (igual ao StencilPortal.shader)
        // ------------------------------------------------------------------
        Stencil
        {
            Ref 1
            Comp Always
            Pass Replace
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // ---- structs ----
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 viewDir  : TEXCOORD1;
                float3 normal   : TEXCOORD2;
            };

            // ---- propriedades ----
            fixed4   _BorderColor;
            float    _BorderThickness;
            float    _RimPower;

            fixed4   _VortexColor;
            float    _VortexSpeed;
            sampler2D _VortexTex;

            fixed4   _WindColor;
            float    _WindSpeed;
            float    _WindLineCount;
            float    _WindIntensity;

            float    _GlowIntensity;
            float    _PulseSpeed;

            // ---- helpers ----

            // Pseudo-random clássico (mesmo estilo que CCTV_Glitch.shader)
            float random(float2 st)
            {
                return frac(sin(dot(st, float2(12.9898, 78.233))) * 43758.5453123);
            }

            // Rotação UV em coordenadas polares (vórtice)
            // spin acelera perto do centro — conservação de momento angular
            float2 vortexUV(float2 uv, float speed)
            {
                float2 centered = uv - float2(0.5, 0.5);
                float  radius   = length(centered);
                float  angle    = atan2(centered.y, centered.x);

                float  spin     = _Time.y * speed / (radius + 0.08);
                angle          += spin;

                return float2(cos(angle), sin(angle)) * radius + float2(0.5, 0.5);
            }

            // Streaks radiais animados para o centro
            float windStreaks(float2 uv, float speed, float lineCount, float intensity)
            {
                float2 centered = uv - float2(0.5, 0.5);
                float  radius   = length(centered);
                float  angle    = atan2(centered.y, centered.x);

                // linhas finas em ângulo, compridas no raio
                float  lineNoise = frac(sin(angle * lineCount) * 43758.5453);
                // animar em direção ao centro
                float  travel    = frac(radius - _Time.y * speed);
                float  streak    = lineNoise * (1.0 - travel) * (1.0 - radius * 1.8);

                return saturate(streak * intensity);
            }

            // ---- vertex ----
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex  = UnityObjectToClipPos(v.vertex);
                o.uv      = v.uv;
                o.normal  = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }

            // ---- fragment ----
            fixed4 frag(v2f i) : SV_Target
            {
                // 1. Borda — Fresnel espalhado (igual à lógica do Hologram.shader)
                float rimDot       = 1.0 - saturate(dot(i.normal, i.viewDir));
                float rimIntensity = pow(rimDot, _RimPower);

                // glow de borda baseado em UV (mesmo cálculo do StencilPortal.shader)
                float2 dist    = abs(i.uv - float2(0.5, 0.5)) * 2.0;
                float  edge    = max(dist.x, dist.y);
                float  edgeGlow = pow(edge, _BorderThickness);

                // combinar rim e edge
                float  border  = saturate(rimIntensity + edgeGlow);

                // pulsação suave (mesmo estilo do EnergyShield.shader)
                float  pulse   = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                border        *= lerp(0.75, 1.0, pulse);

                // 2. Vórtice — UV polar animado, amostrado na textura (ou gerado)
                float2 rotUV   = vortexUV(i.uv, _VortexSpeed);
                // sem textura: gera padrão de espiral por noise
                float  vortNoise = random(rotUV * 3.0 + _Time.y * 0.1);
                // fade para o centro (mais intenso) e para as bordas (some)
                float  radialFade = 1.0 - saturate(length(i.uv - float2(0.5, 0.5)) * 2.2);
                float  vortex  = vortNoise * radialFade;

                // 3. Linhas de vento
                float  wind    = windStreaks(i.uv, _WindSpeed, _WindLineCount, _WindIntensity);

                // 4. Composição final
                fixed4 col = fixed4(0, 0, 0, 0);

                // borda
                col.rgb += _BorderColor.rgb * border * _GlowIntensity;
                col.a    = border * _BorderColor.a;

                // vórtice (só no interior — onde edge é baixo)
                float interior = 1.0 - edgeGlow;
                col.rgb += _VortexColor.rgb * vortex * interior * _GlowIntensity;
                col.a    = saturate(col.a + vortex * interior * 0.7);

                // linhas de vento (sobre o interior)
                col.rgb += _WindColor.rgb * wind * interior;
                col.a    = saturate(col.a + wind * interior * 0.5);

                return col;
            }
            ENDCG
        }
    }
}
