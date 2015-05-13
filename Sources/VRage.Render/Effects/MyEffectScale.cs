using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;

    class MyEffectScale: MyEffectHDRBase
    {
        public enum Technique
        {
            HWScale,
            HWScalePrefabPreviews,
            Downscale4,
            Downscale8,
        }

        readonly EffectHandle m_sourceDimensions;
        readonly EffectHandle m_scale;
        //readonly EffectHandle m_downscale;
        //readonly EffectHandle m_downscaleLuminance;
        readonly EffectHandle m_HWScale;
        readonly EffectHandle m_HWScalePrefabPreviews;
        readonly EffectHandle m_downscale8;
        readonly EffectHandle m_downscale4;
        
        public MyEffectScale()
            : base("Effects2\\HDR\\MyEffectScale")
        {
            m_sourceDimensions = m_D3DEffect.GetParameter(null, "SourceDimensions");
            m_scale = m_D3DEffect.GetParameter(null, "Scale");
            //m_downscale = m_xnaEffect.GetTechnique("Downscale"];
            //m_downscaleLuminance = m_xnaEffect.Technique = m_xnaEffect.GetTechnique("DownscaleLuminance"];
            m_HWScale = m_D3DEffect.Technique = m_D3DEffect.GetTechnique("HWScale");
            m_HWScalePrefabPreviews = m_D3DEffect.Technique = m_D3DEffect.GetTechnique("HWScalePrefabPreviews");
            m_downscale8 = m_D3DEffect.Technique = m_D3DEffect.GetTechnique("Downscale8");
            m_downscale4 = m_D3DEffect.Technique = m_D3DEffect.GetTechnique("Downscale4");
        }

        public void SetSourceDimensions(int width, int height)
        {
            m_D3DEffect.SetValue(m_sourceDimensions, new Vector2(width, height));
        }

        public void SetScale(Vector2 scale)
        {
            m_D3DEffect.SetValue(m_scale, scale);
        }

        public void SetTechnique(Technique technique)
        {
            switch (technique)
            {
                case Technique.HWScale:
                    m_D3DEffect.Technique = m_HWScale;
                    break;

                case Technique.HWScalePrefabPreviews:
                    m_D3DEffect.Technique = m_HWScalePrefabPreviews;
                    break;

                case Technique.Downscale4:
                    m_D3DEffect.Technique = m_downscale4;
                    break;

                case Technique.Downscale8:
                    m_D3DEffect.Technique = m_downscale8;
                    break;
            }
        }
    }
}
