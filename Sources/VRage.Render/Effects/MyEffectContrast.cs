using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;

    class MyEffectContrast : MyEffectBase
    {
        readonly EffectHandle m_diffuseTexture;
        readonly EffectHandle m_contrast;
        readonly EffectHandle m_saturation;
        readonly EffectHandle m_hue;
        readonly EffectHandle m_halfPixel;

        public MyEffectContrast()
            : base("Effects2\\Fullscreen\\MyEffectContrast")
        {
            m_diffuseTexture = m_D3DEffect.GetParameter(null, "DiffuseTexture");
            m_contrast = m_D3DEffect.GetParameter(null, "Contrast");
            m_hue = m_D3DEffect.GetParameter(null, "Hue");
            m_saturation = m_D3DEffect.GetParameter(null, "Saturation");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");
        }

        public void SetDiffuseTexture(Texture dt)
        {
            m_D3DEffect.SetTexture(m_diffuseTexture, dt);
        }

        public void SetHalfPixel(Vector2 hf)
        {
            m_D3DEffect.SetValue(m_halfPixel, hf);
        }

        public void SetContrast(float contrast)
        {
            m_D3DEffect.SetValue(m_contrast, contrast);
        }

        public void SetSaturation(float saturation)
        {
            m_D3DEffect.SetValue(m_saturation, saturation);
        }

        public void SetHue(float hue)
        {
            m_D3DEffect.SetValue(m_hue, hue);
        }
    }
}
