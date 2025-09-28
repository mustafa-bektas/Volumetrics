Shader "Custom/Volumetric"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PreviousFrame ("Previous Frame", 2D) = "black" {}
        _FogDensity ("Fog Density", Float) = 0.01
        _FogColor ("Fog Color", Color) = (0.5, 0.5, 0.5, 1)
        _StepCount ("Step Count", Int) = 64
        _MaxDistance ("Max Distance", Float) = 200
        _ScatteringCoefficient ("Scattering", Float) = 0.5
        _LightColor ("Light Color", Color) = (1, 1, 1, 1)
        _LightIntensity ("Light Intensity", Float) = 1.0
        
        _CloudBaseHeight ("Cloud Base Height", Float) = 20.0
        _CloudTopHeight ("Cloud Top Height", Float) = 40.0
        _CloudCoverage ("Cloud Coverage", Float) = 0.6
        _CloudDensity ("Cloud Density", Float) = 0.8
        _NoiseScale ("Noise Scale", Float) = 1.0
        _NoiseDetailScale ("Detail Noise Scale", Float) = 0.5
        _WindSpeed ("Wind Speed", Float) = 0.1
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0, 0)
        _CloudSharpness ("Cloud Sharpness", Float) = 0.5
        _SilverLining ("Silver Lining", Float) = 1.0
        _AmbientLighting ("Ambient Lighting", Float) = 0.3
        
        _FogType ("Fog Type", Int) = 3
        _UseTemporalAccumulation ("Use Temporal", Int) = 1
        _TemporalBlendFactor ("Temporal Blend", Float) = 0.95
        _ShowFogOnly ("Show Fog Only", Int) = 0
        _ShowStepCount ("Show Step Count", Int) = 0
        _DebugMode ("Debug Mode", Int) = 0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewVector : TEXCOORD1;
            };
            
            // Properties
            sampler2D _MainTex;
            sampler2D _PreviousFrame;
            sampler2D _CameraDepthTexture;
            float _FogDensity;
            float4 _FogColor;
            int _StepCount;
            float _MaxDistance;
            float _ScatteringCoefficient;
            float4 _LightColor;
            float _LightIntensity;
            float3 _LightDirection;
            
            // Cloud parameters
            float _CloudBaseHeight;
            float _CloudTopHeight;
            float _CloudCoverage;
            float _CloudDensity;
            float _NoiseScale;
            float _NoiseDetailScale;
            float _WindSpeed;
            float2 _WindDirection;
            float _CloudSharpness;
            float _SilverLining;
            float _AmbientLighting;
            
            int _FogType;
            int _UseTemporalAccumulation;
            float _TemporalBlendFactor;
            
            float4x4 _CameraInverseProjection;
            float4x4 _ViewProjectionMatrix;
            float4x4 _PreviousViewProjectionMatrix;
            
            int _ShowFogOnly;
            int _ShowStepCount;
            int _DebugMode;
            
            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // Smooth interpolation
                
                return lerp(lerp(lerp(hash(i + float3(0, 0, 0)), 
                                    hash(i + float3(1, 0, 0)), f.x),
                                lerp(hash(i + float3(0, 1, 0)), 
                                    hash(i + float3(1, 1, 0)), f.x), f.y),
                        lerp(lerp(hash(i + float3(0, 0, 1)), 
                                    hash(i + float3(1, 0, 1)), f.x),
                                lerp(hash(i + float3(0, 1, 1)), 
                                    hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            float fbm(float3 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                float maxValue = 0.0;
                
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * noise3D(p * frequency);
                    maxValue += amplitude;
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value / maxValue;
            }

            float GetCloudDensity(float3 worldPos)
            {
                float height = worldPos.y;
                
                if (height < _CloudBaseHeight || height > _CloudTopHeight)
                    return 0.0;
                
                // Smooth height-based density falloff
                float cloudThickness = _CloudTopHeight - _CloudBaseHeight;
                float heightInCloud = (height - _CloudBaseHeight) / cloudThickness;
                
                // Smoother cloud profile
                float heightDensity = smoothstep(0.0, 0.1, heightInCloud) * 
                                    smoothstep(1.0, 0.9, heightInCloud);
                heightDensity = pow(heightDensity, 0.5 + _CloudSharpness);
                
                // Wind animation - slower and smoother
                float time = _Time.y * _WindSpeed * 0.1; // Much slower
                float3 windOffset = float3(_WindDirection.x, 0, _WindDirection.y) * time * 10.0;
                float3 animatedPos = worldPos + windOffset;
                
                // Large scale cloud shapes - much larger scale
                float3 noisePos = animatedPos * _NoiseScale * 0.001; // Much smaller frequency
                float cloudShape = fbm(noisePos, 3); // Fewer octaves
                
                // Smooth coverage threshold
                float coverage = smoothstep(1.0 - _CloudCoverage, 1.0 - _CloudCoverage + 0.2, cloudShape);
                
                // Detail noise - also larger scale
                float3 detailPos = animatedPos * _NoiseDetailScale * 0.01; // Larger scale
                float cloudDetail = fbm(detailPos, 2); // Fewer octaves
                cloudDetail = lerp(0.8, 1.0, cloudDetail); // Subtle detail
                
                // Combine everything smoothly
                float finalDensity = coverage * cloudDetail * heightDensity * _CloudDensity;
                
                // Smooth the result
                finalDensity = smoothstep(0.0, 0.3, finalDensity);
                
                return finalDensity * _FogDensity * 50.0; // Reduced multiplier
            }
            
            // Enhanced phase function for clouds
            float GetCloudPhaseFunction(float3 lightDir, float3 viewDir)
            {
                float cosTheta = dot(lightDir, -viewDir);
                
                // Henyey-Greenstein with strong forward scattering
                float g1 = 0.8; // Strong forward scattering
                float g2 = -0.2; // Slight back scattering
                
                float phase1 = (1.0 - g1 * g1) / pow(1.0 + g1 * g1 - 2.0 * g1 * cosTheta, 1.5);
                float phase2 = (1.0 - g2 * g2) / pow(1.0 + g2 * g2 - 2.0 * g2 * cosTheta, 1.5);
                
                // Mix forward and back scattering
                float phase = lerp(phase1, phase2, 0.3);
                
                // Add silver lining effect
                float silverLining = pow(saturate(cosTheta), 8.0) * _SilverLining;
                
                return phase + silverLining;
            }
            
            // Temporal reprojection
            float2 GetPreviousScreenPos(float3 worldPos)
            {
                float4 prevClipPos = mul(_PreviousViewProjectionMatrix, float4(worldPos, 1.0));
                float2 prevScreenPos = prevClipPos.xy / prevClipPos.w;
                return prevScreenPos * 0.5 + 0.5;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                float3 viewVector = mul(_CameraInverseProjection, float4(v.uv * 2 - 1, 0, -1));
                o.viewVector = mul(unity_CameraToWorld, float4(viewVector, 0)).xyz;
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 sceneColor = tex2D(_MainTex, i.uv);
                float depth = tex2D(_CameraDepthTexture, i.uv).r;
                float linearDepth = LinearEyeDepth(depth);
                
                float3 rayStart = _WorldSpaceCameraPos;
                float3 rayDir = normalize(i.viewVector);
                float rayLength = min(linearDepth, _MaxDistance);
                
                // Debug modes
                if (_DebugMode == 1) // Linear depth
                {
                    float normalizedDepth = saturate(linearDepth / 200.0);
                    return fixed4(normalizedDepth, 0, 1.0 - normalizedDepth, 1);
                }
                
                if (_DebugMode == 2) // Cloud shape
                {
                    float3 testPos = rayStart + rayDir * 50.0;
                    float density = GetCloudDensity(testPos);
                    float normalizedDensity = saturate(density * 0.1);
                    return fixed4(normalizedDensity, normalizedDensity, normalizedDensity, 1);
                }
                
                if (_DebugMode == 3) // Cloud height layers
                {
                    float3 testPos = rayStart + rayDir * 50.0;
                    float height = testPos.y;
                    bool inCloudLayer = (height >= _CloudBaseHeight && height <= _CloudTopHeight);
                    float heightNorm = (height - _CloudBaseHeight) / (_CloudTopHeight - _CloudBaseHeight);
                    return fixed4(heightNorm, inCloudLayer ? 1 : 0, 0, 1);
                }
                
                if (_DebugMode == 4) // Wind animation
                {
                    float time = _Time.y * _WindSpeed;
                    float3 windOffset = float3(_WindDirection.x, 0, _WindDirection.y) * time;
                    float windVis = frac(length(windOffset) * 0.1);
                    return fixed4(windVis, windVis, 0, 1);
                }
                
                if (rayLength <= 0.001)
                {
                    if (_ShowStepCount == 1)
                        return fixed4(0, 1, 0, 1);
                    return sceneColor;
                }
                
                // Ray marching setup with better quality for clouds
                float stepSize = rayLength / float(_StepCount);

                // Use blue noise instead of white noise for jittering
                float2 noiseUV = i.uv * 512.0; // Scale for noise texture
                float jitter = frac(sin(dot(noiseUV, float2(12.9898, 78.233))) * 43758.5453);
                jitter = jitter * 2.0 - 1.0; // [-1, 1] range

                float3 currentPos = rayStart + rayDir * (jitter * stepSize * 0.1); // Much smaller jitter
                float3 rayStep = rayDir * stepSize;

                // Smoothed accumulation
                float3 scatteredLight = float3(0, 0, 0);
                float transmittance = 1.0;
                int actualSteps = 0;

                // Add temporal filtering during ray marching
                float timeSmoothing = sin(_Time.y * 0.1) * 0.01; // Very subtle temporal variation

                for (int step = 0; step < _StepCount; step++)
                {
                    float currentDistance = length(currentPos - rayStart);
                    if (currentDistance >= rayLength || transmittance < 0.01)
                        break;
                    
                    // Sample density with slight temporal smoothing
                    float3 smoothedPos = currentPos + float3(timeSmoothing, 0, timeSmoothing);
                    float density = GetCloudDensity(smoothedPos);
                    
                    if (density > 0.001)
                    {
                        // Smoother phase function
                        float phase = GetCloudPhaseFunction(_LightDirection, rayDir);
                        
                        // Smooth light attenuation
                        float lightAttenuation = exp(-density * stepSize * 1.0); // Reduced attenuation
                        
                        // Gentler lighting
                        float3 ambient = _AmbientLighting * _LightColor.rgb * 0.5;
                        float3 direct = _LightColor.rgb * _LightIntensity * phase * lightAttenuation * 0.3;
                        
                        float3 lightContribution = (ambient + direct) * density * _ScatteringCoefficient;
                        
                        // Smoother accumulation
                        scatteredLight += lightContribution * transmittance * stepSize;
                        transmittance *= exp(-density * stepSize * 0.8); // Reduced extinction
                    }
                    
                    currentPos += rayStep;
                    actualSteps++;
                }
                
                // Debug step count
                if (_ShowStepCount == 1)
                {
                    float stepRatio = float(actualSteps) / float(_StepCount);
                    if (stepRatio < 0.25) return fixed4(stepRatio * 4, 0, 0, 1);
                    else if (stepRatio < 0.5) return fixed4(1, (stepRatio - 0.25) * 4, 0, 1);
                    else if (stepRatio < 0.75) return fixed4(1 - (stepRatio - 0.5) * 4, 1, 0, 1);
                    else return fixed4(0, 1, (stepRatio - 0.75) * 4, 1);
                }
                
                // Final composition
                float3 currentResult = sceneColor.rgb * transmittance + scatteredLight;
                
                // Temporal accumulation
                if (_UseTemporalAccumulation == 1)
                {
                    float3 cloudCenter = rayStart + rayDir * ((_CloudBaseHeight + _CloudTopHeight) * 0.5);
                    float2 prevUV = GetPreviousScreenPos(cloudCenter);
                    
                    bool validReproject = (prevUV.x >= 0.01 && prevUV.x <= 0.99 && prevUV.y >= 0.01 && prevUV.y <= 0.99);
                    
                    if (validReproject)
                    {
                        float3 previousResult = tex2D(_PreviousFrame, prevUV).rgb;
                        currentResult = lerp(currentResult, previousResult, _TemporalBlendFactor);
                    }
                }
                
                if (_ShowFogOnly == 1)
                {
                    return fixed4(scatteredLight, 1.0);
                }
                
                return fixed4(currentResult, 1.0);
            }
            ENDCG
        }
    }
}