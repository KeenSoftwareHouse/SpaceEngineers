using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using VRage;
using VRageMath;
using VRage.Utils;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;
using SteamSDK;
using VRage.Library.Utils;
using VRage.FileSystem;
using Sandbox.ModAPI;
using Sandbox.Engine.Physics;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using VRage.ObjectBuilders;
using Sandbox.Game.Entities.Blocks;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Profiler;

namespace Sandbox.Game.World
{
    public class MyPrefabManager : VRage.Game.ModAPI.IMyPrefabManager
    {
        //private static List<MyCubeGrid> m_tmpSpawnedGridList = new List<MyCubeGrid>();
        private static FastResourceLock m_builderLock = new FastResourceLock();

        public static EventWaitHandle FinishedProcessingGrids = new AutoResetEvent(false);
        public static int PendingGrids;

        static MyPrefabManager()
        {
            Static = new MyPrefabManager();
        }

        public static readonly MyPrefabManager Static;

        public static void SavePrefab(string prefabName, MyObjectBuilder_EntityBase entity)
        {
            var fsPath = Path.Combine(MyFileSystem.ContentPath, Path.Combine("Data", "Prefabs", prefabName + ".sbc"));

            var prefab = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PrefabDefinition>();
            prefab.Id = new MyDefinitionId(new MyObjectBuilderType(typeof(MyObjectBuilder_PrefabDefinition)), prefabName);
            prefab.CubeGrid = (MyObjectBuilder_CubeGrid)entity;
            
            var definitions = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Definitions>();
            definitions.Prefabs = new MyObjectBuilder_PrefabDefinition[1];
            definitions.Prefabs[0] = prefab;
            
            MyObjectBuilderSerializer.SerializeXML(fsPath, false, definitions);
        }

        public static void SavePrefab(string prefabName, List<MyObjectBuilder_CubeGrid> copiedPrefab)
        {
            var fsPath = Path.Combine(MyFileSystem.ContentPath, Path.Combine("Data", "Prefabs", prefabName + ".sbc"));

            SavePrefabToPath(prefabName, fsPath, copiedPrefab);
        }

        public static void SavePrefabToPath(string prefabName, string path, List<MyObjectBuilder_CubeGrid> copiedPrefab)
        {
            var prefab = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PrefabDefinition>();
            prefab.Id = new MyDefinitionId(new MyObjectBuilderType(typeof(MyObjectBuilder_PrefabDefinition)), prefabName);
            prefab.CubeGrids = copiedPrefab.ToArray();

            var definitions = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Definitions>();
            definitions.Prefabs = new MyObjectBuilder_PrefabDefinition[1];
            definitions.Prefabs[0] = prefab;

            MyObjectBuilderSerializer.SerializeXML(path, false, definitions);
        }

        public MyObjectBuilder_CubeGrid[] GetGridPrefab(string prefabName)
        {
            var prefabDefinition = MyDefinitionManager.Static.GetPrefabDefinition(prefabName);

            Debug.Assert(prefabDefinition != null, "Could not spawn prefab named " + prefabName);
            if (prefabDefinition == null) return null;

            MyObjectBuilder_CubeGrid[] grids = prefabDefinition.CubeGrids;
            MyEntities.RemapObjectBuilderCollection(grids);

            return grids;
        }

        // Note: This method is not synchronized. If you want synchronized prefab spawning, use SpawnPrefab
        public void AddShipPrefab(string prefabName, Matrix? worldMatrix = null, long factionId = 0, bool spawnAtOrigin = false)
        {
            //m_tmpSpawnedGridList.Clear();
            //CreateGridsFromPrefab(m_tmpSpawnedGridList, prefabName, worldMatrix ?? Matrix.Identity, factionId: factionId, spawnAtOrigin: spawnAtOrigin);
            CreateGridsData createGridsData = new CreateGridsData(new List<MyCubeGrid>(), prefabName, worldMatrix ?? Matrix.Identity, factionId: factionId, spawnAtOrigin: spawnAtOrigin);
            Interlocked.Increment(ref PendingGrids);
            ParallelTasks.Parallel.Start(createGridsData.CallCreateGridsFromPrefab, createGridsData.OnGridsCreated, createGridsData);

            //foreach (var entity in m_tmpSpawnedGridList)
            //{			
            //    MyEntities.Add(entity);
            //}

            //m_tmpSpawnedGridList.Clear();
        }

