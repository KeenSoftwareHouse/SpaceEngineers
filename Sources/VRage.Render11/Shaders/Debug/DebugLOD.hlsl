#include "Debug.hlsli"
#include <Template.hlsli>
#include <Math/Color.hlsli>


void __pixel_shader(PostprocessVertex vertex, out float4 output : SV_Target0)
 {
    SurfaceInterface input = read_gbuffer(vertex.position.xy);

    float3 color;

	if (input.LOD == 255) // the lod is used with new pipeline
	{
		output.xyz = input.base_color.xyz;
		//output.xyz = float3(1, 1, 1) / 2;
		output.w = 1;
		return;
	}

    // MyClipmap.cs colors
    switch (input.LOD)
    {
    case 0:
        color = float3(1, 0 , 0);
        break;
    case 1:
        color = float3(0, 1, 0);
        break;
    case 2:
        color = float3(0, 0, 1);
        break;
    case 3:
        color = float3(1, 1, 0);
        break;
    case 4:
        color = float3(0, 1, 1);
        break;
    case 5:
        color = float3(1, 0, 1);
        break;
    case 6:
        color = float3(0.5, 0, 1);
        break;
    case 7:
        color = float3(0.5, 1, 0);
        break;
    case 8:
        color = float3(1, 0, 0.5);
        break;
    case 9:
        color = float3(0, 1, 0.5);
        break;
    case 10:
        color = float3(1, 0.5, 0);
        break;
    case 11:
        color = float3(0, 0.5, 1);
        break;
    case 12:
        color = float3(0.5, 1, 1);
        break;
    case 13:
        color = float3(1, 0.5, 1);
        break;
    case 14:
        color = float3(1, 1, 0.5);
        break;
    case 15:
        color = float3(0.5, 0.5, 1);
        break;
    default:
        color = float3(1, 1, 1);
        break;
    }

    /*
    static const float PI = 3.14159265f;

    const int DIVIDER = 8;
    float step = 2 * PI * DIVIDER;
    float offset = input.LOD * step;
    float h = cos(offset);
    float s = sin(offset);
    float l = 0.5;

    float3 color = HSLToRGB(float3(h, s, l));

    */

    /*
    
    int value = clamp(input.LOD, 0, DIVIDER);
    color.r = (float)value / (float)DIVIDER;
    value = clamp((int)input.LOD - DIVIDER, 0, DIVIDER);
    color.g = (float)value / (float)DIVIDER;
    value = clamp((int)input.LOD - DIVIDER * 2, 0, DIVIDER);
    color.b = (float)value / (float)DIVIDER;
    */
    output = float4(color, 1);
}
