Shader "Custom/PolyhedronFlatShadedOutline"
{
    Properties
    {
        _Ambient ("Ambient Light", Range(0,1)) = 0.4
        _Diffuse ("Diffuse Strength", Range(0,1)) = 0.6
    }
    SubShader
    {
        Tags { 
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float3 normalWS : NORMAL;
                float fogFactor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _Ambient;
            float _Diffuse;

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.normalWS = normalInputs.normalWS;
                OUT.color = IN.color;
                OUT.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                Light mainLight = GetMainLight();
                float3 normalWS = normalize(IN.normalWS);
                float ndotl = saturate(dot(normalWS, mainLight.direction));
                // float ndotl = saturate((normalWS, mainLight.direction));
                
                float3 lighting = _Ambient + (_Diffuse * ndotl * mainLight.color);
                // float3 lighting = _Ambient + (_Diffuse * mainLight.color);
                half4 color = IN.color;
                color.rgb *= lighting;
                
                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }
    
    // Fallback for platforms that don't support HLSL
    Fallback "Universal Render Pipeline/Simple Lit"
}