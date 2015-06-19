using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using VRage.Components;


namespace Sandbox.Game.Entities
{
    [MyEntityType(typeof(MyObjectBuilder_Planet))]
    public class MyPlanet : MyVoxelBase, IMyGravityProvider, IMyOxygenProvider
    {
        const float GRAVITY_AT_CORE = 4.0f;
        const int PHYSICS_SECTOR_SIZE_METERS = 2048;
        const float DEFAULT_GRAVITY_RADIUS_KM = 50.0f;
        const int ENVIROMENT_EXTEND = 1;
        const int ENVIROMENT_EXTEND_KEEP =  2*ENVIROMENT_EXTEND;

        Dictionary<Vector3I, MyVoxelPhysics> m_physicsShapes;
        MyDynamicObjectPool<MyPlanetEnvironmentSector> m_planetSectorsPool;
        Dictionary<Vector3I, MyPlanetEnvironmentSector> m_planetEnvironmentSectors;
        List<Vector3I> m_savedEnviromentSectors;

        List<Vector3I> m_sectorsToKeep = new List<Vector3I>();
        List<Vector3I> m_sectorsToRemove = new List<Vector3I>();

        Vector3I m_numCells;

        override public Vector3D PositionLeftBottomCorner
        {
            get
            {
                return base.PositionLeftBottomCorner;
            }
            set
            {
                if(value != base.PositionLeftBottomCorner)
                {
                    base.PositionLeftBottomCorner = value;

                    if (m_physicsShapes != null)
                    {
                        foreach (var physicsShape in m_physicsShapes)
                        {
                            Vector3D pos = PositionLeftBottomCorner + physicsShape.Key * PHYSICS_SECTOR_SIZE_METERS * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
                            physicsShape.Value.PositionLeftBottomCorner = pos;
                            physicsShape.Value.PositionComp.SetPosition(pos + physicsShape.Value.Size * 0.5f);
                        }
                    }
                }
            }
        }

        float m_planetRadius = 0.0f;
        float m_atmosphereRadius = 0.0f;
        float m_minimumSurfaceRadius = 0.0f;
        float m_maximumHillRadius = 0.0f;

        Vector3 m_atmosphereWavelengths;
        public Vector3 AtmosphereWavelengths
        {
            get 
            {
                return m_atmosphereWavelengths;
            }
        }

        bool m_hasAtmosphere = false;
        bool m_hasSpawningMaterial = false;

        float m_maximumOxygen = 0.0f;

        public MyPlanet()
        {
            (PositionComp as MyPositionComponent).WorldPositionChanged = WorldPositionChanged;

            Render = new MyRenderComponentPlanet();
            AddDebugRenderComponent(new MyDebugRenderComponentPlanet(this));
        }

        public float MinimumSurfaceRadius 
        {
            get 
            {
                return m_minimumSurfaceRadius;
            }
        }

        public float AtmosphereRadius
        {
            get
            {
                return m_atmosphereRadius;
            }
        }

        public bool HasAtmosphere
        {
            get
            {
                return m_hasAtmosphere;
            }
        }

        bool CanSpawnFlora
        {
            get 
            {
                return m_hasAtmosphere && MySession.Static.Settings.EnableFlora && m_hasSpawningMaterial;
            }
        }

        public override void Init(MyObjectBuilder_EntityBase builder)
        {        
            ProfilerShort.Begin("base init");

            SyncFlag = true;

            base.Init(builder);
            base.Init(null, null, null, null, null);

            ProfilerShort.BeginNextBlock("Load file");

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

            m_savedEnviromentSectors = ob.SavedEnviromentSectors;

            Init(StorageName, MyStorageBase.Load(ob.StorageName), ob.PositionAndOrientation.Value.Position, ob.Radius, ob.AtmosphereRadius, 
                ob.MaximumHillRadius, ob.MinimumSurfaceRadius, ob.HasAtmosphere, ob.AtmosphereWavelengths,ob.MaximumOxygen);

            ProfilerShort.End();
        }

