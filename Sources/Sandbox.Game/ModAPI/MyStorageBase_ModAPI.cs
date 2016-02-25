using Sandbox.Definitions;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Engine.Voxels
{
    partial class MyStorageBase : VRage.ModAPI.IMyStorage
    {
        Vector3I VRage.ModAPI.IMyStorage.Size
        {
            get { return Size; }
        }

        void VRage.ModAPI.IMyStorage.OverwriteAllMaterials(byte materialIndex)
        {
            OverwriteAllMaterials(MyDefinitionManager.Static.GetVoxelMaterialDefinition(materialIndex));
        }

        void VRage.ModAPI.IMyStorage.ReadRange(MyStorageData target, MyStorageDataTypeFlags dataToRead, int lodIndex, Vector3I lodVoxelRangeMin, Vector3I lodVoxelRangeMax)
        {
            if ((uint)lodIndex >= (uint)MyCellCoord.MAX_LOD_COUNT)
                return;

            ReadRange(target, dataToRead, lodIndex, ref lodVoxelRangeMin, ref lodVoxelRangeMax);
        }

        void VRage.ModAPI.IMyStorage.WriteRange(MyStorageData source, MyStorageDataTypeFlags dataToWrite, Vector3I voxelRangeMin, Vector3I voxelRangeMax)
        {
            WriteRange(source, dataToWrite, ref voxelRangeMin, ref voxelRangeMax);
        }
    }
}