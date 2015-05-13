using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Utils;

    class MyEffectDownsampleDepthForSSAO : MyEffectBase
    {
        readonly EffectHandle m_sourceDepthsRT;
        readonly EffectHandle m_halfPixel;

        public MyEffectDownsampleDepthForSSAO()
            : base("Effects2\\SSAO\\MyEffectDownsampleDepthForSSAO")
        {
            m_sourceDepthsRT = m_D3DEffect.GetParameter(null, "SourceDepthsRT");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");
        }

        public void SetSourceDepthsRT(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_sourceDepthsRT, renderTarget2D);
        }

        //  Set half-pixel and calculates 'quarter pixel' immediatelly
        public void SetHalfPixel(int screenSizeX, int screenSizeY)
        {
            Vector2 halfPixel = MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY);
            m_D3DEffect.SetValue(m_halfPixel, halfPixel);
        }    
    }
}
