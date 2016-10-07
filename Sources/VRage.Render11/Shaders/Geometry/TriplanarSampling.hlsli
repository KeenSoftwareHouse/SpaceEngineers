#define INVERT_NM_Y
#define DEBUG_TEX_COORDS 0

float4 sample_color_triplanar_grad(Texture2DArray<float4> texture_array, float3 texcoords, float3 weights, float3 N,
	float2 texcoords_ddx[3], float2 texcoords_ddy[3], float f)
{
    float2 texcoords_x = texcoords.zy * f;
    float2 texcoords_y = texcoords.xz * f;
    float2 texcoords_z = texcoords.xy * f;

    //float4 result = float4(weights.xxx, 1);
    float4 result = 
		texture_array.SampleGrad(TextureSampler, float3(texcoords_x, 0), texcoords_ddx[0] * f, texcoords_ddy[0] * f) * weights.x +
		texture_array.SampleGrad(TextureSampler, float3(texcoords_y, 1), texcoords_ddx[1] * f, texcoords_ddy[1] * f) * weights.y +
		texture_array.SampleGrad(TextureSampler, float3(texcoords_z, 0), texcoords_ddx[2] * f, texcoords_ddy[2] * f) * weights.z;

#ifdef DEBUG
#if DEBUG_TEX_COORDS
	result.rgb = texcoords;
#endif
#endif

	return result;
}

float3x3 PixelTangentSpaceX(float3 N, float3 dpxperp, float3 dpyperp, float2 uv_ddx, float2 uv_ddy)
{
    float3 T = dpyperp * uv_ddx.x + dpxperp * uv_ddy.x;
    float3 B = dpyperp * uv_ddx.y + dpxperp * uv_ddy.y;

    float invmax = rsqrt(max(dot(T, T), dot(B, B)));
    return float3x3(T * invmax, B * invmax, N);
}

float4 sample_normal_gloss_triplanar_grad(Texture2DArray<float4> texture_array, float3 texcoords, float3 weights, float3 N,
	float2 texcoords_ddx[3], float2 texcoords_ddy[3], float f, float3 dpxperp, float3 dpyperp)
{
	float2 texcoords_x = texcoords.zy * f;
	float2 texcoords_y = texcoords.xz * f;
	float2 texcoords_z = texcoords.xy * f;

	float4 nm_gloss_x = texture_array.SampleGrad(TextureSampler, float3(texcoords_x, 0), texcoords_ddx[0] * f, texcoords_ddy[0] * f);
	float4 nm_gloss_y = texture_array.SampleGrad(TextureSampler, float3(texcoords_y, 1), texcoords_ddx[1] * f, texcoords_ddy[1] * f);
	float4 nm_gloss_z = texture_array.SampleGrad(TextureSampler, float3(texcoords_z, 0), texcoords_ddx[2] * f, texcoords_ddy[2] * f);

	float gloss = dot(float3(nm_gloss_x.w, nm_gloss_y.w, nm_gloss_z.w), weights);

	float3 nx = nm_gloss_x.xyz * 2 - 1;
	float3 ny = nm_gloss_y.xyz * 2 - 1;
	float3 nz = nm_gloss_z.xyz * 2 - 1;
    nx.y = -nx.y;
    ny.y = -ny.y;
    nz.y = -nz.y;

    nx = mul(nx, PixelTangentSpaceX(N, dpxperp, dpyperp, texcoords_ddx[0] * f, texcoords_ddy[0] * f));
    ny = mul(ny, PixelTangentSpaceX(N, dpxperp, dpyperp, texcoords_ddx[1] * f, texcoords_ddy[1] * f));
    nz = mul(nz, PixelTangentSpaceX(N, dpxperp, dpyperp, texcoords_ddx[2] * f, texcoords_ddy[2] * f));
	float3 Nt = nx * weights.x + ny * weights.y + nz * weights.z;

	return float4(Nt, gloss);
}

