#include <Template.hlsli>
#include <Lighting/EnvAmbient.hlsli>
#include <Postprocess/PostprocessBase.hlsli>

Texture2D<float>	DepthBuffer	: register( t0 );


cbuffer Constants : register( MERGE(b,OBJECT_SLOT) )
{
	matrix viewMatrix;
};

void __pixel_shader(PostprocessVertex vertex, out float4 output : SV_Target0) 
{
	float depth = DepthBuffer[vertex.position.xy];

	if(depth > 0)
		discard;

	const float ray_x = 1;
	const float ray_y = 1;
	float3 screen_ray = float3(lerp( -ray_x, ray_x, vertex.uv.x ), -lerp( -ray_y, ray_y, vertex.uv.y ), -1.);

	float3 V = mul(screen_ray, transpose((float3x3)viewMatrix));

    output = float4(SkyboxColorReflected(V) * frame_.Light.envSkyboxBrightness, 0);
}
