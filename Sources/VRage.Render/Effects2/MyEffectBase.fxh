//	This common effects header file is common for ALL effects we use. Every effect includes it.

// Define optimized vertex format. See MyVertexFormats.cs. Same definition must be also in engine preprocesor.
#define PACKED_VERTEX_FORMAT
#define SKINNED_EFFECT_MAX_BONES   60

// Define optimized vertex format. See MyVertexFormats.cs. Same definition must be also in engine preprocesor.
//#define COLOR_SHADOW_MAP_FORMAT

//These values are set from the game
const float NEAR_PLANE_DISTANCE;
const float FAR_PLANE_DISTANCE;

const float3 DIRECTION[6] = { float3(0,0,-1), float3(0,0,1), float3(-1,0,0), float3(1,0,0), float3(0,1,0), float3(0,-1,0) };

//	These are three types of render quality. It's like an enum, but we can't use enums in HLSL so I used numeric constants.
//	"static" hides the variable from the Effect system - without the static keyword it will show up in your Effect Parameters
static const int RENDER_QUALITY_LOW = 0;
static const int RENDER_QUALITY_NORMAL = 1;
static const int RENDER_QUALITY_HIGH = 2;
static const int RENDER_QUALITY_EXTREME = 3;

//Maximum value of specular intensity which can be used (otherwise is truncated)
static const float SPECULAR_INTENSITY_RATIO = 8.0f;

//Maximum value of specular power which can be used (otherwise is truncated)
static const float SPECULAR_POWER_RATIO = 32.0f;

float FogDistanceNear = 1000;
float FogDistanceFar = 2500;
float FogMultiplier = 0.8f;
float FogBacklightMultiplier = 1.0f;

float3 LightDirection = {1.0f, 1.0f, 1.0f};
float3 FogColor = float3(1,0,0);
float4 LightColorAndIntensity;
float4 BacklightColorAndIntensity = float4(1,0,0,1);
float4 AmbientMinimumAndIntensity = float4(0.1f, 0.1f, 0.1f, 0.75f);
float TextureEnvironmentBlendFactor;
float3 LightSpecularColor;

TextureCube TextureAmbientMain;
sampler TextureAmbientMainSampler = sampler_state
{
	texture = <TextureAmbientMain>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = NONE;
	AddressU = CLAMP; 
	AddressV = CLAMP;
};

TextureCube TextureAmbientAux;
sampler TextureAmbientAuxSampler = sampler_state
{
	texture = <TextureAmbientAux>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = NONE;
	AddressU = CLAMP; 
	AddressV = CLAMP;
};

//	Pixel shader output when rendering into MRTs
struct MyGbufferPixelShaderOutput
{
    float4 NormalAndSpecPower : COLOR0;
    float4 DiffuseAndSpecIntensity : COLOR1;
    float4 DepthAndEmissivity : COLOR2;	
};

//	Gets the screen-space texel coordinate from clip-space position (or just from 4D position)
//	Once you've transformed your position by a projection matrix, you need to perform homogeneous 
//	division by w...in other words divide X and Y by W. This gives you X and Y such that X = -1 is the 
//	left side of the screen, X = 1 is the right, Y = -1 is the bottom, and Y = 1 is the top. 
//	So you just need to so a little bit more conversion to get a texture coordinate (and also account 
//	for the half-pixel offset if you're using D3D9):
float2 GetScreenSpaceTextureCoord(float4 position, float2 halfPixel)
{
    float2 texCoord = position.xy / position.w;

	#if 0
    texCoord = texCoord * 0.5f + 0.5f;    
    texCoord.y = 1.0f - texCoord.y; 
	#else
	texCoord = texCoord * float2(0.5f,-0.5f) + 0.5f;  	
	#endif

	//	Fixing half-pixel position    
    texCoord += halfPixel;
    
    return texCoord;
}

//	Convert normal vector so it can be saved in texture or render target. Basically rescales from
//	interval [-1 ... +1] to [0 ... +1]
float3 GetNormalVectorIntoRenderTarget(float3 normal)
{
	return 0.5f * (normal + 1.0f);
}

//	Tranform normal back into [-1,1] range
float3 GetNormalVectorFromRenderTarget(float3 normal)
{
	 return 2.0f * normal - 1.0f;
}

