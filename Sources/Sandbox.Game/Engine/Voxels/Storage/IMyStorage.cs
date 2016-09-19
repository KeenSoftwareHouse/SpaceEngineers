using System;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using System.Linq;
using Sandbox.Game.World;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Engine.Voxels
{
    public delegate void RangeChangedDelegate(Vector3I minVoxelChanged, Vector3I maxVoxelChanged, MyStorageDataTypeFlags changedData);

    public interface IMyStorage : VRage.ModAPI.IMyStorage
    {
        new Vector3I Size { get; }

        MyVoxelGeometry Geometry { get; }

        /// <summary>
        /// please use RangeChanged on voxelbase if possible
        /// </summary>
        event RangeChangedDelegate RangeChanged;

        void OverwriteAllMaterials(MyVoxelMaterialDefinition material);

        /// <summary>
        /// Reads range of content and/or materials from specified LOD. If you want to write data back later, you must read LOD0 as that is the only writable one.
        /// </summary>
        /// <param name="lodVoxelRangeMin">Inclusive.</param>
        /// <param name="lodVoxelRangeMax">Inclusive.</param>
        void ReadRange(MyStorageData target, MyStorageDataTypeFlags dataToRead, int lodIndex, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax);

        /**
         * Read from the storage accepting the optimizations provided in requestFlags.
         * 
         * After this call requestFlags will inform of which optimization were in fact employed on the request.
         */
        void ReadRange(MyStorageData target, MyStorageDataTypeFlags dataToRead, int lodIndex, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax, ref MyVoxelRequestFlags requestFlags);

        /// <summary>
        /// Writes range of content and/or materials from cache to storage. Note that this can only write to LOD0 (higher LODs must be computed based on that).
        /// </summary>
        /// <param name="voxelRangeMin">Inclusive.</param>
        /// <param name="voxelRangeMax">Inclusive.</param>
        void WriteRange(MyStorageData source, MyStorageDataTypeFlags dataToWrite, ref Vector3I voxelRangeMin, ref Vector3I voxelRangeMax);

        /**
         * Check for intersection with a bounding box.
         * 
         * The bounding box must be in local space (LLB corner origin).
         * 
         * If lazy is true the method is faster but may return Intersects when the box is actually contained.
         */
        ContainmentType Intersect(ref BoundingBox box, bool lazy = true);

        /**
         * Find the smallest continuous segment from the provided line that may intersect the storage
         * (within some storage dependent margin).
         * 
         * The coordinates must be in local space (LLB corner origin).
         * 
         * The provided segment is replaced with the intersecting segment.
         * 
         * This returns true when there is intersection, false otherwise.
         */
        bool Intersect(ref LineD line);

        void DebugDraw(MyVoxelBase voxelMap, MyVoxelDebugDrawMode mode);

        IMyStorageDataProvider DataProvider { get; }

        /**
         * Pin storage.
         * 
         * Prevent the storage from closing while the pin is alive.
         */
        StoragePin Pin();

        void Unpin();

        /**
         * Weather the storage is closed.
         */
        bool Closed { get; }

        void Reset();

        void Close();

        bool Shared { get; }

        IMyStorage Copy();
    }

    public struct StoragePin : IDisposable
    {
        IMyStorage m_storage;

        public StoragePin(MyStorageBase myStorageBase)
        {
            m_storage = myStorageBase;
        }

        public bool Valid { get { return m_storage != null; } }

        public void Dispose()
        {
            m_storage.Unpin();
        }
    }

    public static class IMyStorageExtensions
    {
        public static void ClampVoxelCoord(this VRage.ModAPI.IMyStorage self, ref Vector3I voxelCoord, int distance = 1)
        {
            if (self == null) return;
            var sizeMinusOne = self.Size - distance;
            Vector3I.Clamp(ref voxelCoord, ref Vector3I.Zero, ref sizeMinusOne, out voxelCoord);
        }

        public static MyVoxelMaterialDefinition GetMaterialAt(this IMyStorage self, ref Vector3D localCoords)
        {
            MyVoxelMaterialDefinition def;

            Vector3I voxelCoords = Vector3D.Floor(localCoords / MyVoxelConstants.VOXEL_SIZE_IN_METRES);

            MyStorageData cache = new MyStorageData();
            cache.Resize(Vector3I.One);
            cache.ClearMaterials(0);

            self.ReadRange(cache, MyStorageDataTypeFlags.Material, 0, ref voxelCoords, ref voxelCoords);

            def = MyDefinitionManager.Static.GetVoxelMaterialDefinition(cache.Material(0));

            return def;
        }

        public static MyVoxelMaterialDefinition GetMaterialAt(this IMyStorage self, ref Vector3I voxelCoords)
        {
            MyVoxelMaterialDefinition def;

            MyStorageData cache = new MyStorageData();
            cache.Resize(Vector3I.One);
            cache.ClearMaterials(0);

            self.ReadRange(cache, MyStorageDataTypeFlags.Material, 0, ref voxelCoords, ref voxelCoords);

            def = MyDefinitionManager.Static.GetVoxelMaterialDefinition(cache.Material(0));

            return def;
        }

        public static void DebugDrawChunk(this IMyStorage self, Vector3I start, Vector3I end, Color? c = null)
        {
            if (!c.HasValue) c = Color.Blue;

            var vmaps = MySession.Static.VoxelMaps.Instances.Where(x => x.Storage == self);

            var box = new BoundingBoxD(start, end + 1);

            box.Translate(-((Vector3D)self.Size * .5) - .5);

            foreach (var map in vmaps)
            {
                MyRenderProxy.DebugDrawOBB(new MyOrientedBoundingBoxD(box, map.WorldMatrix), c.Value, 0.5f, true, true);
            }
        }
    }
}