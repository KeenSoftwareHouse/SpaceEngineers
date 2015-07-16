// using code from
//--------------------------------------------------------------------------------------
// Copyright 2013 Intel Corporation
// All Rights Reserved
//
// Permission is granted to use, copy, distribute and prepare derivative works of this
// software for any purpose and without fee, provided, that the above copyright notice
// and this statement appear in all copies.  Intel makes no representations about the
// suitability of this software for any purpose.  THIS SOFTWARE IS PROVIDED "AS IS."
// INTEL SPECIFICALLY DISCLAIMS ALL WARRANTIES, EXPRESS OR IMPLIED, AND ALL LIABILITY,
// INCLUDING CONSEQUENTIAL AND OTHER INDIRECT DAMAGES, FOR THE USE OF THIS SOFTWARE,
// INCLUDING LIABILITY FOR INFRINGEMENT OF ANY PROPRIETARY RIGHTS, AND INCLUDING THE
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE.  Intel does not
// assume any responsibility for any errors which may appear in this software nor any
// responsibility to update it.
//--------------------------------------------------------------------------------------

#include <math.h>


void GetRaySphereIntersection(float3 rayOrigin,
                              float3 rayDirection,
                              float3 sphereCenter,
                              float sphereRadius,
                              out float2 intersections)
{
    // http://wiki.cgsociety.org/index.php/Ray_Sphere_Intersection
    rayOrigin -= sphereCenter;
    float A = dot(rayDirection, rayDirection);
    float B = 2 * dot(rayOrigin, rayDirection);
    float C = dot(rayOrigin,rayOrigin) - sphereRadius*sphereRadius;
    float D = B*B - 4*A*C;
    // If discriminant is negative, there are no real roots hence the ray misses the
    // sphere
    if( D<0 )
    {
        intersections = -1;
    }
    else
    {
        D = sqrt(D);
        intersections = float2(-B - D, -B + D) / (2*A); // A must be positive here!!
    }
}

void GetRaySphereIntersection2(in float3 f3RayOrigin,
                               in float3 f3RayDirection,
                               in float3 f3SphereCenter,
                               in float2 f2SphereRadius,
                               out float4 f4Intersections)
{
    // http://wiki.cgsociety.org/index.php/Ray_Sphere_Intersection
    f3RayOrigin -= f3SphereCenter;
    float A = dot(f3RayDirection, f3RayDirection);
    float B = 2 * dot(f3RayOrigin, f3RayDirection);
    float2 C = dot(f3RayOrigin,f3RayOrigin) - f2SphereRadius*f2SphereRadius;
    float2 D = B*B - 4*A*C;
    // If discriminant is negative, there are no real roots hence the ray misses the
    // sphere
    float2 f2RealRootMask = (D.xy >= 0);
    D = sqrt( max(D,0) );
    f4Intersections =   f2RealRootMask.xxyy * float4(-B - D.x, -B + D.x, -B - D.y, -B + D.y) / (2*A) + 
                      (1-f2RealRootMask.xxyy) * float4(-1,-1,-1,-1);
}

float PhaseRayleigh(float mu) {
    return 3/4.f * (1 + mu*mu) / (4 * M_PI);
}

float PhaseMie(float mu) {
    const float g = 0.76f;
    return 3/2.f * (1-g*g) * (1+mu*mu) / (2+g*g) / pow(abs(1+g*g-2*g*mu), 1.5f) / (4 * M_PI);
}

float2 IntegrateParticleDensity(in float3 f3Start, 
                                in float3 f3End,
                                in float3 f3EarthCentre,
                                float fNumSteps )
{
    float3 f3Step = (f3End - f3Start) / fNumSteps;
    float fStepLen = length(f3Step);
        
    float fStartHeightAboveSurface = abs( length(f3Start - f3EarthCentre) - RadiusGround );
    float2 f2PrevParticleDensity = exp( -fStartHeightAboveSurface / HeightScaleRayleighMie );

    float2 f2ParticleNetDensity = 0;
    for(float fStepNum = 1; fStepNum <= fNumSteps; fStepNum += 1.f)
    {
        float3 f3CurrPos = f3Start + f3Step * fStepNum;
        float fHeightAboveSurface = abs( length(f3CurrPos - f3EarthCentre) - RadiusGround );
        float2 f2ParticleDensity = exp( -fHeightAboveSurface / HeightScaleRayleighMie );
        f2ParticleNetDensity += (f2ParticleDensity + f2PrevParticleDensity) * fStepLen / 2.f;
        f2PrevParticleDensity = f2ParticleDensity;
    }
    return f2ParticleNetDensity;
}


