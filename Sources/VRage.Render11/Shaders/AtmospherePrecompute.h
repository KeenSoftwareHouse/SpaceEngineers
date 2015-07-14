// modified code from:

/**
 * Precomputed Atmospheric Scattering
 * Copyright (c) 2008 INRIA
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holders nor the names of its
 *    contributors may be used to endorse or promote products derived from
 *    this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */



float Limit(float radius, float mu) 
{
    float dout = -radius * mu + sqrt(radius * radius * (mu * mu - 1.0) + RadiusLimit * RadiusLimit);
    float delta2 = radius * radius * (mu * mu - 1.0) + RadiusGround * RadiusGround;
    if (delta2 >= 0.0) 
    {
        float din = -radius * mu - sqrt(delta2);
        if (din >= 0.0) 
        {
            dout = min(dout, din);
        }
    }
    return dout;
}

float OpticalDepth(float H, float radius, float mu) 
{
    float result = 0.0;
    float dx = Limit(radius, mu) / float(TRANSMITTANCE_STEPS_NUM);
    float xi = 0.0;
    float yi = exp(-(radius - RadiusGround) / H);
    for (uint i = 1; i <= TRANSMITTANCE_STEPS_NUM; ++i) 
    {
        float xj = float(i) * dx;
        float yj = exp(-(sqrt(radius * radius + xj * xj + 2.0 * xj * radius * mu) - RadiusGround) / H);
        result += (yi + yj) / 2.0 * dx;
        xi = xj;
        yi = yj;
    }
    return mu < -sqrt(1.0 - (RadiusGround / radius) * (RadiusGround / radius)) ? 1e9 : result;
}

void GetTransmittanceRMuS(float2 uv, out float radius, out float muS) 
{
    radius = uv.y;
    muS = uv.x;

    radius = RadiusGround + (radius * radius) * (RadiusAtmosphere - RadiusGround);
    muS = -0.15 + tan(1.5 * muS) / tan(1.5) * (1.0 + 0.15);
}

void GetInscatterRMuMuS(float3 uvw, out float radius, out float mu, out float muS) {
    //
    mu = uvw.y * 2 - 1;
    muS = -0.357143f * log(-2.16473f *(uvw.z - 1.02809f));

    //
    muS = uvw.z * 2 - 1;

    float r = uvw.x;
    r = sqrt(RadiusGround * RadiusGround + r * (RadiusLimit * RadiusLimit - RadiusGround * RadiusGround)) + (uvw.x == 0 ? 0.01 : (uvw.x == 1 ? -0.01 : 0.0));
    radius = r;

    float H = sqrt(RadiusLimit * RadiusLimit - RadiusGround * RadiusGround);
    float h = radius - RadiusGround; // z
    float rho = sqrt(radius * radius - RadiusGround * RadiusGround); // w
    float rh = RadiusLimit - radius; // x
    float rhoH = sqrt(radius * radius - RadiusGround * RadiusGround) + sqrt(RadiusLimit * RadiusLimit - RadiusGround * RadiusGround); // y

    float y = uvw.y;
    if(y < 0.5) {
        float d = 1 - y * 2;

        d = min(max(h, d * rho), rho * 0.999);
        mu = (RadiusGround * RadiusGround - radius * radius - d * d) / (2.0 * radius * d);
        mu = min(mu, -sqrt(1.0 - (RadiusGround / radius) * (RadiusGround / radius)) - 0.001);
    } else {
        float d = (y - 0.5) * 2;
        d = min(max(rh, d * rhoH), rhoH * 0.999);
        mu = (RadiusLimit * RadiusLimit - radius * radius - d * d) / (2.0 * radius * d);
    }

    muS = 0.1 - 0.27027 * tan(1.25 - 2.5*uvw.z);
}

void Integrand(float radius, float mu, float muS, float nu, float t, out float3 rayleigh, out float3 mie) 
{
    rayleigh = 0;
    mie = 0;
    float ri = sqrt(radius * radius + t * t + 2.0 * radius * mu * t);
    float muSi = (nu * t + muS * radius) / ri;
    ri = max(RadiusGround, ri);
    if (muSi >= -sqrt(1.0 - RadiusGround * RadiusGround / (ri * ri)) ) 
    {
        float3 ti = TransmittanceWithDistance(radius, mu, t) * Transmittance(ri, muSi);
        rayleigh = exp(-(ri - RadiusGround) / HeightScaleRayleighMie.x) * ti;
        mie = exp(-(ri - RadiusGround) / HeightScaleRayleighMie.y) * ti;
    }
}

void Inscatter(float radius, float mu, float muS, float nu, out float3 rayleigh, out float3 mie) // For Inscatter 1
{
    rayleigh = 0;
    mie = 0;
    float dx = Limit(radius, mu) / float(INSCATTER_STEPS_NUM);
    float xi = 0.0;
    float3 rayi;
    float3 miei;
    Integrand(radius, mu, muS, nu, 0.0, rayi, miei);
    for (uint i = 1; i <= INSCATTER_STEPS_NUM; ++i) 
    {
        float xj = float(i) * dx;
        float3 rayj;
        float3 miej;
        Integrand(radius, mu, muS, nu, xj, rayj, miej);
        rayleigh += (rayi + rayj) / 2.0 * dx;
        mie += (miei + miej) / 2.0 * dx;
        xi = xj;
        rayi = rayj;
        miei = miej;
    }

    rayleigh *= BetaRayleighScattering;
    mie *= BetaMieScattering;
}