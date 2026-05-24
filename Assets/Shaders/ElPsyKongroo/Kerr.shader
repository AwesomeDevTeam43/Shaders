Shader "Custom/TrueVolumetricBlackHole"
{
    Properties
    {
        _Mass ("Black Hole Mass", Range(0.01, 2.0)) = 0.3
        _EventHorizon ("Event Horizon", Range(0.01, 0.5)) = 0.15
        _WarpStrength ("Lensing Exaggeration", Range(1, 10)) = 5.0 // NEW: Multiplies the background tear
        
        [Header(Accretion Disk)]
        _DiskRadius ("Disk Outer Radius", Range(0.2, 2.0)) = 1.0
        _DiskThickness ("Disk Thickness", Range(0.001, 0.2)) = 0.02
        _DiskDensity ("Gas Density", Range(1, 100)) = 40.0
        [HDR] _DiskColor ("Disk Color", Color) = (1, 0.4, 0.1, 1)
        _SpinSpeed ("Gas Spin Speed", Range(0, 20)) = 8.0
        
        [Header(Raymarcher Limits)]
        _MaxSteps ("Max Ray Steps", Integer) = 100
        _StepSize ("Step Size", Range(0.005, 0.1)) = 0.02
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        GrabPass { "_BackgroundTexture" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

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
                float4 vertex : SV_POSITION;
                float3 localPos : TEXCOORD0;
                float3 localCamPos : TEXCOORD1;
                float4 screenPos : TEXCOORD02;
            };

            sampler2D _BackgroundTexture;
            
            float _Mass;
            float _EventHorizon;
            float _WarpStrength;
            float _DiskRadius;
            float _DiskThickness;
            float _DiskDensity;
            float4 _DiskColor;
            float _SpinSpeed;
            
            int _MaxSteps;
            float _StepSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeGrabScreenPos(o.vertex);
                o.localPos = v.vertex.xyz;
                o.localCamPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 rayPos = i.localPos;
                if (length(i.localCamPos) < 0.5) rayPos = i.localCamPos; 
                
                float3 rayDir = normalize(rayPos - i.localCamPos);
                float2 originalScreenUV = i.screenPos.xy / i.screenPos.w;
                float3 initialRayDir = rayDir;
                
                float4 accumulatedGlow = float4(0, 0, 0, 0);
                bool hitHorizon = false;
                
                for (int step = 0; step < _MaxSteps; step++)
                {
                    float distFromCenter = length(rayPos);
                    
                    if (distFromCenter < _EventHorizon)
                    {
                        hitHorizon = true;
                        break;
                    }
                    
                    if (distFromCenter > 0.51) break; 

                    // GRAVITY SINK
                    float3 gravityPull = -normalize(rayPos) * (_Mass / (distFromCenter * distFromCenter + 0.001));
                    rayDir = normalize(rayDir + gravityPull * _StepSize);
                    
                    // ACCRETION DISK (NOW WITH ANGULAR SPIN)
                    float distToEquator = abs(rayPos.y);
                    if (distToEquator < _DiskThickness && distFromCenter > _EventHorizon && distFromCenter < _DiskRadius)
                    {
                        float verticalDensity = 1.0 - (distToEquator / _DiskThickness);
                        float radialFalloff = 1.0 - ((distFromCenter - _EventHorizon) / (_DiskRadius - _EventHorizon));
                        
                        // NEW: Calculate actual 360-degree angle for true rotation
                        float angle = atan2(rayPos.z, rayPos.x);
                        float spin = angle + _Time.y * _SpinSpeed;
                        
                        // Break the gas into chaotic, spinning clumps
                        float radialNoise = sin(distFromCenter * 40.0) * 0.5 + 0.5;
                        float angularNoise = sin(spin * 6.0) * 0.5 + 0.5;
                        float ringNoise = radialNoise * angularNoise;
                        
                        // NEW: Exaggerated Doppler Beaming (Relativistic beaming)
                        float3 spinDir = normalize(cross(float3(0, 1, 0), rayPos));
                        // Gas moving toward you is blinding, gas moving away is dark
                        float doppler = pow(max(0.0, dot(rayDir, spinDir)), 2.0); 
                        
                        float stepGlow = verticalDensity * radialFalloff * ringNoise * _DiskDensity * _StepSize;
                        
                        // Boost the intensity heavily on the approaching side
                        accumulatedGlow.rgb += _DiskColor.rgb * stepGlow * (doppler * 3.0 + 0.2);
                        accumulatedGlow.a += stepGlow;
                    }

                    rayPos += rayDir * _StepSize;
                }

                if (hitHorizon)
                {
                    return float4(accumulatedGlow.rgb, 1.0);
                }
                else
                {
                    // MULTIPLY THE ESCAPE DISTORTION
                    float2 distortion = (rayDir.xy - initialRayDir.xy) * _WarpStrength;
                    float2 lensedUV = originalScreenUV + distortion;
                    
                    float4 background = tex2D(_BackgroundTexture, lensedUV);
                    return background + accumulatedGlow;
                }
            }
            ENDCG
        }
    }
}