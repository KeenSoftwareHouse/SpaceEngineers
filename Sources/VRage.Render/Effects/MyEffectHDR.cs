using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    class MyEffectHDR : MyEffectHDRBase
    {
        readonly EffectHandle m_bloomTexture;

        public MyEffectHDR()
            : base("Effects2\\HDR\\MyEffectHDR")
        {
            m_bloomTexture = m_D3DEffect.GetParameter(null, "BloomTexture");
        }

        public void SetBloomTexture(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_bloomTexture, renderTarget2D);
        }
    }
}
