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
    class MyEffectVignetting : MyEffectBase
    {
        readonly EffectHandle m_inputTexture;
        readonly EffectHandle m_halfPixel;
        readonly EffectHandle m_vignettingPower;
        readonly EffectHandle m_vignettingEnabled;
        readonly EffectHandle m_vignettingDisabled;

        public MyEffectVignetting():
            base("Effects2\\Fullscreen\\MyEffectVignetting")
        {
            m_inputTexture       = m_D3DEffect.GetParameter(null, "InputTexture");
            m_halfPixel          = m_D3DEffect.GetParameter(null, "HalfPixel");
            m_vignettingPower    = m_D3DEffect.GetParameter(null, "VignettingPower");
            m_vignettingEnabled  = m_D3DEffect.GetTechnique("VignettingEnabled");
            m_vignettingDisabled = m_D3DEffect.GetTechnique("VignettingDisabled");
        }

        public void SetInputTexture(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_inputTexture, renderTarget2D);
        }

        public void EnableVignetting()
        {
            m_D3DEffect.Technique = m_vignettingEnabled;
        }

        public void DisableVignetting()
        {
            m_D3DEffect.Technique = m_vignettingDisabled;
        }

        public void SetHalfPixel(int screenSizeX, int screenSizeY)
        {
            Vector2 halfPixel = MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY);
            m_D3DEffect.SetValue(m_halfPixel, halfPixel);
        }

        internal void SetVignettingPower(float vignettingPower)
        {
            m_D3DEffect.SetValue(m_vignettingPower, vignettingPower);
        }
    }
}
