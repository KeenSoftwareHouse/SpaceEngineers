using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Collections;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    public delegate void RangeChangedDelegate(Vector3I minVoxelChanged, Vector3I maxVoxelChanged, MyStorageDataTypeFlags changedData);

    public interface IMyStorage : Sandbox.ModAPI.Interfaces.IMyStorage
    {
        new Vector3I Size { get; }

        MyVoxelGeometry Geometry { get; }

        event RangeChangedDelegate RangeChanged;

        void OverwriteAllMaterials(MyVoxelMaterialDefinition material);

        void Save(out byte[] outCompressedData);

        /// <summary>
        /// Reads range of content and/or materials from specified LOD. If you want to write data back later, you must read LOD0 as that is the only writable one.
        /// </summary>
        /// <param name="lodVoxelRangeMin">Inclusive.</param>
        /// <param name="lodVoxelRangeMax">Inclusive.</param>
        void ReadRange(MyStorageDataCache target, MyStorageDataTypeFlags dataToRead, int lodIndex, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax);

        /// <summary>
        /// Writes range of content and/or materials from cache to storage. Note that this can only write to LOD0 (higher LODs must be computed based on that).
        /// </summary>
        /// <param name="voxelRangeMin">Inclusive.</param>
        /// <param name="voxelRangeMax">Inclusive.</param>
        void WriteRange(MyStorageDataCache source, MyStorageDataTypeFlags dataToWrite, ref Vector3I voxelRangeMin, ref Vector3I voxelRangeMax);

        /// <summary>
        /// Resets map ouside the given aabb.
        /// </summary>
        /// <param name="minVoxel"></param>
        /// <param name="maxVoxel"></param>
        void ResetOutsideBorders(MyVoxelBase voxelMap, BoundingBoxD worldAabb);

        void DebugDraw(MyVoxelBase voxelMap, MyVoxelDebugDrawMode mode);

        IMyStorageDataProvider DataProvider { get; }
    }

    public static class IMyStorageExtensions
    {
        public static void ClampVoxelCoord(this Sandbox.ModAPI.Interfaces.IMyStorage self, ref Vector3I voxelCoord, int distance = 1)
        {
            var sizeMinusOne = self.Size - distance;
            Vector3I.Clamp(ref voxelCoord, ref Vector3I.Zero, ref sizeMinusOne, out voxelCoord);
        }
    }
}