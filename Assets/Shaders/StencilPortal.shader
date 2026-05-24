Shader "Custom/Stencil/PortalMask_Hologram"
{
    Properties
    {
        _BorderColor ("Cor da Borda", Color) = (0, 0.8, 1, 1)
        _Thickness ("Espessura da Borda", Range(1, 10)) = 5.0
    }
    SubShader
    {
        // Mudámos para Transparent-1 para suportar cores transparentes antes do objeto escondido
        Tags { "RenderType"="Transparent" "Queue"="Geometry+1" }
        
        Blend SrcAlpha OneMinusSrcAlpha // Permite transparência real
        ZWrite Off 
        Cull Off // Vês o ecrã de ambos os lados

        Stencil
        {
            Ref 1           
            Comp Always     
            Pass Replace    
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _BorderColor;
            float _Thickness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calcula a distância do centro (0.5, 0.5) até às bordas (0 ou 1)
                float2 dist = abs(i.uv - 0.5) * 2.0; 
                
                // Escolhe a borda mais próxima (X ou Y)
                float edge = max(dist.x, dist.y);
                
                // Puxa a cor apenas para os extremos usando uma potência
                float glow = pow(edge, _Thickness);
                
                // A cor final terá Alpha 0 no centro (buraco do stencil) e Alpha 1 nas bordas
                fixed4 finalColor = _BorderColor;
                finalColor.a = glow;
                
                return finalColor;
            }
            ENDCG
        }
    }
}