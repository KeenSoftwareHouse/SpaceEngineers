#ifndef FRAME_CONSTANTS__
#define FRAME_CONSTANTS__
#include <Common.hlsli>
#include <Math/Math.hlsli>

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

struct FrameConstants 
{
	matrix 	view_projection_matrix;
	matrix	view_matrix;
	matrix	projection_matrix;
	matrix 	inv_view_matrix;
	matrix  inv_proj_matrix;
	matrix 	inv_view_proj_matrix;

	matrix 	view_projection_matrix_world;
	float4	world_offset;

	// used in stereo rendering:
	float3  eye_offset_in_world;
    float   __p0;

	int2    offset_in_gbuffer;
	int2    resolution_of_gbuffer;

	float2	resolution;
	float 	frameTime;
	float 	frameTimeDelta;

	float4 	terrain_texture_distances;

	float2 	terrain_material_transition;
	uint 	tiles_num;
	uint 	tiles_x;

	float4 	foliage_clipping_scaling;

	float3 	wind_vec;
    float 	debug_voxel_lod;

    float3  directionalLightColor;
    float   directionalLightDiffuseFactor;

    float3  backLightColor1;
    float   directionalLightGlossFactor;

    float3  backLightColor2;
    float   backLightGlossFactor;

    float3  directionalLightVec;
    float   aoDirLight;

    float3  backLightVec1;
    float   ambientDiffuseFactor;

    float3  backLightVec2;
    float   ambientSpecularFactor;

    float 	ambientGlobalMinimum;
    float 	ambientGlobalDensity;
    float 	ambientGlobalMultiplier;
    float   forwardPassAmbient;

    float3  SunDiscColor;
    float   SunDiscInnerDot;

    float   SunDiscOuterDot;
    float   aoIndirectLight, aoPointLight, aoSpotLight;

 	float   skyboxBrightness;
    float   envSkyboxBrightness;
	float   shadowFadeout;
	float   envShadowFadeout;
    
    float   envAtmosphereBrightness;
    float   _pad0, _pad1, _pad2;

    PostProcessSettings Post;

    float 	fog_density;
    float 	fog_mult;
    uint	fog_color;
	float   randomSeed;

	// up to 8 lod levels + 16 massive levels
	float4 voxel_lod_range[12];

	float EnableVoxelAo;
	float VoxelAoMin;
	float VoxelAoMax;
	float VoxelAoOffset;

	matrix background_orientation;

    TextureDebugMultipliersType TextureDebugMultipliers;

	float3  cameraPositionDelta;
};

cbuffer Frame : register( MERGE(b,FRAME_SLOT) )
{
	FrameConstants frame_;
};

float3 view_to_world(float3 view)
{
    return mul(view, (float3x3)frame_.inv_view_matrix);
}

float3 world_to_view(float3 world)
{
    return mul(world, (float3x3)frame_.view_matrix);
}

float2 screen_to_uv(float2 screencoord)
{
	const float2 invres = 1 / frame_.resolution;
	return (screencoord - frame_.offset_in_gbuffer) * invres + invres/2;
}

float3 get_camera_position()
{
	return 0;
}

float3 GetEyeCenterPosition()
{
    return 0;
}

float3 ReconstructWorldPosition(float hwDepth, float2 uv) 
{
	const float ray_x = 1./frame_.projection_matrix._11;
	const float ray_y = 1./frame_.projection_matrix._22;
	float3 screen_ray = float3(lerp( -ray_x, ray_x, uv.x ), -lerp( -ray_y, ray_y, uv.y ), -1.);
	float depth = -linearize_depth(hwDepth, frame_.projection_matrix);
	float3 viewDirection = mul(screen_ray, transpose((float3x3)frame_.view_matrix));

	return depth * viewDirection - frame_.eye_offset_in_world;
}

float2 get_voxel_lod_range(uint lod, int isMassive)
{
	lod = min(lod + 8 * isMassive, 8 + 16 - 1);
	return (lod % 2) ? frame_.voxel_lod_range[lod / 2].zw : frame_.voxel_lod_range[lod / 2].xy;
}

#endif
