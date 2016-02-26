using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using VRageMath;
using Sandbox.Game.Components;
using VRage.Voxels;
using Sandbox.Game.World;
using VRage;
using Sandbox.Common;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Entities.Character;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using VRage.Library.Utils;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Engine.Physics;
using VRage.Generics;
using ParallelTasks;
using VRage.Utils;
using Sandbox.Game.World.Generator;
using VRage.ModAPI;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using System.Diagnostics;
using Sandbox.Engine.Utils;
using VRageRender;
using Sandbox.Game.Screens.Helpers;
using VRage.Game.ObjectBuilders.Definitions;
using Havok;
using VRage.Collections;
using VRage.Game.Entity;

namespace Sandbox.Game.Entities
{
    public partial class MyPlanet
    {
        /**
         * 
         * Planet sectors are attached to the faces of the heightmap cube and project towards the center of the planet.
         * 
         * Their size is adjusted so that the total number of sectors per face is an integer.
         * 
         * They are positioned at maximum hill height so they can be clearly seen and that their bounding boxes are clear.
         * 
         */

        // Expected average number of sectors in a planet.
        private const int SECTOR_POOL_SIZE = 128;

        private const float SECTOR_PHYSICS_EXTENT = .5f;

        private const float BASE_SECTOR_LOD0_EXTENT = 1.5f;
        private const float BASE_SECTOR_LOD1_EXTENT = 2.5f;
        private const float MAX_SECTOR_LOD1_EXTENT = 6f;

        private const float SECTOR_KEEP_INFLATE = .5f;

        // Minimum size for a sector, can be up to 99.999...% larger
        // When the number of sectors is at least 2 it can omly go up to 50% larger it's no reason for concern
        private const double MIN_SECTOR_SIZE = 768;

        public MyConcurrentDictionary<MyPlanetSectorId, List<int>> SavedSectors = new MyConcurrentDictionary<MyPlanetSectorId, List<int>>();

        private float SECTOR_LOD0_EXTENT;
        private float SECTOR_LOD1_EXTENT;
        
        public struct PlanetSectorRanges
        {
            public double SECTOR_LOD0_SQUARED;
            public double SECTOR_LOD0_KEEP_SQUARED;
            public double SECTOR_LOD1_SQUARED;
            public double SECTOR_LOD1_KEEP_SQUARED;

            public double PLANET_GRAPHICS_SCAN_MIN;
            public double PLANET_GRAPHICS_SCAN_MAX;
            public double PLANET_PHYSICS_SCAN_MIN;
            public double PLANET_PHYSICS_SCAN_MAX;
        }

        private MyDynamicAABBTreeD m_sectors;

        private void PrepareSectors()
        {
            ComputeSectorParameters();

            m_planetEnvironmentSectors = new Dictionary<MyPlanetSectorId, MyPlanetEnvironmentSector>();
            m_sectorsToRemove = new List<MyPlanetSectorId>((int)(Math.Ceiling(SECTOR_LOD1_EXTENT) * Math.Ceiling(SECTOR_LOD1_EXTENT) * 2));

            SECTOR_LOD0_EXTENT = BASE_SECTOR_LOD0_EXTENT;// *m_numSectors / 7f;
            SECTOR_LOD1_EXTENT = MathHelper.Lerp(BASE_SECTOR_LOD1_EXTENT, MAX_SECTOR_LOD1_EXTENT, (Provider.Radius - 9500) / (60000 - 9500));

            ComputeSectorRanges();


            m_sectors = new MyDynamicAABBTreeD(Vector3D.Zero);

            Hierarchy.QueryAABBImpl = Hierarchy_QueryAABB;
            Hierarchy.QueryLineImpl = Hierarchy_QueryLine;
            Hierarchy.QuerySphereImpl = Hierarchy_QuerySphere;

            InitCounters();
        }

        #region Performance stats

        [Conditional(VRageRender.Profiler.MyRenderProfiler.PerformanceProfilingSymbol)]
        private void InitCounters()
        {
            SectorScans = new MyDebugWorkTracker<int>(100);
            SectorOperations = new MyDebugWorkTracker<int>(100);
            SectorsScanned = new MyDebugWorkTracker<int>(100);
            SectorsClosed = new MyDebugWorkTracker<int>(100);
            SectorsCreated = new MyDebugWorkTracker<int>(100);
        }