        public void Init(string storageName, IMyStorage storage, Vector3D positionMinCorner, float averagePlanetRadius, float atmosphereRadius, float maximumHillRadius, float minimumSurfaceRadius, bool hasAtmosphere, Vector3 atmosphereWavelengths,float maxOxygen)
        {
            m_maximumOxygen = maxOxygen;
            m_atmosphereWavelengths = atmosphereWavelengths;
            m_hasSpawningMaterial = storage.DataProvider.HasMaterialSpawningFlora();
            m_maximumHillRadius = maximumHillRadius;
            m_minimumSurfaceRadius = minimumSurfaceRadius;
            m_planetRadius = averagePlanetRadius;
            m_atmosphereRadius = atmosphereRadius;
            m_hasAtmosphere = hasAtmosphere;

            base.Init(storageName,storage,positionMinCorner);

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            m_storage.RangeChanged += storage_RangeChangedPlanet;

            if (Physics != null)
            {
                Physics.Enabled = false;
                Physics.Close();
                Physics = null;
            }

            Vector3I storageSize = storage.Size;
            m_numCells = new Vector3I(storageSize.X / PHYSICS_SECTOR_SIZE_METERS, storageSize.Y / PHYSICS_SECTOR_SIZE_METERS, storageSize.Z / PHYSICS_SECTOR_SIZE_METERS);
         
            m_numCells -= 1;

           StorageName = storageName;
           m_storageMax = storage.Size;
          
           MyGravityProviderSystem.AddPlanet(this);
           MyOxygenProviderSystem.AddOxygenGenerator(this);

        }

        bool ChekPosition(Vector3D pos)
        {
            Vector3I newSector = Vector3I.Floor(pos / MyPlanetEnvironmentSector.SECTOR_SIZE_METERS);

            if (m_savedEnviromentSectors != null && m_savedEnviromentSectors.Contains(newSector))
            {
                return true;
            }
            MyPlanetEnvironmentSector sector;
            return m_planetEnvironmentSectors.TryGetValue(newSector, out sector);
        }

        Vector3D PlaceToOrbit(Vector3D pos,ref Vector3D gravity)
        {
            Vector3D planetRelativePos = (pos - this.PositionComp.GetPosition());
            double distanceToCenter = planetRelativePos.Length();
            double distanceToAtmosphere = m_atmosphereRadius - distanceToCenter;

            pos -= gravity * distanceToAtmosphere;
            pos -= PositionLeftBottomCorner;

            return pos;
        }

        public void GenerateFloraGraphics(Vector3D pos)
        {
            Vector3D gravity = GetWorldGravityNormalized(ref pos);
            Vector3D perpedincular = MyUtils.GetRandomPerpendicularVector(ref gravity);
            Vector3D third = Vector3D.Cross(gravity, perpedincular);

            perpedincular += third;

            Vector3I min = new Vector3I(-ENVIROMENT_EXTEND);
            Vector3I max = new Vector3I(ENVIROMENT_EXTEND);

            Vector3 offset = new Vector3(-MyPlanetEnvironmentSector.SECTOR_SIZE_METERS);
            for (var it = new Vector3I.RangeIterator(ref min, ref max); it.IsValid(); it.MoveNext())
            {
                Vector3D currentPos = pos + it.Current * offset * perpedincular;
                currentPos = PlaceToOrbit(currentPos, ref gravity);

                Vector3I newSector = Vector3I.Floor(currentPos / MyPlanetEnvironmentSector.SECTOR_SIZE_METERS);
                MyPlanetEnvironmentSector sector;
                if (true == m_planetEnvironmentSectors.TryGetValue(newSector, out sector) && sector.HasGraphics == false)
                {
                    sector.UpdateSectorGraphics();
                }
            }

        }

