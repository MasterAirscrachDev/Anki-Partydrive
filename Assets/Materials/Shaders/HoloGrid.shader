Shader "Custom/HoloGrid"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _EdgeColor ("Edge Color", Color) = (0,0,0,1)
        _EdgeWidth ("Edge Width", Range(0,1)) = 0.1
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        // First pass - the main mesh
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #pragma target 3.0

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float3 viewDir;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = _Color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = _Color.a;
        }
        ENDCG

        // Second pass - edge detection with wireframe (back faces)
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2g {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct g2f {
                float4 pos : SV_POSITION;
                float3 barycentric : TEXCOORD0;
            };

            v2g vert (appdata v) {
                v2g o;
                o.vertex = v.vertex;
                o.normal = v.normal;
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream) {
                g2f o;
                
                // Pass barycentric coordinates to fragment shader
                o.pos = UnityObjectToClipPos(IN[0].vertex);
                o.barycentric = float3(1, 0, 0);
                triStream.Append(o);
                
                o.pos = UnityObjectToClipPos(IN[1].vertex);
                o.barycentric = float3(0, 1, 0);
                triStream.Append(o);
                
                o.pos = UnityObjectToClipPos(IN[2].vertex);
                o.barycentric = float3(0, 0, 1);
                triStream.Append(o);
            }

            fixed4 _EdgeColor;
            float _EdgeWidth;

            fixed4 frag (g2f i) : SV_Target {
                // Calculate distance from any edge
                float3 distanceToEdges = i.barycentric;
                
                // Find minimum distance to an edge
                float minDistance = min(min(distanceToEdges.x, distanceToEdges.y), distanceToEdges.z);
                
                // Create sharp falloff for edges
                float edgeFactor = smoothstep(0, _EdgeWidth, minDistance);
                
                // Make the edges visible and everything else transparent
                fixed4 col = _EdgeColor;
                col.a = 1.0 - edgeFactor;
                
                return col;
            }
            ENDCG
        }
        
        // Third pass - edge detection with wireframe (front faces)
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2g {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct g2f {
                float4 pos : SV_POSITION;
                float3 barycentric : TEXCOORD0;
            };

            v2g vert (appdata v) {
                v2g o;
                o.vertex = v.vertex;
                o.normal = v.normal;
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream) {
                g2f o;
                
                // Pass barycentric coordinates to fragment shader
                o.pos = UnityObjectToClipPos(IN[0].vertex);
                o.barycentric = float3(1, 0, 0);
                triStream.Append(o);
                
                o.pos = UnityObjectToClipPos(IN[1].vertex);
                o.barycentric = float3(0, 1, 0);
                triStream.Append(o);
                
                o.pos = UnityObjectToClipPos(IN[2].vertex);
                o.barycentric = float3(0, 0, 1);
                triStream.Append(o);
            }

            fixed4 _EdgeColor;
            float _EdgeWidth;

            fixed4 frag (g2f i) : SV_Target {
                // Calculate distance from any edge
                float3 distanceToEdges = i.barycentric;
                
                // Find minimum distance to an edge
                float minDistance = min(min(distanceToEdges.x, distanceToEdges.y), distanceToEdges.z);
                
                // Create sharp falloff for edges
                float edgeFactor = smoothstep(0, _EdgeWidth, minDistance);
                
                // Make the edges visible and everything else transparent
                fixed4 col = _EdgeColor;
                col.a = 1.0 - edgeFactor;
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
