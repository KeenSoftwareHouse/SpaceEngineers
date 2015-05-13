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
    class MyEffectColorMapping : MyEffectBase
    {
        readonly EffectHandle m_inputTexture;
        readonly EffectHandle m_halfPixel;
        readonly EffectHandle m_techniqueEnabled;
        readonly EffectHandle m_techniqueDisabled;

        public MyEffectColorMapping() :
            base("Effects2\\Fullscreen\\MyEffectColorMapping")
        {
            m_inputTexture      = m_D3DEffect.GetParameter(null, "InputTexture");
            m_halfPixel         = m_D3DEffect.GetParameter(null, "HalfPixel");
            m_techniqueEnabled  = m_D3DEffect.GetTechnique("ColorMappingEnabled");
            m_techniqueDisabled = m_D3DEffect.GetTechnique("ColorMappingDisabled");
        }

        public void SetInputTexture(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_inputTexture, renderTarget2D);
        }

        public void Enable()
        {
            m_D3DEffect.Technique = m_techniqueEnabled;
        }

        public void Disable()
        {
            m_D3DEffect.Technique = m_techniqueDisabled;
        }

        public void SetHalfPixel(int screenSizeX, int screenSizeY)
        {
            Vector2 halfPixel = MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY);
            m_D3DEffect.SetValue(m_halfPixel, halfPixel);
        }
    }
}
