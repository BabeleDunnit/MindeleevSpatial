Shader "Custom/PolyhedronFlatShaded"
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
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float3 normalWS : NORMAL;
            };

            float _Ambient;
            float _Diffuse;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                Light mainLight = GetMainLight();
                float3 normalWS = normalize(IN.normalWS);
                float ndotl = max(0, dot(normalWS, mainLight.direction));
                
                float3 lighting = _Ambient + (_Diffuse * ndotl);
                half4 color = IN.color;
                color.rgb *= lighting;
                return color;
            }
            ENDHLSL
        }
    }
}