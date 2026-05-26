Shader "Custom/WarpStarfield"
{
    Properties
    {
        [HDR] _StarColor ("Star Color", Color) = (0.6, 0.8, 1.0, 1.0)
        _Speed ("Warp Speed", Range(0.0, 50.0)) = 15.0
        _Density ("Star Density", Range(1.0, 10.0)) = 5.0
        _Layers ("Depth Layers", Integer) = 5
        _Stretch ("Speed Streak Stretch", Range(1.0, 20.0)) = 8.0
        _StarSize ("Star Core Size", Range(0.001, 0.1)) = 0.015
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Front 
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float4 _StarColor;
            float _Speed;
            float _Density;
            int _Layers;
            float _Stretch;
            float _StarSize;

            float3 hash33(float3 p3)
            {
                p3 = frac(p3 * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yxz + 33.33);
                return frac((p3.xxy + p3.yxx) * p3.zyx);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 rayDir = normalize(i.worldPos - _WorldSpaceCameraPos);
                float3 accumulatedColor = float3(0, 0, 0);
                
                rayDir.z /= _Stretch;

                for (int layer = 1; layer <= _Layers; layer++)
                {
                    float3 scaledPos = rayDir * (layer * _Density);
                    scaledPos.z += _Time.y * _Speed / layer;

                    float3 gridId = floor(scaledPos);
                    float3 localPos = frac(scaledPos) - 0.5;

                    float3 random3D = hash33(gridId);

                    localPos -= (random3D - 0.5) * 0.5;

                    if (random3D.x > 0.85) 
                    {
                        float dist = length(localPos);
                        float core = smoothstep(_StarSize + 0.01, _StarSize, dist);
                        float halo = smoothstep(0.25, 0.0, dist) * 0.3; 
                        
                        float brightness = core + halo;

                        float fade = sin(_Time.y * 2.0 + random3D.y * 10.0) * 0.5 + 0.5;
                        
                        accumulatedColor += _StarColor.rgb * brightness * fade;
                    }
                }

                return fixed4(accumulatedColor, 1.0);
            }
            ENDCG
        }
    }
}