float2 IntegrateParticleDensityAlongRay(in float3 f3Pos, 
                                        in float3 f3RayDir,
                                        float3 f3EarthCentre, 
                                        const float fNumSteps,
                                        const bool bOccludeByEarth)
{
    if( bOccludeByEarth )
    {
        // If the ray intersects the Earth, return huge optical depth
        float2 f2RayEarthIsecs; 
        GetRaySphereIntersection(f3Pos, f3RayDir, f3EarthCentre, RadiusGround, f2RayEarthIsecs);
        if( f2RayEarthIsecs.x > 0 )
            return 1e+20;
    }

    // Get intersection with the top of the atmosphere (the start point must always be under the top of it)
    //      
    //                     /
    //                .   /  . 
    //      .  '         /\         '  .
    //                  /  f2RayAtmTopIsecs.y > 0
    //                 *
    //                   f2RayAtmTopIsecs.x < 0
    //                  /
    //      
    float2 f2RayAtmTopIsecs;
    GetRaySphereIntersection(f3Pos, f3RayDir, f3EarthCentre, RadiusAtmosphere, f2RayAtmTopIsecs);
    float fIntegrationDist = f2RayAtmTopIsecs.y;

    float3 f3RayEnd = f3Pos + f3RayDir * fIntegrationDist;

    return IntegrateParticleDensity(f3Pos, f3RayEnd, f3EarthCentre, fNumSteps);
}

float2 GetNetParticleDensity(in float fHeightAboveSurface,
                             in float fCosZenithAngle)
{
#if 1
    const float AtmTopHeight = (RadiusAtmosphere - RadiusGround);
    float fRelativeHeightAboveSurface = fHeightAboveSurface / AtmTopHeight;
    return DensityLut.SampleLevel(LinearSampler, float2(fRelativeHeightAboveSurface, fCosZenithAngle*0.5+0.5), 0).xy;
#else
//    return 0.1 * float2(OpticalDepth(Frame.RayleighHeightScale, fHeightAboveSurface + RadiusGround, fCosZenithAngle), OpticalDepth(Frame.MieHeightScale, fHeightAboveSurface + RadiusGround, fCosZenithAngle));

    const float AtmTopHeight = (RadiusAtmosphere - RadiusGround);

    float fRelativeHeightAboveSurface = fHeightAboveSurface / AtmTopHeight;
    float2 f2UV = float2(fRelativeHeightAboveSurface, fCosZenithAngle*0.5+0.5);
    float fStartHeight = clamp( lerp(0, AtmTopHeight, f2UV.x), AtmTopHeight * 0.01f, AtmTopHeight * 0.99f );

    float fCosTheta = f2UV.y * 2 - 1;
    float fSinTheta = sqrt( saturate(1 - fCosTheta*fCosTheta) );
    float3 f3RayStart = float3(0, 0, fStartHeight);
    float3 f3RayDir = float3(fSinTheta, 0, fCosTheta);
    
    float3 f3EarthCentre = float3(0,0,-RadiusGround);

    const float fNumSteps = 8;
    return IntegrateParticleDensityAlongRay(f3RayStart, f3RayDir, f3EarthCentre, fNumSteps, true);
#endif
}


float2 PrecomputeNetDensityToAtmTopPS(float2 uv) {
    const float AtmTopHeight = (RadiusAtmosphere - RadiusGround);
    float fStartHeight = clamp( lerp(0, AtmTopHeight, uv.x), 0.01f, AtmTopHeight-0.01f );

    float fCosTheta = uv.y * 2 - 1;
    float fSinTheta = sqrt( saturate(1 - fCosTheta*fCosTheta) );
    float3 f3RayStart = float3(0, 0, fStartHeight);
    float3 f3RayDir = float3(fSinTheta, 0, fCosTheta);
    
    float3 f3EarthCentre = float3(0,0,-RadiusGround);

    return IntegrateParticleDensityAlongRay(f3RayStart, f3RayDir, f3EarthCentre, 50, true);
}

float2 GetNetParticleDensity(in float3 f3Pos,
                             in float3 f3EarthCentre,
                             in float3 f3RayDir)
{
    float3 f3EarthCentreToPointDir = f3Pos - f3EarthCentre;
    float fDistToEarthCentre = length(f3EarthCentreToPointDir);
    f3EarthCentreToPointDir /= fDistToEarthCentre;
    float fHeightAboveSurface = fDistToEarthCentre - RadiusGround;
    float fCosZenithAngle = dot( f3EarthCentreToPointDir, f3RayDir );
    return GetNetParticleDensity(fHeightAboveSurface, fCosZenithAngle);
    //return IntegrateParticleDensityAlongRay(f3Pos, f3RayDir, f3EarthCentre, 8, true);
}

