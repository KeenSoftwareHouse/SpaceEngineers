#region Using Statements

using System.Collections.Generic;
using VRage;
using VRage.Import;
using VRageMath;


#endregion

namespace VRageRender
{
    class MyRenderVoxelDebris : MyRenderEntity
    {
        public float TextureCoordOffset;
        public float TextureCoordScale;
        public float TextureColorMultiplier;
        public byte VoxelMaterialIndex;

        public MyRenderVoxelDebris(uint id, string debugName, string model, MatrixD worldMatrix, float textureCoordOffset, float textureCoordScale, float textureColorMultiplier, byte voxelMaterialIndex)
            : base(id, debugName, model, worldMatrix, MyMeshDrawTechnique.VOXELS_DEBRIS, RenderFlags.Visible)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(model));

            TextureCoordOffset = textureCoordOffset;
            TextureCoordScale = textureCoordScale;
            TextureColorMultiplier = textureColorMultiplier;
            VoxelMaterialIndex = voxelMaterialIndex;
        }


        public override void GetRenderElements(MyLodTypeEnum lodTypeEnum, List<MyRender.MyRenderElement> elements, List<MyRender.MyRenderElement> transparentElements)
        {
            MyPerformanceCounter.PerCameraDrawWrite.EntitiesRendered++;

            //   GetRenderProfiler().StartNextBlock("Collect render elements");
            CollectRenderElements(elements, transparentElements, m_lods[0].Model, m_lods[0].MeshMaterials, 0);
            //   GetRenderProfiler().EndProfilingBlock();
        }

       
    }
}
