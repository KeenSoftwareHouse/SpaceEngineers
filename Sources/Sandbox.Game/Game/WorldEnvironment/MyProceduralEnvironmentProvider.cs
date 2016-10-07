using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Game.Entities.Planet;
using Sandbox.Game.World;
using Sandbox.Game.WorldEnvironment.Definitions;
using VRage.Collections;
using VRage.ObjectBuilders;
using VRageMath;
using VRageRender;
using System;
using System.Linq;
using VRage.Serialization;
using System.Xml.Serialization;
using ParallelTasks;
using Sandbox.Engine.Multiplayer;
using Sandbox.Definitions;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage.Utils;

namespace Sandbox.Game.WorldEnvironment
{
    public class MyProceduralEnvironmentProvider : IMyEnvironmentDataProvider
    {
        private readonly Dictionary<long, MyProceduralLogicalSector> m_sectors = new Dictionary<long, MyProceduralLogicalSector>();

        private readonly Dictionary<long, MyObjectBuilder_ProceduralEnvironmentSector> m_savedSectors = new Dictionary<long, MyObjectBuilder_ProceduralEnvironmentSector>();

        private volatile bool m_sectorsQueued = false;
        private readonly MyConcurrentQueue<MyProceduralLogicalSector> m_sectorsToRaise = new MyConcurrentQueue<MyProceduralLogicalSector>();
        private readonly MyConcurrentQueue<MyProceduralLogicalSector> m_sectorsToDestroy = new MyConcurrentQueue<MyProceduralLogicalSector>();

        private readonly MyConcurrentHashSet<MyProceduralLogicalSector> m_sectorsForReplication = new MyConcurrentHashSet<MyProceduralLogicalSector>();

        public int LodFactor = 3;

        public IMyEnvironmentOwner Owner { get; private set; }

        public int ProviderId { get; set; }

        private Vector3D m_origin;
        private Vector3D m_basisX, m_basisY;

        private double m_sectorSize;

        internal int SyncLod { get { return Owner.EnvironmentDefinition.SyncLod; } }

        public MyProceduralEnvironmentProvider()
        {
            m_raiseCallback = RaiseLogicalSectors;
        }

        public void Init(IMyEnvironmentOwner owner, ref Vector3D origin, ref Vector3D basisA, ref Vector3D basisB, double sectorSize, MyObjectBuilder_Base ob)
        {
            Debug.Assert(owner.EnvironmentDefinition is MyProceduralEnvironmentDefinition, "The procedural world environment provider requires a Procedural World Environment Definition.");

            Owner = owner;

            m_sectorSize = sectorSize;

            m_origin = origin;
            m_basisX = basisA;
            m_basisY = basisB;

            var builder = ob as MyObjectBuilder_ProceduralEnvironmentProvider;

            if (builder != null)
            {
                for (int i = 0; i < builder.Sectors.Count; i++)
                {
                    var sector = builder.Sectors[i];
                    m_savedSectors.Add(sector.SectorId, sector);
                }
            }
        }

        private readonly Action<WorkData> m_raiseCallback;
        private void RaiseLogicalSectors(WorkData data)
        {
            MyProceduralLogicalSector sector;
            var server = (MyMultiplayerServerBase)MyMultiplayer.Static;

            m_sectorsQueued = false;

            while (m_sectorsToDestroy.TryDequeue(out sector))
            {
                sector.Close();
            }

            while (m_sectorsToRaise.TryDequeue(out sector))
            {
                server.RaiseReplicableCreated(sector);
            }
        }

        private void QueueRaiseLogicalSector(MyProceduralLogicalSector sector)
        {
            if (Sync.IsServer && Sync.MultiplayerActive)
            {
                Debug.Assert(MyMultiplayer.Static != null);
                m_sectorsToRaise.Enqueue(sector);
            }
        }

        private void QueueDestroyLogicalSector(MyProceduralLogicalSector sector)
        {
            if (Sync.IsServer && Sync.MultiplayerActive)
            {
                Debug.Assert(MyMultiplayer.Static != null);
                m_sectorsToDestroy.Enqueue(sector);
            }
            else
            {
                sector.Close();
            }
        }

