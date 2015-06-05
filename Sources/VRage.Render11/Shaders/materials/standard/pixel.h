#include <brdf.h>

float3x3 pixel_tangent_space(float3 N, float3 pos, float2 uv) {
	float3 dp1 =  ddx(pos);
	float3 dp2 =  ddy(pos);
	float2 duv1 =  ddx(uv);
	float2 duv2 =  ddy(uv);
	float3 dp2perp = cross( dp2, N );
    float3 dp1perp = cross( N, dp1 );

    float3 T = dp2perp * duv1.x + dp1perp * duv2.x;
    float3 B = dp2perp * duv1.y + dp1perp * duv2.y;

    float invmax = rsqrt( max( dot(T,T), dot(B,B) ) );
    return float3x3( T * invmax, B * invmax, N );
}

float3 colorize_1(float3 texcolor, float3 hsvmask, float coloring) {
	float3 coloringc = hsv_to_rgb(float3(hsvmask.x, 1, 1)); // TODO: probably won't optimize by itself

	// applying coloring & convert for masking
	float3 hsv = rgb_to_hsv(lerp(1, coloringc, coloring) * texcolor);

	hsv.x = 0;
	float3 fhsv = hsv + hsvmask * float3(1, 1, 0.5); // magic, matches colors from se better
	fhsv.x = frac(fhsv.x);

	float gray2 = 1 - saturate((hsvmask.y + 1.0f) / 0.1f);
	fhsv.yz = lerp(saturate(fhsv.yz), saturate(hsv.yz + hsvmask.yz), gray2);

	float gray3 = 1 - saturate((hsvmask.y + 0.9f) / 0.1f);
	fhsv.y = lerp(saturate(fhsv.y), saturate(hsv.y + hsvmask.y), gray3);

	return lerp(texcolor, hsv_to_rgb(fhsv), coloring);
}

//#define COLORIZE_SPACE_STYLE

void pixel_program(PixelInterface pixel, inout MaterialOutputInterface output)
{
#ifdef ALPHA_MASKED
	#if !defined(MS_SAMPLE_COUNT) || defined(DEPTH_ONLY)

		float alpha = AlphamaskTexture.Sample(TextureSampler, pixel.custom.texcoord0).x;
		if(alpha < 0.5f)
		{
			DISCARD_PIXEL;
		}

	#else
	
		output.coverage = 0;
		[unroll]
		for(int s=0; s < MS_SAMPLE_COUNT ; s++)
		{
			float2 sample_texcoord = EvaluateAttributeAtSample(pixel.custom.texcoord0, s);
			float alpha = AlphamaskTexture.Sample(TextureSampler, sample_texcoord).x;
			output.coverage |= (alpha > 0.5f) ? (uint(1) << s) : 0;
		}

	#endif
#endif

#ifndef DEPTH_ONLY
	float4 cm = ColorMetalTexture.Sample(TextureSampler, pixel.custom.texcoord0);
	float4 ng = NormalGlossTexture.Sample(TextureSampler, pixel.custom.texcoord0);
	float4 extras = AmbientOcclusionTexture.Sample(TextureSampler, pixel.custom.texcoord0);
	float ao = extras.x; 
	float color_mask = extras.w;

	float3 normalmap = ng.xyz * 2 - 1;
	normalmap.y *= -1;
	float normalmap_len = length(normalmap);

	float3 N = pixel.custom.normal;
	float3x3 tangent_to_world;

#ifndef BUILD_TANGENT_IN_PIXEL
	float3 T, B;
	T = pixel.custom.tangent.xyz;
	B = cross(T, N) * pixel.custom.tangent.w;

	tangent_to_world = float3x3(T, B, N);
#else
	float3 pos_ws = pixel.position_ws;

	N = -normalize(cross(ddx(pos_ws), ddy(pos_ws)));
	tangent_to_world = pixel_tangent_space(N, pixel.position_ws, pixel.custom.texcoord0);
#endif

	N = normalize(mul(normalmap, tangent_to_world));


	//output.base_color = colorize_space_style(cm.xyz, pixel.key_color.xyz, color_mask);

	if(pixel.material_flags & MATERIAL_FLAG_RGB_COLORING) {
		float3 c_rgb = hsv_to_rgb(saturate(pixel.key_color.xyz * float3(1, 0.5, 0.5) + float3(0, 0.5, 0.5)));
		output.base_color = lerp(1, c_rgb, color_mask) * cm.xyz;
	}
	else {
		output.base_color = colorize_1(cm.xyz, pixel.key_color.xyz, color_mask);
	}

	// for hologram sampling in branch
	float t = frame_.time / 10.0;
	float2 screenPos = screen_to_uv(pixel.screen_position) * 2 - 1;
	float offset = t * 500.0 * 0.2 + frac(sin(dot(screenPos.x, float2(12.9898,78.233))) * 43758.5453) * 1.5;
	float3 overlay = Dither8x8.Sample(LinearSampler, frac((screenPos.yy * 8.0 + offset / 16.0) + float2(0, 0.8)));
	if(pixel.custom_alpha < 0) {
		float tex_dither = Dither8x8[(uint2)pixel.screen_position % 8];
		if(tex_dither < -pixel.custom_alpha) {
			discard;
		}
		else {
			float2 param = float2(t, screenPos.x + screenPos.y);
			float flicker = frac(sin(dot(param, float2(12.9898,78.233))) * 43758.5453) * 0.2 + 0.8;
			
			output.base_color *= flicker * pow(abs(overlay), 1.5);

			if (pixel.custom_alpha >= -0.25)
			{
				output.base_color *= 1.5;
			}
			output.emissive = 1;
		}
	}
	
	//output.base_color = cm.xyz;

	//output.base_color = color_mask < 0.0;
	//output.base_color = lerp(1, pixel.key_color.xyz, color_mask) * cm.xyz;
	
	output.normal = N;
	output.gloss = toksvig_gloss(ng.w, min(normalmap_len, 1));
	output.metalness = cm.w;
	output.transparency = 0;
	output.id = pixel.material_index;

	output.base_color *= pixel.color_mul;
	// bc7 compression artifacts can give byte value 1 for 0, which should more visible than small shift
	output.emissive = max(output.emissive, saturate(extras.y - 1/255. + pixel.emissive));
	

	#ifdef FOLIAGE
		output.id = 1;
	#endif
	output.ao = ao;

#endif

#ifdef DITHERED

	float tex_dither = Dither8x8[(uint2)pixel.screen_position % 8];
	float object_dither = pixel.custom_alpha;

	tex_dither = object_dither >= 0 ? tex_dither : -tex_dither;
	if(tex_dither < object_dither) {
		DISCARD_PIXEL;
	}
#endif

#if defined(MS_SAMPLE_COUNT) && defined(ALPHA_MASKED)
	// some compiler bug? if I make it earlier it skips instructions
	if(output.coverage == 0)
	{
		DISCARD_PIXEL;
	}
#endif
}