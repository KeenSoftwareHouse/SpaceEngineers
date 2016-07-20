using System.Collections;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.WorldEnvironment.Definitions;
using VRage.Game.Entity;
using VRage.ObjectBuilders;
using VRageMath;
using VRage.ObjectBuilders;
using System;
using Sandbox.Engine.Voxels;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;

namespace Sandbox.Game.WorldEnvironment
{

    public struct MySurfaceParams
    {
        public Vector3 Position;
        public Vector3 Gravity;
        public Vector3 Normal;
        public byte Material;
        public float HeightRatio;
        public float Latitude;
        public float Longitude;
        public byte Biome;
    }

    public interface IMyEnvironmentOwner
    {
        // Surface parameters
        void QuerySurfaceParameters(Vector3D localOrigin, ref BoundingBoxD queryBounds, List<Vector3> queries, List<MySurfaceParams> results);

        // Get active sector for a poin in the world.
        MyEnvironmentSector GetSectorForPosition(Vector3D positionWorld);

        MyEnvironmentSector GetSectorById(long packedSectorId);

        void SetSectorPinned(MyEnvironmentSector sector, bool pinned);

        // Get seed for procedural stuff (TO BE REMOVED)
        int GetSeed();

        // Model Management
        short GetModelId(MyPhysicalModelDefinition def);
        MyPhysicalModelDefinition GetModelForId(short id);

        // Get item definition by index.
        void GetDefinition(ushort index, out MyRuntimeEnvironmentItemInfo def);

        // Get the environment definition
        MyWorldEnvironmentDefinition EnvironmentDefinition { get; }

        // Entity associated with this sector;
        MyEntity Entity { get; }

        // Find the projection of a point to the terrain surface.
        void ProjectPointToSurface(ref Vector3D center);

        void GetSurfaceNormalForPoint(ref Vector3D point, out Vector3D normal);

        // Get the bounding shape for a sector.
        Vector3D[] GetBoundingShape(ref Vector3D worldPos, ref Vector3 basisX, ref Vector3 basisY);

        //Schedule a sector for work.
        void ScheduleWork(MyEnvironmentSector sector, bool parallel);
    }

    public struct MyEnvironmentSectorParameters
    {
        // Entity ID to set for this sector (if != 0)
        public long EntityId;

        public BoundingBox2I DataRange;

        // Items positions lie within BasisA*[-1,1] x BasisB*[-1,1]
        public Vector3 SurfaceBasisX;
        public Vector3 SurfaceBasisY;

        // Center of the sector with respect to the basis.
        public Vector3D Center;

        // Visual bounding convex of this sector. A 6-faced convex shape (8 vertices).
        // This is used for calculating the center of the sector and for debug draw.
        // The order of the vertices is left to right : back to front : bottom to top
        public Vector3D[] Bounds;

        // Definitions for this sector.
        public MyWorldEnvironmentDefinition Environment;

        // Provider for this sector.
        public IMyEnvironmentDataProvider Provider;

        // Id used by the parent to identify this sector.
        public long SectorId;
    }

    // Contact point event for environment sectors.
    public delegate void MySectorContactEvent(int itemId, MyEntity other, ref MyPhysics.MyContactPointEvent evt);

    /**
     * Information about a item.
     * 
     * Total Size: 32 bytes;
     */
    public struct ItemInfo
    {
        public Vector3 Position; // Position relative to sector center.
        public short DefinitionIndex; // Index of the item's definition. Per environment
        public short ModelIndex; // Index of the item's physical item. Per environment owner.
        public Quaternion Rotation; // Rotation of this item. This is post slope adjustment

        public override string ToString()
        {
            return string.Format("Model: {0}; Def: {1}", ModelIndex, DefinitionIndex);
        }
    }

    public abstract class MyEnvironmentDataView
    {
        public Vector2I Start, End;
        public int Lod;

        public List<ItemInfo> Items;

        public abstract void Close();

        public void GetLogicalSector(int item, out int logicalItem, out MyLogicalEnvironmentSectorBase sector)
        {
            int logical = SectorOffsets.BinaryIntervalSearch(item) - 1;

            logicalItem = item - SectorOffsets[logical] + IntraSectorOffsets[logical];

            sector = LogicalSectors[logical];
        }

        // Listener for item changes.
        public MyEnvironmentSector Listener;

        // Item offsets per logical sector
        public List<int> SectorOffsets;

