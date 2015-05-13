using System;
using SharpDX.Direct3D9;
using VRage;

namespace VRageRender
{

    //  IMPORTANT: Order of this enum values is important. It has impact on sorting batches.
    enum MyRenderVoxelBatchType : byte
    {
        MULTI_MATERIAL_CLEAR = 0,
        SINGLE_MATERIAL = 1,
        MULTI_MATERIAL = 2
    }

    class MyRenderVoxelBatch : IComparable
    {
        public MyRenderVoxelBatchType Type;
        public byte Material0;
        public byte? Material1;
        public byte? Material2;

        public int Lod;

        //  For sorting batches by type and material
        public int SortOrder;

        // Unique id of (multi)material
        public int MaterialId;

        //  Index buffer (may be null if type is MULTI_MATERIAL_CLEAR)
        public int IndexCount;
        public IndexBuffer IndexBuffer;

        //  Vertex buffer
        public int VertexCount;
        public VertexBuffer VertexBuffer;

        public void UpdateSortOrder()
        {
            int maxMat = MyRenderVoxelMaterials.GetMaterialsCount() + 1;
            int matCount = MyRenderVoxelMaterials.GetMaterialsCount() + 2;

            int mats0 = (int)Material0;
            int mats1 = (int)(Material1.HasValue ? (int)Material1.Value : maxMat);
            int mats2 = (int)(Material2.HasValue ? (int)Material2.Value : maxMat);

            //  Important is type and material/texture. Order of type is defined by enum values
            SortOrder = ((int)Type * matCount * matCount * matCount) + mats2 * matCount * matCount + mats1 * matCount + mats0;
            MaterialId = mats2 * matCount * matCount + mats1 * matCount + mats0;
        }

        //  For sorting batches by type and material
        //  We want first multi-clear, then single-material and multi-material as last
        public int CompareTo(object compareToObject)
        {
            MyRenderVoxelBatch compareToBatch = (MyRenderVoxelBatch)compareToObject;
            return this.SortOrder.CompareTo(compareToBatch.SortOrder);
        }

        public void Dispose()
        {
            if (VertexBuffer != null)
            {
                VertexBuffer.Dispose();
                VertexBuffer = null;
                VertexCount = 0;
            }
            if (IndexBuffer != null)
            {
                IndexBuffer.Dispose();
                IndexBuffer = null;
                IndexCount = 0;
            }
        }
    }
}