//	Tranform normal back into [-1,1] range from swizzled DDS
float3 GetNormalVectorFromDDS(float4 normal)
{
	// ! don't need to swizzle, now we have non-swizzled rgba normalmaps!
	//normal = normal.wyzx;

	return 2.0f * normal.xyz - 1.0f;
}

//	Tranform normal back into [-1,1] range, but leaves alpha unchanged because it probably contains something not related to normals (e.g. alpha or emisivity)
float4 GetNormalVectorFromRenderTarget(float4 normal)
{
	 return float4(2.0f * normal.xyz - 1.0f, normal.w);
}

 //tranform normal back into [-1,1] range
float3 GetNormalVectorFromRenderTargetNormalized(float3 normal)
{
#if 0
	 normal = GetNormalVectorFromRenderTarget(normal);
	 return length(normal.xyz) > 0.01f ? normalize(normal.xyz) : 0;
#else

	float3	n = GetNormalVectorFromRenderTarget(normal);

	normalize(n);

	return n;
#endif
}

//Determines, if this normalized depth is background depth or not
bool IsDepthBackground(float depth)
{
	return depth >= 0.99999f;
}

inline float4 EncodeFloatRGBA(float v)
{
	if (v > 0.9999)
	{
		v = 0.9999;
	}
	//float4 enc = float4(1.0, 255.0, 65025.0, 160581375.0) * v;
	float4 enc = float4(1.0, 255.0, 65025.0, 0.0) * v;
	enc = frac(enc);
	enc -= enc.yzww * float4(1.0 / 255.0, 1.0 / 255.0, 1.0 / 255.0, 0.0);

	return enc;
}

inline float4 EncodeFloatRGBAPrecise( float v ) 
{
  float4 enc = float4(1.0, 255.0, 65025.0, 160581375.0) * v;
  enc = frac(enc);
  enc -= enc.yzww * float4(1.0/255.0,1.0/255.0,1.0/255.0,0.0);
  
  return enc;
}

inline float DecodeFloatRGBA( float4 rgba ) 
{
#if 0
	rgba.a = 0; //we store specular here
	return dot( rgba, float4(1.0, 1/255.0, 1/65025.0, 1/160581375.0) );
#else
	return dot( rgba, float4(1.0, 1/255.0, 1/65025.0, 0) );
#endif
}

inline float ConvertRGBToGray(float3 rgb)
{
#if 0
	return 0.299f * rgb.x + 0.587f * rgb.y + 0.114f * rgb.z;
#else
	return dot(float3(0.299f,0.587f,0.114f),rgb);
#endif
}

float4x4 CreateWorld(float3 pos, float3 forward, float3 up)
{
    float3 right = cross(up, -forward);
    
	float4x4 result = { float4(right, 0), float4(up, 0), float4(-forward, 0), float4(pos, 1) };
	return result;
}

float4x4 CreateWorldFromTranslationAndBasicRotation(float4 translationAndRotation)
{
	int val = translationAndRotation.w;

	float3 forward = DIRECTION[val / 6];
	float3 up = DIRECTION[val - (val / 6) * 6];

	return CreateWorld(translationAndRotation.xyz, forward, up);
}

float3 GetViewPositionFromDepth(float depth, float3 viewDir)
{
	float fSceneDepth = depth * FAR_PLANE_DISTANCE;
	return viewDir.xyz / -viewDir.z * fSceneDepth;
} 

float GetViewDistanceFromDepth(float depth, float3 viewDir)
{
	float3 vPositionVS = GetViewPositionFromDepth(depth, viewDir);
	return length(vPositionVS);
} 

//	Packs all data required for our gbuffer deferred shader, that outputs them into MRT (multiple render targets)
MyGbufferPixelShaderOutput GetGbufferPixelShaderOutput(float3 normal, float3 diffuse, float viewDistance)
{
	//	Output into MRT
	MyGbufferPixelShaderOutput output;
	output.NormalAndSpecPower.rgb = GetNormalVectorIntoRenderTarget(normal);
	output.NormalAndSpecPower.a = 1;	
	output.DiffuseAndSpecIntensity.rgb = diffuse;
	output.DiffuseAndSpecIntensity.a = 1;
	output.DepthAndEmissivity = EncodeFloatRGBA(viewDistance / FAR_PLANE_DISTANCE);		
	output.DepthAndEmissivity.a = 0.0f; //zero emissivity
    return output;
}

