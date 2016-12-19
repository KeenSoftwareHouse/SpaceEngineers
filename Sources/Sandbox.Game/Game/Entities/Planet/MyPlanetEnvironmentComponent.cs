using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ParallelTasks;
using Sandbox.Definitions;
using Sandbox.Game.World;
using Sandbox.Game.WorldEnvironment;
using Sandbox.Game.WorldEnvironment.Definitions;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Library.Utils;
using VRageMath;
using VRageRender;
using VRage.Game;
using VRage.ObjectBuilders;
using Sandbox.Engine.Voxels;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage.ModAPI;
using VRage.Profiler;

namespace Sandbox.Game.Entities.Planet
{
    [MyComponentBuilder(typeof(MyObjectBuilder_PlanetEnvironmentComponent))]
    public class MyPlanetEnvironmentComponent : MyEntityComponentBase, IMy2DClipmapManager, IMyEnvironmentOwner
    {
        #region Caches

        private List<BoundingBoxD> m_sectorBoxes = new List<BoundingBoxD>();

        #endregion

        #region Public

        #endregion

        internal int ActiveFace { get; private set; }

        internal MyPlanet Planet
        {
            get { return (MyPlanet)Entity; }
        }

        internal Vector3D PlanetTranslation { get; private set; }

        private readonly My2DClipmap<MyPlanetEnvironmentClipmapProxy>[] m_clipmaps = new My2DClipmap<MyPlanetEnvironmentClipmapProxy>[6];

        internal My2DClipmap<MyPlanetEnvironmentClipmapProxy> ActiveClipmap;

        // Sectors with physics
        internal Dictionary<long, MyEnvironmentSector> PhysicsSectors = new Dictionary<long, MyEnvironmentSector>();

        // Sectors that were pinned for one reason or the other.
        internal Dictionary<long, MyEnvironmentSector> HeldSectors = new Dictionary<long, MyEnvironmentSector>();

        internal Dictionary<long, MyPlanetEnvironmentClipmapProxy> Proxies = new Dictionary<long, MyPlanetEnvironmentClipmapProxy>();
        internal Dictionary<long, MyPlanetEnvironmentClipmapProxy> OutgoingProxies = new Dictionary<long, MyPlanetEnvironmentClipmapProxy>();

        internal readonly IMyEnvironmentDataProvider[] Providers = new IMyEnvironmentDataProvider[6];

        private MyObjectBuilder_EnvironmentDataProvider[] m_providerData = new MyObjectBuilder_EnvironmentDataProvider[6];

        public MyPlanetEnvironmentComponent()
        {
            m_parallelWorkDelegate = ParallelWorkCallback;
            m_serialWorkDelegate = SerialWorkCallback;
        }

        public void InitEnvironment()
        {
            EnvironmentDefinition = Planet.Generator.EnvironmentDefinition;

            PlanetTranslation = Planet.WorldMatrix.Translation;

            m_InstanceHash = Planet.GetInstanceHash();

            double radius = Planet.AverageRadius;
            double faceSize = radius * Math.Sqrt(2);
            double faceSize2 = faceSize / 2;

            double sectorSize = EnvironmentDefinition.SectorSize;

            // Prepare each clipmap
            for (int i = 0; i < 6; ++i)
            {
                // get forward and up
                Vector3D forward, up;
                MyPlanetCubemapHelper.GetForwardUp((Base6Directions.Direction)i, out forward, out up);

                var translation = forward * faceSize2 + PlanetTranslation;

                forward = -forward;

                // prepare matrix
                MatrixD worldMatrix;
                MatrixD.CreateWorld(ref translation, ref forward, ref up, out worldMatrix);

                // Setup origins
                Vector3D origin = new Vector3D(-faceSize2, -faceSize2, 0);
                Vector3D.Transform(ref origin, ref worldMatrix, out origin);

                // Basis vectors
                Vector3D basisX = new Vector3D(1, 0, 0), basisY = new Vector3D(0, 1, 0);
                Vector3D.RotateAndScale(ref basisX, ref worldMatrix, out basisX);
                Vector3D.RotateAndScale(ref basisY, ref worldMatrix, out basisY);

                // Create and init the clipmap.
                m_clipmaps[i] = new My2DClipmap<MyPlanetEnvironmentClipmapProxy>();
                ActiveClipmap = m_clipmaps[i];
                ActiveFace = i;

                m_clipmaps[i].Init(this, ref worldMatrix, sectorSize, faceSize);
                ActiveFace = -1;

                // Prepare the provider for the face
                var provider = new MyProceduralEnvironmentProvider { ProviderId = i };

                provider.Init(this, ref origin, ref basisX, ref basisY, ActiveClipmap.LeafSize, m_providerData[i]);

                Providers[i] = provider;
            }
        }