        [Conditional(VRageRender.Profiler.MyRenderProfiler.PerformanceProfilingSymbol)]
        private void WrapCounters()
        {
            SectorScans.Wrap();
            SectorOperations.Wrap();
            SectorsScanned.Wrap();
            SectorsClosed.Wrap();
            SectorsCreated.Wrap();
        }

        public MyDebugWorkTracker<int> SectorScans;

        public MyDebugWorkTracker<int> SectorOperations;

        public MyDebugWorkTracker<int> SectorsScanned;

        public MyDebugWorkTracker<int> SectorsClosed;

        public MyDebugWorkTracker<int> SectorsCreated;

        public int SectorsWithPhysics
        {
            get;
            private set;
        }

        #endregion

        #region Hierarchy implementation

        private void Hierarchy_QueryAABB(BoundingBoxD query, List<MyEntity> results)
        {
            m_sectors.OverlapAllBoundingBox<MyEntity>(ref query, results, clear: false);
        }

        private void Hierarchy_QuerySphere(BoundingSphereD query, List<MyEntity> results)
        {
            m_sectors.OverlapAllBoundingSphere<MyEntity>(ref query, results, clear: false);
        }

        private void Hierarchy_QueryLine(LineD query, List<MyLineSegmentOverlapResult<MyEntity>> results)
        {
            m_sectors.OverlapAllLineSegment<MyEntity>(ref query, results, clear: false);
        }

        public void AddChildEntity(MyEntity child)
        {
            if (MyFakes.ENABLE_PLANET_HIERARCHY)
            {
                var bbox = child.PositionComp.WorldAABB;

                ProfilerShort.Begin("Add sector to tree.");
                int proxyId = m_sectors.AddProxy(ref bbox, child, 0);
                ProfilerShort.BeginNextBlock("Add to child hierarchy.");
                Hierarchy.AddChild(child, true);
                ProfilerShort.End();

                MyHierarchyComponentBase childHierarchy = child.Components.Get<MyHierarchyComponentBase>();
                childHierarchy.ChildId = proxyId;
            }
            else
            {
                MyEntities.Add(child);
            }
        }

        public void RemoveChildEntity(MyEntity child)
        {
            if (MyFakes.ENABLE_PLANET_HIERARCHY)
            {
                if (child.Parent == this)
                {
                    MyHierarchyComponentBase childHierarchy = child.Components.Get<MyHierarchyComponentBase>();
                    m_sectors.RemoveProxy((int)childHierarchy.ChildId);
                    Hierarchy.RemoveChild(child, true);
                }
            }
        }

        internal void CloseChildEntity(MyEntity child)
        {
            RemoveChildEntity(child);
            child.Close();
        }

        #endregion

        #region Fileds

        // Sector information 
        private double m_sectorSize; // Size of a sector
        private double m_sectorDensity; // per sqm density of items in sectors, comes from definitions
        private double m_itemsPerSector; // calculated number of items per sector
        private double m_sectorFaceSize; // Size of a whole cube face
        private int m_numSectors; // Numnber of sectors per face

        //Box that spans all sectors
        private BoundingBoxI m_sectorBox;

        // Sectors
        Dictionary<MyPlanetSectorId, MyPlanetEnvironmentSector> m_planetEnvironmentSectors; // list of spawned sectors
        MyDynamicObjectPool<MyPlanetEnvironmentSector> m_planetSectorsPool; // re-usable pool of sectors

        public Dictionary<MyPlanetSectorId, MyPlanetEnvironmentSector> EnvironmentSectors
        {
            get
            {
                return m_planetEnvironmentSectors;
            }
        }

        #endregion

        private void ComputeSectorParameters()
        {
            // face size is the length of an edge of a cube with half diagonal r.
            // this is equal to sqrt(2) * r
            m_sectorFaceSize = MaximumRadius * 1.41421356237309504880;

            // Ensure at least one sector.
            if (m_sectorFaceSize < MIN_SECTOR_SIZE)
            {
                m_numSectors = 1;
                m_sectorSize = m_sectorFaceSize;
            }
            else
            {
                m_numSectors = (int)Math.Floor(m_sectorFaceSize / MIN_SECTOR_SIZE);
                m_sectorSize = m_sectorFaceSize / m_numSectors;
            }

            m_sectorBox = new BoundingBoxI(0, m_numSectors - 1);

            m_sectorDensity = Generator.SectorDensity;

            m_itemsPerSector = m_sectorDensity * m_sectorSize * m_sectorSize;
        }