void ApplyPhaseFunctions(inout float3 f3RayleighInscattering,
                         inout float3 f3MieInscattering,
                         in float cosTheta)
{
    //f3RayleighInscattering *= g_MediaParams.f4AngularRayleighSctrCoeff.rgb * (1.0 + cosTheta*cosTheta);
    
    // Apply Cornette-Shanks phase function (see Nishita et al. 93):
    // F(theta) = 1/(4*PI) * 3*(1-g^2) / (2*(2+g^2)) * (1+cos^2(theta)) / (1 + g^2 - 2g*cos(theta))^(3/2)
    // f4CS_g = ( 3*(1-g^2) / (2*(2+g^2)), 1+g^2, -2g, 1 )
    // float fDenom = rsqrt( dot(g_MediaParams.f4CS_g.yz, float2(1.f, cosTheta)) ); // 1 / (1 + g^2 - 2g*cos(theta))^(1/2)
    // float fCornettePhaseFunc = g_MediaParams.f4CS_g.x * (fDenom*fDenom*fDenom) * (1 + cosTheta*cosTheta);
    // f3MieInscattering *= g_MediaParams.f4AngularMieSctrCoeff.rgb * fCornettePhaseFunc;



    f3RayleighInscattering *= BetaRayleighScattering * PhaseRayleigh(cosTheta);
    f3MieInscattering *= BetaMieScattering * PhaseMie(cosTheta);
}

// This function computes atmospheric properties in the given point
void GetAtmosphereProperties(in float3 f3Pos,
                             in float3 f3EarthCentre,
                             in float3 f3DirOnLight,
                             out float2 f2ParticleDensity,
                             out float2 f2NetParticleDensityToAtmTop)
{
    // Calculate the point height above the SPHERICAL Earth surface:
    float3 f3EarthCentreToPointDir = f3Pos - f3EarthCentre;
    float fDistToEarthCentre = length(f3EarthCentreToPointDir);
    f3EarthCentreToPointDir /= fDistToEarthCentre;
    float fHeightAboveSurface = fDistToEarthCentre - RadiusGround;

    f2ParticleDensity = exp( -fHeightAboveSurface / HeightScaleRayleighMie );

    // Get net particle density from the integration point to the top of the atmosphere:
    float fCosSunZenithAngleForCurrPoint = dot( f3EarthCentreToPointDir, f3DirOnLight );
    f2NetParticleDensityToAtmTop = GetNetParticleDensity(fHeightAboveSurface, fCosSunZenithAngleForCurrPoint);
}

// This function computes differential inscattering for the given particle densities 
// (without applying phase functions)
void ComputePointDiffInsctr(in float2 f2ParticleDensityInCurrPoint,
                            in float2 f2NetParticleDensityFromCam,
                            in float2 f2NetParticleDensityToAtmTop,
                            out float3 f3DRlghInsctr,
                            out float3 f3DMieInsctr)
{
    // Compute total particle density from the top of the atmosphere through the integraion point to camera
    float2 f2TotalParticleDensity = f2NetParticleDensityFromCam + f2NetParticleDensityToAtmTop;
        
    // Get optical depth
    float3 f3TotalRlghOpticalDepth = BetaRayleighScattering * f2TotalParticleDensity.x;
    float3 f3TotalMieOpticalDepth  = BetaMieScattering * f2TotalParticleDensity.y;
        
    // And total extinction for the current integration point:
    float3 f3TotalExtinction = exp( -(f3TotalRlghOpticalDepth + f3TotalMieOpticalDepth) );

    f3DRlghInsctr = f2ParticleDensityInCurrPoint.x * f3TotalExtinction;
    f3DMieInsctr  = f2ParticleDensityInCurrPoint.y * f3TotalExtinction; 
}

void ComputeInsctrIntegral(in float3 f3RayStart,
                           in float3 f3RayEnd,
                           in float3 f3EarthCentre,
                           in float3 f3DirOnLight,
                           inout float2 f2NetParticleDensityFromCam,
                           inout float3 f3RayleighInscattering,
                           inout float3 f3MieInscattering,
                           const float fNumSteps)
{
    float3 f3Step = (f3RayEnd - f3RayStart) / fNumSteps;
    float fStepLen = length(f3Step);

    // For trapezoidal integration we need to compute some variables for the starting point of the ray
    float2 f2PrevParticleDensity = 0;
    float2 f2NetParticleDensityToAtmTop = 0;
    GetAtmosphereProperties(f3RayStart, f3EarthCentre, f3DirOnLight, f2PrevParticleDensity, f2NetParticleDensityToAtmTop);

    float3 f3PrevDiffRInsctr = 0, f3PrevDiffMInsctr = 0;
    ComputePointDiffInsctr(f2PrevParticleDensity, f2NetParticleDensityFromCam, f2NetParticleDensityToAtmTop, f3PrevDiffRInsctr, f3PrevDiffMInsctr);

    // With trapezoidal integration, we will evaluate the function at the end of each section and 
    // compute area of a trapezoid
    for(float fStepNum = 1.f; fStepNum <= fNumSteps; fStepNum += 1.f)
    {
        float3 f3CurrPos = f3RayStart + f3Step * fStepNum;
        float2 f2ParticleDensity, f2NetParticleDensityToAtmTop;
        GetAtmosphereProperties(f3CurrPos, f3EarthCentre, f3DirOnLight, f2ParticleDensity, f2NetParticleDensityToAtmTop);

        // Accumulate net particle density from the camera to the integration point:
        f2NetParticleDensityFromCam += (f2PrevParticleDensity + f2ParticleDensity) * (fStepLen / 2.f);
        f2PrevParticleDensity = f2ParticleDensity;

        float3 f3DRlghInsctr, f3DMieInsctr;
        ComputePointDiffInsctr(f2ParticleDensity, f2NetParticleDensityFromCam, f2NetParticleDensityToAtmTop, f3DRlghInsctr, f3DMieInsctr);

        f3RayleighInscattering += (f3DRlghInsctr + f3PrevDiffRInsctr) * (fStepLen / 2.f);
        f3MieInscattering      += (f3DMieInsctr  + f3PrevDiffMInsctr) * (fStepLen / 2.f);

        f3PrevDiffRInsctr = f3DRlghInsctr;
        f3PrevDiffMInsctr = f3DMieInsctr;
    }  
}