        // Offsets of taken items in each logical sector.
        public List<int> IntraSectorOffsets;

        // List of logical sectors providing data.
        public List<MyLogicalEnvironmentSectorBase> LogicalSectors;
    }

    public interface IMyEnvironmentDataProvider
    {
        MyEnvironmentDataView GetItemView(int lod, ref Vector2I start, ref Vector2I end, ref Vector3D localOrigin);

        MyObjectBuilder_EnvironmentDataProvider GetObjectBuilder();

        void DebugDraw();
        IEnumerable<MyLogicalEnvironmentSectorBase> LogicalSectors { get; }

        MyLogicalEnvironmentSectorBase GetLogicalSector(long sectorId);
    }

    public static class MyEnvironmentSectorConstants
    {
        public const int MaximumLod = 15;
    }

    public static class MyEnvironmentSectorExtensions
    {
        public static bool HasWorkPending(this MyEnvironmentSector self)
        {
            return self.HasSerialWorkPending || self.HasParallelWorkPending;
        }

        public static unsafe void DisableItemsInShape(this MyEnvironmentSector sector, MyShape shape)
        {
            if (sector.DataView == null)
                return;

            for (int sectorInd = 0; sectorInd < sector.DataView.LogicalSectors.Count; sectorInd++)
            {
                var logicalSector = sector.DataView.LogicalSectors[sectorInd];
                var logicalItems = logicalSector.Items;
                var cnt = logicalItems.Count;

                fixed (ItemInfo* items = logicalItems.GetInternalArray())
                    for (int i = 0; i < cnt; ++i)
                    {
                        var worldPos = sector.SectorCenter + items[i].Position;
                        if (shape.GetVolume(ref worldPos) > 0f)
                        {
                            logicalSector.EnableItem(i, false);
                        }
                    }
            }
        }

        public static unsafe void DisableItemsInObb(this MyEnvironmentSector sector, ref MyOrientedBoundingBoxD obb)
        {
            if (sector.DataView == null)
                return;

            obb.Center -= sector.SectorCenter;

            for (int sectorInd = 0; sectorInd < sector.DataView.LogicalSectors.Count; sectorInd++)
            {
                var logicalSector = sector.DataView.LogicalSectors[sectorInd];
                var logicalItems = logicalSector.Items;
                var cnt = logicalItems.Count;

                fixed (ItemInfo* items = logicalItems.GetInternalArray())
                    for (int i = 0; i < cnt; ++i)
                    {
                        if (items[i].DefinitionIndex >= 0 && obb.Contains(ref items[i].Position))
                            logicalSector.EnableItem(i, false);
                    }
            }
        }

        public static unsafe void DisableItemsInAabb(this MyEnvironmentSector sector, ref BoundingBoxD aabb)
        {
            if (sector.DataView == null)
                return;

            aabb.Translate(-sector.SectorCenter);

            for (int sectorInd = 0; sectorInd < sector.DataView.LogicalSectors.Count; sectorInd++)
            {
                var logicalSector = sector.DataView.LogicalSectors[sectorInd];
                var logicalItems = logicalSector.Items;
                var cnt = logicalItems.Count;

                fixed (ItemInfo* items = logicalItems.GetInternalArray())
                    for (int i = 0; i < cnt; ++i)
                    {
                        if (items[i].DefinitionIndex >= 0 && aabb.Contains(items[i].Position) != ContainmentType.Disjoint)
                            logicalSector.EnableItem(i, false);
                    }
            }
        }

        public static unsafe void GetItemsInAabb(this MyEnvironmentSector sector, ref BoundingBoxD aabb, List<int> itemsInBox)
        {
            if (sector.DataView == null)
                return;

            aabb.Translate(-sector.SectorCenter);

            for (int sectorInd = 0; sectorInd < sector.DataView.LogicalSectors.Count; sectorInd++)
            {
                var logicalSector = sector.DataView.LogicalSectors[sectorInd];
                var logicalItems = logicalSector.Items;
                var cnt = logicalItems.Count;

                fixed (ItemInfo* items = logicalItems.GetInternalArray())
                    for (int i = 0; i < cnt; ++i)
                    {
                        if (items[i].DefinitionIndex >= 0 && aabb.Contains(items[i].Position) != ContainmentType.Disjoint)
                            itemsInBox.Add(i);
                    }
            }
        }
    }
}
