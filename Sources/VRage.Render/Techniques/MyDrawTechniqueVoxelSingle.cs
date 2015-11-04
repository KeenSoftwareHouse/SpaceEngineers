using System;
using VRage;
using VRageMath;
using VRageRender.Effects;

namespace VRageRender.Techniques
{
    class MyDrawTechniqueVoxelSingle : MyDrawTechniqueBase
    {
        MyLodTypeEnum m_currentLod;

        public override MyEffectBase PrepareAndBeginShader(MyRender.MyRenderSetup setup, MyLodTypeEnum lodType)
        {
            var shader = MyRender.GetEffect(MyEffects.VoxelsMRT) as MyEffectVoxels;
            SetupBaseEffect(shader, setup, lodType);

            {
                shader.Apply();
            }
            m_currentLod = lodType;
            shader.Begin(0, SharpDX.Direct3D9.FX.None);
            return shader;
        }

        public override void SetupVoxelMaterial(MyEffectVoxels effect, MyRenderVoxelBatch batch)
        {
            effect.UpdateVoxelTextures(MyRender.OverrideVoxelMaterial ?? batch.Material0);
        }

        public override void SetupEntity(MyEffectBase shader, MyRender.MyRenderElement renderElement)
        {
            SetupVoxelEntity(m_currentLod,shader, renderElement);
        }

        public static void SetupVoxelEntity(MyLodTypeEnum lod, MyEffectBase shader, MyRender.MyRenderElement renderElement)
        {
            MyEffectVoxels effectVoxels = shader as MyEffectVoxels;

            {
                MatrixD worldMatrixD = renderElement.WorldMatrix;
                worldMatrixD.Translation -= MyRenderCamera.Position;
                var worldMatrix = (Matrix)worldMatrixD;
                effectVoxels.SetWorldMatrix(ref worldMatrix);
            }

            var voxelCell = renderElement.RenderObject as MyRenderVoxelCell;
            if (voxelCell != null)
            {
                MyRenderVoxelCell.EffectArgs args;
                voxelCell.GetEffectArgs(out args);
                effectVoxels.VoxelVertex.SetArgs(ref args);
            }

            effectVoxels.SetDiffuseColor(Vector3.One);
            if (MyRenderSettings.DebugClipmapLodColor && renderElement.VoxelBatch.Lod < MyRenderVoxelCell.LOD_COLORS.Length)
            {
                effectVoxels.SetDiffuseColor(MyRenderVoxelCell.LOD_COLORS[renderElement.VoxelBatch.Lod].ToVector3());
            }
            effectVoxels.EnablePerVertexAmbient(
                MyRenderSettings.EnableVoxelAo,
                MyRenderSettings.VoxelAoMin,
                MyRenderSettings.VoxelAoMax,
                MyRenderSettings.VoxelAoOffset);

        }

        public override void SetupMaterial(MyEffectBase shader, MyRenderMeshMaterial material)
        {
            throw new InvalidOperationException();
        }

    }
}
