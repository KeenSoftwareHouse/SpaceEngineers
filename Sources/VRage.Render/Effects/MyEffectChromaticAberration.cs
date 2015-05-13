using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D9;
using VRageMath;
using VRageRender.Utils;

namespace VRageRender.Effects
{
    class MyEffectChromaticAberration : MyEffectBase
    {
        readonly EffectHandle m_inputTexture;
        readonly EffectHandle m_halfPixel;
        readonly EffectHandle m_aspectRation;
        readonly EffectHandle m_distortionLens;
        readonly EffectHandle m_distortionCubic;
        readonly EffectHandle m_distortionWeights;
        readonly EffectHandle m_techniqueEnabled;
        readonly EffectHandle m_techniqueDisabled;

        public MyEffectChromaticAberration() :
            base("Effects2\\Fullscreen\\MyEffectChromaticAberration")
        {
            m_inputTexture      = m_D3DEffect.GetParameter(null, "InputTexture");
            m_halfPixel         = m_D3DEffect.GetParameter(null, "HalfPixel");
            m_aspectRation      = m_D3DEffect.GetParameter(null, "AspectRatio");
            m_distortionLens    = m_D3DEffect.GetParameter(null, "DistortionLens");
            m_distortionCubic   = m_D3DEffect.GetParameter(null, "DistortionCubic");
            m_distortionWeights = m_D3DEffect.GetParameter(null, "DistortionWeights");
            m_techniqueEnabled  = m_D3DEffect.GetTechnique("TechniqueEnabled");
            m_techniqueDisabled = m_D3DEffect.GetTechnique("TechniqueDisabled");
        }

        public void SetInputTexture(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_inputTexture, renderTarget2D);
        }

        public void SetHalfPixel(int screenSizeX, int screenSizeY)
        {
            Vector2 halfPixel = MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY);
            m_D3DEffect.SetValue(m_halfPixel, halfPixel);
        }

        public void SetDistortionLens(float val)
        {
            m_D3DEffect.SetValue(m_distortionLens, val);
        }

        public void SetDistortionCubic(float val)
        {
            m_D3DEffect.SetValue(m_distortionCubic, val);
        }

        public void SetDistortionWeights(ref Vector3 val)
        {
            m_D3DEffect.SetValue(m_distortionWeights, val);
        }

        public void SetAspectRatio(float val)
        {
            m_D3DEffect.SetValue(m_aspectRation, val);
        }

        public void Enable()
        {
            m_D3DEffect.Technique = m_techniqueEnabled;
        }

        public void Disable()
        {
            m_D3DEffect.Technique = m_techniqueDisabled;
        }
    }
}
