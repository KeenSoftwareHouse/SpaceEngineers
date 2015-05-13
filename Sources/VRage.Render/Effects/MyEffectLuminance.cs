using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;

    class MyEffectLuminance: MyEffectHDRBase
    {
        readonly EffectHandle m_DT;
        readonly EffectHandle m_Tau;
        readonly EffectHandle m_MipLevel;
        readonly EffectHandle m_SourceTexture2;
        readonly EffectHandle m_luminance;
        readonly EffectHandle m_calcAdaptedLuminance;
        readonly EffectHandle m_luminanceMipmap;

        public MyEffectLuminance()
            : base("Effects2\\HDR\\MyEffectLuminance")
        {
            m_DT = m_D3DEffect.GetParameter(null, "DT");
            m_Tau = m_D3DEffect.GetParameter(null, "Tau");
            m_MipLevel = m_D3DEffect.GetParameter(null, "MipLevel");
            m_SourceTexture2 = m_D3DEffect.GetParameter(null, "SourceTexture2");
            m_luminance = m_D3DEffect.GetTechnique("Luminance");
            m_calcAdaptedLuminance = m_D3DEffect.GetTechnique("CalcAdaptedLuminance");
            m_luminanceMipmap = m_D3DEffect.GetTechnique("LuminanceMipmap");
        }

        public void SetDT(float dt)
        {
            m_D3DEffect.SetValue(m_halfPixel, dt);
        }

        public void SetMipLevel(int exponent)
        {
            m_D3DEffect.SetValue(m_MipLevel, (float)exponent);
        }

        public void SetTau(float tau)
        {
            m_D3DEffect.SetValue(m_Tau, tau);
        }

        public void SetSourceTexture2(Texture source)
        {
            m_D3DEffect.SetTexture(m_SourceTexture2, source);
        }

        public void SetTechniqueLuminance()
        {
            m_D3DEffect.Technique = m_luminance;
        }

        public void SetTechniqueAdaptedLuminance()
        {
            m_D3DEffect.Technique = m_calcAdaptedLuminance;
        }

        internal void SetTechniqueLuminanceMipmap()
        {
            m_D3DEffect.Technique = m_luminanceMipmap;
        }
    }
}
