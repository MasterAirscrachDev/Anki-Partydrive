Shader "Custom/TrackMain"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #include "UnityCG.cginc"
        #include "UnityPBSLighting.cginc"
        // Use a custom lighting model without specular
        #pragma surface surf StandardNoSpecular fullforwardshadows
        #pragma target 3.0

        // Define a custom lighting model that disables specular highlights
        half4 LightingStandardNoSpecular(SurfaceOutputStandard s, half3 lightDir, half3 viewDir, half atten) {
            half NdotL = saturate(dot(s.Normal, lightDir));
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * NdotL * atten;
            c.a = s.Alpha;
            return c;
        }

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
