// @defineMandatory NUMTHREADS 8
// @define _FINAL

#include "Defines.hlsli"

[numthreads(1, 1, 1)]
void __compute_shader()
{
    float exposure = CalculateExposure(frame_.Post.ConstantLuminance, frame_.Post.LuminanceExposure);
    ReduceOutput[uint2(0,0)] = float2(frame_.Post.ConstantLuminance, exposure);
}
