// @define NO_SHADOWS
// @define SAMPLE_FREQ_PASS

#include <Lighting/light.h>

Texture2D<float> ShadowsMainView : register(MERGE(t, SHADOW_SLOT));

float3 GetSunColor(float3 L, float3 V, float3 color, float sizeMult)
{
    return (color + 0.5f + float3(0.5f, 0.35f, 0.0f)) * pow(saturate(dot(L, -V)), 4000.0f) * sizeMult;
}

void __pixel_shader(PostprocessVertex vertex, out float3 output : SV_Target0
#ifdef SAMPLE_FREQ_PASS
	, uint sample_index : SV_SampleIndex
#endif
	)
{
#if !defined(MS_SAMPLE_COUNT) || defined(PIXEL_FREQ_PASS)
	SurfaceInterface input = read_gbuffer(vertex.position.xy);
#else
	SurfaceInterface input = read_gbuffer(vertex.position.xy, sample_index);
#endif

	float shadow = 1;
	float ao = input.ao;
#ifndef NO_SHADOWS
	if (input.id == 2)
	{	
		shadow = calculate_shadow_fast(input.position, input.stencil);
	}
	else
	{
#if !defined(MS_SAMPLE_COUNT) || defined(PIXEL_FREQ_PASS)
		shadow = ShadowsMainView[vertex.position.xy].x;
#else
		shadow = calculate_shadow_fast(input.position, input.stencil);
#endif
	}

	float shadowMultiplier = mad(frame_.shadowFadeout, -frame_.skyboxBrightness, frame_.shadowFadeout);
	shadow = mad(shadow, 1 - shadowMultiplier, shadowMultiplier);
#endif

	if(depth_not_background(input.native_depth)) 
	{
        float3 shaded = 0;
        float sqrtAo = sqrt(ao);

		// emissive
        shaded += input.base_color * input.emissive;
		
		// ambient diffuse & specular
		shaded += ambient_diffuse(input.albedo, input.N, input.depth) * ao;
		shaded += ambient_specular(input.f0, input.gloss, input.N, input.V) * ao;

		// main directional light diffuse & specular
		shaded += main_directional_light(input) * shadow * sqrtAo;

		// additional directional light diffuse & specular
		// TODO: purpose of w?
		/*float4 ambientSample = SkyboxIBLTex.SampleLevel(TextureSampler, input.N, mad(-input.gloss, IBL_MAX_MIPMAP, IBL_MAX_MIPMAP)) / 10000;
		for (int sunIndex = 0; sunIndex < max(1, frame_.additionalSunsInUse); ++sunIndex)
			shaded += back_directional_light(input, sunIndex) * sqrtAo * ambientSample.w;*/

		output = add_fog(shaded, input.depth, -input.V, get_camera_position());
	}
	else 	
	{
		float3 v = mul(float4(-input.V, 0.0f), frame_.background_orientation).xyz;
		// This is because DX9 code does the same (see MyBackgroundCube.cs)
		v.z *= -1;
		output = SkyboxColor(v) * frame_.skyboxBrightness;

		output += GetSunColor(-frame_.directionalLightVec, input.V, frame_.directionalLightColor, 5);
	}
}
