using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Utils;

    abstract class MyEffectHDRBase : MyEffectBase
    {
        readonly protected EffectHandle m_sourceMod;
        readonly protected EffectHandle m_sourceDiv;
        readonly protected EffectHandle m_lumSource;
        readonly protected EffectHandle m_halfPixel;
        //readonly protected EffectHandle m_middleGrey;
        readonly protected EffectHandle m_Exposure;

        public MyEffectHDRBase(string asset)
            : base(asset)
        {
            m_sourceMod = m_D3DEffect.GetParameter(null, "SourceTexture");
            m_sourceDiv = m_D3DEffect.GetParameter(null, "SourceTextureDiv");
            m_lumSource = m_D3DEffect.GetParameter(null, "LumTexture");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");
            //m_middleGrey = m_xnaEffect.GetParameter(null, "MiddleGrey");
            m_Exposure = m_D3DEffect.GetParameter(null, "Exposure");
        }

        public void SetSourceTextureMod(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_sourceMod, renderTarget2D);
        }

        public void SetSourceTextureDiv(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_sourceDiv, renderTarget2D);
        }

        public void SetLumTexture(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_lumSource, renderTarget2D);
        }

        public void SetExposure(float exposure)
        {
            m_D3DEffect.SetValue(m_Exposure, exposure);
        }

        public void SetHalfPixel(int screenSizeX, int screenSizeY)
        {
            m_D3DEffect.SetValue(m_halfPixel, MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY));
        }

        public void SetMiddleGrey(float middleGrey)
        {
            //m_D3DEffect.SetValue(m_middleGrey, middleGrey);
        }
    }
}
