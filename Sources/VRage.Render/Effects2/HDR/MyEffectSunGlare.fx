#include "../MyEffectBase.fxh"

sampler TextureSampler : register(s0);

float sunColorMultiplier;

float4 LDR_PS(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
	return color * tex2D(TextureSampler, texCoord);
}

technique LDR
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 LDR_PS();
    }
}
