#include "UnityCG.cginc"

#ifndef SPATIAL_FOG_INCLUDED
#define SPATIAL_FOG_INCLUDED
    // Replacement for UNITY_FOG_COORDS(#) to have 4 channels at the fog varying
    #define SPATIAL_FOG_COORDS(idx) UNITY_FOG_COORDS_PACKED(idx, float4)

    // View direction helpers
    #define SPATIAL_GET_VIEW_DIRECTION_FROM_OBJECT_SPACE(objectSpaceVertexPosition) UnityWorldSpaceViewDir(mul(unity_ObjectToWorld, objectSpaceVertexPosition));
    #define SPATIAL_GET_VIEW_DIRECTION_FROM_WORLD_SPACE(worldSpaceVertexPosition) UnityWorldSpaceViewDir(worldSpaceVertexPosition);

    // Replacement for UNITY_TRANSFER_FOG(o, clip space vertex)
    #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
        // *viewDirection shouldn't be normalized
        #define SPATIAL_TRANSFER_FOG_WITH_COMPUTED_VIEW_DIRECTION(o, outpus, viewDirection) UNITY_TRANSFER_FOG(o, outpus); o.fogCoord.yzw = viewDirection;
        // For a convenience. Same as computing world view direction. No need to compute twice if you already have view direction
        #define SPATIAL_TRANSFER_FOG_WITH_COMPUTING_VIEW_DIRECTION_FROM_OBJECT_SPACE(o, outpus, objectSpaceVertexPosition) UNITY_TRANSFER_FOG(o, outpus); o.fogCoord.yzw = SPATIAL_GET_VIEW_DIRECTION_FROM_OBJECT_SPACE(objectSpaceVertexPosition);
        #define SPATIAL_TRANSFER_FOG_WITH_COMPUTING_VIEW_DIRECTION_FROM_WORLD_SPACE(o, outpus, worldSpaceVertexPosition) UNITY_TRANSFER_FOG(o, outpus); o.fogCoord.yzw = SPATIAL_GET_VIEW_DIRECTION_FROM_WORLD_SPACE(worldSpaceVertexPosition);
    #else
        #define SPATIAL_TRANSFER_FOG_WITH_COMPUTED_VIEW_DIRECTION(o, outpus, viewDirection)
        #define SPATIAL_TRANSFER_FOG_WITH_COMPUTING_VIEW_DIRECTION_FROM_OBJECT_SPACE(o, outpus, objectSpaceVertexPosition)
        #define SPATIAL_TRANSFER_FOG_WITH_COMPUTING_VIEW_DIRECTION_FROM_WORLD_SPACE(o, outpus, worldSpaceVertexPosition)
    #endif

    // Skybox sampling helper
    #define SPATIAL_SAMPLE_FOG_COLOR(viewDirection, lod) UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, viewDirection, lod)

    // Apply fog with skybox color
    #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
        // *Need a reflection prob in the scene
        // *Use without lod if the reflection prob resolution is low enough. Then make sure disable mipmap
        // *Use with lod if you need a reflectio prob with higher resolution for environment mapping or something, make sure mipmap is enabled
        #define SPATIAL_APPLY_FOG(fogCoord, sourceColor) UNITY_APPLY_FOG_COLOR(fogCoord, sourceColor, SPATIAL_SAMPLE_FOG_COLOR(-normalize(fogCoord.yzw), 0));
        #define SPATIAL_APPLY_FOG_SAMPLE_WITH_LOD(fogCoord, sourceColor, lod) UNITY_APPLY_FOG_COLOR(fogCoord, sourceColor, SPATIAL_SAMPLE_FOG_COLOR(-normalize(fogCoord.yzw), lod));
        // Use this if you already use viewDireciton in your fragment shader in order to avoid redundant normalize view direction vector
        #define SPATIAL_APPLY_FOG_WITH_CUSTOM_VIEW_DIRECTION(fogCoord, viewDirection, sourceColor) UNITY_APPLY_FOG_COLOR(fogCoord, sourceColor, SPATIAL_SAMPLE_FOG_COLOR(-viewDirection, 0));
        #define SPATIAL_APPLY_FOG_SAMPLE_WITH_LOD_WITH_CUSTOM_VIEW_DIRECTION(fogCoord, viewDirection, sourceColor, lod) UNITY_APPLY_FOG_COLOR(fogCoord, sourceColor, SPATIAL_SAMPLE_FOG_COLOR(-viewDirection, lod));
    #else
        #define SPATIAL_APPLY_FOG(fogCoord, sourceColor)
        #define SPATIAL_APPLY_FOG_SAMPLE_WITH_LOD(fogCoord, sourceColor, lod)
        #define SPATIAL_APPLY_FOG_WITH_CUSTOM_VIEW_DIRECTION(fogCoord, viewDirection, sourceColor)
        #define SPATIAL_APPLY_FOG_SAMPLE_WITH_LOD_WITH_CUSTOM_VIEW_DIRECTION(fogCoord, viewDirection, sourceColor, lod)
    #endif
#endif