        public void Update(bool doLazyUpdates = true, bool forceUpdate = false)
        {
            var tempMaxLod = MaxLod;
            MaxLod = MathHelper.Log2Floor((int)(MySandboxGame.Config.VegetationDrawDistance / EnvironmentDefinition.SectorSize));
            if (tempMaxLod != MaxLod)
            {
                CloseAll();
                forceUpdate = true;
            }

            UpdateClipmaps();

            UpdatePhysics();

            if (doLazyUpdates) LazyUpdate();

            // Manage our scheduling
            if (!m_parallelInProgress)
            {
                if (m_sectorsToWorkParallel.Count > 0)
                {
                    // Use this at the start of the game.
                    if (forceUpdate)
                    {
                        MyEnvironmentSector sector;
                        while (m_sectorsToWorkParallel.TryDequeue(out sector))
                            sector.DoParallelWork();

                        while (m_sectorsToWorkSerial.TryDequeue(out sector))
                            sector.DoSerialWork();
                    }
                    else
                    {
                        m_parallelInProgress = true;
                        Parallel.Start(m_parallelWorkDelegate, m_serialWorkDelegate);
                    }
                }
                else if (m_sectorsToWorkSerial.Count > 0)
                {
                    SerialWorkCallback();
                }
            }
        }

        public int MaxLod { get; private set; }

        private void UpdateClipmaps()
        {
            // This avoids the clipmap from overruning and going bad
            if (m_sectorsToWorkParallel.Count > 0) return;

            Vector3D camera = MySector.MainCamera.Position - PlanetTranslation;

            double distance = camera.Length();

            // Update only if relevant
            if (distance > Planet.AverageRadius + m_clipmaps[0].FaceHalf && Proxies.Count == 0)
            {
                return;
            }

            distance = Math.Abs(Planet.Provider.Shape.GetDistanceToSurfaceCacheless(camera));

            // Get cubemap coordinates
            Vector2D texcoords;
            int direction;
            MyPlanetCubemapHelper.ProjectToCube(ref camera, out direction, out texcoords);

            // Update each clipmap accordingly.
            for (int face = 0; face < 6; ++face)
            {
                ActiveFace = face;
                ActiveClipmap = m_clipmaps[face];

                Vector2D localCoords;
                MyPlanetCubemapHelper.TranslateTexcoordsToFace(ref texcoords, direction, face, out localCoords);

                Vector3D pos;
                pos.X = localCoords.X * ActiveClipmap.FaceHalf;
                pos.Y = localCoords.Y * ActiveClipmap.FaceHalf;

                if ((face ^ direction) == 1)
                    pos.Z = distance + Planet.AverageRadius * 2;
                else
                    pos.Z = distance;

                //pos.Z = 0;

                ActiveClipmap.Update(pos);
                EvaluateOperations(); // Enqueue operations from this clipmap.
            }

            ActiveFace = -1;
        }

        private void LazyUpdate()
        {
            // Process physics to close
            foreach (var sector in m_sectorsWithPhysics.Set())
            {
                sector.EnablePhysics(false);

                PhysicsSectors.Remove(sector.SectorId);

                if (!Proxies.ContainsKey(sector.SectorId) && !OutgoingProxies.ContainsKey(sector.SectorId) && !sector.IsPinned)
                {
                    Debug.Assert(sector.IsPendingLodSwitch || sector.LodLevel == -1);
                    m_sectorsClosing.Add(sector); // Add to close if also no graphics
                }
            }
            m_sectorsWithPhysics.ClearSet(); // remove any sectors remaining in set.
            m_sectorsWithPhysics.AllToSet();

            // Foreach sector marked to close
            foreach (var sector in m_sectorsClosing)
            {
                if (!sector.HasWorkPending())
                {
                    // If no work is left, kill the sector
                    sector.Close();
                    Planet.RemoveChildEntity((MyEntity)sector);
                    m_sectorsClosed.Add(sector);
                }
                else
                {
                    // Tell it to stop working
                    sector.CancelParallel();
                    if (sector.HasSerialWorkPending)
                        sector.DoSerialWork();
                }
            }

            // Remove any sectors that finally died
            foreach (var sector in m_sectorsClosed)
            {
                m_sectorsClosing.Remove(sector);
            }
            m_sectorsClosed.Clear();
        }

