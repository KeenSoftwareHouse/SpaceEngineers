﻿#region Using

using Havok;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using VRage.Groups;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using VRage;
using VRage.Collections;
using VRage.Plugins;
using VRageMath;
using VRageRender;
using VRage;
using Sandbox.ModAPI;
using Sandbox.Game.Weapons;
using VRage.Win32;
using VRage.Utils;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Components;
using VRage.Game.Components;
using System.Text;
using Sandbox.Game.Components;
using ParallelTasks;

#endregion

namespace Sandbox.Game.Entities
{
    public static class MyEntities
    {
        static readonly long EntityNativeMemoryLimit = 1300 * 1024 * 1024;
        static readonly long EntityManagedMemoryLimit = 650 * 1024 * 1024;

        #region Fields

        // List of physic objects in the world
        static HashSet<MyEntity> m_entities = new HashSet<MyEntity>();

        //Entities updated once before next frame
        static CachingList<MyEntity> m_entitiesForUpdateOnce = new CachingList<MyEntity>();

        //Entities updated each frame
        static CachingList<MyEntity> m_entitiesForUpdate = new CachingList<MyEntity>();

        //Entities updated each 10th frame
        static CachingList<MyEntity> m_entitiesForUpdate10 = new CachingList<MyEntity>();

        //Entities updated each 100th frame
        static CachingList<MyEntity> m_entitiesForUpdate100 = new CachingList<MyEntity>();

        //Entities drawn each frame
        static List<IMyEntity> m_entitiesForDraw = new List<IMyEntity>();
        static List<IMyEntity> m_entitiesForDrawToAdd = new List<IMyEntity>();

        // Scene data components
        static List<IMySceneComponent> m_sceneComponents = new List<IMySceneComponent>();

        // Helper for remapping of entityIds to new values
        static MyEntityIdRemapHelper m_remapHelper = new MyEntityIdRemapHelper();

        // Count of objects editable in editor
        readonly static int MAX_ENTITIES_CLOSE_PER_UPDATE = 10;

        // Event called when entity is removed from scene
        public static event Action<MyEntity> OnEntityRemove;
        public static event Action<MyEntity> OnEntityAdd;

        public static event Action<MyEntity> OnEntityCreate;
        public static event Action<MyEntity> OnEntityDelete;

        public static event Action OnCloseAll;
        public static event Action<MyEntity, string, string> OnEntityNameSet;

        public static bool IgnoreMemoryLimits = false;

        #endregion

        static MyEntities()
        {
            MyEntityFactory.RegisterDescriptorsFromAssembly(Assembly.GetCallingAssembly());
            MyEntityFactory.RegisterDescriptorsFromAssembly(MyPlugins.GameAssembly);
            MyEntityFactory.RegisterDescriptorsFromAssembly(MyPlugins.SandboxAssembly);
            MyEntityFactory.RegisterDescriptorsFromAssembly(MyPlugins.UserAssembly);
        }

        static MyEntityCreationThread m_creationThread;
        static Dictionary<uint, IMyEntity> m_renderObjectToEntityMap = new Dictionary<uint, IMyEntity>();
        static FastResourceLock m_renderObjectToEntityMapLock = new FastResourceLock();
        public static void AddRenderObjectToMap(uint id, IMyEntity entity)
        {
            if (!MySandboxGame.IsDedicated)
            {
                using (m_renderObjectToEntityMapLock.AcquireExclusiveUsing())
                {
                    m_renderObjectToEntityMap.Add(id, entity);
                }
            }
        }
        public static void RemoveRenderObjectFromMap(uint id)
        {
            if (!MySandboxGame.IsDedicated)
            {
                using (m_renderObjectToEntityMapLock.AcquireExclusiveUsing())
                {
                    m_renderObjectToEntityMap.Remove(id);
                }
            }
        }

        [ThreadStatic]
        static List<MyEntity> m_overlapRBElementList;
        static List<List<MyEntity>> m_overlapRBElementListCollection = new List<List<MyEntity>>();

        static List<HkBodyCollision> m_rigidBodyList = new List<HkBodyCollision>();

        static List<MyLineSegmentOverlapResult<MyEntity>> LineOverlapEntityList = new List<MyLineSegmentOverlapResult<MyEntity>>();
        static List<MyEntity> OverlapRBElementList
        {
            get
            {
                if (m_overlapRBElementList == null)
                {
                    m_overlapRBElementList = new List<MyEntity>(256);
                    lock (m_overlapRBElementListCollection)
                    {
                        m_overlapRBElementListCollection.Add(m_overlapRBElementList);
                    }
                }
                return m_overlapRBElementList;
            }
        }


        public static bool IsShapePenetrating(HkShape shape, ref Vector3D position, ref Quaternion rotation, int filter = MyPhysics.DefaultCollisionLayer)
        {
            try
            {
                MyPhysics.GetPenetrationsShape(shape, ref position, ref rotation, m_rigidBodyList, filter);
                return m_rigidBodyList.Count > 0;
            }
            finally
            {
                m_rigidBodyList.Clear();
            }
        }


        public static bool IsSpherePenetrating(ref BoundingSphereD bs)
        {
            bool isPenetrating = MyEntities.IsShapePenetrating(m_cameraSphere, ref bs.Center, ref Quaternion.Identity);

            return isPenetrating;
        }

        /// <summary>
        /// Finds free place for objects defined by position and radius.
        /// StepSize is how fast to increase radius, 0.5f means by half radius
        /// </summary>
        public static Vector3D? FindFreePlace(Vector3D basePos, float radius, int maxTestCount = 20, int testsPerDistance = 5, float stepSize = 1)
        {
            Vector3D currentPos = basePos;
            Quaternion rot = Quaternion.Identity;
            HkShape sphere = new HkSphereShape(radius);
            try
            {
                if (MyEntities.IsInsideWorld(currentPos) && !IsShapePenetrating(sphere, ref currentPos, ref rot))
                    return basePos;

                int count = (int)Math.Ceiling(maxTestCount / (float)testsPerDistance);
                float distance = 0;
                for (int i = 0; i < count; i++)
                {
                    distance += radius * stepSize;
                    for (int j = 0; j < testsPerDistance; j++)
                    {
                        currentPos = basePos + MyUtils.GetRandomVector3Normalized() * distance;
                        if (MyEntities.IsInsideWorld(currentPos) && !IsShapePenetrating(sphere, ref currentPos, ref rot))
                        {
                            //test voxels
                            BoundingSphereD boundingSphere = new BoundingSphereD(currentPos, radius);
                            MyVoxelBase overlappedVoxelmap = MySession.Static.VoxelMaps.GetOverlappingWithSphere(ref boundingSphere);

                            if (overlappedVoxelmap == null)
                                return currentPos;
                        }
                    }
                }
                return null;
            }
            finally
            {
                sphere.RemoveReference();
            }
        }

        static List<MyPhysics.HitInfo> m_hits = new List<MyPhysics.HitInfo>();