        private void ComputeSectorRanges()
        {
            Ranges.SECTOR_LOD0_SQUARED = m_sectorSize * SECTOR_LOD0_EXTENT;
            Ranges.SECTOR_LOD0_KEEP_SQUARED = m_sectorSize * (SECTOR_LOD0_EXTENT + SECTOR_KEEP_INFLATE);
            Ranges.SECTOR_LOD1_SQUARED = m_sectorSize * SECTOR_LOD1_EXTENT;
            Ranges.SECTOR_LOD1_KEEP_SQUARED = m_sectorSize * (SECTOR_LOD1_EXTENT + SECTOR_KEEP_INFLATE);

            Ranges.PLANET_GRAPHICS_SCAN_MIN = Math.Max(MinimumRadius - Ranges.SECTOR_LOD1_SQUARED, 0);
            Ranges.PLANET_GRAPHICS_SCAN_MAX = MaximumRadius + Ranges.SECTOR_LOD1_SQUARED;
            Ranges.PLANET_PHYSICS_SCAN_MIN = Math.Max(MinimumRadius - Ranges.SECTOR_LOD0_SQUARED, 0);
            Ranges.PLANET_PHYSICS_SCAN_MAX = MaximumRadius + Ranges.SECTOR_LOD0_SQUARED;

            Ranges.SECTOR_LOD0_SQUARED *= Ranges.SECTOR_LOD0_SQUARED;
            Ranges.SECTOR_LOD0_KEEP_SQUARED *= Ranges.SECTOR_LOD0_KEEP_SQUARED;
            Ranges.SECTOR_LOD1_SQUARED *= Ranges.SECTOR_LOD1_SQUARED;
            Ranges.SECTOR_LOD1_KEEP_SQUARED *= Ranges.SECTOR_LOD1_KEEP_SQUARED;
            Ranges.PLANET_GRAPHICS_SCAN_MIN *= Ranges.PLANET_GRAPHICS_SCAN_MIN;
            Ranges.PLANET_GRAPHICS_SCAN_MAX *= Ranges.PLANET_GRAPHICS_SCAN_MAX;
            Ranges.PLANET_PHYSICS_SCAN_MIN *= Ranges.PLANET_PHYSICS_SCAN_MIN;
            Ranges.PLANET_PHYSICS_SCAN_MAX *= Ranges.PLANET_PHYSICS_SCAN_MAX;
        }

        #region Internal Control

        private delegate void SectorIteration(MyPlanet me, ref MyPlanetSectorId id, ref Vector3D center);

        #region Spawning

        internal readonly MyConcurrentQueue<MyPlanetEnvironmentSector> SectorsToWorkParallel = new MyConcurrentQueue<MyPlanetEnvironmentSector>(PLANET_SECTOR_WORK_CYCLE * 10);
        internal readonly MyConcurrentQueue<MyPlanetEnvironmentSector> SectorsToWorkSerial = new MyConcurrentQueue<MyPlanetEnvironmentSector>(PLANET_SECTOR_WORK_CYCLE * 10);

        // Estimate how many sectors we could be removing at any given time.
        private List<MyPlanetSectorId> m_sectorsToRemove;

        // Weather a worker is currently handling sectors.
        // This is set conservativelly so we don't have race conditions.
        private bool m_sectorsWorking = false;