        public void DebugDraw()
        {
            if (MyPlanetEnvironmentSessionComponent.DebugDrawSectors)
            {
                if (MyPlanetEnvironmentSessionComponent.DebugDrawDynamicObjectClusters)
                    using (var batch = MyRenderProxy.DebugDrawBatchAABB(MatrixD.Identity, new Color(Color.Green, 0.2f), true, true))
                    {
                        foreach (var box in m_sectorBoxes)
                        {
                            BoundingBoxD bb = box;
                            batch.Add(ref bb);
                        }
                    }
            }

            if (MyPlanetEnvironmentSessionComponent.DebugDrawProxies)
            {
                foreach (var proxy in Proxies.Values)
                {
                    proxy.DebugDraw();
                }

                foreach (var proxy in OutgoingProxies.Values)
                {
                    proxy.DebugDraw(true);
                }
            }

            if (MyPlanetEnvironmentSessionComponent.DebugDrawCollisionCheckers)
            {
                if (m_obstructorsPerSector != null)
                foreach (var obbList in m_obstructorsPerSector.Values)
                {
                    foreach (var obb in obbList)
                        MyRenderProxy.DebugDrawOBB(obb, Color.Red, 0.1f, true, true);
                }
            }
        }

        #region Physics Sectors

        private void UpdatePhysics()
        {
            ProfilerShort.Begin("Update Physics Sectors");

            BoundingBoxD box = Planet.PositionComp.WorldAABB;
            box.Min -= MyPlanet.PHYSICS_SECTOR_SIZE_METERS;
            box.Max += MyPlanet.PHYSICS_SECTOR_SIZE_METERS;

            m_sectorBoxes.Clear();
            MyGamePruningStructure.GetAproximateDynamicClustersForSize(ref box, EnvironmentDefinition.SectorSize/2, m_sectorBoxes);

            foreach (var cell in m_sectorBoxes)
            {
                var c = cell;
                c.Translate(-PlanetTranslation);
                c.Inflate(EnvironmentDefinition.SectorSize/2);

                var position = c.Center;

                double distance = position.Length();

                double inflate = c.Size.Length() / 2;

                if (distance >= Planet.MinimumRadius - inflate && distance <= Planet.MaximumRadius + inflate)
                {
                    RasterSectorsForPhysics(c);
                }
            }
            ProfilerShort.End();
        }

        // Iterate over sector boxes in a range.
        // TODO: Dumb version of this for small boxes
        private unsafe void RasterSectorsForPhysics(BoundingBoxD range)
        {
            range.InflateToMinimum(EnvironmentDefinition.SectorSize);

            Vector2I top = new Vector2I(1 << m_clipmaps[0].Depth) - 1;

            Vector3D* pos = stackalloc Vector3D[8];

            range.GetCornersUnsafe(pos);

            // bitmask for faces, 7th bit is simple bit
            int markedFaces = 0;
            int firstFace = 0;

            for (var i = 0; i < 8; ++i)
            {
                Vector3D copy = pos[i];

                int index = MyPlanetCubemapHelper.FindCubeFace(ref copy);
                firstFace = index;
                index = 1 << index;

                if ((markedFaces & ~index) != 0) markedFaces |= 0x40;

                markedFaces |= index;
            }

            // This way we can ensure a single code path.
            int startFace = 0;
            int endFace = 5;

            // If we only encounter one face we narrow it down.
            if ((markedFaces & 0x40) == 0)
            {
                startFace = endFace = firstFace;
            }

            for (int face = startFace; face <= endFace; ++face)
            {
                if (((1 << face) & markedFaces) == 0)
                    continue;

                double size = m_clipmaps[face].LeafSize;

                // Offset 
                var offset = 1 << m_clipmaps[face].Depth - 1;

                BoundingBox2D bounds = BoundingBox2D.CreateInvalid();
                for (int i = 0; i < 8; ++i)
                {
                    Vector3D copy = pos[i];

                    Vector2D normCoords;
                    MyPlanetCubemapHelper.ProjectForFace(ref copy, face, out normCoords);
                    bounds.Include(normCoords);
                }

                bounds.Min += 1;
                bounds.Min *= offset;

                bounds.Max += 1;
                bounds.Max *= offset;

                // Calculate bounds in sectors.
                var start = new Vector2I((int)bounds.Min.X, (int)bounds.Min.Y);
                var end = new Vector2I((int)bounds.Max.X, (int)bounds.Max.Y);

                Vector2I.Max(ref start, ref Vector2I.Zero, out start);
                Vector2I.Min(ref end, ref top, out end);

                for (int x = start.X; x <= end.X; ++x)
                    for (int y = start.Y; y <= end.Y; ++y)
                    {
                        EnsurePhysicsSector(x, y, face);
                    }
            }
        }

