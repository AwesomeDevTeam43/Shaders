Shader "Custom/Stencil/PortalMask_Hologram"
{
    Properties
    {
        _BorderColor ("Cor da Borda", Color) = (0, 0.8, 1, 1)
        _Thickness ("Espessura da Borda", Range(1, 10)) = 5.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Geometry+1" }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off 
        Cull Off

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
                float2 dist = abs(i.uv - 0.5) * 2.0; 
                
                float edge = max(dist.x, dist.y);
                
                float glow = pow(edge, _Thickness);
                
                fixed4 finalColor = _BorderColor;
                finalColor.a = glow;
                
                return finalColor;
            }
            ENDCG
        }
    }
}