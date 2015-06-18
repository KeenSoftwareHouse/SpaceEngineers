
Texture DepthsRT;
sampler DepthsRTSampler = sampler_state
{
	texture = <DepthsRT>;
	magfilter = POINT;
	minfilter = POINT;
	mipfilter = NONE;
	AddressU = clamp;
	AddressV = clamp;
};


float2 HalfPixel;
float2 Scale;

struct VertexShaderOutputWorldPosition
{
	float4 Position : POSITION0;
	float4 ScreenPosition : TEXCOORD0;
	float4 WorldPosition : TEXCOORD1;
	float4 ViewPosition : TEXCOORD2;
};

struct CalculatedWorldValues
{
	float3 Position;
	float3 ViewPosition;
	float3 ViewDir;
	float2 TexCoord;
};

//calc, not load
void LoadWorldValues(VertexShaderOutputWorldPosition input, out CalculatedWorldValues values)
{
	values.TexCoord = GetScreenSpaceTextureCoord(input.ScreenPosition, HalfPixel) * Scale;

	float4 encodedDepth = tex2D(DepthsRTSampler, values.TexCoord);
	float depthNormalized = DecodeFloatRGBA(encodedDepth);

	//VS
	values.ViewDir = input.ViewPosition.xyz / input.ViewPosition.w;
	values.ViewPosition = GetViewPositionFromDepth(depthNormalized, values.ViewDir);

	float4 wPosition = mul(float4(values.ViewPosition, 1), InvViewMatrix);
	wPosition.xyz = wPosition.xyz / wPosition.w;
	values.Position = wPosition.xyz;


}