        private void EnsurePhysicsSector(int x, int y, int face)
        {
            MyEnvironmentSector sector;

            long key = Entities.Planet.MyPlanetSectorId.MakeSectorId(x, y, face);

            if (!PhysicsSectors.TryGetValue(key, out sector))
            {
                MyPlanetEnvironmentClipmapProxy proxy;
                if (Proxies.TryGetValue(key, out proxy))
                {
                    sector = proxy.EnvironmentSector;
                    sector.EnablePhysics(true);
                }
                else if (HeldSectors.TryGetValue(key, out sector)) ;
                else
                {
                    sector = EnvironmentDefinition.CreateSector();

                    MyEnvironmentSectorParameters parms = new MyEnvironmentSectorParameters();

                    var leafSize = m_clipmaps[face].LeafSize;
                    var leafSizeHalf = m_clipmaps[face].LeafSize / 2;
                    int leafCountHalf = 1 << m_clipmaps[face].Depth - 1;

                    Matrix wmf = m_clipmaps[face].WorldMatrix;

                    parms.SurfaceBasisX = new Vector3(leafSizeHalf, 0, 0);
                    Vector3.RotateAndScale(ref parms.SurfaceBasisX, ref wmf, out parms.SurfaceBasisX);

                    parms.SurfaceBasisY = new Vector3(0, leafSizeHalf, 0);
                    Vector3.RotateAndScale(ref parms.SurfaceBasisY, ref wmf, out parms.SurfaceBasisY);

                    parms.Environment = EnvironmentDefinition;
                    parms.Center = Vector3D.Transform(new Vector3D((x - leafCountHalf + .5) * leafSize, (y - leafCountHalf + .5) * leafSize, 0),
                        m_clipmaps[face].WorldMatrix);

                    parms.DataRange = new BoundingBox2I(new Vector2I(x, y), new Vector2I(x, y));

                    parms.Provider = Providers[face];
                    parms.EntityId = Entities.Planet.MyPlanetSectorId.MakeSectorEntityId(x, y, 0, face, Planet.EntityId);
                    parms.SectorId = Entities.Planet.MyPlanetSectorId.MakeSectorId(x, y, face);

                    parms.Bounds = GetBoundingShape(ref parms.Center, ref parms.SurfaceBasisX,
                        ref parms.SurfaceBasisY);

                    sector.Init(this, ref parms);

                    sector.EnablePhysics(true);

                    Planet.AddChildEntity((MyEntity)sector);
                }

                PhysicsSectors.Add(key, sector);
            }

            m_sectorsWithPhysics.AddOrEnsureOnComplement(sector);
        }

        #endregion

        #region Component

        public override string ComponentTypeDebugString
        {
            get { return "Planet Environment Component"; }
        }

        public override void OnAddedToScene()
        {
            MySession.Static.GetComponent<MyPlanetEnvironmentSessionComponent>().RegisterPlanetEnvironment(this);
        }

        public override void OnRemovedFromScene()
        {
            MySession.Static.GetComponent<MyPlanetEnvironmentSessionComponent>().UnregisterPlanetEnvironment(this);

            CloseAll();
        }

        public override bool IsSerialized()
        {
            return true;
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var builder = new MyObjectBuilder_PlanetEnvironmentComponent();

            for (int i = 0; i < Providers.Length; i++)
            {
                builder.DataProviders[i] = Providers[i].GetObjectBuilder();
                builder.DataProviders[i].Face = (Base6Directions.Direction)i;
            }

            if (CollisionCheckEnabled && m_obstructorsPerSector.Count > 0)
            {
                builder.SectorObstructions = new List<MyObjectBuilder_PlanetEnvironmentComponent.ObstructingBox>();

                foreach (var sect in m_obstructorsPerSector)
                {
                    builder.SectorObstructions.Add(new MyObjectBuilder_PlanetEnvironmentComponent.ObstructingBox
                    {
                        SectorId = sect.Key,
                        ObstructingBoxes = sect.Value
                    });
                }
            }

            return builder;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            var ob = builder as MyObjectBuilder_PlanetEnvironmentComponent;

            if (ob == null)
                return;

            m_providerData = ob.DataProviders;

            if (ob.SectorObstructions != null)
            {
                CollisionCheckEnabled = true;
                m_obstructorsPerSector = new Dictionary<long, List<MyOrientedBoundingBoxD>>();

                foreach (var sector in ob.SectorObstructions)
                {
                    m_obstructorsPerSector[sector.SectorId] = sector.ObstructingBoxes;
                }
            }
        }

