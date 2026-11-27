Shader "Universal Render Pipeline/RockAndHardPlace/Pixel Soil"
{
    Properties
    {
        _MainTex("Tile", 2D) = "white" {}
        _Tint("Tint", Color) = (1,1,1,1)
        _GrassEdge("Grass Edge", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _GrassEdge;
            CBUFFER_END
            struct Input { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varying { float4 positionCS : SV_POSITION; float2 world : TEXCOORD0; float2 uv : TEXCOORD1; half4 color : COLOR; };
            Varying Vert(Input input)
            {
                Varying output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(world);
                output.world = world.xy;
                output.uv = input.uv;
                output.color = input.color * _Tint;
                return output;
            }
            float Grain(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            half4 Frag(Varying input) : SV_Target
            {
                // Match the 21px tiles at half a world unit: no smooth noise or tile seams.
                float2 pixel = floor(input.world * 42);
                float grain = (Grain(pixel) - 0.5) * 0.035 + (Grain(floor(pixel / 5)) - 0.5) * 0.016;
                float surface = clamp(abs(input.world.x) - 6, -3, 0);
                float belowSurface = max(0, surface - input.world.y);
                half3 color = lerp(half3(0.59, 0.52, 0.35), half3(0.34, 0.34, 0.28), saturate(belowSurface / 2));
                color = lerp(color, half3(0.22, 0.25, 0.23), saturate((belowSurface - 2) / 5)) + grain;
                half4 tile = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                // Keep the artist's grass pixels and slope cutout, while making their dirt
                // meet the interior seamlessly instead of exposing square brown tile bands.
                if (_GrassEdge > 0.5 && tile.g > tile.r + 0.015)
                    color = tile.rgb * input.color.rgb;
                return half4(color, tile.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
