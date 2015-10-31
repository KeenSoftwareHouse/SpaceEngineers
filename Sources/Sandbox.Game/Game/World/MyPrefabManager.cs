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

namespace Sandbox.Game.World
{
    public class MyPrefabManager : Sandbox.ModAPI.IMyPrefabManager
    {
        private static List<MyCubeGrid> m_tmpSpawnedGridList = new List<MyCubeGrid>();

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
        public void AddShipPrefab(string prefabName, Matrix? worldMatrix = null)
        {
            m_tmpSpawnedGridList.Clear();
            CreateGridsFromPrefab(m_tmpSpawnedGridList, prefabName, worldMatrix ?? Matrix.Identity);

            foreach (var entity in m_tmpSpawnedGridList)
            {			
                MyEntities.Add(entity);
            }

            m_tmpSpawnedGridList.Clear();
        }

        // Note: This method is not synchronized. If you want synchronized prefab spawning, use SpawnPrefab
        public void AddShipPrefabRandomPosition(string prefabName, Vector3D position, float distance)
        {
            m_tmpSpawnedGridList.Clear();

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
            
            CreateGridsFromPrefab(m_tmpSpawnedGridList, prefabName, Matrix.CreateWorld(spawnPos, Vector3.Forward, Vector3.Up));

            foreach (var grid in m_tmpSpawnedGridList)
            {
                MyEntities.Add(grid);
            }

            m_tmpSpawnedGridList.Clear();
        }

        // Creates prefab, but won't add into scene
        // WorldMatrix is the matrix of the first grid in the prefab. The others will be transformed to keep their relative positions
        private void CreateGridsFromPrefab(List<MyCubeGrid> results, string prefabName, MatrixD worldMatrix, bool spawnAtOrigin = false, bool ignoreMemoryLimits = true)
        {
            var prefabDefinition = MyDefinitionManager.Static.GetPrefabDefinition(prefabName);
            Debug.Assert(prefabDefinition != null, "Could not spawn prefab named " + prefabName);
            if (prefabDefinition == null) return;

            MyObjectBuilder_CubeGrid[] gridObs = prefabDefinition.CubeGrids;

            Debug.Assert(gridObs.Count() != 0);
            if (gridObs.Count() == 0) return;

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

            List<MyCubeGrid> gridsToMove=new List<MyCubeGrid>();
            bool needMove=true;
            Vector3D moveVector=new Vector3D();
            bool ignoreMemoryLimitsPrevious = MyEntities.IgnoreMemoryLimits;
            MyEntities.IgnoreMemoryLimits = ignoreMemoryLimits;
            for (int i = 0; i < gridObs.Count(); ++i)
            {
                MyEntity entity = MyEntities.CreateFromObjectBuilder(gridObs[i]);
                MyCubeGrid cubeGrid = entity as MyCubeGrid;

                Debug.Assert(cubeGrid != null, "Could not create grid prefab!");
                if (cubeGrid != null)
                {
                    MatrixD originalGridMatrix = gridObs[i].PositionAndOrientation.HasValue ? gridObs[i].PositionAndOrientation.Value.GetMatrix() : MatrixD.Identity;
                    MatrixD newWorldMatrix;
                    newWorldMatrix = MatrixD.Multiply(originalGridMatrix, MatrixD.Multiply(translateToOriginMatrix, worldMatrix));

                    if (cubeGrid.IsStatic)
                    {
                        Debug.Assert(Vector3.IsZero(newWorldMatrix.Forward - Vector3.Forward, 0.001f), "Creating a static grid with orientation that is not identity");
                        Debug.Assert(Vector3.IsZero(newWorldMatrix.Up - Vector3.Up, 0.001f), "Creating a static grid with orientation that is not identity");
                        Vector3 rounded = default(Vector3I);
                        if (MyPerGameSettings.BuildingSettings.StaticGridAlignToCenter)
                            rounded = Vector3I.Round(newWorldMatrix.Translation / cubeGrid.GridSize) * cubeGrid.GridSize;
                        else
                            rounded = Vector3I.Round(newWorldMatrix.Translation / cubeGrid.GridSize + 0.5f) * cubeGrid.GridSize - 0.5f * cubeGrid.GridSize;
                        moveVector = new Vector3D(rounded - newWorldMatrix.Translation);
                        newWorldMatrix.Translation = rounded;
                        cubeGrid.WorldMatrix = newWorldMatrix;
                        needMove=false;

                        if (MyPerGameSettings.Destruction)
                        {
                            Debug.Assert(cubeGrid.Physics != null && cubeGrid.Physics.Shape != null);
                            if (cubeGrid.Physics != null && cubeGrid.Physics.Shape != null)
                            {
                                cubeGrid.Physics.Shape.RecalculateConnectionsToWorld(cubeGrid.GetBlocks());
                            }
                        }
                    }
                    else
                    {
                        newWorldMatrix.Translation += moveVector;
                        cubeGrid.WorldMatrix = newWorldMatrix;
                        if (needMove)
                            gridsToMove.Add(cubeGrid);
                    }
                    //if some mods are missing prefab can have 0 blocks,
                    //we don't want to process this grid
                    if (cubeGrid.CubeBlocks.Count > 0)
                    {
                        results.Add(cubeGrid);
                    }
                }
            }
            foreach (var grid in gridsToMove)
            {
                MatrixD wmatrix = grid.WorldMatrix;
                wmatrix.Translation += moveVector;
            }
            MyEntities.IgnoreMemoryLimits = ignoreMemoryLimitsPrevious;
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
            bool updateSync = false)
        {
            m_tmpSpawnedGridList.Clear();
            SpawnPrefabInternal(m_tmpSpawnedGridList, prefabName, position, forward, up, initialLinearVelocity, initialAngularVelocity, beaconName, spawningOptions, ownerId, updateSync);
            m_tmpSpawnedGridList.Clear();
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
            bool updateSync = false)
        {
            SpawnPrefabInternal(resultList, prefabName, position, forward, up, initialLinearVelocity, initialAngularVelocity, beaconName, spawningOptions, ownerId, updateSync);
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
            bool updateSync)
        {
            Debug.Assert(Vector3.IsUnit(ref forward));
            Debug.Assert(Vector3.IsUnit(ref up));
            Debug.Assert(Vector3.ArePerpendicular(ref forward, ref up));

            int rngSeed = 0;
            using (updateSync ? MyRandom.Instance.PushSeed(rngSeed = MyRandom.Instance.CreateRandomSeed()) : new MyRandom.StateToken())
            {
                bool spawnAtOrigin = spawningOptions.HasFlag(SpawningOptions.UseGridOrigin);
                CreateGridsFromPrefab(resultList, prefabName, MatrixD.CreateWorld(position, forward, up), spawnAtOrigin);

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
                            if (block.FatBlock is MyCockpit && rotateToCockpit && firstCockpit == null)
                            {
                                firstCockpit = (MyCockpit)block.FatBlock;
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

                Matrix transform = default(Matrix);
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
                    if (firstCockpit != null)
                    {
                        grid.WorldMatrix = grid.WorldMatrix * transform;
                    }
                    if (grid.Physics != null)
                    {
                        grid.Physics.LinearVelocity = initialLinearVelocity;
                        grid.Physics.AngularVelocity = initialAngularVelocity;
                    }

                    ProfilerShort.Begin("Add entity");
                    MyEntities.Add(grid);
                    ProfilerShort.End();
                }
            }
        }

