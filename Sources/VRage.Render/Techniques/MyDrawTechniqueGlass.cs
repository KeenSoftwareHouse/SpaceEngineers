using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageRender.Effects;
using VRageRender.Graphics;

namespace VRageRender.Techniques
{
    class MyDrawTechniqueGlass : MyDrawTechniqueBaseDNS
    {
        public MyDrawTechniqueGlass()
        {
        }

        public override MyEffectBase PrepareAndBeginShader(MyRender.MyRenderSetup setup, MyLodTypeEnum lodType)
        {
            var shader = (MyEffectModelsDNS)MyRender.GetEffect(MyEffects.ModelDNS);
            SetupBaseEffect(shader, setup, lodType);

            shader.SetTechnique(MyRenderConstants.RenderQualityProfile.ModelsRenderTechnique);
            shader.Begin(0, SharpDX.Direct3D9.FX.None);
            return shader;
        }

        public override void SetupEntity(MyEffectBase shader, MyRender.MyRenderElement renderElement)
        {
            base.SetupEntity(shader, renderElement);

            if (renderElement.Dithering > 0)
            {
            //    renderElement.Dithering = 0;
            }
        }
    }
}