        /**
         * Scan sectors arround entities in planet and update them accordingly.
         */
        private void UpdateSectors(bool serial, ref BoundingBoxD box)
        {
            // Prepare sectors for update
            foreach (var sp in m_planetEnvironmentSectors)
            {
                sp.Value.PrepareForUpdate();
            }

            // Find all entities, spawn physics arround ships and players, spawn graphics arround cameras

            ProfilerShort.Begin("Update Sectors");
            MyGamePruningStructure.GetTopMostEntitiesInBox(ref box, m_entities, MyEntityQueryType.Dynamic);

            foreach (var entity in m_entities)
            {
                // entity is MyPlanet || entity is MyVoxelMap || entity is MyEnvironmentItems should all be static
                // If any of that changes keep in mind they must be skipped
                // It is also important to avoid adding physics where there are no clusters, otherwise the entities won't get activated properly.
                if (entity.MarkedForClose || entity.Physics == null || entity.Physics.IsStatic)
                    continue;

                Vector3 position = entity.PositionComp.GetPosition() - WorldMatrix.Translation;
                double distanceSq = position.LengthSquared();

                var predictionOffset = ComputePredictionOffset(entity);

                if (CanSpawnFlora && RUN_SECTORS && distanceSq >= Ranges.PLANET_PHYSICS_SCAN_MIN && distanceSq <= Ranges.PLANET_PHYSICS_SCAN_MAX)
                {
                    ProfilerShort.Begin("EntitySpawn");
                    position += predictionOffset;

                    MyPlanetSectorId sectId;
                    GetSectorIdAt(position, out sectId);

                    MyPlanetEnvironmentSector sector;
                    if (!m_planetEnvironmentSectors.TryGetValue(sectId, out sector) || !sector.HasEntity)
                    {
                        ForEachSector(position, SECTOR_PHYSICS_EXTENT + SECTOR_KEEP_INFLATE, SectorPhysicsDelegate);
                    }
                    if ((sector != null || m_planetEnvironmentSectors.TryGetValue(sectId, out sector)) && !sector.ShouldClose)
                    {
                        // Make sure other entities in same sector do not cause new scans.
                        sector.HasEntity = true;
                    }
                    ProfilerShort.End();
                }
            }

            if (CanSpawnFlora && RUN_SECTORS)
            {
                ProfilerShort.Begin("MainCameraSpawn");
                Vector3D position = MySector.MainCamera.Position - WorldMatrix.Translation;
                double distanceSq = position.LengthSquared();

                if (distanceSq > Ranges.PLANET_GRAPHICS_SCAN_MIN && distanceSq < Ranges.PLANET_GRAPHICS_SCAN_MAX)
                {
                    ForEachSector(position, SECTOR_LOD1_EXTENT + SECTOR_KEEP_INFLATE, SectorGraphicsDelegate);
                }
                ProfilerShort.End();

                // Remove sectors marked for removal and enqueue sectors with pending operations.

                ProfilerShort.Begin("Recycle Sectors");
                m_sectorsToRemove.Clear();
                foreach (var sp in m_planetEnvironmentSectors)
                {
                    var sector = sp.Value;
                    using (sector.AcquireStatusLock())
                    {
                        sector.EvaluateOperations();

                        if (sector.ShouldClose)
                        {
                            if ((sector.PendingOperations & MyPlanetEnvironmentSector.SERIAL_OPERATIONS_MASK) != 0)
                            {
                                // This will close the sector here if necessary.
                                sector.DoSerialWork(false);
                            }
                            Debug.Assert(sector.IsClosed);
                            m_planetSectorsPool.Deallocate(sector);
                            m_sectorsToRemove.Add(sp.Key);
                        }
                        else if (sector.ParallelPending)
                        {
                            if (!sector.IsQueuedParallel && sector.ParallelPending)
                            {
                                sector.IsQueuedParallel = true;
                                SectorsToWorkParallel.Enqueue(sector);
                            }
                        }
                        else if (sector.SerialPending && !sector.IsQueuedSerial)
                        {
                            SectorsToWorkSerial.Enqueue(sector);
                            sector.IsQueuedSerial = true;
                        }
                    }
                }

                // Remove from the dictionary all the sectors that were closed
                foreach (var sector in m_sectorsToRemove)
                {
                    m_planetEnvironmentSectors.Remove(sector);
                }
                ProfilerShort.End();

                ProfilerShort.Begin("Schedule Tasks");
                // Lastly we start the sectors worker if any work is left in the queue.
                if (!m_sectorsWorking)
                {
                    if (SectorsToWorkParallel.Count > 0)
                    {
                        m_sectorsWorking = true;

                        if (serial)
                        {
                            ParallelWorkCallback();
                            SerialWorkCallback();
                        }
                        else
                        {
                            Parallel.Start(m_parallelWorkDelegate, m_serialWorkDelegate);
                        }
                    }
                    else
                    {
                        SerialWorkCallback();
                    }
                }
                ProfilerShort.End();
            }

            ProfilerShort.End();

            WrapCounters();
        }


        #region Callbacks

        // How many sectors to do work for per parallel tasks
        private static readonly int PLANET_SECTOR_WORK_CYCLE = 5;
        private static readonly int PLANET_SECTOR_IDLE_WORK_CYCLE = 50; // maximum number of cancelled sector requests to process