        public void SpawnFlora(Vector3D pos)
        {
            if (m_planetEnvironmentSectors == null)
            {
                m_planetEnvironmentSectors = new Dictionary<Vector3I, MyPlanetEnvironmentSector>(500);
            }

            Vector3D gravity = GetWorldGravityNormalized(ref pos);
            Vector3D perpedincular = MyUtils.GetRandomPerpendicularVector(ref gravity);
            Vector3D third = Vector3D.Cross(gravity, perpedincular);

            perpedincular += third;

            Vector3I min = new Vector3I(-ENVIROMENT_EXTEND);
            Vector3I max = new Vector3I(ENVIROMENT_EXTEND);

            Vector3 offset = new Vector3(-MyPlanetEnvironmentSector.SECTOR_SIZE_METERS);

            for (var it = new Vector3I.RangeIterator(ref min, ref max); it.IsValid(); it.MoveNext())
            {
                Vector3D currentPos = pos + it.Current * offset * perpedincular;
                currentPos = PlaceToOrbit(currentPos, ref gravity);

                if (false == ChekPosition(currentPos))
                {
                    Vector3I newSector = Vector3I.Floor(currentPos / MyPlanetEnvironmentSector.SECTOR_SIZE_METERS);
                    if (m_planetSectorsPool == null)
                    {
                        m_planetSectorsPool = new MyDynamicObjectPool<MyPlanetEnvironmentSector>(400);
                    }
                   

                    MyPlanetEnvironmentSector sector = m_planetSectorsPool.Allocate();

                    sector.Init(ref newSector, this);
                    m_planetEnvironmentSectors[newSector] = sector;
                    sector.PlaceItems();
                }
            }

            Vector3I sectorCoords = Vector3I.Floor(PlaceToOrbit(pos, ref gravity) / MyPlanetEnvironmentSector.SECTOR_SIZE_METERS);

            Vector3I keepMin = sectorCoords + new Vector3I(-ENVIROMENT_EXTEND_KEEP);
            Vector3I keepMax = sectorCoords + new Vector3I(ENVIROMENT_EXTEND_KEEP);

            foreach (var enviromentSector in m_planetEnvironmentSectors)
            {
                if (enviromentSector.Key.IsInsideInclusive(keepMin, keepMax))
                {
                    m_sectorsToKeep.Add(enviromentSector.Key);
                }
            }
        }
        
        protected override void Closing()
        {
            base.Closing();
            MyGravityProviderSystem.RemovePlanet(this);
            MyOxygenProviderSystem.RemoveOxygenGenerator(this);
        }

        protected override void BeforeDelete()
        {
            base.BeforeDelete();

            if (m_physicsShapes != null)
            {
                foreach (var voxelMap in m_physicsShapes)
                {
                    voxelMap.Value.Close();
                }
            }


            if (Render is MyRenderComponentPlanet)
            {
                (Render as MyRenderComponentPlanet).CancelAllRequests();
            }

            if (m_planetEnvironmentSectors != null)
            {
                foreach (var sector in m_planetEnvironmentSectors)
                {
                    sector.Value.CloseSector();
                    m_planetSectorsPool.Deallocate(sector.Value);
                }
            }

            m_storage = null;
            MySession.Static.VoxelMaps.RemoveVoxelMap(this);
        }

        private void storage_RangeChangedPlanet(Vector3I minChanged, Vector3I maxChanged, MyStorageDataTypeFlags dataChanged)
        {
            ProfilerShort.Begin("MyVoxelMap::storage_RangeChanged");
            Vector3I minSector = minChanged / PHYSICS_SECTOR_SIZE_METERS;
            Vector3I maxSector = maxChanged/PHYSICS_SECTOR_SIZE_METERS;

            Vector3I increment = m_storage.Size / (m_numCells+1);
            for (var it = new Vector3I.RangeIterator(ref minSector, ref maxSector);
                it.IsValid(); it.MoveNext())
            {
                MyVoxelPhysics voxelMap = CreatePhysicsShape(ref increment, ref it);

                voxelMap.OnStorageChanged(minChanged, maxChanged, dataChanged);
            }

            if (Render is MyRenderComponentVoxelMap)
            {
                (Render as MyRenderComponentVoxelMap).InvalidateRange(minChanged, maxChanged);
            }

            ProfilerShort.End();
        }

        private MyVoxelPhysics CreatePhysicsShape(ref Vector3I increment, ref Vector3I.RangeIterator it)
        {
            if(m_physicsShapes == null)
            {
                m_physicsShapes = new Dictionary<Vector3I, MyVoxelPhysics>();
            }

            MyVoxelPhysics voxelMap = null;
            if (m_physicsShapes.TryGetValue(it.Current, out voxelMap) == false)
            {
                voxelMap = new MyVoxelPhysics();

                Vector3I storageMin = it.Current * increment;
                Vector3I storageMax = storageMin + increment;

                voxelMap.EntityId = 0;
                voxelMap.Init(m_storage, this.PositionLeftBottomCorner + storageMin * MyVoxelConstants.VOXEL_SIZE_IN_METRES, storageMin, storageMax,this);
                voxelMap.Save = false;
                m_physicsShapes.Add(it.Current, voxelMap);
                MyEntities.Add(voxelMap);
            }
            return voxelMap;
        }

