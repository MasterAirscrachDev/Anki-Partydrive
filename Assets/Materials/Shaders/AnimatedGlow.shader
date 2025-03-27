Shader "Unlit/AnimatedGlow"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Thickness ("Ring Thickness", Range(0.01,0.5)) = 0.1
        _Gap ("Ring Gap", Range(0.01,0.5)) = 0.2
        _Speed ("Expansion Speed", Range(0,0.5)) = 1.0
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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD2;
            };

            float4 _Color;
            float _Thickness;
            float _Gap;
            float _Speed;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate distance from center (0.5, 0.5) to current UV
                float2 center = float2(0.5, 0.5);
                float2 dir = i.uv - center;
                
                // Calculate distance from center
                float dist = length(dir);
                
                // Apply animation - expanding rings by offsetting distance with time
                float animatedDist = frac(dist - _Time.y * _Speed);
                
                // Create ring pattern with thickness and gaps
                float ringPattern = frac(animatedDist / (_Thickness + _Gap));
                
                // If in the ring, use _Color, otherwise render black
                fixed4 col = ringPattern < (_Thickness / (_Thickness + _Gap)) ? _Color : fixed4(0, 0, 0, 1);
                
                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
