using Sandbox.Definitions;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Engine.Voxels
{
    partial class MyStorageBase : ModAPI.Interfaces.IMyStorage
    {
        Vector3I ModAPI.Interfaces.IMyStorage.Size
        {
            get { return Size; }
        }

        void ModAPI.Interfaces.IMyStorage.OverwriteAllMaterials(byte materialIndex)
        {
            OverwriteAllMaterials(MyDefinitionManager.Static.GetVoxelMaterialDefinition(materialIndex));
        }

        void ModAPI.Interfaces.IMyStorage.ReadRange(MyStorageDataCache target, MyStorageDataTypeFlags dataToRead, int lodIndex,  Vector3I lodVoxelRangeMin,  Vector3I lodVoxelRangeMax)
        {
            if ((uint)lodIndex >= (uint)MyCellCoord.MAX_LOD_COUNT)
                return;

            ReadRange(target, dataToRead, lodIndex, ref lodVoxelRangeMin, ref lodVoxelRangeMax);
        }

        void ModAPI.Interfaces.IMyStorage.WriteRange(MyStorageDataCache source, MyStorageDataTypeFlags dataToWrite, Vector3I voxelRangeMin,  Vector3I voxelRangeMax)
        {
            WriteRange(source, dataToWrite, ref voxelRangeMin, ref voxelRangeMax);
        }
    }
}