        public bool IsWorking
        {
            get { return true; }
        }

        public Vector3 GetWorldGravity(Vector3D worldPoint)
        {
            Vector3 direction = GetWorldGravityNormalized(ref worldPoint);

            double distance = (WorldMatrix.Translation - worldPoint).Length();
            float attenuation = 1.0f;

            if (distance > m_maximumHillRadius )
            {
                double distanceToRadius = m_planetRadius / (m_planetRadius + distance);
                attenuation = (float)(GRAVITY_AT_CORE * distanceToRadius * distanceToRadius);
            }

            float planetScale = m_planetRadius / (DEFAULT_GRAVITY_RADIUS_KM * 1000.0f);
            return direction * MyGravityProviderSystem.G * attenuation * planetScale;
        }

        public Vector3 GetWorldGravityNormalized(ref Vector3D worldPoint)
        {
            Vector3 direction = WorldMatrix.Translation - worldPoint;
            direction.Normalize();
            return direction;
        }

        public bool IsPositionInRange(Vector3D worldPoint)
        {
            return (WorldMatrix.Translation - worldPoint).Length() < 2.0f*m_planetRadius;
        }

        public Vector3 GetWorldGravityGrid(Vector3D worldPoint)
        {
            return GetWorldGravity(worldPoint);
        }

        public bool IsPositionInRangeGrid(Vector3D worldPoint)
        {
            return IsPositionInRange(worldPoint);
        }

        static List<MyEntity> m_entities = new List<MyEntity>();

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            UpdateFloraAndPhysics();
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            UpdateFloraAndPhysics();

            if(m_sectorsToKeep.Count > 0)
            {
                m_sectorsToRemove.Clear();
                foreach (var sector in m_planetEnvironmentSectors)
                { 
                    if(!m_sectorsToKeep.Contains(sector.Key))
                    {
                        m_sectorsToRemove.Add(sector.Key);
                    }
                }

                foreach (var sectorCoords in m_sectorsToRemove)
                {
                    MyPlanetEnvironmentSector sector = m_planetEnvironmentSectors[sectorCoords];
                    sector.CloseSector();
                    m_planetEnvironmentSectors.Remove(sectorCoords);
                    m_planetSectorsPool.Deallocate(sector);
                }

            }

            m_entities.Clear();
        }

        bool IsInRange(Vector3D pos)
        {
            double distance = (pos - this.PositionComp.GetPosition()).LengthSquared();
            return distance < AtmosphereRadius * AtmosphereRadius;
        }

