// vrmc_materials_mtoon_forward_vertex_depth_fov.hlsl
// Based on vrmc_materials_mtoon_forward_vertex.hlsl from UniVRM (https://github.com/vrm-c/UniVRM)
//
// Original License:
// MIT License
// Copyright (c) 2020 VRM Consortium
// Copyright (c) 2018 Masataka SUMI for MToon

#ifndef VRMC_MATERIALS_MTOON_FORWARD_VERTEX_DEPTH_FOV_INCLUDED
#define VRMC_MATERIALS_MTOON_FORWARD_VERTEX_DEPTH_FOV_INCLUDED

#include "Packages/com.vrmc.vrm/MToon10/Shaders/vrmc_materials_mtoon_render_pipeline.hlsl"
#include "Packages/com.vrmc.vrm/MToon10/Shaders/vrmc_materials_mtoon_define.hlsl"
#include "Packages/com.vrmc.vrm/MToon10/Shaders/vrmc_materials_mtoon_utility.hlsl"
#include "Packages/com.vrmc.vrm/MToon10/Shaders/vrmc_materials_mtoon_input.hlsl"
#include "Packages/com.vrmc.vrm/MToon10/Shaders/vrmc_materials_mtoon_attribute.hlsl"
#include "Packages/com.vrmc.vrm/MToon10/Shaders/vrmc_materials_mtoon_geometry_vertex.hlsl"
#include "Packages/com.neoneobeam.depth-adaptive-fov.mtoon10/Runtime/Shaders/vrmc_materials_mtoon_depth_fov.hlsl"

Varyings MToonVertexDepthFOV(const Attributes v)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.uv = v.texcoord0;

    if (MToon_IsOutlinePass())
    {
        const VertexPositionInfo position = MToon_GetOutlineVertex(v.vertex.xyz, normalize(v.normalOS), output.uv);
        // Apply Depth Adaptive FOV to outline
        output.pos = MToon_ApplyDepthAdaptiveFOV(position.positionCS, position.positionWS.xyz);
        output.positionWS = position.positionWS.xyz;

        output.normalWS = MToon_TransformObjectToWorldNormal(-v.normalOS);
    }
    else
    {
        const VertexPositionInfo position = MToon_GetVertex(v.vertex.xyz);
        // Apply Depth Adaptive FOV
        output.pos = MToon_ApplyDepthAdaptiveFOV(position.positionCS, position.positionWS.xyz);
        output.positionWS = position.positionWS.xyz;

        output.normalWS = MToon_TransformObjectToWorldNormal(v.normalOS);
    }

    output.viewDirWS = MToon_GetWorldSpaceNormalizedViewDir(output.positionWS);

#if defined(_NORMALMAP)
    const half tangentSign = v.tangentOS.w * unity_WorldTransformParams.w;
    output.tangentWS = half4(MToon_TransformObjectToWorldDir(v.tangentOS), tangentSign);
#endif

    MTOON_TRANSFER_FOG_AND_LIGHTING(output, output.pos, v.texcoord1.xy, v.vertex.xyz);

    return output;
}

#endif
