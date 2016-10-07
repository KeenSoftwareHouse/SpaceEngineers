using System;
using VRage.Voxels;
using VRageMath;

namespace VRage.ModAPI
{
    public interface IMyStorage
    {
        /// <summary>
        /// Returns true if voxel storage was closed
        /// </summary>
        bool Closed { get; }

        /// <summary>
        /// Returns true if the voxel storage is marked for a pending close
        /// </summary>
        bool MarkedForClose { get; }

        /// <summary>
        /// Gets compressed voxel data
        /// </summary>
        void Save(out byte[] outCompressedData);

        /// <summary>
        /// The size of the voxel storage, in voxels
        /// </summary>
        Vector3I Size { get; }

        /// <summary>
        /// Replaces all materials in range with the specific material
        /// </summary>
        /// <param name="materialIndex"></param>
        [Obsolete]
        void OverwriteAllMaterials(byte materialIndex);

        /// <summary>
        /// Returns the intersection with the storage region
        /// </summary>
        /// <param name="box"></param>
        /// <param name="lazy"></param>
        /// <returns></returns>
        ContainmentType Intersect(ref BoundingBox box, bool lazy);

        /// <summary>
        /// Returns true if the specific line intersects the storage region
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        bool Intersect(ref LineD line);

        /// <summary>
        /// Pins the voxel storage to prevent closing, then executes specified action. Unpins when action completes.
        /// </summary>
        /// <param name="action">Action to execute</param>
        void PinAndExecute(Action action);

        /// <summary>
        /// Pins the voxel storage to prevent closing, then executes specified action. Unpins when action completes.
        /// </summary>
        /// <param name="action">Action to execute</param>
        void PinAndExecute(Action<IMyStorage> action);

        /// <summary>
        /// Resets the data specified by flags to values from data provider, or default if no provider is assigned.
        /// </summary>
        /// <param name="dataToReset"></param>
        void Reset(MyStorageDataTypeFlags dataToReset);

        /// <summary>
        /// Reads range of content and/or materials from specified LOD. If you want to write data back later, you must read LOD0 as that is the only writable one.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="dataToRead"></param>
        /// <param name="lodIndex"></param>
        /// <param name="lodVoxelRangeMin">Inclusive.</param>
        /// <param name="lodVoxelRangeMax">Inclusive.</param>
        void ReadRange(MyStorageData target, MyStorageDataTypeFlags dataToRead, int lodIndex, Vector3I lodVoxelRangeMin, Vector3I lodVoxelRangeMax);

        /// <summary>
        /// Reads range of content and/or materials from specified LOD. If you want to write data back later, you must read LOD0 as that is the only writable one.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="dataToRead"></param>
        /// <param name="lodIndex"></param>
        /// <param name="lodVoxelRangeMin"></param>
        /// <param name="lodVoxelRangeMax"></param>
        /// <param name="requestFlags"></param>
        void ReadRange(MyStorageData target, MyStorageDataTypeFlags dataToRead, int lodIndex, Vector3I lodVoxelRangeMin, Vector3I lodVoxelRangeMax, ref MyVoxelRequestFlags requestFlags);

        /// <summary>
        /// Writes range of content and/or materials from cache to storage. Note that this can only write to LOD0 (higher LODs must be computed based on that).
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dataToWrite"></param>
        /// <param name="voxelRangeMin">Inclusive.</param>
        /// <param name="voxelRangeMax">Inclusive.</param>
        void WriteRange(MyStorageData source, MyStorageDataTypeFlags dataToWrite, Vector3I voxelRangeMin, Vector3I voxelRangeMax);
    }
}