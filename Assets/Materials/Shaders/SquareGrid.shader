Shader "Unlit/SquareGrid"
{
    Properties
    {
        _InnerColor ("Inner Color", Color) = (1,1,1,1)
        _OuterColor ("Outer Color", Color) = (0,0,0,1)
        _PulseColor ("Pulse Color", Color) = (0.5,0.8,1,1)
        _SquareSize ("Square Size", Range(0,50)) = 1.0
        _SquareSpacing ("Square Spacing", Range(0,1)) = 0.1
        _PulseSpeed ("Pulse Speed", Range(0,5)) = 1.0
        _PulseAmount ("Pulse Amount", Range(0,10)) = 0.1
        _TileScale ("Tile Scale", Vector) = (1,1,0,0)
        _AspectRatio ("Aspect Ratio Correction", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
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
                float3 worldPos : TEXCOORD2;
            };

            float4 _InnerColor;
            float4 _OuterColor;
            float _SquareSize;
            float _SquareSpacing;
            float _PulseSpeed;
            float _PulseAmount;
            float4 _PulseColor;
            float4 _MainTex_ST;
            float2 _TileScale;
            float _AspectRatio;
            float _UseWorldSpace;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                // Apply aspect ratio correction to avoid distortion on non-square UVs
                o.uv = float2(o.worldPos.x, o.worldPos.z) * _TileScale;
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate UV coordinates for the grid pattern - no need to multiply by _SquareSize here
                float2 uv = i.uv * _SquareSize;
                
                // Calculate grid cells
                float2 cellPosition = floor(uv);
                float2 localPosition = frac(uv);
                
                // Calculate grid lines based on spacing
                float lineWidth = _SquareSpacing * 0.5;
                bool isOnGridLine = localPosition.x < lineWidth || 
                                    localPosition.x > (1.0 - lineWidth) || 
                                    localPosition.y < lineWidth || 
                                    localPosition.y > (1.0 - lineWidth);
                
                // Create some variation based on cell position for visual interest
                float variation = frac(sin(dot(cellPosition, float2(12.9898, 78.233))) * 43758.5453);
                float timeOffset = variation * 0.5;
                
                // Create an improved wave-like pulse that travels across the material
                float wavePhase = (i.worldPos.x + i.worldPos.z) * 0.5 - _Time.y * _PulseSpeed;
                float distanceEffect = sin(wavePhase * 6.28318) * 0.5 + 0.5;
                float pulseAmount = distanceEffect * _PulseAmount * (0.8 + 0.2 * sin(_Time.y + variation * 6.28318));
                
                // Determine color based on grid line presence
                fixed4 color;
                if (isOnGridLine) {
                    color = _OuterColor;
                } else {
                    // For inner squares, interpolate between inner color and pulse color based on wave position
                    color = lerp(_InnerColor, _PulseColor, pulseAmount);
                }
                
                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, color);
                return color;
            }
            ENDCG
        }
        // Shadow casting pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            
            struct v2f
            {
                V2F_SHADOW_CASTER;
            };
            
            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
