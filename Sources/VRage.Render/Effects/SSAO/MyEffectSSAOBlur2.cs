using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Utils;

    class MyEffectSSAOBlur2 : MyEffectBase
    {
        readonly EffectHandle m_depthsRT;
        readonly EffectHandle m_ssaoRT;
        readonly EffectHandle m_halfPixel;
        readonly EffectHandle m_SSAOhalfPixel;
        readonly EffectHandle m_blurDirection;

        public MyEffectSSAOBlur2()
            : base("Effects2\\SSAO\\MyEffectSSAOBlur2")
        {
            m_depthsRT = m_D3DEffect.GetParameter(null, "DepthsRT");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");
            m_SSAOhalfPixel = m_D3DEffect.GetParameter(null, "SSAOHalfPixel");
            m_ssaoRT = m_D3DEffect.GetParameter(null, "SsaoRT");
            m_blurDirection = m_D3DEffect.GetParameter(null, "BlurDirection");
        }

        public void SetDepthsRT(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_depthsRT, renderTarget2D);
        }

        public void SetSsaoRT(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_ssaoRT, renderTarget2D);
        }

        public void SetHalfPixel(int screenSizeX, int screenSizeY)
        {
            m_D3DEffect.SetValue(m_halfPixel, MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY));
        }

        public void SetSSAOHalfPixel(int screenSizeX, int screenSizeY)
        {
            m_D3DEffect.SetValue(m_SSAOhalfPixel, MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY));
        }


        public void SetBlurDirection(Vector2 blurDirection)
        {
            m_D3DEffect.SetValue(m_blurDirection, blurDirection);
        }
    }
}