        private void UpdateFloraAndPhysics()
        {
            BoundingBoxD box = this.PositionComp.WorldAABB;
            box.Min -= PHYSICS_SECTOR_SIZE_METERS;
            box.Max += PHYSICS_SECTOR_SIZE_METERS;
            m_entities.Clear();
            m_sectorsToKeep.Clear();

            MyGamePruningStructure.GetAllTopMostEntitiesInBox<MyEntity>(ref box, m_entities);
            Vector3I increment = m_storage.Size / (m_numCells + 1);

            ProfilerShort.Begin("Myplanet::update physics");
            foreach (var entity in m_entities)
            {
                if (entity.MarkedForClose || entity is MyPlanet || entity is MyVoxelMap)
                    continue;

                Vector3D position = entity.PositionComp.GetPosition();
                double distance = (WorldMatrix.Translation - position).Length();
                if (IsInRange(position) == false)
                {
                    continue;
                }

                var predictionOffset = ComputePredictionOffset(entity);

                if (CanSpawnFlora)
                {
                    ProfilerShort.Begin("Myplanet:: spawn flora");                   
                    SpawnFlora(position);
                    ProfilerShort.End();
                }

                var shapeBox = entity.PositionComp.WorldAABB;
                shapeBox.Inflate(MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES);
                shapeBox.Translate(predictionOffset);
                GeneratePhysicalShapeForBox(ref increment, ref shapeBox);
            }

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
                CreatePhysicsShape(ref increment, ref it);
                ProfilerShort.End();
            }
        }

        private Vector3 ComputePredictionOffset(IMyEntity entity)
        {
            if (entity.Physics == null)
            {
                return Vector3.Zero;
            }
            return entity.Physics.LinearVelocity * 3.0f;
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            if (CanSpawnFlora && MySession.LocalHumanPlayer != null)
            {
                Vector3D playerPosition = MySession.LocalHumanPlayer.GetPosition();
                if (IsInRange(playerPosition))
                {
                    GenerateFloraGraphics(playerPosition);
                }
            }

            m_sectorsToRemove.Clear();
            if (m_physicsShapes != null)
            {
                foreach (var physicsShape in m_physicsShapes)
                {
                    m_entities.Clear();
                    BoundingBoxD box = physicsShape.Value.PositionComp.WorldAABB;
                    box.Min -= 2.0 * box.HalfExtents;
                    box.Max += 2.0 * box.HalfExtents;

                    MyGamePruningStructure.GetAllTopMostEntitiesInBox<MyEntity>(ref box, m_entities);

                    if (m_entities.Count < 2)
                    {
                        m_sectorsToRemove.Add(physicsShape.Key);
                    }
                }
            }

            foreach (var shapeToRemove in m_sectorsToRemove)
            {
                MyVoxelPhysics physics;
                if (m_physicsShapes.TryGetValue(shapeToRemove, out physics))
                {
                    physics.Close();
                }
                m_physicsShapes.Remove(shapeToRemove);
            }
            m_sectorsToRemove.Clear();
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            MyObjectBuilder_Planet planetBuilder = (MyObjectBuilder_Planet)base.GetObjectBuilder(copy);

            planetBuilder.Radius = m_planetRadius;
            planetBuilder.HasAtmosphere = m_hasAtmosphere;
            planetBuilder.AtmosphereRadius = m_atmosphereRadius;
            planetBuilder.MinimumSurfaceRadius = m_minimumSurfaceRadius;
            planetBuilder.MaximumHillRadius = m_maximumHillRadius;
            planetBuilder.AtmosphereWavelengths = m_atmosphereWavelengths;
            planetBuilder.MaximumOxygen = m_maximumOxygen;
            planetBuilder.SavedEnviromentSectors = m_savedEnviromentSectors;
            return planetBuilder;
        }
      
        public float GetOxygenForPosition(Vector3D worldPoint)
        {
            if (m_hasAtmosphere == false)
            {
                return 0;
            }
            float distanceFromSurface =(float) ((WorldMatrix.Translation - worldPoint).Length() - m_planetRadius);
            return m_maximumOxygen*(1.0f-MathHelper.Saturate(distanceFromSurface / (m_atmosphereRadius - m_planetRadius)));
        }

        override public MyClipmapScaleEnum ScaleGroup
        {
            get
            {
                return MyClipmapScaleEnum.Massive;
            }
        }

        public void DebugDrawEnviromentSectors()
        {
            foreach (var sector in m_planetEnvironmentSectors)
            {
                VRageRender.MyRenderProxy.DebugDrawAABB(sector.Value.SectorBox, Color.Green, 1f, 1f, true);
            }
        }

        public Vector3D GetClosestSurfacePoint(ref Vector3D localPos,ref Vector3D gravity,int MaxNumIterations,int currentIteration)
        {
            var dataProvider = this.Storage.DataProvider;

            float distance = dataProvider.GetDistanceToPoint(ref localPos);        
            Vector3D newPos = localPos + gravity * distance;

            if (Math.Abs(distance) > 0.01f && MaxNumIterations > currentIteration) 
            {
                return GetClosestSurfacePoint(ref newPos, ref gravity, MaxNumIterations, MaxNumIterations++);
            }
            return newPos;
        }

        public void OnEnviromentSectorItemRemoved(Vector3I pos)
        {
            if (m_savedEnviromentSectors == null)
            {
                m_savedEnviromentSectors = new List<Vector3I>();
            }

            if (m_savedEnviromentSectors.Contains(pos) == false)
            {
                m_savedEnviromentSectors.Add(pos);
                if (m_planetEnvironmentSectors[pos].HasGraphics == false)
                {
                    m_planetEnvironmentSectors[pos].UpdateSectorGraphics();
                    m_planetEnvironmentSectors.Remove(pos);
                }
            }
        }
    }
}
