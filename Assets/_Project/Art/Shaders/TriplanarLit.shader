// Triplanar texture with simple URP lighting, for models without UVs.
// Projected in object space (scaled to world units) so the pattern sticks to moving parts such as
// turbine sails and mill wheels, while tiling stays in tiles per meter whatever the model's scale.
Shader "EcoGame/TriplanarLit"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _Tiling ("Tiles Per Meter", Float) = 0.5
        _BlendSharpness ("Blend Sharpness", Range(1, 16)) = 6
        _Smoothness ("Smoothness", Range(0, 1)) = 0.2
        _SpecularStrength ("Specular Strength", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _Tiling;
                float _BlendSharpness;
                half _Smoothness;
                half _SpecularStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                float3 projectPos : TEXCOORD3;
                float3 projectNormal : TEXCOORD4;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(position.positionCS.z);

                // Object space scaled by the object's world scale: moves with the object, keeps meter-sized tiles.
                float3 objectScale = float3(
                    length(GetObjectToWorldMatrix()._m00_m10_m20),
                    length(GetObjectToWorldMatrix()._m01_m11_m21),
                    length(GetObjectToWorldMatrix()._m02_m12_m22));
                output.projectPos = input.positionOS.xyz * objectScale;
                output.projectNormal = input.normalOS;
                return output;
            }

            half3 SampleTriplanar(float3 projectPos, float3 projectNormal)
            {
                float3 weights = pow(abs(normalize(projectNormal)), _BlendSharpness);
                weights /= max(weights.x + weights.y + weights.z, 1e-4);
                float3 p = projectPos * _Tiling;
                half3 x = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, p.zy).rgb;
                half3 y = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, p.xz).rgb;
                half3 z = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, p.xy).rgb;
                return x * weights.x + y * weights.y + z * weights.z;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                half3 albedo = SampleTriplanar(input.projectPos, input.projectNormal) * _BaseColor.rgb;

                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half ndotl = saturate(dot(normalWS, light.direction));
                half3 diffuse = albedo * light.color * ndotl * light.shadowAttenuation;

                // Soft plastic sheen: a low, wide highlight instead of a glossy one.
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 halfDir = normalize(light.direction + viewDir);
                half specPower = exp2(10 * _Smoothness + 1);
                half spec = pow(saturate(dot(normalWS, halfDir)), specPower) * _SpecularStrength * light.shadowAttenuation;

                half3 ambient = albedo * SampleSH(normalWS);
                half3 color = diffuse + ambient + spec * light.color;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    FallBack "Universal Render Pipeline/Lit"
}
