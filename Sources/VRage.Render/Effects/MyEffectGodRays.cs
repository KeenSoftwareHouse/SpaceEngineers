using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Utils;

    class MyEffectGodRays : MyEffectBase
    {
        readonly EffectHandle m_diffuseTexture;
        readonly EffectHandle m_depthTexture;
        readonly EffectHandle m_halfPixel;
        readonly EffectHandle m_frustumCorners;

        readonly EffectHandle m_view;
        readonly EffectHandle m_worldViewProjection;
        readonly EffectHandle m_density;
        readonly EffectHandle m_weight;
        readonly EffectHandle m_decay;
        readonly EffectHandle m_exposition;
        readonly EffectHandle m_lightPosition;
        readonly EffectHandle m_lightDirection;
        readonly EffectHandle m_cameraPos;

        public MyEffectGodRays()
            : base("Effects2\\Fullscreen\\MyEffectGodRays")
        {
            m_diffuseTexture = m_D3DEffect.GetParameter(null, "frameTex");
            m_depthTexture = m_D3DEffect.GetParameter(null, "depthTex");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");
            m_view = m_D3DEffect.GetParameter(null, "View");
            m_worldViewProjection = m_D3DEffect.GetParameter(null, "WorldViewProjection");
            m_density = m_D3DEffect.GetParameter(null, "Density");
            m_weight = m_D3DEffect.GetParameter(null, "Weight");
            m_decay = m_D3DEffect.GetParameter(null, "Decay");
            m_exposition = m_D3DEffect.GetParameter(null, "Exposition");
            m_lightPosition = m_D3DEffect.GetParameter(null, "LightPosition");
            m_lightDirection = m_D3DEffect.GetParameter(null, "LightDirection");
            m_cameraPos = m_D3DEffect.GetParameter(null, "CameraPos");
            m_frustumCorners = m_D3DEffect.GetParameter(null, "FrustumCorners");
        }

        public void SetDiffuseTexture(Texture dt)
        {
            m_D3DEffect.SetTexture(m_diffuseTexture, dt);

            SetHalfPixel(MyUtilsRender9.GetHalfPixel(dt.GetLevelDescription(0).Width, dt.GetLevelDescription(0).Height));
        }

        public void SetDepthTexture(Texture dt)
        {
            m_D3DEffect.SetTexture(m_depthTexture, dt);
        }

        void SetHalfPixel(Vector2 hf)
        {
            m_D3DEffect.SetValue(m_halfPixel, hf);
        }

        public void SetFrustumCorners(Vector3[] frustumCornersVS)
        {
            m_D3DEffect.SetValue(m_frustumCorners, frustumCornersVS);
        }

        public void SetView(Matrix matrix)
        {
            m_D3DEffect.SetValue(m_view, matrix);
        }

        public void SetWorldViewProjection(Matrix worldViewProjection)
        {
            m_D3DEffect.SetValue(m_worldViewProjection, worldViewProjection);
        }

        public void SetDensity(float density)
        {
            m_D3DEffect.SetValue(m_density, density);
        }

        public void SetWeight(float weight)
        {
            m_D3DEffect.SetValue(m_weight, weight);
        }

        public void SetDecay(float decay)
        {
            m_D3DEffect.SetValue(m_decay, decay);
        }

        public void SetExposition(float e)
        {
            m_D3DEffect.SetValue(m_exposition, e);
        }

        public void SetLightPosition(Vector3 pos)
        {
            m_D3DEffect.SetValue(m_lightPosition, pos);
        }

        public void SetLightDirection(Vector3 dir)
        {
            m_D3DEffect.SetValue(m_lightDirection, dir);
        }

        public void SetCameraPos(Vector3 pos)
        {
            m_D3DEffect.SetValue(m_cameraPos, pos);
        }
    }
}