        #endregion

        #region Work Queues and Sector Management

        private readonly ManualResetEvent m_parallelSyncPoint = new ManualResetEvent(true);

        private const long ParallelWorkTimeMilliseconds = 100; // 100 ms, 10 updates is 166 but we don't  want to disrupt the parallels too much.
        private const int SequentialWorkCount = 10; // 10 jobs, sequential is supposed to be super fast so we don't bother checking time.

        private bool m_parallelInProgress;

        private readonly HashSet<MyEnvironmentSector> m_sectorsClosing = new HashSet<MyEnvironmentSector>();
        private readonly List<MyEnvironmentSector> m_sectorsClosed = new List<MyEnvironmentSector>();

        private readonly MyIterableComplementSet<MyEnvironmentSector> m_sectorsWithPhysics = new MyIterableComplementSet<MyEnvironmentSector>();

        private readonly MyConcurrentQueue<MyEnvironmentSector> m_sectorsToWorkParallel = new MyConcurrentQueue<MyEnvironmentSector>(10);
        private readonly MyConcurrentQueue<MyEnvironmentSector> m_sectorsToWorkSerial = new MyConcurrentQueue<MyEnvironmentSector>(10);

        private readonly Action m_parallelWorkDelegate;
        private void ParallelWorkCallback()
        {
            ProfilerShort.Begin("Planet::ParallelWork");

            Stopwatch s = Stopwatch.StartNew();

            m_parallelSyncPoint.Reset();

            MyEnvironmentSector sector;
            while (s.ElapsedMilliseconds < ParallelWorkTimeMilliseconds && m_sectorsToWorkParallel.TryDequeue(out sector))
            {
                sector.DoParallelWork();

                //Debug.Assert(!sector.HasParallelWorkPending);
            }

            m_parallelSyncPoint.Set();
            ProfilerShort.End();
        }

        private readonly Action m_serialWorkDelegate;
        private void SerialWorkCallback()
        {
            ProfilerShort.Begin("Planet::SerialWork");
            int work = m_sectorsToWorkSerial.Count;
            for (; work > 0 && m_sectorsToWorkSerial.Count > 0; --work)
            {
                var sector = m_sectorsToWorkSerial.Dequeue();

                if (!sector.HasParallelWorkPending)
                    sector.DoSerialWork();
                else
                    m_sectorsToWorkSerial.Enqueue(sector); // Sometimes a sector is marked for serial when it has parallel pending= that should be done before.
            }

            m_parallelInProgress = false;
            ProfilerShort.End();
        }

        internal void EnqueueClosing(MyEnvironmentSector sector)
        {
            Debug.Assert(sector.LodLevel == -1 || sector.IsPendingLodSwitch);
            m_sectorsClosing.Add(sector);
        }

        // Operation resolution

        private struct Operation
        {
            public MyPlanetEnvironmentClipmapProxy Proxy;
            public int LodToSet;
            public bool ShouldClose;
        }

        private readonly Dictionary<long, Operation> m_sectorOperations = new Dictionary<long, MyPlanetEnvironmentComponent.Operation>();

        internal bool IsQueued(MyPlanetEnvironmentClipmapProxy sector)
        {
            return m_sectorOperations.ContainsKey(sector.Id);
        }

        internal int QueuedLod(MyPlanetEnvironmentClipmapProxy sector)
        {
            MyPlanetEnvironmentComponent.Operation op;
            if (m_sectorOperations.TryGetValue(sector.Id, out op))
                return op.LodToSet;
            return sector.Lod;
        }

        internal void EnqueueOperation(MyPlanetEnvironmentClipmapProxy proxy, int lod, bool close = false)
        {
            long id = proxy.Id;

            MyPlanetEnvironmentComponent.Operation op;
            if (m_sectorOperations.TryGetValue(id, out op))
            {
                op.LodToSet = lod;
                op.ShouldClose = close;
                m_sectorOperations[id] = op;
            }
            else
            {
                op.LodToSet = lod;
                op.Proxy = proxy;
                op.ShouldClose = close;
                m_sectorOperations.Add(id, op);
            }
        }

