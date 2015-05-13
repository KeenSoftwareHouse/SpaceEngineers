struct VertexStageIn
{
    float4 packed0 : TEXCOORD0;
    float4 packed1 : TEXCOORD1;
};

struct VertexStageOut
{
    float3 position : POSITION;
    float3 normal   : NORMAL;
    uint seed       : TEXCOORD;
};

struct PixelStage
{
    float4 position : SV_Position;
    float3 normal   : NORMAL;
    float2 texcoord : TEXCOORD0;    
};

#include <template.h>
#include <math.h>
#include <random.h>
#include <common.h>

// potentially arrays (?)
// do we need metal for foliage?
Texture2D ColorMetalTexture : register( t0 );
Texture2D NormalGlossTexture : register( t1 );
Texture2D AlphaMaskTexture : register( t2 );

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

void vs(VertexStageIn input, out VertexStageOut output)
{
    output.position = mul(float4(input.packed0.xyz, 1), object_.world_matrix).xyz;
    output.normal = normalize(unpack_normal(input.packed1.xy));
    output.seed = f32tof16(input.packed0.w);
}

void spawn_billboard(float3 position, float3x3 onb, float2 scale, inout TriangleStream<PixelStage> triangle_stream )
{
    PixelStage vertex;
    float3 tanx = onb[0];
    float3 tany = onb[1];
    float3 N = onb[2];

    vertex.normal = tany;

    vertex.position = world_to_clip(position - tanx * scale.x);
    vertex.texcoord = float2(0,1);
    triangle_stream.Append(vertex);

    vertex.position = world_to_clip(position + tanx * scale.x);
    vertex.texcoord = float2(1,1);
    triangle_stream.Append(vertex);

    vertex.position = world_to_clip(position - tanx * scale.x + N * scale.y);
    vertex.texcoord = float2(0,0);
    triangle_stream.Append(vertex);

    vertex.position = world_to_clip(position + tanx * scale.x + N * scale.y);
    vertex.texcoord = float2(1,0);
    triangle_stream.Append(vertex);

    triangle_stream.RestartStrip();
}

float cosine_like_cheap(float xi)
{
    return sqrt(1-xi);
}

[maxvertexcount(8)] 
void gs( point VertexStageOut input[1], inout TriangleStream<PixelStage> triangle_stream )
{
    // constants?
    float2 scale = float2(0.5,0.5);   

    // variables
    uint seed = input[0].seed;
    float3 position = input[0].position;
    float3 N = input[0].normal;
    PixelStage vertex;

    float3x3 onb = create_onb(N);
    // TODO: random rotation
    float3 tanx = onb[0];
    float3 tany = onb[1];

    float r = random(seed);
    float r2 = random2(seed);
    scale *= 1.25 - r2 * 0.5;

    float c = cosine_like_cheap(r);
    float s = sqrt(1-c*c);
    onb = mul( rotate_z(s, c) , onb);

    // case X shape
    {
        spawn_billboard(position, mul(rotate_x(M_PI / 8), onb), scale, triangle_stream);
        onb = mul(rotate_z(M_PI_2), onb);
        spawn_billboard(position, onb = mul(rotate_x(M_PI / 8), onb), scale, triangle_stream);
    }
}

#include <gbuffer_write.h>
//[earlydepthstencil]
void ps(PixelStage input, out GbufferOutput output, bool front_face : SV_IsFrontFace)
{
    float a = AlphaMaskTexture.Sample(TextureSampler, input.texcoord).x;
    if(a < 0.5)
        discard;
    float4 CM = ColorMetalTexture.Sample(TextureSampler, input.texcoord);
    float4 NG = NormalGlossTexture.Sample(TextureSampler, input.texcoord);

    gbuffer_write(output, CM.xyz, 0, NG.w, input.normal, 1);
}