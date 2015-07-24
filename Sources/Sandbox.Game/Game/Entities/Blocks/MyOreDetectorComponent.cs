#region Using

using ParallelTasks;
using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Collections;
using VRage.Generics;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    public class MyEntityOreDeposit
    {
        public struct Data
        {
            public MyVoxelMaterialDefinition Material;
            public Vector3D AverageLocalPosition;

            internal void ComputeWorldPosition(MyVoxelBase voxelMap, out Vector3D oreWorldPosition)
            {
                MyVoxelCoordSystems.LocalPositionToWorldPosition(voxelMap.PositionLeftBottomCorner - (Vector3D)voxelMap.StorageMin, ref AverageLocalPosition, out oreWorldPosition);
            }
        }

        public MyVoxelBase VoxelMap;
        public Vector3I CellCoord;
        public readonly List<Data> Materials = new List<Data>();

        public MyEntityOreDeposit(MyVoxelBase voxelMap, Vector3I cellCoord)
        {
            VoxelMap = voxelMap;
            CellCoord = cellCoord;
        }

        public class TypeComparer : IEqualityComparer<MyEntityOreDeposit>
        {
            bool IEqualityComparer<MyEntityOreDeposit>.Equals(MyEntityOreDeposit x, MyEntityOreDeposit y)
            {
                return x.VoxelMap.EntityId == y.VoxelMap.EntityId &&
                    x.CellCoord == y.CellCoord;
            }

            int IEqualityComparer<MyEntityOreDeposit>.GetHashCode(MyEntityOreDeposit obj)
            {
                return (int)(obj.VoxelMap.EntityId ^ obj.CellCoord.GetHashCode());
            }
        }

        public static readonly TypeComparer Comparer = new TypeComparer();
    }

    class MyOreDepositGroup
    {
        private static Dictionary<Vector3I, MyEntityOreDeposit> m_swapBuffer = new Dictionary<Vector3I, MyEntityOreDeposit>(Vector3I.Comparer);

        private readonly HashSet<Vector3I> m_queriesToIssue = new HashSet<Vector3I>(Vector3I.Comparer);
        private readonly HashSet<Vector3I> m_issuedQueries = new HashSet<Vector3I>(Vector3I.Comparer);
        private readonly MyVoxelBase m_voxelMap;
        private readonly Action<Vector3I, MyEntityOreDeposit> m_onDepositQueryComplete;
        private Dictionary<Vector3I, MyEntityOreDeposit> m_depositsByCellCoord = new Dictionary<Vector3I, MyEntityOreDeposit>(Vector3I.Comparer);
        private Vector3I m_lastDetectionMin;
        private Vector3I m_lastDetectionMax;

        public MyOreDepositGroup(MyVoxelBase voxelMap)
        {
            m_voxelMap = voxelMap;
            m_onDepositQueryComplete = OnDepositQueryComplete;
            m_lastDetectionMax = new Vector3I(int.MinValue);
            m_lastDetectionMin = new Vector3I(int.MaxValue);
        }

        private void OnDepositQueryComplete(Vector3I depositCell, MyEntityOreDeposit deposit)
        {
            Debug.Assert(m_issuedQueries.Contains(depositCell));
            m_issuedQueries.Remove(depositCell);
            m_depositsByCellCoord[depositCell] = deposit;
            IssueQueries();
        }

        public DictionaryValuesReader<Vector3I, MyEntityOreDeposit> Deposits
        {
            get { return m_depositsByCellCoord; }
        }

        /// <summary>
        /// Starts queries to update Deposits unless worldDetectionSphere has not changed since last invokation.
        /// </summary>
        /// <param name="worldDetectionSphere">Sphere within which the Ore Detector can detect ores.</param>
        /// <param name="force">Start queries even if worldDetectionSphere has not changed.</param>
        public void UpdateDeposits(ref BoundingSphereD worldDetectionSphere, bool force)
        {
            Vector3I min, max;
            {
                var worldMin = worldDetectionSphere.Center - worldDetectionSphere.Radius;
                var worldMax = worldDetectionSphere.Center + worldDetectionSphere.Radius;
                MyVoxelCoordSystems.WorldPositionToVoxelCoord(m_voxelMap.PositionLeftBottomCorner, ref worldMin, out min);
                MyVoxelCoordSystems.WorldPositionToVoxelCoord(m_voxelMap.PositionLeftBottomCorner, ref worldMax, out max);
                // mk:TODO Get rid of this computation. Might require a mechanism to figure out whether MyVoxelMap is subpart of MyPlanet or not. (Maybe third class for subparts?)
                min += m_voxelMap.StorageMin;
                max += m_voxelMap.StorageMin;

                m_voxelMap.Storage.ClampVoxelCoord(ref min);
                m_voxelMap.Storage.ClampVoxelCoord(ref max);
                min >>= (MyOreDetectorComponent.CELL_SIZE_IN_VOXELS_BITS + MyOreDetectorComponent.QUERY_LOD);
                max >>= (MyOreDetectorComponent.CELL_SIZE_IN_VOXELS_BITS + MyOreDetectorComponent.QUERY_LOD);
            }

            if (min == m_lastDetectionMin && max == m_lastDetectionMax && !force)
            {
                IssueQueries();
                return;
            }

            m_lastDetectionMin = min;
            m_lastDetectionMax = max;

            Vector3I c;
            for (c.Z = min.Z; c.Z <= max.Z; ++c.Z)
            {
                for (c.Y = min.Y; c.Y <= max.Y; ++c.Y)
                {
                    for (c.X = min.X; c.X <= max.X; ++c.X)
                    {
                        MyEntityOreDeposit deposit;
                        if (m_depositsByCellCoord.TryGetValue(c, out deposit))
                        {
                            m_swapBuffer.Add(c, deposit);
                        }
                        else if (!m_issuedQueries.Contains(c))
                        {
                            m_queriesToIssue.Add(c);
                        }
                    }
                }
            }
            MyUtils.Swap(ref m_swapBuffer, ref m_depositsByCellCoord);
            m_swapBuffer.Clear();
            IssueQueries();
        }

        private void IssueQueries()
        {
            while (m_issuedQueries.Count < 100 && m_queriesToIssue.Count > 0)
            { // discard queries which have gone out of range
                var e = m_queriesToIssue.GetEnumerator();
                e.MoveNext();
                var coord = e.Current;
                if (!coord.IsInsideInclusive(ref m_lastDetectionMin, ref m_lastDetectionMax))
                {
                    m_queriesToIssue.Remove(coord);
                    continue;
                }

                MyDepositQuery.Start(new MyDepositQuery.Args()
                {
                    VoxelMap = m_voxelMap,
                    CompletionCallback = m_onDepositQueryComplete,
                    Cell = coord,
                });
                m_issuedQueries.Add(coord);
                m_queriesToIssue.Remove(coord);
            }
            Stats.Generic.Write("Ore detector queries", m_queriesToIssue.Count + m_issuedQueries.Count, VRage.Stats.MyStatTypeEnum.CurrentValue, 100, 0);
        }
    }

    class MyOreDetectorComponent
    {
        public const int QUERY_LOD = 1;
        public const int CELL_SIZE_IN_VOXELS_BITS = 3;
        public const int CELL_SIZE_IN_LOD_VOXELS = 1 << CELL_SIZE_IN_VOXELS_BITS;
        public const float CELL_SIZE_IN_METERS = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << (CELL_SIZE_IN_VOXELS_BITS + QUERY_LOD));
        public const float CELL_SIZE_IN_METERS_HALF = CELL_SIZE_IN_METERS * 0.5f;

        private static readonly List<MyVoxelBase> m_inRangeCache = new List<MyVoxelBase>();
        private static readonly List<MyVoxelBase> m_notInRangeCache = new List<MyVoxelBase>();

        public delegate bool CheckControlDelegate();

        public float DetectionRadius { get; set; }
        public CheckControlDelegate OnCheckControl;
        /// <summary>Provides access to all the ores detected by an Ore Detector for mod API.</summary>
        public event Action<ReadOnlyDictionary<Vector3D, byte>> OnOreUpdated;

        public bool BroadcastUsingAntennas { get; set; }

        private readonly Dictionary<MyVoxelBase, MyOreDepositGroup> m_depositGroupsByEntity = new Dictionary<MyVoxelBase, MyOreDepositGroup>();

        /// <summary>All the detected ores in a form that can be passed to mod A.P.I. Should never be cleared or manipulated, only replaced.</summary>
        private Dictionary<Vector3D, byte> m_lastOreLocation;

        public MyOreDetectorComponent()
        {
            DetectionRadius = 50;
            SetRelayedRequest = false;
            BroadcastUsingAntennas = false;
        }

        public bool SetRelayedRequest { get; set; }

        /// <summary>
        /// Updates ore deposit H.U.D. markers and triggers OnOreUpdated.
        /// </summary>
        /// <param name="position">The world location of the Ore Detector.</param>
        /// <param name="checkControl">If true, OnCheckControl determines if H.U.D. markers shall be updated.</param>
        /// <param name="hasLocalPlayerAccess">True iff this session's player has permission to access this Ore Detector.</param>
        public void Update(Func<Vector3D> position, bool checkControl = true, bool hasLocalPlayerAccess = true)
        {
            Clear();

            bool registerHudMarker = hasLocalPlayerAccess && (SetRelayedRequest || !checkControl || OnCheckControl());
            bool registeredListener = OnOreUpdated != null;

            if (!registeredListener)
                m_lastOreLocation = null;

            if (!registerHudMarker && !registeredListener)
            {
                m_depositGroupsByEntity.Clear();
                return;
            }

            SetRelayedRequest = false;

            var sphere = new BoundingSphereD(position.Invoke(), DetectionRadius);
            MyGamePruningStructure.GetAllVoxelMapsInSphere(ref sphere, m_inRangeCache);

            { // Find voxel maps which went out of range and then remove them.
                foreach (var voxelMap in m_depositGroupsByEntity.Keys)
                {
                    if (!m_inRangeCache.Contains(voxelMap))
                        m_notInRangeCache.Add(voxelMap);
                }
                foreach (var notInRange in m_notInRangeCache)
                {
                    m_depositGroupsByEntity.Remove(notInRange);
                }
                m_notInRangeCache.Clear();
            }

            { // Add voxel maps which came into range.
                foreach (var voxelMap in m_inRangeCache)
                {
                    if (!m_depositGroupsByEntity.ContainsKey(voxelMap))
                        m_depositGroupsByEntity.Add(voxelMap, new MyOreDepositGroup(voxelMap));
                }
                m_inRangeCache.Clear();
            }

            Dictionary<Vector3D, byte> oreLocation = registeredListener
                                                        ? new Dictionary<Vector3D, byte>(m_lastOreLocation == null ? 0 : m_lastOreLocation.Count) 
                                                        : null;
            bool force = oreLocation != null && m_lastOreLocation == null;

            // Update deposit queries using current detection sphere.
            foreach (MyOreDepositGroup group in m_depositGroupsByEntity.Values)
            {
                group.UpdateDeposits(ref sphere, force);

                foreach (MyEntityOreDeposit deposit in group.Deposits)
                {
                    if (deposit != null)
                    {
                        if (registerHudMarker)
                            MyHud.OreMarkers.RegisterMarker(deposit);

                        if (oreLocation != null)
                        {
                            foreach (MyEntityOreDeposit.Data ore in deposit.Materials)
                            {
                                Vector3D oreWorldPosition;
                                ore.ComputeWorldPosition(deposit.VoxelMap, out oreWorldPosition);
                                oreLocation.Add(oreWorldPosition, ore.Material.Index);
                            }
                        }
                    }
                }
            }

            if (oreLocation != null)
            {
                if (oreLocation.Count == 0)
                {
                    if (m_lastOreLocation != null)
                        oreLocation = m_lastOreLocation;
                }
                else
                    m_lastOreLocation = oreLocation;

                if (oreLocation.Count > 0 && OnOreUpdated != null)
                        OnOreUpdated(new ReadOnlyDictionary<Vector3D, byte>(oreLocation));
            }

            m_inRangeCache.Clear();
        }

        public void Clear()
        {
            m_lastOreLocation = null;
            foreach (var group in m_depositGroupsByEntity.Values)
            {
                foreach (var deposit in group.Deposits)
                {
                    if (deposit != null)
                        MyHud.OreMarkers.UnregisterMarker(deposit);
                }
            }
        }

    }

    /// <summary>
    /// This is not in Sandbox.Engine.Voxels as I consider it gameplay related,
    /// rather than voxel engine functionality.
    /// </summary>
    class MyDepositQuery : IPrioritizedWork
    {
        public struct Args
        {
            public Vector3I Cell;
            public MyVoxelBase VoxelMap;
            public Action<Vector3I, MyEntityOreDeposit> CompletionCallback;
        }

        struct MaterialPositionData
        {
            public Vector3 Sum;
            public int Count;
        }

        private static readonly MyDynamicObjectPool<MyDepositQuery> m_instancePool = new MyDynamicObjectPool<MyDepositQuery>(16);

        [ThreadStatic]
        private static MyStorageDataCache m_cache;
        private static MyStorageDataCache Cache
        {
            get
            {
                if (m_cache == null)
                    m_cache = new MyStorageDataCache();
                return m_cache;
            }
        }

        [ThreadStatic]
        private static MaterialPositionData[] m_materialData;
        private static MaterialPositionData[] MaterialData
        {
            get
            {
                if (m_materialData == null)
                    m_materialData = new MaterialPositionData[byte.MaxValue];
                return m_materialData;
            }
        }

        private Args m_args;

        private MyEntityOreDeposit m_result;

        private Action m_onComplete;

        public MyDepositQuery()
        {
            m_onComplete = OnComplete;
        }

        public static void Start(Args args)
        {
            var job = m_instancePool.Allocate();
            job.m_args = args;
            Parallel.Start(job, job.m_onComplete);
        }

        private void OnComplete()
        {
            m_args.CompletionCallback(m_args.Cell, m_result);
            m_args = default(Args);
            m_result = null;
            m_instancePool.Deallocate(this);
        }

        WorkPriority IPrioritizedWork.Priority
        {
            get { return WorkPriority.VeryLow; }
        }

        void IWork.DoWork()
        {
            ProfilerShort.Begin("MyDepositQuery.DoWork");
            try
            {
                var storage = m_args.VoxelMap.Storage;
                if (storage == null)
                    return; // voxel map was probably closed in the meantime.

                var cache = Cache;
                cache.Resize(new Vector3I(MyOreDetectorComponent.CELL_SIZE_IN_LOD_VOXELS));
                var min = m_args.Cell << MyOreDetectorComponent.CELL_SIZE_IN_VOXELS_BITS;
                var max = min + (MyOreDetectorComponent.CELL_SIZE_IN_LOD_VOXELS - 1);
                storage.ReadRange(cache, MyStorageDataTypeFlags.Content, MyOreDetectorComponent.QUERY_LOD, ref min, ref max);
                if (!cache.ContainsVoxelsAboveIsoLevel())
                    return;

                var materialData = MaterialData;
                storage.ReadRange(cache, MyStorageDataTypeFlags.Material, MyOreDetectorComponent.QUERY_LOD, ref min, ref max);
                Vector3I c;
                for (c.Z = 0; c.Z < MyOreDetectorComponent.CELL_SIZE_IN_LOD_VOXELS; ++c.Z)
                for (c.Y = 0; c.Y < MyOreDetectorComponent.CELL_SIZE_IN_LOD_VOXELS; ++c.Y)
                for (c.X = 0; c.X < MyOreDetectorComponent.CELL_SIZE_IN_LOD_VOXELS; ++c.X)
                {
                    int i = cache.ComputeLinear(ref c);
                    if (cache.Content(i) > MyVoxelConstants.VOXEL_ISO_LEVEL)
                    {
                        const float VOXEL_SIZE = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << MyOreDetectorComponent.QUERY_LOD);
                        const float VOXEL_SIZE_HALF = VOXEL_SIZE * 0.5f;
                        var material = cache.Material(i);
                        Vector3D localPos = (c + min) * VOXEL_SIZE + VOXEL_SIZE_HALF;
                        materialData[material].Sum += localPos;
                        materialData[material].Count += 1;
                    }
                }

                for (int materialIdx = 0; materialIdx < materialData.Length; ++materialIdx)
                {
                    if (materialData[materialIdx].Count == 0)
                        continue;

                    var material = MyDefinitionManager.Static.GetVoxelMaterialDefinition((byte)materialIdx);
                    if (material.IsRare)
                    {
                        if (m_result == null)
                            m_result = new MyEntityOreDeposit(m_args.VoxelMap, m_args.Cell);

                        m_result.Materials.Add(new MyEntityOreDeposit.Data()
                        {
                            Material = material,
                            AverageLocalPosition = materialData[materialIdx].Sum / materialData[materialIdx].Count,
                        });
                    }
                }
                Array.Clear(materialData, 0, materialData.Length);
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        WorkOptions IWork.Options
        {
            get { return Parallel.DefaultOptions; }
        }
    }


}