float Pack4(float lower, float upper)
{
	int l = (int)round(saturate(lower) * 15);		// convert lower to 0 ~ 15
	int u = (int)round(saturate(upper) * 15) * 16;   // convert upper to 0,16,32,48,64...etc.
	return (l + u) / 255.0f;
}

float Unpack4Lower(float packedValue)
{
	int value = ((int)(packedValue * 255)) % 16;// get lower 4 bytes to range 0 ~ 15
	return value / 15.0f;						// scale to 0.0 ~ 1.0
}

float Unpack4Upper(float packedValue)
{
	int value = ((int)(packedValue * 255)) / 16;// get upper 4 bytes to range 0 ~ 15
	return value / 15.0f;						// scale to 0.0 ~ 1.0
}

float Pack7to1(float lower, float upper)
{
	float l = round(saturate(lower) * 127) * 2; // convert lower to odd
	float u = round(saturate(upper));			// convert upper to 0 or 1
	return (l + u) * (1.0f / 255.0f);
}

float Unpack7to1Lower(float packedValue)
{
	return packedValue;
}

float Unpack7to1Upper(float packedValue)
{
	return (packedValue * 255) % 2;
}

void Unpack7to1(float packedValue, out float upper, out float lower)
{
	lower = packedValue;
	upper = (packedValue * 255) % 2;
}

float PackGBufferEmissivityReflection(float emissivity, float reflection)
{
	//return emissivity;
	return Pack7to1(emissivity, reflection);
}

void UnpackGBufferEmissivityReflection(float packedValue, out float emissivity, out float reflection)
{
	//emissivity = packedValue;
	//reflection = 0;
	//return;
	Unpack7to1(packedValue, reflection, emissivity);
}

float UnpackGBufferEmissivity(float packedValue)
{
	return Unpack7to1Lower(packedValue);
}

float UnpackGBufferReflection(float packedValue)
{
	return Unpack7to1Upper(packedValue);
}

//	Packs all data required for our gbuffer deferred shader, that outputs them into MRT (multiple render targets)
// This version does not write in depth buffer (alpha blending must be enabled)
// Normal has also alpha channel (blends between surface normal and decal normal)
MyGbufferPixelShaderOutput GetGbufferPixelShaderOutputBlended(float4 normal, float4 diffuse, float emissivity, float reflection)
{
	//	Output into MRT
	MyGbufferPixelShaderOutput output;
	output.NormalAndSpecPower.rgb = GetNormalVectorIntoRenderTarget(normal.xyz);
	output.NormalAndSpecPower.a = normal.w;
	output.DiffuseAndSpecIntensity = diffuse;
	output.DepthAndEmissivity = float4(0,0,0,PackGBufferEmissivityReflection(emissivity, reflection));
    return output;
}

//	Packs all data required for our gbuffer deferred shader, that outputs them into MRT (multiple render targets)
MyGbufferPixelShaderOutput GetGbufferPixelShaderOutput(float3 normal, float3 diffuse, float specularIntensity, float specularPower, float viewDistance)
{
	//	Output into MRT
	MyGbufferPixelShaderOutput output;
	output.NormalAndSpecPower.rgb = GetNormalVectorIntoRenderTarget(normal);
	output.NormalAndSpecPower.a = specularPower;	
	output.DiffuseAndSpecIntensity.rgb = diffuse;
	output.DiffuseAndSpecIntensity.a = specularIntensity;
	output.DepthAndEmissivity = EncodeFloatRGBA(viewDistance / FAR_PLANE_DISTANCE);		
	output.DepthAndEmissivity.a = PackGBufferEmissivityReflection(0.0f, 0.0f); //zero emissivity, zero reflection
    return output;
}

float4 Encode1010102( float4 color )
{
	float4 result;
	result.rgb = 0.25f * color.rgb;
	result.a = color.a;
	return result;
}

