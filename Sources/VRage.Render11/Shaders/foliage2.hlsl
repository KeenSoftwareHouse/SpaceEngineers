struct VertexStageIn
{
    //float4 packed0 : TEXCOORD0;
    float3 position : POSITION;
    float4 packed1 : TEXCOORD0;
};

struct VertexStageOut
{
    float4 position          : POSITION;
    float3 normal            : NORMAL;
    float4 instance_position : TEXCOORD0;
    uint seed_id             : TEXCOORD1;
};

struct PixelStage
{
    float4 position : SV_Position;
    float3 normal   : NORMAL;
	float3 tangent  : TANGENT;
    float3 texcoord : TEXCOORD0;    
};

#include <template.h>
#include <math.h>
#include <random.h>
#include <common.h>
#include <frame.h>

struct MaterialFoliage
{
    float2 scale; 
    float rescale_mult;   
    uint textures_num;
};

#define FOLIAGE_SLOT 4
cbuffer FoliageConstants : register( b4 ) 
{
    MaterialFoliage foliage_[256];
};

Texture2DArray FoliageArray : register ( t0 );
Texture2DArray FoliageNormalArray : register (t1);

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
    //output.position = mul(float4(input.packed0.xyz, 1), object_.world_matrix).xyz;
    output.position.xyz = mul(float4(input.position.xyz, 1), get_object_matrix()).xyz;
    output.position.w = 1;
    output.instance_position = float4(input.position, 1);
    output.normal = normalize(unpack_normal(input.packed1.xy));

    //output.seed = f32tof16(input.packed0.w);
    output.seed_id = dot((uint2)(input.packed1.zw * 255.f), uint2(256, 1));
}

float3 calculate_wind_offset(float3 position)
{
    const float3 wind_d = frame_.wind_vec;
    if(length(wind_d) == 0)
        return 0;

    float4 freq = float4(1.975, 0.973, 0.375, 0.193);
    float4 x = frame_.time * length(wind_d) + dot(normalize(wind_d), position);
    float4 waves = smooth_triangle_wave(freq * x);

    return normalize(wind_d) * dot(waves, 0.25);
}
#ifdef ROCK_FOLIAGE
float2 calculate_scale(float3 position, float2 scale)
{
	float4 freq = float4(1.975, 0.973, 0.375, 0.193);
	float4 x = float4(position * 1000, 0);
	float4 waves = smooth_triangle_wave(freq * x);

	return scale * float2(dot(waves, 0.25), dot(waves, 0.5));
}

