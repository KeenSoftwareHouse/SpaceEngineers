using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;

    class MyEffectThreshold : MyEffectHDRBase
    {
        readonly EffectHandle m_threshold;
        readonly EffectHandle m_bloomIntensity;
        readonly EffectHandle m_bloomIntensityBackground;

        public MyEffectThreshold()
            : base("Effects2\\HDR\\MyEffectThreshold")
        {
            m_threshold = m_D3DEffect.GetParameter(null, "Threshold");
            m_bloomIntensity = m_D3DEffect.GetParameter(null, "BloomIntensity");
            m_bloomIntensityBackground = m_D3DEffect.GetParameter(null, "BloomIntensityBackground");
        }

        public void SetThreshold(float threshold)
        {
            m_D3DEffect.SetValue(m_threshold,threshold);
        }

        public void SetBloomIntensity(float bloomIntensity)
        {
            m_D3DEffect.SetValue(m_bloomIntensity,bloomIntensity);
        }

        public void SetBloomIntensityBackground(float bloomIntensityBackground)
        {
            m_D3DEffect.SetValue(m_bloomIntensityBackground,bloomIntensityBackground);
        }
    }
}
