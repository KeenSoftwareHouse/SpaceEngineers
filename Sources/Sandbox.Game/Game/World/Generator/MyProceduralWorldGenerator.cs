using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Library.Utils;
using VRage.Profiler;
using VRageMath;


namespace Sandbox.Game.World.Generator
{
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

        public MyProceduralCell(Vector3I cellId, double cellSize)
        {
            CellId = cellId;
            BoundingVolume = new BoundingBoxD(CellId * cellSize, (CellId + 1) * cellSize);
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
        public MyObjectSeedParams Params = new MyObjectSeedParams();

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

        public object UserData
        {
            get;
            set;
        }

        public MyObjectSeed()
        {
        }

        public MyObjectSeed(MyProceduralCell cell, Vector3D position, double size)
        {
            Cell = cell;
            Size = (float)size;
            BoundingVolume = new BoundingBoxD(position - Size, position + Size);
        }
    }

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

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, 500, typeof(MyObjectBuilder_WorldGenerator))]
    public class MyProceduralWorldGenerator : MySessionComponentBase
    {  
        public bool Enabled { get; private set; }

        public static MyProceduralWorldGenerator Static;

        private int m_seed = 0;
        private double m_objectDensity = -1; // needs to be -1..1

        private List<MyProceduralWorldModule> m_modules = new List<MyProceduralWorldModule>();

        private Dictionary<MyEntity, MyEntityTracker> m_trackedEntities = new Dictionary<MyEntity, MyEntityTracker>();
        private Dictionary<MyEntity, MyEntityTracker> m_toAddTrackedEntities = new Dictionary<MyEntity, MyEntityTracker>();
        HashSet<EmptyArea> m_markedAreas = new HashSet<EmptyArea>();
        HashSet<MyObjectSeedParams> m_existingObjectsSeeds = new HashSet<MyObjectSeedParams>();

        public DictionaryReader<MyEntity, MyEntityTracker> GetTrackedEntities()
        {
            return new DictionaryReader<MyEntity, MyEntityTracker>(m_trackedEntities);
        }

        public void GetAllExistingCells(List<MyProceduralCell> list)
        {
            list.Clear();
            foreach (var module in m_modules)
            {
                module.GetAllCells(list);
            }
        }

        private List<MyProceduralCell> m_tempProceduralCellsList = new List<MyProceduralCell>();

        public void GetAllExisting(List<MyObjectSeed> list)
        {
            list.Clear();
            GetAllExistingCells(m_tempProceduralCellsList);
            foreach (var cell in m_tempProceduralCellsList)
            {
                cell.GetAll(list, false);
            }
            m_tempProceduralCellsList.Clear();
        }

        public void GetAllInSphere(BoundingSphereD area, List<MyObjectSeed> list)
        {
            foreach (var module in m_modules)
            {
                module.GetObjectSeeds(area, list, false);
                module.MarkCellsDirty(area, null, false);
            }
        }

        public void OverlapAllPlanetSeedsInSphere(BoundingSphereD area, List<MyObjectSeed> list)
        {
            if (m_planetsModule == null)
            {
                return;
            }

            m_planetsModule.GetObjectSeeds(area, list, false);
            m_planetsModule.MarkCellsDirty(area, null, false);
        }

        public void OverlapAllAsteroidSeedsInSphere(BoundingSphereD area, List<MyObjectSeed> list)
        {
            if (m_asteroidsModule == null)
            {
                return;
            }

            m_asteroidsModule.GetObjectSeeds(area, list, false);
            m_asteroidsModule.MarkCellsDirty(area, null, false);
        }

        private List<MyObjectSeed> m_tempObjectSeedList = new List<MyObjectSeed>();
        private MyProceduralPlanetCellGenerator m_planetsModule;
        private MyProceduralAsteroidCellGenerator m_asteroidsModule;

        public override void LoadData()
        {
            //if (true) return;

            Static = this;
            if (!MyFakes.ENABLE_ASTEROID_FIELDS)
                return;

            var settings = MySession.Static.Settings;
            if (settings.ProceduralDensity == 0f)
            {
                Enabled = false;
                MySandboxGame.Log.WriteLine("Skip Procedural World Generator");
                return;
            }

            m_seed = settings.ProceduralSeed;
            m_objectDensity = MathHelper.Clamp(settings.ProceduralDensity * 2 - 1, -1, 1); // must be -1..1


            MySandboxGame.Log.WriteLine(string.Format("Loading Procedural World Generator: Seed = '{0}' = {1}, Density = {2}", settings.ProceduralSeed, m_seed, settings.ProceduralDensity));

            using (MyRandom.Instance.PushSeed(m_seed))
            {
                m_asteroidsModule = new MyProceduralAsteroidCellGenerator(m_seed, m_objectDensity);
                m_modules.Add(m_asteroidsModule);
            }

            Enabled = true;
        }

        protected override void UnloadData()
        {
            Enabled = false;
            if (!MyFakes.ENABLE_ASTEROID_FIELDS)
                return;
            MySandboxGame.Log.WriteLine("Unloading Procedural World Generator");

            m_modules.Clear();

            m_trackedEntities.Clear();

            Debug.Assert(m_tempObjectSeedList.Count == 0, "temp list is not empty!");
            m_tempObjectSeedList.Clear();

            Static = null;
        }

        public override void UpdateBeforeSimulation()
        {
            if (Enabled)
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

                ProfilerShort.BeginNextBlock("Update tracked entities");
                foreach (var tracker in m_trackedEntities.Values)
                {
                    if (tracker.ShouldGenerate())
                    {
                        var oldBoundingVolume = tracker.BoundingVolume;
                        tracker.UpdateLastPosition();

                        foreach (var module in m_modules)
                        {
                            ProfilerShort.Begin("GenerateObjectsInSphere");
                            module.GetObjectSeeds(tracker.BoundingVolume, m_tempObjectSeedList);
                            module.GenerateObjects(m_tempObjectSeedList, m_existingObjectsSeeds);
                            m_tempObjectSeedList.Clear();
                            ProfilerShort.End();

                            module.MarkCellsDirty(oldBoundingVolume, tracker.BoundingVolume);
                        }
                    }
                }

                ProfilerShort.BeginNextBlock("Process dirty cells");
                foreach (var module in m_modules)
                {
                    module.ProcessDirtyCells(m_trackedEntities);
                }
                ProfilerShort.End();
            }

            if (!MySandboxGame.AreClipmapsReady && MySession.Static.VoxelMaps.Instances.Count == 0)
            {
                // Render will not send any message if it has no clipmaps, so we have to specify there is nothing to wait for.
                MySandboxGame.SignalClipmapsReady();
            }
        }

        public void TrackEntity(MyEntity entity)
        {
            if (!Enabled)
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
                    foreach (var module in m_modules)
                    {
                        module.MarkCellsDirty(tracker.BoundingVolume);
                    }
                };
            }
        }

        override public bool UpdatedBeforeInit()
        {
            return true;
        }

        public static Vector3D GetRandomDirection(MyRandom random)
        {
            double phi = random.NextDouble() * 2.0 * Math.PI;
            double z = random.NextDouble() * 2.0 - 1.0;
            double root = Math.Sqrt(1.0 - z * z);

            return new Vector3D(root * Math.Cos(phi), root * Math.Sin(phi), z);
        }

        public void MarkEmptyArea(Vector3D pos, float radius)
        {
            MarkModules(pos, radius);
            m_markedAreas.Add(new EmptyArea(){Position = pos,Radius = radius});
        }

        private void MarkModules(Vector3D pos, float radius)
        {
            MySphereDensityFunction func = new MySphereDensityFunction(pos, radius * MyProceduralPlanetCellGenerator.GRAVITY_SIZE_MULTIPLIER + MyProceduralPlanetCellGenerator.FALLOFF, MyProceduralPlanetCellGenerator.FALLOFF);
            foreach (var module in m_modules)
            {
                module.AddDensityFunctionRemoved(func);
            }
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            MyObjectBuilder_WorldGenerator builder = (MyObjectBuilder_WorldGenerator)sessionComponent;
            if (Sync.IsServer == false)
            {
                m_markedAreas = builder.MarkedAreas;
            }
            m_existingObjectsSeeds = builder.ExistingObjectsSeeds;
            if(m_markedAreas == null)
            {
                m_markedAreas = new HashSet<EmptyArea>();
            }

            foreach(var area in m_markedAreas)
            {
                MarkModules(area.Position, area.Radius);
            }
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            MyObjectBuilder_WorldGenerator builder = (MyObjectBuilder_WorldGenerator)base.GetObjectBuilder();
            builder.MarkedAreas = m_markedAreas;
            builder.ExistingObjectsSeeds = m_existingObjectsSeeds;
            return builder;
        }

    }
}