        // Note: This method is not synchronized. If you want synchronized prefab spawning, use SpawnPrefab
        public void AddShipPrefabRandomPosition(string prefabName, Vector3D position, float distance, long factionId = 0, bool spawnAtOrigin = false)
        {
            //m_tmpSpawnedGridList.Clear();

            var prefabDefinition = MyDefinitionManager.Static.GetPrefabDefinition(prefabName);
            Debug.Assert(prefabDefinition != null, "Could not spawn prefab named " + prefabName);
            if (prefabDefinition == null) return;

            BoundingSphereD collisionSphere = new BoundingSphereD(Vector3D.Zero, prefabDefinition.BoundingSphere.Radius);
            Vector3 spawnPos;
            MyEntity collidedEntity;
            int count = 0;
            do
            {
                spawnPos = position + MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(0.5f, 1.0f) * distance;
                collisionSphere.Center = spawnPos;
                collidedEntity = MyEntities.GetIntersectionWithSphere(ref collisionSphere);
                count++;
                if (count % 8 == 0)
                    distance += (float)collisionSphere.Radius / 2;
            }
            while (collidedEntity != null);

            //CreateGridsFromPrefab(m_tmpSpawnedGridList, prefabName, Matrix.CreateWorld(spawnPos, Vector3.Forward, Vector3.Up), factionId: factionId, spawnAtOrigin: spawnAtOrigin);
            CreateGridsData createGridsData = new CreateGridsData(new List<MyCubeGrid>(), prefabName, Matrix.CreateWorld(spawnPos, Vector3.Forward, Vector3.Up), factionId: factionId, spawnAtOrigin: spawnAtOrigin);
            Interlocked.Increment(ref PendingGrids);
            ParallelTasks.Parallel.Start(createGridsData.CallCreateGridsFromPrefab, createGridsData.OnGridsCreated, createGridsData);
            //foreach (var grid in m_tmpSpawnedGridList)
            //{
            //    MyEntities.Add(grid);
            //}

            //m_tmpSpawnedGridList.Clear();
        }

        /// <summary>
        /// Holds data for asynchrnonous initialization of prefabs
        /// </summary>
        public class CreateGridsData : ParallelTasks.WorkData
        {
            List<MyCubeGrid> m_results;
            string m_prefabName;
            MatrixD m_worldMatrix;
            bool m_spawnAtOrigin;
            bool m_ignoreMemoryLimits;
            long m_factionId;
            Stack<Action> m_callbacks;
            List<VRage.ModAPI.IMyEntity> m_resultIDs;

            public CreateGridsData(List<MyCubeGrid> results, string prefabName, MatrixD worldMatrix, bool spawnAtOrigin = false, bool ignoreMemoryLimits = true, long factionId = 0, Stack<Action> callbacks = null)
            {
                m_results = results;
                m_prefabName = prefabName;
                m_worldMatrix = worldMatrix;
                m_spawnAtOrigin = spawnAtOrigin;
                m_ignoreMemoryLimits = ignoreMemoryLimits;
                m_factionId = factionId;
                if (callbacks != null)
                    m_callbacks = callbacks;
                else
                    m_callbacks = new Stack<Action>();
            }

            public void CallCreateGridsFromPrefab(ParallelTasks.WorkData workData)
            {
                try
                {
                    MyEntityIdentifier.LazyInitPerThreadStorage(2048);
                    MyPrefabManager.Static.CreateGridsFromPrefab(m_results, m_prefabName, m_worldMatrix, m_spawnAtOrigin, m_ignoreMemoryLimits, m_factionId, m_callbacks);
                }
                finally
                {
                    m_resultIDs = new List<VRage.ModAPI.IMyEntity>();
                    MyEntityIdentifier.GetPerThreadEntities(m_resultIDs);
                    MyEntityIdentifier.ClearPerThreadEntities();
                    Interlocked.Decrement(ref PendingGrids);
                    if (PendingGrids <= 0)
                        FinishedProcessingGrids.Set();
                }
            }

