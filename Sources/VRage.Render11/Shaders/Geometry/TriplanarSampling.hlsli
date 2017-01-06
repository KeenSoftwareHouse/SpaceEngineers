#ifndef INCLUDE_TRIPLANAR_SAMPLING_HLSLI
#define INCLUDE_TRIPLANAR_SAMPLING_HLSLI

#include <Geometry/Materials/TriplanarMaterialConstants.hlsli>

#define DEBUG_TEX_COORDS 0
#define DEBUG_ENABLE_LOOPS_WATCHDOG

struct TriplanarInterface
{
    float3 N;
    float3 weights;
    float3 dpxperp;
    float3 dpyperp;
    float2 ddxTexcoords[3];
    float2 ddyTexcoords[3];
    float3 texcoords;
    float d;
};

struct TriplanarOutput
{
    float4 ext;
    float4 cm;
    float4 ng;
};

float3 GetTriplanarWeights(float3 n)
{
    float3 w = saturate(abs(n) - 0.55);

#if 1
    // This speeds up rendering when dynamic branching optimizations withing tri-planar shader are enabled
    w *= w;
    w *= w;
#endif

    // normalize
    return w / dot(w, 1);
}

void ProcessDithering(PixelInterface pixel, inout MaterialOutputInterface output)
{
#ifdef DITHERED
    float3 lightDir = normalize(frame_.Light.directionalLightVec);
        float3 nrm = normalize(pixel.custom.normal.xyz);
        float shadowTreshold = -0.2f;

    // < 0 dark side; >0 light side
    float voxelSide = shadowTreshold - dot(lightDir, nrm);

    float tex_dither = Dither8x8[(uint2)pixel.screen_position.xy % 8];
    float object_dither = abs(pixel.custom_alpha);
    if (object_dither > 2)
    {
        object_dither -= 2.0f;
        object_dither = 2.0f - object_dither;

        if (object_dither > 1)
        {
#ifdef DEPTH_ONLY
            object_dither -= 1;

            if (voxelSide > 0)
                clip(object_dither - tex_dither);

#endif
        }
        else
        { //0 - 1
#ifdef DEPTH_ONLY
            clip(-voxelSide);
#else
            clip(object_dither - tex_dither);
#endif
        }
    }
    else
    {
        if (object_dither > 1)
        {
#ifdef DEPTH_ONLY
            clip(-voxelSide);
#else

            object_dither -= 1;
            clip(tex_dither - object_dither);
#endif
        }
        else // 1 - 2
        {
#ifdef DEPTH_ONLY
            if (voxelSide > 0)
            {
                clip(tex_dither - object_dither);
            }
#endif
        }
    }
#endif
}

float4 SampleColorTriplanarGrad(Texture2DArray<float4> tex, int sliceIndexXZnY, int sliceIndexY,
    float3 texcoords, TriplanarInterface triplanarInput, float f, uniform int forcedAxis = -1)
{
	float2 texcoordsX = texcoords.zy * f;
    float2 texcoordsY = texcoords.xz * f;
    float2 texcoordsZ = texcoords.xy * f;

    float4 res0 = tex.SampleGrad(TextureSampler, float3(texcoordsX, sliceIndexXZnY), triplanarInput.ddxTexcoords[0] * f, triplanarInput.ddyTexcoords[0] * f);
    float4 res1 = tex.SampleGrad(TextureSampler, float3(texcoordsY, sliceIndexY), triplanarInput.ddxTexcoords[1] * f, triplanarInput.ddyTexcoords[1] * f);
    float4 res2 = tex.SampleGrad(TextureSampler, float3(texcoordsZ, sliceIndexXZnY), triplanarInput.ddxTexcoords[2] * f, triplanarInput.ddyTexcoords[2] * f);

    float4 result = res0 * triplanarInput.weights.x + res1 * triplanarInput.weights.y + res2 * triplanarInput.weights.z;
	
	if (forcedAxis == 0)
	{
		result = res0;
	}
	else if (forcedAxis == 1)
	{
		result = res1;
	}
	else if (forcedAxis == 2)
	{
		result = res2;
	}

#ifdef DEBUG
#if DEBUG_TEX_COORDS
	result.rgb = texcoords;
#endif
#endif

	return result;
}

float4 AdjustGloss(float4 ng)
{
    //ng.w = srgb_to_rgb(ng.w);
    //ng.w = ToksvigGloss(ng.w, min(length(ng.xyz * 2 - 1), 1));
    return ng;
}

