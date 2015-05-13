using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Utils;
    using System;

    class MyEffectScreenshot: MyEffectBase
    {
        public enum ScreenshotTechniqueEnum
        {
            Default,
            Color,
            HDR,
            Alpha,
            DepthToAlpha,
            LinearScale,
            ColorizeTexture,
        }

        readonly EffectHandle m_source;
        readonly EffectHandle m_halfPixel;
        readonly EffectHandle m_scale;

        readonly EffectHandle m_defaultTechnique;
        readonly EffectHandle m_colorTechnique;
        readonly EffectHandle m_alphaTechnique;
        readonly EffectHandle m_hdrTechnique;
        readonly EffectHandle m_depthToAlpha;
        readonly EffectHandle m_linearTechnique;
        readonly EffectHandle m_colorizeTextureTechnique;
        readonly EffectHandle m_colorMaskHSV;

        public MyEffectScreenshot()
            : base("Effects2\\Fullscreen\\MyEffectScreenshot")
        {
            m_source = m_D3DEffect.GetParameter(null, "SourceTexture");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");
            m_scale = m_D3DEffect.GetParameter(null, "Scale");
            m_colorMaskHSV = m_D3DEffect.GetParameter(null, "ColorMaskHSV");

            m_defaultTechnique = m_D3DEffect.GetTechnique("BasicTechnique");
            m_colorTechnique = m_D3DEffect.GetTechnique("ColorTechnique");
            m_alphaTechnique = m_D3DEffect.GetTechnique("AlphaTechnique");
            m_hdrTechnique = m_D3DEffect.GetTechnique("HDRTechnique");
            m_depthToAlpha = m_D3DEffect.GetTechnique("DepthToAlphaTechnique");
            m_linearTechnique = m_D3DEffect.GetTechnique("LinearTechnique");
            m_colorizeTextureTechnique = m_D3DEffect.GetTechnique("ColorizeTextureTechnique");    
        }

        public void SetSourceTexture(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_source, renderTarget2D);
            m_D3DEffect.SetValue(m_halfPixel, MyUtilsRender9.GetHalfPixel(renderTarget2D.GetLevelDescription(0).Width, renderTarget2D.GetLevelDescription(0).Height));
        }

        public void SetScale(Vector2 scale)
        {
            m_D3DEffect.SetValue(m_scale, scale);
        }
        public void SetColorMaskHSV(Vector3 HSV)
        {
            m_D3DEffect.SetValue(m_colorMaskHSV, HSV);
        }

        public void SetTechnique(ScreenshotTechniqueEnum technique)
        {
            switch (technique)
            {
                case ScreenshotTechniqueEnum.Default:
                    m_D3DEffect.Technique = m_defaultTechnique;
                    break;

                case ScreenshotTechniqueEnum.Color:
                    m_D3DEffect.Technique = m_colorTechnique;
                    break;

                case ScreenshotTechniqueEnum.HDR:
                    m_D3DEffect.Technique = m_hdrTechnique;
                    break;

                case ScreenshotTechniqueEnum.Alpha:
                    m_D3DEffect.Technique = m_alphaTechnique;
                    break;

                case ScreenshotTechniqueEnum.DepthToAlpha:
                    m_D3DEffect.Technique = m_depthToAlpha;
                    break;

                case ScreenshotTechniqueEnum.LinearScale:
                    m_D3DEffect.Technique = m_linearTechnique;
                    break;
                case ScreenshotTechniqueEnum.ColorizeTexture:
                    m_D3DEffect.Technique = m_colorizeTextureTechnique;
                    break;

                default:
                    throw new InvalidBranchException();
            }
        }
        
    }
}