            public void OnGridsCreated(ParallelTasks.WorkData workData)
            {
                foreach (var entity in m_resultIDs)
                {
                    VRage.ModAPI.IMyEntity foundEntity;
                    MyEntityIdentifier.TryGetEntity(entity.EntityId, out foundEntity);
                    if (foundEntity == null)
                        MyEntityIdentifier.AddEntityWithId(entity);
                    else
                        Debug.Fail("Two threads added the same entity");
                }
                foreach (var grid in m_results)
                {
                    MyEntities.Add(grid);
                    grid.IsReadyForReplication = true;
                }

                while (m_callbacks.Count > 0)
                {
                    var callback = m_callbacks.Pop();
                    if (callback != null)
                        callback();
                }
            }
        }

        // Creates prefab, but won't add into scene
        // WorldMatrix is the matrix of the first grid in the prefab. The others will be transformed to keep their relative positions
        private void CreateGridsFromPrefab(List<MyCubeGrid> results, string prefabName, MatrixD worldMatrix, bool spawnAtOrigin, bool ignoreMemoryLimits, long factionId, Stack<Action> callbacks)
        {
            var prefabDefinition = MyDefinitionManager.Static.GetPrefabDefinition(prefabName);
            Debug.Assert(prefabDefinition != null, "Could not spawn prefab named " + prefabName);
            if (prefabDefinition == null) return;

            MyObjectBuilder_CubeGrid[] gridObs = new MyObjectBuilder_CubeGrid[prefabDefinition.CubeGrids.Length];

            Debug.Assert(gridObs.Length != 0);
           
            if (gridObs.Length == 0) return;

            for (int i = 0; i < gridObs.Length; i++)
            {
                gridObs[i] = (MyObjectBuilder_CubeGrid)prefabDefinition.CubeGrids[i].Clone();
            }

            MyEntities.RemapObjectBuilderCollection(gridObs);

            MatrixD translateToOriginMatrix;
            if (spawnAtOrigin)
            {
                Vector3D translation = Vector3D.Zero;
                if (prefabDefinition.CubeGrids[0].PositionAndOrientation.HasValue)
                    translation = prefabDefinition.CubeGrids[0].PositionAndOrientation.Value.Position;
                translateToOriginMatrix = MatrixD.CreateWorld(-translation, Vector3D.Forward, Vector3D.Up);
            }
            else
            {
                translateToOriginMatrix = MatrixD.CreateWorld(-prefabDefinition.BoundingSphere.Center, Vector3D.Forward, Vector3D.Up);
            }

            //Vector3D moveVector=new Vector3D();
            bool ignoreMemoryLimitsPrevious = MyEntities.IgnoreMemoryLimits;
            MyEntities.IgnoreMemoryLimits = ignoreMemoryLimits;
            IMyFaction faction = MySession.Static.Factions.TryGetFactionById(factionId);
            for (int i = 0; i < gridObs.Length; ++i)
            {
                // Set faction defined in the operation
                if (faction != null)
                {
                    foreach (var cubeBlock in gridObs[i].CubeBlocks)
                    {
                        cubeBlock.Owner = faction.FounderId;
                        cubeBlock.ShareMode = MyOwnershipShareModeEnum.Faction;
                    }
                }

                MatrixD originalGridMatrix = gridObs[i].PositionAndOrientation.HasValue ? gridObs[i].PositionAndOrientation.Value.GetMatrix() : MatrixD.Identity;
                MatrixD newWorldMatrix;
                newWorldMatrix = MatrixD.Multiply(originalGridMatrix, MatrixD.Multiply(translateToOriginMatrix, worldMatrix));

                MyEntity entity = MyEntities.CreateFromObjectBuilder(gridObs[i], false);
                MyCubeGrid cubeGrid = entity as MyCubeGrid;


                Debug.Assert(cubeGrid != null, "Could not create grid prefab!");
                if (cubeGrid != null)
                {
                    //if some mods are missing prefab can have 0 blocks,
                    //we don't want to process this grid
                    if (cubeGrid.CubeBlocks.Count > 0)
                    {
                        results.Add(cubeGrid);
                        callbacks.Push(delegate() { SetPrefabPosition(entity, newWorldMatrix); }); 
                    }
                }
            }
            MyEntities.IgnoreMemoryLimits = ignoreMemoryLimitsPrevious;
        }