float4 SampleNormalTriplanarGrad(Texture2DArray<float4> tex, int sliceIndexXZnY, int sliceIndexY,
    float3 texcoords, TriplanarInterface triplanarInput, float f, uniform int forcedAxis = -1)
{
    float2 texcoordsX = texcoords.zy * f;
    float2 texcoordsY = texcoords.xz * f;
    float2 texcoordsZ = texcoords.xy * f;

    float4 ngX = tex.SampleGrad(TextureSampler, float3(texcoordsX, sliceIndexXZnY), 
        triplanarInput.ddxTexcoords[0] * f, triplanarInput.ddyTexcoords[0] * f);
    float4 ngY = tex.SampleGrad(TextureSampler, float3(texcoordsY, sliceIndexY), 
        triplanarInput.ddxTexcoords[1] * f, triplanarInput.ddyTexcoords[1] * f);
    float4 ngZ = tex.SampleGrad(TextureSampler, float3(texcoordsZ, sliceIndexXZnY), 
        triplanarInput.ddxTexcoords[2] * f, triplanarInput.ddyTexcoords[2] * f);

    //float3 glossV = float3(ngX.w, ngY.w, ngZ.w);
    ngX = AdjustGloss(ngX);
    ngY = AdjustGloss(ngY);
    ngZ = AdjustGloss(ngZ);
    float3 glossV = float3(ngX.w, ngY.w, ngZ.w);
    float gloss = dot(glossV, triplanarInput.weights);

    float3 nx = ngX.xyz;
    float3 ny = ngY.xyz;
    float3 nz = ngZ.xyz;
    float3 Nt = nx * triplanarInput.weights.x + ny * triplanarInput.weights.y + nz * triplanarInput.weights.z;
	
	if (forcedAxis == 0)
        return ngX;
	
	if (forcedAxis == 1)
        return ngY;

	if (forcedAxis == 2)
        return ngZ;

	return float4(Nt, gloss);
}

float4 GetNearestDistanceAndScale(float distance, float4 materialSettings)
{
	//float curDistance = 0;
	//float curScale = materialSettings.x;

	//float nextDistance = materialSettings.y;
	//float nextScale = materialSettings.z;

	//float4 output = float4(curDistance, nextDistance, curScale, nextScale);
	//float2 step = float2(materialSettings.w, materialSettings.z);

	//while (output.y < distance)
	//{
	//	output.xz = output.yw;
	//	output.yw *= step;
	//}
	//return output;

	float curDistance = 0;
	float curScale = materialSettings.x;

	float nextDistance = materialSettings.y;
	float nextScale = materialSettings.z;

#if defined(DEBUG_ENABLE_LOOPS_WATCHDOG)
	float dbgWatchDogCnt = 0;
#endif
	
	while (nextDistance < distance)
	{
		curDistance = nextDistance;
		curScale = nextScale;

		nextDistance *= materialSettings.w;
		nextScale *= materialSettings.z;

#if defined(DEBUG_ENABLE_LOOPS_WATCHDOG)
		if (++dbgWatchDogCnt > 16)
		{
			break;
		}
#endif
	}
	return float4(curDistance, nextDistance, curScale, nextScale);
}

struct SlicesNum
{
	int sliceColorMetalXZnY;
	int sliceColorMetalY;
	int sliceNormalGlossXZnY;
	int sliceNormalGlossY;
	int sliceExtXZnY;
	int sliceExtY;
};

SlicesNum GetSlices(TriplanarMaterialConstants material, int nDistance)
{
	SlicesNum slices;
	slices.sliceColorMetalXZnY = material.slices[nDistance].slices1.x;
	slices.sliceColorMetalY = material.slices[nDistance].slices1.y;
	slices.sliceNormalGlossXZnY = material.slices[nDistance].slices1.z;
	slices.sliceNormalGlossY = material.slices[nDistance].slices1.w;
	slices.sliceExtXZnY = material.slices[nDistance].slices2.x;
	slices.sliceExtY = material.slices[nDistance].slices2.y;
	return slices;
}

