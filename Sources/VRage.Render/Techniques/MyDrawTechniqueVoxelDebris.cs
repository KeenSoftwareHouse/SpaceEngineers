using System;
using VRage;
using VRageMath;
using VRageRender.Effects;

namespace VRageRender.Techniques
{
    class MyDrawTechniqueVoxelDebris : MyDrawTechniqueBase
    {
        public override MyEffectBase PrepareAndBeginShader(MyRender.MyRenderSetup setup, MyLodTypeEnum lodType)
        {
            MyEffectVoxelsDebris shader = (MyEffectVoxelsDebris)MyRender.GetEffect(MyEffects.VoxelDebrisMRT);
            SetupBaseEffect(shader, setup, lodType);

            shader.SetTechnique(MyRenderConstants.RenderQualityProfile.VoxelsRenderTechnique);
            shader.Begin(0, SharpDX.Direct3D9.FX.None);
            return shader;
        }

        public override void SetupMaterial(MyEffectBase shader, MyRenderMeshMaterial material)
        {
            // nothing to do
        }

        public override void SetupEntity(MyEffectBase shader, MyRender.MyRenderElement renderElement)
        {
            MyEffectVoxelsDebris effectVoxelsDebris = (MyEffectVoxelsDebris)shader;

            MyRenderVoxelDebris voxelDebris = renderElement.RenderObject as MyRenderVoxelDebris;

            //  Random texture coord scale and per-object random texture coord offset
            effectVoxelsDebris.SetTextureCoordRandomPositionOffset(voxelDebris.TextureCoordOffset);
            effectVoxelsDebris.SetTextureCoordScale(voxelDebris.TextureCoordScale);
            effectVoxelsDebris.SetDiffuseTextureColorMultiplier(voxelDebris.TextureColorMultiplier);
            
            Matrix m = (Matrix)renderElement.WorldMatrixForDraw;
            effectVoxelsDebris.SetViewWorldScaleMatrix(m * MyRenderCamera.ViewMatrixAtZero);
            
            effectVoxelsDebris.SetWorldMatrix(ref m);
            effectVoxelsDebris.SetDiffuseColor(Vector3.One);
            effectVoxelsDebris.SetEmissivity(0);

            effectVoxelsDebris.UpdateVoxelTextures(voxelDebris.VoxelMaterialIndex);
        }

        public override void SetupVoxelMaterial(MyEffectVoxels shader, MyRenderVoxelBatch batch)
        {
            throw new InvalidOperationException();
        }
    }
}
