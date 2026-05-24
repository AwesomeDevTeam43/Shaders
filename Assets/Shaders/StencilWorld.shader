Shader "Custom/Stencil/HiddenObject_XRay"
{
    Properties
    {
        _ScanColor ("Cor do Raio-X", Color) = (1, 0, 0, 1) // Vermelho Alerta
        _ScanSpeed ("Velocidade do Scan", Float) = 3.0
    }
    SubShader
    {
        // Continua a ignorar paredes à frente
        Tags { "RenderType"="Opaque" "Queue"="Geometry+2" }
        ZTest Always 

        Stencil
        {
            Ref 1           
            Comp Equal      
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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normalWorld : TEXCOORD0;
                float3 viewDirWorld : TEXCOORD1;
                float3 objPos : TEXCOORD2;
            };

            fixed4 _ScanColor;
            float _ScanSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.objPos = v.vertex; // Posição local para as linhas
                
                o.normalWorld = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.viewDirWorld = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Fresnel (Contorno de Raio-X brilhante nas bordas)
                float fresnel = 1.0 - saturate(dot(i.normalWorld, i.viewDirWorld));
                fresnel = pow(fresnel, 2.0);

                // 2. Linhas de scan dinâmicas
                float scanline = sin(i.objPos.y * 50.0 + _Time.y * _ScanSpeed);
                scanline = saturate(scanline * 0.5 + 0.5);

                // Mistura tudo para parecer digital
                fixed4 finalColor = _ScanColor * fresnel;
                finalColor.rgb += _ScanColor.rgb * scanline * 0.5;
                
                return finalColor;
            }
            ENDCG
        }
    }
}