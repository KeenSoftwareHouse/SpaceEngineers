using VRage.Voxels;
using VRageMath;

namespace VRage.ModAPI
{
    public interface IMyStorage
    {
        /// <summary>
        /// Gets compressed voxel data
        /// </summary>
        void Save(out byte[] outCompressedData);

        Vector3I Size { get; }

        void OverwriteAllMaterials(byte materialIndex);

        /// <summary>
        /// Reads range of content and/or materials from specified LOD. If you want to write data back later, you must read LOD0 as that is the only writable one.
        /// </summary>
        /// <param name="lodVoxelRangeMin">Inclusive.</param>
        /// <param name="lodVoxelRangeMax">Inclusive.</param>
        void ReadRange(MyStorageData target, MyStorageDataTypeFlags dataToRead, int lodIndex, Vector3I lodVoxelRangeMin, Vector3I lodVoxelRangeMax);

        /// <summary>
        /// Writes range of content and/or materials from cache to storage. Note that this can only write to LOD0 (higher LODs must be computed based on that).
        /// </summary>
        /// <param name="voxelRangeMin">Inclusive.</param>
        /// <param name="voxelRangeMax">Inclusive.</param>
        void WriteRange(MyStorageData source, MyStorageDataTypeFlags dataToWrite, Vector3I voxelRangeMin, Vector3I voxelRangeMax);
    }
}