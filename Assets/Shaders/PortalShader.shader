Shader "Custom/Stencil/FrankStonePortal"
{
    Properties
    {
        [Header(1. Fundo e Centro)]
        [HDR] _CoreColor    ("Cor do Nucleo", Color) = (0.8, 1.0, 0.8, 1.0)
        _DarkColor          ("Cor do Vazio (Fundo)", Color) = (0.0, 0.02, 0.01, 1.0)
        _CoreSize           ("Tamanho do Brilho Central", Range(0.0, 0.5)) = 0.05

        [Header(2. Borda Fresnel Rim)]
        [HDR] _RimColor     ("Cor do Fresnel", Color) = (0.0, 1.0, 0.2, 1.0)
        _RimPower           ("Potencia do Fresnel", Range(0.1, 10.0)) = 3.0

        [Header(3. Interior Vortice Espiral)]
        [HDR] _VortexColor  ("Cor do Vortice", Color) = (0.0, 1.5, 0.2, 1.0)
        _ArmCount           ("Numero de Bracos", Range(1, 15)) = 4.0
        _SwirlSpeed         ("Torcao (Twist)", Range(0.0, 30.0)) = 12.0
        _TimeSpeed          ("Velocidade da Espiral", Range(0.0, 15.0)) = 6.0
        _EnergySharpness    ("Contraste da Energia", Range(1.0, 20.0)) = 8.0

        [Header(4. Linhas de Vento)]
        [HDR] _WindColor    ("Cor das Linhas de Vento", Color) = (0.5, 1.0, 0.5, 1.0)
        _WindSpeed          ("Velocidade de Succao", Range(0.0, 10.0)) = 3.0
        _WindIntensity      ("Intensidade do Vento", Range(0.0, 2.0)) = 0.8
        _WindLines          ("Quantidade de Linhas", Range(10, 100)) = 60.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Geometry+2" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always

        Stencil
        {
            Ref 1
            Comp Always // (Muda para Equal quando fores usar a parede falsa!)
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
            float4 _DarkColor;
            float _CoreSize;

            float4 _RimColor;
            float _RimPower;

            float4 _VortexColor;
            float _ArmCount;
            float _SwirlSpeed;
            float _TimeSpeed;
            float _EnergySharpness;

            float4 _WindColor;
            float _WindSpeed;
            float _WindIntensity;
            float _WindLines;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normalWorld = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.viewDirWorld = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 centered = i.uv - float2(0.5, 0.5);
                float radius = length(centered);
                float angle = atan2(centered.y, centered.x);

                // ==========================================
                // 1. O VAZIO PROFUNDO
                // ==========================================
                float3 col = _DarkColor.rgb;

                // ==========================================
                // 2. VÓRTICE (Espirais de Energia)
                // ==========================================
                float arms = floor(_ArmCount);
                
                // O Segredo: A torção matemática
                float spiralMath = angle * arms + radius * _SwirlSpeed - _Time.y * _TimeSpeed;
                float spiral = sin(spiralMath) * 0.5 + 0.5;
                
                // Eleva a potência para fazer as linhas de energia ficarem afiadas (raios elétricos)
                float energy = pow(spiral, _EnergySharpness);
                
                // Faz a energia desaparecer perto do centro (buraco negro) e nas bordas
                energy *= smoothstep(0.5, 0.1, radius) * smoothstep(0.0, 0.1, radius);

                col = lerp(col, _VortexColor.rgb, energy);

                // ==========================================
                // 3. LINHAS DE VENTO (Agora Dobradas!)
                // ==========================================
                float lines = floor(_WindLines);
                
                // O ERRO ESTAVA AQUI: Agora as linhas sofrem a torção da espiral!
                float twistedAngle = angle + radius * (_SwirlSpeed * 0.8);
                float normalizedAngle = frac((twistedAngle + 3.14159) / 6.28318);
                
                float slice = floor(normalizedAngle * lines);
                float streakNoise = frac(sin(slice * 12.9898) * 43758.5);
                
                // Multiplicar o raio por 3.0 faz as linhas parecerem traços curtos em vez de riscos infinitos
                float travel = frac(radius * 3.0 - _Time.y * _WindSpeed);
                
                float localAngle = frac(normalizedAngle * lines);
                float lineThickness = smoothstep(0.1, 0.5, localAngle) * smoothstep(0.9, 0.5, localAngle);
                
                float streakMask = smoothstep(0.05, 0.2, radius) * smoothstep(0.5, 0.2, radius);
                float streak = streakNoise * (1.0 - travel) * lineThickness * streakMask;

                col += _WindColor.rgb * streak * _WindIntensity;

                // ==========================================
                // 4. BRILHO CENTRAL (O Fim do Túnel)
                // ==========================================
                // Usar pow(..., 3.0) corta o "ovo gigante" e deixa só um ponto de luz focado
                float coreGlow = pow(smoothstep(_CoreSize + 0.1, 0.0, radius), 3.0);
                col = lerp(col, _CoreColor.rgb, coreGlow);

                // ==========================================
                // 5. CAMADA FRESNEL (Bordas da Porta)
                // ==========================================
                float3 normal = normalize(i.normalWorld);
                float3 viewDir = normalize(i.viewDirWorld);
                float rim = 1.0 - saturate(dot(normal, viewDir));
                float rimIntensity = pow(rim, _RimPower);

                col += _RimColor.rgb * rimIntensity;

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}