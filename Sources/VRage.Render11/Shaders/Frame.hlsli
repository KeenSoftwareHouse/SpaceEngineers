#ifndef INCLUDE_FRAME_HLSLI
#define INCLUDE_FRAME_HLSLI
#include <Common.hlsli>
#include <Math/Math.hlsli>

struct EnvironmentSettings
{
    matrix view_matrix;
    matrix projection_matrix;
    matrix view_projection_matrix;
    matrix inv_view_matrix;
    matrix inv_proj_matrix;
    matrix inv_view_proj_matrix;
    matrix view_projection_matrix_world;
    
    matrix background_orientation; // Skybox
    
    float4 world_offset;

    float3 eye_offset_in_world; // Used in stereo rendering
    float __pad0;

    float3 cameraPositionDelta;
    float __pad1;
};

struct ScreenSettings
{
    int2 offset_in_gbuffer;
    int2 resolution_of_gbuffer;

    float2 resolution;
    uint tiles_num;
    uint tiles_x;
};

struct FoliageSettings
{
    float4 clipping_scaling;
    float3 wind_vec;
    float __pad;
};

struct LightSettings
{
    float3 directionalLightColor;
    float directionalLightDiffuseFactor;

    float3 backLightColor1;
    float directionalLightGlossFactor;

    float3 backLightColor2;
    float backLightGlossFactor;

    float3 directionalLightVec;
    float aoDirLight;

    float3 backLightVec1;
    float ambientDiffuseFactor;

    float3 backLightVec2;
    float ambientSpecularFactor;

    float ambientGlobalMinimum;
    float ambientGlobalDensity;
    float ambientGlobalMultiplier;
    float forwardPassAmbient;

    float3 SunDiscColor;
    float SunDiscInnerDot;

    float SunDiscOuterDot;
    float aoIndirectLight, aoPointLight, aoSpotLight;

    float skyboxBrightness;
    float envSkyboxBrightness;
    float shadowFadeout;
    float envShadowFadeout;
    
    float envAtmosphereBrightness;
    float3 __pad;
};

struct PostProcessSettings
{
    float Contrast;
    float Brightness;
    float ConstantLuminance;
    float LuminanceExposure;

    float Saturation;
    float BrightnessFactorR;
    float BrightnessFactorG;
    float BrightnessFactorB;

    float3 TemperatureColor;
    float TemperatureStrength;

    float Vibrance;
    float EyeAdaptationTau;
    float BloomExposure;
    float BloomLumaThreshold;

    float BloomMult;
    float BloomEmissiveness;
    float BloomDepthStrength;
    float BloomDepthSlope;

    float3 LightColor;
    float LogLumThreshold;

    float3 DarkColor;
    float SepiaStrength;
};

struct FogSettings
{
    float density;
    float mult;
    uint color;
    float __pad;
};

struct VoxelSettings
{
	// up to 4 lod levels + 8 massive levels
    float4 LodRanges[12];

    float DebugVoxelLod;
    float __pad1, __pad2, __pad3;
};

struct FrameConstants
{
    EnvironmentSettings Environment;

    ScreenSettings Screen;

    FoliageSettings Foliage;

    LightSettings Light;

    PostProcessSettings Post;

    FogSettings Fog;

    VoxelSettings Voxels;

    TextureDebugMultipliersType TextureDebugMultipliers;
    
    float frameTime;
    float frameTimeDelta;
    float randomSeed;
    float __pad;
};

cbuffer Frame : register(MERGE(b, FRAME_SLOT))
{
    FrameConstants frame_;
};

float3 view_to_world(float3 view)
{
    return mul(view, (float3x3) frame_.Environment.inv_view_matrix);
}

float3 world_to_view(float3 world)
{
    return mul(world, (float3x3) frame_.Environment.view_matrix);
}

float2 screen_to_uv(float2 screencoord)
{
    const float2 invres = 1 / frame_.Screen.resolution;
    return (screencoord - frame_.Screen.offset_in_gbuffer) * invres + invres / 2;
}

float3 get_camera_position()
{
    return 0;
}

float3 GetEyeCenterPosition()
{
    return 0;
}

float3 compute_screen_ray(float2 uv)
{
    const float ray_x = 1. / frame_.Environment.projection_matrix._11;
    const float ray_y = 1. / frame_.Environment.projection_matrix._22;
    return float3(lerp(-ray_x, ray_x, uv.x), -lerp(-ray_y, ray_y, uv.y), -1.);
}

float compute_depth(float hw_depth)
{
    return -linearize_depth(hw_depth, frame_.Environment.projection_matrix);
}

float3 ReconstructWorldPosition(float hwDepth, float2 uv)
{
    float3 screen_ray = compute_screen_ray(uv);
    float depth = compute_depth(hwDepth);
    float3 viewDirection = mul(screen_ray, transpose((float3x3) frame_.Environment.view_matrix));

    return depth * viewDirection - frame_.Environment.eye_offset_in_world;
}

float2 get_voxel_lod_range(uint lod, int isMassive)
{
    lod = min(lod + 8 * isMassive, 8 + 16 - 1);
    return (lod % 2) ? frame_.Voxels.LodRanges[lod / 2].zw : frame_.Voxels.LodRanges[lod / 2].xy;
}

#endif