        private void EvaluateOperations()
        {
            foreach (MyPlanetEnvironmentComponent.Operation operation in m_sectorOperations.Values)
            {
                var proxy = operation.Proxy;
                //Debug.Assert(!m_sectorsClosing.Contains(sector));

                proxy.EnvironmentSector.SetLod(operation.LodToSet);
                Debug.Assert(operation.LodToSet == proxy.LodSet);

                if (operation.ShouldClose && operation.LodToSet == -1)
                    CheckOnGraphicsClose(proxy.EnvironmentSector);
            }

            m_sectorOperations.Clear();
        }

        internal bool CheckOnGraphicsClose(MyEnvironmentSector sector)
        {
            if (sector.HasPhysics == sector.IsPendingPhysicsToggle && !sector.IsPinned)
            {
                EnqueueClosing(sector);
                return true;
            }
            return false;
        }

        internal void RegisterProxy(MyPlanetEnvironmentClipmapProxy proxy)
        {
            Proxies.Add(proxy.Id, proxy);
        }

        internal void MarkProxyOutgoingProxy(MyPlanetEnvironmentClipmapProxy proxy)
        {
            if (!Proxies.Remove(proxy.Id))
                Debug.Fail("Proxy was already marked outgoing");
            Debug.Assert(!OutgoingProxies.ContainsKey(proxy.Id) || OutgoingProxies[proxy.Id] == proxy);

            OutgoingProxies[proxy.Id] = proxy;
        }

        internal void UnmarkProxyOutgoingProxy(MyPlanetEnvironmentClipmapProxy proxy)
        {
            if (!OutgoingProxies.Remove(proxy.Id))
                Debug.Fail("Proxy was already unmarked outgoing");
            Proxies.Add(proxy.Id, proxy);
        }

        internal void UnregisterProxy(MyPlanetEnvironmentClipmapProxy proxy)
        {
            if (!Proxies.Remove(proxy.Id))
                Debug.Fail("Proxy was already unregistered");
        }

        internal void UnregisterOutgoingProxy(MyPlanetEnvironmentClipmapProxy proxy)
        {
            if (!OutgoingProxies.Remove(proxy.Id))
                Debug.Fail("OutgoingProxies proxy was already unregistered");
        }

        #endregion

        #region IMyEnvironmentOwner

        public unsafe void QuerySurfaceParameters(Vector3D localOrigin, ref BoundingBoxD queryBounds, List<Vector3> queries, List<MySurfaceParams> results)
        {
            localOrigin -= Planet.PositionLeftBottomCorner;

            using (Planet.Storage.Pin())
            {
                var bounds = (BoundingBox)queryBounds.Translate(-Planet.PositionLeftBottomCorner);

                Planet.Provider.Shape.PrepareCache();
                Planet.Provider.Material.PrepareRulesForBox(ref bounds);

                if (results.Capacity != queries.Count)
                {
                    results.Capacity = queries.Count;
                }

                fixed (MySurfaceParams* pars = results.GetInternalArray())
                {
                    for (int i = 0; i < queries.Count; ++i)
                    {
                        Planet.Provider.ComputeCombinedMaterialAndSurface(queries[i] + localOrigin, true, out pars[i]);
                        pars[i].Position -= localOrigin;
                    }
                }

                results.SetSize(queries.Count);
            }
        }

        public MyEnvironmentSector GetSectorForPosition(Vector3D positionWorld)
        {
            var positionLocal = positionWorld - PlanetTranslation;

            int face;
            Vector2D texcoords;
            MyPlanetCubemapHelper.ProjectToCube(ref positionLocal, out face, out texcoords);

            texcoords *= m_clipmaps[face].FaceHalf;

            var handler = m_clipmaps[face].GetHandler(texcoords);

            if (handler != null) return handler.EnvironmentSector;
            return null;
        }

        public MyEnvironmentSector GetSectorById(long packedSectorId)
        {
            MyEnvironmentSector sector;
            if (!PhysicsSectors.TryGetValue(packedSectorId, out sector))
            {
                MyPlanetEnvironmentClipmapProxy proxy;
                if (!Proxies.TryGetValue(packedSectorId, out proxy))
                {
                    return null;
                }
                return proxy.EnvironmentSector;
            }
            return sector;
        }

        public void SetSectorPinned(MyEnvironmentSector sector, bool pinned)
        {
            if (pinned != sector.IsPinned)
            {
                if (pinned)
                {
                    sector.IsPinned = true;
                    HeldSectors.Add(sector.SectorId, sector);
                }
                else
                {
                    sector.IsPinned = false;
                    HeldSectors.Remove(sector.SectorId);
                }
            }
        }