void spawn_pebble(float3 position, float3 instance_position, float3x3 onb, float2 scale, float3 surface_normal, float index, float2 xi, float V_dist, inout TriangleStream<PixelStage> triangle_stream)
{
	PixelStage vertex;

	scale.y *= 0.1;
	scale *= 0.75;
	scale = calculate_scale(instance_position, scale);

	float big_stone = 1;


	if (frac(instance_position.x  * 10000) < 0.985)
	{
		scale *= 0.2f;
		scale = calculate_scale(instance_position, scale);
		scale = clamp(scale, 0.01, 1);
		scale.y = 0.25 * scale.x;
		big_stone = 0;
	}

	if (!big_stone && V_dist > 7.5)
	{
		return;
	}

	float3 tanx = onb[0] * scale.x;
	float3 tany = onb[1] * scale.x;
	float3 N = normalize(onb[2]);

	xi *= 0.5f;
	xi += 1.0f;

	float angle = 0;
	int segments = big_stone ? 4 : 2;
	float angle_delta = 2.0f * 3.14f / (segments * 2);
	
	float sin_angle = 0;
	float cos_angle = 0;
	
	float beta = atan(length(tanx + tany) / length(N * xi.x * scale.y * 5));
	float sin_beta = sin_fast(beta);
	float cos_beta = cos_fast(beta);
	
	float3 delta = 0;

	[unroll]
	for (int i = 0; i < segments; i++)
	{
		sin_angle = sin_fast(angle);
		cos_angle = cos_fast(angle);
	
		delta = tanx * cos_angle * xi.y + tany * sin_angle * xi.y;
		vertex.position = world_to_clip(position + delta);
		vertex.normal = normalize(delta) * cos_beta + N * sin_beta;
		vertex.tangent = normalize(-tanx * sin_angle + tany * cos_angle);
		vertex.texcoord = float3(cos_angle * 0.5 + 0.5, sin_angle * 0.5 + 0.5, index);
		triangle_stream.Append(vertex);
	
		vertex.position = world_to_clip(position + N * xi.x * scale.y);
		vertex.normal = N;
		vertex.tangent = normalize(-tanx * sin_angle + tany * cos_angle);
		vertex.texcoord = float3(0.5, 0.5, index);
		triangle_stream.Append(vertex);
	
		angle += angle_delta;
		sin_angle = sin_fast(angle);
		cos_angle = cos_fast(angle);
	
		delta = tanx * cos_angle * xi.x + tany * sin_angle * xi.y;
		vertex.position = world_to_clip(position + delta);
		vertex.normal = normalize(delta) * cos_beta + N * sin_beta;
		vertex.tangent = normalize(-tanx * sin_angle + tany * cos_angle);
		vertex.texcoord = float3(cos_angle * 0.5 + 0.5, sin_angle * 0.5 + 0.5, index);
		triangle_stream.Append(vertex);
	
		angle += angle_delta;
	}
	
	sin_angle = sin_fast(angle);
	cos_angle = cos_fast(angle);
	
	delta = tanx * cos_angle * xi.y + tany * sin_angle * xi.y;
	vertex.position = world_to_clip(position + delta);
	vertex.normal = normalize(delta) * cos_beta + N * sin_beta;
	vertex.tangent = normalize(-tanx * sin_angle + tany * cos_angle);
	vertex.texcoord = float3(cos_angle * 0.5 + 0.5, sin_angle * 0.5 + 0.5, index);
	triangle_stream.Append(vertex);

	triangle_stream.RestartStrip();
}
#endif
void spawn_billboard(float3 position, float3 instance_position, float3x3 onb, float2 scale, float3 surface_normal, float index, inout TriangleStream<PixelStage> triangle_stream )
{
    PixelStage vertex;
    float3 tanx = onb[0];
    float3 tany = onb[1];
    float3 N = onb[2];
    scale.x *= 0.5;

    float3 windoff = calculate_wind_offset(instance_position);
    N = normalize(N + windoff);

    vertex.normal = normalize(surface_normal);
	vertex.tangent = normalize(tanx);

    vertex.position = world_to_clip(position - tanx * scale.x);
    vertex.texcoord = float3(0,1,index);
    triangle_stream.Append(vertex);

    vertex.position = world_to_clip(position + tanx * scale.x);
    vertex.texcoord = float3(1,1,index);
    triangle_stream.Append(vertex);

    vertex.position = world_to_clip(position - tanx * scale.x + N * scale.y);
    vertex.texcoord = float3(0,0,index);
    triangle_stream.Append(vertex);

    vertex.position = world_to_clip(position + tanx * scale.x + N * scale.y);
    vertex.texcoord = float3(1,0,index);
    triangle_stream.Append(vertex);

    triangle_stream.RestartStrip();
}
#ifdef ROCK_FOLIAGE
[maxvertexcount(13)] 
#else
[maxvertexcount(4)]
#endif
void gs( point VertexStageOut input[1], inout TriangleStream<PixelStage> triangle_stream )
{
    float4 position = input[0].position;
    float3 V = get_camera_position() - position.xyz;
    float V_dist = length(V);
    float far_clip = frame_.foliage_clipping_scaling.x;

    [branch]
    if(V_dist < far_clip && position.w) {

        // variables
        uint id = input[0].seed_id & 0xFF;
        uint seed = (input[0].seed_id >> 8);

        float2 xi = hammersley(seed, 256);

        float2 scale = foliage_[id].scale; 
        scale += foliage_[id].rescale_mult * scale * (xi.x * 2 - 1);

        float3 surface_normal = input[0].normal;
        PixelStage vertex;

        float3x3 onb = create_onb(surface_normal);
        float3 tanx = onb[0];
        float3 tany = onb[1];

        float c = cos_fast(xi.y * M_PI * 2);
        float s = sin_fast(xi.y * M_PI * 2);
        onb = mul( rotate_z(s, c) , onb);

        const float angle_min = M_PI / 8;
        const float angle_max = M_PI * 3 / 16; 

        V = normalize(V);

        //scale *= lerp(1, frame_.foliage_clipping_scaling.w, saturate((V_dist - frame_.foliage_clipping_scaling.y) / frame_.foliage_clipping_scaling.z));

        float critical_point = far_clip * 0.8f;
        const float bump = 1.3;
        scale *= min(1 + (bump - 1) * smoothstep(0, critical_point, V_dist), bump * smoothstep(0, far_clip - critical_point, far_clip - V_dist));

        float3 N = normalize(surface_normal * 3 + onb[1]);
        float f = dot(V, N);
        /*
        if(f < 0)
        {
            f = -f;
            N = -N;
        }*/
        float index = min(floor(xi.x * foliage_[id].textures_num), foliage_[id].textures_num);
#ifdef ROCK_FOLIAGE
		spawn_pebble(position.xyz, input[0].instance_position.xyz, onb, scale, surface_normal, index, xi, V_dist, triangle_stream);
#else
		float angle = lerp(angle_max, angle_min, saturate(f + 0.2f));
		onb = mul(rotate_x(angle), onb);
		spawn_billboard(position.xyz, input[0].instance_position.xyz, onb, scale, surface_normal, index, triangle_stream);
#endif
    }
}

#include <gbuffer_write.h>
void ps(PixelStage input, out GbufferOutput output)
{
	float4 tex = FoliageArray.Sample(TextureSampler, float3(input.texcoord));
	float3 N = input.normal;
	int material_id = 1;

	float4 ng = FoliageNormalArray.Sample(TextureSampler, float3(input.texcoord));
	float3 normalmap = ng.xyz * 2 - 1;
	normalmap.y *= -1;
	
	float3x3 tangent_to_world;
	float3 T, B;
	T = input.tangent;
	B = cross(T, N);
	
	tangent_to_world = float3x3(T, B, N);
	N = normalize(mul(normalmap, tangent_to_world));

	material_id = 0;
#ifdef ROCK_FOLIAGE
#else
    if(tex.w < 0.5)    
        discard;
#endif

    gbuffer_write(output, (tex.xyz), 0, ng.w, N, material_id, 1); // foliage material is forced in code to be in 1 index-slot
}