void IntegrateUnshadowedInscattering(in float3 f3RayStart, 
                                     in float3 f3RayEnd,
                                     in float3 f3ViewDir,
                                     in float3 f3EarthCentre,
                                     in float3 f3DirOnLight,
                                     const float fNumSteps,
                                     out float3 f3Inscattering,
                                     out float3 f3Extinction)
{
    float2 f2NetParticleDensityFromCam = 0;
    float3 f3RayleighInscattering = 0;
    float3 f3MieInscattering = 0;
    ComputeInsctrIntegral( f3RayStart,
                           f3RayEnd,
                           f3EarthCentre,
                           f3DirOnLight,
                           f2NetParticleDensityFromCam,
                           f3RayleighInscattering,
                           f3MieInscattering,
                           fNumSteps);

    float3 f3TotalRlghOpticalDepth = BetaRayleighScattering * f2NetParticleDensityFromCam.x;
    float3 f3TotalMieOpticalDepth  = BetaMieScattering      * f2NetParticleDensityFromCam.y;
    f3Extinction = exp( -(f3TotalRlghOpticalDepth + f3TotalMieOpticalDepth) );

    // Apply phase function
    // Note that cosTheta = dot(DirOnCamera, LightDir) = dot(ViewDir, DirOnLight) because
    // DirOnCamera = -ViewDir and LightDir = -DirOnLight
    float cosTheta = dot(f3ViewDir, f3DirOnLight);
    ApplyPhaseFunctions(f3RayleighInscattering, f3MieInscattering, cosTheta);

    f3Inscattering = f3RayleighInscattering + f3MieInscattering;
}

#define PRECOMPUTED_SCTR_LUT_DIM float4(32,128,32,8)

#define NON_LINEAR_PARAMETERIZATION 1
static const float SafetyHeightMargin = 0.01f;
static const float HeightPower = 0.5f;
static const float ViewZenithPower = 0.6f;
static const float SunViewPower = 1.5f;

float GetCosHorizonAnlge(float fHeight)
{
    // Due to numeric precision issues, fHeight might sometimes be slightly negative
    fHeight = max(fHeight, 0);
    return -sqrt(fHeight * (2*RadiusGround + fHeight) ) / (RadiusGround + fHeight);
}

float TexCoord2ZenithAngle(float fTexCoord, float fHeight, in float fTexDim, float power)
{
    float fCosZenithAngle;

    float fCosHorzAngle = GetCosHorizonAnlge(fHeight);
    if( fTexCoord > 0.5 )
    {
        // Remap to [0,1] from the upper half of the texture [0.5 + 0.5/fTexDim, 1 - 0.5/fTexDim]
        fTexCoord = saturate( (fTexCoord - (0.5f + 0.5f / fTexDim)) * fTexDim / (fTexDim/2 - 1) );
        fTexCoord = pow(fTexCoord, 1/power);
        // Assure that the ray does NOT hit Earth
        fCosZenithAngle = max( (fCosHorzAngle + fTexCoord * (1 - fCosHorzAngle)), fCosHorzAngle + 1e-4);
    }
    else
    {
        // Remap to [0,1] from the lower half of the texture [0.5, 0.5 - 0.5/fTexDim]
        fTexCoord = saturate((fTexCoord - 0.5f / fTexDim) * fTexDim / (fTexDim/2 - 1));
        fTexCoord = pow(fTexCoord, 1/power);
        // Assure that the ray DOES hit Earth
        fCosZenithAngle = min( (fCosHorzAngle - fTexCoord * (fCosHorzAngle - (-1))), fCosHorzAngle - 1e-4);
    }
    return fCosZenithAngle;
}