        /**
         * Find or create sector for scanning
         */
        private static bool GetOrCreateSector(MyPlanet me, ref MyPlanetSectorId id, ref Vector3D position, double minDistance, out Vector3D pos, out double distance, out MyPlanetEnvironmentSector sector)
        {
            if (!me.m_planetEnvironmentSectors.TryGetValue(id, out sector))
            {
                BoundingBoxD box;
                me.SectorIdToBoundingBox(ref id, out box);
                pos = box.Center;
                var posf = (Vector3)pos;

                pos = me.GetClosestSurfacePointLocal(ref posf);
                Vector3D.DistanceSquared(ref position, ref pos, out distance);

                if (distance > minDistance) return false;

                // Build allocator if necessary
                if (me.m_planetSectorsPool == null)
                    me.m_planetSectorsPool = new MyDynamicObjectPool<MyPlanetEnvironmentSector>(SECTOR_POOL_SIZE);

                // Try to get from cache if possible
                sector = me.m_planetSectorsPool.Allocate();
                sector.Init(ref id, me);

                me.m_planetEnvironmentSectors.Add(id, sector);
                me.SectorsCreated.Hit();
            }
            else
            {
                if (sector.IsClosed)
                    sector.Init(ref id, me);

                pos = sector.LocalSurfaceCenter;
                Vector3D.DistanceSquared(ref position, ref pos, out distance);
            }
            return true;
        }

        private static readonly SectorIteration SectorGraphicsDelegate = SectorGraphicsCallback;
        private static void SectorGraphicsCallback(MyPlanet me, ref MyPlanetSectorId id, ref Vector3D position)
        {
            double distance;
            Vector3D pos;

            MyPlanetEnvironmentSector sector;
            if (!GetOrCreateSector(me, ref id, ref position, me.Ranges.SECTOR_LOD1_SQUARED, out pos, out distance, out sector)) return;

            if (distance <= me.Ranges.SECTOR_LOD1_KEEP_SQUARED)
            {
                me.SectorsScanned.Hit();
                using (sector.AcquireStatusLock())
                {
                    MyPlanetSectorOperation ops = sector.PendingOperations;

                    ops &= ~(MyPlanetSectorOperation.Close | MyPlanetSectorOperation.CloseGraphics);

                    if (distance <= me.Ranges.SECTOR_LOD1_SQUARED)
                    {
                        if (!sector.Loaded)
                            ops |= MyPlanetSectorOperation.Spawn;

                        ops |= MyPlanetSectorOperation.SpawnGraphics;

                        if (distance <= me.Ranges.SECTOR_LOD0_KEEP_SQUARED)
                        {
                            ops &= ~MyPlanetSectorOperation.CloseDetails;

                            if (distance <= me.Ranges.SECTOR_LOD0_SQUARED)
                                ops |= MyPlanetSectorOperation.SpawnDetails;
                        }
                    }

                    sector.PendingOperations = ops;
                }
            }
        }

        private static readonly SectorIteration SectorPhysicsDelegate = SectorPhysicsCallback;
        private static void SectorPhysicsCallback(MyPlanet me, ref MyPlanetSectorId id, ref Vector3D position)
        {
            double distance;
            Vector3D pos;
            MyPlanetEnvironmentSector sector;

            if (!GetOrCreateSector(me, ref id, ref position, me.Ranges.SECTOR_LOD0_SQUARED, out pos, out distance, out sector)) return;

            me.SectorsScanned.Hit();
            if (distance <= me.Ranges.SECTOR_LOD0_KEEP_SQUARED)
            {
                using (sector.AcquireStatusLock())
                {
                    MyPlanetSectorOperation ops = sector.PendingOperations;

                    if (sector.HasPhysics || ops.HasFlags(MyPlanetSectorOperation.SpawnPhysics))
                    {
                        ops &= ~(MyPlanetSectorOperation.Close | MyPlanetSectorOperation.ClosePhysics | MyPlanetSectorOperation.CloseDetails);
                    }
                    else if (distance <= me.Ranges.SECTOR_LOD0_SQUARED)
                    {
                        ops |= MyPlanetSectorOperation.SpawnPhysics | MyPlanetSectorOperation.SpawnDetails;
                        if (!sector.Loaded)
                            ops |= MyPlanetSectorOperation.Spawn;
                        ops &= ~(MyPlanetSectorOperation.Close | MyPlanetSectorOperation.ClosePhysics | MyPlanetSectorOperation.CloseDetails);
                    }
                    sector.PendingOperations = ops;
                }
            }

        }

