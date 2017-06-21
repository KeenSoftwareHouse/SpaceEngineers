#ifndef COMMON_H__
#define COMMON_H__

#include <D3DX_DXGIFormatConvert.inl>

SamplerState	DefaultSampler			: register( s0 );
SamplerState	PointSampler			: register( s1 );
SamplerState	LinearSampler			: register( s2 );
SamplerState	TextureSampler			: register( s3 );
SamplerState	AlphamaskSampler		: register( s4 );
SamplerState	AlphamaskArraySampler   : register( s5 );

#define MERGE(a,b) a##b

// cbuffers
#define FRAME_SLOT 0
#define PROJECTION_SLOT 1
#define OBJECT_SLOT 2
#define MATERIAL_SLOT 3
#define FOLIAGE_SLOT 4
#define ALPHAMASK_SLOT 5

// srvs 0-9 
// material textures

// srvs geometry
#define BIG_TABLE_INDICES 10
#define BIG_TABLE_VERTEX_POSITION 11
#define BIG_TABLE_VERTEX 12
#define INSTANCE_INDIRECTION 13
#define INSTANCE_DATA 14
#define DITHER_8X8_SLOT 28

// srvs lighting
#define SKYBOX_SLOT 10
#define SKYBOX_IBL_SLOT 11
#define AO_SLOT 12
#define POINTLIGHT_SLOT 13
#define TILE_LIGHT_INDICES_SLOT 14
#define CASCADES_SM_SLOT 15
#define AMBIENT_BRDF_LUT_SLOT 16
#define SKYBOX2_SLOT 17
#define SKYBOX2_IBL_SLOT 18
#define SHADOW_SLOT 19
#define MATERIAL_BUFFER_SLOT 20

//samplers
#define SHADOW_SAMPLER_SLOT 15

#ifndef MS_SAMPLE_COUNT
	#define COVERAGE_MASK_ALL 1
#elif MS_SAMPLE_COUNT == 2
	#define COVERAGE_MASK_ALL 0x3
#elif MS_SAMPLE_COUNT == 4
	#define COVERAGE_MASK_ALL 0xF
#elif MS_SAMPLE_COUNT == 8
	#define COVERAGE_MASK_ALL 0xFF
#endif

#define COMPLEMENTARY_DEPTH

#ifndef COMPLEMENTARY_DEPTH
	bool depth_not_background(float x) { return x < 1; }
	static const bool DEPTH_CLEAR = 1;
#else
	bool depth_not_background(float x) { return x > 0; }
	static const bool DEPTH_CLEAR = 0;
#endif

#define MAX_ADDITIONAL_SUNS 5
#define MAX_VOXEL_MATERIALS 128



static const float3 DEBUG_COLORS [] = {
	{ 1, 0, 0 },
	{ 0, 1, 0 },
	{ 0, 0, 1 },

	{ 1, 1, 0 },
	{ 0, 1, 1 },
	{ 1, 0, 1 },

	{ 0.5, 0, 1 },
	{ 0.5, 1, 0 },

	{ 1, 0, 0.5 },
	{ 0, 1, 0.5 },

	{ 1, 0.5, 0 },
	{ 0, 0.5, 1 },

	{ 0.5, 1, 1 },
	{ 1, 0.5, 1 },
	{ 1, 1, 0.5 },
	{ 0.5, 0.5, 1 },	
};

static const uint DEBUG_COLORS_LEN = 16;

struct TextureDebugMultipliersType
{
    float AlbedoMultiplier;
    float MetalnessMultiplier;
    float GlossMultiplier;
    float AoMultiplier;

    float EmissiveMultiplier;
    float ColorMaskMultiplier;
    float AlbedoShift;
    float MetalnessShift;

    float GlossShift;
    float AoShift;
    float EmissiveShift;
    float ColorMaskShift;
};


#endif