        public IEnumerable<MyEnvironmentSector> GetSectorsInRange(MyShape shape)
        {
            var bb = shape.GetWorldBoundaries();
            var hierarchyComp = Container.Get<MyHierarchyComponentBase>() as MyHierarchyComponent<MyEntity>;
            var entities = new List<MyEntity>();
            hierarchyComp.QueryAABB(ref bb, entities);

            return entities.Cast<MyEnvironmentSector>();
        }

        public int GetSeed()
        {
            return m_InstanceHash;
        }

        private readonly List<MyPhysicalModelDefinition> m_physicalModels = new List<MyPhysicalModelDefinition>();
        private readonly Dictionary<MyPhysicalModelDefinition, short> m_physicalModelToKey = new Dictionary<MyPhysicalModelDefinition, short>();

        public MyPhysicalModelDefinition GetModelForId(short id)
        {
            if (id < m_physicalModels.Count)
            {
                return m_physicalModels[id];
            }

            return null;
        }

        public void GetDefinition(ushort index, out MyRuntimeEnvironmentItemInfo def)
        {
            def = EnvironmentDefinition.Items[index];
        }

        public MyWorldEnvironmentDefinition EnvironmentDefinition { get; private set; }

        MyEntity IMyEnvironmentOwner.Entity
        {
            get { return Planet; }
        }

        public IMyEnvironmentDataProvider DataProvider
        {
            get { return null; }
        }

        public void ProjectPointToSurface(ref Vector3D center)
        {
            center = Planet.GetClosestSurfacePointGlobal(ref center);
        }

        public void GetSurfaceNormalForPoint(ref Vector3D point, out Vector3D normal)
        {
            normal = point - PlanetTranslation;
            normal.Normalize();
        }

        public Vector3D[] GetBoundingShape(ref Vector3D worldPos, ref Vector3 basisX, ref Vector3 basisY)
        {
            BoundingBox box = BoundingBox.CreateInvalid();
            box.Include(-basisX - basisY);
            box.Include(basisX + basisY);

            box.Translate(worldPos - Planet.WorldMatrix.Translation);

            Planet.Provider.Shape.GetBounds(ref box);

            box.Min.Z--;
            box.Max.Z++;

            // Sector Frustum
            Vector3D[] v = new Vector3D[8];

            v[0] = worldPos - basisX - basisY;
            v[1] = worldPos + basisX - basisY;
            v[2] = worldPos - basisX + basisY;
            v[3] = worldPos + basisX + basisY;

            for (int i = 0; i < 4; ++i)
            {
                v[i] -= Planet.WorldMatrix.Translation;
                v[i].Normalize();
                v[i + 4] = v[i] * box.Max.Z;
                v[i] *= box.Min.Z;

                v[i] += Planet.WorldMatrix.Translation;
                v[i + 4] += Planet.WorldMatrix.Translation;
            }

            return v;
        }

        public short GetModelId(MyPhysicalModelDefinition def)
        {
            short id;
            if (!m_physicalModelToKey.TryGetValue(def, out id))
            {
                id = (short)m_physicalModels.Count;
                m_physicalModelToKey.Add(def, id);
                m_physicalModels.Add(def);
            }

            return id;
        }

        public void ScheduleWork(MyEnvironmentSector sector, bool parallel)
        {
            if (parallel)
                m_sectorsToWorkParallel.Enqueue(sector);
            else
                m_sectorsToWorkSerial.Enqueue(sector);
        }

        #endregion

        #region Backwards Compatbility

        private Dictionary<long, List<MyOrientedBoundingBoxD>> m_obstructorsPerSector;
        private int m_InstanceHash;

        public bool CollisionCheckEnabled { get; private set; }

        public List<MyOrientedBoundingBoxD> GetCollidedBoxes(long sectorId)
        {
            List<MyOrientedBoundingBoxD> boxes;
            if (m_obstructorsPerSector.TryGetValue(sectorId, out boxes))
            {
                m_obstructorsPerSector.Remove(sectorId);
            }


            return boxes;
        }

        // For backwards compatbility we store areas that need to be cleared
        public void InitClearAreasManagement()
        {
            m_obstructorsPerSector = new Dictionary<long, List<MyOrientedBoundingBoxD>>();

            var interestArea = Planet.PositionComp.WorldAABB;

            List<MyEntity> entities = new List<MyEntity>();
            MyGamePruningStructure.GetTopMostEntitiesInBox(ref interestArea, entities);

            foreach (var entity in entities)
            {
                RasterSectorsForCollision(entity);
            }

            CollisionCheckEnabled = true;
        }