void SampleTriplanar(int startIndex, TriplanarMaterialConstants material, TriplanarInterface triplanarInput, 
    out TriplanarOutput output, uniform int forcedAxis = -1)
{
    float4 das = GetNearestDistanceAndScale(triplanarInput.d, material.distance_and_scale);

	float distanceNear = das.x;
	float distanceFar = das.y;

	float scaleNear = das.z;
    float scaleFar = das.w;

	float textureNear = 0;
    float textureFar = 0;
	
	float pixelizationDistance = 10;

	// applies offset and .. when texture threshold distance is further then 10 meters
    float pixelizationMultiplierNear = step(pixelizationDistance, distanceNear);
    float pixelizationMultiplierFar = step(pixelizationDistance, distanceFar);
	
	if (material.distance_and_scale_far.y > 0)
	{
        if (distanceNear >= material.distance_and_scale_far.y)
		{
            scaleNear = material.distance_and_scale_far.x;
            textureNear = material.distance_and_scale_far.z;
		}
        if (distanceFar >= material.distance_and_scale_far.y)
		{
            scaleFar = material.distance_and_scale_far.x;
			textureFar = material.distance_and_scale_far.z;
		}
	}

	if (material.distance_and_scale_far2.y > 0)
	{
        if (distanceNear >= material.distance_and_scale_far2.y)
		{
            scaleNear = material.distance_and_scale_far2.x;
            textureNear = material.distance_and_scale_far2.z;
		}
        if (distanceFar >= material.distance_and_scale_far2.y)
		{
            scaleFar = material.distance_and_scale_far2.x;
            textureFar = material.distance_and_scale_far2.z;
		}
	}

	if (material.distance_and_scale_far3.y > 0)
	{
        if (distanceNear >= material.distance_and_scale_far3.y)
		{
            scaleNear = material.distance_and_scale_far3.x;
            textureNear = material.distance_and_scale_far3.z;
		}
        if (distanceFar >= material.distance_and_scale_far3.y)
		{
            scaleFar = material.distance_and_scale_far3.x;
            textureFar = material.distance_and_scale_far3.z;
		}
	}

    float scaleWeight = saturate(((triplanarInput.d - distanceNear) / (distanceFar - distanceNear) - 0.5f) * 2.0f);

    SlicesNum slicesNear = GetSlices(material, min(2, textureNear));
    SlicesNum slicesFar = GetSlices(material, min(2, textureFar));

    scaleNear = 1.0f / scaleNear;
    scaleFar = 1.0f / scaleFar;

    float3 voxelOffset = 0;
#ifdef USE_VOXEL_DATA
        voxelOffset = object_.voxel_offset;
#endif
    float3 offsetNear = pixelizationMultiplierNear * voxelOffset;
	float3 offsetFar = pixelizationMultiplierFar * voxelOffset;

    float3 texcoordsNear = (triplanarInput.texcoords + offsetNear);
    float3 texcoordsFar = (triplanarInput.texcoords + offsetFar);

	float4 cmNear = float4(0, 0, 0, 0);
    float4 ngNear = float4(0, 0, 0, 0);
	float4 extNear = float4(0, 0, 0, 0);

    float4 cmFar = float4(0, 0, 0, 0);
    float4 ngFar = float4(0, 0, 0, 0);
	float4 extFar = float4(0, 0, 0, 0);

	[branch]
	if (scaleWeight <= 0.995f)
	{
        cmNear = SampleColorTriplanarGrad(ColorMetal, slicesNear.sliceColorMetalXZnY, slicesNear.sliceColorMetalY,
            texcoordsNear, triplanarInput, scaleNear, forcedAxis);

        ngNear = SampleNormalTriplanarGrad(NormalGloss, slicesNear.sliceNormalGlossXZnY, slicesNear.sliceNormalGlossY,
            texcoordsNear, triplanarInput, scaleNear, forcedAxis);

        extNear = SampleColorTriplanarGrad(Ext, slicesNear.sliceExtXZnY, slicesNear.sliceExtY,
            texcoordsNear, triplanarInput, scaleNear, forcedAxis);
	}

    if (textureNear == 3)
        cmNear = float4(material.color_far3.xyz, 0);


	[branch]
	if (scaleWeight >= 0.005f)
	{
        cmFar = SampleColorTriplanarGrad(ColorMetal, slicesFar.sliceColorMetalXZnY, slicesFar.sliceColorMetalY,
            texcoordsFar, triplanarInput, scaleFar, forcedAxis);

        ngFar = SampleNormalTriplanarGrad(NormalGloss, slicesFar.sliceNormalGlossXZnY, slicesFar.sliceNormalGlossY,
            texcoordsFar, triplanarInput, scaleFar, forcedAxis);

        extFar = SampleColorTriplanarGrad(Ext, slicesFar.sliceExtXZnY, slicesFar.sliceExtY,
            texcoordsFar, triplanarInput, scaleFar, forcedAxis);
	}

    if (textureFar == 3)
        cmFar = float4(material.color_far3.xyz, 0);

	float highPass = 1;

	if (material.extension_detail_scale > 0)
	{
		float4 highPass1 = 1;
		float4 highPass2 = 1;

		if (pixelizationMultiplierNear > 0)
		{
            highPass1 = SampleColorTriplanarGrad(Ext, slicesNear.sliceExtXZnY, slicesNear.sliceExtY,
                texcoordsNear, triplanarInput, material.extension_detail_scale, forcedAxis);

            if (textureNear >= 3)
				highPass1 = 1; // use default value;
		}

		if (pixelizationMultiplierFar > 0)
		{
            highPass2 = SampleColorTriplanarGrad(Ext, slicesFar.sliceExtXZnY, slicesFar.sliceExtY,
                texcoordsFar, triplanarInput, material.extension_detail_scale, forcedAxis);

            if (textureFar >= 3)
				highPass2 = 1; // use default value;
		}

		highPass = lerp(highPass1.z, highPass2.z, scaleWeight);
	}

	//x = AO
	//y = emissivity
	//z = lowFreq noise
	//a = alpha mask

	output.ext = lerp(extNear, extFar, scaleWeight);
    output.cm = lerp(cmNear, cmFar, scaleWeight) * float4(highPass.xxx, 1);
    output.ng = lerp(ngNear, ngFar, scaleWeight);
}