void calc_derivatives(float3 texcoords, out float2 t_dx[3], out float2 t_dy[3])
{
	float2 texcoords_x = texcoords.zy;
	float2 texcoords_y = texcoords.xz;
	float2 texcoords_z = texcoords.xy;

	t_dx[0] = ddx(texcoords_x);
	t_dy[0] = ddy(texcoords_x);
	t_dx[1] = ddx(texcoords_y);
	t_dy[1] = ddy(texcoords_y);
	t_dx[2] = ddx(texcoords_z);
	t_dy[2] = ddy(texcoords_z);
}

struct TriplanarMaterialConstants
{
	float4 distance_and_scale;  //x = initial scale, y = initial distance, z = scale multiplier, w = distance multiplier
	float4 distance_and_scale_far; //x = far1 texture scale, y = switch to far1 texture, z = far2 texture scale, w = switch to far2 texture
	float2 distance_and_scale_far3;
	float extension_detail_scale;
	float _padding;
	float4 color_far3;	
};

struct TriplanarOutput
{
	float4 ext;
	float4 color_metal;
	float4 normal_gloss;
};

float4 GetNearestDistanceAndScale(float distance, float4 materialSettings)
{
	float curDistance = 0;
	float curScale = materialSettings.x;

	float nextDistance = materialSettings.y;
	float nextScale = materialSettings.z;


	while (nextDistance < distance)
	{
		curDistance = nextDistance;
		curScale = nextScale;

		nextDistance *= materialSettings.w;
		nextScale *= materialSettings.z;
	}

	return float4(curDistance, nextDistance, curScale, nextScale);
}

