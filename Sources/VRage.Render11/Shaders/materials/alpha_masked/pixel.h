#include <brdf.h>
#include <alphamaskViews.h>

float3x3 pixel_tangent_space(float3 N, float3 pos, float2 uv) {
	float3 dp1 = ddx(pos);
	float3 dp2 = ddy(pos);
	float2 duv1 = ddx(uv);
	float2 duv2 = ddy(uv);
	float3 dp2perp = cross(dp2, N);
	float3 dp1perp = cross(N, dp1);

	float3 T = dp2perp * duv1.x + dp1perp * duv2.x;
	float3 B = dp2perp * duv1.y + dp1perp * duv2.y;

	float invmax = rsqrt(max(dot(T, T), dot(B, B)));
	return float3x3(T * invmax, B * invmax, N);
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


float2 transformUV(int index, float3 inCDir)
{
	float3x3 m = (float3x3)alphamask_constants.impostor_view_matrix[index];
	float3 trTex = mul(inCDir, m);
	//trTex = 1 - (trTex / 2.0 + 0.5);
	trTex = (trTex / 2.0 + 0.5);
	trTex.y = 1 - trTex.y;
	//trTex.x = 1 - trTex.x;
	return trTex.xy;
}


float4 sampleColor(int index, float3 cDir)
{
#ifdef ALPHA_MASK_ARRAY
	int NT = 181;

	float3 tex = float3(transformUV(index % NT, cDir), (index * 2));
	return AlphaMaskArrayTexture.Sample(AlphamaskArraySampler, tex);
#else
	return 0;
#endif
}

float4 sampleTree(int index, float3 cDir)
{
#ifdef ALPHA_MASK_ARRAY
	int NT = 181;
	float3 tex = float3(transformUV(index % NT, cDir), index * 2 + 1);
	return AlphaMaskArrayTexture.Sample(AlphamaskArraySampler, tex);
#else
	return 0;
#endif
}





void pixel_program(PixelInterface pixel, inout MaterialOutputInterface output)
{
	
	if (object_.facing != 3)
	{
#if !defined(MS_SAMPLE_COUNT) || defined(DEPTH_ONLY)

		float alpha = AlphamaskTexture.Sample(TextureSampler, pixel.custom.texcoord0).x;
		if (alpha < 0.5f)
		{
			DISCARD_PIXEL;
		}

#else

		output.coverage = 0;
		[unroll]
		for (int s = 0; s < MS_SAMPLE_COUNT; s++)
		{
			float2 sample_texcoord = EvaluateAttributeAtSample(pixel.custom.texcoord0, s);
			float alpha = AlphamaskTexture.Sample(TextureSampler, sample_texcoord).x;
			output.coverage |= (alpha > 0.5f) ? (uint(1) << s) : 0;
		}

#endif
	}


	
	float4 cm = 0;
	float alpha = 0;


#ifndef DEPTH_ONLY
	//float4 cm = float4(pixel.custom.texcoord0.xy, 0, 1);
	//float4 cm = float4(pixel.custom.cDir.xy, 0, 1);
	float4 extras = AmbientOcclusionTexture.Sample(TextureSampler, pixel.custom.texcoord0);
	float4 ng = NormalGlossTexture.Sample(TextureSampler, pixel.custom.texcoord0);
#endif

	
	float2 texCoords = pixel.custom.texcoord0;


	if (object_.facing == 3)
	{
	
		//float3 p0 = (pixel.custom.cDir * 20 - pixel.custom.world_pos) / 20.0;
		float3 p0 = pixel.custom.cDir / 20; //scaled tree
		//float3 camDir = normalize(pixel.custom.cDir);
		float3 camDirW = normalize(pixel.custom.cPos);


		float4 t1 = sampleTree(pixel.custom.view_indices.x, p0);
		float4 t2 = sampleTree(pixel.custom.view_indices.y, p0);
		float4 t3 = sampleTree(pixel.custom.view_indices.z, p0);
		float4 tIt = (t1 * pixel.custom.view_blends.x + t2 * pixel.custom.view_blends.y + t3 * pixel.custom.view_blends.z);

		float4 cm1 = sampleColor(pixel.custom.view_indices.x, p0);
		float4 cm2 = sampleColor(pixel.custom.view_indices.y, p0);
		float4 cm3 = sampleColor(pixel.custom.view_indices.z, p0);
		float4 cmIt = (cm1 * pixel.custom.view_blends.x + cm2 * pixel.custom.view_blends.y + cm3 * pixel.custom.view_blends.z);

		float3 offsetDir = 0;
		float3 p = p0;

		/*if (cmIt.a > 0.2)
		{
			cmIt.r /= (cmIt.a == 0.0 ? 1.0 : cmIt.a);
			float t = (cmIt.r * (2.0 * sqrt(2.0)) - sqrt(2.0));
			t = 1.0 - t;
			p = p0 - 0.25 * camDirW * t;

			cm1 = sampleColor(pixel.custom.view_indices.x, p);
			cm2 = sampleColor(pixel.custom.view_indices.y, p);
			cm3 = sampleColor(pixel.custom.view_indices.z, p);

			cmIt = (cm1 * pixel.custom.view_blends.x + cm2 * pixel.custom.view_blends.y + cm3 * pixel.custom.view_blends.z);

			cmIt.r /= (cmIt.a == 0.0 ? 1.0 : cmIt.a);
			t = (cmIt.r * (2.0 * sqrt(2.0)) - sqrt(2.0));
			t = 1.0 - t;
			p = p0 - 0.15 * camDirW * t;
		}*/

		cm = cmIt;
		alpha = tIt.w;

		if (alpha > 0)
		{

			float3 newP = p0 - camDirW * tIt.r * 0.5f;

			float4 lm1 = sampleTree(pixel.custom.view_indices_light.x, newP);
			float4 lm2 = sampleTree(pixel.custom.view_indices_light.y, newP);
			float4 lm3 = sampleTree(pixel.custom.view_indices_light.z, newP);
			

#ifndef DEPTH_ONLY

			float4 lm = lm1 * pixel.custom.view_blends_light.x + lm2 * pixel.custom.view_blends_light.y + lm3 * pixel.custom.view_blends_light.z;
			lm.r /= (lm.a == 0.0 ? 1.0 : lm.a);

			float d = (lm.r * (2.0 * sqrt(2.0)) - sqrt(2.0)) - dot(normalize(newP), pixel.custom.lDir);
			float kc = 1 - exp(-10.0*max(d, 0.0));

			ng = float4(0, 0, 1, 0.5);

			extras = float4(tIt.z, 0, 0, 0);
		/*	if (pixel.custom.view_indices.x > 181)
				cm.xyz = float3(0.01f, 0.01f, 0);
			else
				cm.xyz = float3(0.01f, 0.02f, 0.01);

*/
			//cm.xyz = calculate_shadow_fast_aprox(newP);
			//cm.xyz = lerp(cm.xyz, pixel.key_color, tIt.r);

			cm.w = 0;

			//cm.xyz = pixel.custom.lDir;
			//cm.xyz = float3(1,0,0);
			//cm = lm;
			cm.xyz += cm.xyz * kc.xxx * frame_.directionalLightColor;

			//if ((int)pixel.custom.view_indices.z == 120)
			//cm.xyz = float3(0, 0, 1);
			//cm.xyz = tIt.w;
			//cm.xyz = tIt.r;
			//cm.xyz = tIt.z;

#endif

			float proj43 = frame_.projection_matrix._43;
			float proj33 = frame_.projection_matrix._33;

			//x = -proj43 / (depth + proj33);
			//depth = -proj43 / x - proj33;
			
			float linearDepth = -proj43 / (pixel.screen_position.z + proj33);
			float newDepth = linearDepth + 40 * tIt.r;

			output.depth = -proj43 / newDepth - proj33;
		}

		if (alpha < 0.2)
		{
			DISCARD_PIXEL;
		}
	}
	else
	{
#ifndef ALPHA_MASK_ARRAY
		cm = ColorMetalTexture.Sample(TextureSampler, pixel.custom.texcoord0);
		alpha = AlphamaskTexture.Sample(TextureSampler, pixel.custom.texcoord0).x;

		float proj43 = frame_.projection_matrix._43;
		float proj33 = frame_.projection_matrix._33;

		//x = -proj43 / (depth + proj33);
		//depth = -proj43 / x - proj33;

		float linearDepth = -proj43 / (pixel.screen_position.z + proj33);
		float newDepth = linearDepth + 20 * cm.w * cm.w * cm.w;

		output.depth = -proj43 / newDepth - proj33;

		//cm.xyz = cm.w;
#endif
	}
#ifndef DEPTH_ONLY


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

	if (pixel.material_flags & MATERIAL_FLAG_RGB_COLORING) 
	{
		float3 c_rgb = hsv_to_rgb(saturate(pixel.key_color.xyz * float3(1, 0.5, 0.5) + float3(0, 0.5, 0.5)));
		output.base_color = lerp(1, c_rgb, color_mask) * cm.xyz;
	}
	else 
	{
		output.base_color = colorize_1(cm.xyz, pixel.key_color.xyz, color_mask);
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

	/*output.base_color = float3(pixel.custom.texcoord0.xy, 0);
	output.base_color = pixel.custom.normal;
	output.base_color = horizontalIndex.xxx;*/


	// bc7 compression artifacts can give byte value 1 for 0, which should more visible than small shift
	output.emissive = max(output.emissive, saturate(extras.y - 1 / 255. + pixel.emissive));

	if (object_.facing)
	{
		output.id = 2;
	}
	output.ao = ao;

#endif

#ifdef DITHERED

	float tex_dither = Dither8x8[(uint2)pixel.screen_position.xy % 8];
	float object_dither = abs(pixel.custom_alpha);

	if (object_dither > 1)
	{
		object_dither -= 1.0f;
		object_dither = 1.0f - object_dither;
		if (tex_dither > object_dither)
		{
			DISCARD_PIXEL;
		}
	}
	else if (tex_dither < object_dither)
	{
		DISCARD_PIXEL;
	}
#endif

#if defined(MS_SAMPLE_COUNT) && defined(ALPHA_MASKED)
	// some compiler bug? if I make it earlier it skips instructions
	if (output.coverage == 0)
	{
		DISCARD_PIXEL;
	}
#endif
}