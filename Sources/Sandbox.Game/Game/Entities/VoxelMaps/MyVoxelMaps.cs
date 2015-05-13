using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.VoxelMaps;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Collections;
using VRage.Common.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Voxels
{

    //  Used to sort data cells by their distance to given line's starting point
    class MySortDataCellByDistanceComparer : IComparer<MyDataCellForSorting>
    {
        public int Compare(MyDataCellForSorting x, MyDataCellForSorting y)
        {
            return x.CellDistanceToLineFrom.CompareTo(y.CellDistanceToLineFrom);
        }
    }

    //  Used to sort render cell by distance between their center and camera
    class MyRenderCellByDistanceComparer : IComparer<MyRenderCellForSorting>
    {
        public int Compare(MyRenderCellForSorting x, MyRenderCellForSorting y)
        {
            float xDist, yDist;
            Vector3 campos = MySector.MainCamera.Position;
            Vector3.Distance(ref x.RenderCell.Center, ref campos, out xDist);
            Vector3.Distance(ref y.RenderCell.Center, ref campos, out yDist);
            return xDist.CompareTo(yDist);
        }
    }

    struct MyRenderCellForSorting
    {
        public MyVoxelCacheCellRender RenderCell;
    }

    struct MyDataCellForSorting
    {
        public float CellDistanceToLineFrom;
    }

    public partial class MyVoxelMaps
    {
        private List<MyVoxelMap> m_voxelMaps;
        private Dictionary<int, MyVoxelMap> m_voxelMapsById;
        private Dictionary<MyStringId, MyStorageBase> m_storageByName = new Dictionary<MyStringId, MyStorageBase>(MyStringId.Comparer);

        //  For generating new and unique VoxelMapId
        private int m_voxelMapIdGenerator = 0;

        //Spare some time in un/load
        public bool AutoRecalculateVoxelMaps = true;

        //  This array is used for holding potential triangles we need to test for intersections more closly
        // Size of this array is 2048 * 100B = 200KB
        internal static readonly MyRenderCellByDistanceComparer SortedRenderCellsByDistanceComparer = new MyRenderCellByDistanceComparer();
        internal static readonly MySortDataCellByDistanceComparer SortedDataCellByDistanceToLineComparer = new MySortDataCellByDistanceComparer();

        internal static Dictionary<string, byte[]> MultiplayerVoxelMaps;

        public MyVoxelMaps()
        {
            Debug.Assert((MyVoxelConstants.HASH_VOXEL_CELL_COORD_BITS * 3 + MyVoxelConstants.HASH_VOXEL_ID_BITS + 1) <= 64,
                "Voxel cell hash does not fit inside 64 bits.");
            Debug.Assert(MyVoxelConstants.MAX_VOXEL_CELLS_COUNT <= (1 << MyVoxelConstants.HASH_VOXEL_CELL_COORD_BITS),
                string.Format("MyVoxelConstants.MAX_VOXEL_CELLS_COUNT mus be less than {0}, but it is {1}. Hash computation is unreliabled.",
                (1 << MyVoxelConstants.HASH_VOXEL_CELL_COORD_BITS), MyVoxelConstants.MAX_VOXEL_CELLS_COUNT));

            m_voxelMaps = new List<MyVoxelMap>();
            m_voxelMapsById = new Dictionary<int, MyVoxelMap>();
            m_voxelMapIdGenerator = 0;
        }

        public void Clear()
        {
            foreach (var voxelMap in m_voxelMaps)
                voxelMap.MarkForClose();
            m_voxelMaps.Clear();
            m_voxelMapsById.Clear();

            foreach (var storage in m_storageByName.Values)
                storage.Close();
            m_storageByName.Clear();

            m_voxelMapIdGenerator = 0;

            MultiplayerVoxelMaps = null;
        }

        //  Allows you to iterate through all voxel maps
        public ListReader<MyVoxelMap> GetVoxelMaps()
        {
            return m_voxelMaps;
        }

        public void RecalcVoxelMaps()
        {
            MySandboxGame.Log.WriteLine("MyVoxelMaps.RecalcVoxelMaps - START");

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //  Get voxel map with highest count of render cells, and then use this number to preallocate sorting list
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 

            Profiler.Begin("sortedRenderCells");

            int maxRenderCellsCount = 0;
            foreach (MyVoxelMap voxelMap in m_voxelMaps)
            {
                int count = voxelMap.RenderCellsCount.X * voxelMap.RenderCellsCount.Y * voxelMap.RenderCellsCount.Z;
                if (count > maxRenderCellsCount)
                    maxRenderCellsCount = count;
            }

            Profiler.End();

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //  Precalculate all data cells (we get triangles from voxels). In this step we don't get render cells, but that will be fast.
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            Profiler.Begin("MyVoxelMaps::PrepareRenderCellCache");

            PrepareRenderCellCache();

            Profiler.End();

            MySandboxGame.Log.WriteLine("MyVoxelMaps.RecalcVoxelMaps - END");
        }

        public int AllocateId()
        {
            Debug.Assert(m_voxelMapIdGenerator < MyVoxelConstants.MAX_VOXEL_MAP_ID - 1);
            return m_voxelMapIdGenerator++;
        }

        public bool Exist(MyVoxelMap voxelMap)
        {
            return m_voxelMapsById.ContainsKey(voxelMap.VoxelMapId);
        }

        public void RemoveVoxelMap(MyVoxelMap voxelMap)
        {
            if (m_voxelMaps.Remove(voxelMap))
            {
                m_voxelMaps.Remove(voxelMap);
                if (AutoRecalculateVoxelMaps)
                    RecalcVoxelMaps();

                m_voxelMapsById.Remove(voxelMap.VoxelMapId);
            }
        }

        /// <summary>
        /// Voxel cell hash computation using id, coordinates and LOD. These values are all merged into single
        /// 64 bit number in order [LOD|MapId|Coord.X|Coord.Y|Coord.Z]. There should be constaints that ensure
        /// everything fits in required number of bits. See MyVoxelConstants for number of bits used in hash
        /// computation.
        /// </summary>
        /// <param name="cellHashType">Must be 0 or 1 when cast to integer.</param>
        public Int64 GetCellHashCode(int voxelMapId, ref Vector3I cellCoord, MyLodTypeEnum cellHashType)
        {
            Debug.Assert((uint)cellHashType <= 1, "Cell hash type is only allowed to be 0 or 1 (has to fit inside single bit).");

            Int64 hash = (Int64)cellHashType;
            hash = (hash << MyVoxelConstants.HASH_VOXEL_ID_BITS) + voxelMapId;
            hash = (hash << MyVoxelConstants.HASH_VOXEL_CELL_COORD_BITS) + cellCoord.X;
            hash = (hash << MyVoxelConstants.HASH_VOXEL_CELL_COORD_BITS) + cellCoord.Y;
            hash = (hash << MyVoxelConstants.HASH_VOXEL_CELL_COORD_BITS) + cellCoord.Z;
            return hash;
        }

        public MyVoxelMap GetOverlappingWithSphere(ref BoundingSphere sphere)
        {
            for (int i = 0; i < m_voxelMaps.Count; i++)
            {
                MyVoxelMap voxelMap = m_voxelMaps[i];
                if (voxelMap.DoOverlapSphereTest(sphere.Radius, sphere.Center))
                {
                    return voxelMap;
                }
            }

            //  No intersection found
            return null;
        }

        //  Create vertex buffers and index buffers and fill them with voxel render cells
        //  IMPORTANT: Don't call from background thread or from LoadContent. Only from Draw call
        public void PrepareRenderCellCache()
        {
            MySandboxGame.Log.WriteLine("MyVoxelMaps.PrepareRenderCellCache - START");
            MySandboxGame.Log.IncreaseIndent();

            Profiler.Begin("MyVoxelMaps.PrepareRenderCellCache");

            for (int voxelMapIterator = 0; voxelMapIterator < m_voxelMaps.Count; voxelMapIterator++)
            {
                MyVoxelMap voxelMap = m_voxelMaps[voxelMapIterator];
                voxelMap.PrepareRenderCellCache();
            }

            Profiler.End();

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyVoxelMaps.PrepareRenderCellCache - END");
        }

        public void Add(MyVoxelMap voxelMap)
        {
            if (!Exist(voxelMap))
            {
                m_voxelMaps.Add(voxelMap);
                m_voxelMapsById.Add(voxelMap.VoxelMapId, voxelMap);
                //Need to be here because of materials, when added voxel in editor
                if (AutoRecalculateVoxelMaps)
                    RecalcVoxelMaps();
            }
        }

        public MyVoxelMap GetLargestVoxelMap()
        {
            MyVoxelMap largestVoxelMap = null;
            Vector3 voxelSizeInMetres = Vector3.Zero;
            foreach (MyVoxelMap voxelMap in m_voxelMaps)
            {
                if (voxelMap.SizeInMetres.Length() > voxelSizeInMetres.Length())
                {
                    voxelSizeInMetres = voxelMap.SizeInMetres;
                    largestVoxelMap = voxelMap;
                }
            }
            return largestVoxelMap;
        }

        public MyVoxelMap GetVoxelMap(int voxelMapID)
        {
            MyVoxelMap voxelMap;
            m_voxelMapsById.TryGetValue(voxelMapID, out voxelMap);
            return voxelMap;
        }

        public bool IsCollidingWithVoxelMap(MyMwcVoxelFilesEnum voxelFileEnum, Vector3 voxelPosition)
        {
            MyVoxelFile voxelFile = MyVoxelFiles.Get(voxelFileEnum);
            Vector3 sizeInMeters = voxelFile.SizeInVoxels * MyVoxelConstants.VOXEL_SIZE_IN_METRES;

            BoundingBox newBoundingBox = MyUtils.GetNewBoundingBox(voxelPosition, sizeInMeters);
            MyVoxelMap intersectingVoxelMap = GetVoxelMapWhoseBoundingBoxIntersectsBox(ref newBoundingBox, null);

            if (intersectingVoxelMap != null)
                return true;

            return false;
        }

        /// <summary>
        /// Return reference to voxel map that intersects the box. If not voxel map found, null is returned.
        /// </summary>
        public MyVoxelMap GetVoxelMapWhoseBoundingBoxIntersectsBox(ref BoundingBox boundingBox, MyVoxelMap ignoreVoxelMap)
        {
            for (int i = 0; i < m_voxelMaps.Count; i++)
            {
                MyVoxelMap voxelMap = m_voxelMaps[i];
                if (voxelMap != ignoreVoxelMap)
                {
                    if (voxelMap.IsBoxIntersectingBoundingBoxOfThisVoxelMap(ref boundingBox))
                        return voxelMap;
                }
            }

            //  If we get here, no intersection was found
            return null;
        }

        public Dictionary<string, byte[]> GetVoxelMapsArray()
        {
            Profiler.Begin("GetVoxelMapsArray");

            Dictionary<string, byte[]> voxelMaps = new Dictionary<string, byte[]>();

            byte[] compressedData;
            foreach (var voxelMap in m_voxelMaps)
            {
                if (!voxelMaps.ContainsKey(voxelMap.Storage.Name))
                {
                    voxelMap.Storage.Save(out compressedData);
                    voxelMaps.Add(voxelMap.Storage.Name, compressedData);
                }
            }

            Profiler.End();
            return voxelMaps;
        }

        public static void SaveVoxelMapsArray(Dictionary<string, byte[]> voxelMaps)
        {
            MultiplayerVoxelMaps = voxelMaps;
        }

        internal void AddStorage(MyStorageBase storage)
        {
            m_storageByName.Add(storage.NameId, storage);
        }

        internal void OnStorageRenamed(MyStorageBase storage, MyStringId oldName)
        {
            Debug.Assert(m_storageByName.ContainsKey(oldName) && m_storageByName[oldName] == storage);
            m_storageByName.Remove(oldName);
            AddStorage(storage);
        }

        internal bool TryGetStorage(string name, out MyStorageBase result)
        {
            MyStringId id;
            if (MyStringId.TryGet(name, out id))
                return m_storageByName.TryGetValue(id, out result);

            result = null;
            return false;
        }
    }
}