float4 Decode1010102( float4 color )
{
	float4 result;
	result.rgb = 4.0f * color.rgb;
	result.a = color.a;
	return result;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// Constants for vertex compression. Values must be same as in engine. See MyConstants.MyVertexCompression.
static const int COMPRESSION_VOXEL_OFFSET = 32767;                           // Offset to add to coordinates when mapping voxel from float<0, 8191> to short<-32767, 32767>.
static const int COMPRESSION_VOXEL_MULTIPLIER = 8;                           // Multiplier for mapping voxel from float to short.
static const int COMPRESSION_AMBIENT_MULTIPLIER = 32767;                     // Multiplier for mapping value from float<-1, 1> to short<-32767, 32767>.

float4 UnpackNormal(float4 packedNormal)
{
#ifdef PACKED_VERTEX_FORMAT

	// get sign of Z from last bit of Y
	float sign = packedNormal.y > 127.5f ? 1.0f : -1.0f;

	// clear last bit of Y
	if(sign > 0)
		packedNormal.y -= 128.0f;

	// construct X and Y into format <0, 32767>
	float x = packedNormal.x + (256.0f * packedNormal.y);
	float y = packedNormal.z + (256.0f * packedNormal.w);

	// normalize X and Y to <0,1>
	x /= 32767.0f;
	y /= 32767.0f;

	// transform X and Y to <-1, 1>
	float nx = (2 * x) - 1.0f;
	float ny = (2 * y) - 1.0f;

	// compute Z
	float nz = sign * sqrt(max(0, 1 - nx * nx - ny * ny));

	// output vector is already normalized
	return float4(nx, ny, nz, 1);
#else
	return packedNormal.xyzw;
#endif
}

float4 UnpackTangent(float4 packedTangent)
{
#ifdef PACKED_VERTEX_FORMAT

	// get sign of Z from last bit of Y
	float z_sign = packedTangent.y > 127.5f ? 1.0f : -1.0f;
	float sign = packedTangent.w > 127.5f ? 1 : -1;

	// clear last bit of Y
	if(z_sign > 0)
		packedTangent.y -= 128.0f;
	if(sign)
		packedTangent.w -= 128.0f;

	// construct X and Y into format <0, 32767>
	float x = packedTangent.x + (256.0f * packedTangent.y);
	float y = packedTangent.z + (256.0f * packedTangent.w);

	// normalize X and Y to <0,1>
	x /= 32767.0f;
	y /= 32767.0f;

	// transform X and Y to <-1, 1>
	float nx = (2 * x) - 1.0f;
	float ny = (2 * y) - 1.0f;

	// compute Z
	float nz = z_sign * sqrt(max(0, 1 - nx * nx - ny * ny));

	// output vector is already normalized
	return float4(nx, ny, nz, sign);
#else
	return packedTangent.xyzw;
#endif
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

float3 UnpackVoxelPosition(float4 PositionAndAmbient)
{
#ifdef PACKED_VERTEX_FORMAT
	return float3((PositionAndAmbient.xyz + COMPRESSION_VOXEL_OFFSET) / COMPRESSION_VOXEL_MULTIPLIER);
#else
	return PositionAndAmbient.xyz;
#endif
}

float UnpackVoxelAmbient(float4 PositionAndAmbient)
{
	return (PositionAndAmbient.w % 8192) / 8191.0f;
	//return frac(PositionAndAmbient.w / 8192.0f); // This is faster? But it does not work (can't handle negative values?)
}

float3 UnpackVoxelAlpha(float4 PositionAndAmbient)
{
	int index = (int)abs(PositionAndAmbient.w / 8192.0f);
	return float3(step(0, -index), step(abs(index - 1), 0), step(2, index));
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

float4 UnpackPositionAndScale(float4 Position)
{
#ifdef PACKED_VERTEX_FORMAT
	return float4(Position.xyz * Position.w, 1);
#else
	return Position;
#endif
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

float3 UnpackMaterialAlpha(float3 MaterialAlpha)
{
#ifdef PACKED_VERTEX_FORMAT
	return MaterialAlpha / 255;
#else
	return MaterialAlpha.xyz;
#endif
}

float4 CalculateFogLinear(float distance)
{
	float fogBlend = saturate((distance - FogDistanceNear) / (FogDistanceFar - FogDistanceNear));

	return float4(FogColor, fogBlend * FogMultiplier);

	// This is no hack, it's just interpolator based on background blend and fog blend, it's pretty complicated, but right
	//float i = (backgroundBlend * (fogBlend + backgroundBlend - 2) + 1) * FogMultiplier;
	//return lerp(sourceColor.xyz, FogColor, i);
	//return float4(FogColor, i);
}

float3 CalculateFogExponencial(float3 sourceColor, float distance, float density)
{
	float fogBlend = 1.0f/exp(abs(distance / FogDistanceFar) * density);
	float3 diffuse = lerp(sourceColor.xyz, FogColor, (1 - saturate(fogBlend)) * FogMultiplier);
	return diffuse;
}

static const float HueMid = 320.0/360.0;
static const float HueStep = 5.0 / 360.0;


uint ColorMaskEnabled;
float3 ColorMaskHSV; // Hue is absolute, saturation is offset, value is offset\

float3 HSVtoRGB(in float3 hsv)
{
    float4 K = float4(1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f);
    float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0f - K.www);
    return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);

	// Older implementation
	//float R = abs(hsv.x * 6 - 3) - 1;
    //float G = 2 - abs(hsv.x * 6 - 2);
    //float B = 2 - abs(hsv.x * 6 - 4);
    //return ((saturate(float3(R,G,B)) - 1) * hsv.y + 1) * hsv.z;
}

float3 RGBtoHSV(in float3 c)
{
	float4 K = float4(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);

	// Older implementation
	//float3 RGB = c;
	//float3 HSV = 0;
	//
	//HSV.z = max(RGB.r, max(RGB.g, RGB.b));
	//float M = min(RGB.r, min(RGB.g, RGB.b));
	//float C = HSV.z - M;
	//
	//float D = step(0, -C);
	//HSV.y = C / (HSV.z + D);
	//float3 Delta = (HSV.z - RGB) / (C + D);
	//Delta -= Delta.brg;
	//Delta += float3(2,4,6);
	//Delta *= step(HSV.zzz, RGB.gbr);
	//HSV.x = max(Delta.r, max(Delta.g, Delta.b));
	//HSV.x = frac(HSV.x / 6);
	//return HSV;
}
float4 ColorizeTexture(float4 diffuseTexture,float3 inputColor)
{
		float3 hsv = RGBtoHSV(diffuseTexture.rgb);
		hsv.x = 0;
		float3 finalHsv = hsv + inputColor;// - float3(HueMid, 0, 0);
		finalHsv.x = frac(finalHsv.x);
		
		// Gray colors won't be colorized (blend 0.3 - 0.8), other interesting values are (0.15 - 0.3)
		//float gray = 1 - saturate((hsv.y - 0.3f) / 0.5f);
		//finalHsv.yz = lerp(saturate(finalHsv.yz), hsv.yz, gray);
		
		// White, gray and black will have colorized all colors including gray (blend 0.7 - 0.9), other interesting values are (1.1 - 1.3)
		float gray2 = 1 - saturate((inputColor.y + 1.0f) / 0.1f);
		finalHsv.yz = lerp(saturate(finalHsv.yz), saturate(hsv.yz + inputColor.yz), gray2);

		// Desaturate color for white/gray/black paint
		float gray3 = 1 - saturate((inputColor.y + 0.9f) / 0.1f);
		finalHsv.y = lerp(saturate(finalHsv.y), saturate(hsv.y + inputColor.y), gray3);

		const float halfBlendThreshold = 25.0f / 2 / 255.0f;
		const float colorizeValue = 0.5f - halfBlendThreshold; // Alpha = 115
		const float preserveValue = 0.5f + halfBlendThreshold; // Alpha = 140
		const float roundingErrorThreshold = 2 / 255.0f;

		float coloring = 1 - saturate((diffuseTexture.a - colorizeValue) / (halfBlendThreshold * 2));
		diffuseTexture.rgb = lerp(diffuseTexture.rgb, HSVtoRGB(finalHsv), coloring);

		// Emissivity is down from 115 or up from 140
		diffuseTexture.a = 1 - (saturate(abs(diffuseTexture.a - 0.5f) - halfBlendThreshold - roundingErrorThreshold) / (0.5f - halfBlendThreshold - roundingErrorThreshold));
		return diffuseTexture;
}

float4 SampleAmbientTexture(float3 ambientTexCoord)
{
	float4 mainColor = texCUBE(TextureAmbientMainSampler, ambientTexCoord);
	float4 auxColor = texCUBE(TextureAmbientAuxSampler, ambientTexCoord);
	return lerp(mainColor, auxColor, TextureEnvironmentBlendFactor);
}