        #region IMyEnvironmentDataProvider
        public unsafe MyEnvironmentDataView GetItemView(int lod, ref Vector2I start, ref Vector2I end, ref Vector3D localOrigin)
        {
            var localLod = lod / LodFactor;
            var logicalLod = lod % LodFactor;

            start >>= (localLod * LodFactor);
            end >>= (localLod * LodFactor);

            MyProceduralDataView view = new MyProceduralDataView(this, lod, ref start, ref end);

            var lcount = (end - start + 1).Size();

            view.SectorOffsets = new List<int>(lcount);
            view.LogicalSectors = new List<MyLogicalEnvironmentSectorBase>(lcount);
            view.IntraSectorOffsets = new List<int>(lcount);

            // First round, calculate offsets and find any missing sectors.
            int offset = 0;
            for (int y = start.Y; y <= end.Y; y++)
                for (int x = start.X; x <= end.X; x++)
                {
                    var sector = GetLogicalSector(x, y, localLod);

                    if (sector.MinimumScannedLod != logicalLod)
                        sector.ScanItems(logicalLod);

                    sector.Viewers.Add(view);
                    sector.UpdateMinLod();

                    view.SectorOffsets.Add(offset);
                    view.LogicalSectors.Add(sector);
                    offset += sector.ItemCountForLod[logicalLod];
                    view.IntraSectorOffsets.Add(0);
                }

            // Allocate item list.
            view.Items = new List<ItemInfo>(offset);

            int offsetIndex = 0;
            for (int y = start.Y; y <= end.Y; y++)
                for (int x = start.X; x <= end.X; x++)
                {
                    MyProceduralLogicalSector sector = m_sectors[MyPlanetSectorId.MakeSectorId(x, y, ProviderId, localLod)];

                    int itemCnt = sector.ItemCountForLod[logicalLod];

                    offset = view.SectorOffsets[offsetIndex++];

                    Vector3 centerOffset = sector.WorldPos - localOrigin;

                    fixed (ItemInfo* viewItems = view.Items.GetInternalArray())
                    fixed (ItemInfo* sectorItems = sector.Items.GetInternalArray())
                        for (int i = 0; i < itemCnt; ++i)
                        {
                            int vi = i + offset; // view index

                            viewItems[vi].Position = sectorItems[i].Position + centerOffset;

                            // TODO: Memcpy?
                            viewItems[vi].DefinitionIndex = sectorItems[i].DefinitionIndex;
                            viewItems[vi].ModelIndex = sectorItems[i].ModelIndex;
                            viewItems[vi].Rotation = sectorItems[i].Rotation;
                        }
                }

            view.Items.SetSize(view.Items.Capacity);

            if ((m_sectorsToRaise.Count > 0 || m_sectorsToRaise.Count > 0) && !m_sectorsQueued)
            {
                m_sectorsQueued = true;

                Parallel.ScheduleForThread(m_raiseCallback, null, MySandboxGame.Static.UpdateThread);
            }
            return view;
        }

        private MyProceduralLogicalSector GetLogicalSector(int x, int y, int localLod)
        {
            var key = MyPlanetSectorId.MakeSectorId(x, y, ProviderId, localLod);
            MyProceduralLogicalSector sector;
            if (!m_sectors.TryGetValue(key, out sector))
            {
                MyObjectBuilder_ProceduralEnvironmentSector sectorBuilder;
                m_savedSectors.TryGetValue(key, out sectorBuilder);
                sector = new MyProceduralLogicalSector(this, x, y, localLod, sectorBuilder);
                sector.Id = key;

                m_sectors[key] = sector;
            }
            return sector;
        }

        public MyObjectBuilder_EnvironmentDataProvider GetObjectBuilder()
        {
            var ob = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ProceduralEnvironmentProvider>();

            foreach (var sector in m_sectors)
            {
                var sectorData = sector.Value.GetObjectBuilder();
                if (sectorData == null)
                    continue;

                ob.Sectors.Add((MyObjectBuilder_ProceduralEnvironmentSector)sectorData);
            }

            foreach (var sector in m_savedSectors)
            {
                if (!m_sectors.ContainsKey(sector.Key))
                    ob.Sectors.Add(sector.Value);
            }

            return ob;
        }