        private readonly Action m_parallelWorkDelegate;
        private void ParallelWorkCallback()
        {
            ProfilerShort.Begin("Planet::ParallelWork");

            int work = PLANET_SECTOR_WORK_CYCLE;
            int discard = PLANET_SECTOR_IDLE_WORK_CYCLE;

            for (; work > 0 && SectorsToWorkParallel.Count > 0 && discard > 0; )
            {
                var sector = SectorsToWorkParallel.Dequeue();

                // When the sector is deleted it is sometimes impossible to ensure
                // it was not queued so we ignore it here
                if (sector.IsQueuedParallel)
                {
                    if (sector.DoParallelWork())
                        --work;
                    else --discard;
                }

                // It may have new work already but will get re-scheduled eventually.
                sector.IsQueuedParallel = false;
            }
            ProfilerShort.End();
        }

        public PlanetSectorRanges Ranges;

        private readonly Action m_serialWorkDelegate;
        private void SerialWorkCallback()
        {
            int work = PLANET_SECTOR_WORK_CYCLE;
            for (; work > 0 && SectorsToWorkSerial.Count > 0; --work)
            {
                var sector = SectorsToWorkSerial.Dequeue();

                sector.DoSerialWork();
            }

            m_sectorsWorking = false;
        }

        #endregion

        #endregion

        // Compute the position in the sector cube from planet space coordinates.
        private void LocalPositionToSectorCube(Vector3D localCoords, out Vector3 sectorCoords)
        {
            Vector3 textcoord;

            MyCubemapHelpers.ProjectToNearestFace(ref localCoords, out textcoord);

            // bring textcoords into 0-1 space.
            textcoord = (textcoord + 1f) * .5f;

            sectorCoords = new Vector3(textcoord.X * m_numSectors, textcoord.Y * m_numSectors, textcoord.Z * m_numSectors);

            if (sectorCoords.X == m_numSectors) sectorCoords.X = (m_numSectors - 1);
            if (sectorCoords.Y == m_numSectors) sectorCoords.Y = (m_numSectors - 1);
            if (sectorCoords.Z == m_numSectors) sectorCoords.Z = (m_numSectors - 1);
        }

        // Iterate over sector boxes in a range.
        private void ForEachSector(Vector3 localPosition, float range, SectorIteration iterator)
        {
            Vector3 sectorPosition;
            LocalPositionToSectorCube(localPosition, out sectorPosition);

            Vector3D referencePoint = localPosition;

            SectorScans.Hit();

            Vector3 rangev = new Vector3(range);

            BoundingBox probe = new BoundingBox(sectorPosition - rangev, sectorPosition + rangev);
            BoundingBoxI inter = m_sectorBox.Intersect(probe);

            Vector3S min = new Vector3S(inter.Min);
            Vector3S max = new Vector3S(inter.Max);


            // Iterate over each face of the cube that is in the intersection.

            MyPlanetSectorId id = new MyPlanetSectorId();

            // Front
            if (inter.Min.Z == m_sectorBox.Min.Z)
            {
                id.Direction = Vector3B.Backward;
                id.Position.Z = min.Z;
                for (id.Position.X = min.X; id.Position.X <= max.X; id.Position.X++)
                    for (id.Position.Y = min.Y; id.Position.Y <= max.Y; id.Position.Y++)
                        iterator(this, ref id, ref referencePoint);
            }

            // Back
            if (inter.Max.Z == m_sectorBox.Max.Z)
            {
                id.Direction = Vector3B.Forward;
                id.Position.Z = max.Z;
                for (id.Position.X = min.X; id.Position.X <= max.X; id.Position.X++)
                    for (id.Position.Y = min.Y; id.Position.Y <= max.Y; id.Position.Y++)
                        iterator(this, ref id, ref referencePoint);
            }

            // Right
            if (inter.Min.X == m_sectorBox.Min.X)
            {
                id.Direction = Vector3B.Right;
                id.Position.X = min.X;
                for (id.Position.Y = min.Y; id.Position.Y <= max.Y; id.Position.Y++)
                    for (id.Position.Z = min.Z; id.Position.Z <= max.Z; id.Position.Z++)
                        iterator(this, ref id, ref referencePoint);
            }

            // Left
            if (inter.Max.X == m_sectorBox.Max.X)
            {
                id.Direction = Vector3B.Left;
                id.Position.X = max.X;
                for (id.Position.Y = min.Y; id.Position.Y <= max.Y; id.Position.Y++)
                    for (id.Position.Z = min.Z; id.Position.Z <= max.Z; id.Position.Z++)
                        iterator(this, ref id, ref referencePoint);
            }

            // Up
            if (inter.Min.Y == m_sectorBox.Min.Y)
            {
                id.Direction = Vector3B.Up;
                id.Position.Y = min.Y;
                for (id.Position.X = min.X; id.Position.X <= max.X; id.Position.X++)
                    for (id.Position.Z = min.Z; id.Position.Z <= max.Z; id.Position.Z++)
                        iterator(this, ref id, ref referencePoint);
            }

            // Down
            if (inter.Max.Y == m_sectorBox.Max.Y)
            {
                id.Direction = Vector3B.Down;
                id.Position.Y = max.Y;
                for (id.Position.X = min.X; id.Position.X <= max.X; id.Position.X++)
                    for (id.Position.Z = min.Z; id.Position.Z <= max.Z; id.Position.Z++)
                        iterator(this, ref id, ref referencePoint);
            }
        }

