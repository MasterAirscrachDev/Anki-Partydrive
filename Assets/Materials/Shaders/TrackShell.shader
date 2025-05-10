Shader "Custom/TrackShell"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Color ("Color", Color) = (1,1,1,1)

        _ShellTexture ("Shell Texture", 2D) = "white" {}
        _ShellCount ("Shell Count", Range(1, 10)) = 3
        _ShellAlphaCutoff ("Shell Alpha Cutoff", Range(0, 1)) = 0.5
        _ShellEmmission ("Shell Emission", Color) = (1,1,1,1)
        _ShellEmmissionStrength ("Shell Emission Strength", Range(0, 20)) = 0.0
        _ShellHeight ("Shell Height", Range(0.0, 1.0)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        // Base layer pass
        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG

        // Shell layers pass
        Pass {
            Tags {"LightMode"="ForwardBase"}
            Cull Off
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            // Properties for the shell effect
            // sampler2D _ShellTexture;
            // float _ShellCount;
            // float _ShellAlphaCutoff;
            // float4 _ShellEmmission;
            // float _ShellHeight;

            // struct appdata {
            //     float4 vertex : POSITION;
            //     float3 normal : NORMAL;
            //     float2 texcoord : TEXCOORD0;
            // };

            // struct v2f {
            //     float2 uv : TEXCOORD0;
            //     UNITY_FOG_COORDS(1)
            //     float4 vertex : SV_POSITION;
            //     float3 worldNormal : TEXCOORD2;
            //     float3 worldPos : TEXCOORD3;
            // };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            // Create shell instances in the vertex shader
            // v2f vert(appdata v, uint instanceID : SV_InstanceID) {
            //     v2f o;
            //     float shellLayer = instanceID / (_ShellCount - 1.0);
                
            //     // Displace vertex along normal by layer height
            //     float3 displaceDir = normalize(v.normal);
            //     float displaceAmount = shellLayer * _ShellHeight;
            //     float4 displacedVertex = v.vertex + float4(displaceDir * displaceAmount, 0);
                
            //     o.vertex = UnityObjectToClipPos(displacedVertex);
            //     o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                
            //     // Calculate normal and position for lighting
            //     o.worldNormal = UnityObjectToWorldNormal(v.normal);
            //     o.worldPos = mul(unity_ObjectToWorld, displacedVertex).xyz;
                
            //     UNITY_TRANSFER_FOG(o,o.vertex);
            //     return o;
            // }
            
            // fixed4 frag(v2f i) : SV_Target {
            //     // Sample shell texture
            //     fixed4 shellColor = tex2D(_ShellTexture, i.uv);
                
            //     // Alpha cutoff for the shell effect
            //     clip(shellColor.a - _ShellAlphaCutoff);
                
            //     // Simple lighting for the shell
            //     half nl = max(0, dot(i.worldNormal, _WorldSpaceLightPos0.xyz));
            //     fixed3 lighting = nl * _LightColor0.rgb + unity_AmbientSky.rgb;
                
            //     // Combine shell color with emission and lighting
            //     fixed4 col = fixed4(shellColor.rgb * _ShellEmmission.rgb * lighting, shellColor.a);
                
            //     UNITY_APPLY_FOG(i.fogCoord, col);
            //     return col;
            // }
            ENDCG
        }
    }
    
    // Add multiple passes for each shell layer
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        CGINCLUDE
        #include "UnityCG.cginc"
        #include "UnityLightingCommon.cginc"
        
        // Add back the property declarations - these are needed in each shader program
        sampler2D _ShellTexture;
        float _ShellCount;
        float _ShellAlphaCutoff;
        float4 _ShellEmmission;
        float _ShellEmmissionStrength;
        float _ShellHeight;
        
        struct appdata {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float2 texcoord : TEXCOORD0;
        };
        
        struct v2f {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
            float shellLayer : TEXCOORD1;
            float3 worldNormal : TEXCOORD2;
            float3 worldPos : TEXCOORD3;
        };
        
        v2f vert(appdata v, uint instanceID : SV_InstanceID)
        {
            v2f o;
            float shellLayer = (instanceID + 1.0) / _ShellCount;
            
            // Displace vertex along normal
            float3 displaceDir = normalize(v.normal);
            float displaceAmount = shellLayer * _ShellHeight;
            float4 displacedVertex = v.vertex + float4(displaceDir * displaceAmount, 0);
            
            o.vertex = UnityObjectToClipPos(displacedVertex);
            o.uv = v.texcoord;
            o.shellLayer = shellLayer;
            
            // For lighting
            o.worldNormal = UnityObjectToWorldNormal(v.normal);
            o.worldPos = mul(unity_ObjectToWorld, displacedVertex).xyz;
            
            return o;
        }
        
        fixed4 frag(v2f i) : SV_Target
        {
            // Sample shell texture
            fixed4 shellColor = tex2D(_ShellTexture, i.uv);
            
            // Apply alpha cutoff
            clip(shellColor.a - _ShellAlphaCutoff * (1.0 - i.shellLayer));
            
            // Simple lighting
            half nl = max(0, dot(i.worldNormal, _WorldSpaceLightPos0.xyz));
            fixed3 lighting = nl * _LightColor0.rgb + unity_AmbientSky.rgb;
            
            // Final color with emission
            return fixed4(shellColor.rgb * _ShellEmmission.rgb * lighting * _ShellEmmissionStrength, shellColor.a);
        }
        ENDCG
        
        // Generate shell layers using instancing
        Pass {
            Tags {"LightMode"="ForwardBase"}
            Cull Off
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            
            void setup() {
                unity_ObjectToWorld = unity_ObjectToWorld; // No change needed here
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
