Shader "Unlit/CircleImage"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RingColor ("Ring Color", Color) = (1,1,1,1)
        _OuterRadius ("Outer Radius", Range(0, 1)) = 0.5
        _Thickness ("Ring Thickness", Range(0, 1)) = 0.1
        _FillAmount ("Radial Fill", Range(0, 1)) = 1.0
        _ShadingStrength ("Shading Strength", Range(0, 1)) = 0.3
        _TextureOffset ("Texture Offset", Vector) = (0, 0, 0, 0)
        _TextureScale ("Texture Scale", Vector) = (1, 1, 1, 1)
        _TextureCutoffAngle ("Texture Cutoff Angle", Range(0, 1)) = 0.5
        _BackgroundRingColor ("Background Ring Color", Color) = (0.5,0.5,0.5,1)
        _BackgroundRingOffset ("Background Ring Offset", Vector) = (0, 0, 0, 0)
        _BackgroundFillColor ("Background Fill Color", Color) = (0.2,0.2,0.2,1)
        _BlinkColor ("Blink Color", Color) = (1,1,1,1)
        _BlinkSpeed ("Blink Speed", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _RingColor;
            float _OuterRadius;
            float _Thickness;
            float _FillAmount;
            float _ShadingStrength;
            float2 _TextureOffset;
            float2 _TextureScale;
            float _TextureCutoffAngle;
            fixed4 _BackgroundRingColor;
            float2 _BackgroundRingOffset;
            fixed4 _BackgroundFillColor;
            fixed4 _BlinkColor;
            float _BlinkSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Center the UV coordinates
                float2 center = float2(0.5, 0.5);
                float2 uv = i.uv - center;
                
                // Calculate distance from center
                float dist = length(uv);
                
                // Calculate inner radius
                float innerRadius = _OuterRadius - _Thickness;
                
                // Check if pixel is within ring bounds
                float ringMask = step(innerRadius, dist) * step(dist, _OuterRadius);
                
                // Calculate angle for radial fill (0 to 1, starting from top)
                float angle = atan2(uv.y, uv.x);
                angle = (angle + UNITY_PI) / (2.0 * UNITY_PI); // Normalize to 0-1
                angle = frac(angle + 0.25); // Rotate to start from top
                
                // Apply radial fill
                float fillMask = step(angle, _FillAmount);
                ringMask *= fillMask;
                
                // Sample texture with scale and offset
                float2 texUV = (i.uv - 0.5) * _TextureScale + 0.5 + _TextureOffset;
                fixed4 texColor = tex2D(_MainTex, texUV);
                
                // Calculate angle for texture cutoff (0-1, starting from top)
                float texAngle = atan2(uv.y, uv.x);
                texAngle = (texAngle + UNITY_PI) / (2.0 * UNITY_PI);
                texAngle = frac(texAngle + 0.25);
                
                // Determine which side based on cutoff angle
                bool textureOnTop = texAngle > _TextureCutoffAngle;
                
                // Basic shading based on angle to give depth
                float shadingAngle = atan2(uv.y, uv.x);
                // Create a gradient that simulates lighting from top-right
                float shading = sin(shadingAngle + UNITY_PI * 0.75) * 0.5 + 0.5;
                shading = lerp(1.0, shading, _ShadingStrength);
                
                // Calculate background ring with offset
                float2 bgUV = uv - _BackgroundRingOffset;
                float bgDist = length(bgUV);
                float bgRingMask = step(innerRadius, bgDist) * step(bgDist, _OuterRadius);
                
                // Background fill (inside the ring)
                float bgFillMask = step(bgDist, innerRadius);
                
                // Calculate blink using sine wave
                float blinkValue = 0.0;
                if (_BlinkSpeed > 0.0)
                {
                    blinkValue = sin(_Time.y * _BlinkSpeed * 6.28318) * 0.5 + 0.5; // 0 to 1
                }
                
                // Interpolate between fill color and blink color
                fixed4 currentFillColor = lerp(_BackgroundFillColor, _BlinkColor, blinkValue);
                currentFillColor.a *= bgFillMask;
                
                // Apply shading to background ring
                float bgShadingAngle = atan2(bgUV.y, bgUV.x);
                float bgShading = sin(bgShadingAngle + UNITY_PI * 0.75) * 0.5 + 0.5;
                bgShading = lerp(1.0, bgShading, _ShadingStrength);
                
                fixed4 shadedBgRing = _BackgroundRingColor;
                shadedBgRing.rgb *= bgShading;
                shadedBgRing.a *= bgRingMask;
                
                // Apply shading to main ring (preserve alpha)
                fixed4 shadedRing = _RingColor;
                shadedRing.rgb *= shading;
                shadedRing.a *= ringMask; // Ring only exists where mask is active
                
                // Build layers from bottom to top: fill -> bg ring -> texture -> main ring
                fixed4 finalColor = currentFillColor;
                
                // Blend background ring on top of fill
                finalColor.rgb = lerp(finalColor.rgb, shadedBgRing.rgb, shadedBgRing.a);
                finalColor.a = max(finalColor.a, shadedBgRing.a);
                
                // Blend texture - only affects areas where texture has alpha
                finalColor.rgb = lerp(finalColor.rgb, texColor.rgb, texColor.a);
                finalColor.a = max(finalColor.a, texColor.a);
                
                // Blend main ring based on cutoff angle
                if (textureOnTop)
                {
                    // Texture is on top, so ring goes between fill/bg and texture
                    // Rebuild: fill -> bg ring -> main ring -> texture
                    finalColor = currentFillColor;
                    finalColor.rgb = lerp(finalColor.rgb, shadedBgRing.rgb, shadedBgRing.a);
                    finalColor.a = max(finalColor.a, shadedBgRing.a);
                    finalColor.rgb = lerp(finalColor.rgb, shadedRing.rgb, shadedRing.a);
                    finalColor.a = max(finalColor.a, shadedRing.a);
                    finalColor.rgb = lerp(finalColor.rgb, texColor.rgb, texColor.a);
                    finalColor.a = max(finalColor.a, texColor.a);
                }
                else
                {
                    // Ring on top: fill -> bg ring -> texture -> main ring
                    finalColor = currentFillColor;
                    finalColor.rgb = lerp(finalColor.rgb, shadedBgRing.rgb, shadedBgRing.a);
                    finalColor.a = max(finalColor.a, shadedBgRing.a);
                    finalColor.rgb = lerp(finalColor.rgb, texColor.rgb, texColor.a);
                    finalColor.a = max(finalColor.a, texColor.a);
                    finalColor.rgb = lerp(finalColor.rgb, shadedRing.rgb, shadedRing.a);
                    finalColor.a = max(finalColor.a, shadedRing.a);
                }
                
                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return finalColor;
            }
            ENDCG
        }
    }
}
