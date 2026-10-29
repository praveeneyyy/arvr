Shader "MedicalSimulation/XRayHologramFresnel"
{
    Properties
    {
        [Header(Color and Glow)]
        _BaseColor ("Inner Core Color", Color) = (0.02, 0.12, 0.22, 0.25)
        _RimColor ("Fresnel Rim Glow Color", Color) = (0.0, 0.9, 1.0, 0.85)
        _FresnelPower ("Fresnel Exponent", Range(0.5, 6.0)) = 2.5
        _RimIntensity ("Rim Brightness Multiplier", Range(0.5, 5.0)) = 2.0

        [Header(Medical Scanlines)]
        _ScanlineFrequency ("Scanline Density", Range(5.0, 100.0)) = 35.0
        _ScanlineSpeed ("Scanline Velocity", Range(-5.0, 5.0)) = 1.2
        _ScanlineIntensity ("Scanline Pulse Contrast", Range(0.0, 0.8)) = 0.25

        [Header(Transparency)]
        _AlphaMultiplier ("Global Opacity Factor", Range(0.0, 1.0)) = 0.85
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True"
        }

        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _FresnelPower;
                float _RimIntensity;
                float _ScanlineFrequency;
                float _ScanlineSpeed;
                float _ScanlineIntensity;
                float _AlphaMultiplier;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normalize(normInputs.normalWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);

                // 1. Calculate Fresnel Rim Factor
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _RimIntensity;

                // 2. Subtle Medical Scanning Grid / Lines
                float scanline = sin(input.positionWS.y * _ScanlineFrequency + _Time.y * _ScanlineSpeed);
                float scanlineFactor = 1.0 - (scanline * _ScanlineIntensity);

                // 3. Composite Core + Rim Glow
                half3 finalColor = lerp(_BaseColor.rgb, _RimColor.rgb, saturate(fresnel));
                finalColor *= scanlineFactor;

                // 4. Alpha Calculation (Center transparent, edges opaque)
                float alpha = lerp(_BaseColor.a, _RimColor.a, saturate(fresnel));
                alpha *= _AlphaMultiplier;

                return half4(finalColor, saturate(alpha));
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