        public static void GetInflatedPlayerBoundingBox(ref BoundingBox playerBox, float inflation)
        {
            foreach (var player in Sync.Players.GetOnlinePlayers())
            {
                playerBox.Include(player.GetPosition());
            }
            playerBox.Inflate(inflation);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos">Position of object</param>
        /// <param name="hintPosition">Position of object few frames back to test whether object enterred voxel. Usually pos - LinearVelocity * x, where x it time.</param>
        public static bool IsInsideVoxel(Vector3 pos, Vector3 hintPosition, out Vector3 lastOutsidePos)
        {
            m_hits.Clear();

            lastOutsidePos = pos;

            MyPhysics.CastRay(hintPosition, pos, m_hits);

            int voxelHits = 0;

            foreach (var hit in m_hits)
            {
                var voxel = hit.HkHitInfo.GetHitEntity() as MyVoxelMap;
                if (voxel != null)
                {
                    voxelHits++;
                    lastOutsidePos = hit.Position;
                }
            }

            m_hits.Clear();

            return (voxelHits % 2) != 0;
        }

        public static bool IsWorldLimited()
        {
            return MySession.Static != null && MySession.Static.Settings.WorldSizeKm != 0;
        }

        /// <summary>
        /// Returns shortest distance (i.e. axis-aligned) to the world border from the world center.
        /// Will be 0 if world is borderless
        /// </summary>
        public static float WorldHalfExtent()
        {
            return MySession.Static == null ? 0 : MySession.Static.Settings.WorldSizeKm * 500;
        }

        /// <summary>
        /// Returns shortest distance (i.e. axis-aligned) to the world border from the world center, minus 600m to make spawning somewhat safer.
        /// WIll be 0 if world is borderless
        /// </summary>
        public static float WorldSafeHalfExtent()
        {
            float worldHalfExtent = WorldHalfExtent();
            return worldHalfExtent == 0 ? 0 : worldHalfExtent - 600;
        }

        public static bool IsInsideWorld(Vector3D pos)
        {
            float worldHalfExtent = WorldHalfExtent();
            if (worldHalfExtent == 0) return true;

            return pos.AbsMax() <= worldHalfExtent;
        }

        public static bool IsRaycastBlocked(Vector3D pos, Vector3D target)
        {
            m_hits.Clear();

            MyPhysics.CastRay(pos, target, m_hits);

            return m_hits.Count > 0;
        }

        /// <summary>
        /// Get all rigid body elements touching a bounding box.
        /// Clear() the result list after you're done with it!
        /// </summary>
        /// <returns>The list of results.</returns>
        public static List<MyEntity> GetEntitiesInAABB(ref BoundingBox boundingBox)
        {
            MyDebug.AssertDebug(OverlapRBElementList.Count == 0, "Result buffer was not cleared after last use!");
            BoundingBoxD bbD = (BoundingBoxD)boundingBox;
            MyGamePruningStructure.GetAllEntitiesInBox(ref bbD, OverlapRBElementList);
            //return GetElementsInBox(m_pruningStructure, ref bbD);
            return OverlapRBElementList;
        }

        public static List<MyEntity> GetEntitiesInAABB(ref BoundingBoxD boundingBox)
        {
            MyDebug.AssertDebug(OverlapRBElementList.Count == 0, "Result buffer was not cleared after last use!");
            ProfilerShort.Begin("GetEntitiesInAABB");
            MyGamePruningStructure.GetAllEntitiesInBox(ref boundingBox, OverlapRBElementList);
            ProfilerShort.End();
            return OverlapRBElementList;
        }
        /// <summary>
        /// Get all rigid body elements touching a bounding sphere.
        /// Clear() the result list after you're done with it!
        /// </summary>
        /// <returns>The list of results.</returns>
        public static List<MyEntity> GetEntitiesInSphere(ref BoundingSphereD boundingSphere)
        {
            MyDebug.AssertDebug(OverlapRBElementList.Count == 0, "Result buffer was not cleared after last use!");
            MyGamePruningStructure.GetAllEntitiesInSphere(ref boundingSphere, OverlapRBElementList);
            //m_pruningStructure.OverlapAllBoundingSphere(ref boundingSphere, OverlapRBElementList);
            return OverlapRBElementList;
        }

        public static List<MyEntity> GetTopMostEntitiesInSphere(ref BoundingSphereD boundingSphere)
        {
            MyDebug.AssertDebug(OverlapRBElementList.Count == 0, "Result buffer was not cleared after last use!");
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref boundingSphere, OverlapRBElementList);
            return OverlapRBElementList;
        }

        public static void GetElementsInBox(ref BoundingBoxD boundingBox, List<MyEntity> foundElements)
        {
            MyGamePruningStructure.GetAllEntitiesInBox(ref boundingBox, foundElements);
        }

        // Helper list for storing results of various operations, mostly used in intersections
        [ThreadStatic]
        private static HashSet<IMyEntity> m_entityResultSet;
        private static List<HashSet<IMyEntity>> m_entityResultSetCollection = new List<HashSet<IMyEntity>>();
        static HashSet<IMyEntity> EntityResultSet
        {
            get
            {
                if (m_entityResultSet == null)
                {
                    m_entityResultSet = new HashSet<IMyEntity>();
                    lock (m_entityResultSetCollection)
                    {
                        m_entityResultSetCollection.Add(m_entityResultSet);
                    }
                }
                return m_entityResultSet;
            }
        }

        // Helper list for storing temporary entities
        [ThreadStatic]
        private static List<MyEntity> m_entityInputList;
        private static List<List<MyEntity>> m_entityInputListCollection = new List<List<MyEntity>>();
        static List<MyEntity> EntityInputList
        {
            get
            {
                if (m_entityInputList == null)
                {
                    m_entityInputList = new List<MyEntity>(32);
                    lock (m_entityInputListCollection)
                    {
                        m_entityInputListCollection.Add(m_entityInputList);
                    }
                }
                return m_entityInputList;
            }
        }
        /*
   [ThreadStatic]
   static List<MyLineSegmentOverlapResult<MyRBElement>> m_lineOverlapRBElementList;
   static List<List<MyLineSegmentOverlapResult<MyRBElement>>> m_lineOverlapRBElementListCollection = new List<List<MyLineSegmentOverlapResult<MyRBElement>>>();
   static List<MyLineSegmentOverlapResult<MyRBElement>> LineOverlapRBElementList
   {
       get
       {
           if (m_lineOverlapRBElementList == null)
           {
               m_lineOverlapRBElementList = new List<MyLineSegmentOverlapResult<MyRBElement>>(256);
               lock (m_lineOverlapRBElementListCollection)
               {
                   m_lineOverlapRBElementListCollection.Add(m_lineOverlapRBElementList);
               }
           }
           return m_lineOverlapRBElementList;
       }
   }
          */

        // Helper collection, entities are added with MarkForClose(entity), real remove is done with CloseRememberedEntities() which is last Update call
        static HashSet<MyEntity> m_entitiesToDelete = new HashSet<MyEntity>();
        static HashSet<MyEntity> m_entitiesToDeleteNextFrame = new HashSet<MyEntity>();
        /*
        //preallocated list of entities fileld up/cleared when testing collisions with element
        static List<MyRBElement> m_CollisionsForElementsHelper = new List<MyRBElement>();

        static public List<MyRBElement> CollisionsElements
        {
            get { return m_CollisionsForElementsHelper; }
        }
          */
        // Dictionary of entities where entity name is key
        static public Dictionary<string, MyEntity> m_entityNameDictionary = new Dictionary<string, MyEntity>();

        static bool m_isLoaded = false;
        public static bool IsLoaded
        {
            get { return m_isLoaded; }
        }

        //  Common functionality is provided by this class for phys objects, however, some of them are not JLX and we need to reuse that functionality
        //  and keep it at one place, for that reason, influence spheres are managed by this class.
        static Havok.HkShape m_cameraSphere;

        // Later we can add some register/unregister
        private static void AddComponents()
        {
            m_sceneComponents.Add(new MyCubeGridGroups());
            m_sceneComponents.Add(new MyWeldingGroups());
        }

        public static void LoadData()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyEntities.LoadData");

            m_entities.Clear();
            m_entitiesToDelete.Clear();
            m_entitiesToDeleteNextFrame.Clear();

            m_cameraSphere = new Havok.HkSphereShape(MyThirdPersonSpectator.CAMERA_RADIUS);

            AddComponents();
            foreach (var component in m_sceneComponents)
            {
                component.Load();
            }

            m_creationThread = new MyEntityCreationThread();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            m_isLoaded = true;
        }

        public static void UnloadData()
        {
            if (m_isLoaded)
            {
                m_cameraSphere.RemoveReference();
            }

            using (UnloadDataLock.AcquireExclusiveUsing())
            {
                m_creationThread.Dispose();
                m_creationThread = null;

                CloseAll();

                System.Diagnostics.Debug.Assert(m_entities.Count == 0);
                System.Diagnostics.Debug.Assert(m_entitiesToDelete.Count == 0);

                // m_lineOverlapRBElementList = null;
                m_overlapRBElementList = null;
                m_entityResultSet = null;
                m_isLoaded = false;
                /*
               lock (m_lineOverlapRBElementListCollection)
               {
                   foreach (var item in m_lineOverlapRBElementListCollection)
                   {
                       item.Clear();
                   }
               }  */
                lock (m_entityInputListCollection)
                {
                    foreach (var item in m_entityInputListCollection)
                    {
                        item.Clear();
                    }
                }
                lock (m_overlapRBElementListCollection)
                {
                    foreach (var item in m_overlapRBElementListCollection)
                    {
                        item.Clear();
                    }
                }
                lock (m_entityResultSetCollection)
                {
                    foreach (var item in m_entityResultSetCollection)
                    {
                        item.Clear();
                    }
                }
                lock (m_allIgnoredEntitiesCollection)
                {
                    foreach (var item in m_allIgnoredEntitiesCollection)
                    {
                        item.Clear();
                    }
                }
            }

            for (int i = m_sceneComponents.Count - 1; i >= 0; i--)
            {
                m_sceneComponents[i].Unload();
            }
            m_sceneComponents.Clear();
        }

