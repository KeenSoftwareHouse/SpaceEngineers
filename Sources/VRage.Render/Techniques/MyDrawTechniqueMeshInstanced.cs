using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageRender.Effects;
using VRageRender.Graphics;
using VRageMath;

namespace VRageRender.Techniques
{
    class MyDrawTechniqueMeshInstanced : MyDrawTechniqueBaseDNS
    {
        public override MyEffectBase PrepareAndBeginShader(MyRender.MyRenderSetup setup, MyLodTypeEnum lodType)
        {
            var shader = (MyEffectModelsDNS)MyRender.GetEffect(MyEffects.ModelDNS);
            SetupBaseEffect(shader, setup, lodType);

            shader.SetTechnique(MyRenderConstants.RenderQualityProfile.ModelsInstancedTechnique);
            //shader.SetTechnique(MyEffectModelsDNS.MyEffectModelsDNSTechniqueEnum.HighInstanced);
            shader.Begin(0, SharpDX.Direct3D9.FX.None);
            return shader;
        }

        public override void SetupEntity(MyEffectBase shader, MyRender.MyRenderElement renderElement)
        {
            MyEffectModelsDNS effectDNS = shader as MyEffectModelsDNS;

            // This is required, it's position of whole group
            effectDNS.SetWorldMatrix((Matrix)renderElement.WorldMatrixForDraw);

            // This should be in instance buffer when required
            effectDNS.SetDiffuseColor(renderElement.Color);
            effectDNS.Dithering = renderElement.Dithering;
            effectDNS.SetColorMaskHSV(renderElement.ColorMaskHSV);
            effectDNS.SetEmissivity(0);
        }
    }
}
