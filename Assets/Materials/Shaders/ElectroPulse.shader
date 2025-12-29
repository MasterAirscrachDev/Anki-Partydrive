Shader "Unlit/ElectroPulse"
{
    Properties
    {
        _Color ("Lightning Color", Color) = (0.2, 0.6, 1.0, 1.0)
        _Speed ("Animation Speed", Range(0.1, 5.0)) = 1.0
        _Scale ("Lightning Scale", Range(1.0, 20.0)) = 8.0
        _Intensity ("Lightning Intensity", Range(0.1, 3.0)) = 1.5
        _FadeRadius ("Fade Radius", Range(0.1, 1.0)) = 0.7
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.2
        _BoltCount ("Bolt Count", Range(1, 8)) = 4
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            float _Speed;
            float _Scale;
            float _Intensity;
            float _FadeRadius;
            float _EdgeSoftness;
            float _BoltCount;

            // Simple hash function for noise
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            // 2D noise function
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Fractal noise for more detail
            float fbm(float2 p)
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5; // Center the UVs
                float dist = length(uv);
                float angle = atan2(uv.y, uv.x);
                
                // Animated time
                float time = _Time.y * _Speed;
                
                // Create multiple lightning bolts
                float lightning = 0.0;
                for(int b = 0; b < _BoltCount; b++)
                {
                    float boltAngle = (b / _BoltCount) * 6.28318; // 2*PI
                    float angleOffset = abs(fmod(angle - boltAngle + 3.14159, 6.28318) - 3.14159);
                    
                    // Create branching lightning pattern
                    float2 boltUV = float2(dist * _Scale, angleOffset * 20.0 + time);
                    float boltNoise = fbm(boltUV);
                    
                    // Add animated variation
                    float2 animUV = float2(dist * 3.0 + time, angleOffset * 10.0);
                    float anim = noise(animUV);
                    
                    // Sharp lightning bolt
                    float bolt = pow(boltNoise * anim, 3.0);
                    lightning += bolt;
                }
                
                // Add electric pulse waves
                float pulse = sin(dist * 15.0 - time * 3.0) * 0.5 + 0.5;
                float2 pulseUV = float2(angle * 2.0, time * 0.5);
                pulse *= noise(pulseUV);
                
                lightning += pulse * 0.3;
                lightning *= _Intensity;
                
                // Fade out towards center
                float fadeMask = smoothstep(_FadeRadius - _EdgeSoftness, _FadeRadius, dist);
                
                // Also fade at outer edge
                float outerFade = 1.0 - smoothstep(0.45, 0.5, dist);
                
                float finalMask = fadeMask * outerFade;
                
                // Apply color and alpha
                float4 col = _Color;
                col.rgb *= lightning;
                col.a *= lightning * finalMask;
                
                // Add glow
                col.rgb += col.rgb * 0.5;
                
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
