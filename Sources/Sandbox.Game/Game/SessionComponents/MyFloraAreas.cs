using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.Models;
using VRage.Library.Utils;
using VRage.Profiler;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using ItemInfo = Sandbox.Game.Entities.EnvironmentItems.MyEnvironmentItems.ItemInfo;

namespace Sandbox.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MyFloraAreas : MySessionComponentBase
    {
        public struct AreaData
        {
            public readonly int Id;
            public readonly BoundingBoxD ForestBox;
            public readonly DictionaryReader<long, HashSetReader<int>> ItemIds;

            public AreaData(int id, BoundingBoxD box, Dictionary<long, HashSet<int>> items)
            {
                Id = id;
                ForestBox = box;
                ItemIds = DictionaryReader<long, HashSetReader<int>>.Empty;

                var dictionary = new Dictionary<long, HashSetReader<int>>();
                foreach (var pair in items)
                    dictionary[pair.Key] = new HashSetReader<int>(pair.Value);
                ItemIds = new DictionaryReader<long, HashSetReader<int>>(dictionary);
            }
        }

        private class Area
        {
            public int ProxyId { get; set; }
            public BoundingBoxD ForestBox;
            public Dictionary<long, HashSet<int>> ItemIds;
            public bool IsFull { get; set; }

            public Area()
            {
                ProxyId = -1;
                ForestBox = BoundingBoxD.CreateInvalid();
                ItemIds = new Dictionary<long, HashSet<int>>();
                IsFull = false;
            }

            public bool IsValid
            {
                get
                {
                    if (ItemIds == null)
                        return false;
                    foreach (var pair in ItemIds)
                        if (pair.Value.Count > 0)
                            return true;
                    return false;
                }
            }

            public void AddItem(long entityId, int localId)
            {
                if (!ItemIds.ContainsKey(entityId))
                    ItemIds[entityId] = new HashSet<int>();
                ItemIds[entityId].Add(localId);
                IsFull = false;

                var handler = MyFloraAreas.ItemAddedToArea;
                if (handler != null)
                    handler(ProxyId);
            }

            public void RemoveItem(long entityId, int localId)
            {
                ItemIds[entityId].Remove(localId);
                IsFull = false;
            }

            public void Merge(BoundingBoxD mergedBox, Area area)
            {
                ForestBox = mergedBox;
                foreach (var entityId in area.ItemIds.Keys)
                {
                    if (!ItemIds.ContainsKey(entityId))
                        ItemIds.Add(entityId, area.ItemIds[entityId]);
                    else
                        ItemIds[entityId].UnionWith(area.ItemIds[entityId]);
                }
                IsFull = false;

                area.ForestBox = BoundingBoxD.CreateInvalid();
                area.ItemIds = null;

                var handler = MyFloraAreas.ItemAddedToArea;
                if (handler != null)
                    handler(ProxyId);
            }

            public void Clean()
            {
                ProxyId = -1;
                ForestBox = BoundingBoxD.CreateInvalid();
                ItemIds = null;
                IsFull = false;
            }

            public AreaData GetAreaData()
            {
                return new AreaData(ProxyId, ForestBox, ItemIds);
            }
        }

        private static MyFloraAreas Static;

        private static readonly double DEBUG_BOX_Y_MAX_POS = 500.25;
        private static readonly double DEBUG_BOX_Y_MIN_POS = 500;
        private static readonly Vector3D DEFAULT_INFLATE_VALUE = new Vector3D(20, 0, 20);

        private double BOX_INCLUDE_DIST;
        private double BOX_INCLUDE_DIST_SQ;
        private BoundingBoxD DEFAULT_BOX;

        private bool m_loadPhase;
        private bool m_findValidForestPhase;
        private int m_updateCounter;

        private MyVoxelBase m_ground;
        private MyStorageData m_voxelCache;
        private HashSet<MyStringHash> m_allowedMaterials;

        private double m_worldArea;
        private double m_currentForestArea;
        private double m_forestsPercent;

        private Dictionary<long, MyEnvironmentItems> m_envItems;
        private List<Area> m_forestAreas;
        private List<BoundingBoxD> m_highLevelBoxes;
        private Queue<Vector3D> m_initialForestLocations;

        private int m_hlCurrentBox;
        private int m_hlSelectionCounter;
        private double m_hlSize;

        private HashSet<Vector3I> m_checkedSectors;
        private Queue<long> m_checkQueue;

        private MyDynamicAABBTreeD m_aabbTree;

        private const int INVALIDATE_TIME = 3; // minutes
        private MyTimeSpan m_invalidateAreaTimer;
        private bool m_immediateInvalidate = false;

        private List<ItemInfo> m_tmpItemInfos;
        private List<Area> m_tmpAreas, m_tmpAreas2;
        private List<Vector3I> m_tmpSectors;
        private List<Vector3D> d_foundEnrichingPoints = new List<Vector3D>();
        private List<Vector3D> d_foundEnlargingPoints = new List<Vector3D>();

        public static event Action LoadFinished;
        public static event Action<int> ItemAddedToArea;
        public static event Action<int> SelectedArea;
        public static event Action<int> RemovedArea;

        public static double ForestsPercent
        {
            get
            {
                return Static.m_forestsPercent;
            }
        }
        public static double WorldArea
        {
            get
            {
                return Static.m_worldArea;
            }
        }

        public static double FullAreasRatio
        {
            get
            {
                int counter = 0;
                foreach (var area in Static.m_forestAreas)
                {
                    if (area.IsFull)
                        counter++;
                }
                return counter / (double)Static.m_forestAreas.Count;
            }
        }

        public MyFloraAreas()
        {
            BOX_INCLUDE_DIST = 5;
            BOX_INCLUDE_DIST_SQ = BOX_INCLUDE_DIST * BOX_INCLUDE_DIST;
            var boxBounds = new Vector3D(0.25, 0.25, 0.25);
            DEFAULT_BOX = new BoundingBoxD(-boxBounds, boxBounds);
        }

        public override bool IsRequiredByGame
        {
            get
            {
                return MyPerGameSettings.Game == GameEnum.ME_GAME && Sync.IsServer;
            }
        }

        public override void LoadData()
        {
            base.LoadData();

            m_updateCounter = 0;

            m_envItems = new Dictionary<long, MyEnvironmentItems>(10);
            m_forestAreas = new List<Area>(100);
            m_highLevelBoxes = new List<BoundingBoxD>();
            m_tmpItemInfos = new List<ItemInfo>(500);
            m_tmpAreas = new List<Area>();
            m_tmpAreas2 = new List<Area>();
            m_checkedSectors = new HashSet<Vector3I>();
            m_checkQueue = new Queue<long>();
            m_initialForestLocations = new Queue<Vector3D>();
            m_tmpSectors = new List<Vector3I>();

            m_aabbTree = new MyDynamicAABBTreeD(Vector3D.Zero);

            // MW:TODO growing items on allowed materials
            m_allowedMaterials = new HashSet<MyStringHash>();

            m_loadPhase = true;
            m_findValidForestPhase = false;

            MyEntities.OnEntityAdd += MyEntities_OnEntityAdd;
            MyEntities.OnEntityRemove += MyEntities_OnEntityRemove;
            Static = this;
        }

        public override void BeforeStart()
        {
            base.BeforeStart();
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            Static = null;
            m_aabbTree.Clear();
            foreach (var item in m_envItems)
            {
                item.Value.ItemAdded -= item_ItemAdded;
                item.Value.ItemRemoved -= item_ItemRemoved;
            }

            MyEntities.OnEntityAdd -= MyEntities_OnEntityAdd;
            MyEntities.OnEntityRemove -= MyEntities_OnEntityRemove;
        }

        void MyEntities_OnEntityAdd(MyEntity obj)
        {
            if (obj is MyEnvironmentItems)
            {
                AddEnvironmentItem(obj as MyEnvironmentItems);
            }
        }

        void MyEntities_OnEntityRemove(MyEntity obj)
        {
            if (obj is MyEnvironmentItems)
            {
                RemoveEnvironmentItem(obj as MyEnvironmentItems);
            }
        }

        public void AddEnvironmentItem(MyEnvironmentItems item)
        {
            item.OnMarkForClose += item_OnMarkForClose;
            m_envItems.Add(item.EntityId, item);
            m_checkQueue.Enqueue(item.EntityId);
            item.ItemRemoved += item_ItemRemoved;
            item.ItemAdded += item_ItemAdded;
            m_loadPhase = true;
        }

        void item_OnMarkForClose(MyEntity obj)
        {
            var envItems = obj as MyEnvironmentItems;
            RemoveEnvironmentItem(envItems);
        }

        private void RemoveEnvironmentItem(MyEnvironmentItems item)
        {
            if (!m_envItems.ContainsKey(item.EntityId))
                return;

            m_envItems.Remove(item.EntityId);
            item.OnMarkForClose -= item_OnMarkForClose;
            item.ItemAdded -= item_ItemAdded;
            item.ItemRemoved -= item_ItemRemoved;

            foreach (var area in m_forestAreas)
            {
                if (area.IsValid)
                    area.ItemIds.Remove(item.EntityId);
            }

            m_immediateInvalidate = true;
        }

        public override void UpdateBeforeSimulation()
        {
            if (m_immediateInvalidate)
            {
                UpdateAreas();
            }

            const int updateFreq = 10;
            if ((++m_updateCounter) % updateFreq == 0)
            {
                if (m_loadPhase)
                {
                    ProfilerShort.Begin("Update load");
                    if (!m_findValidForestPhase)
                        UpdateLoad();
                    else
                        UpdateFindCandidates();
                    ProfilerShort.End();
                }
                else if (m_ground != null)
                {
                    ProfilerShort.Begin("Update areas");
                    UpdateAreas();
                    ProfilerShort.End();
                }
            }

            DebugDraw();
        }

        private void UpdateLoad()
        {
            ConstructAreas();

            if (m_checkQueue.Count == 0)
            {
                bool finishedLoading = true;
                m_ground = MySession.Static.VoxelMaps.TryGetVoxelMapByNameStart("Ground");
                if (m_ground != null)
                {
                    m_worldArea = m_ground.SizeInMetres.X * m_ground.SizeInMetres.Z;
                    m_voxelCache = new MyStorageData();
                    m_voxelCache.Resize(Vector3I.One * 3);

                    InvalidateAreaValues();

                    if (m_highLevelBoxes.Count == 0)
                    {
                        // MW: we need to find some candidates for forest starting points
                        finishedLoading = false;
                        m_findValidForestPhase = true;
                    }
                }

                if (finishedLoading)
                {
                    m_loadPhase = false;
                    if (LoadFinished != null)
                        LoadFinished();
                }
            }
        }

        private void UpdateFindCandidates()
        {
            FindForestInitialCandidate();

            if (m_initialForestLocations.Count == 5)
            {
                m_loadPhase = false;
                m_findValidForestPhase = false;
                if (LoadFinished != null)
                    LoadFinished();
            }
        }

        private void FindForestInitialCandidate()
        {
            BoundingBoxD groundBox = m_ground.PositionComp.WorldAABB;
            Vector3D boxSize = groundBox.Size;
            boxSize *= 0.1f;
            groundBox.Inflate(-boxSize);
            MyBBSetSampler sampler = new MyBBSetSampler(groundBox.Min, groundBox.Max);

            bool posIsValid = true;
            Vector3D worldPos = default(Vector3D);
            int counter = 0;
            do
            {
                // find random position for starting 
                worldPos = sampler.Sample();
                var worldPosProjected = worldPos;
                worldPosProjected.Y = 0.5f;
                posIsValid = true;
                counter++;
                Vector3D areaCheck = new Vector3D(20, 20, 20);
                foreach (var enqueued in m_initialForestLocations)
                {
                    // only interested in XZ plane
                    BoundingBoxD tmp = new BoundingBoxD(enqueued - areaCheck, enqueued + areaCheck);
                    tmp.Min.Y = 0;
                    tmp.Max.Y = 1;

                    if (tmp.Contains(worldPosProjected) == ContainmentType.Contains)
                    {
                        posIsValid = false;
                        break;
                    }
                }
            } while (!posIsValid && counter != 10);

            if (!posIsValid)
            {
                // could not find any position
                return;
            }

            var lineStart = new Vector3D(worldPos.X, groundBox.Max.Y, worldPos.Z);
            var lineEnd = new Vector3D(worldPos.X, groundBox.Min.Y, worldPos.Z);
            LineD line = new LineD(lineStart, lineEnd);
            VRage.Game.Models.MyIntersectionResultLineTriangleEx? result = null;
            var correctGroundDefinition = MyDefinitionManager.Static.GetVoxelMaterialDefinition("Grass");
            var materialId = correctGroundDefinition.Index;

            if (m_ground.GetIntersectionWithLine(ref line, out result, VRage.Game.Components.IntersectionFlags.DIRECT_TRIANGLES))
            {
                Vector3D intersectionPoint = result.Value.IntersectionPointInWorldSpace;
                Vector3I voxelCoord, minRead, maxRead;
                MyVoxelCoordSystems.WorldPositionToVoxelCoord(m_ground.PositionLeftBottomCorner, ref intersectionPoint, out voxelCoord);
                minRead = voxelCoord - Vector3I.One;
                maxRead = voxelCoord + Vector3I.One;
                m_ground.Storage.ReadRange(m_voxelCache, MyStorageDataTypeFlags.Material, 0, ref minRead, ref maxRead);

                var minLocal = Vector3I.Zero;
                var maxLocal = Vector3I.One * 2;
                var it = new Vector3I_RangeIterator(ref minLocal, ref maxLocal);
                while (it.IsValid())
                {
                    var vec = it.Current;
                    var material = m_voxelCache.Material(ref vec);
                    if (material == materialId)
                    {
                        // found a location
                        var desired = voxelCoord - Vector3I.One + vec;
                        Vector3D desiredWorldPosition = default(Vector3D);
                        MyVoxelCoordSystems.VoxelCoordToWorldPosition(m_ground.PositionLeftBottomCorner, ref desired, out desiredWorldPosition);
                        m_initialForestLocations.Enqueue(desiredWorldPosition);
                        break;
                    }

                    it.MoveNext();
                }
            }
        }

        private void UpdateAreas()
        {
            ProfilerShort.Begin("Invalidate areas");
            if ((m_invalidateAreaTimer - MySandboxGame.Static.UpdateTime).Seconds < 0 || m_immediateInvalidate)
            {
                InvalidateAreaValues();
            }
            ProfilerShort.End();
        }

        private void item_ItemAdded(MyEnvironmentItems envItems, ItemInfo itemInfo)
        {
            m_checkedSectors.Add(envItems.GetSectorId(ref itemInfo.Transform.Position));

            var itemBox = GetWorldBox(itemInfo.SubtypeId, itemInfo.Transform.TransformMatrix);
            var checkBox = itemBox.GetInflated(DEFAULT_INFLATE_VALUE);
            m_aabbTree.OverlapAllBoundingBox(ref checkBox, m_tmpAreas);

            var newForestBox = new Area();
            newForestBox.ForestBox = itemBox;
            newForestBox.AddItem(envItems.EntityId, itemInfo.LocalId);
            newForestBox.ProxyId = m_aabbTree.AddProxy(ref itemBox, newForestBox, 0);
            m_tmpAreas.Add(newForestBox);

            MergeAreas(m_tmpAreas);

            if (newForestBox.IsValid)
                m_forestAreas.Add(newForestBox);

            m_tmpAreas.Clear();
        }

        private void item_ItemRemoved(MyEnvironmentItems item, ItemInfo itemInfo)
        {
            var entityId = item.EntityId;
            int i = 0;
            while (i < m_forestAreas.Count)
            {
                var area = m_forestAreas[i];
                if (area.IsValid && area.ItemIds.ContainsKey(entityId) && area.ItemIds[entityId].Contains(itemInfo.LocalId))
                {
                    area.ItemIds[entityId].Remove(itemInfo.LocalId);
                    if (area.IsValid)
                    {
                        InvalidateArea(area);
                        i++;
                    }
                    else
                    {
                        RemoveArea(m_forestAreas, i);
                    }
                }
                else
                {
                    i++;
                }
            }
        }

        private void ConstructAreas()
        {
            bool found = false;
            Vector3D sectorPosition = default(Vector3D);

            while (!found && m_checkQueue.Count > 0)
            {
                var currentEnvItem = m_envItems[m_checkQueue.Peek()];
                foreach (var sector in currentEnvItem.Sectors)
                {
                    if (!m_checkedSectors.Contains(sector.Key))
                    {
                        m_checkedSectors.Add(sector.Key);
                        sectorPosition = sector.Key * currentEnvItem.Definition.SectorSize + currentEnvItem.Definition.SectorSize * 0.5f;
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    ProfilerShort.Begin("Distribute items");
                    foreach (var envItem in m_envItems.Values)
                        DistributeItems(envItem, ref sectorPosition);
                    ProfilerShort.End();
                }
                else
                {
                    m_checkQueue.Dequeue();
                }
            }

            ProfilerShort.Begin("Find merge areas");
            if (found)
            {
                BoundingBoxD mergeBox = BoundingBoxD.CreateInvalid();
                foreach (var envItem in m_envItems.Values)
                {
                    var sector = envItem.GetSector(ref sectorPosition);
                    if (sector != null && sector.IsValid)
                        mergeBox.Include(sector.SectorWorldBox);
                }
                mergeBox.Min.Y = DEBUG_BOX_Y_MIN_POS;
                mergeBox.Max.Y = DEBUG_BOX_Y_MAX_POS;
                mergeBox = mergeBox.Inflate(new Vector3D(100, 0, 100));

                m_aabbTree.OverlapAllBoundingBox(ref mergeBox, m_tmpAreas);

                ProfilerShort.Begin("Merge areas");
                MergeAreas(m_tmpAreas);
                m_tmpAreas.Clear();
                ProfilerShort.End();

                ProfilerShort.Begin("Clear invalid areas");
                ClearInvalidAreas(m_forestAreas);
                ProfilerShort.End();
            }
            ProfilerShort.End();
        }

        private void ClearInvalidAreas(List<Area> areas)
        {
            int i = 0;
            while (i < areas.Count)
            {
                if (!areas[i].IsValid)
                    RemoveArea(areas, i);
                else
                    i++;
            }
        }

        private void InvalidateArea(Area area)
        {
            BoundingBoxD box = BoundingBoxD.CreateInvalid();
            foreach (var item in area.ItemIds)
            {
                foreach (var localId in item.Value)
                {
                    ItemInfo itemInfo;
                    if (m_envItems[item.Key].TryGetItemInfoById(localId, out itemInfo))
                        box.Include(GetWorldBox(itemInfo.SubtypeId, itemInfo.Transform.TransformMatrix));
                }
            }
            area.ForestBox = box;
            if (area.ProxyId != -1)
                m_aabbTree.MoveProxy(area.ProxyId, ref box, Vector3D.Zero);
        }

        public static void ImmediateInvalidateAreaValues()
        {
            Static.InvalidateAreaValues();
        }

        private void InvalidateAreaValues()
        {
            // some areas could have been merged by adding new items in their borders. Remove them if they are invalid
            ClearInvalidAreas(m_forestAreas);

            m_currentForestArea = CalculateEntireArea();
            m_forestsPercent = m_currentForestArea / m_worldArea;
            if (m_currentForestArea == 0)
                m_invalidateAreaTimer = MyTimeSpan.Zero;
            else
                m_invalidateAreaTimer = MySandboxGame.Static.UpdateTime + MyTimeSpan.FromSeconds(INVALIDATE_TIME * 60);
            m_immediateInvalidate = false;
        }

        private double CalculateEntireArea()
        {
            m_highLevelBoxes.Clear();
            CreateHighLevelBoxes(m_highLevelBoxes);

            double area = 0;
            foreach (var box in m_highLevelBoxes)
            {
                var size = box.Size;
                area += size.X * size.Z;
            }

            return area;
        }

        private BoundingBoxD GetWorldBox(MyStringHash id, MatrixD worldMatrix)
        {
            int modelId = MyEnvironmentItems.GetModelId(id);
            var modelData = VRage.Game.Models.MyModels.GetModelOnlyData(MyModel.GetById(modelId));
            BoundingBoxD boxLocal = modelData.BoundingBox.Transform(ref worldMatrix);
            boxLocal.Inflate(0.6); // inflate so it takes a lil bit more space than normally
            boxLocal.Max.Y = DEBUG_BOX_Y_MAX_POS;
            boxLocal.Min.Y = DEBUG_BOX_Y_MIN_POS;
            return boxLocal;
        }

        private void DistributeItems(MyEnvironmentItems envItem, ref Vector3D sectorPosition)
        {
            envItem.GetItemsInSector(ref sectorPosition, m_tmpItemInfos);

            var envItemId = envItem.EntityId;
            foreach (var itemInfo in m_tmpItemInfos)
            {
                bool included = false;
                var worldBox = GetWorldBox(itemInfo.SubtypeId, itemInfo.Transform.TransformMatrix);
                var extended = worldBox.GetInflated(DEFAULT_INFLATE_VALUE);
                m_aabbTree.OverlapAllBoundingBox(ref extended, m_tmpAreas);

                for (int i = 0; i < m_tmpAreas.Count; i++)
                {
                    var forestBox = m_tmpAreas[i].ForestBox;
                    if ((forestBox.Center - worldBox.Center).LengthSquared() <= BOX_INCLUDE_DIST_SQ)
                    {
                        forestBox.Include(ref worldBox);
                        m_tmpAreas[i].ForestBox = forestBox;
                        m_tmpAreas[i].AddItem(envItemId, itemInfo.LocalId);
                        included = true;
                        m_aabbTree.MoveProxy(m_tmpAreas[i].ProxyId, ref worldBox, Vector3D.Zero);
                    }
                }

                m_tmpAreas.Clear();

                if (!included)
                {
                    var newForestBox = new Area();
                    newForestBox.ForestBox = worldBox;
                    newForestBox.AddItem(envItemId, itemInfo.LocalId);
                    newForestBox.ProxyId = m_aabbTree.AddProxy(ref worldBox, newForestBox, 0);
                    m_forestAreas.Add(newForestBox);
                }
            }

            m_tmpItemInfos.Clear();
        }

        private void MergeAreas(List<Area> areas, int multiplier = 1)
        {
            int smallFavor = 5 * multiplier;
            double smallMaxTolerance = 2.5 * multiplier;
            double toleranceMultiplier = 0.3 * multiplier;

            int i = 0;
            while (i < areas.Count)
            {
                var area1 = areas[i];
                if (!area1.IsValid)
                {
                    RemoveArea(areas, i);
                    continue;
                }

                var inflated = area1.ForestBox.GetInflated(DEFAULT_INFLATE_VALUE);
                m_aabbTree.OverlapAllBoundingBox(ref inflated, m_tmpAreas2);

                int j = 0;
                bool removeIdx = false;
                while (j < m_tmpAreas2.Count)
                {
                    var area2 = m_tmpAreas2[j];
                    if (area1 == area2)
                    {
                        j++;
                        continue;
                    }
                    else if (!area2.IsValid)
                    {
                        RemoveArea(m_tmpAreas2, j);
                        continue;
                    }

                    var box1 = area1.ForestBox;
                    var box2 = area2.ForestBox;
                    var output = box1;
                    output.Include(box2);

                    var box1Volume = box1.Volume;
                    var box2Volume = box2.Volume;
                    var outputVolume = output.Volume;

                    float tolerance = 0;
                    if (box1Volume > box2Volume)
                    {
                        if (box1.Contains(box2) == ContainmentType.Contains)
                        {
                            RemoveArea(m_tmpAreas2, j);
                            continue;
                        }

                        tolerance = (float)(1 + toleranceMultiplier * (box2Volume / box1Volume));
                    }
                    else
                    {
                        if (box2.Contains(box1) == ContainmentType.Contains)
                        {
                            removeIdx = true;
                            break;
                        }

                        tolerance = (float)(1 + toleranceMultiplier * (box1Volume / box2Volume));
                    }

                    if (outputVolume < smallFavor)
                        tolerance = (float)Math.Min(smallMaxTolerance, tolerance * (smallFavor / outputVolume));

                    if ((box1Volume + box2Volume) * tolerance > outputVolume)
                    {
                        area1.Merge(output, area2);
                        RemoveArea(m_tmpAreas2, j);
                        m_aabbTree.MoveProxy(area1.ProxyId, ref output, Vector3D.Zero);
                    }
                    else
                    {
                        j++;
                    }
                }

                if (removeIdx)
                    RemoveArea(areas, i);
                else
                    i++;

                m_tmpAreas2.Clear();
            }
        }

        private void RemoveArea(List<Area> areas, int idx)
        {
            var area = areas[idx];
            var proxyId = area.ProxyId;
            areas.RemoveAtFast(idx);
            if (proxyId != -1)
                m_aabbTree.RemoveProxy(proxyId);
            area.Clean();

            var handler = RemovedArea;
            if (handler != null)
                RemovedArea(proxyId);
        }

        private void CreateHighLevelBoxes(List<BoundingBoxD> boxes)
        {
            foreach (var sector in m_checkedSectors)
            {
                var sectorId = sector;
                BoundingBoxD max = BoundingBoxD.CreateInvalid();
                bool includedOnce = false;
                foreach (var envItem in m_envItems)
                {
                    var sec = envItem.Value.GetSector(ref sectorId);
                    if (sec != null && sec.IsValid)
                    {
                        includedOnce = true;
                        max.Include((BoundingBoxD)(sec.SectorWorldBox));   
                    }
                }

                if (!includedOnce)
                {  // MW: invalid sector -> remove it
                    m_tmpSectors.Add(sectorId);
                    continue;
                }

                max.Max.Y = DEBUG_BOX_Y_MAX_POS;
                max.Min.Y = DEBUG_BOX_Y_MIN_POS;

                List<BoundingBoxD> tmp = new List<BoundingBoxD>() { max };
                double sectorAverageVolume = 0;
                foreach (var box in tmp)
                {
                    sectorAverageVolume += box.Volume;
                }
                sectorAverageVolume /= tmp.Count;
                sectorAverageVolume = Math.Min(sectorAverageVolume, 10);

                int i = 0;
                while (i < boxes.Count)
                {
                    var box = boxes[i];
                    bool remove = false;
                    int j = 0;

                    while (j < tmp.Count)
                    {
                        var tmpMax = tmp[j];
                        var tmpInflated = tmpMax.Inflate(new Vector3D(-0.01, 0, -0.01));
                        if (box.Intersects(ref tmpInflated))
                        {
                            if (box.Contains(tmpMax) == ContainmentType.Contains)
                            {
                                tmp.RemoveAtFast(j);
                                continue;
                            }
                            else if (tmpMax.Contains(box) == ContainmentType.Contains)
                            {
                                remove = true;
                                break;
                            }

                            tmp.RemoveAtFast(j);

                            SplitArea(box, tmpMax, tmp);
                        }
                        else
                            j++;
                    }

                    if (remove)
                        boxes.RemoveAtFast(i);
                    else
                        i++;
                }

                int idx = 0;
                while (idx < tmp.Count)
                {
                    var box = tmp[idx];
                    if (box.Volume < sectorAverageVolume)
                        tmp.RemoveAtFast(idx);
                    else
                        idx++;
                }

                boxes.AddList(tmp);
            }

            foreach (var sector in m_tmpSectors)
                m_checkedSectors.Remove(sector);
            m_tmpSectors.Clear();

            if (boxes.Count > 0)
            {
                boxes.Sort((x, y) => y.Volume.CompareTo(x.Volume));
                m_hlCurrentBox = 0;
                m_hlSize = boxes.Average(x => x.Volume);
                m_hlSelectionCounter = (int)(boxes.First().Volume / m_hlSize);
            }
        }

        private void SplitArea(BoundingBoxD box1, BoundingBoxD box2, List<BoundingBoxD> output)
        {
            var intersectValue = box1.Intersect(box2);

            double x1 = Math.Min(intersectValue.Min.X, box2.Min.X);
            double x2 = intersectValue.Min.X;
            double x3 = intersectValue.Max.X;
            double x4 = Math.Max(intersectValue.Max.X, box2.Max.X);

            double z1 = Math.Min(intersectValue.Min.Z, box2.Min.Z);
            double z2 = intersectValue.Min.Z;
            double z3 = intersectValue.Max.Z;
            double z4 = Math.Max(intersectValue.Max.Z, box2.Max.Z);

            bool sameXMin = x1 == x2;
            bool sameXMax = x3 == x4;
            bool sameZMin = z1 == z2;
            bool sameZMax = z3 == z4;

            double minY = DEBUG_BOX_Y_MIN_POS;
            double maxY = DEBUG_BOX_Y_MAX_POS;

            if (sameXMin && sameXMax)
            {
                if (sameZMin && !sameZMax)
                {
                    output.Add(new BoundingBoxD(new Vector3D(x1, minY, z3), new Vector3D(x4, maxY, z4)));
                }
                else if (!sameZMin && sameZMax)
                {
                    output.Add(new BoundingBoxD(new Vector3D(x1, minY, z1), new Vector3D(x4, maxY, z2)));
                }
                else
                {
                    output.Add(new BoundingBoxD(new Vector3D(x1, minY, z1), new Vector3D(x4, maxY, z2)));
                    output.Add(new BoundingBoxD(new Vector3D(x1, minY, z3), new Vector3D(x4, maxY, z4)));
                }
            }
            else if (sameZMin && sameZMax)
            {
                if (sameXMin && !sameXMax)
                {
                    output.Add(new BoundingBoxD(new Vector3D(x3, minY, z1), new Vector3D(x4, maxY, z4)));
                }
                else if (!sameXMin && sameXMax)
                {
                    output.Add(new BoundingBoxD(new Vector3D(x1, minY, z1), new Vector3D(x2, maxY, z2)));
                }
                else
                {
                    output.Add(new BoundingBoxD(new Vector3D(x1, minY, z1), new Vector3D(x2, maxY, z4)));
                    output.Add(new BoundingBoxD(new Vector3D(x3, minY, z1), new Vector3D(x4, maxY, z4)));
                }
            }
            else
            {
                if (sameXMin)
                {
                    if (sameZMin)
                    {
                        output.Add(new BoundingBoxD(new Vector3D(x3, minY, z1), new Vector3D(x4, maxY, z3)));
                        output.Add(new BoundingBoxD(new Vector3D(x1, minY, z3), new Vector3D(x4, maxY, z4)));
                    }
                    else if (sameZMax)
                    {
                        output.Add(new BoundingBoxD(new Vector3D(x1, minY, z1), new Vector3D(x4, maxY, z2)));
                        output.Add(new BoundingBoxD(new Vector3D(x3, minY, z2), new Vector3D(x4, maxY, z4)));
                    }
                    else
                    {
                        output.Add(new BoundingBoxD(new Vector3D(x1, minY, z1), new Vector3D(x4, maxY, z2)));
                        output.Add(new BoundingBoxD(new Vector3D(x3, minY, z2), new Vector3D(x4, maxY, z3)));
                        output.Add(new BoundingBoxD(new Vector3D(x1, minY, z3), new Vector3D(x4, maxY, z4)));
                    }
                }
                else if (sameXMax)
                {
                    if (sameZMin)
                    {
                        output.Add(new BoundingBoxD(new Vector3D(x1, minY, z1), new Vector3D(x2, maxY, z3)));
                        output.Add(new BoundingBoxD(new Vector3D(x1, minY, z3), new Vector3D(x4, maxY, z4)));
                    }
                    else if (sameZMax)
                    {
                        output.Add(new BoundingBoxD(new Vector3D(x1, minY, z1), new Vector3D(x4, maxY, z2)));
                        output.Add(new BoundingBoxD(new Vector3D(x1, minY, z2), new Vector3D(x4, maxY, z4)));
                    }
                    else
                    {
                        output.Add(new BoundingBoxD(new Vector3D(x1, minY, z1), new Vector3D(x4, maxY, z2)));
                        output.Add(new BoundingBoxD(new Vector3D(x1, minY, z2), new Vector3D(x3, maxY, z3)));
                        output.Add(new BoundingBoxD(new Vector3D(x1, minY, z3), new Vector3D(x4, maxY, z4)));
                    }
                }
                else if (sameZMin)
                {
                    output.Add(new BoundingBoxD(new Vector3D(x1, minY, z1), new Vector3D(x2, maxY, z4)));
                    output.Add(new BoundingBoxD(new Vector3D(x2, minY, z3), new Vector3D(x3, maxY, z4)));
                    output.Add(new BoundingBoxD(new Vector3D(x3, minY, z1), new Vector3D(x4, maxY, z4)));
                }
                else if (sameZMax)
                {
                    output.Add(new BoundingBoxD(new Vector3D(x1, minY, z1), new Vector3D(x2, maxY, z4)));
                    output.Add(new BoundingBoxD(new Vector3D(x2, minY, z1), new Vector3D(x3, maxY, z3)));
                    output.Add(new BoundingBoxD(new Vector3D(x3, minY, z1), new Vector3D(x4, maxY, z4)));
                }
                else
                {
                    MyDebug.AssertDebug(false);
                }
            }
        }

        private bool HasBelongingItemsInternal(int areaId, MyEnvironmentItems items)
        {    
            var area = m_aabbTree.GetUserData<Area>(areaId);
            if (!area.ItemIds.ContainsKey(items.EntityId))
                return false;
            return area.ItemIds[items.EntityId].Count > 0;
        }

        public static bool HasBelongingItems(int areaId, MyEnvironmentItems items)
        {
            return Static.HasBelongingItemsInternal(areaId, items);
        }

        public static bool TryFindLocationInsideForest(out Vector3D location, Predicate<AreaData> predicate = null)
        {
            return Static.TryFindLocationInsideForestInternal(null, out location, predicate);
        }

        public static bool TryFindLocationInsideForest(Vector3D desiredLocationSize, out Vector3D location, Predicate<AreaData> predicate = null)
        {
            return Static.TryFindLocationInsideForestInternal(desiredLocationSize, out location, predicate);
        }

        public static bool TryFindLocationOutsideForest(out Vector3D location, Predicate<AreaData> predicate = null)
        {
            return Static.TryFindLocationOutsideForestInternal(null, out location, predicate);
        }

        public static bool TryFindLocationOutsideForest(Vector3D desiredLocationSize, out Vector3D location, Predicate<AreaData> predicate = null)
        {
            return Static.TryFindLocationOutsideForestInternal(desiredLocationSize, out location, predicate);
        }

        private bool TryFindLocationInsideForestInternal(Vector3D? desiredLocationSize, out Vector3D location, Predicate<AreaData> predicate = null)
        {
            if (!TryGetRandomAreas(m_tmpAreas))
            {
                location = Vector3D.Zero;
                return false;
            }

            Vector3D desiredHalfSize = desiredLocationSize.HasValue ? desiredLocationSize.Value * 0.5f : Vector3D.Zero;
            desiredHalfSize.Y = 0;

            int areaIdx = 0;
            int randomStartIdx = MyUtils.GetRandomInt(m_tmpAreas.Count);
            while (areaIdx < m_tmpAreas.Count)
            {
                var spawnArea = m_tmpAreas[randomStartIdx];
                randomStartIdx = (randomStartIdx + 1) % m_tmpAreas.Count;
                areaIdx++;

                if (!spawnArea.IsValid || spawnArea.IsFull)
                    continue;

                if (predicate != null && !predicate(spawnArea.GetAreaData()))
                {
                    spawnArea.IsFull = true;
                    continue;
                }

                var spawnBox = spawnArea.ForestBox;
                MyBBSetSampler setSampler = new MyBBSetSampler(spawnBox.Min, spawnBox.Max);

                RefineSampler(spawnArea, ref spawnBox, ref desiredHalfSize, setSampler);

                if (!setSampler.Valid)
                    continue;

                Vector3D exactLocation;
                if (TryGetExactLocation(spawnArea, setSampler.Sample(), 10, out exactLocation))
                {
                    location = exactLocation;
                    d_foundEnrichingPoints.Add(exactLocation);
                    m_tmpAreas.Clear();

                    if (SelectedArea != null)
                        SelectedArea(spawnArea.ProxyId);

                    return true;
                }
            }

            location = Vector3D.Zero;
            m_tmpAreas.Clear();
            return false;
        }

        private bool TryFindLocationOutsideForestInternal(Vector3D? desiredLocationSize, out Vector3D location, Predicate<AreaData> predicate = null)
        {
            Vector3D desiredHalfSize = desiredLocationSize.HasValue ? desiredLocationSize.Value * 0.5f : Vector3D.Zero;
            desiredHalfSize.Y = 0;

            if (m_highLevelBoxes.Count == 0)
            {
                // no forest on the map, generate starting point
                bool valid = false;
                while (m_initialForestLocations.Count > 0 && !valid)
                {
                    var potentialTreePosition = m_initialForestLocations.Dequeue();
                    valid = true;
                    BoundingBoxD itemBox = new BoundingBoxD(potentialTreePosition, potentialTreePosition);
                    itemBox.Inflate(desiredHalfSize);

                    var entities = MyEntities.GetEntitiesInAABB(ref itemBox);
                    foreach (var entity in entities)
                    {
                        if (entity is MyEnvironmentItems)
                            continue;
                        if (entity is MyVoxelBase)
                            continue;
                        var entityBox = entity.PositionComp.WorldAABB;
                        var containment = entityBox.Intersects(itemBox);
                        if (containment)
                        {
                            valid = false;
                            break;
                        }
                    }
                    entities.Clear();

                    if (valid)
                    {
                        Vector3D end = potentialTreePosition;
                        end.Y -= 20;
                        if (RaycastForExactPosition(potentialTreePosition, end, out location))
                        {
                            d_foundEnlargingPoints.Add(location);
                            return true;
                        }
                        else
                        {
                            valid = false;
                        }
                    }
                }

                location = Vector3D.Zero;
                return false;
            }
            else
            {
                if (!TryGetRandomAreas(m_tmpAreas))
                {
                    location = Vector3D.Zero;
                    return false;
                }

                int areaIdx = 0;
                int randomStartIdx = MyUtils.GetRandomInt(m_tmpAreas.Count);
                while (areaIdx < m_tmpAreas.Count)
                {
                    var spawnArea = m_tmpAreas[randomStartIdx];
                    randomStartIdx = (randomStartIdx + 1) % m_tmpAreas.Count;
                    areaIdx++;

                    if (!spawnArea.IsValid || spawnArea.IsFull)
                        continue;

                    if (predicate != null && !predicate(spawnArea.GetAreaData()))
                    {
                        spawnArea.IsFull = true;
                        continue;
                    }

                    var spawnBox = spawnArea.ForestBox;
                    var forestBox = spawnArea.ForestBox;
                    spawnBox = forestBox.Inflate(desiredHalfSize);
                    spawnBox.Inflate(new Vector3D(0.2, 0, 0.2)); // inflate for some minimum size

                    MyBBSetSampler setSampler = new MyBBSetSampler(spawnBox.Min, spawnBox.Max);
                    setSampler.SubtractBB(ref forestBox);
                    RefineSampler(spawnArea, ref spawnBox, ref desiredHalfSize, setSampler);

                    if (!setSampler.Valid)
                        continue;

                    Vector3D exactLocation;
                    Vector3D samplePosition = setSampler.Sample();
                    if (TryGetExactLocation(spawnArea, samplePosition, 40, out exactLocation))
                    {
                        location = exactLocation;
                        d_foundEnlargingPoints.Add(exactLocation);
                        m_tmpAreas.Clear();
                        return true;
                    }
                    else
                    {
                        location = Vector3D.Zero;
                        m_tmpAreas.Clear();
                        return false;
                    }
                }
            }

            location = Vector3D.Zero;
            m_tmpAreas.Clear();
            return false;
        }

        private void RefineSampler(Area spawnArea, ref BoundingBoxD spawnBox, ref Vector3D desiredHalfSize, MyBBSetSampler setSampler)
        {
            var entities = MyEntities.GetEntitiesInAABB(ref spawnBox);
            foreach (var entity in entities)
            {
                if (entity is MyEnvironmentItems)
                    continue;
                if (entity is MyVoxelBase)
                    continue;
                var entityBox = entity.PositionComp.WorldAABB;
                entityBox.Inflate(desiredHalfSize);
                entityBox.Min.Y = DEBUG_BOX_Y_MIN_POS;
                entityBox.Max.Y = DEBUG_BOX_Y_MAX_POS;
                setSampler.SubtractBB(ref entityBox);
            }
            entities.Clear();

            m_aabbTree.OverlapAllBoundingBox(ref spawnBox, m_tmpAreas2);
            foreach (var area in m_tmpAreas2)
            {
                if (area != spawnArea)
                {
                    var box = area.ForestBox;
                    box.Inflate(desiredHalfSize);
                    setSampler.SubtractBB(ref box);
                }
            }
            m_tmpAreas2.Clear();
        }

        private bool TryGetRandomAreas(List<Area> output)
        {
            if (m_highLevelBoxes.Count == 0)
                return false;

            while (m_hlSelectionCounter > 0)
            {
                m_hlSelectionCounter--;
                var desiredBox = m_highLevelBoxes[m_hlCurrentBox];
                m_aabbTree.OverlapAllBoundingBox(ref desiredBox, output);

                if (m_hlSelectionCounter == 0 || output.Count == 0)
                {
                    m_hlCurrentBox = (m_hlCurrentBox + 1) % m_highLevelBoxes.Count;
                    m_hlSelectionCounter = (int)Math.Ceiling(m_highLevelBoxes[m_hlCurrentBox].Volume / m_hlSize);
                }

                if (output.Count != 0)
                    return true;
            }

            return false;
        }

        private bool RaycastForExactPosition(Vector3D start, Vector3D end, out Vector3D exact)
        {
            LineD line = new LineD(start, end);
            Vector3D? output;
            if (m_ground.GetIntersectionWithLine(ref line, out output))
            {
                exact = output.Value;
                return true;
            }

            exact = Vector3D.Zero;
            return false;
        }

        private bool TryGetExactLocation(Area area, Vector3D point, float thresholdDistance, out Vector3D exact)
        {
            const float DISTANCE = 25;

            ProfilerShort.Begin("Raycast");
            try
            {
                exact = default(Vector3D);
                foreach (var envItemData in area.ItemIds)
                {
                    var envItem = m_envItems[envItemData.Key];
                    foreach (var localId in area.ItemIds[envItemData.Key])
                    {
                        MatrixD world;
                        envItem.GetItemWorldMatrix(localId, out world);

                        point.Y = world.Translation.Y;
                        var distSq = (world.Translation - point).LengthSquared();
                        if (distSq < thresholdDistance)
                        {
                            Vector3D finish = new Vector3D(point.X, Math.Max(0, point.Y - DISTANCE), point.Z);
                            point.Y += DISTANCE;
                            bool result = RaycastForExactPosition(point, finish, out exact);
                            if (result)
                                return true;
                        }
                    }
                }
            }
            finally
            {
                ProfilerShort.End();
            }

            return false;
        }

        public void DebugDraw()
        {
            if (MyDebugDrawSettings.DEBUG_DRAW_FLORA_BOXES)
            {
                MatrixD offset = MatrixD.CreateTranslation(Vector3.Down * 256.0f);
                for (int i = 0; i < m_forestAreas.Count; i++)
                {
                    var area = m_forestAreas[i];
                    var dbgDrawBox = area.ForestBox;
                    dbgDrawBox.Translate(offset);

                    Color boxColor = area.IsFull ? Color.DarkOrange : Color.Honeydew;

                    VRageRender.MyRenderProxy.DebugDrawAABB(dbgDrawBox, boxColor, 1.0f, 1.0f, false);
                    if ((MySector.MainCamera.Position - dbgDrawBox.Center).Length() <= 5)
                    {
                        var str = string.Format("Gran {0}: {1}", i, (int)(0.5f + dbgDrawBox.Volume));
                        VRageRender.MyRenderProxy.DebugDrawText3D(dbgDrawBox.Center, str, Color.PaleVioletRed, 1.0f, true);
                        foreach (var item in area.ItemIds)
                        {
                            var envItem = m_envItems[item.Key];
                            foreach (var ins in item.Value)
                            {
                                ItemInfo info;
                                if (envItem.TryGetItemInfoById(ins, out info))
                                {
                                    Vector3D itemPos = Vector3D.Transform(info.Transform.TransformMatrix.Translation, offset);
                                    VRageRender.MyRenderProxy.DebugDrawPoint(itemPos, Color.Red, true);
                                    VRageRender.MyRenderProxy.DebugDrawText3D(itemPos, "Item: " + info.SubtypeId.ToString(), Color.PaleVioletRed, 1.0f, true);
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < m_highLevelBoxes.Count; i++)
                {
                    var higherOrder = m_highLevelBoxes[i];
                    higherOrder.Translate(offset);
                    VRageRender.MyRenderProxy.DebugDrawAABB(higherOrder, Color.Red, 1.0f, 1.0f, false);
                    if ((MySector.MainCamera.Position - higherOrder.Center).Length() <= 30)
                    {
                        VRageRender.MyRenderProxy.DebugDrawText3D(higherOrder.Center, "Gran: " + (int)(0.5f + higherOrder.Volume), Color.CadetBlue, 1.0f, true);
                        Color c = Color.Coral;
                        MySimpleObjectDraw.DrawTransparentBox(ref MatrixD.Identity, ref higherOrder, ref c, MySimpleObjectRasterizer.Solid, 1);
                    }
                }

                var invalidateTimer = (m_invalidateAreaTimer - MySandboxGame.Static.UpdateTime).Seconds;
                VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(10, 280), "Total boxes count: " + m_forestAreas.Count, Color.Violet, 0.8f);
                VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(10, 300), "Taken area size: " + (int)m_currentForestArea, Color.Violet, 0.8f);
                VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(10, 320), "World area size: " + m_worldArea, Color.Violet, 0.8f);
                VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(10, 340), "Taken area percent: " + (m_currentForestArea / m_worldArea), Color.Violet, 0.8f);
                VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(10, 360), "Invalidate area timer: " + invalidateTimer.ToString(), Color.SlateBlue, 0.8f);
                if (m_ground != null)
                    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(10, 380), "World dimensions: " + m_ground.SizeInMetres.X + " x " + m_ground.SizeInMetres.Z, Color.Violet, 0.8f);
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_FLORA_SPAWNED_ITEMS)
            {
                foreach (var spawnPoint in d_foundEnrichingPoints)
                {
                    VRageRender.MyRenderProxy.DebugDrawPoint(spawnPoint, Color.MidnightBlue, false);
                }

                foreach (var spawnPoint in d_foundEnlargingPoints)
                {
                    VRageRender.MyRenderProxy.DebugDrawPoint(spawnPoint, Color.Moccasin, false);
                }
            }
        }
    }
}