        private unsafe void RasterSectorsForCollision(MyEntity entity)
        {
            if (!(entity is MyCubeGrid)) return;

            BoundingBoxD range = entity.PositionComp.WorldAABB;
            range.Inflate(8);
            range.Translate(-PlanetTranslation);

            Vector2I top = new Vector2I(1 << m_clipmaps[0].Depth) - 1;

            Vector3D* pos = stackalloc Vector3D[8];

            range.GetCornersUnsafe(pos);

            // bitmask for faces, 7th bit is simple bit
            int markedFaces = 0;
            int firstFace = 0;

            for (var i = 0; i < 8; ++i)
            {
                Vector3D copy = pos[i];

                int index = MyPlanetCubemapHelper.FindCubeFace(ref copy);
                firstFace = index;
                index = 1 << index;

                if ((markedFaces & ~index) != 0) markedFaces |= 0x40;

                markedFaces |= index;
            }

            // This way we can ensure a single code path.
            int startFace = 0;
            int endFace = 5;

            // If we only encounter one face we narrow it down.
            if ((markedFaces & 0x40) == 0)
            {
                startFace = endFace = firstFace;
            }

            for (int face = startFace; face <= endFace; ++face)
            {
                if (((1 << face) & markedFaces) == 0)
                    continue;

                // Offset 
                var offset = 1 << m_clipmaps[face].Depth - 1;

                BoundingBox2D bounds = BoundingBox2D.CreateInvalid();
                for (int i = 0; i < 8; ++i)
                {
                    Vector3D copy = pos[i];

                    Vector2D normCoords;
                    MyPlanetCubemapHelper.ProjectForFace(ref copy, face, out normCoords);
                    bounds.Include(normCoords);
                }

                bounds.Min += 1;
                bounds.Min *= offset;

                bounds.Max += 1;
                bounds.Max *= offset;

                // Calculate bounds in sectors.
                var start = new Vector2I((int)bounds.Min.X, (int)bounds.Min.Y);
                var end = new Vector2I((int)bounds.Max.X, (int)bounds.Max.Y);

                Vector2I.Max(ref start, ref Vector2I.Zero, out start);
                Vector2I.Min(ref end, ref top, out end);

                for (int x = start.X; x <= end.X; ++x)
                    for (int y = start.Y; y <= end.Y; ++y)
                    {
                        long sect = MyPlanetSectorId.MakeSectorId(x, y, face);

                        List<MyOrientedBoundingBoxD> boxes;
                        if (!m_obstructorsPerSector.TryGetValue(sect, out boxes))
                        {
                            boxes = new List<MyOrientedBoundingBoxD>();
                            m_obstructorsPerSector.Add(sect, boxes);
                        }

                        var bb = entity.PositionComp.LocalAABB;
                        bb.Inflate(8); // inflate by 8m to increase the likellyhood of overlap with trees' roots.

                        boxes.Add(new MyOrientedBoundingBoxD((BoundingBoxD)bb, entity.PositionComp.WorldMatrix));
                    }
            }
        }


        #endregion

        public MyLogicalEnvironmentSectorBase GetLogicalSector(long packedSectorId)
        {
            var face = MyPlanetSectorId.GetFace(packedSectorId);

            return Providers[face].GetLogicalSector(packedSectorId);
        }

        public void CloseAll()
        {
            m_parallelSyncPoint.Reset();

            // Clear physics
            foreach (var sector in PhysicsSectors.Values)
            {
                sector.EnablePhysics(false);

                if (sector.LodLevel == -1 && !sector.IsPendingLodSwitch)
                    m_sectorsClosing.Add(sector);
            }
            m_sectorsWithPhysics.Clear();

            // Clear graphics
            for (int index = 0; index < m_clipmaps.Length; index++)
            {
                ActiveFace = index;
                (ActiveClipmap = m_clipmaps[index]).Clear();
                EvaluateOperations();
            }

            ActiveFace = -1;
            ActiveClipmap = null;

            foreach (var sector in m_sectorsClosing)
            {
                if (sector.HasParallelWorkPending)
                    sector.DoParallelWork();

                if (sector.HasSerialWorkPending)
                    sector.DoSerialWork();

                sector.Close();
            }

            m_sectorsClosing.Clear();
            m_sectorsToWorkParallel.Clear();
            m_sectorsToWorkSerial.Clear();

            m_parallelSyncPoint.Set();
        }

        public bool TryGetSector(long id, out MyEnvironmentSector environmentSector)
        {
            if (!PhysicsSectors.TryGetValue(id, out environmentSector))
                return HeldSectors.TryGetValue(id, out environmentSector);
            return false;
        }
    }
}
