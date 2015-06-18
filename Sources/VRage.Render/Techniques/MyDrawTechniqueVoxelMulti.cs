using System;
using VRage;
using VRageRender.Effects;

namespace VRageRender.Techniques
{
    class MyDrawTechniqueVoxelMulti : MyDrawTechniqueBase
    {
        MyLodTypeEnum m_currentLod;
        public override MyEffectBase PrepareAndBeginShader(MyRender.MyRenderSetup setup, MyLodTypeEnum lodType)
        {
            var shader = MyRender.GetEffect(MyEffects.VoxelsMRT) as MyEffectVoxels;
            SetupBaseEffect(shader, setup, lodType);

            if (lodType == MyLodTypeEnum.LOD_BACKGROUND)
            {
                shader.ApplyMultimaterialFar(MyRenderConstants.RenderQualityProfile.VoxelsRenderTechnique);
            }
            else
            {
                shader.ApplyMultimaterial();
            }
            shader.Begin(0, SharpDX.Direct3D9.FX.None);
            m_currentLod = lodType;
            return shader;
        }

        public override void SetupVoxelMaterial(MyEffectVoxels effect, MyRenderVoxelBatch batch)
        {
            effect.UpdateVoxelMultiTextures(MyRender.OverrideVoxelMaterial ?? batch.Material0, MyRender.OverrideVoxelMaterial ?? batch.Material1, MyRender.OverrideVoxelMaterial ?? batch.Material2);
        }

        public override void SetupMaterial(MyEffectBase shader, MyRenderMeshMaterial material)
        {
            throw new InvalidOperationException();
        }

        public override void SetupEntity(MyEffectBase shader, MyRender.MyRenderElement renderElement)
        {
            MyDrawTechniqueVoxelSingle.SetupVoxelEntity(m_currentLod,shader, renderElement);
        }
    }
}
