Shader "Custom/OpacityNearEdges"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _PropEdgeSharpness ("Edge Sharpness", Range(1, 10)) = 5
    }
    SubShader
    {
        //Tags { "RenderType"="Transparent" }
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        //#pragma surface surf Standard fullforwardshadows
        #pragma surface surf Standard alpha

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        half _PropEdgeSharpness;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Create scrolling UV coordinates
            float2 scrolledUV = IN.uv_MainTex;
            scrolledUV.x += _Time.y * 0.1; // Offset X based on time

            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, scrolledUV) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            // Improved edge detection
            float edgeSharpness = _PropEdgeSharpness;
            float distToEdgeX = min(IN.uv_MainTex.x, 1.0 - IN.uv_MainTex.x) * edgeSharpness;
            float distToEdgeY = min(IN.uv_MainTex.y, 1.0 - IN.uv_MainTex.y) * edgeSharpness;
            float distToEdge = min(distToEdgeX, distToEdgeY);

            // Use an exponential function to make the edge detection more sensitive
            float edgeOpacity = smoothstep(0.2, 0.8, distToEdge);
            //for testing make the pixel white to black based on distance to edge
            //o.Albedo = float3(edgeOpacity, edgeOpacity, edgeOpacity);
            //o.Alpha = 1;
            edgeOpacity = 1 - edgeOpacity;
            o.Alpha = min(edgeOpacity, c.a); // Ensure we don't reduce the texture's inherent alpha
            o.Metallic = min(edgeOpacity, _Metallic); // Ensure we don't reduce the texture's inherent alpha

        }
        ENDCG
    }
    FallBack "Diffuse"
}
