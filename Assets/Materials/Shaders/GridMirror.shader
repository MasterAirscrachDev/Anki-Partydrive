Shader "Custom/GridMirror"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Color2 ("Grid Color", Color) = (0,0,0,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Gridsize ("Grid Size", Range(0,0.001)) = 0.0
        _GridX ("Grid X Offset", Range(0,0.001)) = 0.0
        _GridY ("Grid Y Offset", Range(0,0.001)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

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
        half _Metallic;
        half _Gridsize;
        half _GridX;
        half _GridY;
        fixed4 _Color;
        fixed4 _Color2;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Adjust UVs for grid offset
            float2 uv = IN.uv_MainTex;
            uv.x += _GridX;
            uv.y += _GridY;

            // Calculate grid pattern
            float2 gridPosition = frac(uv / _Gridsize);
            bool isOnGridLine = gridPosition.x < 0.05 || gridPosition.y < 0.05;

            // Mix grid color and base color based on grid line presence
            fixed4 gridColor = isOnGridLine ? _Color2 : _Color;

            // Existing lighting calculations...
            o.Albedo = gridColor.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = gridColor.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