        private void SetPrefabPosition(MyEntity entity, MatrixD newWorldMatrix)
        {
            MyCubeGrid cubeGrid = entity as MyCubeGrid;

            if (cubeGrid != null)
            {
                cubeGrid.PositionComp.SetWorldMatrix(newWorldMatrix, forceUpdate: true);
                if (MyPerGameSettings.Destruction && cubeGrid.IsStatic)
                {
                    Debug.Assert(cubeGrid.Physics != null && cubeGrid.Physics.Shape != null);
                    if (cubeGrid.Physics != null && cubeGrid.Physics.Shape != null)
                    {
                        cubeGrid.Physics.Shape.RecalculateConnectionsToWorld(cubeGrid.GetBlocks());
                    }
                }
            }
        }

        public void SpawnPrefab(
            String prefabName,
            Vector3 position,
            Vector3 forward,
            Vector3 up,
            Vector3 initialLinearVelocity = default(Vector3),
            Vector3 initialAngularVelocity = default(Vector3),
            String beaconName = null,
            SpawningOptions spawningOptions = SpawningOptions.None,
            long ownerId = 0,
            bool updateSync = false,
            Stack<Action> callbacks = null)
        {
            //m_tmpSpawnedGridList.Clear();
            if (callbacks == null)
                callbacks = new Stack<Action>();
            SpawnPrefabInternal(new List<MyCubeGrid>()/*m_tmpSpawnedGridList*/, prefabName, position, forward, up, initialLinearVelocity, initialAngularVelocity, beaconName, spawningOptions, ownerId, updateSync, callbacks);
            //m_tmpSpawnedGridList.Clear();
        }

        public void SpawnPrefab(
            List<MyCubeGrid> resultList,
            String prefabName,
            Vector3D position,
            Vector3 forward,
            Vector3 up,
            Vector3 initialLinearVelocity = default(Vector3),
            Vector3 initialAngularVelocity = default(Vector3),
            String beaconName = null,
            SpawningOptions spawningOptions = SpawningOptions.None,
            long ownerId = 0,
            bool updateSync = false,
            Stack<Action> callbacks = null)
        {
            if (callbacks == null)
                callbacks = new Stack<Action>();
            SpawnPrefabInternal(resultList, prefabName, position, forward, up, initialLinearVelocity, initialAngularVelocity, beaconName, spawningOptions, ownerId, updateSync, callbacks);
        }

        void IMyPrefabManager.SpawnPrefab(
           List<IMyCubeGrid> resultList,
           String prefabName,
           Vector3D position,
           Vector3 forward,
           Vector3 up,
           Vector3 initialLinearVelocity,
           Vector3 initialAngularVelocity,
           String beaconName,
           SpawningOptions spawningOptions,
           bool updateSync)
        {
            List<MyCubeGrid> results=new List<MyCubeGrid>();
            SpawnPrefab(results,prefabName,position,forward,up,initialLinearVelocity,initialAngularVelocity,beaconName,spawningOptions,0,updateSync);
            foreach (var result in results)
                resultList.Add(result);
        }

        void IMyPrefabManager.SpawnPrefab(
           List<IMyCubeGrid> resultList,
           String prefabName,
           Vector3D position,
           Vector3 forward,
           Vector3 up,
           Vector3 initialLinearVelocity,
           Vector3 initialAngularVelocity,
           String beaconName,
           SpawningOptions spawningOptions,
           long ownerId,
           bool updateSync)
        {
            List<MyCubeGrid> results = new List<MyCubeGrid>();
            SpawnPrefab(results, prefabName, position, forward, up, initialLinearVelocity, initialAngularVelocity, beaconName, spawningOptions, ownerId, updateSync);
            foreach (var result in results)
                resultList.Add(result);
        }

