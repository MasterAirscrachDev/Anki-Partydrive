Shader "Unlit/HazardZoneFire"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)
        _FadeStart ("Fade Start Height", Float) = 0
        _FadeEnd ("Fade End Height", Float) = 2
        _FireTex ("Fire Texture", 2D) = "white" {}
        _EmissionStrength ("Emission Strength", Range(0, 5)) = 2.0
        _NoiseScale ("Noise Scale", Float) = 5.0
        _NoiseSpeed ("Noise Speed", Float) = 1.5
        _ScrollSpeed ("Fire Scroll Speed", Float) = 1.0
        _DistortionStrength ("Distortion Strength", Range(0, 0.5)) = 0.1
        _FireCoreColor ("Fire Core Color", Color) = (1, 0.8, 0.2, 1)
        _FireEdgeColor ("Fire Edge Color", Color) = (1, 0.15, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            float _NoiseScale;
            float _NoiseSpeed;
            float _ScrollSpeed;
            float _DistortionStrength;

            // Hash & smooth noise
            float Hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }
            float Noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep for smoother interpolation
                return lerp(
                    lerp(Hash(i),              Hash(i + float2(1,0)), u.x),
                    lerp(Hash(i + float2(0,1)), Hash(i + float2(1,1)), u.x),
                    u.y);
            }

            // Fractional Brownian Motion — 4 octaves for organic fire shape
            float FBM(float2 uv)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 4; k++)
                {
                    v += a * Noise(uv);
                    uv  = uv * 2.0 + float2(1.7, 9.2);
                    a  *= 0.5;
                }
                return v;
            }

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
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _FadeStart;
            float _FadeEnd;

            sampler2D _FireTex;
            float4 _FireTex_ST;
            float _EmissionStrength;
            fixed4 _FireCoreColor;
            fixed4 _FireEdgeColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y;

                // Height factor: 1 at base (_FadeStart), 0 at tip (_FadeEnd)
                float heightFactor = 1.0 - saturate((i.worldPos.y - _FadeStart) / (_FadeEnd - _FadeStart));

                // Two offset FBM layers scrolling upward for horizontal distortion
                float2 noiseUV = i.uv * _NoiseScale;
                float noiseA = FBM(noiseUV + float2(0.0,  -time * _NoiseSpeed));
                float noiseB = FBM(noiseUV + float2(3.17, -time * _NoiseSpeed * 0.85));

                // Fire UV: scroll upward + wobble horizontally
                float2 fireUV  = i.uv;
                fireUV.y -= time * _ScrollSpeed;
                fireUV.x += (noiseA - noiseB) * _DistortionStrength;

                fixed4 fireCol = tex2D(_FireTex, TRANSFORM_TEX(fireUV, _FireTex));

                // Base texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                // Fire intensity: shaped by FBM noise, shrinks toward the top
                float fireNoise   = saturate(noiseA * 1.6 - 0.15);
                float fireIntensity = saturate(fireNoise * heightFactor * heightFactor);

                // Color gradient: yellow-white core → orange-red edge
                float coreness = saturate(fireNoise * 2.0 - 0.5);
                fixed3 fireColorBlend = lerp(_FireEdgeColor.rgb, _FireCoreColor.rgb, coreness);

                // Add fire glow; also incorporate the fire texture's luminance
                float texLuminance = dot(fireCol.rgb, float3(0.299, 0.587, 0.114));
                col.rgb += fireColorBlend * _EmissionStrength * fireIntensity * (0.6 + texLuminance * 0.4);

                // Fade alpha upward + slight boost where fire burns bright
                float fadeAmount = saturate((i.worldPos.y - _FadeStart) / (_FadeEnd - _FadeStart));
                col.a *= (1.0 - fadeAmount);
                col.a  = saturate(col.a + fireIntensity * 0.25);

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
