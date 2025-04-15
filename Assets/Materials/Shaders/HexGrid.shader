Shader "Custom/HexagonalGrid"
{
    Properties
    {
        _Scale ("Scale", Float) = 5.0
        _BorderThickness ("Border Thickness", Range(0.01, 0.1)) = 0.03
        _BorderOffset ("Border Offset", Range(0.0, 0.1)) = 0.04
        _BorderColor ("Border Color", Color) = (0,1,1,1)
        _InnerColor ("Inner Color", Color) = (1,1,1,1)
        _PulseColor ("Pulse Color", Color) = (0.5,0.8,1,1)
        _PulseSpeed ("Pulse Speed", Range(0,5)) = 1.0
        _PulseAmount ("Pulse Amount", Range(0,10)) = 0.1
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
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Scale;
            float _BorderThickness;
            float _BorderOffset;
            float4 _BorderColor;
            float4 _InnerColor;
            float4 _PulseColor;
            float _PulseSpeed;
            float _PulseAmount;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = float2(o.worldPos.x, o.worldPos.z) * _Scale;
                return o;
            }
            
            // Uncomment this line for flat-top hexagons
            // #define FLAT_TOP_HEXAGON
            
            // Helper vector for regular triangles or hexagons
            #ifdef FLAT_TOP_HEXAGON
                static const float2 s = float2(1.7320508, 1);
            #else
                static const float2 s = float2(1, 1.7320508);
            #endif
            
            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(141.13, 289.97))) * 43758.5453);
            }
            
            // Hexagonal isosuface function
            float hex(float2 p)
            {    
                p = abs(p);
                
                #ifdef FLAT_TOP_HEXAGON
                    return max(dot(p, s * 0.5), p.y); // Flat-top hexagon
                #else
                    return max(dot(p, s * 0.5), p.x); // Pointy-top hexagon
                #endif    
            }
            
            // Returns hexagonal grid coordinate and cell ID
            float4 getHex(float2 p)
            {    
                #ifdef FLAT_TOP_HEXAGON
                    float4 hC = floor(float4(p, p - float2(1, 0.5)) / s.xyxy) + 0.5;
                #else
                    float4 hC = floor(float4(p, p - float2(0.5, 1)) / s.xyxy) + 0.5;
                #endif
                
                // Centering the coordinates with the hexagon centers
                float4 h = float4(p - hC.xy * s, p - (hC.zw + 0.5) * s);
                
                // Find nearest hexagon center
                return dot(h.xy, h.xy) < dot(h.zw, h.zw) 
                    ? float4(h.xy, hC.xy) 
                    : float4(h.zw, hC.zw + 0.5);
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Scale and animate the UV
                float3 h = getHex(i.uv * _Scale + s.yx);
                
                // Calculate edge distance
                float eDist = hex(h.xy);

                
                float2 gexPosition = floor(i.uv * _Scale + s.yx) * s.yx;
                float2 localPosition = frac(i.uv * _Scale + s.yx) * s.yx;

                // Create some variation based on cell position for visual interest
                float variation = frac(sin(dot(hexPosition, float2(12.9898, 78.233))) * 43758.5453);
                float timeOffset = variation * 0.5;
                
                // Create an improved wave-like pulse that travels across the material
                float wavePhase = (i.worldPos.x + i.worldPos.z) * 0.5 - _Time.y * _PulseSpeed;
                float distanceEffect = sin(wavePhase * 6.28318) * 0.5 + 0.5;
                float pulseAmount = distanceEffect * _PulseAmount * (0.8 + 0.2 * sin(_Time.y + variation * 6.28318));

                fixed4 trueColor = lerp(_InnerColor, _PulseColor, pulseAmount);

                
                // Create color with border
                float3 col = lerp(trueColor, _BorderColor.rgb,  smoothstep(0.0, _BorderThickness, eDist - 0.5 + _BorderOffset));
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}