        private void SpawnPrefabInternal(
            List<MyCubeGrid> resultList,
            String prefabName,
            Vector3D position,
            Vector3 forward,
            Vector3 up,
            Vector3 initialLinearVelocity,
            Vector3 initialAngularVelocity,
            String beaconName,
            SpawningOptions spawningOptions,
            long ownerId,
            bool updateSync,
            Stack<Action> callbacks)
        {
            Debug.Assert(Vector3.IsUnit(ref forward));
            Debug.Assert(Vector3.IsUnit(ref up));
            Debug.Assert(Vector3.ArePerpendicular(ref forward, ref up));

            bool spawnAtOrigin = spawningOptions.HasFlag(SpawningOptions.UseGridOrigin);
            //CreateGridsFromPrefab(resultList, prefabName, MatrixD.CreateWorld(position, forward, up), spawnAtOrigin);
            CreateGridsData createGridsData = new CreateGridsData(resultList, prefabName, MatrixD.CreateWorld(position, forward, up), spawnAtOrigin, callbacks: callbacks);
            Interlocked.Increment(ref PendingGrids);
            callbacks.Push(delegate() { SpawnPrefabInternalSetProperties(resultList, position, forward, up, initialLinearVelocity, initialAngularVelocity, beaconName, spawningOptions, ownerId, updateSync); });
            ParallelTasks.Parallel.Start(createGridsData.CallCreateGridsFromPrefab, createGridsData.OnGridsCreated, createGridsData);
        }

        private void SpawnPrefabInternalSetProperties(
            List<MyCubeGrid> resultList,
            Vector3D position,
            Vector3 forward,
            Vector3 up,
            Vector3 initialLinearVelocity,
            Vector3 initialAngularVelocity,
            String beaconName,
            SpawningOptions spawningOptions,
            long ownerId,
            bool updateSync)
        {
            int rngSeed = 0;
            using (updateSync ? MyRandom.Instance.PushSeed(rngSeed = MyRandom.Instance.CreateRandomSeed()) : new MyRandom.StateToken())
            {
                MyCockpit firstCockpit = null;

                bool rotateToCockpit = spawningOptions.HasFlag(SpawningOptions.RotateFirstCockpitTowardsDirection);
                bool spawnCargo = spawningOptions.HasFlag(SpawningOptions.SpawnRandomCargo);
                bool setNeutralOwner = spawningOptions.HasFlag(SpawningOptions.SetNeutralOwner);
                bool needsToIterateThroughBlocks = spawnCargo || rotateToCockpit || setNeutralOwner || beaconName != null;

                long owner = ownerId;
                if (updateSync && spawningOptions.HasFlag(SpawningOptions.SetNeutralOwner) && resultList.Count != 0)
                {
                    string npcName = "NPC " + MyRandom.Instance.Next(1000, 9999);
                    var identity = Sync.Players.CreateNewIdentity(npcName);
                    owner = identity.IdentityId;
                }
                bool setOwnership = owner != 0;

                List<MyCockpit> shipCockpits = new List<MyCockpit>();

                foreach (var grid in resultList)
                {
                    grid.ClearSymmetries();

                    if (spawningOptions.HasFlag(SpawningOptions.DisableDampeners))
                    {
	                    var thrustComp = grid.Components.Get<MyEntityThrustComponent>();
						if(thrustComp != null)
							thrustComp.DampenersEnabled = false;
                    }

                    if ((spawningOptions.HasFlag(SpawningOptions.DisableSave)))
                    {
                        grid.Save = false;
                    }
                    if (needsToIterateThroughBlocks || spawningOptions.HasFlag(SpawningOptions.TurnOffReactors))
                    {
                        ProfilerShort.Begin("Iterate through blocks");
                        foreach (var block in grid.GetBlocks())
                        {
                            if (block.FatBlock is MyCockpit && block.FatBlock.IsFunctional)
                            {
                                shipCockpits.Add(block.FatBlock as MyCockpit);
                            }

                            else if (block.FatBlock is MyCargoContainer && spawnCargo)
                            {
                                MyCargoContainer container = block.FatBlock as MyCargoContainer;
                                container.SpawnRandomCargo();
                            }

                            else if (block.FatBlock is MyBeacon && beaconName != null)
                            {
                                MyBeacon beacon = block.FatBlock as MyBeacon;
                                beacon.SetCustomName(beaconName);
                            }
							else if (spawningOptions.HasFlag(SpawningOptions.TurnOffReactors) && block.FatBlock != null && block.FatBlock.Components.Contains(typeof(MyResourceSourceComponent)))
							{
								var sourceComp = block.FatBlock.Components.Get<MyResourceSourceComponent>();
								if (sourceComp != null)
								{
									if(sourceComp.ResourceTypes.Contains(MyResourceDistributorComponent.ElectricityId))
										sourceComp.Enabled = false;
								}
							}
                            if (setOwnership && block.FatBlock != null && block.BlockDefinition.RatioEnoughForOwnership(block.BuildLevelRatio))
                            {
                                block.FatBlock.ChangeOwner(owner, MyOwnershipShareModeEnum.None);
                            }
                        }
                        ProfilerShort.End();
                    }
                }

                // First sort cockpits by order: Ship controlling cockpits set to main, then ship controlling cockpits not set to main, lastly whatever remains, e.g. CryoChambers and Passenger Seats
                if (shipCockpits.Count > 1)
                {
                    shipCockpits.Sort(delegate(MyCockpit cockpitA, MyCockpit cockpitB)
                    {
                        int controlCompare = cockpitB.EnableShipControl.CompareTo(cockpitA.EnableShipControl);
                        if (controlCompare != 0) return controlCompare;

                        int mainCompare = cockpitB.IsMainCockpit.CompareTo(cockpitA.IsMainCockpit);
                        if (mainCompare != 0) return mainCompare;

                        return 0;
                    });
                }
                if (shipCockpits.Count > 0)
                    firstCockpit = shipCockpits[0];

                // Try to rotate to the first cockpit
                Matrix transform = Matrix.Identity;
                if (rotateToCockpit)
                {
                    System.Diagnostics.Debug.Assert(firstCockpit != null,"cockpit in prefab ship is missing !");
                    if (firstCockpit != null)
                    {
                        Matrix cockpitTransform = firstCockpit.WorldMatrix;
                        Matrix cockpitInvertedTransform = Matrix.Invert(cockpitTransform);
                        transform = Matrix.Multiply(cockpitInvertedTransform, Matrix.CreateWorld(firstCockpit.WorldMatrix.Translation, forward, up));
                    }
                }

                foreach (var grid in resultList)
                {
                    if (firstCockpit != null && rotateToCockpit)
                    {
                        grid.WorldMatrix = grid.WorldMatrix * transform;
                    }
                    if (grid.Physics != null)
                    {
                        grid.Physics.LinearVelocity = initialLinearVelocity;
                        grid.Physics.AngularVelocity = initialAngularVelocity;
                    }

                    ProfilerShort.Begin("Add entity");
                    //MyEntities.Add(grid);
                    ProfilerShort.End();
                }
            }
        }