float ZenithAngle2TexCoord(float fCosZenithAngle, float fHeight, in float fTexDim, float power, float fPrevTexCoord)
{
    fCosZenithAngle = fCosZenithAngle;
    float fTexCoord;
    float fCosHorzAngle = GetCosHorizonAnlge(fHeight);
    // When performing look-ups into the scattering texture, it is very important that all the look-ups are consistent
    // wrt to the horizon. This means that if the first look-up is above (below) horizon, then the second look-up
    // should also be above (below) horizon. 
    // We use previous texture coordinate, if it is provided, to find out if previous look-up was above or below
    // horizon. If texture coordinate is negative, then this is the first look-up
    bool bIsAboveHorizon = fPrevTexCoord >= 0.5;
    bool bIsBelowHorizon = 0 <= fPrevTexCoord && fPrevTexCoord < 0.5;
    if(  bIsAboveHorizon || 
        !bIsBelowHorizon && (fCosZenithAngle > fCosHorzAngle) )
    {
        // Scale to [0,1]
        fTexCoord = saturate( (fCosZenithAngle - fCosHorzAngle) / (1 - fCosHorzAngle) );
        fTexCoord = pow(fTexCoord, power);
        // Now remap texture coordinate to the upper half of the texture.
        // To avoid filtering across discontinuity at 0.5, we must map
        // the texture coordinate to [0.5 + 0.5/fTexDim, 1 - 0.5/fTexDim]
        //
        //      0.5   1.5               D/2+0.5        D-0.5  texture coordinate x dimension
        //       |     |                   |            |
        //    |  X  |  X  | .... |  X  ||  X  | .... |  X  |  
        //       0     1          D/2-1   D/2          D-1    texel index
        //
        fTexCoord = 0.5f + 0.5f / fTexDim + fTexCoord * (fTexDim/2 - 1) / fTexDim;
    }
    else
    {
        fTexCoord = saturate( (fCosHorzAngle - fCosZenithAngle) / (fCosHorzAngle - (-1)) );
        fTexCoord = pow(fTexCoord, power);
        // Now remap texture coordinate to the lower half of the texture.
        // To avoid filtering across discontinuity at 0.5, we must map
        // the texture coordinate to [0.5, 0.5 - 0.5/fTexDim]
        //
        //      0.5   1.5        D/2-0.5             texture coordinate x dimension
        //       |     |            |       
        //    |  X  |  X  | .... |  X  ||  X  | .... 
        //       0     1          D/2-1   D/2        texel index
        //
        fTexCoord = 0.5f / fTexDim + fTexCoord * (fTexDim/2 - 1) / fTexDim;
    }    

    return fTexCoord;
}

float4 WorldParams2InsctrLUTCoords(float fHeight,
                                   float fCosViewZenithAngle,
                                   float fCosSunZenithAngle,
                                   float fCosSunViewAngle,
                                   in float4 f4RefUVWQ)
{
    float4 f4UVWQ;

    // Limit allowable height range to [SafetyHeightMargin, AtmTopHeight - SafetyHeightMargin] to
    // avoid numeric issues at the Earth surface and the top of the atmosphere
    // (ray/Earth and ray/top of the atmosphere intersection tests are unstable when fHeight == 0 and
    // fHeight == AtmTopHeight respectively)

    const float AtmTopHeight = (RadiusAtmosphere - RadiusGround);

    fHeight = clamp(fHeight, SafetyHeightMargin, AtmTopHeight - SafetyHeightMargin);
    f4UVWQ.x = saturate( (fHeight - SafetyHeightMargin) / (AtmTopHeight - 2*SafetyHeightMargin) );

#if NON_LINEAR_PARAMETERIZATION
    f4UVWQ.x = pow(f4UVWQ.x, HeightPower);

    f4UVWQ.y = ZenithAngle2TexCoord(fCosViewZenithAngle, fHeight, PRECOMPUTED_SCTR_LUT_DIM.y, ViewZenithPower, f4RefUVWQ.y);
    
    // Use Eric Bruneton's formula for cosine of the sun-zenith angle
    //f4UVWQ.z = (atan(max(fCosSunZenithAngle, -0.1975) * tan(1.26 * 1.1)) / 1.1 + (1.0 - 0.26)) * 0.5;
    f4UVWQ.z = atan((fCosSunZenithAngle-0.1)*3.7)*0.4 + 0.5;

    fCosSunViewAngle = clamp(fCosSunViewAngle, -1, +1);
    f4UVWQ.w = acos(fCosSunViewAngle) / M_PI;
    f4UVWQ.w = sign(f4UVWQ.w - 0.5) * pow( abs((f4UVWQ.w - 0.5)/0.5), SunViewPower)/2 + 0.5;
    
    f4UVWQ.xzw = ((f4UVWQ * (PRECOMPUTED_SCTR_LUT_DIM-1) + 0.5) / PRECOMPUTED_SCTR_LUT_DIM).xzw;
#else
    f4UVWQ.y = (fCosViewZenithAngle+1.f) / 2.f;
    f4UVWQ.z = (fCosSunZenithAngle +1.f) / 2.f;
    f4UVWQ.w = (fCosSunViewAngle   +1.f) / 2.f;

    f4UVWQ = (f4UVWQ * (PRECOMPUTED_SCTR_LUT_DIM-1) + 0.5) / PRECOMPUTED_SCTR_LUT_DIM;
#endif

    //f4UVWQ.y = (fCosViewZenithAngle+1.f) / 2.f;
    //f4UVWQ.z = (fCosSunZenithAngle +1.f) / 2.f;
    //f4UVWQ.w = (fCosSunViewAngle   +1.f) / 2.f;

    //f4UVWQ.z = (f4UVWQ.z * (PRECOMPUTED_SCTR_LUT_DIM.z-1) + 0.5) / PRECOMPUTED_SCTR_LUT_DIM.z;

    return f4UVWQ;
}

