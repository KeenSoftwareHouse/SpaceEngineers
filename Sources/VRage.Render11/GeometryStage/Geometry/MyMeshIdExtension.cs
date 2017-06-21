using System.Diagnostics;
using VRageMath;

namespace VRageRender
{
    public static class MeshIdExtensions
    {
        internal static int GetIndexCount(this MeshId meshId)
        {
            return MyMeshes.GetLodMesh(meshId, 0).Info.IndicesNum;
        }

        internal static BoundingBox? GetBoundingBox(this MeshId meshId, int lod)
        {
            return MyMeshes.GetLodMesh(meshId, lod).Info.BoundingBox;
        }

        internal static void AssignLodMeshToProxy(this MeshId meshId, MyRenderableProxy proxy)
        {
            Debug.Assert(proxy != null, "Proxy cannot be null!");
            proxy.Mesh = MyMeshes.GetLodMesh(meshId, 0);
        }

        internal static MyMeshBuffers GetMeshBuffers(this MeshId meshId)
        {
            return MyMeshes.GetLodMesh(meshId, 0).Buffers;
        }

        internal static bool ShouldHaveFoliage(this MeshId meshId)
        {
            int partsNum = MyMeshes.GetLodMesh(meshId, 0).Info.PartsNum;

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
