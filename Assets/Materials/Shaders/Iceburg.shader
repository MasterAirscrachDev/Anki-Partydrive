Shader "Custom/Iceburg"
{
    Properties
    {
        _IceColor ("Ice Color", Color) = (0.4, 0.7, 0.9, 0.5)
        _DeepColor ("Deep Ice Color", Color) = (0.1, 0.3, 0.6, 0.7)
        _FrostColor ("Frost Color", Color) = (0.9, 0.95, 1.0, 1.0)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.85
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _FresnelPower ("Edge Frost Width", Range(0.5, 6)) = 2.5
        _FresnelStrength ("Edge Frost Strength", Range(0, 2)) = 1.2
        _PeakFrost ("Peak Frost Amount", Range(0, 1)) = 0.7
        _Transparency ("Transparency", Range(0, 1)) = 0.55
        _EmissionStrength ("Emission Strength", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldNormal;
            float3 worldPos;
        };

        half _Glossiness;
        fixed4 _IceColor;
        fixed4 _DeepColor;
        fixed4 _FrostColor;
        half _FresnelPower;
        half _FresnelStrength;
        half _PeakFrost;
        half _Transparency;
        half _EmissionStrength;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);

            // Fresnel rim for frosted edges
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), IN.worldNormal));
            half fresnel = pow(rim, _FresnelPower) * _FresnelStrength;

            // Peak frost: surfaces facing upward get more white frost
            half upFacing = saturate(dot(IN.worldNormal, float3(0, 1, 0)));
            half peakFrost = pow(upFacing, 1.5) * _PeakFrost;

            // Blend between deep ice and surface ice based on view angle
            fixed3 iceBlend = lerp(_DeepColor.rgb, _IceColor.rgb, rim * 0.5 + 0.3);

            // Layer frost on top at edges and peaks
            half totalFrost = saturate(fresnel + peakFrost);
            fixed3 finalColor = lerp(iceBlend, _FrostColor.rgb, totalFrost) * tex.rgb;

            o.Albedo = finalColor;
            o.Emission = finalColor * _EmissionStrength;
            o.Smoothness = lerp(_Glossiness, 0.3, totalFrost); // frost is rougher
            o.Alpha = lerp(_Transparency, 1.0, totalFrost); // frost is opaque, ice is translucent
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
