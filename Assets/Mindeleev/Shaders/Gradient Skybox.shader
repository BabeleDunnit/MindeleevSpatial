Shader "Spatial/Skybox/GradientSkybox"
{
    Properties
    {
        _Color1 ("Color 1", Color) = (1, 1, 1, 0)
        _Color2 ("Color 2", Color) = (1, 1, 1, 0)
        _UpVector ("Up Vector", Vector) = (0, 1, 0, 0)
        _Intensity ("Intensity", Float) = 1.0
        _Exponent ("Exponent", Float) = 1.0
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    struct appdata
    {
        float4 position : POSITION;
        float3 texcoord : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    
    struct v2f
    {
        float4 position : SV_POSITION;
        fixed4 Color : COLOR;
        UNITY_VERTEX_OUTPUT_STEREO
    };
    
    half4 _UpVector;
    fixed4 _Color1;
    fixed4 _Color2;
    fixed _Intensity;
    fixed _Exponent;
    
    v2f vert (appdata v)
    {
        UNITY_SETUP_INSTANCE_ID(v);
        v2f o;
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        o.position = UnityObjectToClipPos (v.position);
        o.Color = lerp(_Color1, _Color2, pow(mad(dot(normalize (v.texcoord), _UpVector), .5, .5), _Exponent)) * _Intensity;
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        return o;
    }
    
    fixed4 frag (v2f i) : COLOR
    {
        return i.Color;
    }

    ENDCG

    SubShader
    {
        Tags
        {
            "RenderType"="Background"
            "Queue"="Background"
            "PerformanceChecks"="False"
            "IgnoreProjector"="True"
            "DisableBatching"="True"
        }
        Pass
        {
            ZWrite Off
            Cull Back
            Fog { Mode Off }
            CGPROGRAM
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma exclude_renderers xboxone ps4 n3ds wiiu
            #pragma multi_compile_fwdbase nodirlightmap nodynlightmap novertexlight noambient exclude_path:deferred exclude_path:prepass
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
    CustomEditor "GradientSkyboxInspector"
}
