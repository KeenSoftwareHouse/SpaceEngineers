using ParallelTasks;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Planet;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities
{
    public class PlanetsNotEnabledException : System.Exception
    {
    }

    public struct MyPlanetInitArguments
    {
        public string StorageName;
        public Sandbox.Engine.Voxels.IMyStorage Storage;
        public Vector3D PositionMinCorner;
        public float Radius;
        public float AtmosphereRadius;
        public float MaxRadius;
        public float MinRadius;
        public bool HasAtmosphere;
        public Vector3 AtmosphereWavelengths;
        public float GravityFalloff;
        public bool MarkAreaEmpty;
        public MyAtmosphereSettings AtmosphereSettings;
        public float SurfaceGravity;
        public bool AddGps;
        public bool SpherizeWithDistance;
        public MyPlanetGeneratorDefinition Generator;
        public bool UserCreated;
    }

    [MyEntityType(typeof(MyObjectBuilder_Planet))]
    public partial class MyPlanet : MyVoxelBase, IMyGravityProvider, IMyOxygenProvider
    {
        const int PHYSICS_SECTOR_SIZE_METERS = 1024;

        private const double INTRASECTOR_OBJECT_CLUSTER_SIZE = PHYSICS_SECTOR_SIZE_METERS/2;

        const double GRAVITY_LIMIT_STRENGTH = 0.05;

        public static bool RUN_SECTORS = true;

        List<BoundingBoxD> m_clustersIntersection = new List<BoundingBoxD>();

        #region Shape properties

        public float AtmosphereAltitude
        {
            get;
            private set;
        }

        #endregion

        #region Oxygen & Atmosphere

        bool IMyOxygenProvider.IsPositionInRange(Vector3D worldPoint)
        {
            if (!Generator.HasAtmosphere || !Generator.Atmosphere.Breathable) return false;

            return (WorldMatrix.Translation - worldPoint).Length() < AtmosphereAltitude + AverageRadius;
        }

        public float GetOxygenForPosition(Vector3D worldPoint)
        {
            if (Generator.Atmosphere.Breathable)
                return GetAirDensity(worldPoint) * Generator.Atmosphere.OxygenDensity;
            return 0f;
        }

        public float GetAirDensity(Vector3D worldPosition)
        {
            if (Generator.HasAtmosphere)
            {
                double distance = (worldPosition - WorldMatrix.Translation).Length();
                var rate = MathHelper.Clamp(1 - (distance - AverageRadius) / (AtmosphereAltitude), 0, 1);
                return (float)rate * Generator.Atmosphere.Density;
            }

            return 0f;
        }

        #endregion

        #region Gravity
        // THe gravity limit gets calculated from the GRAVITY_LIMIT_STRENGTH so that the gravity stops where it is equal to G_L_S
        private float m_gravityLimit;
        private float m_gravityLimitSq;

        public float GravityLimit
        {
            get { return m_gravityLimit; }
            private set
            {
                m_gravityLimitSq = value * value;
                m_gravityLimit = value;
            }
        }

        public float GravityLimitSq
        {
            get { return m_gravityLimitSq; }
            private set
            {
                m_gravityLimitSq = value;
                m_gravityLimit = (float)Math.Sqrt(value);
            }
        }


        public bool IsWorking
        {
            get { return true; }
        }

        bool IMyGravityProvider.IsPositionInRange(Vector3D worldPoint)
        {
            return IsPositionInGravityWell(worldPoint);
        }

        public Vector3 GetWorldGravity(Vector3D worldPoint)
        {
            Vector3 direction = GetWorldGravityNormalized(ref worldPoint);
            var gravityMultiplier = GetGravityMultiplier(worldPoint);

            return direction * MyGravityProviderSystem.G * gravityMultiplier;
        }

        public Vector3 GetWorldGravityNormalized(ref Vector3D worldPoint)
        {
            Vector3 direction = WorldMatrix.Translation - worldPoint;
            direction.Normalize();
            return direction;
        }

        public float GetGravityMultiplier(Vector3D worldPoint)
        {
            double distanceToCenter = (WorldMatrix.Translation - worldPoint).Length();
            // The Gravity limit should be calculated so that the gravity cuts-off at GRAVITY_LIMIT_STRENGTH
            if (distanceToCenter > m_gravityLimit) return 0.0f;

            float attenuation = 1.0f;

            if (distanceToCenter > m_planetInitValues.MaxRadius)
            {
                attenuation = (float)Math.Pow(distanceToCenter / m_planetInitValues.MaxRadius, -m_planetInitValues.GravityFalloff);
            }
            else if (distanceToCenter < m_planetInitValues.MinRadius)
            {
                attenuation = (float)(distanceToCenter / m_planetInitValues.MinRadius);
                if (attenuation < 0.01f)
                    attenuation = 0.01f;
            }

            float planetScale = m_planetInitValues.SurfaceGravity;
            return attenuation * planetScale;
        }

        public bool IsPositionInGravityWell(Vector3D worldPoint)
        {
            return (WorldMatrix.Translation - worldPoint).LengthSquared() <= m_gravityLimitSq;
        }

        public Vector3 GetWorldGravityGrid(Vector3D worldPoint)
        {
            return GetWorldGravity(worldPoint);
        }

        public bool IsPositionInRangeGrid(Vector3D worldPoint)
        {
            return ((IMyGravityProvider)this).IsPositionInRange(worldPoint);
        }

        #endregion

        public MyPlanetStorageProvider Provider
        {
            get;
            private set;
        }

        Dictionary<Vector3I, MyVoxelPhysics> m_physicsShapes;

        HashSet<Vector3I> m_sectorsPhysicsToRemove = new HashSet<Vector3I>();
        Vector3I m_numCells;

        bool m_canSpawnSectors = true;

        public override MyVoxelBase RootVoxel { get { return this; } }

        public MyPlanetGeneratorDefinition Generator { get; private set; }

        public new Sandbox.Engine.Voxels.IMyStorage Storage
        {
            get { return m_storage; }
            set
            {
                if (!(value is MyPlanetStorageProvider))
                {
                    Debug.Fail("The planet is coupled with it's storage and will not work with other storages.");
                    return;
                }

                if (m_storage != null)
                {
                    m_storage.RangeChanged -= storage_RangeChangedPlanet;
                }

                m_storage = value;
                m_storage.RangeChanged += storage_RangeChangedPlanet;
                m_storageMax = m_storage.Size;

                m_storage.Reset();
            }
        }

        public override Vector3D PositionLeftBottomCorner
        {
            get
            {
                return base.PositionLeftBottomCorner;
            }
            set
            {
                if (value != base.PositionLeftBottomCorner)
                {
                    base.PositionLeftBottomCorner = value;

                    if (m_physicsShapes != null)
                    {
                        foreach (var physicsShape in m_physicsShapes)
                        {
                            if (physicsShape.Value != null)
                            {
                                Vector3D pos = PositionLeftBottomCorner +
                                               physicsShape.Key*PHYSICS_SECTOR_SIZE_METERS*
                                               MyVoxelConstants.VOXEL_SIZE_IN_METRES;
                                physicsShape.Value.PositionLeftBottomCorner = pos;
                                physicsShape.Value.PositionComp.SetPosition(pos + physicsShape.Value.Size*0.5f);
                            }
                        }
                    }
                }
            }
        }

        MyPlanetInitArguments m_planetInitValues;

        public MyPlanetInitArguments GetInitArguments
        {
            get { return m_planetInitValues; }
        }

        public Vector3 AtmosphereWavelengths
        {
            get
            {
                return m_planetInitValues.AtmosphereWavelengths;
            }
        }

        public MyAtmosphereSettings AtmosphereSettings
        {
            get
            {
                return m_planetInitValues.AtmosphereSettings;
            }
            set
            {
                m_planetInitValues.AtmosphereSettings = value;
                (Render as MyRenderComponentPlanet).UpdateAtmosphereSettings(value);
            }
        }

        public float MinimumRadius
        {
            get
            {
                return Provider.Shape.InnerRadius;
            }
        }

        public float AverageRadius
        {
            get
            {
                return Provider.Shape.Radius;
            }
        }

        public float MaximumRadius
        {
            get
            {
                return Provider.Shape.OuterRadius;
            }
        }

        public float AtmosphereRadius
        {
            get
            {
                return m_planetInitValues.AtmosphereRadius;
            }
        }

        public bool HasAtmosphere
        {
            get
            {
                return m_planetInitValues.HasAtmosphere;
            }
        }

        public bool SpherizeWithDistance
        {
            get
            {
                return m_planetInitValues.SpherizeWithDistance;
            }
        }

        bool CanSpawnFlora
        {
            get;
            set;
        }

        #region Load/Unload

        public MyPlanet()
        {
            (PositionComp as MyPositionComponent).WorldPositionChanged = WorldPositionChanged;

            Render = new MyRenderComponentPlanet();
            AddDebugRenderComponent(new MyDebugRenderComponentPlanet(this));

            Render.DrawOutsideViewDistance = true;

            m_parallelWorkDelegate = ParallelWorkCallback;
            m_serialWorkDelegate = SerialWorkCallback;
        }

        public override void Init(MyObjectBuilder_EntityBase builder)
        {
            Init(builder, null);
        }

        public override void Init(MyObjectBuilder_EntityBase builder, Sandbox.Engine.Voxels.IMyStorage storage)
        {
            ProfilerShort.Begin("MyPlanet::Init()");
            if (MyFakes.ENABLE_PLANETS == false)
            {
                throw new PlanetsNotEnabledException();
            }

            ProfilerShort.Begin("MyVoxelBase Init");

            SyncFlag = true;

            base.Init(builder);
            base.Init(null, null, null, null, null);

            ProfilerShort.BeginNextBlock("Load Saved Data");

            var ob = (MyObjectBuilder_Planet)builder;
            if (ob == null)
            {
                return;
            }

            if (ob.MutableStorage)
            {
                StorageName = ob.StorageName;
            }
            else
            {
                StorageName = string.Format("{0}", ob.StorageName);
            }
            if (ob.SavedEnviromentSectors != null)
            {
                foreach (var sect in ob.SavedEnviromentSectors)
                {
                    MyPlanetSectorId id;
                    id.Position = sect.IdPos;
                    id.Direction = sect.IdDir;
                    SavedSectors[id] = new List<int>(sect.RemovedItems);
                    SavedSectors[id].Sort();
                }
            }

            m_planetInitValues.StorageName = StorageName;
            m_planetInitValues.PositionMinCorner = ob.PositionAndOrientation.Value.Position;
            m_planetInitValues.HasAtmosphere = ob.HasAtmosphere;
            m_planetInitValues.AtmosphereRadius = ob.AtmosphereRadius;
            m_planetInitValues.AtmosphereWavelengths = ob.AtmosphereWavelengths;
            m_planetInitValues.GravityFalloff = ob.GravityFalloff;
            m_planetInitValues.MarkAreaEmpty = ob.MarkAreaEmpty;
            m_planetInitValues.SurfaceGravity = ob.SurfaceGravity;
            m_planetInitValues.AddGps = ob.ShowGPS;
            m_planetInitValues.SpherizeWithDistance = ob.SpherizeWithDistance;
            m_planetInitValues.Generator = ob.PlanetGenerator == "" ? null : MyDefinitionManager.Static.GetDefinition<MyPlanetGeneratorDefinition>(MyStringHash.GetOrCompute(ob.PlanetGenerator));
            if (m_planetInitValues.Generator == null)
            {
                string message = string.Format("No definition found for planet generator {0}.", ob.PlanetGenerator);
                MyLog.Default.WriteLine(message);
                throw new Exception(message);
            }
            
            m_planetInitValues.AtmosphereSettings = m_planetInitValues.Generator.AtmosphereSettings.HasValue ? m_planetInitValues.Generator.AtmosphereSettings.Value : MyAtmosphereSettings.Defaults();
            m_planetInitValues.UserCreated = false;

            ProfilerShort.BeginNextBlock("Load Storage");
            if (storage != null)
            {
                m_planetInitValues.Storage = storage;
            }
            else
            {
                m_planetInitValues.Storage = MyStorageBase.Load(ob.StorageName);
            }

            ProfilerShort.BeginNextBlock("Init Internal");
            Init(m_planetInitValues);
            ProfilerShort.End();

            ProfilerShort.End();
        }

        public void Init(MyPlanetInitArguments arguments)
        {
            if (MyFakes.ENABLE_PLANETS == false)
            {
                throw new PlanetsNotEnabledException();
            }

            m_planetInitValues = arguments;

            // Parameteres from storage
            Provider = m_planetInitValues.Storage.DataProvider as MyPlanetStorageProvider;

            m_planetInitValues.Radius = Provider.Radius;
            m_planetInitValues.MaxRadius = Provider.Shape.OuterRadius;
            m_planetInitValues.MinRadius = Provider.Shape.InnerRadius;

            Generator = arguments.Generator;

            AtmosphereAltitude = Provider.Shape.MaxHillHeight * Generator.Atmosphere.LimitAltitude;

            // Calculate the distance from the planet center where the gravity will be equal to GRAVITY_LIMIT_STRENGTH
            {
                double s = (double)m_planetInitValues.SurfaceGravity;
                double radius = m_planetInitValues.MaxRadius;
                double invFalloff = 1.0 / (double)m_planetInitValues.GravityFalloff;
                GravityLimit = (float)(radius * Math.Pow(s / GRAVITY_LIMIT_STRENGTH, invFalloff));
            }

            base.Init(m_planetInitValues.StorageName, m_planetInitValues.Storage, m_planetInitValues.PositionMinCorner);

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            m_storage.RangeChanged += storage_RangeChangedPlanet;

            if (m_planetInitValues.MarkAreaEmpty)
            {
                MyProceduralWorldGenerator.Static.MarkEmptyArea(PositionComp.GetPosition(), m_planetInitValues.MaxRadius);
            }

            if (Physics != null)
            {
                Physics.Enabled = false;
                Physics.Close();
                Physics = null;
            }


            /* Prepare sectors */
            Vector3I storageSize = m_planetInitValues.Storage.Size;
            m_numCells = new Vector3I(storageSize.X / PHYSICS_SECTOR_SIZE_METERS, storageSize.Y / PHYSICS_SECTOR_SIZE_METERS, storageSize.Z / PHYSICS_SECTOR_SIZE_METERS);
            m_numCells -= 1;
            m_numCells = Vector3I.Max(Vector3I.Zero, m_numCells);

            CanSpawnFlora = Generator.MaterialEnvironmentMappings.Count != 0 && MySession.Static.Settings.EnableFlora && MyFakes.ENABLE_ENVIRONMENT_ITEMS && Storage.DataProvider is MyPlanetStorageProvider;

            StorageName = m_planetInitValues.StorageName;
            m_storageMax = m_planetInitValues.Storage.Size;

            // Init sector metadata
            PrepareSectors();

            if(m_planetInitValues.UserCreated)
            {
                ContentChanged = true;
            }
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            MyObjectBuilder_Planet planetBuilder = (MyObjectBuilder_Planet)base.GetObjectBuilder(copy);

            var len = SavedSectors.Count;

            var sectors = new MyObjectBuilder_Planet.SavedSector[len];

            int i = 0;
            foreach (var sect in SavedSectors)
            {
                sectors[i++] = new MyObjectBuilder_Planet.SavedSector()
                {
                    IdPos = sect.Key.Position,
                    IdDir = sect.Key.Direction,
                    RemovedItems = new HashSet<int>(sect.Value)
                };
            }

            planetBuilder.Radius = m_planetInitValues.Radius;
            planetBuilder.HasAtmosphere = m_planetInitValues.HasAtmosphere;
            planetBuilder.AtmosphereRadius = m_planetInitValues.AtmosphereRadius;
            planetBuilder.MinimumSurfaceRadius = m_planetInitValues.MinRadius;
            planetBuilder.MaximumHillRadius = m_planetInitValues.MaxRadius;
            planetBuilder.AtmosphereWavelengths = m_planetInitValues.AtmosphereWavelengths;
            planetBuilder.SavedEnviromentSectors = sectors;
            planetBuilder.GravityFalloff = m_planetInitValues.GravityFalloff;
            planetBuilder.MarkAreaEmpty = m_planetInitValues.MarkAreaEmpty;
            planetBuilder.AtmosphereSettings = m_planetInitValues.AtmosphereSettings;
            planetBuilder.SurfaceGravity = m_planetInitValues.SurfaceGravity;
            planetBuilder.ShowGPS = m_planetInitValues.AddGps;
            planetBuilder.SpherizeWithDistance = m_planetInitValues.SpherizeWithDistance;
            planetBuilder.PlanetGenerator = Generator != null ? Generator.Id.SubtypeId.ToString() : null;

            return planetBuilder;
        }

        protected override void Closing()
        {
            base.Closing();

            if (m_physicsShapes != null)
            {
                foreach (var voxelMap in m_physicsShapes)
                {
                    if (voxelMap.Value != null)
                        voxelMap.Value.Close();
                }
            }
        }

        protected override void BeforeDelete()
        {
            base.BeforeDelete();

            var planet = Render as MyRenderComponentPlanet;
            if (planet != null)
                planet.CancelAllRequests();

            if (m_planetEnvironmentSectors != null)
            {
                foreach (var sector in m_planetEnvironmentSectors)
                {
                    sector.Value.CloseSector();
                    m_planetSectorsPool.Deallocate(sector.Value);
                }
            }
            if (m_physicsShapes != null)
            {
                foreach (var voxelMap in m_physicsShapes)
                {
                    if (voxelMap.Value != null)
                    {
                        MySession.Static.VoxelMaps.RemoveVoxelMap(voxelMap.Value);
                        voxelMap.Value.RemoveFromGamePruningStructure();
                    }
                }
            }

            MySession.Static.VoxelMaps.RemoveVoxelMap(this);

            m_storage.Close();

            m_storage = null;
            Provider = null;
        }

        #endregion

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            MyPlanets.Register(this);

            MyGravityProviderSystem.AddPlanet(this);
            MyOxygenProviderSystem.AddOxygenGenerator(this);
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);

            MyPlanets.UnRegister(this);
            
            MyGravityProviderSystem.RemovePlanet(this);
            MyOxygenProviderSystem.RemoveOxygenGenerator(this);
        }

        private void storage_RangeChangedPlanet(Vector3I minChanged, Vector3I maxChanged, MyStorageDataTypeFlags dataChanged)
        {
            ProfilerShort.Begin("MyVoxelMap::storage_RangeChanged");
            Vector3I minSector = minChanged / PHYSICS_SECTOR_SIZE_METERS;
            Vector3I maxSector = maxChanged / PHYSICS_SECTOR_SIZE_METERS;

            MyVoxelPhysics voxelMap;

            if (m_physicsShapes != null)
            {
                for (var it = new Vector3I.RangeIterator(ref minSector, ref maxSector);
                    it.IsValid(); it.MoveNext())
                {
                    if (m_physicsShapes.TryGetValue(it.Current, out voxelMap))
                    {
                        if (voxelMap != null)
                            voxelMap.OnStorageChanged(minChanged, maxChanged, dataChanged);
                    }
                }
            }

            if (Render is MyRenderComponentVoxelMap)
            {
                (Render as MyRenderComponentVoxelMap).InvalidateRange(minChanged, maxChanged);
            }

            OnRangeChanged(minChanged, maxChanged, dataChanged);
            ProfilerShort.End();
        }

        private MyVoxelPhysics CreateVoxelPhysics(ref Vector3I increment, ref Vector3I.RangeIterator it)
        {
            if (m_physicsShapes == null)
            {
                m_physicsShapes = new Dictionary<Vector3I, MyVoxelPhysics>();
            }

            MyVoxelPhysics voxelMap = null;
            if (!m_physicsShapes.TryGetValue(it.Current, out voxelMap))
            {
                Vector3I storageMin = it.Current * increment;
                Vector3I storageMax = storageMin + increment;

                BoundingBox check = new BoundingBox(storageMin, storageMax);

                if (Storage.Intersect(ref check, false) == ContainmentType.Intersects)
                {
                    voxelMap = new MyVoxelPhysics();

                    voxelMap.Init(m_storage,
                        this.PositionLeftBottomCorner + storageMin*MyVoxelConstants.VOXEL_SIZE_IN_METRES, storageMin,
                        storageMax, this);
                    voxelMap.Save = false;
                    MyEntities.Add(voxelMap);
                }

                m_physicsShapes.Add(it.Current, voxelMap);
            }
            return voxelMap;
        }

        static List<MyEntity> m_entities = new List<MyEntity>();

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            UpdateFloraAndPhysics(true);

            if (m_planetInitValues.AddGps)
            {
                MyGps newGps = new MyGps()
                {
                    Name = (m_planetInitValues.Radius * 2.0f).ToString(),
                    Coords = PositionComp.GetPosition(),
                    ShowOnHud = true
                };
                newGps.UpdateHash();
                MySession.Static.Gpss.SendAddGps(MySession.Static.LocalPlayerId, ref newGps);
            }
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            UpdateFloraAndPhysics();
            m_entities.Clear();
        }

        public override void BeforePaste()
        {
        }

        public override void AfterPaste()
        {

        }

        private void UpdateFloraAndPhysics(bool serial = false)
        {
            BoundingBoxD box = this.PositionComp.WorldAABB;
            box.Min -= PHYSICS_SECTOR_SIZE_METERS;
            box.Max += PHYSICS_SECTOR_SIZE_METERS;
            m_entities.Clear();

            UpdatePlanetPhysics(ref box);

            UpdateSectors(serial, ref box);
        }

        #region Planet Physics

        private void UpdatePlanetPhysics(ref BoundingBoxD box)
        {
            ProfilerShort.Begin("PlanetPhysics");
            Vector3I increment = m_storage.Size / (m_numCells + 1);
            MyGamePruningStructure.GetAproximateDynamicClustersForSize(ref box, INTRASECTOR_OBJECT_CLUSTER_SIZE, m_clustersIntersection);
            foreach (var res in m_clustersIntersection)
            {
                var shapeBox = res;
                res.Inflate(MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES);
                GeneratePhysicalShapeForBox(ref increment, ref shapeBox);
            }
            m_clustersIntersection.Clear();
            ProfilerShort.End();
        }

        private void GeneratePhysicalShapeForBox(ref Vector3I increment, ref BoundingBoxD shapeBox)
        {
            if (!shapeBox.Intersects(PositionComp.WorldAABB))
                return;

            Vector3I minCorner, maxCorner;
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(PositionLeftBottomCorner, ref shapeBox.Min, out minCorner);
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(PositionLeftBottomCorner, ref shapeBox.Max, out maxCorner);

            minCorner /= PHYSICS_SECTOR_SIZE_METERS;
            maxCorner /= PHYSICS_SECTOR_SIZE_METERS;

            for (var it = new Vector3I.RangeIterator(ref minCorner, ref maxCorner);
                it.IsValid(); it.MoveNext())
            {
                ProfilerShort.Begin("Myplanet::create physics shape");
                CreateVoxelPhysics(ref increment, ref it);
                ProfilerShort.End();
            }
        }

        private Vector3 ComputePredictionOffset(IMyEntity entity)
        {
            if (entity.Physics == null)
            {
                return Vector3.Zero;
            }
            // since we scan every 10 frames + some speedup compensation
            return entity.Physics.LinearVelocity * 0.166f * 2f;
        }

        public override void UpdateAfterSimulation100()
        {
            //Debug.Assert(MyExternalReplicable.FindByObject(this) != null, "Planet replicable not found, but it should be there");
            base.UpdateAfterSimulation100();

            if (m_physicsShapes != null)
            {
                ProfilerShort.Begin("Study shapes to remove");
                foreach (var physicsShape in m_physicsShapes)
                {
                    BoundingBoxD box;
                    if (physicsShape.Value != null)
                    {
                        box = physicsShape.Value.PositionComp.WorldAABB;
                        box.Min -= box.HalfExtents;
                        box.Max += box.HalfExtents;
                    }
                    else
                    {
                        Vector3 min = (Vector3)physicsShape.Key * PHYSICS_SECTOR_SIZE_METERS + PositionLeftBottomCorner;
                        box = new BoundingBoxD(min, min + PHYSICS_SECTOR_SIZE_METERS);
                    }

                    m_entities.Clear();

                    MyGamePruningStructure.GetTopMostEntitiesInBox(ref box, m_entities);

                    bool keep = false;
                    foreach (var entity in m_entities)
                    {
                        if (entity.Physics != null && !entity.Physics.IsStatic)
                        {
                            keep = true;
                        }
                    }

                    if(!keep)
                        m_sectorsPhysicsToRemove.Add(physicsShape.Key);
                }

                foreach (var shapeToRemove in m_sectorsPhysicsToRemove)
                {
                    MyVoxelPhysics physics;
                    if (m_physicsShapes.TryGetValue(shapeToRemove, out physics))
                    {
                        if (physics != null)
                            physics.Close();
                    }
                    m_physicsShapes.Remove(shapeToRemove);
                }
                m_sectorsPhysicsToRemove.Clear();
                ProfilerShort.End();
            }
        }
        #endregion

        override public MyClipmapScaleEnum ScaleGroup
        {
            get
            {
                return MyClipmapScaleEnum.Massive;
            }
        }

        public void DebugDrawEnviromentSectors()
        {
            if (m_planetEnvironmentSectors == null)
            {
                return;
            }

            /*Vector3 position = MySector.MainCamera.Position - WorldMatrix.Translation;
            MyPlanetSectorId id;
            GetSectorIdAt(position, out id);

            MyPlanetEnvironmentSector userSector;
            m_planetEnvironmentSectors.TryGetValue(id, out userSector);*/

            foreach (var sector in m_planetEnvironmentSectors)
            {
                sector.Value.DebugDraw();
            }
        }

        /// <param name="resumeSearch">Don't modify initial search position</param>
        /// <returns>True if it a safe position is found</returns>
        public bool CorrectSpawnLocation2(ref Vector3D position, double radius, bool resumeSearch = false)
        {
            // Generate up vector according to the gravity of the planet.
            Vector3D upVector = -GetWorldGravityNormalized(ref position);

            Vector3D offset = new Vector3D(radius, radius, radius);
            ContainmentType cType;
            Vector3D localPos;
            BoundingBox testBBox;
            Vector3D fixedPosition;
            if (resumeSearch)
            {
                cType = ContainmentType.Intersects;
                fixedPosition = position;
            }
            else
            {
                // Calculate if the position is not inside of the planet:
                VRage.Voxels.MyVoxelCoordSystems.WorldPositionToLocalPosition(PositionLeftBottomCorner, ref position, out localPos);

                // Setup safe bounding box for the drone.
                testBBox = new BoundingBox(localPos - offset, localPos + offset);

                cType = Storage.Intersect(ref testBBox);
                if (cType == ContainmentType.Disjoint)
                    return true;

                fixedPosition = GetClosestSurfacePointGlobal(ref position);
            }

            int i = 0;
            while (i < 10)
            {
                // Spawn it above the ground in the direction of up vector
                fixedPosition += upVector * radius;

                VRage.Voxels.MyVoxelCoordSystems.WorldPositionToLocalPosition(PositionLeftBottomCorner, ref fixedPosition, out localPos);
                testBBox = new BoundingBox(localPos - offset, localPos + offset);
                cType = Storage.Intersect(ref testBBox);

                if (cType == ContainmentType.Disjoint)
                {
                    position = fixedPosition;
                    return true;
                }

                i++;
            }

            return false;
        }

        // NOTE: This method has a problem: "closestPlanetPos" is always a point on the surface
        // of the planet, so "position" is never more than up*radius far away than the surface.
        // Also we don't really know if the given position was really fixed or not cause the
        // modified position may still be colliding. CorrectSpawnLocation2() addresses
        // all these problems
        public void CorrectSpawnLocation(ref Vector3D position, double radius)
        {
            // Generate up vector according to the gravity of the planet.
            Vector3D upVector = -GetWorldGravityNormalized(ref position);

            // Calculate if the position is not inside of the planet:
            Vector3D localPos;
            VRage.Voxels.MyVoxelCoordSystems.WorldPositionToLocalPosition(PositionLeftBottomCorner, ref position, out localPos);

            // Setup safe bounding box for the drone.
            Vector3D offset = new Vector3D(radius, radius, radius);
            BoundingBox testBBox = new BoundingBox(localPos - offset, localPos + offset);

            ContainmentType cType = Storage.Intersect(ref testBBox);
            int i = 0;
            while (i < 10 && (cType == ContainmentType.Intersects || cType == ContainmentType.Contains))
            {
                Vector3D closestPlanetPos = GetClosestSurfacePointGlobal(ref position);
                // Spawn it above the ground in the direction of up vector + 50% of spawn radius (just in case)
                position = closestPlanetPos + upVector * radius;

                VRage.Voxels.MyVoxelCoordSystems.WorldPositionToLocalPosition(PositionLeftBottomCorner, ref position, out localPos);
                testBBox = new BoundingBox(localPos - offset, localPos + offset);
                cType = Storage.Intersect(ref testBBox);

                i++;
            }
        }

        public Vector3D GetClosestSurfacePointGlobal(ref Vector3D globalPos)
        {
            Vector3 localPosition;

            localPosition = globalPos - WorldMatrix.Translation;

            return GetClosestSurfacePointLocal(ref localPosition) + WorldMatrix.Translation;
        }

        public Vector3D GetClosestSurfacePointLocal(ref Vector3 localPos)
        {
            Vector3 localSurface;

            Provider.Shape.ProjectToSurface(localPos, out localSurface);

            return localSurface;
        }

        public void OnEnviromentSectorItemRemoved(ref MyPlanetSectorId id)
        {

        }

        override public void DebugDrawPhysics()
        {
            if (m_physicsShapes != null)
            {
                foreach (var shape in m_physicsShapes)
                {
                    Vector3 min = (Vector3)shape.Key * PHYSICS_SECTOR_SIZE_METERS + PositionLeftBottomCorner;
                    var box = new BoundingBoxD(min, min + PHYSICS_SECTOR_SIZE_METERS);

                    if (shape.Value != null)
                    {
                        shape.Value.Physics.DebugDraw();
                        MyRenderProxy.DebugDrawAABB(box, Color.Cyan, 1.0f, 1.0f, true);
                    }
                    else
                    {
                        MyRenderProxy.DebugDrawAABB(box, Color.DarkGreen, 1.0f, 1.0f, true);
                    }
                }
            }
        }

        public override int GetOrePriority()
        {
            // Voxel physics are also drilled every time so this saves us from doing the removal twice.
            return MyVoxelConstants.PRIORITY_IGNORE_EXTRACTION;
        }

        public int GetInstanceHash()
        {
            return m_planetInitValues.StorageName.GetHashCode() * 775 + m_sectorSize.GetHashCode();
        }

        public void PrefetchShapeOnRay(ref LineD ray)
        {
            Vector3I minCorner, maxCorner;
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(PositionLeftBottomCorner, ref ray.From, out minCorner);
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(PositionLeftBottomCorner, ref ray.To, out maxCorner);

            minCorner /= PHYSICS_SECTOR_SIZE_METERS;
            maxCorner /= PHYSICS_SECTOR_SIZE_METERS;

            for (var it = new Vector3I.RangeIterator(ref minCorner, ref maxCorner);
                it.IsValid(); it.MoveNext())
            {
               if(m_physicsShapes.ContainsKey(it.Current))
               {
                   m_physicsShapes[it.Current].PrefetchShapeOnRay(ref ray);
               }
            }
        }
    }
}