        public void DebugDraw()
        {
            var dsq = MyPlanetEnvironmentSessionComponent.DebugDrawDistance * MyPlanetEnvironmentSessionComponent.DebugDrawDistance;

            foreach (var sector in m_sectors.Values.ToArray())
            {
                MyRenderProxy.DebugDraw6FaceConvex(sector.Bounds, Color.Violet, .5f, true, false);

                var center = (sector.Bounds[4] + sector.Bounds[7]) / 2;
                if (Vector3D.DistanceSquared(center, MySector.MainCamera.Position) < dsq)
                {
                    var offset = -MySector.MainCamera.UpVector * 3;
                    MyRenderProxy.DebugDrawText3D(center + offset, sector.ToString(), Color.Violet, 1, true);
                }
            }
        }

        public IEnumerable<MyLogicalEnvironmentSectorBase> LogicalSectors
        {
            get { return m_sectorsForReplication; }
        }

        public MyLogicalEnvironmentSectorBase GetLogicalSector(long sectorId)
        {
            MyProceduralLogicalSector sector;
            m_sectors.TryGetValue(sectorId, out sector);

            return sector;
        }

        #endregion

        public void CloseView(MyProceduralDataView view)
        {
            var lod = view.Lod / LodFactor;

            for (int y = view.Start.Y; y <= view.End.Y; y++)
                for (int x = view.Start.X; x <= view.End.X; x++)
                {
                    var key = MyPlanetSectorId.MakeSectorId(x, y, ProviderId, lod);

                    var sector = m_sectors[key];
                    sector.Viewers.Remove(view);
                    sector.UpdateMinLod();

                    if (sector.Viewers.Count == 0 && !sector.ServerOwned)
                    {
                        CloseSector(sector);
                    }
                }
        }

        internal void CloseSector(MyProceduralLogicalSector sector)
        {
            SaveLogicalSector(sector);
            m_sectors.Remove(sector.Id);

            if (sector.Replicable)
                UnmarkReplicable(sector);
            else
                QueueDestroyLogicalSector(sector);
        }

        private void SaveLogicalSector(MyProceduralLogicalSector sector)
        {
            var sectorOb = sector.GetObjectBuilder();

            if (sectorOb == null)
            {
                m_savedSectors.Remove(sector.Id);
            }
            else
            {
                m_savedSectors[sector.Id] = (MyObjectBuilder_ProceduralEnvironmentSector)sectorOb;
            }
        }

        public MyProceduralLogicalSector TryGetLogicalSector(int lod, int logicalx, int logicaly)
        {
            MyProceduralLogicalSector sector;
            m_sectors.TryGetValue(MyPlanetSectorId.MakeSectorId(logicalx, logicaly, ProviderId, lod), out sector);

            return sector;
        }

        public void GeSectorWorldParameters(int x, int y, int localLod, out Vector3D worldPos, out Vector3 scanBasisA, out Vector3 scanBasisB)
        {
            double sectorLodSize = (1 << localLod) * m_sectorSize;

            worldPos = m_origin + m_basisX * ((x + .5) * sectorLodSize) + m_basisY * ((y + .5) * sectorLodSize);

            scanBasisA = m_basisX * (sectorLodSize * .5);
            scanBasisB = m_basisY * (sectorLodSize * .5);
        }

        public int GetSeed()
        {
            // TODO: Eliminate this, seed should be on the procedural side
            return Owner.GetSeed();
        }

        internal void MarkReplicable(MyProceduralLogicalSector sector)
        {
            Debug.Assert(!sector.Replicable);

            m_sectorsForReplication.Add(sector);
            QueueRaiseLogicalSector(sector);
            sector.Replicable = true;
        }

        internal void UnmarkReplicable(MyProceduralLogicalSector sector)
        {
            Debug.Assert(sector.Replicable);
            m_sectorsForReplication.Remove(sector);
            QueueDestroyLogicalSector(sector);
            sector.Replicable = false;
        }
    }

    public class MyProceduralDataView : MyEnvironmentDataView
    {
        private readonly MyProceduralEnvironmentProvider m_provider;

        public MyProceduralDataView(MyProceduralEnvironmentProvider provider, int lod, ref Vector2I start, ref Vector2I end)
        {
            m_provider = provider;

            Start = start;
            End = end;
            Lod = lod;
        }

        public override void Close()
        {
            m_provider.CloseView(this);
        }

        public int GetSectorIndex(int x, int y)
        {
            return (x - Start.X) + (y - Start.Y) * (End.X - Start.X + 1);
        }
    }
}