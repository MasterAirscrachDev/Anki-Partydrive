Shader "Unlit/TrackShell"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EmissiveColor ("Emissive Color", Color) = (1,1,1,1)
        _EmissionStrength ("Emission Strength", Float) = 1.0
        _ShellColor      ("Shell Color",         Color) = (1,1,1,1)
        _ShellOffset     ("Shell Offset",        Float) = 0.01
        _ShellThickness  ("Shell Thickness",     Float) = 0.01
        _Cutoff          ("Alpha Cutoff",        Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Cutout" }
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
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _EmissiveColor;
            float _EmissionStrength;
            float _Cutoff;
            float _ShellOffset;

            v2f vert (appdata v)
            {
                v2f o;
                float3 wN = UnityObjectToWorldNormal(v.normal);
                float3 wP = mul(unity_ObjectToWorld, v.vertex).xyz + wN * _ShellOffset;
                o.vertex = UnityWorldToClipPos(float4(wP,1));
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * _EmissiveColor * _EmissionStrength;
                if (col.a < _Cutoff) discard;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
        // -- Shell pass: extrude along normals for a fading shell --
        Pass
        {
            Name "Shell"
            Tags { "LightMode"="Always" }
            Cull Back
            CGPROGRAM
            #pragma vertex vertShell
            #pragma fragment fragShell
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4   _MainTex_ST;
            fixed4   _ShellColor;
            float    _ShellOffset;
            float    _ShellThickness;
            float    _EmissionStrength;
            float _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };
            struct v2f
            {
                float2 uv       : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex   : SV_POSITION;
            };

            v2f vertShell(appdata v)
            {
                v2f o;
                float3 wN = UnityObjectToWorldNormal(v.normal);
                float3 wP = mul(unity_ObjectToWorld, v.vertex).xyz + wN * _ShellOffset * _ShellThickness;
                o.vertex = UnityWorldToClipPos(float4(wP,1));
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 fragShell(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _ShellColor * _EmissionStrength;
                if (col.a < _Cutoff) discard;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
        // -- Shell pass: extrude along normals for a fading shell --
        Pass
        {
            Name "Shell"
            Tags { "LightMode"="Always" }
            Cull Back
            CGPROGRAM
            #pragma vertex vertShell
            #pragma fragment fragShell
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4   _MainTex_ST;
            fixed4   _ShellColor;
            float    _ShellOffset;
            float    _ShellThickness;
            float    _EmissionStrength;
            float _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };
            struct v2f
            {
                float2 uv       : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex   : SV_POSITION;
            };

            v2f vertShell(appdata v)
            {
                v2f o;
                float3 wN = UnityObjectToWorldNormal(v.normal);
                float3 wP = mul(unity_ObjectToWorld, v.vertex).xyz + wN * _ShellOffset * (_ShellThickness * 2);
                o.vertex = UnityWorldToClipPos(float4(wP,1));
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 fragShell(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _ShellColor * _EmissionStrength;
                if (col.a < _Cutoff) discard;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
        // -- Shell pass: extrude along normals for a fading shell --
        Pass
        {
            Name "Shell"
            Tags { "LightMode"="Always" }
            Cull Back
            CGPROGRAM
            #pragma vertex vertShell
            #pragma fragment fragShell
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4   _MainTex_ST;
            fixed4   _ShellColor;
            float    _ShellOffset;
            float    _ShellThickness;
            float    _EmissionStrength;
            float _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };
            struct v2f
            {
                float2 uv       : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex   : SV_POSITION;
            };

            v2f vertShell(appdata v)
            {
                v2f o;
                float3 wN = UnityObjectToWorldNormal(v.normal);
                float3 wP = mul(unity_ObjectToWorld, v.vertex).xyz + wN * _ShellOffset * (_ShellThickness * 3);
                o.vertex = UnityWorldToClipPos(float4(wP,1));
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 fragShell(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _ShellColor * _EmissionStrength;
                if (col.a < _Cutoff) discard;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
        // -- Shell pass: extrude along normals for a fading shell --
        Pass
        {
            Name "Shell"
            Tags { "LightMode"="Always" }
            Cull Back
            CGPROGRAM
            #pragma vertex vertShell
            #pragma fragment fragShell
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4   _MainTex_ST;
            fixed4   _ShellColor;
            float    _ShellOffset;
            float    _ShellThickness;
            float    _EmissionStrength;
            float _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };
            struct v2f
            {
                float2 uv       : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex   : SV_POSITION;
            };

            v2f vertShell(appdata v)
            {
                v2f o;
                float3 wN = UnityObjectToWorldNormal(v.normal);
                float3 wP = mul(unity_ObjectToWorld, v.vertex).xyz + wN * _ShellOffset * (_ShellThickness * 4);
                o.vertex = UnityWorldToClipPos(float4(wP,1));
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 fragShell(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _ShellColor * _EmissionStrength;
                if (col.a < _Cutoff) discard;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
