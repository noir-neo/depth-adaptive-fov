#ifndef VRMC_MATERIALS_MTOON_DEPTH_FOV_INCLUDED
#define VRMC_MATERIALS_MTOON_DEPTH_FOV_INCLUDED

// Depth Adaptive FOV parameters
float _DepthFOV_FarFOV;
float _DepthFOV_FarDistance;

// Apply Depth Adaptive FOV transformation to clip space position
// This modifies the XY coordinates based on view-space depth
inline float4 MToon_ApplyDepthAdaptiveFOV(float4 positionCS, float3 positionWS)
{
    // Calculate view-space depth (distance from camera)
    float3 viewPos = mul(UNITY_MATRIX_V, float4(positionWS, 1.0)).xyz;
    float depth = -viewPos.z; // Camera looks down -Z, so negate

    // Get camera's FOV scale from projection matrix
    // unity_CameraProjection[1][1] = 1 / tan(fov/2)
    float cameraScale = unity_CameraProjection[1][1];
    float nearScale = 1.0 / cameraScale; // tan(cameraFov/2)
    float farScale = tan(radians(_DepthFOV_FarFOV * 0.5));

    // Non-linear interpolation: depth=0 -> t=0, depth=farDist -> t=0.5, depth=inf -> t=1
    float t = depth / (depth + _DepthFOV_FarDistance);
    float currentScale = lerp(nearScale, farScale, t);

    // Calculate scale factor relative to camera's original FOV
    // Invert the ratio so smaller FOV = larger appearance (telephoto effect)
    float scaleFactor = nearScale / currentScale;

    // Convert to NDC, apply scale, convert back
    float2 ndc = positionCS.xy / positionCS.w;
    positionCS.xy = ndc * scaleFactor * positionCS.w;

    return positionCS;
}

#endif
