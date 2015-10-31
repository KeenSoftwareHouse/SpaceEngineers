﻿#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using VRage.Utils;
using VRageMath;

using VRage;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Common.Components;
using VRage.Voxels;
using VRage.Components;
using VRage.ObjectBuilders;
using Sandbox.Game.Entities.Character;


#endregion

namespace Sandbox.Game.Entities
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, 800)]
    public class MyFloatingObjects : MySessionComponentBase
    {
        #region Comparer

        private class MyFloatingObjectComparer : IEqualityComparer<MyFloatingObject>
        {
            public bool Equals(MyFloatingObject x, MyFloatingObject y)
            {
                return x.EntityId == y.EntityId;
            }

            public int GetHashCode(MyFloatingObject obj)
            {
                return (int)obj.EntityId;
            }
        }

        private class MyFloatingObjectTimestampComparer : IComparer<MyFloatingObject>
        {
            public int Compare(MyFloatingObject x, MyFloatingObject y)
            {
                if (x.CreationTime != y.CreationTime)
                    return y.CreationTime.CompareTo(x.CreationTime);

                return y.EntityId.CompareTo(x.EntityId);
            }
        }

        class MyFloatingObjectsSynchronizationComparer : IComparer<MyFloatingObject>
        {
            public int Compare(MyFloatingObject x, MyFloatingObject y)
            {
                return x.ClosestDistanceToAnyPlayerSquared.CompareTo(y.ClosestDistanceToAnyPlayerSquared);
            }
        }

        #endregion

        private struct StabilityInfo
        {
            public MyPositionAndOrientation PositionAndOr;

            public StabilityInfo(MyPositionAndOrientation posAndOr)
            {
                PositionAndOr = posAndOr;
            }
        }

        #region Fields

        private static MyFloatingObjects m_instance;

        private static MyFloatingObjectComparer m_entityComparer = new MyFloatingObjectComparer();
        private static MyFloatingObjectTimestampComparer m_comparer = new MyFloatingObjectTimestampComparer();
        private static SortedSet<MyFloatingObject> m_floatingOres = new SortedSet<MyFloatingObject>(m_comparer);
        private static SortedSet<MyFloatingObject> m_floatingItems = new SortedSet<MyFloatingObject>(m_comparer);

        //Sync
        static List<MyVoxelBase> m_tmpResultList = new List<MyVoxelBase>();
        static List<MyFloatingObject> m_synchronizedFloatingObjects = new List<MyFloatingObject>();
        static List<MyFloatingObject> m_floatingObjectsToSyncCreate = new List<MyFloatingObject>();
        static MyFloatingObjectsSynchronizationComparer m_synchronizationComparer = new MyFloatingObjectsSynchronizationComparer();
        static MySyncFloatingObjects SyncObject;
        static List<MyFloatingObject> m_highPriority = new List<MyFloatingObject>();
        static List<MyFloatingObject> m_normalPriority = new List<MyFloatingObject>();
        static List<MyFloatingObject> m_lowPriority = new List<MyFloatingObject>();
        static Stopwatch m_measurementTime;
        static int m_updateCounter = 0;
        static int m_highMessagesSent = 0;
        static int m_normalMessagesSent = 0;
        static int m_lowMessagesSent = 0;
        static int m_highIndex = 0;
        static int m_normalIndex = 0;
        static int m_lowIndex = 0;
        static float m_kilobytesPerSecond = 0;
        static int m_syncCounter = 0;
        static bool m_needReupdateNewObjects = false;
        static int m_checkObjectInsideVoxel = 0;

		static List<Tuple<MyPhysicalInventoryItem, BoundingBoxD, Vector3D>> m_itemsToSpawnNextUpdate = new List<Tuple<MyPhysicalInventoryItem, BoundingBoxD, Vector3D>>();

        static Dictionary<MyFloatingObject, StabilityInfo> m_stableObjectsClient = new Dictionary<MyFloatingObject, StabilityInfo>(m_entityComparer);
        static Dictionary<MyFloatingObject, StabilityInfo> m_tmpObjects = new Dictionary<MyFloatingObject, StabilityInfo>(m_entityComparer);

        static HashSet<long> m_requestedEntities = new HashSet<long>();
        static List<long> m_tmpEntities = new List<long>();
        static int m_requestDelay = 0;
        static int m_noRequestedCounter = 0;
        static bool m_stableDirty = false;

        static HashSet<MyFloatingObject> m_stableObjectsServer = new HashSet<MyFloatingObject>(m_entityComparer);
        static HashSet<MyFloatingObject> m_futureStableObjectsServer = new HashSet<MyFloatingObject>(m_entityComparer);
        static HashSet<MyFloatingObject> m_removedStableObjectsServer = new HashSet<MyFloatingObject>(m_entityComparer);

        #endregion

        public override void LoadData()
        {
            base.LoadData();

            Debug.Assert(m_instance == null);
            m_instance = this;

            SyncObject = new MySyncFloatingObjects(this);
            m_measurementTime = new Stopwatch();
            m_measurementTime.Start();
        }

        protected override void UnloadData()
        {
            //Debug.Assert(m_instance == this);
            m_instance = null;

            base.UnloadData();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (m_updateCounter++ > 100)
            {
                m_updateCounter = 0;
                OptimizeFloatingObjects();
            }
            else
                if (m_needReupdateNewObjects)
                    OptimizeCloseDistances();

            if (VRage.Input.MyInput.Static.ENABLE_DEVELOPER_KEYS)
            {
                UpdateObjectCounters();
            }

            m_syncCounter++;

            if (m_syncCounter % 20 == 0)
            {
                if (SynchronizeObjects(m_highPriority, ref m_highIndex))
                    m_highMessagesSent++;
            }

            if (m_syncCounter % 30 == 0)
            {
                if (SynchronizeObjects(m_normalPriority, ref m_normalIndex))
                    m_normalMessagesSent++;
            }

            if (m_syncCounter % 50 == 0)
            {
                if (SynchronizeObjects(m_lowPriority, ref m_lowIndex))
                    m_lowMessagesSent++;

                SynchronizeNewObjects();
            }

            CheckObjectInVoxel();

            if (m_measurementTime.ElapsedMilliseconds > 1000)
            {
                int totalMessages = m_highMessagesSent + m_normalMessagesSent + m_lowMessagesSent;
                //int size = totalMessages * MySyncFloatingObjects.PositionUpdateCompressedMsgSize;
                int size = totalMessages * MySyncFloatingObjects.PositionUpdateMsgSize;
                m_kilobytesPerSecond = (size / (m_measurementTime.ElapsedMilliseconds / 1000.0f)) / 1024;
                m_measurementTime.Restart();

                //MyTrace.Send(TraceWindow.MultiplayerFiltered, "");
                //MyTrace.Send(TraceWindow.MultiplayerFiltered, "High messages /s: " + m_highMessagesSent.ToString());
                //MyTrace.Send(TraceWindow.MultiplayerFiltered, "Normal messages /s: " + m_normalMessagesSent.ToString());
                //MyTrace.Send(TraceWindow.MultiplayerFiltered, "Low messages /s: " + m_lowMessagesSent.ToString());
                //MyTrace.Send(TraceWindow.MultiplayerFiltered, "Total messages /s: " + totalMessages.ToString());
                //MyTrace.Send(TraceWindow.MultiplayerFiltered, "Floating objects [KB/s]: " + m_kilobytesPerSecond.ToString());

                m_highMessagesSent = 0;
                m_normalMessagesSent = 0;
                m_lowMessagesSent = 0;
            }

            if (m_itemsToSpawnNextUpdate.Count > 0)
            {
                SpawnInventoryItems();
            }

            if (!Sync.IsServer && MyPerGameSettings.EnableFloatingObjectsActiveSync)
            {
                if (m_stableDirty)
                {
                    m_stableDirty = false;
                    foreach (var pair in m_stableObjectsClient)
                    {
                        var floatingObj = pair.Key;
                        floatingObj.Physics.LinearVelocity = Vector3D.Zero;
                        floatingObj.Physics.AngularVelocity = Vector3D.Zero;
                        floatingObj.WorldMatrix = pair.Value.PositionAndOr.GetMatrix();
                    }
                }

                if (MySession.LocalCharacter != null && ((--m_requestDelay) <= 0 || m_requestedEntities.Count == 0))
                {
                    Vector3D characterPos = MySession.LocalCharacter.WorldMatrix.Translation;
                    double checkDist = 35;
                    double enterDistSq = 20 * 20;
                    m_requestDelay = 30; // 2 per sec
                    m_requestedEntities.Clear();

                    var manipulatedEntity = MySession.LocalCharacter.ManipulatedEntity;
                    if (manipulatedEntity != null && manipulatedEntity is MyFloatingObject)
                        m_stableObjectsClient.Remove(manipulatedEntity as MyFloatingObject);

                    BoundingSphereD sphere = new BoundingSphereD(characterPos, checkDist);
                    var entities = MyEntities.GetTopMostEntitiesInSphere(ref sphere);
                    foreach (var entity in entities)
                    {
                        var fo = entity as MyFloatingObject;
                        if (fo == null)
                            continue;
                        if (fo.MarkedForClose || fo.Physics == null || !fo.Physics.Enabled)
                            continue;
                        if (fo == manipulatedEntity)
                            continue;

                        if (Vector3D.DistanceSquared(characterPos, fo.PositionComp.GetPosition()) <= enterDistSq)
                        {
                            m_tmpEntities.Add(fo.EntityId);
                            m_requestedEntities.Add(fo.EntityId);
                        }
                        if (m_stableObjectsClient.ContainsKey(fo))
                            m_tmpObjects.Add(fo, m_stableObjectsClient[fo]);
                    }
                    entities.Clear();

                    SyncObject.SendMakeStableBatchReq(m_tmpEntities);
                    m_tmpEntities.Clear();

                    var swap = m_stableObjectsClient;
                    m_stableObjectsClient = m_tmpObjects;
                    m_tmpObjects = swap;
                    m_tmpObjects.Clear();
      
                    if (m_requestedEntities.Count == 0)
                    {
                        m_noRequestedCounter++;
                        if (m_noRequestedCounter % 400 == 0)
                        {
                            m_stableDirty = true;
                        }
                    }
                    else
                    {
                        m_noRequestedCounter = 0;
                    }
                }
            }
        }

        private void UpdateObjectCounters()
        {
            VRageRender.MyPerformanceCounter.PerCameraDrawRead.CustomCounters["Floating Ores"] = (float)MyFloatingObjects.FloatingOreCount;
            VRageRender.MyPerformanceCounter.PerCameraDrawRead.CustomCounters["Floating Items"] = (float)MyFloatingObjects.FloatingItemCount;
            VRageRender.MyPerformanceCounter.PerCameraDrawWrite.CustomCounters["Floating Ores"] = (float)MyFloatingObjects.FloatingOreCount;
            VRageRender.MyPerformanceCounter.PerCameraDrawWrite.CustomCounters["Floating Items"] = (float)MyFloatingObjects.FloatingItemCount;
        }

        void OptimizeFloatingObjects()
        {
            ReduceFloatingObjects();

            OptimizeCloseDistances();

            OptimizeQualityType();

            if (MyPerGameSettings.EnableFloatingObjectsActiveSync && Sync.IsServer)
                OptimizeStableObjects();    
        }

        private void OptimizeCloseDistances()
        {
            UpdateClosestDistancesToPlayers();
            m_synchronizedFloatingObjects.Sort(m_synchronizationComparer);

            m_highPriority.Clear();
            m_normalPriority.Clear();
            m_lowPriority.Clear();
            m_futureStableObjectsServer.Clear();
            m_removedStableObjectsServer.Clear();
            m_needReupdateNewObjects = false;

            float CLOSEST_DISTANCE = 4 * 4;
            float CLOSE_DISTANCE = 16 * 16;
            int HIGH_LIMIT = 32;
            float NORMAL_DISTANCE = 64 * 64;
            int NORMAL_LIMIT = 128;
            float epsilonSq = 0.05f * 0.05f;
            float lowerEpsilonSq = 0.0005f * 0.0005f;

            for (int i = 0; i < m_synchronizedFloatingObjects.Count; i++)
            {
                var syncObject = m_synchronizedFloatingObjects[i];

                m_needReupdateNewObjects |= syncObject.ClosestDistanceToAnyPlayerSquared == -1 || (syncObject.ClosestDistanceToAnyPlayerSquared < CLOSEST_DISTANCE && syncObject.SyncWaitCounter > 5);
                var linearVelocitySq = syncObject.Physics.LinearVelocity.LengthSquared();
                var angularVelocitySq = syncObject.Physics.AngularVelocity.LengthSquared();
                if (syncObject.ClosestDistanceToAnyPlayerSquared == -1 ||
                    linearVelocitySq > epsilonSq ||
                    angularVelocitySq > epsilonSq)
                {
                    if ((syncObject.ClosestDistanceToAnyPlayerSquared < CLOSE_DISTANCE) && (i < HIGH_LIMIT))
                        m_highPriority.Add(syncObject);
                    else if ((syncObject.ClosestDistanceToAnyPlayerSquared < NORMAL_DISTANCE) && (i < NORMAL_LIMIT))
                        m_normalPriority.Add(syncObject);
                    else
                        m_lowPriority.Add(syncObject);

                    m_removedStableObjectsServer.Add(syncObject);
                }
                else
                {
                    if (linearVelocitySq > lowerEpsilonSq || angularVelocitySq > lowerEpsilonSq)
                    { // if it is below the sync value but still moving, then resend
                        m_stableObjectsServer.Remove(syncObject);
                    }
                    //syncObject.SyncWaitCounter++;
                    m_futureStableObjectsServer.Add(syncObject);
                }
            }
        }

        void OptimizeStableObjects()
        {
            foreach (var removedObject in m_removedStableObjectsServer)
            {
                m_tmpEntities.Add(removedObject.EntityId);
            }
            SyncObject.SendMakeUnstable(m_tmpEntities);
            m_tmpEntities.Clear();
            m_stableObjectsServer.ExceptWith(m_removedStableObjectsServer);
            m_stableObjectsServer.UnionWith(m_futureStableObjectsServer);
        }

        void CheckObjectInVoxel()
        {
            if (m_checkObjectInsideVoxel >= m_synchronizedFloatingObjects.Count)
                m_checkObjectInsideVoxel = 0;

            if (m_synchronizedFloatingObjects.Count > 0)
            {
                var floatingObjectToCheck = m_synchronizedFloatingObjects[m_checkObjectInsideVoxel];

                var localAabb = (BoundingBoxD)floatingObjectToCheck.PositionComp.LocalAABB;
                var worldMatrix = floatingObjectToCheck.PositionComp.WorldMatrix;
                var worldAabb = floatingObjectToCheck.PositionComp.WorldAABB;

                using (m_tmpResultList.GetClearToken())
                {
                    MyGamePruningStructure.GetAllVoxelMapsInBox(ref worldAabb, m_tmpResultList);
                    //Debug.Assert(m_tmpResultList.Count == 1, "Voxel map AABBs shouldn't overlap!");
                    foreach (var voxelMap in m_tmpResultList)
                    {
                        if (voxelMap != null && !voxelMap.MarkedForClose)
                        {
                            if (voxelMap.AreAllAabbCornersInside(ref worldMatrix, localAabb))
                            {
                                floatingObjectToCheck.NumberOfFramesInsideVoxel++;

                                if (floatingObjectToCheck.NumberOfFramesInsideVoxel > MyFloatingObject.NUMBER_OF_FRAMES_INSIDE_VOXEL_TO_REMOVE)
                                {
                                    //MyLog.Default.WriteLine("Floating object " + (floatingObjectToCheck.DisplayName != null ? floatingObjectToCheck.DisplayName : floatingObjectToCheck.ToString()) + " was removed because it was inside voxel.");
                                    if (Sync.IsServer)
                                        RemoveFloatingObject(floatingObjectToCheck);
                                }
                            }
                            else
                            {
                                floatingObjectToCheck.NumberOfFramesInsideVoxel = 0;
                            }
                        }
                    }
                }
            }

            m_checkObjectInsideVoxel++;

        }


        /// <summary>
        /// Spawning of inventory items is delayed to UpdateAfterSimulation
        /// </summary>
        private void SpawnInventoryItems()
        {
            for (int i = 0; i < Math.Min(m_itemsToSpawnNextUpdate.Count,1); ++i)
            {
                var item = m_itemsToSpawnNextUpdate[0];
                m_itemsToSpawnNextUpdate.RemoveAt(0);

                var entity = item.Item1.Spawn(item.Item1.Amount, item.Item2);
                if (entity != null)
                {
                    entity.Physics.LinearVelocity = item.Item3;
                    entity.Physics.ApplyImpulse(MyUtils.GetRandomVector3Normalized() * entity.Physics.Mass / 5.0f, entity.PositionComp.GetPosition());
                }
            }
        }

        #region Spawning

        public static MyEntity Spawn(MyPhysicalInventoryItem item, Vector3D position, Vector3D forward, Vector3D up, MyPhysicsComponentBase motionInheritedFrom = null)
        {
            var orientedForward = forward;
            var orientedUp = up;

            var left = Vector3D.Cross(up, forward);

            MyPhysicalItemDefinition itemDefinition = null;            

            if (MyDefinitionManager.Static.TryGetDefinition<MyPhysicalItemDefinition>(item.Content.GetObjectId(), out itemDefinition))
            {
                if (itemDefinition.RotateOnSpawnX)
                {
                    orientedForward = up;
                    orientedUp = -forward;
                }
                if (itemDefinition.RotateOnSpawnY)
                {
                    orientedForward = left;
                }
                if (itemDefinition.RotateOnSpawnZ)
                {
                    orientedUp = -left;
                }
            }

            return Spawn(item, MatrixD.CreateWorld(position, orientedForward, orientedUp), motionInheritedFrom);
        }

        public static MyEntity Spawn(MyPhysicalInventoryItem item, MatrixD worldMatrix, MyPhysicsComponentBase motionInheritedFrom = null)
        {
            var floatingBuilder = PrepareBuilder(ref item);

            floatingBuilder.PositionAndOrientation = new MyPositionAndOrientation(worldMatrix);
            var thrownEntity = MyEntities.CreateFromObjectBuilderAndAdd(floatingBuilder);
            thrownEntity.Physics.ForceActivate();
            ApplyPhysics(thrownEntity, motionInheritedFrom);
            Debug.Assert(thrownEntity.Save == true, "Thrown item will not be saved. Feel free to ignore this.");
            return thrownEntity;
        }

        internal static MyEntity Spawn(MyPhysicalInventoryItem item, BoundingBoxD box, MyPhysicsComponentBase motionInheritedFrom = null)
        {
            var floatingBuilder = PrepareBuilder(ref item);
            var thrownEntity = MyEntities.CreateFromObjectBuilder(floatingBuilder);
            System.Diagnostics.Debug.Assert(thrownEntity != null);
            if (thrownEntity != null)
            {
                var size = thrownEntity.PositionComp.LocalVolume.Radius;
                var halfSize = box.Size / 2 - new Vector3(size);
                halfSize = Vector3.Max(halfSize, Vector3.Zero);

                box = new BoundingBoxD(box.Center - halfSize, box.Center + halfSize);
                var pos = MyUtils.GetRandomPosition(ref box);

                AddToPos(thrownEntity, pos, motionInheritedFrom);

                thrownEntity.Physics.ForceActivate();
            }
            return thrownEntity;
        }

        public static MyEntity Spawn(MyPhysicalInventoryItem item, BoundingSphereD sphere, MyPhysicsComponentBase motionInheritedFrom = null, MyVoxelMaterialDefinition voxelMaterial = null)
        {
            ProfilerShort.Begin("MyFloatingObjects.Spawn");
            var floatingBuilder = PrepareBuilder(ref item);
            ProfilerShort.Begin("Create");
            var thrownEntity = MyEntities.CreateFromObjectBuilder(floatingBuilder);
            ProfilerShort.End();
            ((MyFloatingObject)thrownEntity).VoxelMaterial = voxelMaterial;

            var size = thrownEntity.PositionComp.LocalVolume.Radius;
            var sphereSize = sphere.Radius - size;
            sphereSize = Math.Max(sphereSize, 0);

            sphere = new BoundingSphereD(sphere.Center, sphereSize);

            var pos = MyUtils.GetRandomBorderPosition(ref sphere);
            AddToPos(thrownEntity, pos, motionInheritedFrom);
            ProfilerShort.End();
            return thrownEntity;
        }

        public static MyEntity Spawn(MyPhysicalItemDefinition itemDefinition, Vector3D translation, Vector3D forward, Vector3D up, int amount = 1)
        {
            var objectBuilder = MyObjectBuilderSerializer.CreateNewObject(itemDefinition.Id.TypeId, itemDefinition.Id.SubtypeName) as MyObjectBuilder_PhysicalObject;

            var floatingObj = MyFloatingObjects.Spawn(
                new MyPhysicalInventoryItem((MyFixedPoint)amount, objectBuilder),
                translation,
                forward,
                up);

            return floatingObj;
        }

        public static void EnqueueInventoryItemSpawn(MyPhysicalInventoryItem inventoryItem, BoundingBoxD boundingBox, Vector3D inheritedVelocity)
        {
			m_itemsToSpawnNextUpdate.Add(Tuple.Create(inventoryItem, boundingBox, inheritedVelocity));
        }

        private static MyObjectBuilder_FloatingObject PrepareBuilder(ref MyPhysicalInventoryItem item)
        {
            Debug.Assert(item.Amount > 0, "FloatObject item amount must be > 0");

            var floatingBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_FloatingObject>();
            floatingBuilder.Item = item.GetObjectBuilder();
            floatingBuilder.PersistentFlags |= MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;
            return floatingBuilder;
        }

        private static void AddToPos(MyEntity thrownEntity, Vector3D pos, MyPhysicsComponentBase motionInheritedFrom)
        {
            ProfilerShort.Begin("AddToPos");
            ProfilerShort.Begin("GetPos");
            Vector3 forward = MyUtils.GetRandomVector3Normalized();
            Vector3 up = MyUtils.GetRandomVector3Normalized();
            while (forward == up)
                up = MyUtils.GetRandomVector3Normalized();

            Vector3 right = Vector3.Cross(forward, up);
            up = Vector3.Cross(right, forward);

            thrownEntity.WorldMatrix = MatrixD.CreateWorld(pos, forward, up);
            ProfilerShort.End();
            ProfilerShort.Begin("MyEntities.Add");
            MyEntities.Add(thrownEntity);
            ProfilerShort.BeginNextBlock("ApplyPhysics");
            ApplyPhysics(thrownEntity, motionInheritedFrom);
            ProfilerShort.End();
            ProfilerShort.End();
        }

        private static void ApplyPhysics(MyEntity thrownEntity, MyPhysicsComponentBase motionInheritedFrom)
        {
            if (thrownEntity.Physics != null && motionInheritedFrom != null)
            {
                thrownEntity.Physics.LinearVelocity = motionInheritedFrom.LinearVelocity;
                thrownEntity.Physics.AngularVelocity = motionInheritedFrom.AngularVelocity;
            }
        }

        void OptimizeQualityType()
        {
            for (int i = 0; i < m_synchronizedFloatingObjects.Count; i++)
            {
                var floatingObject = m_synchronizedFloatingObjects[i];
                if (floatingObject.Physics.LinearVelocity.Length() > 5)
                {
                    floatingObject.Physics.ChangeQualityType(Havok.HkCollidableQualityType.Bullet);
                }
                else
                {
                    floatingObject.Physics.ChangeQualityType(Havok.HkCollidableQualityType.Debris);
                }
            }
        }


        #endregion

        #region Floating object reduction

        public static int FloatingOreCount
        {
            get { return m_floatingOres.Count; }
        }

        public static int FloatingItemCount
        {
            get { return m_floatingItems.Count; }
        }

        internal static void RegisterFloatingObject(MyFloatingObject obj)
        {
            Debug.Assert(obj != null && obj.Item.Amount > 0, "Object cannot be null and amount must be > 0");

            if (obj.WasRemovedFromWorld)
            {
                return;
            }
            obj.CreationTime = Stopwatch.GetTimestamp();

            if (obj.VoxelMaterial != null)
                m_floatingOres.Add(obj);
            else
                m_floatingItems.Add(obj);

            if (Sync.IsServer)
            {
                MyFloatingObjects.AddToSynchronization(obj);
            }
        }

        internal static void UnregisterFloatingObject(MyFloatingObject obj)
        {
            if (obj.VoxelMaterial != null)
                m_floatingOres.Remove(obj);
            else
                m_floatingItems.Remove(obj);

            if (Sync.IsServer)
            {
                MyFloatingObjects.RemoveFromSynchronization(obj);
            }
            obj.WasRemovedFromWorld = true;
        }

        public static void AddFloatingObjectAmount(MyFloatingObject obj, MyFixedPoint amount)
        {
            var item = obj.Item;
            item.Amount += amount;
            obj.Item = item;

            obj.UpdateInternalState();

            if (Sync.IsServer)
                SyncObject.SendAddFloatingObject(obj, amount);
        }

        public static void RemoveFloatingObject(MyFloatingObject obj, bool sync)
        {
            if (sync)
            {
                if (Sync.IsServer)
                    RemoveFloatingObject(obj);
                else
                    SyncObject.SendRemoveFloatingObjectRequest(obj, MyFixedPoint.MaxValue);
            }
            else
            {
                RemoveFloatingObject(obj);
            }
        }

        public static void RemoveFloatingObject(MyFloatingObject obj)
        {
            RemoveFloatingObject(obj, MyFixedPoint.MaxValue);
        }

        /// <param name="amount">MyFixedPoint.MaxValue to remove object</param>
        internal static void RemoveFloatingObject(MyFloatingObject obj, MyFixedPoint amount)
        {
            if (amount <= 0)
            {
                Debug.Fail("RemoveFloatingObject, amount must be > 0");
                return;
            }

            if (amount < obj.Item.Amount)
            {
                obj.Item.Amount -= amount;
                obj.RefreshDisplayName();
            }
            else
                obj.Close();

            obj.WasRemovedFromWorld = true;

            if (Sync.IsServer)
                SyncObject.SendRemoveFloatingObjectSuccess(obj, amount);

            m_requestedEntities.Remove(obj.EntityId);
            m_stableObjectsClient.Remove(obj);
            m_stableObjectsServer.Remove(obj);
        }


        public static void ReduceFloatingObjects()
        {
            var count = m_floatingOres.Count + m_floatingItems.Count;
            int minFloatingOres = Math.Max(MySession.Static.MaxFloatingObjects / 5, 4);
            while (count > MySession.Static.MaxFloatingObjects)
            {
                SortedSet<MyFloatingObject> set;
                if (m_floatingOres.Count > minFloatingOres || m_floatingItems.Count == 0)
                    set = m_floatingOres;
                else
                    set = m_floatingItems;
                if (set.Count > 0)
                {
                    var floatingObject = set.Last();
                    set.Remove(floatingObject);
                    if (Sync.IsServer)
                        RemoveFloatingObject(floatingObject);
                }
                --count;
            }
        }

        #endregion

        #region Synchronization

        static void AddToSynchronization(MyFloatingObject floatingObject)
        {
            Debug.Assert(floatingObject.Item.Amount > 0, "Floating object item amount must be > 0");

            //SyncObject.OnCreateFloatingObject(floatingObject);
            m_floatingObjectsToSyncCreate.Add(floatingObject);

            m_synchronizedFloatingObjects.Add(floatingObject);

            floatingObject.OnClose += floatingObject_OnClose;
            m_needReupdateNewObjects = true;
        }

        static void floatingObject_OnClose(MyEntity obj)
        {
            var floating = obj as MyFloatingObject;
            System.Diagnostics.Debug.Assert(!m_synchronizedFloatingObjects.Contains(floating), "Must be already removed in RemoveFromSynchronization");
            System.Diagnostics.Debug.Assert(!m_floatingObjectsToSyncCreate.Contains(floating), "Must be already removed in RemoveFromSynchronization");
        }

        static void RemoveFromSynchronization(MyFloatingObject floatingObject)
        {
            floatingObject.OnClose -= floatingObject_OnClose;
            m_synchronizedFloatingObjects.Remove(floatingObject);
            m_floatingObjectsToSyncCreate.Remove(floatingObject);
            m_highPriority.Remove(floatingObject);
            m_normalPriority.Remove(floatingObject);
            m_lowPriority.Remove(floatingObject);
        }

        void UpdateClosestDistancesToPlayers()
        {
            foreach (var floatingObject in m_synchronizedFloatingObjects)
            {
                if (floatingObject.ClosestDistanceToAnyPlayerSquared == -1)
                    continue;

                floatingObject.ClosestDistanceToAnyPlayerSquared = float.MaxValue;

                foreach (var controller in Sync.Players.GetOnlinePlayers())
                {
                    if (controller.Identity.Character != null)
                    {
                        float distanceSq = (float)Vector3D.DistanceSquared(floatingObject.PositionComp.GetPosition(), ((MyEntity)controller.Identity.Character).PositionComp.GetPosition());

                        if (distanceSq < floatingObject.ClosestDistanceToAnyPlayerSquared)
                        {
                            floatingObject.ClosestDistanceToAnyPlayerSquared = distanceSq;
                        }
                    }
                }
            }
        }

        public static bool SynchronizeObjects(List<MyFloatingObject> objects, ref int index)
        {
            if (objects.Count == 0)
                return false;

            if (index >= objects.Count)
                index = 0;

            var floatingObject = objects[index];
            MySyncFloatingObjects.UpdatePosition(floatingObject);

            if (floatingObject.ClosestDistanceToAnyPlayerSquared == -1)
                floatingObject.ClosestDistanceToAnyPlayerSquared = 0;

            floatingObject.SyncWaitCounter = 0;

            index++;

            return true;
        }


        void SynchronizeNewObjects()
        {
            SynchronizeNewObjects(m_floatingObjectsToSyncCreate);
            m_floatingObjectsToSyncCreate.Clear();
        }

        public static void SynchronizeNewObjects(List<MyFloatingObject> objects)
        {
            if (SyncObject != null)
            {
                SyncObject.OnCreateFloatingObjects(objects);
            }
        }

        #endregion

        #region Stability

        public void MakeStable(List<MySyncFloatingObjects.MakeStableEntityData> data)
        {
            var requestedCount = m_requestedEntities.Count;
            MyFloatingObject floatingObj;
            foreach (var ins in data)
            {
                if (!MyEntities.TryGetEntityById(ins.EntityId, out floatingObj))
                    continue;
                m_stableObjectsClient[floatingObj] = new StabilityInfo(ins.GetPositionAndOrientation());
                m_requestedEntities.Remove(ins.EntityId);
            }           
            var afterRemoveCount = m_requestedEntities.Count;
            if (afterRemoveCount == 0 && requestedCount != afterRemoveCount)
            {
                m_stableDirty = true;
            }
        }

        public void MakeStableFailed(List<long> entities)
        {
            foreach (var id in entities)
                m_requestedEntities.Remove(id);
        }

        public void MakeUnstable(List<long> entities)
        {
            MyFloatingObject floatingObj;
            foreach (var entityId in entities)
            {
                if (!MyEntities.TryGetEntityById(entityId, out floatingObj))
                    continue;
                m_requestedEntities.Remove(entityId);
                m_stableObjectsClient.Remove(floatingObj);
            }
        }

        public void ProcessStableBatchReq(List<long> entityIds, MyCharacter playerCharacter, List<MySyncFloatingObjects.MakeStableEntityData> outStable, List<long> outNonStable)
        {
            var sqDist = 50 * 50;
            MyFloatingObject fo;
            var characterPos = playerCharacter.PositionComp.GetPosition();
            foreach (var id in entityIds)
            {
                if (!MyEntities.TryGetEntityById(id, out fo) || !m_stableObjectsServer.Contains(fo) || Vector3D.DistanceSquared(fo.WorldMatrix.Translation, characterPos) > sqDist)
                {
                    outNonStable.Add(id);
                }
                else
                {
                    var newData = new MySyncFloatingObjects.MakeStableEntityData();
                    newData.EntityId = fo.EntityId;
                    newData.Position = fo.WorldMatrix.Translation;
                    newData.Forward = fo.WorldMatrix.Forward;
                    newData.Up = fo.WorldMatrix.Up;
                    outStable.Add(newData);
                }
            }
        }

        #endregion

        /// <summary>
        /// This is used mainly for compactibility issues, it takes the builder of an entity of old object representation and creates a floating object builder for it
        /// </summary>
        public static MyObjectBuilder_FloatingObject ChangeObjectBuilder(MyComponentDefinition componentDef, MyObjectBuilder_EntityBase entityOb)
        {
            var componentBuilder = MyObjectBuilderSerializer.CreateNewObject(componentDef.Id.TypeId, componentDef.Id.SubtypeName) as MyObjectBuilder_PhysicalObject;

            Vector3 up = entityOb.PositionAndOrientation.Value.Up;
            Vector3 forward = entityOb.PositionAndOrientation.Value.Forward;
            Vector3D position = entityOb.PositionAndOrientation.Value.Position;

            var item = new MyPhysicalInventoryItem((MyFixedPoint)1, componentBuilder);
            var floatingBuilder = PrepareBuilder(ref item);

            floatingBuilder.PositionAndOrientation = new MyPositionAndOrientation(position, forward, up);
            floatingBuilder.EntityId = entityOb.EntityId;

            return floatingBuilder;
        }

        // sends message to server to 
        internal static void RequestSync(MyFloatingObject myFloatingObject)
        {
            MySyncFloatingObjects.RequestPositionFromServer(myFloatingObject);
        }
    }
}
