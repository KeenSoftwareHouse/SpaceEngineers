using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Utils;

    class MyEffectAntiAlias : MyEffectBase
    {
        readonly EffectHandle m_diffuse;
        readonly EffectHandle m_fullPixel;
        readonly EffectHandle m_halfPixel;
        readonly EffectHandle m_applyFxaa;
        readonly EffectHandle m_noAntialiasing;

        public MyEffectAntiAlias()
            : base("Effects2\\Fullscreen\\MyEffectAntiAlias")
        {
            m_diffuse = m_D3DEffect.GetParameter(null, "DiffuseTexture");
            m_fullPixel = m_D3DEffect.GetParameter(null, "FullPixel");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");
            m_applyFxaa = m_D3DEffect.GetTechnique("ApplyFxaa");
            m_noAntialiasing = m_D3DEffect.GetTechnique("NoAntialiasing");
        }

        public void SetDiffuseTexture(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_diffuse, renderTarget2D);
        }

        public void SetHalfPixel(int screenSizeX, int screenSizeY)
        {
            Vector2 halfPixel = MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY);
            m_D3DEffect.SetValue(m_halfPixel, halfPixel);
            m_D3DEffect.SetValue(m_fullPixel, 2.0f * halfPixel);
        }

        public void ApplyFxaa()
        {
            m_D3DEffect.Technique = m_applyFxaa;
        }

        public void DisableAntialiasing()
        {
            m_D3DEffect.Technique = m_noAntialiasing;
        }
    }
}
