#region Using

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


#endregion

namespace Sandbox.Game.Entities
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, 800)]
    public class MyFloatingObjects : MySessionComponentBase
    {
        #region Comparer

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


        #region Fields

        private static MyFloatingObjects m_instance;

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

        static List<Tuple<MyPhysicalInventoryItem, BoundingBoxD>> m_itemsToSpawnNextUpdate = new List<Tuple<MyPhysicalInventoryItem, BoundingBoxD>>();

        #endregion

        public override void LoadData()
        {
            base.LoadData();

            Debug.Assert(m_instance == null);
            m_instance = this;

            SyncObject = new MySyncFloatingObjects();
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
        }

        private void OptimizeCloseDistances()
        {
            UpdateClosestDistancesToPlayers();
            m_synchronizedFloatingObjects.Sort(m_synchronizationComparer);

            m_highPriority.Clear();
            m_normalPriority.Clear();
            m_lowPriority.Clear();
            m_needReupdateNewObjects = false;

            float HIGH_DISTANCE = 16 * 16;
            int HIGH_LIMIT = 32;
            float NORMAL_DISTANCE = 64 * 64;
            int NORMAL_LIMIT = 128;
            float epsilonSq = 0.05f * 0.05f;

            for (int i = 0; i < m_synchronizedFloatingObjects.Count; i++)
            {
                var syncObject = m_synchronizedFloatingObjects[i];

                m_needReupdateNewObjects |= syncObject.ClosestDistanceToAnyPlayerSquared == -1;

                if (syncObject.ClosestDistanceToAnyPlayerSquared == -1 ||
                syncObject.Physics.LinearVelocity.LengthSquared() > epsilonSq ||
                syncObject.Physics.AngularVelocity.LengthSquared() > epsilonSq)
                {
                    if ((syncObject.ClosestDistanceToAnyPlayerSquared < HIGH_DISTANCE) && (i < HIGH_LIMIT))
                        m_highPriority.Add(syncObject);
                    else if ((syncObject.ClosestDistanceToAnyPlayerSquared < NORMAL_DISTANCE) && (i < NORMAL_LIMIT))
                        m_normalPriority.Add(syncObject);
                    else
                        m_lowPriority.Add(syncObject);
                }
            }
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
                        if (voxelMap != null && !voxelMap.MarkedForClose && !(voxelMap is MyVoxelPhysics))
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
            foreach (var item in m_itemsToSpawnNextUpdate)
            {
                var entity = item.Item1.Spawn(item.Item1.Amount, item.Item2);
                entity.Physics.ApplyImpulse(MyUtils.GetRandomVector3Normalized() * entity.Physics.Mass / 5.0f, entity.PositionComp.GetPosition());
            }

            m_itemsToSpawnNextUpdate.Clear();
        }

        #region Spawning
        public static MyEntity Spawn(MyPhysicalInventoryItem item, Vector3D position, Vector3D forward, Vector3D up, MyPhysicsComponentBase motionInheritedFrom = null)
        {
            return Spawn(item, MatrixD.CreateWorld(position, forward, up), motionInheritedFrom);
        }

        internal static MyEntity Spawn(MyPhysicalInventoryItem item, MatrixD worldMatrix, MyPhysicsComponentBase motionInheritedFrom = null)
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

        public static void EnqueueInventoryItemSpawn(MyPhysicalInventoryItem inventoryItem, BoundingBoxD boundingBox)
        {
            m_itemsToSpawnNextUpdate.Add(Tuple.Create(inventoryItem, boundingBox));
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

        internal static void RemoveFloatingObject(MyFloatingObject obj)
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
                SyncObject.OnRemoveFloatingObject(obj, amount);
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

            m_needReupdateNewObjects = true;
        }

        static void RemoveFromSynchronization(MyFloatingObject floatingObject)
        {
            m_synchronizedFloatingObjects.Remove(floatingObject);
            m_floatingObjectsToSyncCreate.Remove(floatingObject);
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

        bool SynchronizeObjects(List<MyFloatingObject> objects, ref int index)
        {
            if (objects.Count == 0)
                return false;

            if (index >= objects.Count)
                index = 0;

            var floatingObject = objects[index];
            SyncObject.UpdatePosition(floatingObject);

            if (floatingObject.ClosestDistanceToAnyPlayerSquared == -1)
                floatingObject.ClosestDistanceToAnyPlayerSquared = 0;

            index++;

            return true;
        }

        void SynchronizeNewObjects()
        {
            SyncObject.OnCreateFloatingObjects(m_floatingObjectsToSyncCreate);
            m_floatingObjectsToSyncCreate.Clear();
        }

        #endregion
    }
}
