Shader "Spatial/Transparent Grid"
{
    Properties
    {
        _GridThickness("Grid Thickness", Range(0, 0.5)) = 0.05
        _GridSpacing("Grid Spacing", Float) = 0.1
        _GridColour("Grid Colour", Color) = (0.5, 1.0, 1.0, 1.0)
        _GridOpacity("Grid Opacity", Range(0,1)) = 1
        _GridAntialiasingBias("Grid Antialiasing Bias", Range(0, 500)) = 1.0
        _GridEdgeFeather("Grid Edge Feather", Range(0, 5)) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue" = "Transparent"
            "PerformanceChecks"="False"
            "IgnoreProjector"="True"
            "DisableBatching"="True"
        }

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma target 3.0

            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma exclude_renderers xboxone ps4 n3ds wiiu
            #pragma multi_compile_fwdbase nodirlightmap nodynlightmap novertexlight noambient exclude_path:deferred exclude_path:prepass

            #pragma multi_compile_fog
            #include "SpatialFog.cginc"

            uniform fixed _GridThickness;
            uniform fixed _GridSpacing;
            uniform fixed4 _GridColour;
            uniform fixed _GridOpacity;
            uniform half _GridAntialiasingBias;
            uniform half _GridEdgeFeather;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                SPATIAL_FOG_COORDS(1)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                SPATIAL_TRANSFER_FOG_WITH_COMPUTING_VIEW_DIRECTION_FROM_OBJECT_SPACE(o, o.pos, v.vertex);

                return o;
            }

            fixed4 frag(v2f i) : COLOR
            {
                fixed2 tiledCoord = abs(frac(i.uv / _GridSpacing) - .5);
                fixed grid = min(tiledCoord.x, tiledCoord.y);
                fixed filteredGrid = fwidth(grid) * .5 * _GridEdgeFeather;
                float gridThickness = _GridThickness;
                
                // Thickness calculation + Antialiasing
                fixed opacity = saturate(smoothstep(filteredGrid, -filteredGrid, grid - gridThickness) - fwidth(i.uv) * _GridAntialiasingBias);
                opacity *= _GridOpacity;
                clip(opacity - .001);

                fixed4 col = _GridColour;
                col.a *= opacity;
                SPATIAL_APPLY_FOG(i.fogCoord, col);

                return col;
            }
            ENDCG
        }
    }
}
