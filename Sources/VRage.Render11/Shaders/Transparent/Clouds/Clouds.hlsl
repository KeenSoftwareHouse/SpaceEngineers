// @define NUMTHREADS 8

#ifndef NUMTHREADS
#define NUMTHREADS 1
#endif

#ifndef NUMTHREADS_X
#define NUMTHREADS_X NUMTHREADS
#endif

#ifndef NUMTHREADS_Y
#define NUMTHREADS_Y NUMTHREADS_X
#endif

#include <Frame.hlsli>

#include <VertexTransformations.hlsli>

cbuffer CloudsConstants : register(b1) {
	matrix World;
	matrix ViewProj;
	float4 Color;
};

Texture2D<float4> ColorMetalTexture : register(t0);
Texture2D<float> AlphamaskTexture : register(t1);
Texture2D<float4> NormalGlossTexture : register(t2);
//Texture2D<float4> AmbientOcclusionTexture : register(t7);

SamplerState Sampler : register(s0);

struct VsInputVertex
{
	float4 positionLocal : POSITION0;
	uint2 normal : NORMAL0;
	float4 tangent4 : TANGENT0;
	float2 uv : TEXCOORD0;
};

struct PsInput
{
	float4 positionScreen : SV_Position;
	float3 normal : NORMAL0;
	float2 uv : TEXCOORD0;
	float4 positionWorld : POSITION0;
};

void __vertex_shader(VsInputVertex inputVertex, out PsInput output)
{
	output.positionWorld = mul(unpack_position_and_scale(inputVertex.positionLocal), World);
	output.positionScreen = mul(output.positionWorld, ViewProj);
	output.normal = normalize(mul(unpack_normal(inputVertex.normal), (float3x3)World));
	
//	float4 tangent = unpack_tangent_sign(inputVertex.tangent4);
	output.uv = inputVertex.uv;
}

void __pixel_shader(PsInput input, out float4 output : SV_Target0)
{
	float alphaSample = AlphamaskTexture.Sample(Sampler, input.uv) * Color.w;


	float3 cameraForward = float3(frame_.Environment.view_matrix._13, frame_.Environment.view_matrix._23, frame_.Environment.view_matrix._33);
	float3 centerPosition = float3(World._41, World._42, World._43);
	float layerScale = length(float3(World._13, World._23, World._33));

	float3 cameraPosition = float3(frame_.Environment.view_matrix._41, frame_.Environment.view_matrix._42, frame_.Environment.view_matrix._43);
	float distance = length(input.positionWorld.xyz - cameraPosition);
	alphaSample *= pow(min(distance / (layerScale / 12), 1.0), 3.0);

	float3 colorSample = ColorMetalTexture.Sample(Sampler, input.uv).xyz * Color.xyz;

	float3 adjustedNormal = normalize(input.normal - 0.15*cameraForward);
	float cameraNormalDot = dot(cameraForward, adjustedNormal);
	float edgeFactor = clamp(abs(cameraNormalDot), 0.075, 1);

	float cameraFromCenter = length(centerPosition - cameraPosition);
	if ( cameraFromCenter < layerScale )	// Inside cloud layer
		edgeFactor = lerp(edgeFactor, 1, 5 * (layerScale / cameraFromCenter - 1));

	// Extremely simple shading
	//float3 normalSample = normalize(mul(NormalGlossTexture.Sample(PointSampler, input.uv).xyz, (float3x3)World));
	float shadingMultiplier = clamp(pow(abs((1 - dot(input.normal, frame_.Light.directionalLightVec)) / 2), 2.0), 0.025, 1);

	output = float4(colorSample, alphaSample) * shadingMultiplier * edgeFactor;
}

#include <GBuffer/GBuffer.hlsli>
#include <Lighting/LightingModel.hlsli>

cbuffer FogConstants : register(b1) {
	float LayerAltitude;
	float CameraAltitude;
	float LayerThickness;
	uint fogTexelX;
	uint fogTexelY;
	float3 _padding;
};

#ifndef MS_SAMPLE_COUNT
Texture2D<float4> Input : register(t1);
#else
Texture2DMS<float4, MS_SAMPLE_COUNT> Input : register(t1);
#endif
Texture2D<float> LayerAlphaTexture : register(t2);

RWTexture2D<float4> Output : register(u0);

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void __compute_shader(
	uint3 dispatchThreadID : SV_DispatchThreadID,
	uint3 groupThreadID : SV_GroupThreadID,
	uint3 GroupID : SV_GroupID,
	uint ThreadIndex : SV_GroupIndex)
{
	uint2 texel = dispatchThreadID.xy;

#ifndef MS_SAMPLE_COUNT
	float depthSample = DepthBuffer[texel];
#else
	float depthSample = DepthBuffer.Load(texel, 0);
#endif
//	float layerAlphaSample = pow(abs(LayerAlphaTexture[uint2(fogTexelX, fogTexelY)]), 1.5);
	float highestFogDensity = 0.03;//*layerAlphaSample;
	float fogDensity = lerp(0, highestFogDensity, 1 - saturate(abs(CameraAltitude - LayerAltitude) / (LayerThickness / 2)));
	float fogMultiplier = uniform_fog(max(1 - depthSample*500, 0), fogDensity);

	float4 fogColor = D3DX_R8G8B8A8_UNORM_to_FLOAT4(frame_.Fog.color);

#ifndef MS_SAMPLE_COUNT
	float4 sample = Input[texel];
#else
	float4 sample = Input.Load(texel, 0);
#endif
	Output[texel] = lerp(sample, fogColor, fogMultiplier);
}