void SampleTriplanarBranched(int startIndex, TriplanarMaterialConstants material, TriplanarInterface triplanarInput, out TriplanarOutput triplanarOutput)
{
    const float threshold = 0.995f;

    [branch]
    if (triplanarInput.weights.x >= threshold)
    {
        SampleTriplanar(startIndex, material, triplanarInput, triplanarOutput, 0);
    }
    else
    {
        [branch]
        if (triplanarInput.weights.y >= threshold)
        {
            SampleTriplanar(startIndex, material, triplanarInput, triplanarOutput, 1);
        }
        else
        {
            [branch]
            if (triplanarInput.weights.z >= threshold)
            {
                SampleTriplanar(startIndex, material, triplanarInput, triplanarOutput, 2);
            }
            else
            {
                SampleTriplanar(startIndex, material, triplanarInput, triplanarOutput);
            }
        }
    }
}

float3x3 TriplanarTangentSpace(TriplanarInterface triplanarInput, int index)
{
    float3 T = triplanarInput.dpyperp * triplanarInput.ddxTexcoords[index].x + triplanarInput.dpxperp * triplanarInput.ddyTexcoords[index].x;
        float3 B = triplanarInput.dpyperp * triplanarInput.ddxTexcoords[index].y + triplanarInput.dpxperp * triplanarInput.ddyTexcoords[index].y;

    float invmax = rsqrt(max(dot(T, T), dot(B, B)));
    return float3x3(T * invmax, B * invmax, triplanarInput.N);
}

void FeedOutputTriplanar(PixelInterface pixel, TriplanarInterface triplanarInput, TriplanarOutput triplanar, inout MaterialOutputInterface output)
{
    float3 tgN = triplanar.ng.xyz * 2 - 1;

    tgN.x = -tgN.x;

    float3 nx = mul(tgN, TriplanarTangentSpace(triplanarInput, 0));
    float3 ny = mul(tgN, TriplanarTangentSpace(triplanarInput, 1));
    float3 nz = mul(tgN, TriplanarTangentSpace(triplanarInput, 2));

    triplanar.ng.xyz = nx * triplanarInput.weights.x + ny * triplanarInput.weights.y + nz * triplanarInput.weights.z;

    output.base_color = triplanar.cm.xyz;
    if (frame_.Voxels.DebugVoxelLod == 1.0f)
    {
        float voxelLodSize = 0;
#ifdef USE_VOXEL_DATA
        voxelLodSize = object_.voxelLodSize;
#endif
        float3 debugColor = DEBUG_COLORS[clamp(voxelLodSize, 0, 15)];
        output.base_color.xyz = debugColor;
    }

    output.metalness = triplanar.cm.w;
    output.normal = normalize(mul(triplanar.ng.xyz, pixel.custom.world_matrix));
    output.gloss = triplanar.ng.w;
    output.emissive = triplanar.ext.y;

    output.ao = triplanar.ext.x;

    float hardAmbient = 1 - pixel.custom.colorBrightnessFactor;
    output.base_color *= hardAmbient;
}

void InitilizeTriplanarInterface(PixelInterface pixel, out TriplanarInterface input)
{
    input.N = normalize(pixel.custom.normal);
    input.weights = saturate(GetTriplanarWeights(input.N));
    input.d = pixel.custom.distance;

    float3 pos_ddx = ddx(pixel.position_ws);
    float3 pos_ddy = ddy(pixel.position_ws);
    input.dpxperp = cross(input.N, pos_ddx);
    input.dpyperp = cross(pos_ddy, input.N);

    float2 texcoordsX = pixel.custom.texcoords.zy;
    float2 texcoordsY = pixel.custom.texcoords.xz;
    float2 texcoordsZ = pixel.custom.texcoords.xy;

    input.ddxTexcoords[0] = ddx(texcoordsX);
    input.ddyTexcoords[0] = ddy(texcoordsX);
    input.ddxTexcoords[1] = ddx(texcoordsY);
    input.ddyTexcoords[1] = ddy(texcoordsY);
    input.ddxTexcoords[2] = ddx(texcoordsZ);
    input.ddyTexcoords[2] = ddy(texcoordsZ);
    input.texcoords = pixel.custom.texcoords;
}

#endif
