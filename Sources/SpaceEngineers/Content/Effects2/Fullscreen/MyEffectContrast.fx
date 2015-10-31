#include "../MyEffectBase.fxh"

// HDTV conversion weights
static const float3 GRAYSCALE_WEIGHTS = { 0.2126f, 0.7152f, 0.0722f };

float Contrast;
float Hue;
float Saturation;

Texture DiffuseTexture;
sampler DiffuseSampler = sampler_state 
{ 
    texture = <DiffuseTexture> ; 
    magfilter = POINT; 
    minfilter = POINT; 
    mipfilter = NONE; 
    AddressU = CLAMP; 
    AddressV = CLAMP;
};

float2 HalfPixel;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 TexCoordAndCornerIndex	: TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;	
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = input.Position;
    output.TexCoord = input.TexCoordAndCornerIndex.xy + HalfPixel;
    return output;
}

// Converts the rgb value to hsv, where H's range is -1 to 5
float3 rgb_to_hsv(float3 RGB)
{
    float r = RGB.x;
    float g = RGB.y;
    float b = RGB.z;

    float minChannel = min(r, min(g, b));
    float maxChannel = max(r, max(g, b));

    float h = 0;
    float s = 0;
    float v = maxChannel;

    float delta = maxChannel - minChannel;

    if (delta != 0)
    {
        s = delta / v;

        if (r == v) h = (g - b) / delta;
        else if (g == v) h = 2 + (b - r) / delta;
        else if (b == v) h = 4 + (r - g) / delta;
    }

    return float3(h, s, v);
}

float3 hsv_to_rgb(float3 HSV)
{
    float3 RGB = HSV.z;

    float h = HSV.x;
    float s = HSV.y;
    float v = HSV.z;

    float i = floor(h);
    float f = h - i;

    float p = (1.0 - s);
    float q = (1.0 - s * f);
    float t = (1.0 - s * (1 - f));

    if (i == 0) { RGB = float3(1, t, p); }
    else if (i == 1) { RGB = float3(q, 1, p); }
    else if (i == 2) { RGB = float3(p, 1, t); }
    else if (i == 3) { RGB = float3(p, q, 1); }
    else if (i == 4) { RGB = float3(t, p, 1); }
    else /* i == -1 */ { RGB = float3(1, p, q); }

    RGB *= v;

    return RGB;
}

float4 PixelShaderFunction(VertexShaderOutput input, float2 screenPosition : VPOS) : COLOR0
{
    float4 color = tex2D(DiffuseSampler, input.TexCoord);

    float3 hsv = rgb_to_hsv(color.rgb);
    hsv.x += Hue;
    hsv.x = fmod(hsv.x+7.0f, 6.0f)-1.0f; // Get resulting value back to range <-1, 6>
    color.rgb = hsv_to_rgb(hsv);

    float grayscale = dot(color.rgb, GRAYSCALE_WEIGHTS);
    color.rgb = (color.rgb-grayscale) * Saturation + grayscale;
    color.rgb = (color.rgb-0.5f) * Contrast + 0.5f;
    //color.rgb = pow(color.rgb, Contrast);
    return color;
}

technique Default
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
