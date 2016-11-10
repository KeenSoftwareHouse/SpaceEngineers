using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public static class MeshIdExtensions
    {
        internal static int GetIndexCount(this MeshId meshId)
        {
            int indexCount = 0;
            if (MyMeshes.IsMergedVoxelMesh(meshId))
                indexCount = MyMeshes.GetMergedLodMesh(meshId, 0).Info.IndicesNum;
            else
                indexCount = MyMeshes.GetLodMesh(meshId, 0).Info.IndicesNum;

            return indexCount;
        }

        internal static BoundingBox? GetBoundingBox(this MeshId meshId, int lod)
        {
            return MyMeshes.IsMergedVoxelMesh(meshId) ? MyMeshes.GetMergedLodMesh(meshId, 0).Info.BoundingBox : MyMeshes.GetLodMesh(meshId, lod).Info.BoundingBox;
        }

        internal static void AssignLodMeshToProxy(this MeshId meshId, MyRenderableProxy proxy)
        {
            Debug.Assert(proxy != null, "Proxy cannot be null!");
            if (MyMeshes.IsMergedVoxelMesh(meshId))
                proxy.MergedMesh = MyMeshes.GetMergedLodMesh(meshId, 0);
            else
                proxy.Mesh = MyMeshes.GetLodMesh(meshId, 0);
        }

        internal static MyMeshBuffers GetMeshBuffers(this MeshId meshId)
        {
            MyMeshBuffers buffers;
            if (MyMeshes.IsMergedVoxelMesh(meshId))
                buffers = MyMeshes.GetMergedLodMesh(meshId, 0).Buffers;
            else
                buffers = MyMeshes.GetLodMesh(meshId, 0).Buffers;

            return buffers;
        }

        internal static bool ShouldHaveFoliage(this MeshId meshId)
        {
            int partsNum;
            if (MyMeshes.IsMergedVoxelMesh(meshId))
                partsNum = MyMeshes.GetMergedLodMesh(meshId, 0).Info.PartsNum;
            else
                partsNum = MyMeshes.GetLodMesh(meshId, 0).Info.PartsNum;

            bool shouldHaveFoliage = false;
            for (int partIndex = 0; partIndex < partsNum; ++partIndex)
            {
                var triple = MyMeshes.GetVoxelPart(meshId, partIndex).Info.MaterialTriple;

                if (triple.I0 >= 0 && MyVoxelMaterials1.Table[triple.I0].HasFoliage)
                {
                    shouldHaveFoliage = true;
                    break;
                }
                if (triple.I1 >= 0 && MyVoxelMaterials1.Table[triple.I1].HasFoliage)
                {
                    shouldHaveFoliage = true;
                    break;
                }
                if (triple.I2 >= 0 && MyVoxelMaterials1.Table[triple.I2].HasFoliage)
                {
                    shouldHaveFoliage = true;
                    break;
                }
            }
            return shouldHaveFoliage;
        }
    }
}