void SampleTriplanar(int startIndex, TriplanarMaterialConstants material, float d, float3 N, float3 weights, float3 voxelOffset, 
    float3 dpxperp, float3 dpyperp, float3 texcoords, float2 texcoords_ddx[3], float2 texcoords_ddy[3], out TriplanarOutput output)
{
	float4 das = GetNearestDistanceAndScale(d, material.distance_and_scale);
	
    float scale_near = das.z;
	float scale_far = das.w;

	float distance_near = das.x;
	float distance_far = das.y;

	uint texture_near = 0;
	uint texture_far = 0;

	float pixelizationDistance = 10;
	
    // applies offset and .. when texture threshold distance is further then 10 meters
	float pixelizationMultiplier_near = step(pixelizationDistance, distance_near);
	float pixelizationMultiplier_far = step(pixelizationDistance, distance_far);

	if (material.distance_and_scale_far.y > 0)
	{
		if (distance_near >= material.distance_and_scale_far.y)
		{
			scale_near = material.distance_and_scale_far.x;
			texture_near = 1;
		}
		if (distance_far >= material.distance_and_scale_far.y)
		{
			scale_far = material.distance_and_scale_far.x;
			texture_far = 1;
		}
	}

	if (material.distance_and_scale_far.w > 0)
	{
		if (distance_near >= material.distance_and_scale_far.w)
		{
			scale_near = material.distance_and_scale_far.z;
			texture_near = 2;
		}
		if (distance_far >= material.distance_and_scale_far.w)
		{
			scale_far = material.distance_and_scale_far.z;
			texture_far = 2;
		}
	}

	if (material.distance_and_scale_far3.y > 0)
	{
		if (distance_near >= material.distance_and_scale_far3.y)
		{
			scale_near = material.distance_and_scale_far3.x;
			texture_near = 3;
		}
		if (distance_far >= material.distance_and_scale_far3.y)
		{
			scale_far = material.distance_and_scale_far3.x;
			texture_far = 3;
		}
	}

	float scale_weight = saturate(((d - distance_near) / (distance_far - distance_near) - 0.25f) * 1.5f);

	scale_near = 1.0f / scale_near;
	scale_far = 1.0f / scale_far;

    float3 offset_near = pixelizationMultiplier_near * voxelOffset;
    float3 offset_far = pixelizationMultiplier_far * voxelOffset;

	float3 texcoords_near = (texcoords + offset_near);
	float3 texcoords_far = (texcoords + offset_far);

	float4 color_near = float4(0, 0, 0, 0);
	float4 normal_gloss_near = float4(0, 0, 0, 0);
	float4 ext_near = float4(0, 0, 0, 0);

	float4 color_far = float4(0, 0, 0, 0);
	float4 normal_gloss_far = float4(0, 0, 0, 0);
	float4 ext_far = float4(0, 0, 0, 0);

    if (texture_near == 0)
	{
		color_near = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[startIndex + 0], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
		normal_gloss_near = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[startIndex + 0], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near, dpxperp, dpyperp);
		ext_near = sample_color_triplanar_grad(Ext_BottomSides_Up[startIndex + 0], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
	}
	else if (texture_near == 1)
	{
		color_near = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[startIndex + 1], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
		normal_gloss_near = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[startIndex + 1], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near, dpxperp, dpyperp);
		ext_near = sample_color_triplanar_grad(Ext_BottomSides_Up[startIndex + 1], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
	}
	else if (texture_near == 2)
	{
		color_near = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[startIndex + 2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
		normal_gloss_near = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[startIndex + 2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near, dpxperp, dpyperp);
		ext_near = sample_color_triplanar_grad(Ext_BottomSides_Up[startIndex + 2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
	}
	else if (texture_near == 3)
	{
		color_near = float4(material.color_far3.xyz, 0);
		normal_gloss_near = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[startIndex + 2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near, dpxperp, dpyperp);
		ext_near = sample_color_triplanar_grad(Ext_BottomSides_Up[startIndex + 2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
	}

	if (texture_far == 0)
	{
		color_far = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[startIndex + 0], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
		normal_gloss_far = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[startIndex + 0], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far, dpxperp, dpyperp);
		ext_far = sample_color_triplanar_grad(Ext_BottomSides_Up[startIndex + 0], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
	}
	else if (texture_far == 1)
	{
		color_far = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[startIndex + 1], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
		normal_gloss_far = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[startIndex + 1], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far, dpxperp, dpyperp);
		ext_far = sample_color_triplanar_grad(Ext_BottomSides_Up[startIndex + 1], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
	}
	else if (texture_far == 2)
	{
		color_far = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[startIndex + 2], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
		normal_gloss_far = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[startIndex + 2], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far, dpxperp, dpyperp);
		ext_far = sample_color_triplanar_grad(Ext_BottomSides_Up[startIndex + 2], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
	}
	else if (texture_far == 3)
	{
		color_far = float4(material.color_far3.xyz, 0);
		normal_gloss_far = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[startIndex + 2], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far, dpxperp, dpyperp);
		ext_far = sample_color_triplanar_grad(Ext_BottomSides_Up[startIndex + 2], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
	}

	float highPass = 1;

	if (material.extension_detail_scale > 0)
	{
		float4 highPass1 = 1;
		float4 highPass2 = 1;

		if (pixelizationMultiplier_near > 0)
		{
			if (texture_near == 0)
			{
				highPass1 = sample_color_triplanar_grad(Ext_BottomSides_Up[startIndex + 0], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, material.extension_detail_scale);
			}
			else if (texture_near == 1)
			{
				highPass1 = sample_color_triplanar_grad(Ext_BottomSides_Up[startIndex + 1], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, material.extension_detail_scale);
			}
			else if (texture_near == 2)
			{
				highPass1 = sample_color_triplanar_grad(Ext_BottomSides_Up[startIndex + 2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, material.extension_detail_scale);
			}
    	}
	
		if (pixelizationMultiplier_far > 0)
		{
			if (texture_far == 0)
			{
				highPass2 = sample_color_triplanar_grad(Ext_BottomSides_Up[startIndex + 0], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, material.extension_detail_scale);
			}
			else if (texture_far == 1)
			{
				highPass2 = sample_color_triplanar_grad(Ext_BottomSides_Up[startIndex + 1], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, material.extension_detail_scale);
			}
			else if (texture_far == 2)
			{
				highPass2 = sample_color_triplanar_grad(Ext_BottomSides_Up[startIndex + 2], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, material.extension_detail_scale);
			}
		}

		highPass = lerp(highPass1.z, highPass2.z, scale_weight);
	}

	//x = AO
	//y = emissivity
	//z = lowFreq noise
	//a = alpha mask
	output.ext = lerp(ext_near, ext_far, scale_weight);
	output.color_metal = lerp(color_near, color_far, scale_weight) * float4(highPass.xxx, 1);
	output.normal_gloss = lerp(normal_gloss_near, normal_gloss_far, scale_weight);
}
