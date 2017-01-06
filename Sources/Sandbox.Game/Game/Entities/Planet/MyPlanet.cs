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
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Profiler;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

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
        public bool InitializeComponents;

        public override string ToString()
        {
            return "Planet init arguments: \nStorage name: " + (StorageName ?? "<null>")
                   + "\n Storage: " + (Storage != null ? Storage.ToString() : "<null>")
                   + "\n PositionMinCorner: " + PositionMinCorner
                   + "\n Radius: " + Radius
                   + "\n AtmosphereRadius: " + AtmosphereRadius
                   + "\n MaxRadius: " + MaxRadius
                   + "\n MinRadius: " + MinRadius
                   + "\n HasAtmosphere: " + HasAtmosphere
                   + "\n AtmosphereWavelengths: " + AtmosphereWavelengths
                   + "\n GravityFalloff: " + GravityFalloff
                   + "\n MarkAreaEmpty: " + MarkAreaEmpty
                   + "\n AtmosphereSettings: " + AtmosphereSettings.ToString()
                   + "\n SurfaceGravity: " + SurfaceGravity
                   + "\n AddGps: " + AddGps
                   + "\n SpherizeWithDistance: " + SpherizeWithDistance
                   + "\n Generator: " + (Generator != null ? Generator.ToString() : "<null>")
                   + "\n UserCreated: " + UserCreated
                   + "\n InitializeComponents: " + InitializeComponents;
        }
    }

    [MyEntityType(typeof(MyObjectBuilder_Planet))]
    public partial class MyPlanet : MyVoxelBase, IMyOxygenProvider
    {
        public const int PHYSICS_SECTOR_SIZE_METERS = 1024;

        private const double INTRASECTOR_OBJECT_CLUSTER_SIZE = PHYSICS_SECTOR_SIZE_METERS / 2;

        public static bool RUN_SECTORS = false;

        private List<BoundingBoxD> m_clustersIntersection = new List<BoundingBoxD>();

        #region Shape properties

        public float AtmosphereAltitude
        {
            get;
            private set;
        }

        #endregion Shape properties

        #region Oxygen & Atmosphere

        bool IMyOxygenProvider.IsPositionInRange(Vector3D worldPoint)
        {
            if (Generator == null || !Generator.HasAtmosphere || !Generator.Atmosphere.Breathable) 
                return false;

            return (WorldMatrix.Translation - worldPoint).Length() < AtmosphereAltitude + AverageRadius;
        }

        public float GetOxygenForPosition(Vector3D worldPoint)
        {
            if (Generator == null)
                return 0;

            if (Generator.Atmosphere.Breathable)
                return GetAirDensity(worldPoint) * Generator.Atmosphere.OxygenDensity;
            return 0f;
        }

        public float GetAirDensity(Vector3D worldPosition)
        {
            if (Generator == null)
                return 0;

            if (Generator.HasAtmosphere)
            {
                double distance = (worldPosition - WorldMatrix.Translation).Length();
                var rate = MathHelper.Clamp(1 - (distance - AverageRadius) / (AtmosphereAltitude), 0, 1);
                return (float)rate * Generator.Atmosphere.Density;
            }

            return 0f;
        }

        #endregion Oxygen & Atmosphere

        #region Gravity

        // THe gravity limit gets calculated from the GRAVITY_LIMIT_STRENGTH so that the gravity stops where it is equal to G_L_S

        #endregion Gravity

        public MyPlanetStorageProvider Provider
        {
            get;
            private set;
        }

        private MyConcurrentDictionary<Vector3I, MyVoxelPhysics> m_physicsShapes;

        private HashSet<Vector3I> m_sectorsPhysicsToRemove = new HashSet<Vector3I>();
        private Vector3I m_numCells;

        private bool m_canSpawnSectors = true;

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
                                               physicsShape.Key * PHYSICS_SECTOR_SIZE_METERS *
                                               MyVoxelConstants.VOXEL_SIZE_IN_METRES;
                                physicsShape.Value.PositionLeftBottomCorner = pos;
                                physicsShape.Value.PositionComp.SetPosition(pos + physicsShape.Value.Size * 0.5f);
                            }
                        }
                    }
                }
            }
        }

        private MyPlanetInitArguments m_planetInitValues;

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
                return Provider != null ? Provider.Shape.InnerRadius : 0;
            }
        }

        public float AverageRadius
        {
            get
            {
                return Provider != null ? Provider.Shape.Radius : 0;
            }
        }

        public float MaximumRadius
        {
            get
            {
                return Provider != null ? Provider.Shape.OuterRadius : 0;
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

        private bool CanSpawnFlora
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

            ProfilerShort.BeginNextBlock("Load Saved Data");

            var ob = (MyObjectBuilder_Planet) builder;
            if (ob == null)
            {
                return;
            }

            MyLog.Default.WriteLine("Planet init info - MutableStorage:" + ob.MutableStorage + " StorageName:" + ob.StorageName + " storage?:" + (storage != null).ToString());

            if (ob.MutableStorage)
            {
                StorageName = ob.StorageName;
            }
            else
            {
                StorageName = string.Format("{0}", ob.StorageName);
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
            m_planetInitValues.Generator = ob.PlanetGenerator == ""
                ? null
                : MyDefinitionManager.Static.GetDefinition<MyPlanetGeneratorDefinition>(
                    MyStringHash.GetOrCompute(ob.PlanetGenerator));
            if (m_planetInitValues.Generator == null)
            {
                string message = string.Format("No definition found for planet generator {0}.", ob.PlanetGenerator);
                MyLog.Default.WriteLine(message);
                throw new MyIncompatibleDataException(message);
            }

            m_planetInitValues.AtmosphereSettings = m_planetInitValues.Generator.AtmosphereSettings.HasValue
                ? m_planetInitValues.Generator.AtmosphereSettings.Value
                : MyAtmosphereSettings.Defaults();
            m_planetInitValues.UserCreated = false;

            ProfilerShort.BeginNextBlock("Load Storage");
            if (storage != null)
            {
                m_planetInitValues.Storage = storage;
            }
            else
            {
                m_planetInitValues.Storage = MyStorageBase.Load(ob.StorageName);
                
                if (m_planetInitValues.Storage == null)
                {
                    string message = string.Format("No storage loaded for planet {0}.", ob.StorageName);
                    MyLog.Default.WriteLine(message);
                    throw new MyIncompatibleDataException(message);
                }
            }

            m_planetInitValues.InitializeComponents = false;

            ProfilerShort.BeginNextBlock("Init Internal");

            // MZ: if any crashes are related to MP planet init in the future, i added logging of MyPlanetInitArguments and other sanity checks.
            //     we are currently having crashes without this additional info and it is likely that even after my hotfixes it is going to crash again
            //     ...but we can check the logs and know the setup of the player :)
            MyLog.Default.Log(MyLogSeverity.Info, "Planet generator name: {0}", ob.PlanetGenerator ?? "<null>");

            // Initialize!
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
            
            // MZ: if any crashes are related to MP planet init in the future, i added logging of MyPlanetInitArguments and other sanity checks.
            //     we are currently having crashes without this additional info and it is likely that even after my hotfixes it is going to crash again
            //     ...but we can check the logs and know the setup of the player :)
            MyLog.Default.Log(MyLogSeverity.Info, "Planet init values: {0}", m_planetInitValues.ToString());   // m_planetInitValues is struct and therefore never null

            // Parameteres from storage
            if (m_planetInitValues.Storage == null)
            {
                MyLog.Default.Log(MyLogSeverity.Error, "MyPlanet.Init: Planet storage is null! Init of the planet was cancelled.");
                return;
            }

            Provider = m_planetInitValues.Storage.DataProvider as MyPlanetStorageProvider;
            System.Diagnostics.Debug.Assert(Provider != null, "Invalid provider!");
            if (Provider == null)
            {
                MyLog.Default.Error("Invalid plane provider!");
                return;
            }

            if (Provider == null)
            {
                MyLog.Default.Log(MyLogSeverity.Error, "MyPlanet.Init: Planet storage provider is null! Init of the planet was cancelled.");
                return;
            }

            if (arguments.Generator == null)
            {
                MyLog.Default.Log(MyLogSeverity.Error, "MyPlanet.Init: Planet generator is null! Init of the planet was cancelled.");
                return;
            }

            m_planetInitValues.Radius = Provider.Radius;
            m_planetInitValues.MaxRadius = Provider.Shape.OuterRadius;
            m_planetInitValues.MinRadius = Provider.Shape.InnerRadius;

            Generator = arguments.Generator;

            AtmosphereAltitude = Provider.Shape.MaxHillHeight * (Generator != null ? Generator.Atmosphere.LimitAltitude : 1);

            base.Init(m_planetInitValues.StorageName, m_planetInitValues.Storage, m_planetInitValues.PositionMinCorner);

            // Set storage as caching:
            ((MyStorageBase)Storage).InitWriteCache();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            m_storage.RangeChanged += storage_RangeChangedPlanet;

            if (m_planetInitValues.MarkAreaEmpty && MyProceduralWorldGenerator.Static != null)
            {
                MyProceduralWorldGenerator.Static.MarkEmptyArea(PositionComp.GetPosition(), m_planetInitValues.MaxRadius);
            }

            if (Physics != null)
            {
                Physics.Enabled = false;
                Physics.Close();
                Physics = null;
            }

            if (Name == null)
                Name = StorageName;

            /* Prepare sectors */
            Vector3I storageSize = m_planetInitValues.Storage.Size;
            m_numCells = new Vector3I(storageSize.X / PHYSICS_SECTOR_SIZE_METERS, storageSize.Y / PHYSICS_SECTOR_SIZE_METERS, storageSize.Z / PHYSICS_SECTOR_SIZE_METERS);
            m_numCells -= 1;
            m_numCells = Vector3I.Max(Vector3I.Zero, m_numCells);

            CanSpawnFlora = Generator != null && Generator.MaterialEnvironmentMappings.Count != 0 && MySession.Static.EnableFlora && MyFakes.ENABLE_ENVIRONMENT_ITEMS && Storage.DataProvider is MyPlanetStorageProvider;

            StorageName = m_planetInitValues.StorageName;
            m_storageMax = m_planetInitValues.Storage.Size;

            // Init sector metadata
            PrepareSectors();

            // Prepare components
            // TODO: breaks loading of worlds. Overrides loaded deserialization of ownership components replacing them with clean one. Will be fixed after Daniel fixes generation of components on planet when new world is created. Also remove bool from arguments.
            if (arguments.InitializeComponents && Generator != null)
                HackyComponentInitByMiroPleaseDontUseEver(new MyDefinitionId(typeof(MyObjectBuilder_Planet), Generator.Id.SubtypeId));

            if (Generator != null && Generator.EnvironmentDefinition != null)
            {
                if (!Components.Contains(typeof(MyPlanetEnvironmentComponent)))
                    Components.Add(new MyPlanetEnvironmentComponent());
                Components.Get<MyPlanetEnvironmentComponent>().InitEnvironment();
            }

            Components.Add<MyGravityProviderComponent>(new MySphericalNaturalGravityComponent(m_planetInitValues.MinRadius, m_planetInitValues.MaxRadius, m_planetInitValues.GravityFalloff, m_planetInitValues.SurfaceGravity));

            if (m_planetInitValues.UserCreated)
            {
                ContentChanged = true;
            }
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            MyObjectBuilder_Planet planetBuilder = (MyObjectBuilder_Planet)base.GetObjectBuilder(copy);

            planetBuilder.Radius = m_planetInitValues.Radius;
            planetBuilder.HasAtmosphere = m_planetInitValues.HasAtmosphere;
            planetBuilder.AtmosphereRadius = m_planetInitValues.AtmosphereRadius;
            planetBuilder.MinimumSurfaceRadius = m_planetInitValues.MinRadius;
            planetBuilder.MaximumHillRadius = m_planetInitValues.MaxRadius;
            planetBuilder.AtmosphereWavelengths = m_planetInitValues.AtmosphereWavelengths;
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

        #endregion Load/Unload

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            MyPlanets.Register(this);

            MyGravityProviderSystem.AddNaturalGravityProvider(Components.Get<MyGravityProviderComponent>());
            MyOxygenProviderSystem.AddOxygenGenerator(this);
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);

            MyPlanets.UnRegister(this);

            MyGravityProviderSystem.RemoveNaturalGravityProvider(Components.Get<MyGravityProviderComponent>());
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
                for (var it = new Vector3I_RangeIterator(ref minSector, ref maxSector);
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

        private MyVoxelPhysics CreateVoxelPhysics(ref Vector3I increment, ref Vector3I_RangeIterator it)
        {
            if (m_physicsShapes == null)
            {
                m_physicsShapes = new MyConcurrentDictionary<Vector3I, MyVoxelPhysics>();
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
                        this.PositionLeftBottomCorner + storageMin * MyVoxelConstants.VOXEL_SIZE_IN_METRES, storageMin,
                        storageMax, this);
                    voxelMap.Save = false;
                    MyEntities.Add(voxelMap);
                }

                m_physicsShapes.Add(it.Current, voxelMap);
            }
            return voxelMap;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            UpdateFloraAndPhysics(true);

            if (m_planetInitValues.AddGps)
            {
                MyGps newGps = new MyGps()
                {
                    Name = StorageName,
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

            UpdatePlanetPhysics(ref box);
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

            for (var it = new Vector3I_RangeIterator(ref minCorner, ref maxCorner);
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

        private List<MyEntity> m_entities = new List<MyEntity>();

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
                        if (entity.Physics != null)
                        {
                            if (entity.Physics.IsStatic)
                            {
                                MyCubeGrid grid = entity as MyCubeGrid;
                                //welded grids to voxels are static but planet physics sector needs to be kept for them
                                if (grid != null && grid.IsStatic == false)
                                {
                                    keep = true;
                                }
                            }
                            else
                            {
                                keep = true;
                            }
                        }
                    }

                    if (!keep)
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

        #endregion Planet Physics

        public override MyClipmapScaleEnum ScaleGroup
        {
            get
            {
                return MyClipmapScaleEnum.Massive;
            }
        }

        /// <param name="resumeSearch">Don't modify initial search position</param>
        /// <returns>True if it a safe position is found</returns>
        public bool CorrectSpawnLocation2(ref Vector3D position, double radius, bool resumeSearch = false)
        {
            Vector3D upVector = position - WorldMatrix.Translation;
            upVector.Normalize();

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
                MyVoxelCoordSystems.WorldPositionToLocalPosition(PositionLeftBottomCorner, ref position, out localPos);

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

                MyVoxelCoordSystems.WorldPositionToLocalPosition(PositionLeftBottomCorner, ref fixedPosition, out localPos);
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
            Vector3D upVector = position - WorldMatrix.Translation;
            upVector.Normalize();

            // Calculate if the position is not inside of the planet:
            Vector3D localPos;
            MyVoxelCoordSystems.WorldPositionToLocalPosition(PositionLeftBottomCorner, ref position, out localPos);

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

                MyVoxelCoordSystems.WorldPositionToLocalPosition(PositionLeftBottomCorner, ref position, out localPos);
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

            if (!localPos.IsValid())
            {
                Debug.Fail("Invalid position!");
                return Vector3D.Zero;
            }

            Provider.Shape.ProjectToSurface(localPos, out localSurface);

            return localSurface;
        }

        public override void DebugDrawPhysics()
        {
            if (m_physicsShapes != null)
            {
                foreach (var shape in m_physicsShapes)
                {
                    Vector3 min = (Vector3)shape.Key * PHYSICS_SECTOR_SIZE_METERS + PositionLeftBottomCorner;
                    var box = new BoundingBoxD(min, min + PHYSICS_SECTOR_SIZE_METERS);

                    if (shape.Value != null && !shape.Value.Closed)
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
            return Name.GetHashCode();
        }

        public void PrefetchShapeOnRay(ref LineD ray)
        {
            Vector3I minCorner, maxCorner;
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(PositionLeftBottomCorner, ref ray.From, out minCorner);
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(PositionLeftBottomCorner, ref ray.To, out maxCorner);

            minCorner /= PHYSICS_SECTOR_SIZE_METERS;
            maxCorner /= PHYSICS_SECTOR_SIZE_METERS;

            for (var it = new Vector3I_RangeIterator(ref minCorner, ref maxCorner);
                it.IsValid(); it.MoveNext())
            {
                if (m_physicsShapes.ContainsKey(it.Current))
                {
                    m_physicsShapes[it.Current].PrefetchShapeOnRay(ref ray);
                }
            }
        }
    }
}