        private static List<MyPhysics.HitInfo> m_raycastHits = new List<MyPhysics.HitInfo>();
        bool IMyPrefabManager.IsPathClear(Vector3D from, Vector3D to)
        {
            MyPhysics.CastRay(from, to, m_raycastHits, MyPhysics.ObjectDetectionCollisionLayer);
            m_raycastHits.Clear();
            return m_raycastHits.Count()== 0;
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
            MyPhysics.CastRay(from+other, to+other, m_raycastHits, MyPhysics.ObjectDetectionCollisionLayer);
            if (m_raycastHits.Count() > 0)
            {
                m_raycastHits.Clear();
                return false;
            }
            //second
            other *= -1;
            MyPhysics.CastRay(from + other, to + other, m_raycastHits, MyPhysics.ObjectDetectionCollisionLayer);
            if (m_raycastHits.Count() > 0)
            {
                m_raycastHits.Clear();
                return false;
            }
            //third
            other = Vector3D.Cross(forward, other);
            MyPhysics.CastRay(from + other, to + other, m_raycastHits, MyPhysics.ObjectDetectionCollisionLayer);
            if (m_raycastHits.Count() > 0)
            {
                m_raycastHits.Clear();
                return false;
            }
            //fourth
            other *= -1;
            MyPhysics.CastRay(from + other, to + other, m_raycastHits, MyPhysics.ObjectDetectionCollisionLayer);
            if (m_raycastHits.Count() > 0)
            {
                m_raycastHits.Clear();
                return false;
            }
            return true;
        }


    }
}
