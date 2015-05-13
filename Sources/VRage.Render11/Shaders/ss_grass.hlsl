#include <common.h>
#include <postprocess_base.h>
#include <frame.h>
#include <math.h>

Texture2D<float>	DepthBuffer	: register( t0 );
Texture2D<float4>	Source	 : register( t1 );
Texture2D<float4>	Gbuffer2 : register( t2 );

cbuffer Constants : register ( b1 )
{
	float close_distance_;
};

float blend(float v, float v0, float v1, float r0, float r1)
{
	return lerp(r0, r1, saturate((v-v0)/(v1-v0)));
}

#define DOWN_UP

void ps(PostprocessVertex input, out float4 output : SV_Target0)
{
	float2 screencoord = input.position.xy;
	float native_depth = DepthBuffer[screencoord].r;

	const float ray_x = 1./frame_.projection_matrix._11;
	const float ray_y = 1./frame_.projection_matrix._22;
	float3 screen_ray = float3(lerp( -ray_x, ray_x, input.uv.x ), -lerp( -ray_y, ray_y, input.uv.y ), -1.);
	float depth = length(linearize_depth(native_depth, frame_.projection_matrix) * screen_ray);

	float d = blend(depth, 0, 1000, 300, 400);	
	float dclose = blend(depth, 0, close_distance_, 30, 1);

	float4 srccolor = Source[input.position.xy];

	d*=dclose;

	float xx = input.uv.x;
	float yy = input.uv.y;
	yy += xx * 1000 / d;

	float yoffset = frac(yy * d) / d;

#ifdef DOWN_UP
	float2 uvoffset = input.uv - float2(0, yoffset);

	float depth1 = DepthBuffer.Sample(DefaultSampler, uvoffset);

	bool mask_ok = (Gbuffer2.Sample(PointSampler, uvoffset).z * 255 == 1);

	if( depth1 < native_depth || !mask_ok) 
	{
		output = srccolor;
		return;
	}

	float blend_rate = ((Gbuffer2[input.position.xy].z * 255 == 1)) ? saturate(1 - yoffset * d / 4) : 0;
#else
	float2 uvoffset = input.uv + float2(0, yoffset);

	float depth1 = DepthBuffer.Sample(DefaultSampler, uvoffset);

	bool mask_ok = (Gbuffer2.Sample(PointSampler, uvoffset).z * 255 == 1) && ((Gbuffer2[input.position.xy].z * 255 == 1) || (depth1 == 1));

	if( depth1 > native_depth || !mask_ok) 
	{
		output = srccolor;
		return;
	}

	float blend_rate = saturate(1 - yoffset * d / 4);
#endif

	output = lerp(srccolor, Source.Sample(DefaultSampler, uvoffset), blend_rate);
}
