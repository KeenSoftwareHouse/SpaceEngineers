using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using VRage;
using VRage.Collections;
using VRage;
using VRage.Noise;
using VRage.Noise.Combiners;
using VRageMath;
using VRage.Voxels;
using VRage.Library.Utils;

namespace Sandbox.Game.World.Generator
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, 500)]
    public class MyAsteroidCellGenerator : MySessionComponentBase
    {
        private static int OBJECT_SIZE_MIN = 64;
        private static int OBJECT_SIZE_MAX = 512;
        private static int SUBCELL_SIZE = 4 * 1024 + OBJECT_SIZE_MAX * 2;
        private static int SUBCELLS = 3;
        private static double CELL_SIZE = SUBCELL_SIZE * SUBCELLS;
        private double m_objectDensity = -1; // needs to be -1..1

        private static int OBJECT_MAX_IN_CLUSTER = 4;
        private static int OBJECT_MIN_DISTANCE_CLUSTER = 128;
        private static int OBJECT_MAX_DISTANCE_CLUSTER = 256;
        private static int OBJECT_SIZE_MIN_CLUSTER = 32;
        private static int OBJECT_SIZE_MAX_CLUSTER = 64;
        private static double OBJECT_DENSITY_CLUSTER = 0.50 * 2 - 1; // needs to be -1..1

        public static MyAsteroidCellGenerator Static;

        public class MyProceduralCell
        {
            public Vector3I CellId
            {
                get;
                private set;
            }

            public BoundingBoxD BoundingVolume
            {
                get;
                private set;
            }

            public int proxyId = -1;
            private MyDynamicAABBTreeD m_tree = new MyDynamicAABBTreeD(Vector3D.Zero);

            public void AddObject(MyObjectSeed objectSeed)
            {
                var bbox = objectSeed.BoundingVolume;
                objectSeed.m_proxyId = m_tree.AddProxy(ref bbox, objectSeed, 0);
            }

            public MyProceduralCell(Vector3I cellId)
            {
                CellId = cellId;
                BoundingVolume = new BoundingBoxD(CellId * CELL_SIZE, (CellId + 1) * CELL_SIZE);
            }

            public void OverlapAllBoundingSphere(ref BoundingSphereD sphere, List<MyObjectSeed> list, bool clear = false)
            {
                m_tree.OverlapAllBoundingSphere(ref sphere, list, clear);
            }

            public void OverlapAllBoundingBox(ref BoundingBoxD box, List<MyObjectSeed> list, bool clear = false)
            {
                m_tree.OverlapAllBoundingBox(ref box, list, 0, clear);
            }

            public void GetAll(List<MyObjectSeed> list, bool clear = true)
            {
                m_tree.GetAll(list, clear);
            }

            public override int GetHashCode()
            {
                return CellId.GetHashCode();
            }

            public override string ToString()
            {
                return CellId.ToString();
            }
        }

        public class MyObjectSeed
        {
            public int Index = 0;
            public int Seed = 0;
            public MyObjectSeedType Type = MyObjectSeedType.Asteroid;
            public bool Generated = false;

            public int m_proxyId = -1;

            public BoundingBoxD BoundingVolume
            {
                get;
                private set;
            }
            public float Size
            {
                get;
                private set;
            }

            public MyProceduralCell Cell
            {
                get;
                private set;
            }

            public Vector3I CellId
            {
                get { return Cell.CellId; }
            }

            public MyObjectSeed(MyProceduralCell cell, Vector3D position, double size)
            {
                Cell = cell;
                Size = (float)size;
                BoundingVolume = new BoundingBoxD(position - size, position + size);
            }
        }

        public enum MyObjectSeedType
        {
            Asteroid,
            AsteroidCluster,
            EncounterAlone,
            EncounterSingle,
            EncounterMulti,
        }

        private double m_seedTypeProbabilitySum;
        private Dictionary<MyObjectSeedType, double> m_seedTypeProbability = new Dictionary<MyObjectSeedType, double>()
        {
            {MyObjectSeedType.Asteroid, 800},
            {MyObjectSeedType.AsteroidCluster, 300},
            {MyObjectSeedType.EncounterAlone, 1},
        };

        private double m_seedClusterTypeProbabilitySum;
        private Dictionary<MyObjectSeedType, double> m_seedClusterTypeProbability = new Dictionary<MyObjectSeedType, double>()
        {
            {MyObjectSeedType.Asteroid, 300},
            {MyObjectSeedType.EncounterSingle, 3},
            {MyObjectSeedType.EncounterMulti, 2},
        };

        private int m_seed = 0;
        private List<IMyAsteroidFieldDensityFunction> m_densityFunctions = new List<IMyAsteroidFieldDensityFunction>();

        private MyDynamicAABBTreeD m_cellsTree = new MyDynamicAABBTreeD(Vector3D.Zero);

        private Dictionary<MyEntity, MyEntityTracker> m_trackedEntities = new Dictionary<MyEntity, MyEntityTracker>();
        private Dictionary<MyEntity, MyEntityTracker> m_toAddTrackedEntities = new Dictionary<MyEntity, MyEntityTracker>();

        List<MyObjectSeed> m_tempObjectSeedList = new List<MyObjectSeed>();
        List<MyProceduralCell> m_tempProceduralCellsList = new List<MyProceduralCell>();

        private Dictionary<Vector3I, MyProceduralCell> m_cells = new Dictionary<Vector3I, MyProceduralCell>();

        public class MyEntityTracker
        {
            public MyEntity Entity
            {
                get;
                private set;
            }
            public BoundingSphereD BoundingVolume = new BoundingSphereD(Vector3D.PositiveInfinity, 0);

            public Vector3D CurrentPosition
            {
                get { return Entity.PositionComp.WorldAABB.Center; }
            }

            public Vector3D LastPosition
            {
                get { return BoundingVolume.Center; }
                private set { BoundingVolume.Center = value; }
            }

            public double Radius
            {
                get { return BoundingVolume.Radius; }
                set
                {
                    Tolerance = MathHelper.Clamp(value / 2, 128, 512);
                    BoundingVolume.Radius = value + Tolerance;
                }
            }

            public double Tolerance
            {
                get;
                private set;
            }

            public MyEntityTracker(MyEntity entity, double radius)
            {
                Entity = entity;
                Radius = radius;
            }

            public bool ShouldGenerate()
            {
                return !Entity.Closed && Entity.Save && (CurrentPosition - LastPosition).Length() > Tolerance;
            }

            public void UpdateLastPosition()
            {
                LastPosition = CurrentPosition;
            }

            public override string ToString()
            {
                return Entity.ToString() + ", " + BoundingVolume.ToString() + ", " + Tolerance.ToString();
            }
        }

        public DictionaryReader<MyEntity, MyEntityTracker> GetTrackedEntities()
        {
            return new DictionaryReader<MyEntity, MyEntityTracker>(m_trackedEntities);
        }

        public override void LoadData()
        {
            Static = this;
            if (!MyFakes.ENABLE_ASTEROID_FIELDS)
                return;

            var settings = MySession.Static.Settings;
            if (settings.ProceduralDensity == 0f)
            {
                m_densityFunctions.Clear();
                MySandboxGame.Log.WriteLine("Skip Procedural World Generator");
                return;
            }

            m_seed = settings.ProceduralSeed;
            m_objectDensity = settings.ProceduralDensity * 2 - 1; // must be -1..1
            MySandboxGame.Log.WriteLine(string.Format("Loading Procedural World Generator: Seed = '{0}' = {1}, Density = {2}", settings.ProceduralSeed, m_seed, settings.ProceduralDensity));

            using (MyRandom.Instance.PushSeed(m_seed))
            {
                m_densityFunctions.Add(new MyAsteroidInfiniteDensityFunction(MyRandom.Instance, 0.003));
            }

            m_seedTypeProbabilitySum = 0;
            foreach (var probability in m_seedTypeProbability.Values)
            {
                m_seedTypeProbabilitySum += probability;
            }
            Debug.Assert(m_seedTypeProbabilitySum != 0);

            m_seedClusterTypeProbabilitySum = 0;
            foreach (var probability in m_seedClusterTypeProbability.Values)
            {
                m_seedClusterTypeProbabilitySum += probability;
            }
            Debug.Assert(m_seedClusterTypeProbabilitySum != 0);
        }

        protected override void UnloadData()
        {
            if (!MyFakes.ENABLE_ASTEROID_FIELDS)
                return;
            MySandboxGame.Log.WriteLine("Unloading Procedural World Generator");
            m_densityFunctions.Clear();

            m_cellsTree.Clear();
            m_trackedEntities.Clear();
            m_cells.Clear();

            Debug.Assert(m_tempObjectSeedList.Count == 0, "temp list is not empty!");
            m_tempObjectSeedList.Clear();

            Debug.Assert(m_tempProceduralCellsList.Count == 0, "temp list is not empty!");
            m_tempProceduralCellsList.Clear();

            Debug.Assert(m_tmpClusterBoxes.Count == 0, "temp list is not empty!");
            m_tmpClusterBoxes.Clear();

            Debug.Assert(m_tmpVoxelMapsList.Count == 0, "temp list is not empty!");
            m_tmpVoxelMapsList.Clear();

            Debug.Assert(m_dirtyCellsToRemove.Count == 0, "temp list is not empty!");
            m_dirtyCellsToRemove.Clear();

            Debug.Assert(m_dirtyCells.Count == 0, "temp list is not empty!");
            m_dirtyCells.Clear();

            m_dirtyCellsToAdd.Clear(); // this can have some cells if the unload happened after removing tracked entity

            Static = null;
        }

        private HashSet<MyProceduralCell> m_dirtyCells = new HashSet<MyProceduralCell>();
        private List<MyProceduralCell> m_dirtyCellsToRemove = new List<MyProceduralCell>();
        private List<MyProceduralCell> m_dirtyCellsToAdd = new List<MyProceduralCell>();

        public override void UpdateBeforeSimulation()
        {
            if (m_densityFunctions.Count != 0)
            {
                ProfilerShort.Begin("Add tracked entities");
                if (m_toAddTrackedEntities.Count != 0)
                {
                    foreach (var pair in m_toAddTrackedEntities)
                    {
                        m_trackedEntities.Add(pair.Key, pair.Value);
                    }
                    m_toAddTrackedEntities.Clear();
                }
                ProfilerShort.End();

                foreach (var tracker in m_trackedEntities.Values)
                {
                    if (tracker.ShouldGenerate())
                    {
                        var oldBoundingVolume = tracker.BoundingVolume;
                        tracker.UpdateLastPosition();
                        //if (!(tracker.Entity is MyCubeGrid && ((MyCubeGrid)tracker.Entity).IsTrash()))
                        GenerateObjects(tracker.BoundingVolume);

                        MarkCellsDirty(oldBoundingVolume, tracker.BoundingVolume);
                    }
                }

                ProcessDirtyCells();
            }

            if (!MySandboxGame.AreClipmapsReady && MySession.Static.VoxelMaps.Instances.Count == 0)
            {
                // Render will not send any message if it has no clipmaps, so we have to specify there is nothing to wait for.
                MySandboxGame.SignalClipmapsReady();
            }
        }

        private void ProcessDirtyCells()
        {
            foreach (var cell in m_dirtyCellsToAdd)
            {
                m_dirtyCells.Add(cell);
            }
            m_dirtyCellsToAdd.Clear();

            if (m_dirtyCells.Count == 0)
            {
                return;
            }
            ProfilerShort.Begin("Find false possitive dirty cells");
            foreach (var cell in m_dirtyCells)
            {
                foreach (var tracker in m_trackedEntities.Values)
                {
                    if (tracker.BoundingVolume.Contains(cell.BoundingVolume) != ContainmentType.Disjoint)
                    {
                        m_dirtyCellsToRemove.Add(cell);
                        break;
                    }
                }
            }

            ProfilerShort.BeginNextBlock("Remove false possitive dirty cells");
            foreach (var cell in m_dirtyCellsToRemove)
            {
                m_dirtyCells.Remove(cell);
            }
            m_dirtyCellsToRemove.Clear();

            ProfilerShort.BeginNextBlock("Remove stuff");
            foreach (var cell in m_dirtyCells)
            {
                cell.GetAll(m_tempObjectSeedList);

                foreach (var objectSeed in m_tempObjectSeedList)
                {
                    switch (objectSeed.Type)
                    {
                        case MyObjectSeedType.Asteroid:
                        case MyObjectSeedType.AsteroidCluster:
                            var bbox = objectSeed.BoundingVolume;
                            MyGamePruningStructure.GetAllVoxelMapsInBox(ref bbox, m_tmpVoxelMapsList);

                            String storageName = string.Format("Asteroid_{0}_{1}_{2}_{3}_{4}", cell.CellId.X, cell.CellId.Y, cell.CellId.Z, objectSeed.Index, objectSeed.Seed);

                            foreach (var voxelMap in m_tmpVoxelMapsList)
                            {
                                if (voxelMap.StorageName == storageName)
                                {
                                    if (!voxelMap.Save) // for now
                                    {
                                        m_asteroidCount--;
                                        voxelMap.Close();
                                    }
                                    break;
                                }
                            }
                            m_asteroidSeedCount--;
                            m_tmpVoxelMapsList.Clear();
                            break;
                        case MyObjectSeedType.EncounterAlone:
                        case MyObjectSeedType.EncounterSingle:
                        case MyObjectSeedType.EncounterMulti:
                            if (MyEncounterGenerator.RemoveEncounter(objectSeed.BoundingVolume, objectSeed.Seed))
                            {
                                m_encounterCount--;
                            }
                            m_encounterSeedCount--;
                            break;
                        default:
                            throw new InvalidBranchException();
                            break;
                    }
                }
            }
            m_tempObjectSeedList.Clear();

            ProfilerShort.BeginNextBlock("Remove dirty cells");
            foreach (var cell in m_dirtyCells)
            {
                m_cells.Remove(cell.CellId);
                m_cellsTree.RemoveProxy(cell.proxyId);
            }
            m_dirtyCells.Clear();
            ProfilerShort.End();
        }

        private void MarkCellsDirty(BoundingSphereD toMark)
        {
            ProfilerShort.Begin("Mark dirty cells");
            Vector3I cellId = Vector3I.Floor((toMark.Center - toMark.Radius) / CELL_SIZE);
            for (var iter = GetCellsIterator(toMark); iter.IsValid(); iter.GetNext(out cellId))
            {
                MyProceduralCell cell;
                if (m_cells.TryGetValue(cellId, out cell))
                {
                    m_dirtyCellsToAdd.Add(cell);
                }
            }
            ProfilerShort.End();
        }

        private void MarkCellsDirty(BoundingSphereD toMark, BoundingSphereD toExclude)
        {
            ProfilerShort.Begin("Mark dirty cells");
            Vector3I cellId = Vector3I.Floor((toMark.Center - toMark.Radius) / CELL_SIZE);
            for (var iter = GetCellsIterator(toMark); iter.IsValid(); iter.GetNext(out cellId))
            {
                MyProceduralCell cell;
                if (m_cells.TryGetValue(cellId, out cell))
                {
                    if (toExclude.Contains(cell.BoundingVolume) == ContainmentType.Disjoint)
                    {
                        m_dirtyCellsToAdd.Add(cell);
                    }
                }
            }
            ProfilerShort.End();
        }

        public void TrackEntity(MyEntity entity)
        {
            if (m_densityFunctions.Count == 0)
                return;

            if (entity is MyCharacter)
            {
                TrackEntity(entity, MySession.Static.Settings.ViewDistance); // should be farplane
            }
            if (entity is MyCameraBlock)
            {
                TrackEntity(entity, Math.Min(10000, MySession.Static.Settings.ViewDistance));
            }
            if (entity is MyRemoteControl)
            {
                TrackEntity(entity, Math.Min(10000, MySession.Static.Settings.ViewDistance));
            }
            if (entity is MyCubeGrid)
            {
                TrackEntity(entity, entity.PositionComp.WorldAABB.HalfExtents.Length());
            }
        }

        private void TrackEntity(MyEntity entity, double range)
        {
            MyEntityTracker tracker;
            if (m_trackedEntities.TryGetValue(entity, out tracker) || m_toAddTrackedEntities.TryGetValue(entity, out tracker))
            {
                tracker.Radius = range;
            }
            else
            {
                tracker = new MyEntityTracker(entity, range);
                m_toAddTrackedEntities.Add(entity, tracker);
                entity.OnMarkForClose += (e) =>
                {
                    m_trackedEntities.Remove(e);
                    m_toAddTrackedEntities.Remove(e);
                    MarkCellsDirty(tracker.BoundingVolume);
                };
            }
        }

        private void OverlapAllBoundingSphere(ref BoundingSphereD sphere, List<MyObjectSeed> list)
        {
            m_cellsTree.OverlapAllBoundingSphere(ref sphere, m_tempProceduralCellsList);
            foreach (var cell in m_tempProceduralCellsList)
            {
                cell.OverlapAllBoundingSphere(ref sphere, list);
            }
            m_tempProceduralCellsList.Clear();
        }

        private void OverlapAllBoundingBox(ref BoundingBoxD box, List<MyObjectSeed> list)
        {
            m_cellsTree.OverlapAllBoundingBox(ref box, m_tempProceduralCellsList);
            foreach (var cell in m_tempProceduralCellsList)
            {
                cell.OverlapAllBoundingBox(ref box, list);
            }
            m_tempProceduralCellsList.Clear();
        }

        public void GetAllCells(List<MyProceduralCell> list)
        {
            m_cellsTree.GetAll(list, true);
        }

        public void GetAll(List<MyObjectSeed> list)
        {
            m_cellsTree.GetAll(m_tempProceduralCellsList, false);
            foreach (var cell in m_tempProceduralCellsList)
            {
                cell.GetAll(list, false);
            }
            m_tempProceduralCellsList.Clear();
        }

        public long CellCount
        {
            get { return m_cells.Count; }
        }

        private long m_encounterSeedCount = 0;
        public long EncounterSeedCount
        {
            get { return m_encounterSeedCount; }
        }

        private long m_asteroidSeedCount = 0;
        public long AsteroidSeedCount
        {
            get { return m_asteroidSeedCount; }
        }

        private long m_asteroidClusterSeedCount = 0;
        public long AsteroidClusterSeedCount
        {
            get { return m_asteroidClusterSeedCount; }
        }

        private long m_encounterCount = 0;
        public long EncounterCount
        {
            get { return m_encounterCount; }
        }

        private long m_asteroidCount = 0;
        public long AsteroidCount
        {
            get { return m_asteroidCount; }
        }

        public void GenerateObjects(BoundingSphereD sphere)
        {
            ProfilerShort.Begin("GenerateObjectsInSphere");
            GetObjectSeeds(sphere, m_tempObjectSeedList);
            GenerateObjects(m_tempObjectSeedList);

            m_tempObjectSeedList.Clear();
            ProfilerShort.End();
        }

        public void GetObjectSeeds(BoundingSphereD sphere, List<MyObjectSeed> list)
        {
            ProfilerShort.Begin("GetObjectSeedsInSphere");
            GenerateObjectSeeds(sphere);

            OverlapAllBoundingSphere(ref sphere, list);
            ProfilerShort.End();
        }

        private Vector3I.RangeIterator GetCellsIterator(BoundingSphereD sphere)
        {
            BoundingBoxD box = new BoundingBoxD(sphere.Center - sphere.Radius, sphere.Center + sphere.Radius);
            return GetCellsIterator(box);
        }

        private Vector3I.RangeIterator GetCellsIterator(BoundingBoxD bbox)
        {
            Vector3I min = Vector3I.Floor(bbox.Min / CELL_SIZE);
            Vector3I max = Vector3I.Floor(bbox.Max / CELL_SIZE);

            return new Vector3I.RangeIterator(ref min, ref max);
        }

        private void GenerateObjectSeeds(BoundingSphereD sphere)
        {
            ProfilerShort.Begin("GenerateObjectSeedsInBox");

            BoundingBoxD box = new BoundingBoxD(sphere.Center - sphere.Radius, sphere.Center + sphere.Radius);

            Vector3I cellId = Vector3I.Floor(box.Min / CELL_SIZE);
            for (var iter = GetCellsIterator(sphere); iter.IsValid(); iter.GetNext(out cellId))
            {
                if (!m_cells.ContainsKey(cellId))
                {
                    var cellBox = new BoundingBoxD(cellId * CELL_SIZE, (cellId + 1) * CELL_SIZE);
                    if (sphere.Contains(cellBox) == ContainmentType.Disjoint)
                    {
                        continue;
                    }
                    var cell = GenerateObjectSeedsCell(ref cellId);
                    if (cell != null)
                    {
                        m_cells.Add(cellId, cell);
                        var cellBBox = cell.BoundingVolume;
                        cell.proxyId = m_cellsTree.AddProxy(ref cellBBox, cell, 0);
                    }
                }
            }
            ProfilerShort.End();
        }

        private MyProceduralCell GenerateObjectSeedsCell(ref Vector3I cellId)
        {
            MyProceduralCell cell = new MyProceduralCell(cellId);
            ProfilerShort.Begin("GenerateObjectSeedsCell");
            IMyModule cellDensityFunction = CalculateCellDensityFunction(ref cellId);
            if (cellDensityFunction == null)
            {
                ProfilerShort.End();
                return null;
            }
            int cellSeed = GetCellSeed(ref cellId);
            using (MyRandom.Instance.PushSeed(cellSeed))
            {
                var random = MyRandom.Instance;

                int index = 0;
                Vector3I subCellId = Vector3I.Zero;
                Vector3I max = new Vector3I(SUBCELLS - 1);
                for (var iter = new Vector3I.RangeIterator(ref Vector3I.Zero, ref max); iter.IsValid(); iter.GetNext(out subCellId))
                {
                    Vector3D position = new Vector3D(random.NextDouble(), random.NextDouble(), random.NextDouble());
                    position += (Vector3D)subCellId / SUBCELL_SIZE;
                    position += cellId;
                    position *= CELL_SIZE;

                    if (!MyEntities.IsInsideWorld(position))
                    {
                        continue;
                    }

                    ProfilerShort.Begin("GetValue");
                    var value = cellDensityFunction.GetValue(position.X, position.Y, position.Z);
                    ProfilerShort.End();

                    if (value < m_objectDensity) // -1..+1
                    {
                        var objectSeed = new MyObjectSeed(cell, position, GetObjectSize(random.NextDouble()));
                        objectSeed.Type = GetSeedType(random.NextDouble());
                        objectSeed.Seed = random.Next();
                        objectSeed.Index = index++;

                        GenerateObject(cell, objectSeed, ref index, random, cellDensityFunction);
                    }
                }
            }

            ProfilerShort.End();
            return cell;
        }

        List<BoundingBoxD> m_tmpClusterBoxes = new List<BoundingBoxD>(OBJECT_MAX_IN_CLUSTER + 1);

        private void GenerateObject(MyProceduralCell cell, MyObjectSeed objectSeed, ref int index, MyRandom random, IMyModule densityFunction)
        {
            cell.AddObject(objectSeed);
            switch (objectSeed.Type)
            {
                case MyObjectSeedType.Asteroid:
                    m_asteroidSeedCount++;
                    break;
                case MyObjectSeedType.AsteroidCluster:
                    objectSeed.Type = MyObjectSeedType.Asteroid;
                    m_asteroidSeedCount++;

                    m_tmpClusterBoxes.Add(objectSeed.BoundingVolume);

                    for (int j = 0; j < OBJECT_MAX_IN_CLUSTER; ++j)
                    {
                        var direction = GetRandomDirection(random);
                        var size = GetClusterObjectSize(random.NextDouble());
                        var distance = MathHelper.Lerp(OBJECT_MIN_DISTANCE_CLUSTER, OBJECT_MAX_DISTANCE_CLUSTER, random.NextDouble());
                        var clusterObjectPosition = objectSeed.BoundingVolume.Center + direction * (size + objectSeed.BoundingVolume.HalfExtents.Length() * 2 + distance);

                        ProfilerShort.Begin("GetValue");
                        var value = densityFunction.GetValue(clusterObjectPosition.X, clusterObjectPosition.Y, clusterObjectPosition.Z);
                        ProfilerShort.End();

                        if (value < OBJECT_DENSITY_CLUSTER) // -1..+1
                        {
                            var clusterObjectSeed = new MyObjectSeed(cell, clusterObjectPosition, size);
                            clusterObjectSeed.Seed = random.Next();
                            clusterObjectSeed.Index = index++;
                            clusterObjectSeed.Type = GetClusterSeedType(random.NextDouble());

                            bool overlaps = false;
                            foreach (var box in m_tmpClusterBoxes)
                            {
                                if (overlaps |= clusterObjectSeed.BoundingVolume.Intersects(box))
                                {
                                    break;
                                }
                            }

                            if (!overlaps)
                            {
                                m_tmpClusterBoxes.Add(clusterObjectSeed.BoundingVolume);
                                GenerateObject(cell, clusterObjectSeed, ref index, random, densityFunction);
                            }
                        }
                    }
                    m_tmpClusterBoxes.Clear();
                    break;
                case MyObjectSeedType.EncounterAlone:
                case MyObjectSeedType.EncounterSingle:
                case MyObjectSeedType.EncounterMulti:
                    m_encounterSeedCount++;
                    break;
                default:
                    throw new InvalidBranchException();
                    break;
            }
        }

        private Vector3D GetRandomDirection(MyRandom random)
        {
            double phi = random.NextDouble() * 2.0 * Math.PI;
            double z = random.NextDouble() * 2.0 - 1.0;
            double root = Math.Sqrt(1.0 - z * z);

            return new Vector3D(root * Math.Cos(phi), root * Math.Sin(phi), z);
        }

        private List<MyVoxelMap> m_tmpVoxelMapsList = new List<MyVoxelMap>();

        public void GenerateObjects(List<MyObjectSeed> objectsList)
        {
            ProfilerShort.Begin("GenerateObjects");
            foreach (var objectSeed in objectsList)
            {
                if (objectSeed.Generated)
                    continue;

                objectSeed.Generated = true;

                using (MyRandom.Instance.PushSeed(GetObjectIdSeed(objectSeed)))
                {
                    switch (objectSeed.Type)
                    {
                        case MyObjectSeedType.Asteroid:
                            ProfilerShort.Begin("Asteroid");

                            var bbox = objectSeed.BoundingVolume;
                            MyGamePruningStructure.GetAllVoxelMapsInBox(ref bbox, m_tmpVoxelMapsList);

                            String storageName = string.Format("Asteroid_{0}_{1}_{2}_{3}_{4}", objectSeed.CellId.X, objectSeed.CellId.Y, objectSeed.CellId.Z, objectSeed.Index, objectSeed.Seed);

                            bool exists = false;
                            foreach (var voxelMap in m_tmpVoxelMapsList)
                            {
                                if (voxelMap.StorageName == storageName)
                                {
                                    exists = true;
                                    break;
                                }
                            }

                            if (!exists)
                            {
                                var provider = MyCompositeShapeProvider.CreateAsteroidShape(objectSeed.Seed, objectSeed.Size, MySession.Static.Settings.VoxelGeneratorVersion);
                                MyStorageBase storage = new MyOctreeStorage(provider, GetAsteroidVoxelSize(objectSeed.Size));
                                var voxelMap = MyWorldGenerator.AddVoxelMap(storageName, storage, objectSeed.BoundingVolume.Center - VRageMath.MathHelper.GetNearestBiggerPowerOfTwo(objectSeed.Size) / 2, GetAsteroidEntityId(objectSeed));

                                voxelMap.Save = false;
                                RangeChangedDelegate OnStorageRangeChanged = null;
                                OnStorageRangeChanged = delegate(Vector3I minVoxelChanged, Vector3I maxVoxelChanged, MyStorageDataTypeFlags changedData)
                                {
                                    voxelMap.Save = true;
                                    storage.RangeChanged -= OnStorageRangeChanged;
                                };
                                storage.RangeChanged += OnStorageRangeChanged;
                                m_asteroidCount++;
                            }
                            m_tmpVoxelMapsList.Clear();
                            ProfilerShort.End();
                            break;
                        case MyObjectSeedType.EncounterAlone:
                        case MyObjectSeedType.EncounterSingle:
                        case MyObjectSeedType.EncounterMulti:
                            ProfilerShort.Begin("Encounter");
                            MyEncounterGenerator.PlaceEncounterToWorld(objectSeed.BoundingVolume, objectSeed.Seed, objectSeed.Type);
                            m_encounterCount++;
                            ProfilerShort.End();
                            break;
                        default:
                            throw new InvalidBranchException();
                            break;
                    }
                }
            }
            ProfilerShort.End();
        }

        private IMyModule CalculateCellDensityFunction(ref Vector3I cell)
        {
            List<IMyModule> densFunctions = new List<IMyModule>();

            foreach (IMyAsteroidFieldDensityFunction func in m_densityFunctions)
                if (func.ExistsInCell(ref cell))
                    densFunctions.Add(func);

            if (densFunctions.Count == 0)
            {
                return null;
            }

            int functionsCount = densFunctions.Count;
            while (functionsCount != 1)
            {
                for (int i = 0; i < functionsCount / 2; ++i)
                    densFunctions[i] = new MyMax(densFunctions[i * 2], densFunctions[i * 2 + 1]);

                if (functionsCount % 2 == 1)
                    densFunctions[functionsCount - 1] = densFunctions[functionsCount / 2];

                functionsCount = functionsCount / 2 + functionsCount % 2;
            }

            return densFunctions[0];
        }

        private MyObjectSeedType GetSeedType(double d)
        {
            d *= m_seedTypeProbabilitySum;
            foreach (var probability in m_seedTypeProbability)
            {
                if (probability.Value >= d)
                {
                    return probability.Key;
                }
                d -= probability.Value;
            }
            return MyObjectSeedType.Asteroid;
        }

        private MyObjectSeedType GetClusterSeedType(double d)
        {
            d *= m_seedClusterTypeProbabilitySum;
            foreach (var probability in m_seedClusterTypeProbability)
            {
                if (probability.Value >= d)
                {
                    return probability.Key;
                }
                d -= probability.Value;
            }
            return MyObjectSeedType.Asteroid;
        }

        private static double GetObjectSize(double noise)
        {
            return OBJECT_SIZE_MIN + noise * noise * (OBJECT_SIZE_MAX - OBJECT_SIZE_MIN); // x^2
        }

        private static double GetClusterObjectSize(double noise)
        {
            return OBJECT_SIZE_MIN_CLUSTER + noise * (OBJECT_SIZE_MAX_CLUSTER - OBJECT_SIZE_MIN_CLUSTER); // x
        }

        private static bool IsInCell(ref BoundingBoxD cellbox, ref BoundingSphereD asteroid)
        {
            return cellbox.Contains(asteroid) != ContainmentType.Disjoint;
        }

        private static Vector3I GetAsteroidVoxelSize(double asteroidRadius)
        {
            int radius = Math.Max(64, (int)Math.Ceiling(asteroidRadius));
            return new Vector3I(radius);
        }

        private const int BIG_PRIME1 = 16785407;
        private const int BIG_PRIME2 = 39916801;
        private const int BIG_PRIME3 = 479001599;

        private const int TWIN_PRIME_MIDDLE1 = 240;
        private const int TWIN_PRIME_MIDDLE2 = 312;
        private const int TWIN_PRIME_MIDDLE3 = 462;

        private int GetCellSeed(ref Vector3I cell)
        {
            unchecked
            {
                return m_seed + cell.X * BIG_PRIME1 + cell.Y * BIG_PRIME2 + cell.Z * BIG_PRIME3;
            }
        }

        private int GetObjectIdSeed(MyObjectSeed objectSeed)
        {
            int hash = objectSeed.CellId.GetHashCode();
            hash = (hash * 397) ^ m_seed;
            hash = (hash * 397) ^ objectSeed.Index;
            hash = (hash * 397) ^ objectSeed.Seed;
            return hash;
        }

        private long GetAsteroidEntityId(MyObjectSeed objectSeed)
        {
            var cellId = objectSeed.CellId;
            long hash = (long)(Math.Abs(cellId.X));
            hash = (hash * 397) ^ (long)(Math.Abs(cellId.Y));
            hash = (hash * 397) ^ (long)(Math.Abs(cellId.Z));
            hash = (hash * 397) ^ (long)(Math.Sign(cellId.X) + TWIN_PRIME_MIDDLE1);
            hash = (hash * 397) ^ (long)(Math.Sign(cellId.Y) + TWIN_PRIME_MIDDLE2);
            hash = (hash * 397) ^ (long)(Math.Sign(cellId.Z) + TWIN_PRIME_MIDDLE3);
            hash = (hash * 397) ^ (long)objectSeed.Index * BIG_PRIME1;

            return hash & 0x00FFFFFFFFFFFFFF | ((long)MyEntityIdentifier.ID_OBJECT_TYPE.ASTEROID << 56);
        }

        override public bool UpdatedBeforeInit()
        {
            return true;
        }
    }
}
