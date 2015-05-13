using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageRender.Effects;
using VRageRender.Graphics;
using VRageMath;
using System.Diagnostics;

namespace VRageRender.Techniques
{
    class MyDrawTechniqueCube : MyDrawTechniqueBaseDNS
    {
        public MyDrawTechniqueCube()
        {
            SolidRasterizerState = RasterizerState.CullCounterClockwise;
        }

        public override MyEffectBase PrepareAndBeginShader(MyRender.MyRenderSetup setup, MyLodTypeEnum lodType)
        {
            var shader = (MyEffectModelsDNS)MyRender.GetEffect(MyEffects.ModelDNS);
            SetupBaseEffect(shader, setup, lodType);

            shader.SetTechnique(MyRenderConstants.RenderQualityProfile.ModelsRenderTechnique);
            shader.Begin(0, SharpDX.Direct3D9.FX.None);
            return shader;
        }

               /*
        public override void SetupEntity(MyEffectBase shader, MyRender.MyRenderElement renderElement)
        {
            MyEffectCubeMRT effectCube = shader as MyEffectCubeMRT;
            var cube = ((MyRenderCube)renderElement.RenderObject);

            //cube.GetMaterial();
            effectCube.SetWorldMatrix(ref renderElement.WorldMatrixForDraw);
            effectCube.SetDiffuseColor(Vector3.One);
            effectCube.SetBasePosition(renderElement.WorldMatrix.Translation);
            effectCube.SetHighlightColor(cube.Highlight);
            //effectCube.SetHighlightColor(renderElement.WorldMatrix.Translation);
            effectCube.SetEmissivity(0);
        }

        public override void SetupVoxelMaterial(MyEffectVoxels shader, MyVoxelMaterialsEnum m0, MyVoxelMaterialsEnum? m1, MyVoxelMaterialsEnum? m2)
        {
            throw new InvalidOperationException();
        }        */
    }
}
