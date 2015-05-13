#include <template.h>
#include <EnvAmbient.h>
#include <postprocess_base.h>

Texture2D<float>	DepthBuffer	: register( t0 );


cbuffer Constants : register( MERGE(b,PROJECTION_SLOT) )
{
	matrix viewMatrix;
};

void apply_skybox(PostprocessVertex vertex, out float4 output : SV_Target0) {
	float depth = DepthBuffer[vertex.position.xy];

	if(depth < 1) {
		discard;
	} 

	const float ray_x = 1;
	const float ray_y = 1;
	float3 screen_ray = float3(lerp( -ray_x, ray_x, vertex.uv.x ), -lerp( -ray_y, ray_y, vertex.uv.y ), -1.);

	float3 V = mul(screen_ray, transpose((float3x3)viewMatrix));

	output = float4(SkyboxColor(V), 0);
}