        private static List<MyPhysics.HitInfo> m_raycastHits = new List<MyPhysics.HitInfo>();
        bool IMyPrefabManager.IsPathClear(Vector3D from, Vector3D to)
        {
            MyPhysics.CastRay(from, to, m_raycastHits, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);
            m_raycastHits.Clear();
            return m_raycastHits.Count== 0;
        }
        bool IMyPrefabManager.IsPathClear(Vector3D from, Vector3D to, double halfSize)
        {
            Vector3D other=new Vector3D();
            other.X=1;
            Vector3D forward=to-from;
            forward.Normalize();
            if (Vector3D.Dot(forward,other)>0.9f || Vector3D.Dot(forward,other)<-0.9f)
            {
                other.X = 0;
                other.Y = 1;
            }
            other=Vector3D.Cross(forward,other);
            other.Normalize();
            other = other * halfSize;
            //first
            MyPhysics.CastRay(from+other, to+other, m_raycastHits, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);
            if (m_raycastHits.Count > 0)
            {
                m_raycastHits.Clear();
                return false;
            }
            //second
            other *= -1;
            MyPhysics.CastRay(from + other, to + other, m_raycastHits, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);
            if (m_raycastHits.Count > 0)
            {
                m_raycastHits.Clear();
                return false;
            }
            //third
            other = Vector3D.Cross(forward, other);
            MyPhysics.CastRay(from + other, to + other, m_raycastHits, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);
            if (m_raycastHits.Count > 0)
            {
                m_raycastHits.Clear();
                return false;
            }
            //fourth
            other *= -1;
            MyPhysics.CastRay(from + other, to + other, m_raycastHits, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);
            if (m_raycastHits.Count > 0)
            {
                m_raycastHits.Clear();
                return false;
            }
            return true;
        }


    }
}
