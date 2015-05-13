using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageRender.Effects;
using VRageRender.Graphics;
using VRageMath;

namespace VRageRender.Techniques
{
    class MyDrawTechniqueSkinned : MyDrawTechniqueBaseDNS
    {
        static Matrix[] m_bonesBuffer = new Matrix[MyRenderConstants.MAX_SHADER_BONES];

        public override MyEffectBase PrepareAndBeginShader(MyRender.MyRenderSetup setup, MyLodTypeEnum lodType)
        {
            var shader = (MyEffectModelsDNS)MyRender.GetEffect(MyEffects.ModelDNS);
            SetupBaseEffect(shader, setup, lodType);

            shader.SetTechnique(MyRenderConstants.RenderQualityProfile.ModelsSkinnedTechnique);
            //shader.SetTechnique(MyEffectModelsDNS.MyEffectModelsDNSTechniqueEnum.HighSkinned);
            //shader.SetTechnique(MyEffectModelsDNS.MyEffectModelsDNSTechniqueEnum.High);
            shader.Begin(0, SharpDX.Direct3D9.FX.None);
            return shader;
        }

        public sealed override void SetupEntity(MyEffectBase shader, MyRender.MyRenderElement renderElement)
        {
            base.SetupEntity(shader, renderElement);

            MyEffectModelsDNS effectDNS = shader as MyEffectModelsDNS;

            MyRenderCharacter character = renderElement.RenderObject as MyRenderCharacter;

            var bonesUsed = renderElement.BonesUsed;

            if (character.SkinMatrices != null)
            {
                if (bonesUsed == null)
                    for (int i = 0; i < Math.Min(character.SkinMatrices.Length, MyRenderConstants.MAX_SHADER_BONES); i++)
                        m_bonesBuffer[i] = character.SkinMatrices[i];
                else
                    for (int i = 0; i < bonesUsed.Length; i++)
                        m_bonesBuffer[i] = character.SkinMatrices[bonesUsed[i]];
                    
                 effectDNS.SetBones(m_bonesBuffer);
            }
        }
    }
}
