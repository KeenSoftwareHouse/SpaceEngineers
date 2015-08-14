#ifndef SAMPLE_FREQ_PASS
#define PIXEL_FREQ_PASS
#endif

#include <frame.h>
#include <postprocess_base.h>
#include <gbuffer.h>
#include <vertex_transformations.h>

Texture2D<float4> DensityLut : register( t5 );
Texture3D<float4> Inscatter1LutR : register( t6 );
Texture3D<float4> Inscatter1LutM : register( t7 );

Texture3D<float4> Inscatter1Lut : register( t6 );

cbuffer AtmosphereConstants : register( b1 ) {
    matrix WorldViewProj;
    float3 PlanetCentre;
    float RadiusAtmosphere;
    float3 BetaRayleighScattering;
    float RadiusGround;
    float3 BetaMieScattering;
    float RadiusLimit;
    float2 HeightScaleRayleighMie;
};

static const float BetaRatio = 0.9f;

#include <AtmosphereCommon.h>

struct ProxyVertex
{
    float4 position : POSITION;
};

void proxyVs(float4 vertexPos : POSITION, out float4 svPos : SV_Position)
{
    svPos = mul(unpack_position_and_scale(vertexPos), WorldViewProj);
}


#define FLT_MAX 3.402823466e+38f

// additive blend
void psAtmosphereInscatter(float4 svPos : SV_Position, out float4 output : SV_Target0
#ifdef SAMPLE_FREQ_PASS
    , uint sample_index : SV_SampleIndex
#endif
    ) {    
#if !defined(MS_SAMPLE_COUNT) || defined(PIXEL_FREQ_PASS)
    SurfaceInterface input = read_gbuffer(svPos.xy);
#else
    SurfaceInterface input = read_gbuffer(svPos.xy, sample_index);
#endif

    float3 V = input.V;
    float3 L = -frame_.directionalLightVec;

    float3 f3Inscattering = 0;
    float3 f3Extinction = 1;
    float3 f3RayTermination = input.position;
    float3 f3CameraPos = 0;
    float3 f3ViewDir = -V;
    float fRayLength = length(f3RayTermination - f3CameraPos);

    float3 f3EarthCentre =  PlanetCentre;
    float2 f2RayAtmTopIsecs;
    GetRaySphereIntersection( f3CameraPos, f3ViewDir, f3EarthCentre, 
                              RadiusAtmosphere, 
                              f2RayAtmTopIsecs);
    if( f2RayAtmTopIsecs.y > 0 ) {
        float3 f3RayStart = f3CameraPos + f3ViewDir * max(0, f2RayAtmTopIsecs.x);
        if( !depth_not_background(input.native_depth) ) {
            fRayLength = +FLT_MAX;
        }
        float3 f3RayEnd = f3CameraPos + f3ViewDir * min(fRayLength, f2RayAtmTopIsecs.y);

        #if 0

        IntegrateUnshadowedInscattering(f3RayStart, 
                                        f3RayEnd,
                                        f3ViewDir,
                                        f3EarthCentre,
                                        L,
                                        8,
                                        f3Inscattering,
                                        f3Extinction);
        
        #else
    
        f3Extinction = GetExtinctionUnverified(f3RayStart, f3RayEnd, f3ViewDir, f3EarthCentre);
        // To avoid artifacts, we must be consistent when performing look-ups into the scattering texture, i.e.
        // we must assure that if the first look-up is above (below) horizon, then the second look-up
        // is also above (below) horizon. 
        float4 f4UVWQ = -1;
        f3Inscattering +=                LookUpPrecomputedScattering(f3RayStart, f3ViewDir, f3EarthCentre, L, Inscatter1Lut, f4UVWQ); 
        // Provide previous look-up coordinates to the function to assure that look-ups are consistent
        f3Inscattering -= f3Extinction * LookUpPrecomputedScattering(f3RayEnd,   f3ViewDir, f3EarthCentre, L, Inscatter1Lut, f4UVWQ);

        //f3Inscattering = f4UVWQ.y * 0.1;



        #endif
    }

    output = float4(f3Inscattering * 80, 0);
}

// blend 
void psAtmosphereTransmittance(float4 svPos : SV_Position, out float4 output : SV_Target0
#ifdef SAMPLE_FREQ_PASS
    , uint sample_index : SV_SampleIndex
#endif
    ) {

#if !defined(MS_SAMPLE_COUNT) || defined(PIXEL_FREQ_PASS)
    SurfaceInterface input = read_gbuffer(svPos.xy);
#else
    SurfaceInterface input = read_gbuffer(svPos.xy, sample_index);
#endif

    float3 V = input.V;
    float3 L = -frame_.directionalLightVec;

    float3 f3Inscattering = 0;
    float3 f3Extinction = 1;
    float3 f3RayTermination = input.position;
    float3 f3CameraPos = 0;
    float3 f3ViewDir = -V;
    float fRayLength = length(f3RayTermination - f3CameraPos);

    float3 f3EarthCentre =  PlanetCentre;
    float2 f2RayAtmTopIsecs;
    GetRaySphereIntersection( f3CameraPos, f3ViewDir, f3EarthCentre, 
                              RadiusAtmosphere, 
                              f2RayAtmTopIsecs);
    if( f2RayAtmTopIsecs.y > 0 ) {
        float3 f3RayStart = f3CameraPos + f3ViewDir * max(0, f2RayAtmTopIsecs.x);
        if( !depth_not_background(input.native_depth) ) {
            fRayLength = +FLT_MAX;
        }
        float3 f3RayEnd = f3CameraPos + f3ViewDir * min(fRayLength, f2RayAtmTopIsecs.y);
        f3Extinction = GetExtinctionUnverified(f3RayStart, f3RayEnd, f3ViewDir, f3EarthCentre);

        output = float4(f3Extinction, 1);
    }
    else {
        output = float4(1,1,1,0);
        discard;
    }
    // float2 atmosphereIsects;
    // GetRaySphereIntersection(viewer, -V, PlanetCentre, RadiusAtmosphere, atmosphereIsects);

    // if(atmosphereIsects.y > 0) {
    //     float t = atmosphereIsects.y;
    //     t = min(t, length(input.position));

    //     float3 x0 = viewer -V * t;
    //     float r = length(x0 - PlanetCentre);

    //     if(r < RadiusLimit) {
    //         float mu = dot(x0 - PlanetCentre, -V) / r;
    //         float muS = dot(x0 - PlanetCentre, L) / r;
    //         float nu = dot(-V, L);

    //         viewer += -V * max(atmosphereIsects.x, 0);

    //         float3 attenaution = TransmittanceWithDistance(r, mu, length(x0 - viewer));
    //         attenaution = 1;

    //         output = float4(attenaution, 0); // color = color * t
    //         //return;
    //     }
    // }

    // if(atmosphereIsects.y > 0) {


    //     output = 1;
    // }
    // else {
    //     output = float4(1,1,1,0);
    //     discard;
    // }
    //output = float4(1,1,1,0);
}
