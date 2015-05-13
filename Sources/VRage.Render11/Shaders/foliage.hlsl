struct VertexStageIn
{
    float4 packed0 : TEXCOORD0;
    float4 packed1 : TEXCOORD1;
};

struct VertexStageOut
{
    float3 position : POSITION;
    float3 normal   : NORMAL;
    float3 color    : COLOR;
    uint seed       : TEXCOORD;
};

struct PixelStage
{
    float4 position : SV_Position;
    float3 normal   : NORMAL;    
    float3 color    : COLOR;
};

#include <template.h>
#include <math.h>
#include <random.h>

float3 unpack_normal(float2 packed)
{
    float2 xy = packed * 255;
    float zsign = xy.y > 127;
    xy.y -= zsign * 128;    
    xy /= float2(255, 127);
    xy = xy * 2 - 1;
    float z = sqrt(1-dot(xy, xy)) * (zsign ? 1 : -1);
    return float3(xy, z).xzy;
}

static const float3 index_to_color[] = {
    {1,0,0},
    {0,1,0},
    {0,0,1},
    {1,1,0},
    {0,1,1},
    {1,0,1},
};

void vs(VertexStageIn input, out VertexStageOut output)
{
    output.position = mul(float4(input.packed0.xyz, 1), object_.world_matrix).xyz;
    output.normal = normalize(unpack_normal(input.packed1.xy));
    output.seed = f32tof16(input.packed0.w);
    uint seed = output.seed;
    output.color = float3(0.6, 0.9, 0.2) - float3(random2(seed) * 0.2f, random2(seed) * 0.3f, 0);
}

[maxvertexcount(7)] // (L-1) * 2 + 1
void gs( point VertexStageOut input[1], inout TriangleStream<PixelStage> triangle_stream )
{
    static const int L = 4;

    static const float blade_thickness = 0.025f;
    static const float blade_len = 1.f;
    static const float wind_force = 0.125f;
    uint seed = input[0].seed;

    float4 wind_t = float4(frame_.time.xxxx) * wind_force;
    float4 wave = smooth_triangle_wave(wind_t);

    // close enough 
    float cosa = random2(seed);
    float sina = sqrt(1 - cosa * cosa);

    float3x3 rotation_basis = { float3(cosa, -sina, 0), float3(sina , cosa, 0), float3(0,0,1) };

    PixelStage output;
    output.color = input[0].color;
    float3 base_position = input[0].position;
    float3 N = input[0].normal;

    float3 up = abs(N.y < 0.999) ? float3(0,1,0) : float3(1,0,0);
    float3 tanx = normalize(cross(up, N));
    float3 tany = cross(N, tanx);

    float rotate = random2(seed);

    float3x3 ONB = { tanx, tany, N };
    ONB = mul(rotation_basis, ONB);

    float3 p0 = float3(0,0,0);
    float rescale = 1.25 - random2(seed) * 0.5;
    float3 p1 = normalize(float3(wave.x * 2 - 1, 0, 1)) * blade_len * rescale; // in local space

    float3 m0 = normalize(p1 - p0);
    float3 m1 = normalize(m0 + float3(wave.x * 2 - 1, 0, 0));

    p0 = 0;
    p1 = normalize(float3(1,0,2)) * blade_len * rescale;
    m0 = float3(0,0,1);
    m1 = normalize(float3(1,0,1));

    p1 = mul(p1, ONB);
    m0 = mul(m0, ONB);
    m1 = mul(m1, ONB);

    float3 vr = mul(tany, ONB);
    float3 p_r = vr * blade_thickness;

    [unroll]
    for(int l=0; l< L; l++)
    {
        float t = l / (float)(L-1);

        float3 p_l = cubic_hermit(p0, p1, m0, m1, t);

        float3 N;
        N = normalize(cross(vr, cubic_hermit_tan(p0, p1, m0, m1, t)));
        output.normal = N;

        if(l<L-1)
        {
            float3 p = base_position + p_l + lerp(p_r, 0, t);
            output.position = mul(float4(p, 1), projection_.view_proj_matrix); 
            triangle_stream.Append(output);

            p = base_position + p_l + lerp(-p_r, 0, t);
            output.position = mul(float4(p, 1), projection_.view_proj_matrix); 
            triangle_stream.Append(output);
        }
        else
        {
            float3 p = base_position + cubic_hermit(0, p1, m0, m1, 1);
            output.position = mul(float4(p, 1), projection_.view_proj_matrix); 

            triangle_stream.Append(output);        
        }

    }
    
    triangle_stream.RestartStrip();
}

#include <gbuffer_write.h>
void ps(PixelStage input, out GbufferOutput output, bool front_face : SV_IsFrontFace)
{
    gbuffer_write(output, input.color, 0, 0.85, input.normal, 1);
}