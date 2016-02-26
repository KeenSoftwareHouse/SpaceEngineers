// @define LIT_PARTICLE

#include <common.h>
#include <frame.h>
#include <Math/Color.h>
#include <csm.h>
#include <EnvAmbient.h>

struct BillboardData
{
	int custom_projection_id;
	float4 Color;
	float reflective;
	float AlphaSaturation;
	float3 normal;
	float SoftParticleDistanceScale;
	float AlphaCutout;
};

cbuffer CustomProjections : register ( b2 )
{
	matrix view_projection[32];
};

#define BILLBOARD_BUFFER_SLOT 104

StructuredBuffer<BillboardData> BillboardBuffer : register( MERGE(t,BILLBOARD_BUFFER_SLOT) );
Texture2D<float4> TextureAtlas : register( t0 );

Texture2D<float> Depth : register( t1 );

struct VsIn
{
	float3 position : POSITION;
	float2 texcoord : TEXCOORD0;
};

struct VsOut
{
	float4 position : SV_Position;
	float2 texcoord : Texcoord0;
	uint   index 	: Texcoord1;
	float3 wposition : TEXCOORD2;
#ifdef LIT_PARTICLE
	float3 light 	: Texcoord3;
#endif
};

#define REFLECTIVE

VsOut __vertex_shader(VsIn vertex, uint vertex_id : SV_VertexID)
{
	VsOut result;

	uint billboard_index = vertex_id / 4;
	uint billboard_quad_index = vertex_id % 4;

	int custom_id = BillboardBuffer[billboard_index].custom_projection_id;
	float4 projPos; 
	if(custom_id < 0)	
	{
		projPos = mul(float4(vertex.position.xyz, 1), frame_.view_projection_matrix);	
	}
	else{
		projPos = mul(float4(vertex.position.xyz, 1), view_projection[custom_id]);
	}
	result.position = projPos;
	result.texcoord = vertex.texcoord;
	result.index = billboard_index;
	result.wposition = vertex.position.xyz;

#ifdef LIT_PARTICLE
    float3 vs_pos = mul(float4(vertex.position.xyz, 1), frame_.view_matrix).xyz;
    float3 V = normalize(get_camera_position() - vs_pos);
    result.light = calculate_shadow_fast_particle(vertex.position.xyz, -projPos.z / projPos.w) + ambient_diffuse(0, 0, V, 0, 0.5f);
#endif

	return result;
}

#pragma warning( disable : 3571 )


float4 SaturateAlpha(float4 resultColor, float alpha, float alphaSaturation)
{
    if ( alphaSaturation < 1 )
    {
        float invSat = 1 - alphaSaturation;
        float alphaSaturate = clamp(alpha - invSat, 0, 1);
        resultColor += float4(1, 1, 1, 1) * float4(alphaSaturate.xxx, 0) * alpha;
    }
    return resultColor;
}

float4 CalculateColor(VsOut input, bool minTexture, float alphaCutout)
{
    float softParticleFade = 1;
    float depth_sample = Depth[input.position.xy].r;
    if ( depth_sample < 1 ) {
        float targetdepth = linearize_depth(depth_sample, frame_.projection_matrix);
        float depth = linearize_depth(input.position.z, frame_.projection_matrix);
        softParticleFade = saturate(BillboardBuffer[input.index].SoftParticleDistanceScale * (depth - targetdepth));
    }

	float4 billboardColor = float4(srgb_to_rgb(BillboardBuffer[input.index].Color.xyz), BillboardBuffer[input.index].Color.w);
	//float4 billboardColor = BillboardBuffer[input.index].Color.xyz;

    float4 resultColor = float4(1, 1, 1, 1);

    if ( minTexture )
    {
        resultColor *= billboardColor;
        resultColor *= softParticleFade;
    }
    else
    {
        float4 textureSample = TextureAtlas.Sample(LinearSampler, input.texcoord.xy);
        //float alpha = textureSample.x * textureSample.y * textureSample.z;

        resultColor *= textureSample * billboardColor;
        //resultColor += 100*BillboardBuffer[input.index].Emissivity*resultColor; TODO
        //resultColor = SaturateAlpha(resultColor, alpha, BillboardBuffer[input.index].AlphaSaturation); uncomment for hotspots on lights/thruster flames
        
#ifdef ALPHA_CUTOUT
		float cutout = step(alphaCutout, resultColor.w);
		resultColor = float4(cutout * resultColor.xyz, cutout);
		//resultColor = float4(resultColor.w, resultColor.w, resultColor.w, 1);
#endif

		resultColor *= softParticleFade;
    }
	return resultColor;
}

float4 __pixel_shader(VsOut vertex) : SV_Target0
{
	float4 resultColor = float4(1, 1, 1, 1);

#ifdef ALPHA_CUTOUT
	float alphaCutout = BillboardBuffer[vertex.index].AlphaCutout; 
#else
	float alphaCutout = 0;
#endif

#ifdef REFLECTIVE
    float reflective = BillboardBuffer[vertex.index].reflective;
    if ( reflective )
    {
		float3 N = normalize(BillboardBuffer[vertex.index].normal);
		float3 viewVector = normalize(get_camera_position() - vertex.wposition);

		float3 reflectionSample = ambient_specular(0.04f, 0.95f, N, viewVector);
		float4 color = CalculateColor(vertex, true, alphaCutout);
        color.xyz *= color.w;
        float3 reflectionColor = lerp(color.xyz*color.w, reflectionSample, reflective);

        float4 dirtSample = TextureAtlas.Sample(LinearSampler, vertex.texcoord.xy);
        float3 colorAndDirt = lerp(reflectionColor, dirtSample.xyz, dirtSample.w);

        resultColor = float4(colorAndDirt, max(max(color.w, reflective), dirtSample.w));
    }
    else
#endif
    {
		resultColor = CalculateColor(vertex, false, alphaCutout);
#ifdef LIT_PARTICLE
        resultColor.xyz *= vertex.light;
#endif    
    }

    return resultColor;
}