        //  IMPORTANT: Only adds object to the list. Caller must call Start() or Init() on the object.
        public static void Add(MyEntity entity, bool insertIntoScene = true)
        {
            System.Diagnostics.Debug.Assert(entity.Parent == null, "There are only root entities in MyEntities");

            if (insertIntoScene)
            {
                entity.OnAddedToScene(entity);
            }

            if (Exist(entity) == false)
            {
                if (entity is MyVoxelBase)
                {
                    MySession.Static.VoxelMaps.Add((MyVoxelBase)entity);
                }

                m_entities.Add(entity);
            }

            RaiseEntityAdd(entity);
        }

        public static void SetEntityName(MyEntity myEntity, bool possibleRename = true)
        {
            string oldName = null;
            string newName = myEntity.Name;
            if (possibleRename)
            {
                foreach (var item in m_entityNameDictionary)
                {
                    if (item.Value == myEntity)
                    {
                        m_entityNameDictionary.Remove(item.Key);
                        oldName = item.Key;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(myEntity.Name))
            {
                Debug.Assert(!m_entityNameDictionary.ContainsKey(myEntity.Name));
                if (!m_entityNameDictionary.ContainsKey(myEntity.Name))
                {
                    m_entityNameDictionary.Add(myEntity.Name, myEntity);
                }
            }

            if (OnEntityNameSet != null)
            {
                OnEntityNameSet(myEntity, oldName, newName);
            }
        }

        public static bool IsNameExists(MyEntity entity, string name)
        {
            foreach (var item in m_entityNameDictionary)
            {
                if (item.Key == name && item.Value != entity)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Removes the specified entity from scene
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="skipIfNotExist">if set to <c>true</c> [skip if not exist].</param>
        public static void Remove(MyEntity entity)
        {
            System.Diagnostics.Debug.Assert(entity != null);

            if (entity is MyVoxelMap)
            {
                MySession.Static.VoxelMaps.RemoveVoxelMap((MyVoxelMap)entity);
            }

            if (m_entities.Contains(entity))
            {
                m_entities.Remove(entity);
                entity.OnRemovedFromScene(entity);
            }
            RaiseEntityRemove(entity);
        }


        public static FastResourceLock EntityCloseLock = new FastResourceLock();
        public static FastResourceLock EntityMarkForCloseLock = new FastResourceLock();
        public static FastResourceLock UnloadDataLock = new FastResourceLock();
        //public static object EntityCloseLock = new object();

        private static void DeleteRememberedEntities()
        {
            CloseAllowed = true;

            while (m_entitiesToDelete.Count > 0)
            {
                using (EntityCloseLock.AcquireExclusiveUsing())
                {
                    MyEntity entity = m_entitiesToDelete.FirstElement();

                    var deleteCallback = OnEntityDelete;
                    if (deleteCallback != null)
                    {
                        deleteCallback(entity);
                    }
                    entity.Delete();
                }
            }

            CloseAllowed = false;

            HashSet<MyEntity> tempList = m_entitiesToDelete;
            m_entitiesToDelete = m_entitiesToDeleteNextFrame;
            m_entitiesToDeleteNextFrame = tempList;
        }

        public static void RemoveFromClosedEntities(MyEntity entity)
        {
            if (m_entitiesToDelete.Count > 0)
            {
                m_entitiesToDelete.Remove(entity);
            }
            if (m_entitiesToDeleteNextFrame.Count > 0)
            {
                m_entitiesToDeleteNextFrame.Remove(entity);
            }
        }

        /// <summary>
        /// Remove name of entity from used names
        /// </summary>
        public static void RemoveName(MyEntity entity)
        {
            if (!string.IsNullOrEmpty(entity.Name))
            {
                m_entityNameDictionary.Remove(entity.Name);
            }
        }

        /// <summary>
        /// Checks if entity exists in scene already
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool Exist(MyEntity entity)
        {
            if (m_entities == null)
                return false;

            return m_entities.Contains(entity);
        }

        public static void Close(MyEntity entity)
        {
            //Debug.Assert(!entity.Closed, "MarkForClose() is being called on close entity, use OnClose to prevent this call");

            if (CloseAllowed)
            {
                m_entitiesToDeleteNextFrame.Add(entity);
                return;
            }

            if (!m_entitiesToDelete.Contains(entity))
            {
                EntityMarkForCloseLock.AcquireExclusive();
                m_entitiesToDelete.Add(entity);
                EntityMarkForCloseLock.ReleaseExclusive();
            }
        }

        //  Closes all objects - they are removed from within close method
        public static void CloseAll()
        {
            if (OnCloseAll != null)
                OnCloseAll();

            CloseAllowed = true;

            while (m_entities.Count > 0)
            {
                MyEntity entity = m_entities.First();
                entity.Delete();
            }

            foreach (MyEntity entity in m_entitiesToDelete.ToArray())
            {
                entity.Delete();
            }

            m_entitiesForUpdateOnce.ApplyRemovals();
            m_entitiesForUpdate.ApplyRemovals();
            m_entitiesForUpdate10.ApplyRemovals();
            m_entitiesForUpdate100.ApplyRemovals();

            CloseAllowed = false;
            m_entitiesToDelete.Clear();

            // Clear all
            MyEntityIdentifier.Clear();

            MyGamePruningStructure.Clear();
            MyRadioBroadcasters.Clear();

            m_entitiesForUpdateOnce.DebugCheckEmpty();
            m_entitiesForUpdate.DebugCheckEmpty();
            m_entitiesForUpdate10.DebugCheckEmpty();
            m_entitiesForUpdate100.DebugCheckEmpty();
            Debug.Assert(m_entitiesForDraw.Count == 0);
            Debug.Assert(m_entitiesForDrawToAdd.Count == 0);
        }

        public static void RegisterForUpdate(MyEntity entity)
        {
            if ((entity.NeedsUpdate & MyEntityUpdateEnum.BEFORE_NEXT_FRAME) > 0)
            {
                m_entitiesForUpdateOnce.Add(entity);
            }
            if ((entity.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) > 0)
            {
                m_entitiesForUpdate.Add(entity);
            }
            if ((entity.NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) > 0)
            {
                m_entitiesForUpdate10.Add(entity);
            }
            if ((entity.NeedsUpdate & MyEntityUpdateEnum.EACH_100TH_FRAME) > 0)
            {
                m_entitiesForUpdate100.Add(entity);
            }
        }

        public static void RegisterForDraw(IMyEntity entity)
        {
            if (entity.Render.NeedsDraw)
            {
                m_entitiesForDrawToAdd.Add(entity);
            }
        }

        public static void UnregisterForUpdate(MyEntity entity, bool immediate = false)
        {
            if ((entity.Flags & EntityFlags.NeedsUpdateBeforeNextFrame) != 0)
            {
                m_entitiesForUpdateOnce.Remove(entity, immediate);
            }

            if ((entity.Flags & EntityFlags.NeedsUpdate) != 0)
            {
                m_entitiesForUpdate.Remove(entity, immediate);
            }

            if ((entity.Flags & EntityFlags.NeedsUpdate10) != 0)
            {
                m_entitiesForUpdate10.Remove(entity, immediate);
            }

            if ((entity.Flags & EntityFlags.NeedsUpdate100) != 0)
            {
                m_entitiesForUpdate100.Remove(entity, immediate);
            }
        }

        public static void UnregisterForDraw(IMyEntity entity)
        {
            m_entitiesForDrawToAdd.Remove(entity);
            m_entitiesForDraw.Remove(entity);
        }


        public static bool UpdateInProgress = false;
        public static bool CloseAllowed = false;

        static int m_update10Index = 0;
        static int m_update100Index = 0;
        static float m_update10Count = 0;
        static float m_update100Count = 0;


        public static void UpdateBeforeSimulation()
        {
            if (MySandboxGame.IsGameReady == false)
            {
                return;
            }

            ProfilerShort.Begin("MyEntities.UpdateBeforeSimulation");
            System.Diagnostics.Debug.Assert(UpdateInProgress == false);
            UpdateInProgress = true;

            {
                ProfilerShort.Begin("Before first frame");
                UpdateOnceBeforeFrame();

                ProfilerShort.BeginNextBlock("Each update");
                m_entitiesForUpdate.ApplyChanges();
                foreach (MyEntity entity in m_entitiesForUpdate)
                {
                    ProfilerShort.Begin(Partition.Select(entity.GetType().GetHashCode(), "Part1", "Part2", "Part3", "Part4", "Part5"));
                    ProfilerShort.Begin(entity.GetType().Name);
                    if (entity.MarkedForClose == false)
                    {
                        entity.UpdateBeforeSimulation();
                    }
                    ProfilerShort.End();
                    ProfilerShort.End();
                }

                ProfilerShort.BeginNextBlock("10th update");
                m_entitiesForUpdate10.ApplyChanges();
                if (m_entitiesForUpdate10.Count > 0)
                {
                    ++m_update10Index;
                    m_update10Index %= 10;
                    for (int i = m_update10Index; i < m_entitiesForUpdate10.Count; i += 10)
                    {
                        var entity = m_entitiesForUpdate10[i];
                        ProfilerShort.Begin(entity.GetType().Name);
                        if (entity.MarkedForClose == false)
                        {
                            entity.UpdateBeforeSimulation10();
                        }
                        ProfilerShort.End();
                    }
                }

                ProfilerShort.BeginNextBlock("100th update");
                m_entitiesForUpdate100.ApplyChanges();
                if (m_entitiesForUpdate100.Count > 0)
                {
                    ++m_update100Index;
                    m_update100Index %= 100;
                    for (int i = m_update100Index; i < m_entitiesForUpdate100.Count; i += 100)
                    {
                        var entity = m_entitiesForUpdate100[i];
                        ProfilerShort.Begin(entity.GetType().Name);
                        if (entity.MarkedForClose == false)
                        {
                            entity.UpdateBeforeSimulation100();
                        }
                        ProfilerShort.End();
                    }
                }
                ProfilerShort.End();
            }

            UpdateInProgress = false;

            ProfilerShort.End();
        }

        public static void UpdateOnceBeforeFrame()
        {
            m_entitiesForUpdateOnce.ApplyChanges();
            foreach (var entity in m_entitiesForUpdateOnce)
            {
                entity.NeedsUpdate &= ~MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                if (entity.MarkedForClose == false)
                {
                    entity.UpdateOnceBeforeFrame();
                }
            }
        }

        //  Update all physics objects - AFTER physics simulation
        public static void UpdateAfterSimulation()
        {
            if (MySandboxGame.IsGameReady == false)
            {
                return;
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("UpdateAfterSimulation");
            {
                System.Diagnostics.Debug.Assert(UpdateInProgress == false);
                UpdateInProgress = true;

                ProfilerShort.Begin("UpdateAfter1");
                m_entitiesForUpdate.ApplyChanges();
                for (int i = 0; i < m_entitiesForUpdate.Count; i++)
                {
                    MyEntity entity = m_entitiesForUpdate[i];

                    ProfilerShort.Begin(entity.GetType().Name);
                    if (entity.MarkedForClose == false)
                    {
                        entity.UpdateAfterSimulation();
                    }
                    ProfilerShort.End();
                }

                ProfilerShort.End();

                ProfilerShort.Begin("UpdateAfter10");
                m_entitiesForUpdate10.ApplyChanges();
                if (m_entitiesForUpdate10.Count > 0)
                {
                    for (int i = m_update10Index; i < m_entitiesForUpdate10.Count; i += 10)
                    {
                        MyEntity entity = m_entitiesForUpdate10[i];
                        ProfilerShort.Begin(entity.GetType().Name);
                        if (entity.MarkedForClose == false)
                        {
                            entity.UpdateAfterSimulation10();
                        }
                        ProfilerShort.End();
                    }
                }
                ProfilerShort.End();

                ProfilerShort.Begin("UpdateAfter100");
                m_entitiesForUpdate100.ApplyChanges();
                if (m_entitiesForUpdate100.Count > 0)
                {
                    for (int i = m_update100Index; i < m_entitiesForUpdate100.Count; i += 100)
                    {
                        MyEntity entity = m_entitiesForUpdate100[i];
                        ProfilerShort.Begin(entity.GetType().Name);
                        if (entity.MarkedForClose == false)
                        {
                            entity.UpdateAfterSimulation100();
                        }
                        ProfilerShort.End();
                    }
                }
                ProfilerShort.End();

                UpdateInProgress = false;

                DeleteRememberedEntities();
            }

            while (m_creationThread.ConsumeResult()) ; // Add entities created asynchronously

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void UpdatingStopped()
        {
            for (int i = 0; i < m_entitiesForUpdate.Count; i++)
            {
                MyEntity entity = m_entitiesForUpdate[i];

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock(entity.GetType().Name);
                entity.UpdatingStopped();
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }
        }

        private static bool IsAnyRenderObjectVisible(MyEntity entity)
        {
            ProfilerShort.Begin("Lookup");
            foreach (var id in entity.Render.RenderObjectIDs)
            {
                if (MyRenderProxy.VisibleObjectsRead.Contains(id))
                {
                    ProfilerShort.End();
                    return true;
                }
            }
            ProfilerShort.End();
            return false;
        }

        public static void Draw()
        {
            ProfilerShort.Begin("Each draw");
            m_entitiesForDraw.AddList(m_entitiesForDrawToAdd);
            m_entitiesForDrawToAdd.Clear();

            foreach (MyEntity entity in m_entitiesForDraw)
            {
                entity.PrepareForDraw();

                if (IsAnyRenderObjectVisible(entity) && entity.Render.NeedsDrawFromParent == false)
                {
                    ProfilerShort.Begin(entity.GetType().Name);
                    entity.Render.Draw();
                    ProfilerShort.End();
                }
            }

            ProfilerShort.End();

            foreach (var entry in m_entitiesForBBoxDraw)
            {
                var worldMatrix = entry.Key.WorldMatrix;
                var localAABB = (BoundingBoxD)entry.Key.PositionComp.LocalAABB;
                var args = entry.Value;
                localAABB.Min -= args.InflateAmount;
                localAABB.Max += args.InflateAmount;
                var worldToLocal = MatrixD.Invert(worldMatrix);
                MySimpleObjectDraw.DrawAttachedTransparentBox(ref worldMatrix, ref localAABB, ref args.Color, entry.Key.Render.GetRenderObjectID(), ref worldToLocal, MySimpleObjectRasterizer.Wireframe, Vector3I.One, args.LineWidth, lineMaterial: args.lineMaterial);
            }
        }


        // static List<MyRBElement> m_overlapRBElementListLocal = new List<MyRBElement>(256);
        static bool processingExplosions = false;

        [ThreadStatic]
        private static List<MyEntity> m_allIgnoredEntities;
        private static List<List<MyEntity>> m_allIgnoredEntitiesCollection = new List<List<MyEntity>>();
        private static List<MyEntity> AllIgnoredEntities
        {
            get
            {
                if (m_allIgnoredEntities == null)
                {
                    m_allIgnoredEntities = new List<MyEntity>();
                    m_allIgnoredEntitiesCollection.Add(m_allIgnoredEntities);
                }
                return m_allIgnoredEntities;
            }
        }


        public static MyEntity GetIntersectionWithSphere(ref BoundingSphereD sphere)
        {
            return GetIntersectionWithSphere(ref sphere, null, null, false, false);
        }

        //  Return objects that is intersecting sphere, see comments for method used below
        public static MyEntity GetIntersectionWithSphere(ref BoundingSphereD sphere, MyEntity ignoreEntity0, MyEntity ignoreEntity1)
        {
            return GetIntersectionWithSphere(ref sphere, ignoreEntity0, ignoreEntity1, false, true);
        }


        //Return reference to object that intersects specific sphere. If not intersection, null is returned.
        //We don't look for closest intersection - so we stop on first intersection found.
        //Params:
        //    sphere - sphere we want to test for intersection
        //    ignoreModelInstance0 and 1 - we may specify two phys objects we don't want to test for intersections. Usually this is model instance of who is shoting, or missile, etc.
        //    ignoreVoxelMaps - in some cases, we want to test intersection only with non-voxelmap phys objects
        public static void GetIntersectionWithSphere(ref BoundingSphereD sphere, MyEntity ignoreEntity0, MyEntity ignoreEntity1, bool ignoreVoxelMaps, bool volumetricTest, ref List<MyEntity> result)
        {
            //  Get collision elements near the line's bounding box (use sweep-and-prune, so we iterate only close objects)
            BoundingBoxD boundingBox = BoundingBoxD.CreateInvalid();
            boundingBox = boundingBox.Include(sphere);

            var entities = GetEntitiesInAABB(ref boundingBox);
            foreach (MyEntity entity in entities)
            {
                // Voxelmap to ignore
                if (ignoreVoxelMaps && entity is MyVoxelMap) continue;
                //  Objects to ignore
                if ((entity == ignoreEntity0) || (entity == ignoreEntity1)) continue;


                if (entity.GetIntersectionWithSphere(ref sphere))
                {
                    //  If intersection found, return that object. We don't need to look for more objects.
                    result.Add(entity);
                }

                if (volumetricTest && (entity is MyVoxelMap) && (entity as MyVoxelMap).DoOverlapSphereTest((float)sphere.Radius, sphere.Center))
                {
                    //  If intersection found, return that object. We don't need to look for more objects.
                    result.Add(entity);
                }
            }
            entities.Clear();
        }

        //  Return reference to object that intersects specific sphere. If not intersection, null is returned.
        //  We don't look for closest intersection - so we stop on first intersection found.
        //  Params:
        //      sphere - sphere we want to test for intersection
        //      ignoreModelInstance0 and 1 - we may specify two phys objects we don't want to test for intersections. Usually this is model instance of who is shoting, or missile, etc.
        //      ignoreVoxelMaps - in some cases, we want to test intersection only with non-voxelmap phys objects
        public static MyEntity GetIntersectionWithSphere(ref BoundingSphereD sphere, MyEntity ignoreEntity0, MyEntity ignoreEntity1, bool ignoreVoxelMaps, bool volumetricTest, bool excludeEntitiesWithDisabledPhysics = false, bool ignoreFloatingObjects = true, bool ignoreHandWeapons = true)
        {
            ProfilerShort.Begin("GetIntersectionWithSphere");
            //  Get collision elements near the line's bounding box (use sweep-and-prune, so we iterate only close objects)
            BoundingBoxD boundingBox = BoundingBoxD.CreateInvalid();
            boundingBox = boundingBox.Include(sphere);

            MyEntity result = null;

            ProfilerShort.Begin("AABB test");
            var entities = GetEntitiesInAABB(ref boundingBox);
            ProfilerShort.End();

            ProfilerShort.Begin("Entity loop");
            foreach (MyEntity entity in entities)
            {
                // Voxelmap to ignore
                if (ignoreVoxelMaps && entity is MyVoxelMap) continue;

                //  Objects to ignore
                if ((entity == ignoreEntity0) || (entity == ignoreEntity1)) continue;

                if (excludeEntitiesWithDisabledPhysics && (entity.Physics != null && entity.Physics.Enabled == false))
                    continue;

                if (ignoreFloatingObjects && (entity is MyFloatingObject || entity is Debris.MyDebrisBase))
                    continue;

                if (ignoreHandWeapons && ((entity is IMyHandheldGunObject<MyDeviceBase>) || (entity.Parent is IMyHandheldGunObject<MyDeviceBase>)))
                    continue;

                if (volumetricTest && entity.IsVolumetric && entity.DoOverlapSphereTest((float)sphere.Radius, sphere.Center))
                {
                    //  If intersection found, return that object. We don't need to look for more objects.
                    result = entity;
                    break;
                }
                else
                {
                    if (entity.GetIntersectionWithSphere(ref sphere))
                    {
                        //  If intersection found, return that object. We don't need to look for more objects.
                        result = entity;
                        break;
                    }
                }

            }
            entities.Clear();
            ProfilerShort.End();
            ProfilerShort.End();
            return result;
        }

        public static void OverlapAllLineSegment(ref LineD line, List<MyLineSegmentOverlapResult<MyEntity>> resultList)
        {
            MyGamePruningStructure.GetAllEntitiesInRay(ref line, resultList);
        }

        //  Calculates intersection of line with any triangleVertexes in the world (every model instance). Closest intersection and intersected triangleVertexes will be returned.
        //  Params:
        //      line - line we want to test for intersection
        //      ignoreModelInstance0 and 1 - we may specify two phys objects we don't want to test for intersections. Usually this is model instance of who is shoting, or missile, etc.
        //      outIntersection - intersection data calculated by this method
        public static MyIntersectionResultLineTriangleEx? GetIntersectionWithLine(ref LineD line, MyEntity ignoreEntity0, MyEntity ignoreEntity1, bool ignoreChildren = false, bool ignoreFloatingObjects = true, bool ignoreHandWeapons = true, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES, float timeFrame = 0)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("GetIntersectionWithLine.GetChildren");
            EntityResultSet.Clear();
            if (ignoreChildren)
            {
                if (ignoreEntity0 != null)
                {
                    ignoreEntity0 = ignoreEntity0.GetBaseEntity();
                    ignoreEntity0.Hierarchy.GetChildrenRecursive(EntityResultSet);
                }

                if (ignoreEntity1 != null)
                {
                    ignoreEntity1 = ignoreEntity1.GetBaseEntity();
                    ignoreEntity1.Hierarchy.GetChildrenRecursive(EntityResultSet);
                }
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            //  Get collision skins near the line's bounding box (use sweep-and-prune, so we iterate only close objects)
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("GetIntersectionWithLine.OverlapRBAllLineSegment");
            LineOverlapEntityList.Clear();
            MyGamePruningStructure.GetAllEntitiesInRay(ref line, LineOverlapEntityList);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            LineOverlapEntityList.Sort(MyLineSegmentOverlapResult<MyEntity>.DistanceComparer);

            MyIntersectionResultLineTriangleEx? ret = null;
            RayD ray = new RayD(line.From, line.Direction);
            foreach (var result in LineOverlapEntityList)
            {
                if (ret.HasValue)
                {
                    double? distToAabb = result.Element.PositionComp.WorldAABB.Intersects(ray);
                    if (distToAabb.HasValue)
                    {
                        var distToIntersectionSq = Vector3D.DistanceSquared(line.From, ret.Value.IntersectionPointInWorldSpace);
                        var distToAabbSq = distToAabb.Value * distToAabb.Value;
                        if (distToIntersectionSq < distToAabbSq)
                        {
                            break;
                        }
                    }
                }

                MyEntity entity = result.Element;

                //  Objects to ignore
                if (entity == ignoreEntity0 || entity == ignoreEntity1 || (ignoreChildren && EntityResultSet.Contains(entity))) continue;

                // Ignore objects without physics
                if (entity.Physics == null || !entity.Physics.Enabled) continue;

                if (entity.MarkedForClose) continue;

                if (ignoreFloatingObjects && (entity is MyFloatingObject || entity is Debris.MyDebrisBase))
                    continue;

                if (ignoreHandWeapons && ((entity is IMyHandheldGunObject<MyDeviceBase>) || (entity.Parent is IMyHandheldGunObject<MyDeviceBase>)))
                    continue;



                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("GetIntersectionWithLine.GetIntersectionWithLine");
                MyIntersectionResultLineTriangleEx? testResultEx = null;

                if (timeFrame == 0 || entity.Physics == null || entity.Physics.LinearVelocity.LengthSquared() < 0.1f || !entity.IsCCDForProjectiles)
                    entity.GetIntersectionWithLine(ref line, out testResultEx, flags);
                else
                {
                    float distance = entity.Physics.LinearVelocity.Length() * timeFrame;
                    float distanceStep = entity.PositionComp.LocalVolume.Radius;
                    float distanceTest = 0;
                    Vector3 oldPos = entity.PositionComp.GetPosition();
                    Vector3 dir = Vector3.Normalize(entity.Physics.LinearVelocity);
                    while (!testResultEx.HasValue && distanceTest < distance)
                    {
                        entity.PositionComp.SetPosition(oldPos + distanceTest * dir);
                        entity.GetIntersectionWithLine(ref line, out testResultEx, flags);
                        distanceTest += distanceStep;
                    }
                    entity.PositionComp.SetPosition(oldPos);
                }

                if (testResultEx.HasValue && testResultEx.Value.Entity != ignoreEntity0 && testResultEx.Value.Entity != ignoreEntity1 && (!ignoreChildren || !EntityResultSet.Contains(testResultEx.Value.Entity)))
                {
                    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("GetIntersectionWithLine.GetCloserIntersection");
                    //  If intersection occured and distance to intersection is closer to origin than any previous intersection
                    ret = MyIntersectionResultLineTriangleEx.GetCloserIntersection(ref ret, ref testResultEx);
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                }
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }

            LineOverlapEntityList.Clear();
            return ret;
        }


        //  Allows you to iterate through all phys. objects
        public static HashSet<MyEntity> GetEntities()
        {
            return m_entities;
        }

        public static MyEntity GetEntityById(long entityId)
        {
            return MyEntityIdentifier.GetEntityById(entityId) as MyEntity;
        }

        public static bool EntityExists(long entityId)
        {
            return MyEntityIdentifier.ExistsById(entityId);
        }

        public static bool TryGetEntityById(long entityId, out MyEntity entity)
        {
            return MyEntityIdentifier.TryGetEntity(entityId, out entity);
        }

        public static bool TryGetEntityById<T>(long entityId, out T entity)
            where T : MyEntity
        {
            MyEntity baseEntity;
            var result = MyEntityIdentifier.TryGetEntity(entityId, out baseEntity) && baseEntity is T;
            entity = baseEntity as T;
            return result;
        }

        public static MyEntity GetEntityByName(string name)
        {
            return m_entityNameDictionary[name];
        }

        public static bool TryGetEntityByName(string name, out MyEntity entity)
        {
            return m_entityNameDictionary.TryGetValue(name, out entity);
        }

        public static bool EntityExists(string name)
        {
            return m_entityNameDictionary.ContainsKey(name);
        }

        public static void RaiseEntityRemove(MyEntity entity)
        {
            if (OnEntityRemove != null)
            {
                OnEntityRemove(entity);
            }
        }

        public static void RaiseEntityAdd(MyEntity entity)
        {
            if (OnEntityAdd != null)
            {
                OnEntityAdd(entity);
            }
        }

        #region Global visibility and selectability by entity types/groups

        /// <summary>
        /// Types in this set and their subtypes will be temporarily invisible.
        /// </summary>
        private static HashSet<Type> m_hiddenTypes = new HashSet<Type>();

        public static void SetTypeHidden(Type type, bool hidden)
        {
            if (hidden == m_hiddenTypes.Contains(type)) return;  // no change

            if (hidden)
                m_hiddenTypes.Add(type);
            else
                m_hiddenTypes.Remove(type);
        }

        public static bool IsTypeHidden(Type type)
        {
            foreach (var hiddenType in m_hiddenTypes)
                if (hiddenType.IsAssignableFrom(type))
                    return true;
            return false;
        }

        public static bool IsVisible(IMyEntity entity)
        {
            return !IsTypeHidden(entity.GetType());
        }

        public static void UnhideAllTypes()
        {
            foreach (var type in m_hiddenTypes.ToList())
                SetTypeHidden(type, false);
        }

        public static bool SafeAreasHidden, SafeAreasSelectable;
        public static bool DetectorsHidden, DetectorsSelectable;
        public static bool ParticleEffectsHidden, ParticleEffectsSelectable;

        public static bool ShowDebugDrawStatistics = false;
        static Dictionary<string, int> m_typesStats = new Dictionary<string, int>();

        #endregion

        static public void DebugDrawStatistics()
        {
            m_typesStats.Clear();

            if (!ShowDebugDrawStatistics)
                return;

            foreach (MyEntity entity in m_entitiesForUpdate)
            {
                string ts = entity.GetType().Name.ToString();
                if (!m_typesStats.ContainsKey(ts))
                    m_typesStats.Add(ts, 0);
                m_typesStats[ts]++;
            }

            Vector2 offset = new Vector2(100, 0);
            // TODO: Par
            //MyDebugDraw.DrawText(offset, new System.Text.StringBuilder("Detailed entity statistics"), Color.Yellow, 2);

            // TODO: Par
            //float scale = 0.7f;
            //offset.Y += 50;
            //MyDebugDraw.DrawText(offset, new System.Text.StringBuilder("Entities for update:"), Color.Yellow, scale);
            //offset.Y += 30;
            //foreach (KeyValuePair<string, int> pair in Render.MyRender.SortByValue(m_typesStats))
            //{
            //    MyDebugDraw.DrawText(offset, new System.Text.StringBuilder(pair.Key + ": " + pair.Value.ToString() + "x"), Color.Yellow, scale);
            //    offset.Y += 20;
            //}


            //m_typesStats.Clear();

            //foreach (MyEntity entity in m_entities)
            //{
            //    string ts = entity.GetType().Name.ToString();
            //    if (!m_typesStats.ContainsKey(ts))
            //        m_typesStats.Add(ts, 0);
            //    m_typesStats[ts]++;
            //}

            //offset = new Vector2(500, 0);
            //scale = 0.7f;
            //offset.Y += 50;
            //MyDebugDraw.DrawText(offset, new System.Text.StringBuilder("All entities:"), Color.Yellow, scale);
            //offset.Y += 30;
            //foreach (KeyValuePair<string, int> pair in Render.MyRender.SortByValue(m_typesStats))
            //{
            //    MyDebugDraw.DrawText(offset, new System.Text.StringBuilder(pair.Key + ": " + pair.Value.ToString() + "x"), Color.Yellow, scale);
            //    offset.Y += 20;
            //}
        }

        static HashSet<IMyEntity> m_entitiesForDebugDraw = new HashSet<IMyEntity>();

        public static IMyEntity GetEntityFromRenderObjectID(uint renderObjectID)
        {
            using (m_renderObjectToEntityMapLock.AcquireSharedUsing())
            {
                IMyEntity entity = null;
                m_renderObjectToEntityMap.TryGetValue(renderObjectID, out entity);
                return entity;
            }
        }

        static HashSet<object> m_groupDebugHelper = new HashSet<object>();

        private static void DebugDrawGroups<TNode, TGroupData>(MyGroups<TNode, TGroupData> groups)
            where TGroupData : IGroupData<TNode>, new()
            where TNode : MyCubeGrid
        {
            int hue = 0;
            foreach (var g in groups.Groups)
            {
                Color color = new Vector3((hue++ % 15) / 15.0f, 1, 1).HSVtoColor();

                foreach (var m in g.Nodes)
                {
                    try
                    {
                        foreach (var child in m.Children)
                        {
                            m_groupDebugHelper.Add(child);
                        }

                        // This is O(n^2), but it's only debug draw
                        foreach (var child in m_groupDebugHelper)
                        {
                            MyGroups<TNode, TGroupData>.Node node = null;
                            int count = 0;
                            foreach (var c in m.Children)
                            {
                                if (child == c)
                                {
                                    node = c;
                                    count++;
                                }
                            }
                            MyRenderProxy.DebugDrawLine3D(m.NodeData.PositionComp.WorldAABB.Center, node.NodeData.PositionComp.WorldAABB.Center, color, color, false);
                            MyRenderProxy.DebugDrawText3D((m.NodeData.PositionComp.WorldAABB.Center + node.NodeData.PositionComp.WorldAABB.Center) * 0.5f, count.ToString(), color, 1.0f, false);
                        }

                        var lightColor = new Color(color.ToVector3() + 0.25f);
                        MyRenderProxy.DebugDrawSphere(m.NodeData.PositionComp.WorldAABB.Center, 0.2f, lightColor.ToVector3(), 0.5f, false, true);

                        // Show forward-up
                        //MyRenderProxy.DebugDrawLine3D(m.NodeData.WorldAABB.Center, m.NodeData.WorldAABB.Center + m.NodeData.WorldMatrix.Forward, Color.Red, Color.Red, false);
                        //MyRenderProxy.DebugDrawLine3D(m.NodeData.WorldAABB.Center, m.NodeData.WorldAABB.Center + m.NodeData.WorldMatrix.Up, Color.Green, Color.Green, false);

                        MyRenderProxy.DebugDrawText3D(m.NodeData.PositionComp.WorldAABB.Center, m.LinkCount.ToString(), lightColor, 1.0f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    }
                    finally
                    {
                        m_groupDebugHelper.Clear();
                    }
                }
            }
        }

        public static void DebugDraw()
        {
            ProfilerShort.Begin("MyEntities.DebugDraw");
            MyEntityComponentsDebugDraw.DebugDraw();

            if (MyDebugDrawSettings.DEBUG_DRAW_GRID_GROUPS_PHYSICAL && MyCubeGridGroups.Static != null)
            {
                DebugDrawGroups(MyCubeGridGroups.Static.Physical);
            }
            if (MyDebugDrawSettings.DEBUG_DRAW_GRID_GROUPS_LOGICAL && MyCubeGridGroups.Static != null)
            {
                DebugDrawGroups(MyCubeGridGroups.Static.Logical);
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_SMALL_TO_LARGE_BLOCK_GROUPS && MyCubeGridGroups.Static != null)
            {
                MyCubeGridGroups.DebugDrawBlockGroups(MyCubeGridGroups.Static.SmallToLargeBlockConnections);
            }

            if (
                MyDebugDrawSettings.DEBUG_DRAW_PHYSICS ||
                MyDebugDrawSettings.ENABLE_DEBUG_DRAW ||
                MyFakes.SHOW_INVALID_TRIANGLES)
            {
                using (m_renderObjectToEntityMapLock.AcquireSharedUsing())
                {
                    m_entitiesForDebugDraw.Clear();

                    foreach (uint renderObjectID in VRageRender.MyRenderProxy.VisibleObjectsRead)
                    {
                        IMyEntity entity;
                        m_renderObjectToEntityMap.TryGetValue(renderObjectID, out entity);

                        if (entity != null)
                        {
                            IMyEntity rootEntity = entity.GetTopMostParent();
                            if (!m_entitiesForDebugDraw.Contains(rootEntity))
                            {
                                m_entitiesForDebugDraw.Add(rootEntity);
                            }
                        }
                    }

                    if (MyDebugDrawSettings.DEBUG_DRAW_GRID_COUNTER)
                    {
                        MyRenderProxy.DebugDrawText2D(new Vector2(700.0f, 0.0f), "Grid number: " + MyCubeGrid.GridCounter, Color.Red, 1.0f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);
                    }

                    foreach (IMyEntity entity in m_entitiesForDebugDraw)
                    {
                        if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
                            entity.DebugDraw();
                        if (MyDebugDrawSettings.DEBUG_DRAW_PHYSICS)
                        {
                            if (entity.Physics != null)
                                entity.Physics.DebugDraw();
                        }
                        if (MyFakes.SHOW_INVALID_TRIANGLES)
                            entity.DebugDrawInvalidTriangles();
                    }

                    m_entitiesForDebugDraw.Clear();

                    //if (MyDebugDrawSettings.DEBUG_DRAW_COLLISION_PRIMITIVES)
                    //{
                    //    foreach (uint renderObjectID in VRageRender.MyRenderProxy.VisibleObjectsRead)
                    //    {
                    //        IMyEntity entity;
                    //        m_renderObjectToEntityMap.TryGetValue(renderObjectID, out entity);

                    //        if (entity != null)
                    //        {
                    //            IMyEntity rootEntity = entity.GetTopMostParent();
                    //            if (!m_entitiesForDebugDraw.Contains(rootEntity))
                    //            {
                    //                m_entitiesForDebugDraw.Add(rootEntity);
                    //            }
                    //        }
                    //    }

                    //    foreach (IMyEntity entity in m_entitiesForDebugDraw)
                    //    {
                    //        if(entity.Physics != null)
                    //            entity.Physics.DebugDraw();
                    //    }
                    //}

                    //if (MyFakes.SHOW_INVALID_TRIANGLES)
                    //{
                    //    m_entitiesForDebugDraw.Clear();

                    //    foreach (uint renderObjectID in VRageRender.MyRenderProxy.VisibleObjectsRead)
                    //    {
                    //        IMyEntity entity;
                    //        m_renderObjectToEntityMap.TryGetValue(renderObjectID, out entity);

                    //        if (entity != null)
                    //        {
                    //            IMyEntity rootEntity = entity.GetTopMostParent();
                    //            if (!m_entitiesForDebugDraw.Contains(rootEntity))
                    //            {
                    //                m_entitiesForDebugDraw.Add(rootEntity);
                    //            }
                    //        }
                    //    }

                    //    foreach (IMyEntity entity in m_entitiesForDebugDraw)
                    //    {
                    //        entity.DebugDrawInvalidTriangles();
                    //    }
                    //}

                    if (MyDebugDrawSettings.DEBUG_DRAW_GAME_PRUNNING)
                    {
                        MyGamePruningStructure.DebugDraw();
                    }

                    if (MyDebugDrawSettings.DEBUG_DRAW_RADIO_BROADCASTERS)
                    {
                        MyRadioBroadcasters.DebugDraw();
                    }
                }

                m_entitiesForDebugDraw.Clear();
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_CLUSTERS)
            {
                if (MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_CLUSTERS != MyPhysics.DebugDrawClustersEnable)
                {
                    MyPhysics.DebugDrawClustersMatrix = MySector.MainCamera.WorldMatrix;
                }

                MyPhysics.DebugDrawClusters();
            }
            MyPhysics.DebugDrawClustersEnable = MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_CLUSTERS;
            ProfilerShort.End();
        }

        public static MyEntity CreateFromObjectBuilderAndAdd(MyObjectBuilder_EntityBase objectBuilder)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock(objectBuilder.GetType().Name);

            //Dont add to scene entity which was not there
            bool insertIntoScene = (int)(objectBuilder.PersistentFlags & MyPersistentEntityFlags2.InScene) > 0;

            if (MyFakes.ENABLE_LARGE_OFFSET)
            {
                if (objectBuilder.PositionAndOrientation.Value.Position.X < 10000)
                {

                    objectBuilder.PositionAndOrientation = new MyPositionAndOrientation()
                    {
                        Forward = objectBuilder.PositionAndOrientation.Value.Forward,
                        Up = objectBuilder.PositionAndOrientation.Value.Up,
                        Position = new SerializableVector3D(
                        objectBuilder.PositionAndOrientation.Value.Position + new Vector3D(1E9))
                    };
                }
            }

            MyEntity retVal = CreateFromObjectBuilder(objectBuilder);

            if (retVal != null)
            {
                Add(retVal, insertIntoScene);
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            return retVal;
        }

        /// <summary>
        /// Creates object asynchronously and adds it into scene.
        /// DoneHandler is invoked from update thread when the object is added into scene.
        /// </summary>
        public static void CreateAsync(MyObjectBuilder_EntityBase objectBuilder, bool addToScene, Action<MyEntity> doneHandler = null)
        {
            Debug.Assert(m_creationThread != null, "Creation thread is null, unloading?");
            if (m_creationThread != null)
            {
                m_creationThread.SubmitWork(objectBuilder, addToScene, doneHandler);
            }
        }

        public static void InitAsync(MyEntity entity, MyObjectBuilder_EntityBase objectBuilder, bool addToScene, Action<MyEntity> doneHandler = null)
        {
            Debug.Assert(m_creationThread != null, "Creation thread is null, unloading?");
            if (m_creationThread != null)
            {
                m_creationThread.SubmitWork(objectBuilder, addToScene, doneHandler, entity);
            }
        }

        public static bool MemoryLimitReached
        {
            get
            {
                if (!Environment.Is64BitProcess && MySandboxGame.Config.MemoryLimits)
                    return GC.GetTotalMemory(false) > EntityManagedMemoryLimit || WinApi.WorkingSet > EntityNativeMemoryLimit;
                else
                    return false;
            }
        }

        public static bool MemoryLimitReachedReport
        {
            get
            {
                if (MemoryLimitReached)
                {
                    MySandboxGame.Log.WriteLine("Memory limit reached");
                    MySandboxGame.Log.WriteLine("GC Memory: " + GC.GetTotalMemory(false).ToString());
                    MyHud.Notifications.Add(MyNotificationSingletons.GameOverload);
                    return true;
                }
                return false;
            }
        }

        public static bool MemoryLimitAddFailure
        {
            get;
            private set;
        }

        public static void MemoryLimitAddFailureReset()
        {
            MemoryLimitAddFailure = false;
        }

        public static void RemapObjectBuilderCollection(IEnumerable<MyObjectBuilder_EntityBase> objectBuilders)
        {
            foreach (var objectBuilder in objectBuilders)
                objectBuilder.Remap(m_remapHelper);
            m_remapHelper.Clear();
        }

        public static void RemapObjectBuilder(MyObjectBuilder_EntityBase objectBuilder)
        {
            objectBuilder.Remap(m_remapHelper);
            m_remapHelper.Clear();
        }

        public static MyEntity CreateFromObjectBuilderNoinit(MyObjectBuilder_EntityBase objectBuilder)
        {
            if ((objectBuilder.TypeId == typeof(MyObjectBuilder_CubeGrid) || objectBuilder.TypeId == typeof(MyObjectBuilder_VoxelMap)) && !MyEntities.IgnoreMemoryLimits && MemoryLimitReachedReport)
            {
                MemoryLimitAddFailure = true;
                MySandboxGame.Log.WriteLine("WARNING: MemoryLimitAddFailure reached");
                return null;
            }

            return MyEntityFactory.CreateEntity(objectBuilder);
        }

        public static MyEntity CreateFromObjectBuilder(MyObjectBuilder_EntityBase objectBuilder)
        {
            MyEntity entity = CreateFromObjectBuilderNoinit(objectBuilder);
            InitEntity(objectBuilder, ref entity);
            return entity;
        }

        public static void InitEntity(MyObjectBuilder_EntityBase objectBuilder, ref MyEntity entity)
        {
            if (entity != null)
            {
                if (MyFakes.THROW_LOADING_ERRORS)
                {
                    entity.Init(objectBuilder);
                }
                else
                {
                    try
                    {
                        entity.Init(objectBuilder);
                    }
                    catch (Exception ex)
                    {
                        Debug.Fail("Exception during entity.Init");
                        MySandboxGame.Log.WriteLine("ERROR Entity init!: " + ex);
                        entity.EntityId = 0;
                        entity = null;
                    }
                }
            }
        }

        /// <summary>
        /// Returns false when not all entities were loaded
        /// </summary>
        public static bool Load(List<MyObjectBuilder_EntityBase> objectBuilders)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyEntities.Load");

            MyEntityIdentifier.AllocationSuspended = true;

            bool allEntitiesAdded = true;
            try
            {

                if (objectBuilders != null)
                {
                    //  Objects received from server
                    foreach (MyObjectBuilder_EntityBase objectBuilder in objectBuilders)
                    {
                        // Don't load characters
                        //if (objectBuilder.TypeId == MyObjectBuilderTypeEnum.Character)
                        //continue;

                        // Don't load voxels
                        if (MyFakes.SKIP_VOXELS_DURING_LOAD && objectBuilder.TypeId == typeof(MyObjectBuilder_VoxelMap) && (objectBuilder as MyObjectBuilder_VoxelMap).StorageName != "BaseAsteroid")
                            continue;

                        //if (objectBuilder.TypeId == MyObjectBuilderTypeEnum.CubeGrid && ((MyObjectBuilder_CubeGrid)objectBuilder).CubeBlocks.Count > 500)
                        //continue;

                        // Don't load large grid
                        //if (objectBuilder.TypeId == MyObjectBuilderTypeEnum.CubeGrid && ((MyObjectBuilder_CubeGrid)objectBuilder).CubeBlocks.Count != 52)
                        //continue;

                        // Don't load small grid
                        //if (objectBuilder.TypeId == MyObjectBuilderTypeEnum.CubeGrid && ((MyObjectBuilder_CubeGrid)objectBuilder).GridSizeEnum != MyCubeSize.Large)
                        //continue;

                        var temporaryEntity = MyEntities.CreateFromObjectBuilderAndAdd(objectBuilder);
                        allEntitiesAdded &= temporaryEntity != null;
                    }
                }

            }
            finally
            {
                MyEntityIdentifier.AllocationSuspended = false;
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }
            return allEntitiesAdded;
        }


        internal static List<MyObjectBuilder_EntityBase> Save()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyEntities.Save");

            List<MyObjectBuilder_EntityBase> list = new List<MyObjectBuilder_EntityBase>();

            foreach (MyEntity entity in m_entities.ToArray())
            {
                if (entity.Save && !m_entitiesToDelete.Contains(entity) && !entity.MarkedForClose)
                {
                    entity.BeforeSave();
                    MyObjectBuilder_EntityBase objBuilder = entity.GetObjectBuilder();
                    Debug.Assert(objBuilder != null, "Save flag specified returns nullable objectbuilder");
                    list.Add(objBuilder);
                }

                // recurse
                var childrenObjectBuilders = GetObjectBuilders(entity.Hierarchy.Children);
                if (childrenObjectBuilders != null) list.AddList(childrenObjectBuilders);
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            return list;
        }


        // Saves the whole hierarchy, but every type needs to resolve parent links on its own (probably in Link)
        private static List<MyObjectBuilder_EntityBase> GetObjectBuilders(List<MyHierarchyComponentBase> components)
        {
            List<MyObjectBuilder_EntityBase> objectBuilders = null;
            if (components != null)
            {
                objectBuilders = new List<MyObjectBuilder_EntityBase>();

                foreach (var comp in components)
                {
                    var entity = comp.Container.Entity;
                    if (entity.Save)
                    {
                        entity.BeforeSave();
                        MyObjectBuilder_EntityBase objBuilder = entity.GetObjectBuilder();
                        Debug.Assert(objBuilder != null, "Save flag specified returns nullable objectbuilder");
                        objectBuilders.Add(objBuilder);
                    }

                    // recurse
                    //var childrenObjectBuilders = GetObjectBuilders(entity.Children);
                    //if (childrenObjectBuilders != null)
                    //    objectBuilders.AddList(childrenObjectBuilders);
                }
            }

            return objectBuilders;
        }

        private struct BoundingBoxDrawArgs
        {
            public Color Color;
            public float LineWidth;
            public Vector3 InflateAmount;
            public string lineMaterial;
        }

        private static Dictionary<MyEntity, BoundingBoxDrawArgs> m_entitiesForBBoxDraw = new Dictionary<MyEntity, BoundingBoxDrawArgs>();
        public static void EnableEntityBoundingBoxDraw(MyEntity entity, bool enable, Vector4? color = null, float lineWidth = 0.01f, Vector3? inflateAmount = null, string lineMaterial = null)
        {
            Debug.Assert(entity != null, "Entity must not be null");
            if (enable)
            {
                if (!m_entitiesForBBoxDraw.ContainsKey(entity))
                    entity.OnClose += entityForBBoxDraw_OnClose;

                m_entitiesForBBoxDraw[entity] = new BoundingBoxDrawArgs()
                {
                    Color = color ?? Vector4.One,
                    LineWidth = lineWidth,
                    InflateAmount = inflateAmount ?? Vector3.Zero,
                    lineMaterial = lineMaterial
                };
            }
            else
            {
                m_entitiesForBBoxDraw.Remove(entity);
                entity.OnClose -= entityForBBoxDraw_OnClose;
            }

        }

        static void entityForBBoxDraw_OnClose(MyEntity entity)
        {
            m_entitiesForBBoxDraw.Remove(entity);
        }



        public static MyEntity CreateAndAddFromDefinition(MyObjectBuilder_EntityBase entityBuilder, Definitions.MyDefinitionId entityDefinition)
        {
            MyEntity entity = new MyEntity(true);

            entity.InitFromDefinition(entityBuilder, entityDefinition);

            MyEntities.Add(entity);

            entity.Physics.ForceActivate();
            entity.Physics.ApplyImpulse(entity.WorldMatrix.Forward * 0.1f, Vector3.Zero);  // applying impulse so it triggers activation etc.

            return entity;
        }

        public static void RaiseEntityCreated(MyEntity entity)
        {
            var createCallback = OnEntityCreate;
            if (createCallback != null)
            {
                createCallback(entity);
            }
        }

    }
}