        #endregion

        #region External Access

        public void GetSectorIdAt(Vector3 localPosition, out MyPlanetSectorId id)
        {
            id = new MyPlanetSectorId();

            Vector3D local = localPosition;
            MyCubemapHelpers.ProjectToNearestFace(ref local, out localPosition);
            MyCubemapHelpers.GetCubeFaceDirection(ref localPosition, out id.Direction);
            id.Direction = -id.Direction;

            localPosition = (localPosition + 1f) * .5f;

            id.Position = new Vector3S(localPosition.X * m_numSectors, localPosition.Y * m_numSectors, localPosition.Z * m_numSectors);
            if (id.Position.X == m_numSectors) id.Position.X = (short)(m_numSectors - 1);
            if (id.Position.Y == m_numSectors) id.Position.Y = (short)(m_numSectors - 1);
            if (id.Position.Z == m_numSectors) id.Position.Z = (short)(m_numSectors - 1);
        }

        public void SectorIdToWorldBoundingBox(ref MyPlanetSectorId id, out BoundingBox sectorBox)
        {
            Vector3I modifiedPosition = id.Position - id.Direction;
            Vector3 min = modifiedPosition * m_sectorSize;
            min += WorldMatrix.Translation - m_sectorFaceSize * .5;
            Vector3 max = min + (float)m_sectorSize;

            sectorBox = new BoundingBox(min, max);
        }

        public void SectorIdToWorldBoundingBox(ref MyPlanetSectorId id, out BoundingBoxD sectorBox)
        {
            Vector3I modifiedPosition = id.Position - id.Direction;
            Vector3D min = modifiedPosition * m_sectorSize;
            min += WorldMatrix.Translation - m_sectorFaceSize * .5;
            Vector3D max = min + (float)m_sectorSize;

            sectorBox = new BoundingBoxD(min, max);
        }

        public void SectorIdToBoundingBox(ref MyPlanetSectorId id, out BoundingBoxD sectorBox)
        {
            Vector3I modifiedPosition = id.Position - id.Direction;
            Vector3D min = (modifiedPosition * m_sectorSize);
            min += -m_sectorFaceSize * .5;
            Vector3D max = min + m_sectorSize;

            sectorBox = new BoundingBoxD(min, max);
        }

        public MyPlanetEnvironmentSector GetSector(ref MyPlanetSectorId id)
        {
            MyPlanetEnvironmentSector sect;
            m_planetEnvironmentSectors.TryGetValue(id, out sect);
            return sect;
        }

        public double SectorSize { get { return m_sectorSize; } }

        public double SectorDensity { get { return m_sectorDensity * MySession.Static.Settings.FloraDensityMultiplier; } }

        public int SectorTotalItems { get { return (int)(m_itemsPerSector * MySession.Static.Settings.FloraDensityMultiplier); } }

        public IEnumerable<MyEnvironmentItems> GetEnvironmentItemsAtPosition(ref Vector3D position)
        {
            MyPlanetSectorId sectId;
            GetSectorIdAt(position - WorldMatrix.Translation, out sectId);
            MyPlanetEnvironmentSector sector = GetSector(ref sectId);
            if (sector != null)
                return sector.GetItems();
            else return Enumerable.Empty<MyEnvironmentItems>();
        }

        #endregion
    }
}
