using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Game.Voxels;
using System.Collections.Generic;
using VRage.Collections;
using VRage.Common.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities.VoxelMaps
{
    public interface IMyDepositCell
    {
        int TotalRareOreContent { get; }
        int CellIndex { get; }

        List<MyVoxelMaterialDefinition> GetOreWithContent();

        Vector3 ComputeWorldCenter(MyVoxelMap referenceVoxelMap);
        bool TryFindMaterialWorldPosition(MyVoxelMap referenceVoxelMap, MyVoxelMaterialDefinition material, out Vector3 position);
    }

    public interface IMyStorage
    {
        MyStringId NameId { get; }

        string Name { get; }

        Vector3I Size { get; }

        DictionaryValuesReader<int, IMyDepositCell> OreDeposits { get; }

        void Close();

        void OverwriteAllMaterials(MyVoxelMaterialDefinition material);

        void SetSurfaceMaterial(MyVoxelMaterialDefinition material, int cellThickness);

        void Save(out byte[] outCompressedData);

        void MergeVoxelMaterials(MyMwcVoxelFilesEnum voxelFile, Vector3I voxelPosition, MyVoxelMaterialDefinition materialToSet);

        void GetAllMaterialsPresent(HashSet<MyVoxelMaterialDefinition> outputMaterialSet);

        void RecomputeOreDeposits();

        /// <summary>
        /// Reads range of content and/or materials from specified LOD. If you want to write data back later, you must read LOD0 as that is the only writable one.
        /// </summary>
        /// <param name="lodVoxelRangeMin">Inclusive.</param>
        /// <param name="lodVoxelRangeMax">Inclusive.</param>
        void ReadRange(MyStorageDataCache target, bool readContent, bool readMaterials, int lodIndex, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax);

        /// <summary>
        /// Writes range of content and/or materials from cache to storage. Note that this can only write to LOD0 (higher LODs must be computed based on that).
        /// </summary>
        /// <param name="voxelRangeMin">Inclusive.</param>
        /// <param name="voxelRangeMax">Inclusive.</param>
        void WriteRange(MyStorageDataCache source, bool writeContent, bool writeMaterials, ref Vector3I voxelRangeMin, ref Vector3I voxelRangeMax);

        /// <param name="lodIndex">Index of lod from which we want voxels. 0 is bottom and voxel size doubles with each lod (index can be used as bit shift)</param>
        /// <param name="lodVoxelRangeMin">Inclusive min.</param>
        /// <param name="lodVoxelRangeMax">Inclusive max.</param>
        MyVoxelRangeType GetRangeType(int lodIndex, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax);

        void DebugDraw(MyVoxelMap voxelMap, MyVoxelDebugDrawMode mode, int modeArg);
    }
}
