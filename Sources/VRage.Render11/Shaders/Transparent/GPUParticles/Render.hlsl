//
//    Shader code for rendering particles as simple quads using rasterization
//

#include "Globals.hlsli"
#include <Transparent/OIT/Globals.hlsli>
#include <ShadowsOld/Csm.hlsli>
#include <Lighting/EnvAmbient.hlsli>
#include <Math/Color.hlsli>

struct PS_INPUT
{
    float4    Position                    : SV_POSITION;
    float3    TexCoord                    : TEXCOORD0;
    float2    OITWeight_SoftParticle      : TEXCOORD1;
    float4    Color                       : COLOR0;
};

// The particle buffer data. Note this is only one half of the particle data - the data that is relevant to rendering as opposed to simulation
StructuredBuffer<Particle>                 g_ParticleBuffer          : register(t0);

// The sorted index list of particles
StructuredBuffer<float>                    g_AliveIndexBuffer        : register(t2);

// The number of alive particles this frame
cbuffer ActiveListCount : register(b1)
{
    uint    g_NumActiveParticles;
    uint3   ActiveListCount_pad;
};

// Vertex shader only path
PS_INPUT __vertex_shader(uint VertexId : SV_VertexID)
{
    PS_INPUT Output = (PS_INPUT)0;

    // Particle index 
    uint particleIndex = VertexId / 4;

    // Per-particle corner index
    uint cornerIndex = VertexId % 4;

    float xOffset = 0;
    
    const float2 offsets[ 4 ] =
    {
        float2(-1, 1),
        float2(1, 1),
        float2(-1, -1),
        float2(1, -1),
    };

    uint index = (uint)g_AliveIndexBuffer[g_NumActiveParticles - particleIndex - 1];
    Particle pa = g_ParticleBuffer[index];

    EmittersStructuredBuffer emitter = g_Emitters[pa.EmitterIndex];
    float lifetimeFactor = 1.0 - saturate(pa.Age / emitter.ParticleLifeSpan);

    // PARTICLE RADIUS
    int keyIndex;
    float keyFactor = Interpolate(lifetimeFactor, emitter.ParticleSizeKeys, keyIndex);
    float radius = lerp(emitter.ParticleSize[keyIndex], emitter.ParticleSize[keyIndex + 1], keyFactor) * emitter.Scale;;

    // UV
    float2 offset = offsets[cornerIndex];
    float3 uv = UnpackUV(offset, g_Emitters[pa.EmitterIndex].TextureIndex1, g_Emitters[pa.EmitterIndex].TextureIndex2, g_Emitters[pa.EmitterIndex].ParticleLifeSpan, pa.Age, g_Emitters[pa.EmitterIndex].AnimationFrameTime);

    // VERTEX POSITION / BILLBOARDING + STREAKS
    float3 wPos;
    float4 pPos;
#if defined (STREAKS)
    if (emitter.Flags & EMITTERFLAG_STREAKS)
    {
        float3 cameraFacingPos = mul(float4(pa.Position, 1), frame_.view_matrix).xyz;
        float3x3 mat = (float3x3)frame_.view_matrix;
        float2 vsVelocity = mul(pa.Velocity.xyz * emitter.StreakMultiplier, mat).xy;

        float2 ellipsoidRadius = calcEllipsoidRadius(radius, vsVelocity) * offset;

        float2 extrusionVector = normalize(vsVelocity);
        float2 tangentVector = float2(extrusionVector.y, -extrusionVector.x);
        cameraFacingPos.xy += ellipsoidRadius.y * extrusionVector + ellipsoidRadius.x * tangentVector;
        wPos = mul(float4(cameraFacingPos, 1), frame_.inv_view_matrix).xyz;
        pPos = mul(float4(cameraFacingPos, 1), frame_.projection_matrix);
    }
    else
#endif
    {
        float s, c;
        sincos(lifetimeFactor * pa.RotationVelocity + M_PI * pa.Variation, s, c);
        float2x2 rotation = { float2(c, -s), float2(s, c) };

        offset = mul(offset, rotation);

        // individual billboarding
        float2 localVertex = radius * offset;
        float3 look = normalize(GetEyeCenterPosition() - pa.Position);
        float3 upCamera = float3(frame_.view_matrix._12, frame_.view_matrix._22, frame_.view_matrix._32);
        float3 right = cross(upCamera, look);
        float3 up = cross(look, right);
        float3x3 bbm = float3x3(right, up, look);
        wPos = mul(float3(localVertex, 0), bbm) + pa.Position;
        pPos = mul(float4(wPos, 1), frame_.view_projection_matrix);

        // collective billboarding
        //float3 cameraFacingPos = mul(float4(pa.Position, 1), frame_.view_matrix).xyz;
        //cameraFacingPos.xy += radius * offset;
        //wPos = mul(float4(cameraFacingPos, 1), frame_.inv_view_matrix).xyz;
        //pPos = mul(float4(cameraFacingPos, 1), frame_.projection_matrix);
    }
        
    // COLOR / OPACITY
    keyFactor = Interpolate(lifetimeFactor, emitter.ColorKeys, keyIndex);
    float4 color1 = emitter.Colors[keyIndex];
    float4 color2 = emitter.Colors[keyIndex + 1];
    float4 colorInterpolated = lerp(color1, color2, keyFactor);
    if (colorInterpolated.a > 0)
    {
        float3 colorHSV = rgb_to_hsv(colorInterpolated.rgb / colorInterpolated.a);
        colorHSV.r += emitter.HueVar * pa.Variation;
        colorHSV.b = clamp(colorHSV.b + emitter.ColorVar * -pa.Variation, 0, 1000.0f);
        colorInterpolated.rgb = hsv_to_rgb(colorHSV) * colorInterpolated.a;
    }
    Output.Color = colorInterpolated;

    // LIGHTING
#ifdef LIT_PARTICLE
    if (emitter.Flags & EMITTERFLAG_LIGHT)
    {
        float depth = -pPos.z / pPos.w;

        // shadow
        float shadow = calculate_shadow_fast_particle(wPos, depth);

        // volumetric light
        float3 dirLight = 1;
        if (emitter.Flags & EMITTERFLAG_VOLUMETRICLIGHT)
        {
            float3 emitterToParticle = wPos - pa.Origin;
            float scalarDistanceToParticle = dot(emitterToParticle, pa.Normal);
            float3 projectedParticle = wPos - scalarDistanceToParticle * pa.Normal;
            float3 planarNormal = normalize(projectedParticle - pa.Origin);
            float emitterNdotL = saturate(dot(-frame_.directionalLightVec, planarNormal));
            dirLight = emitterNdotL * frame_.directionalLightColor;
        }

        // ambient
        float3 ambientLight = ambient_diffuse(1, normalize(wPos));

        float3 light = shadow * dirLight + ambientLight;
        Output.Color.rgb *= light;
    }
#endif

    // OUTPUT TO PS
    Output.Position = pPos;
    Output.TexCoord = uv;
    Output.OITWeight_SoftParticle.x = emitter.OITWeightFactor;
    Output.OITWeight_SoftParticle.y = emitter.SoftParticleDistanceScale;

    return Output;
}


// The texture atlas for the particles
Texture2DArray        g_ParticleTextureArray            : register(t1);

// Ratserization path's pixel shader
void __pixel_shader(PS_INPUT In, out float4 accumTarget : SV_TARGET0, out float4 coverageTarget : SV_TARGET1)
{
    // SOFT PARTICLES
    float depth = g_DepthTexture[In.Position.xy].r;
    float targetDepth = linearize_depth(depth, frame_.projection_matrix);
    float particleDepth = linearize_depth(In.Position.z, frame_.projection_matrix);
    float depthFade = CalcSoftParticle(In.OITWeight_SoftParticle.y, targetDepth, particleDepth);

    // COLOR & LIGHT
    float4 albedo = g_ParticleTextureArray.Sample(DefaultSampler, In.TexCoord);    // 2d

    // Multiply in the particle color
    float4 color = albedo * In.Color * depthFade;

    TransparentColorOutput(color, particleDepth, In.Position.z, In.OITWeight_SoftParticle.x, accumTarget, coverageTarget);
}
