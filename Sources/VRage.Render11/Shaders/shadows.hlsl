#ifndef SAMPLE_FREQ_PASS
#define PIXEL_FREQ_PASS
#endif

#ifndef NUMTHREADS_X
#define NUMTHREADS_X NUMTHREADS
#endif

#ifndef NUMTHREADS_Y
#define NUMTHREADS_Y NUMTHREADS_X
#endif

#define GROUP_THREADS NUMTHREADS_X * NUMTHREADS_Y

#include <frame.h>
#include <csm.h>
#include <math.h>


Texture2D<float>	DepthBuffer	: register( t0 );

#ifndef MS_SAMPLE_COUNT
	Texture2D<uint2>	Stencil		: register( t1 );
#else
	Texture2DMS<uint2, MS_SAMPLE_COUNT>	StencilMS	: register( t1 );
#endif


Texture2D<float> Shadow : register( t0 );
RWTexture2D<float> Output : register( u0 );

//

static const float2 PoissonSamplesArray[] = {
	float2( 0.130697, -0.209628),
	float2( -0.112312, 0.327448),
	float2( -0.499089, -0.030236),
	float2( 0.332994, 0.380106),
	float2( -0.234209, -0.557516),
	float2( 0.695785, 0.066096),
	float2( -0.419485, 0.632050),
	float2( 0.678688, -0.447710),
	float2( 0.333877, 0.807633),
	float2( -0.834613, 0.383171),
	float2( -0.682884, -0.637443),
	float2( 0.769794, 0.568801),
	float2( -0.087941, -0.955035),
	float2( -0.947188, -0.166568),
	float2( 0.425303, -0.874130),
	float2( -0.134360, 0.982611),
};

static const float2 RandomRotation[] = {
	float2(0.971327, 0.237749),
	float2(-0.885968, 0.463746),
	float2(-0.913331, 0.407218),
	float2(0.159352, 0.987222),
	float2(-0.640909, 0.767617),
	float2(-0.625570, 0.780168),
	float2(-0.930406, 0.366530),
	float2(-0.940038, 0.341070),
	float2(0.964899, 0.262621),
	float2(-0.647723, 0.761876),
	float2(0.663773, 0.747934),
	float2(0.929892, 0.367833),
	float2(-0.686272, 0.727345),
	float2(-0.999057, 0.043413),
	float2(-0.710684, 0.703511),
	float2(-0.893640, 0.448784)
};

static const uint PoissonSamplesNum = 16;
static const float FilterSize = 3;

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void write_shadow(
	uint3 dispatchThreadID : SV_DispatchThreadID,
	uint3 groupThreadID : SV_GroupThreadID,
	uint3 GroupID : SV_GroupID,
	uint ThreadIndex : SV_GroupIndex) {

	float2 Texel = dispatchThreadID.xy;

	float2 uv = (Texel + 0.5f) / frame_.resolution;
	float3 pos = reconstruct_position(DepthBuffer[Texel], uv);
	
	#ifndef MS_SAMPLE_COUNT
		uint c_id = cascade_id_stencil(Stencil[Texel].y);
	#else
		uint c_id = cascade_id_stencil(StencilMS.Load(Texel, 0).y);
	#endif
	
	float3 lpos = world_to_shadowmap(pos, csm_.cascade_matrix[c_id]);

	float texelsize = 1/512.f;
	float2 filterSize = csm_.cascade_scale[c_id].xy * FilterSize;

	uint2 rotationOffset = dispatchThreadID.xy % 4;
	float2 theta = RandomRotation[rotationOffset.x * 4 + rotationOffset.y].xy;
    float2x2 rotMatrix = float2x2( float2(theta.xy), float2(-theta.y, theta.x) );

	float result = 0;
	[branch]
    if(filterSize.x > 1.0f || filterSize.y > 1.0f) {
    	[unroll]
    	for(uint i=0; i<PoissonSamplesNum; i++) {
    		float2 offset = filterSize * 0.5f * PoissonSamplesArray[i] * texelsize;
    		offset = mul(rotMatrix, offset);
    		result += CSM.SampleCmpLevelZero(ShadowmapSampler, float3(lpos.xy + offset, c_id), lpos.z);	
    	}
    	result /= PoissonSamplesNum;
    }
    else {
    	result = CSM.SampleCmpLevelZero(ShadowmapSampler, float3(lpos.xy, c_id), lpos.z) + any(saturate(lpos.xy) != (lpos.xy));
    }

   	Output[Texel] = result;
}

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void blur(uint3 dispatchThreadID : SV_DispatchThreadID) {
	
	float2 Texel = dispatchThreadID.xy;

	#ifndef MS_SAMPLE_COUNT
		uint c_id = cascade_id_stencil(Stencil[Texel].y);
	#else
		uint c_id = cascade_id_stencil(StencilMS.Load(Texel, 0).y);
	#endif

	float result;
	[branch]
	if(c_id > 1) {
		result = 0;

		for(int i=-5; i<5; i++) {
			#ifdef VERTICAL
			float sample = Shadow[Texel + float2(0, i)];
			#else
			float sample = Shadow[Texel + float2(i, 0)];
			#endif

			result += sample * gaussian_weigth(i, 1.5);
		}
		#ifdef VERTICAL
		result = pow(result, 2);
		#endif		
	}
	else
	{
		result = Shadow[Texel];
	}

	Output[Texel] = result;
}