void InsctrLUTCoords2WorldParams(in float4 f4UVWQ,
                                 out float fHeight,
                                 out float fCosViewZenithAngle,
                                 out float fCosSunZenithAngle,
                                 out float fCosSunViewAngle)
{
#if NON_LINEAR_PARAMETERIZATION
    // Rescale to exactly 0,1 range
    f4UVWQ.xzw = saturate((f4UVWQ* PRECOMPUTED_SCTR_LUT_DIM - 0.5) / (PRECOMPUTED_SCTR_LUT_DIM-1)).xzw;

    f4UVWQ.x = pow( f4UVWQ.x, 1/HeightPower );
    // Allowable height range is limited to [SafetyHeightMargin, AtmTopHeight - SafetyHeightMargin] to
    // avoid numeric issues at the Earth surface and the top of the atmosphere
    fHeight = f4UVWQ.x * ((RadiusAtmosphere - RadiusGround) - 2*SafetyHeightMargin) + SafetyHeightMargin;

    fCosViewZenithAngle = TexCoord2ZenithAngle(f4UVWQ.y, fHeight, PRECOMPUTED_SCTR_LUT_DIM.y, ViewZenithPower);
    
    // Use Eric Bruneton's formula for cosine of the sun-zenith angle
    //fCosSunZenithAngle = tan((2.0 * f4UVWQ.z - 1.0 + 0.26) * 1.1) / tan(1.26 * 1.1);
    fCosSunZenithAngle = 0.1 - 0.27027 * tan(1.25 - 2.5*f4UVWQ.z);

    f4UVWQ.w = sign(f4UVWQ.w - 0.5) * pow( abs((f4UVWQ.w - 0.5)*2), 1/SunViewPower)/2 + 0.5;
    fCosSunViewAngle = cos(f4UVWQ.w*M_PI);
#else
    // Rescale to exactly 0,1 range
    f4UVWQ = (f4UVWQ * PRECOMPUTED_SCTR_LUT_DIM - 0.5) / (PRECOMPUTED_SCTR_LUT_DIM-1);

    // Allowable height range is limited to [SafetyHeightMargin, AtmTopHeight - SafetyHeightMargin] to
    // avoid numeric issues at the Earth surface and the top of the atmosphere
    fHeight = f4UVWQ.x * ((RadiusAtmosphere - RadiusGround) - 2*SafetyHeightMargin) + SafetyHeightMargin;

    fCosViewZenithAngle = f4UVWQ.y * 2 - 1;
    fCosSunZenithAngle  = f4UVWQ.z * 2 - 1;
    fCosSunViewAngle    = f4UVWQ.w * 2 - 1;
#endif

    //fCosViewZenithAngle = f4UVWQ.y * 2 - 1;
    //fCosSunZenithAngle  = f4UVWQ.z * 2 - 1;
    //fCosSunViewAngle    = f4UVWQ.w * 2 - 1;

    fCosViewZenithAngle = clamp(fCosViewZenithAngle, -1, +1);
    fCosSunZenithAngle  = clamp(fCosSunZenithAngle,  -1, +1);
    // Compute allowable range for the cosine of the sun view angle for the given
    // view zenith and sun zenith angles
    float D = (1.0 - fCosViewZenithAngle * fCosViewZenithAngle) * (1.0 - fCosSunZenithAngle  * fCosSunZenithAngle);
    
    // !!!!  IMPORTANT NOTE regarding NVIDIA hardware !!!!

    // There is a very weird issue on NVIDIA hardware with clamp(), saturate() and min()/max() 
    // functions. No matter what function is used, fCosViewZenithAngle and fCosSunZenithAngle
    // can slightly fall outside [-1,+1] range causing D to be negative
    // Using saturate(D), max(D, 0) and even D>0?D:0 does not work!
    // The only way to avoid taking the square root of negative value and obtaining NaN is 
    // to use max() with small positive value:
    D = sqrt( max(D, 1e-20) );
    
    // The issue was reproduceable on NV GTX 680, driver version 9.18.13.2723 (9/12/2013).
    // The problem does not arise on Intel hardware

    float2 f2MinMaxCosSunViewAngle = fCosViewZenithAngle*fCosSunZenithAngle + float2(-D, +D);
    // Clamp to allowable range
    fCosSunViewAngle    = clamp(fCosSunViewAngle, f2MinMaxCosSunViewAngle.x, f2MinMaxCosSunViewAngle.y);
}


