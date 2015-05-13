using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Utils;

    class MyEffectOcclusionQueryDraw : MyEffectBase
    {
        public enum Technique
        {
            DepthTestEnabled,
            DepthTestDisabled,
            DepthTestEnabledNonMRT,
            DepthTestDisabledNonMRT,
        }

        readonly EffectHandle m_depthTestTechnique;
        readonly EffectHandle m_noDepthTestTechnique;
        readonly EffectHandle m_depthTestTechniqueNonMRT;
        readonly EffectHandle m_noDepthTestTechniqueNonMRT;

        readonly EffectHandle m_viewMatrix;
        readonly EffectHandle m_worldMatrix;
        readonly EffectHandle m_projectionMatrix;

        readonly EffectHandle m_depthRT;
        readonly EffectHandle m_halfPixel;
        readonly EffectHandle m_scale;

        public MyEffectOcclusionQueryDraw()
            : base("Effects2\\Models\\MyEffectOcclusionQueryDraw")
        {
            m_depthTestTechnique = m_D3DEffect.GetTechnique("EnableDepthTest");
            m_noDepthTestTechnique = m_D3DEffect.GetTechnique("DisableDepthTest");
            m_depthTestTechniqueNonMRT = m_D3DEffect.GetTechnique("EnableDepthTestNonMRT");
            m_noDepthTestTechniqueNonMRT = m_D3DEffect.GetTechnique("DisableDepthTestNonMRT");

            m_viewMatrix = m_D3DEffect.GetParameter(null, "ViewMatrix");
            m_worldMatrix = m_D3DEffect.GetParameter(null, "WorldMatrix");
            m_projectionMatrix = m_D3DEffect.GetParameter(null, "ProjectionMatrix");

            m_depthRT = m_D3DEffect.GetParameter(null, "DepthsRT");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");
            m_scale = m_D3DEffect.GetParameter(null, "Scale");
        }

        public void SetWorldMatrix(Matrix worldMatrix)
        {
            m_D3DEffect.SetValue(m_worldMatrix, worldMatrix);
        }

        public void SetViewMatrix(Matrix viewMatrix)
        {
            m_D3DEffect.SetValue(m_viewMatrix, viewMatrix);
        }

        public void SetProjectionMatrix(Matrix projectionMatrix)
        {
            m_D3DEffect.SetValue(m_projectionMatrix, projectionMatrix);
        }

        public Effect GetEffect()
        {
            return m_D3DEffect;
        }

        public void SetDepthRT(Texture depthRT)
        {
            m_D3DEffect.SetTexture(m_depthRT, depthRT);
            m_D3DEffect.SetValue(m_halfPixel, MyUtilsRender9.GetHalfPixel(depthRT.GetLevelDescription(0).Width, depthRT.GetLevelDescription(0).Height));
        }

        public void SetScale(Vector2 scale)
        {
            m_D3DEffect.SetValue(m_scale, scale);
        }

        public void SetTechnique(Technique technique)
        {
            switch (technique)
            {
                case Technique.DepthTestEnabled:
                    m_D3DEffect.Technique = m_depthTestTechnique;
                    break;

                case Technique.DepthTestDisabled:
                    m_D3DEffect.Technique = m_noDepthTestTechnique;
                    break;

                case Technique.DepthTestEnabledNonMRT:
                    m_D3DEffect.Technique = m_depthTestTechniqueNonMRT;
                    break;

                case Technique.DepthTestDisabledNonMRT:
                    m_D3DEffect.Technique = m_noDepthTestTechniqueNonMRT;
                    break;
            }
        }
    }
}