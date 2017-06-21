// @define NO_SHADOWS
// @define SAMPLE_FREQ_PASS

#include "Light.hlsli"

Texture2D<float> ShadowsMainView : register(MERGE(t, SHADOW_SLOT));

float3 GetSunColor(float3 L, float3 V, float3 color, float innerDot, float outerDot)
{
    float lv = saturate(dot(L, -V));
    float dirFactor = smoothstep(outerDot, innerDot, lv);
    float dirFactorX = dirFactor * dirFactor;
    return color * dirFactorX;
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

	if(depth_not_background(input.native_depth)) 
	{
        float shadow = 1;
#ifndef NO_SHADOWS
#if !defined(MS_SAMPLE_COUNT) || defined(PIXEL_FREQ_PASS)
        shadow = ShadowsMainView[vertex.position.xy].x;
#else
        shadow = calculate_shadow_fast(input.position, input.stencil);
#endif

        shadow = 1 - (1 - shadow) * (1 - frame_.Light.shadowFadeout);
#endif
        float lightAO = saturate(1 - (1 - input.ao) * frame_.Light.aoDirLight);
        float indirectAO = saturate(1 - (1 - input.ao) * frame_.Light.aoIndirectLight);

        float3 shaded = 0;
		// emissive
        shaded += input.albedo * input.emissive;
        
		// ambient diffuse & specular
        shaded += ambient_global(input.albedo, input.depth) * indirectAO;
        shaded += ambient_diffuse(input.albedo, input.N) * indirectAO;
        shaded += ambient_specular(input.f0, input.gloss, input.N, input.V) * indirectAO;

		// main directional light diffuse & specular
		shaded += main_directional_light(input) * shadow * lightAO;
        
        shaded += back_directional_light1(input) * lightAO;
        shaded += back_directional_light2(input) * lightAO;

		output = add_fog(shaded, input.depth, -input.V, get_camera_position());
	}
	else 	
	{
		output = SkyboxColor(input.V) * frame_.Light.skyboxBrightness;

        output += GetSunColor(-frame_.Light.directionalLightVec, input.V, frame_.Light.SunDiscColor, frame_.Light.SunDiscInnerDot, frame_.Light.SunDiscOuterDot); // multiplier 5
	}
}