float3 ComputeViewDir(in float fCosViewZenithAngle)
{
    return float3(sqrt(saturate(1 - fCosViewZenithAngle*fCosViewZenithAngle)), fCosViewZenithAngle, 0);
}

float3 ComputeLightDir(in float3 f3ViewDir, in float fCosSunZenithAngle, in float fCosSunViewAngle)
{
    float3 f3DirOnLight;
    f3DirOnLight.x = (f3ViewDir.x > 0) ? (fCosSunViewAngle - fCosSunZenithAngle * f3ViewDir.y) / f3ViewDir.x : 0;
    f3DirOnLight.y = fCosSunZenithAngle;
    f3DirOnLight.z = sqrt( saturate(1 - dot(f3DirOnLight.xy, f3DirOnLight.xy)) );
    // Do not normalize f3DirOnLight! Even if its length is not exactly 1 (which can 
    // happen because of fp precision issues), all the dot products will still be as 
    // specified, which is essentially important. If we normalize the vector, all the 
    // dot products will deviate, resulting in wrong pre-computation.
    // Since fCosSunViewAngle is clamped to allowable range, f3DirOnLight should always
    // be normalized. However, due to some issues on NVidia hardware sometimes
    // it may not be as that (see IMPORTANT NOTE regarding NVIDIA hardware)
    //f3DirOnLight = normalize(f3DirOnLight);
    return f3DirOnLight;
}

float3 PrecomputeSingleScattering(float4 f4UVWQ, uint samplesNum)
{
    float fHeight, fCosViewZenithAngle, fCosSunZenithAngle, fCosSunViewAngle;
    InsctrLUTCoords2WorldParams(f4UVWQ, fHeight, fCosViewZenithAngle, fCosSunZenithAngle, fCosSunViewAngle );

    float3 f3EarthCentre =  -float3(0,1,0) * RadiusGround;
    float3 f3RayStart = float3(0, fHeight, 0);
    float3 f3ViewDir = ComputeViewDir(fCosViewZenithAngle);
    float3 f3DirOnLight = ComputeLightDir(f3ViewDir, fCosSunZenithAngle, fCosSunViewAngle);

    // Intersect view ray with the top of the atmosphere and the Earth
    float4 f4Isecs;
    GetRaySphereIntersection2( f3RayStart, f3ViewDir, f3EarthCentre, 
                               float2(RadiusGround, RadiusAtmosphere), 
                               f4Isecs);
    float2 f2RayEarthIsecs  = f4Isecs.xy;
    float2 f2RayAtmTopIsecs = f4Isecs.zw;

    if(f2RayAtmTopIsecs.y <= 0)
        return 0; // This is just a sanity check and should never happen
                  // as the start point is always under the top of the 
                  // atmosphere (look at InsctrLUTCoords2WorldParams())

    // Set the ray length to the distance to the top of the atmosphere
    float fRayLength = f2RayAtmTopIsecs.y;
    // If ray hits Earth, limit the length by the distance to the surface
    if(f2RayEarthIsecs.x > 0) {
        fRayLength = min(fRayLength, f2RayEarthIsecs.x);
    }
    
    float3 f3RayEnd = f3RayStart + f3ViewDir * fRayLength;

    // Integrate single-scattering
    float3 f3Inscattering, f3Extinction;
    IntegrateUnshadowedInscattering(f3RayStart, 
                                    f3RayEnd,
                                    f3ViewDir,
                                    f3EarthCentre,
                                    f3DirOnLight.xyz,
                                    samplesNum,
                                    f3Inscattering,
                                    f3Extinction);

    // float3 ray, mie;
    // Inscatter(fHeight + RadiusGround, fCosViewZenithAngle, fCosSunZenithAngle, 0, ray, mie );
    // return ray;

    return f3Inscattering;
}

