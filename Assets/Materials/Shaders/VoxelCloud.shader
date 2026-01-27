Shader "Unlit/VoxelCloud"
{
    Properties
    {
        _CloudColor ("Cloud Color", Color) = (1, 1, 1, 1)
        _CloudDensity ("Cloud Density", Range(0, 5)) = 0.5
        _VoxelSize ("Voxel Size", Range(0.01, 0.5)) = 0.1
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 2
        _Steps ("Ray Steps", Range(8, 128)) = 64
        _CloudThreshold ("Cloud Threshold", Range(0, 1)) = 0.3
        _AnimSpeed ("Animation Speed", Range(0, 2)) = 0.5
        _EdgeFalloff ("Edge Falloff", Range(0.1, 5)) = 2.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Front

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
                float3 worldPos : TEXCOORD0;
                float3 objectPos : TEXCOORD1;
            };

            float4 _CloudColor;
            float _CloudDensity;
            float _VoxelSize;
            float _NoiseScale;
            int _Steps;
            float _CloudThreshold;
            float _AnimSpeed;
            float _EdgeFalloff;

            // Simple 3D noise function
            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise(float3 x)
            {
                float3 p = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(lerp(hash(p + float3(0, 0, 0)), hash(p + float3(1, 0, 0)), f.x),
                         lerp(hash(p + float3(0, 1, 0)), hash(p + float3(1, 1, 0)), f.x), f.y),
                    lerp(lerp(hash(p + float3(0, 0, 1)), hash(p + float3(1, 0, 1)), f.x),
                         lerp(hash(p + float3(0, 1, 1)), hash(p + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            // Fractal Brownian Motion for cloud-like noise
            float fbm(float3 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            // Voxelize position
            float3 voxelize(float3 p, float voxelSize)
            {
                return floor(p / voxelSize) * voxelSize;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.objectPos = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Ray setup
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = normalize(i.worldPos - rayOrigin);
                
                // Transform to object space
                float3 objRayOrigin = mul(unity_WorldToObject, float4(rayOrigin, 1)).xyz;
                float3 objRayDir = normalize(mul(unity_WorldToObject, float4(rayDir, 0)).xyz);
                
                // Find intersection with cube bounds
                float3 boxMin = float3(-0.5, -0.5, -0.5);
                float3 boxMax = float3(0.5, 0.5, 0.5);
                
                float3 invDir = 1.0 / objRayDir;
                float3 t0 = (boxMin - objRayOrigin) * invDir;
                float3 t1 = (boxMax - objRayOrigin) * invDir;
                
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);
                
                float tNear = max(max(tmin.x, tmin.y), tmin.z);
                float tFar = min(min(tmax.x, tmax.y), tmax.z);
                
                if(tNear > tFar || tFar < 0) discard;
                
                tNear = max(0, tNear);
                
                // Raymarch through volume
                float stepSize = (tFar - tNear) / float(_Steps);
                float3 pos = objRayOrigin + objRayDir * tNear;
                
                float4 accumulatedColor = float4(0, 0, 0, 0);
                float transmittance = 1.0;
                
                float timeOffset = _Time.y * _AnimSpeed;
                
                for(int step = 0; step < _Steps; step++)
                {
                    if(transmittance < 0.01) break;
                    
                    // Voxelize the sample position
                    float3 voxelPos = voxelize(pos, _VoxelSize);
                    
                    // Calculate distance from center for edge falloff (cube-shaped)
                    float distFromCenter = max(max(abs(pos.x), abs(pos.y)), abs(pos.z));
                    float edgeFalloff = 1.0 - pow(distFromCenter * 2.0, _EdgeFalloff);
                    edgeFalloff = saturate(edgeFalloff);
                    
                    // Sample noise at voxelized position with animation
                    float density = fbm((voxelPos + float3(timeOffset, timeOffset * 0.5, 0)) * _NoiseScale);
                    
                    // Apply threshold to create cloud shapes (tighter range for more solid cloud)
                    density = smoothstep(_CloudThreshold, _CloudThreshold + 0.1, density);
                    density *= _CloudDensity;
                    
                    // Apply edge falloff to keep cloud centered
                    density *= edgeFalloff;
                    
                    // Accumulate color
                    float alpha = density * stepSize * 2.0;
                    alpha = saturate(alpha);
                    
                    float3 cloudCol = _CloudColor.rgb;
                    accumulatedColor.rgb += cloudCol * alpha * transmittance;
                    accumulatedColor.a += alpha * transmittance;
                    transmittance *= (1.0 - alpha);
                    
                    pos += objRayDir * stepSize;
                }
                
                accumulatedColor.a = saturate(accumulatedColor.a);
                return accumulatedColor;
            }
            ENDCG
        }
    }
}