float3 LookUpPrecomputedScattering(float3 f3StartPoint, 
                                   float3 f3ViewDir, 
                                   float3 f3EarthCentre,
                                   float3 f3DirOnLight,
                                   in Texture3D<float4> tex3DScatteringLUT,
                                   inout float4 f4UVWQ)
{
    float3 f3EarthCentreToPointDir = f3StartPoint - f3EarthCentre;
    float fDistToEarthCentre = length(f3EarthCentreToPointDir);
    f3EarthCentreToPointDir /= fDistToEarthCentre;
    float fHeightAboveSurface = fDistToEarthCentre - RadiusGround;
    float fCosViewZenithAngle = dot( f3EarthCentreToPointDir, f3ViewDir    );
    float fCosSunZenithAngle  = dot( f3EarthCentreToPointDir, f3DirOnLight );
    float fCosSunViewAngle    = dot( f3ViewDir,               f3DirOnLight );

    // Provide previous look-up coordinates
    f4UVWQ = WorldParams2InsctrLUTCoords(fHeightAboveSurface, fCosViewZenithAngle,
                                         fCosSunZenithAngle, fCosSunViewAngle, 
                                         f4UVWQ);

    float3 f3UVW0; 
    f3UVW0.xy = f4UVWQ.xy;
    float fQ0Slice = floor(f4UVWQ.w * PRECOMPUTED_SCTR_LUT_DIM.w - 0.5);
    fQ0Slice = clamp(fQ0Slice, 0, PRECOMPUTED_SCTR_LUT_DIM.w-1);
    float fQWeight = (f4UVWQ.w * PRECOMPUTED_SCTR_LUT_DIM.w - 0.5) - fQ0Slice;
    fQWeight = max(fQWeight, 0);
    float2 f2SliceMinMaxZ = float2(fQ0Slice, fQ0Slice+1)/PRECOMPUTED_SCTR_LUT_DIM.w + float2(0.5,-0.5) / (PRECOMPUTED_SCTR_LUT_DIM.z*PRECOMPUTED_SCTR_LUT_DIM.w);
    f3UVW0.z =  (fQ0Slice + f4UVWQ.z) / PRECOMPUTED_SCTR_LUT_DIM.w;
    f3UVW0.z = clamp(f3UVW0.z, f2SliceMinMaxZ.x, f2SliceMinMaxZ.y);
    
    float fQ1Slice = min(fQ0Slice+1, PRECOMPUTED_SCTR_LUT_DIM.w-1);
    float fNextSliceOffset = (fQ1Slice - fQ0Slice) / PRECOMPUTED_SCTR_LUT_DIM.w;
    float3 f3UVW1 = f3UVW0 + float3(0,0,fNextSliceOffset);
    float3 f3Insctr0 = tex3DScatteringLUT.SampleLevel(LinearSampler, f3UVW0, 0).xyz;
    float3 f3Insctr1 = tex3DScatteringLUT.SampleLevel(LinearSampler, f3UVW1, 0).xyz;
    float3 f3Inscattering = lerp(f3Insctr0, f3Insctr1, fQWeight);

    return f3Inscattering;
}


float2 GetDensityIntegralAnalytic(float r, float mu, float d) 
{
    float2 f2A = sqrt( (0.5/HeightScaleRayleighMie.xy) * r );
    float4 f4A01 = f2A.xxyy * float2(mu, mu + d / r).xyxy;
    float4 f4A01s = sign(f4A01);
    float4 f4A01sq = f4A01*f4A01;
    
    float2 f2X;
    f2X.x = f4A01s.y > f4A01s.x ? exp(f4A01sq.x) : 0.0;
    f2X.y = f4A01s.w > f4A01s.z ? exp(f4A01sq.z) : 0.0;
    
    float4 f4Y = f4A01s / (2.3193*abs(f4A01) + sqrt(1.52*f4A01sq + 4.0)) * float3(1.0, exp(-d/HeightScaleRayleighMie.xy*(d/(2.0*r)+mu))).xyxz;

    return sqrt((6.2831*HeightScaleRayleighMie)*r) * exp((RadiusGround-r)/HeightScaleRayleighMie.xy) * (f2X + float2( dot(f4Y.xy, float2(1.0, -1.0)), dot(f4Y.zw, float2(1.0, -1.0)) ));
}

float3 GetExtinctionUnverified(in float3 f3StartPos, in float3 f3EndPos, float3 f3EyeDir, float3 f3EarthCentre)
{
#if 0
    float2 f2ParticleDensity = IntegrateParticleDensity(f3StartPos, f3EndPos, f3EarthCentre, 20);
#else
    float r = length(f3StartPos-f3EarthCentre);
    float fCosZenithAngle = dot(f3StartPos-f3EarthCentre, f3EyeDir) / r;
    float2 f2ParticleDensity = GetDensityIntegralAnalytic(r, fCosZenithAngle, length(f3StartPos - f3EndPos));
#endif

    // Get optical depth
    float3 f3TotalRlghOpticalDepth = BetaRayleighScattering * f2ParticleDensity.x;
    float3 f3TotalMieOpticalDepth  = BetaMieScattering / BetaRatio * f2ParticleDensity.y;
        
    // Compute extinction
    float3 f3Extinction = exp( -(f3TotalRlghOpticalDepth + f3TotalMieOpticalDepth) );
    return f3Extinction;
}