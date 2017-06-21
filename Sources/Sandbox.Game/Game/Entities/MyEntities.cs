#region Using

using Havok;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
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
using System.Threading;

using VRage;
using VRage.Collections;
using VRage.Plugins;
using VRageMath;
using VRageRender;
using Sandbox.ModAPI;
using Sandbox.Game.Weapons;
using VRage.Win32;
using VRage.Utils;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using System.Text;
using Sandbox.Game.Components;
using ParallelTasks;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Game.VisualScripting;
using VRage.Library;
using VRage.Profiler;

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
        static MyDistributedUpdater<CachingList<MyEntity>, MyEntity> m_entitiesForUpdate = new MyDistributedUpdater<CachingList<MyEntity>, MyEntity>(1);

        //Entities updated each 10th frame
        static MyDistributedUpdater<CachingList<MyEntity>, MyEntity> m_entitiesForUpdate10 = new MyDistributedUpdater<CachingList<MyEntity>, MyEntity>(10);

        //Entities updated each 100th frame
        static MyDistributedUpdater<CachingList<MyEntity>, MyEntity> m_entitiesForUpdate100 = new MyDistributedUpdater<CachingList<MyEntity>, MyEntity>(100);

        //Entities drawn each frame
        static CachingList<IMyEntity> m_entitiesForDraw = new CachingList<IMyEntity>();

        // Scene data components
        static List<IMySceneComponent> m_sceneComponents = new List<IMySceneComponent>();

        // Helper for remapping of entityIds to new values
        [ThreadStatic]
        static MyEntityIdRemapHelper m_remapHelper;

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

        public static int PendingInits;
        public static EventWaitHandle FinishedProcessingInits = new AutoResetEvent(false);

        #endregion

        static MyEntities()
        {
            var typeOfBlankEntity = typeof(MyEntity);
            var descriptor = typeOfBlankEntity.GetCustomAttribute<MyEntityTypeAttribute>(false);
            MyEntityFactory.RegisterDescriptor(descriptor, typeOfBlankEntity);

#if XB1 // XB1_ALLINONEASSEMBLY
            MyEntityFactory.RegisterDescriptorsFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
            MyEntityFactory.RegisterDescriptorsFromAssembly(Assembly.GetCallingAssembly());
            MyEntityFactory.RegisterDescriptorsFromAssembly(MyPlugins.GameAssembly);
            MyEntityFactory.RegisterDescriptorsFromAssembly(MyPlugins.SandboxAssembly);
            MyEntityFactory.RegisterDescriptorsFromAssembly(MyPlugins.UserAssembly);
#endif // !XB1

            // ------------------ PLEASE READ -------------------------
            // VRAGE TODO: Delegates in MyEntity help us to get rid of sandbox. There are too many dependencies and this was the easy way to cut MyEntity out of sandbox.
            //             These delegates should not last here forever, after complete deletion of sandbox, there should be no reason for them to stay.
            MyEntityExtensions.SetCallbacks();

            MyEntitiesInterface.RegisterUpdate = RegisterForUpdate;
            MyEntitiesInterface.UnregisterUpdate = UnregisterForUpdate;
            MyEntitiesInterface.RegisterDraw = RegisterForDraw;
            MyEntitiesInterface.UnregisterDraw = UnregisterForDraw;
            MyEntitiesInterface.SetEntityName = SetEntityName;
            MyEntitiesInterface.IsUpdateInProgress = IsUpdateInProgress;
            MyEntitiesInterface.IsCloseAllowed = IsCloseAllowed;
            MyEntitiesInterface.RemoveName = RemoveName;
            MyEntitiesInterface.RemoveFromClosedEntities = RemoveFromClosedEntities;
            MyEntitiesInterface.Remove = Remove;
            MyEntitiesInterface.RaiseEntityRemove = RaiseEntityRemove;
            MyEntitiesInterface.Close = Close;
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


        public static bool IsShapePenetrating(HkShape shape, ref Vector3D position, ref Quaternion rotation, int filter = MyPhysics.CollisionLayers.DefaultCollisionLayer)
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

        /// <param name="matrix">Reference frame from which search for a free place</param>
        /// <param name="axis">Axis where to perform a rotation searching for a free place</param>
        public static Vector3D? FindFreePlace(ref MatrixD matrix, Vector3D axis, float radius, int maxTestCount = 20, int testsPerDistance = 5, float stepSize = 1)
        {
            Vector3D forward = matrix.Forward;
            forward.Normalize();
            Vector3D currentPos = matrix.Translation;
            Quaternion rot = Quaternion.Identity;
            HkShape sphere = new HkSphereShape(radius);
            try
            {
                if (MyEntities.IsInsideWorld(currentPos) && !IsShapePenetrating(sphere, ref currentPos, ref rot))
                {
                    bool safe = FindFreePlaceVoxelMap(currentPos, radius, ref sphere, ref currentPos);
                    if (safe)
                        return currentPos;
                }

                int count = (int)Math.Ceiling(maxTestCount / (float)testsPerDistance);
                float angleStep = 2 * (float)Math.PI / testsPerDistance;
                float distance = 0;
                for (int i = 0; i < count; i++)
                {
                    distance += radius * stepSize;
                    Vector3D directionOffset = forward;
                    float angleOffset = 0;
                    for (int j = 0; j < testsPerDistance; j++)
                    {
                        if (j != 0)
                        {
                            angleOffset += angleStep;
                            Quaternion rotation = Quaternion.CreateFromAxisAngle(axis, angleOffset);
                            directionOffset = Vector3D.Transform(forward, rotation);
                        }

                        currentPos = matrix.Translation + directionOffset * distance;
                        if (MyEntities.IsInsideWorld(currentPos) && !IsShapePenetrating(sphere, ref currentPos, ref rot))
                        {
                            // Test voxel maps
                            bool safe = FindFreePlaceVoxelMap(currentPos, radius, ref sphere, ref currentPos);
                            if (safe)
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

        // NOTE: Following method may have the following problems:
        // 1) CorrectSpawnLocation() should be always followed by a second test for
        //    IsShapePenetrating()
        // 2) First overlapping test may result in returning a colliding test sphere with a
        //    physics voxel map (case overlappedVoxelmap != null and not a planet)
        // 3) In second overlapping test, CorrectSpawnLocation() is testing from basePos.
        //    It should probably test from currentPos cause it's the one that is
        //    modified by external cycle
        // 4) In second overlapping test, CorrectSpawnLocation() may have found
        //    a safe position but that won't be spotted and the result will
        //    be corrupted by the external cycle

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
                {
                    BoundingSphereD boundingSphere = new BoundingSphereD(currentPos, radius);
                    MyVoxelBase overlappedVoxelmap = MySession.Static.VoxelMaps.GetOverlappingWithSphere(ref boundingSphere);

                    if (overlappedVoxelmap == null)
                        return currentPos;

                    if (overlappedVoxelmap is MyPlanet)
                    {
                        MyPlanet planet = overlappedVoxelmap as MyPlanet;
                        planet.CorrectSpawnLocation(ref basePos,radius);
                    }     
                    return basePos;
                }

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

                            if (overlappedVoxelmap is MyPlanet)
                            {
                                MyPlanet planet = overlappedVoxelmap as MyPlanet;
                                planet.CorrectSpawnLocation(ref basePos, radius);
                            }                 
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

        public static Vector3D? TestPlaceInSpace(Vector3D basePos, float radius)
        {
            List<MyVoxelBase> voxels = new List<MyVoxelBase>();

            Vector3D currentPos = basePos;
            Quaternion rot = Quaternion.Identity;
            HkShape sphere = new HkSphereShape(radius);
            try
            {
                if (MyEntities.IsInsideWorld(currentPos) && !IsShapePenetrating(sphere, ref currentPos, ref rot))
                {
                    BoundingSphereD boundingSphere = new BoundingSphereD(currentPos, radius);
                    MySession.Static.VoxelMaps.GetAllOverlappingWithSphere(ref boundingSphere, voxels);

                    if (voxels.Count == 0)
                    {
                        return currentPos;
                    }
                    else
                    {
                        //GR: For planets GetAllOverlappingWithSphere is pretty inaccurate and covers large area of empty space
                        //So do custom check for big voxels (planets) manually without raycast. Just check maximum radius of planet for current point
                        //If for at least one planet we are below the maximum radius threshold then do not try to spawn.
                        var ignoreAll = true;
                        foreach (var voxel in voxels)
                        {
                            var planet = voxel as MyPlanet;
                            if (planet == null)
                            {
                                ignoreAll = false;
                                break;
                            }
                            else
                            {
                                var distanceFromPlanet = (currentPos - planet.MaximumRadius).Length();
                                if (distanceFromPlanet < planet.MaximumRadius)
                                {
                                    ignoreAll = false;
                                    break;
                                }
                            }
                        }
                        if (ignoreAll)
                        {
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

        /// <returns>True if it a safe position is found</returns>
        private static bool FindFreePlaceVoxelMap(Vector3D currentPos, float radius, ref HkShape shape, ref Vector3D ret)
        {
            BoundingSphereD boundingSphere = new BoundingSphereD(currentPos, radius);
            MyVoxelBase overlappedVoxelmap = MySession.Static.VoxelMaps.GetOverlappingWithSphere(ref boundingSphere);

            // If a collision with any physics is found, we are just interested in the root voxel map
            overlappedVoxelmap = overlappedVoxelmap == null ? null : overlappedVoxelmap.RootVoxel;
            if (overlappedVoxelmap == null)
            {
                ret = currentPos;
                return true;
            }

            MyPlanet planet = overlappedVoxelmap as MyPlanet;
            if (planet != null)
            {
                bool safe = planet.CorrectSpawnLocation2(ref currentPos, radius);
                Quaternion rot = Quaternion.Identity;
                if (safe)
                {
                    if (!IsShapePenetrating(shape, ref currentPos, ref rot))
                    {
                        ret = currentPos;
                        return true;
                    }

                    // The first attempt to fix the spawn position succeded but
                    // it is now colliding with other objects. Resume the up search
                    // on the planet
                    safe = planet.CorrectSpawnLocation2(ref currentPos, radius, true);
                    if (safe && !IsShapePenetrating(shape, ref currentPos, ref rot))
                    {
                        ret = currentPos;
                        return true;
                    }                    
                }
            }

            return false;
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

            MyPhysics.CastRay(hintPosition, pos, m_hits, MyPhysics.CollisionLayers.DefaultCollisionLayer);

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

        public static List<MyEntity> GetEntitiesInAABB(ref BoundingBoxD boundingBox, bool exact=false)
        {
            MyDebug.AssertDebug(OverlapRBElementList.Count == 0, "Result buffer was not cleared after last use!");
            ProfilerShort.Begin("GetEntitiesInAABB");
            MyGamePruningStructure.GetAllEntitiesInBox(ref boundingBox, OverlapRBElementList);
            if ( exact )
            {
                // game prunning structure returns unaccurate values - we have to filter out results
                for ( int i = 0; i<OverlapRBElementList.Count; )
                {
                    MyEntity entity = OverlapRBElementList[i];
                    // filtering out bad results - like people during last judgement
                    if ( !boundingBox.Intersects(entity.PositionComp.WorldAABB) )
                        // bad one
                        OverlapRBElementList.RemoveAt(i);
                    else
                        // good one
                        i++;
                }
            }
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

        public static void GetTopMostEntitiesInBox(ref BoundingBoxD boundingBox, List<MyEntity> foundElements, MyEntityQueryType qtype = MyEntityQueryType.Both)
        {
            MyGamePruningStructure.GetAllTopMostStaticEntitiesInBox(ref boundingBox, foundElements, qtype);
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
        static public MyConcurrentDictionary<string, MyEntity> m_entityNameDictionary = new MyConcurrentDictionary<string, MyEntity>();

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

            OnEntityRemove = null;
            OnEntityAdd = null;
            OnEntityCreate = null;
            OnEntityDelete = null;

            m_entities = new HashSet<MyEntity>();
            m_entitiesForUpdateOnce = new CachingList<MyEntity>();
            m_entitiesForUpdate = new MyDistributedUpdater<CachingList<MyEntity>, MyEntity>(1);
            m_entitiesForUpdate10 = new MyDistributedUpdater<CachingList<MyEntity>, MyEntity>(10);
            m_entitiesForUpdate100 = new MyDistributedUpdater<CachingList<MyEntity>, MyEntity>(100);
            m_entitiesForDraw = new CachingList<IMyEntity>();
            m_remapHelper = new MyEntityIdRemapHelper();
            m_renderObjectToEntityMap = new Dictionary<uint, IMyEntity>();
            m_entityNameDictionary.Clear();
        }

        //  IMPORTANT: Only adds object to the list. Caller must call Start() or Init() on the object.
        public static void Add(MyEntity entity, bool insertIntoScene = true)
        {
            System.Diagnostics.Debug.Assert(entity.Parent == null, "There are only root entities in MyEntities");
            MySandboxGame.AssertUpdateThread();
            Debug.Assert(Sync.IsServer || !(entity is MyCubeGrid) || entity.SentFromServer || entity.Physics == null, "Entity on client was created without being sent from server.");

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
                RaiseEntityAdd(entity);
            }
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
                Debug.Assert(!m_entityNameDictionary.ContainsKey(myEntity.Name), "MyEntities: Entity names are conflicting. : " + newName);
                if (!m_entityNameDictionary.ContainsKey(myEntity.Name))
                {
                    m_entityNameDictionary.TryAdd(myEntity.Name, myEntity);
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
                RaiseEntityRemove(entity);
            }
        }


        public static FastResourceLock EntityCloseLock = new FastResourceLock();
        public static FastResourceLock EntityMarkForCloseLock = new FastResourceLock();
        public static FastResourceLock UnloadDataLock = new FastResourceLock();
        //public static object EntityCloseLock = new object();

        public static void DeleteRememberedEntities()
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


        public static bool HasEntitiesToDelete()
        {
            return m_entitiesToDelete.Count > 0;
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
            m_entitiesForUpdate.List.ApplyRemovals();
            m_entitiesForUpdate10.List.ApplyRemovals();
            m_entitiesForUpdate100.List.ApplyRemovals();

            CloseAllowed = false;
            m_entitiesToDelete.Clear();

            // Clear all
            MyEntityIdentifier.Clear();

            MyGamePruningStructure.Clear();
            MyRadioBroadcasters.Clear();

            m_entitiesForUpdateOnce.DebugCheckEmpty();
            m_entitiesForUpdate.List.DebugCheckEmpty();
            m_entitiesForUpdate10.List.DebugCheckEmpty();
            m_entitiesForUpdate100.List.DebugCheckEmpty();
            m_entitiesForDraw.ApplyChanges();
            Debug.Assert(m_entitiesForDraw.Count == 0);
        }

        public static void RegisterForUpdate(MyEntity entity)
        {
            if ((entity.NeedsUpdate & MyEntityUpdateEnum.BEFORE_NEXT_FRAME) > 0)
            {
                m_entitiesForUpdateOnce.Add(entity);
            }
            if ((entity.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) > 0)
            {
                m_entitiesForUpdate.List.Add(entity);
            }
            if ((entity.NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) > 0)
            {
                m_entitiesForUpdate10.List.Add(entity);
            }
            if ((entity.NeedsUpdate & MyEntityUpdateEnum.EACH_100TH_FRAME) > 0)
            {
                m_entitiesForUpdate100.List.Add(entity);
            }
        }

        public static void RegisterForDraw(IMyEntity entity)
        {
            if (entity.Render.NeedsDraw)
            {
                m_entitiesForDraw.Add(entity);
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
                m_entitiesForUpdate.List.Remove(entity, immediate);
            }

            if ((entity.Flags & EntityFlags.NeedsUpdate10) != 0)
            {
                m_entitiesForUpdate10.List.Remove(entity, immediate);
            }

            if ((entity.Flags & EntityFlags.NeedsUpdate100) != 0)
            {
                m_entitiesForUpdate100.List.Remove(entity, immediate);
            }
        }

        public static void UnregisterForDraw(IMyEntity entity)
        {
            m_entitiesForDraw.Remove(entity);
        }


        public static bool UpdateInProgress = false;
        public static bool CloseAllowed = false;

        static int m_update10Index = 0;
        static int m_update100Index = 0;
        static float m_update10Count = 0;
        static float m_update100Count = 0;


        public static bool IsUpdateInProgress() { return UpdateInProgress; }
        public static bool IsCloseAllowed() { return CloseAllowed; }

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
                m_entitiesForUpdate.List.ApplyChanges();
                m_entitiesForUpdate.Update();
                MySimpleProfiler.Begin("Blocks");
                m_entitiesForUpdate.Iterate((x) =>
                {
                    ProfilerShort.Begin(x.GetType().Name);
                    if (x.MarkedForClose == false)
                    {
                        x.UpdateBeforeSimulation();
                    }
                    ProfilerShort.End();
                });

                ProfilerShort.BeginNextBlock("10th update");
                m_entitiesForUpdate10.List.ApplyChanges();
                m_entitiesForUpdate10.Update();
                m_entitiesForUpdate10.Iterate((x) => 
                {
                    string typeName = x.GetType().Name;
                    ProfilerShort.Begin(typeName);
                    if (x.MarkedForClose == false)
                    {
                        x.UpdateBeforeSimulation10();
                    }
                    ProfilerShort.End();
                });

                
                ProfilerShort.BeginNextBlock("100th update");
                m_entitiesForUpdate100.List.ApplyChanges();
                m_entitiesForUpdate100.Update();
                m_entitiesForUpdate100.Iterate((x) =>
                {
                    string typeName = x.GetType().Name;
                    ProfilerShort.Begin(typeName);
                    if (x.MarkedForClose == false)
                    {
                        x.UpdateBeforeSimulation100();
                    }
                    ProfilerShort.End();
                });

                ProfilerShort.End();
            }
            MySimpleProfiler.End("Blocks");

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
                m_entitiesForUpdate.List.ApplyChanges();
                MySimpleProfiler.Begin("Blocks");
                m_entitiesForUpdate.Iterate((x) =>
                {
                    string typeName = x.GetType().Name;
                    ProfilerShort.Begin(typeName);
                    if (x.MarkedForClose == false)
                    {
                        x.UpdateAfterSimulation();
                    }
                    ProfilerShort.End();
                });
                ProfilerShort.End();

                ProfilerShort.Begin("UpdateAfter10");
                m_entitiesForUpdate10.List.ApplyChanges();
                m_entitiesForUpdate10.Iterate((x) =>
                    {
                        string typeName = x.GetType().Name;
                        ProfilerShort.Begin(typeName);
                        if (x.MarkedForClose == false)
                        {
                            x.UpdateAfterSimulation10();
                        }
                        ProfilerShort.End();
                    });
                ProfilerShort.End();

                ProfilerShort.Begin("UpdateAfter100");
                m_entitiesForUpdate100.List.ApplyChanges();
                m_entitiesForUpdate100.Iterate((x) =>
                {
                    string typeName = x.GetType().Name;
                    ProfilerShort.Begin(typeName);
                    if (x.MarkedForClose == false)
                    {
                        x.UpdateAfterSimulation100();
                    }
                    ProfilerShort.End();
                });
                ProfilerShort.End();
                MySimpleProfiler.End("Blocks");

                UpdateInProgress = false;

                DeleteRememberedEntities();
            }

            while (m_creationThread.ConsumeResult()) ; // Add entities created asynchronously

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void UpdatingStopped()
        {
            for (int i = 0; i < m_entitiesForUpdate.List.Count; i++)
            {
                MyEntity entity = m_entitiesForUpdate.List[i];

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
            MySimpleProfiler.Begin("Render");
            ProfilerShort.Begin("Each draw");
            m_entitiesForDraw.ApplyChanges();

            foreach (MyEntity entity in m_entitiesForDraw)
            {
                entity.PrepareForDraw();

                if (entity.Render.NeedsDrawFromParent == false && IsAnyRenderObjectVisible(entity))
                {
                    string typeName = entity.GetType().Name;
                    //ProfilerShort.Begin(Partition.Select(typeName.GetHashCode(), "Part1", "Part2", "Part3"));
                    ProfilerShort.Begin(typeName);
                    entity.Render.Draw();
                    ProfilerShort.End();
                    //ProfilerShort.End();
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
            MySimpleProfiler.End("Render");
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
        public static VRage.Game.Models.MyIntersectionResultLineTriangleEx? GetIntersectionWithLine(ref LineD line, MyEntity ignoreEntity0, MyEntity ignoreEntity1, bool ignoreChildren = false, bool ignoreFloatingObjects = true, bool ignoreHandWeapons = true, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES, float timeFrame = 0, bool ignoreObjectsWithoutPhysics = true)
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

            VRage.Game.Models.MyIntersectionResultLineTriangleEx? ret = null;
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
                if (ignoreObjectsWithoutPhysics && (entity.Physics == null || !entity.Physics.Enabled)) continue;

                if (entity.MarkedForClose) continue;

                if (ignoreFloatingObjects && (entity is MyFloatingObject || entity is Debris.MyDebrisBase))
                    continue;

                if (ignoreHandWeapons && ((entity is IMyHandheldGunObject<MyDeviceBase>) || (entity.Parent is IMyHandheldGunObject<MyDeviceBase>)))
                    continue;



                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("GetIntersectionWithLine.GetIntersectionWithLine");
                VRage.Game.Models.MyIntersectionResultLineTriangleEx? testResultEx = null;

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
                    ret = VRage.Game.Models.MyIntersectionResultLineTriangleEx.GetCloserIntersection(ref ret, ref testResultEx);
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

        public static MyEntity GetEntityByIdOrDefault(long entityId, MyEntity defaultValue = null)
        {
            IMyEntity result;
            MyEntityIdentifier.TryGetEntity(entityId, out result);
            return (result as MyEntity) ?? defaultValue;
        }

        public static T GetEntityByIdOrDefault<T>(long entityId, T defaultValue = null)
            where T : MyEntity
        {
            IMyEntity result;
            MyEntityIdentifier.TryGetEntity(entityId, out result);
            return (result as T) ?? defaultValue;
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

        static Dictionary<string, int> m_typesStats = new Dictionary<string, int>();

        #endregion

        static public void DebugDrawStatistics()
        {
            m_typesStats.Clear();

            Vector2 offset = new Vector2(100, 0);
            MyRenderProxy.DebugDrawText2D(offset, "Detailed entity statistics", Color.Yellow, 1);

            foreach (MyEntity entity in m_entitiesForUpdate.List)
            {
                string ts = entity.GetType().Name.ToString();
                if (!m_typesStats.ContainsKey(ts))
                    m_typesStats.Add(ts, 0);
                m_typesStats[ts]++;
            }

            float scale = 0.7f;
            offset.Y += 50;
            MyRenderProxy.DebugDrawText2D(offset, "Entities for update:", Color.Yellow, scale);
            offset.Y += 30;
            foreach (KeyValuePair<string, int> pair in m_typesStats.OrderByDescending(x => x.Value))
            {
                MyRenderProxy.DebugDrawText2D(offset, pair.Key + ": " + pair.Value.ToString() + "x", Color.Yellow, scale);
                offset.Y += 20;
            }
            m_typesStats.Clear();
            offset.Y = 0;


            foreach (MyEntity entity in m_entitiesForUpdate10.List)
            {
                string ts = entity.GetType().Name.ToString();
                if (!m_typesStats.ContainsKey(ts))
                    m_typesStats.Add(ts, 0);
                m_typesStats[ts]++;
            }

            offset.X += 300;
            offset.Y += 50;
            MyRenderProxy.DebugDrawText2D(offset, "Entities for update10:", Color.Yellow, scale);
            offset.Y += 30;
            foreach (KeyValuePair<string, int> pair in m_typesStats.OrderByDescending(x => x.Value))
            {
                MyRenderProxy.DebugDrawText2D(offset, pair.Key + ": " + pair.Value.ToString() + "x", Color.Yellow, scale);
                offset.Y += 20;
            }
            m_typesStats.Clear();
            offset.Y = 0;


            foreach (MyEntity entity in m_entitiesForUpdate100.List)
            {
                string ts = entity.GetType().Name.ToString();
                if (!m_typesStats.ContainsKey(ts))
                    m_typesStats.Add(ts, 0);
                m_typesStats[ts]++;
            }

            offset.X += 300;
            offset.Y += 50;
            MyRenderProxy.DebugDrawText2D(offset, "Entities for update100:", Color.Yellow, scale);
            offset.Y += 30;
            foreach (KeyValuePair<string, int> pair in m_typesStats.OrderByDescending(x => x.Value))
            {
                MyRenderProxy.DebugDrawText2D(offset, pair.Key + ": " + pair.Value.ToString() + "x", Color.Yellow, scale);
                offset.Y += 20;
            }
            m_typesStats.Clear();
            offset.Y = 0;


            foreach (MyEntity entity in m_entities)
            {
                string ts = entity.GetType().Name.ToString();
                if (!m_typesStats.ContainsKey(ts))
                    m_typesStats.Add(ts, 0);
                m_typesStats[ts]++;
            }

            offset.X += 300;
            offset.Y += 50;
            scale = 0.7f;
            offset.Y += 50;
            MyRenderProxy.DebugDrawText2D(offset, "All entities:", Color.Yellow, scale);
            offset.Y += 30;
            foreach (KeyValuePair<string, int> pair in m_typesStats.OrderByDescending(x => x.Value))
            {
                MyRenderProxy.DebugDrawText2D(offset, pair.Key + ": " + pair.Value.ToString() + "x", Color.Yellow, scale);
                offset.Y += 20;
            }
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

            if (MyCubeGridGroups.Static != null)
            {
                if (MyDebugDrawSettings.DEBUG_DRAW_GRID_GROUPS_PHYSICAL)
                {
                    DebugDrawGroups(MyCubeGridGroups.Static.Physical);
                }
                if (MyDebugDrawSettings.DEBUG_DRAW_GRID_GROUPS_LOGICAL)
                {
                    DebugDrawGroups(MyCubeGridGroups.Static.Logical);
                }
                if (MyDebugDrawSettings.DEBUG_DRAW_SMALL_TO_LARGE_BLOCK_GROUPS)
                {
                    MyCubeGridGroups.DebugDrawBlockGroups(MyCubeGridGroups.Static.SmallToLargeBlockConnections);
                }
                if (MyDebugDrawSettings.DEBUG_DRAW_DYNAMIC_PHYSICAL_GROUPS)
                {
                    DebugDrawGroups(MyCubeGridGroups.Static.PhysicalDynamic);
                }
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

                    // Add empty entities -- these cannot be classified as not visible by render proxy
                    // no render id.
                    foreach (var entity in m_entities)
                    {
                        if (entity.DefinitionId == null || entity.Render.GetModel() == null)
                        {
                            m_entitiesForDebugDraw.Add(entity);
                        }
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

            if (MyDebugDrawSettings.DEBUG_DRAW_ENTITY_STATISTICS)
            {
                DebugDrawStatistics();
            }



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
                //by Gregory: added Check if Entity.Id == 0.
                //means that save is corrupted and not all entities will be loaded but at least the save game will run with warning message.
                //Mostly for compatibility with old save games
                if (retVal.EntityId == 0)
                {
                    //If set to null a waning will be printed disabled that now cause got a lot fo Entities with EntityId = 0.
                    retVal = null;
                }
                else
                {
                    Add(retVal, insertIntoScene);
                }
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

        public static void InitAsync(MyEntity entity, MyObjectBuilder_EntityBase objectBuilder, bool addToScene, Action<MyEntity> doneHandler,List<MyObjectBuilder_EntityBase> subBuilders)
        {
            Debug.Assert(m_creationThread != null, "Creation thread is null, unloading?");
            if (m_creationThread != null)
            {
                m_creationThread.SubmitWork(objectBuilder, addToScene, doneHandler, entity, subBuilders);
            }
        }

        public static void CallAsync(MyEntity entity, Action<MyEntity> doneHandler)
        {
            InitAsync(entity, null, false, doneHandler);
        }

        public static void CallAsync(Action doneHandler)
        {
            InitAsync(null, null, false, (e) => doneHandler());
        }

        public static bool MemoryLimitReached
        {
            get
            {
                if (!MyEnvironment.Is64BitProcess && MySandboxGame.Config.MemoryLimits)
#if !XB1
                    return GC.GetTotalMemory(false) > EntityManagedMemoryLimit || WinApi.WorkingSet > EntityNativeMemoryLimit;
#else // XB1
                {
                    System.Diagnostics.Debug.Assert(false, "XB1 TODO?");
                    return false;
                }
#endif // XB1
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
            if (m_remapHelper == null)
                m_remapHelper = new MyEntityIdRemapHelper();
            foreach (var objectBuilder in objectBuilders)
                objectBuilder.Remap(m_remapHelper);
            m_remapHelper.Clear();
        }

        public static void RemapObjectBuilder(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (m_remapHelper == null)
                m_remapHelper = new MyEntityIdRemapHelper();
            objectBuilder.Remap(m_remapHelper);
            m_remapHelper.Clear();
        }

        public static MyEntity CreateFromObjectBuilderNoinit(MyObjectBuilder_EntityBase objectBuilder, bool readyForReplication = true)
        {
            if ((objectBuilder.TypeId == typeof(MyObjectBuilder_CubeGrid) || objectBuilder.TypeId == typeof(MyObjectBuilder_VoxelMap)) && !MyEntities.IgnoreMemoryLimits && MemoryLimitReachedReport)
            {
                MemoryLimitAddFailure = true;
                MySandboxGame.Log.WriteLine("WARNING: MemoryLimitAddFailure reached");
                return null;
            }

            return MyEntityFactory.CreateEntity(objectBuilder, readyForReplication);
        }

        /// <summary>
        /// Holds data for asynchronous entity init
        /// </summary>
        public class InitEntityData : ParallelTasks.WorkData
        {
            MyObjectBuilder_EntityBase m_objectBuilder;
            bool m_addToScene;
            Action m_completionCallback;
            MyEntity m_entity;
            List<IMyEntity> m_resultIDs;
            bool m_callbackNeedsReplicable;

            public InitEntityData(MyObjectBuilder_EntityBase objectBuilder, bool addToScene, Action completionCallback, MyEntity entity, bool callbackNeedsReplicable)
            {
                m_objectBuilder = objectBuilder;
                m_addToScene = addToScene;
                m_completionCallback = completionCallback;
                m_entity = entity;
                m_callbackNeedsReplicable = callbackNeedsReplicable;
            }

            public void CallInitEntity()
            {
                try
                {
                    MyEntityIdentifier.LazyInitPerThreadStorage(2048);
                    InitEntity(m_objectBuilder, ref m_entity);
                }
                finally
                {
                    m_resultIDs = new List<IMyEntity>();
                    MyEntityIdentifier.GetPerThreadEntities(m_resultIDs);
                    MyEntityIdentifier.ClearPerThreadEntities();
                    Interlocked.Decrement(ref PendingInits);
                    if (PendingInits <= 0)
                        FinishedProcessingInits.Set();
                }
            }

            public void OnEntityInitialized()
            {
                foreach (var entity in m_resultIDs)
                {
                    IMyEntity foundEntity;
                    MyEntityIdentifier.TryGetEntity(entity.EntityId, out foundEntity);
                    if (foundEntity == null)
                        MyEntityIdentifier.AddEntityWithId(entity);
                    else
                        Debug.Fail("Two threads added the same entity");
                }
                if (m_addToScene)
                {
                    bool insertIntoScene = (int)(m_objectBuilder.PersistentFlags & MyPersistentEntityFlags2.InScene) > 0;
                    if (m_entity != null && m_entity.EntityId != 0)
                    {
                        Add(m_entity, insertIntoScene);

                        if (m_callbackNeedsReplicable)
                            SetReadyForReplication(m_entity);

                        if (m_completionCallback != null)
                            m_completionCallback();

                        if (!m_callbackNeedsReplicable)
                            SetReadyForReplication(m_entity);
                    }
                }
            }

            void SetReadyForReplication(MyEntity entity)
            {
                entity.IsReadyForReplication = true;

                foreach (var child in entity.Hierarchy.Children)
                {
                    SetReadyForReplication((MyEntity)child.Entity);
                }
            }

        }


        /// <summary>
        /// Create and asynchronously initialize and entity.
        /// </summary>
        /// <param name="completionCallback">Callback when the entity is initialized</param>
        /// <param name="entity">Already created entity you only want to init</param>
        public static MyEntity CreateFromObjectBuilderParallel(MyObjectBuilder_EntityBase objectBuilder, bool addToScene = false, Action completionCallback = null, MyEntity entity = null, bool callbackNeedsReplicable = false)
        {
            if (entity == null)
            {
                entity = CreateFromObjectBuilderNoinit(objectBuilder, false);
                if (entity == null)
                    return null;
            }

            InitEntityData initData = new InitEntityData(objectBuilder, addToScene, completionCallback, entity, callbackNeedsReplicable);
            Interlocked.Increment(ref PendingInits);
            Parallel.Start(CallInitEntity, OnEntityInitialized, initData);
            return entity;
        }

        private static void CallInitEntity(WorkData workData)
        {
            
            InitEntityData initData = workData as InitEntityData;
            if (initData == null)
            {
                workData.FlagAsFailed();
                return;
            }
            initData.CallInitEntity();
        }

        private static void OnEntityInitialized(WorkData workData)
        {

            InitEntityData initData = workData as InitEntityData;
            if (initData == null)
            {
                workData.FlagAsFailed();
                return;
            }
            initData.OnEntityInitialized();
        }

        public static MyEntity CreateFromObjectBuilder(MyObjectBuilder_EntityBase objectBuilder, bool readyForReplication = true)
        {
            MyEntity entity = CreateFromObjectBuilderNoinit(objectBuilder, readyForReplication);
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
                    for (int i = 0; i<objectBuilders.Count; i++)
                    {
                        MyObjectBuilder_EntityBase objectBuilder = objectBuilders[i];

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

                        var temporaryEntity = MyEntities.CreateFromObjectBuilderParallel(objectBuilder, true);

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
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            return list;
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

        public static MyEntity CreateFromComponentContainerDefinitionAndAdd(MyDefinitionId entityContainerDefinitionId, bool insertIntoScene = true)
        {
            // Check type
            Debug.Assert(typeof(MyObjectBuilder_EntityBase).IsAssignableFrom(entityContainerDefinitionId.TypeId));
            if (!typeof(MyObjectBuilder_EntityBase).IsAssignableFrom(entityContainerDefinitionId.TypeId))
            {
                Debug.Fail("Invalid entity object builder type");
                return null;
            }

            // Check existing container definition
            MyContainerDefinition definition;
            if (!MyComponentContainerExtension.TryGetContainerDefinition(entityContainerDefinitionId.TypeId, entityContainerDefinitionId.SubtypeId, out definition))
            {
                Debug.Fail("Entity container definition not found");
                MySandboxGame.Log.WriteLine("Entity container definition not found: " + entityContainerDefinitionId);
                return null;
            }

            // Create builder
            MyObjectBuilder_EntityBase entityBuilder = MyObjectBuilderSerializer.CreateNewObject(entityContainerDefinitionId.TypeId, entityContainerDefinitionId.SubtypeName) as MyObjectBuilder_EntityBase;
            Debug.Assert(entityBuilder != null);
            if (entityBuilder == null) 
            {
                Debug.Fail("Invalid entity object builder type");
                MySandboxGame.Log.WriteLine("Entity builder was not created: " + entityContainerDefinitionId);
                return null;
            }

            // TODO: remove this - should be somewhere in definition of container
            if (insertIntoScene)
                entityBuilder.PersistentFlags |= MyPersistentEntityFlags2.InScene;

            var entity = MyEntities.CreateFromObjectBuilderAndAdd(entityBuilder);
            Debug.Assert(entity != null, "Entity wasn't created!");
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

        /// <summary>
        /// This method will try to retrieve a definition of components container of the entity and create the type of the entity.
        /// This wi
        /// </summary>
        /// <param name="entityContainerId">This is the id of container definition</param>
        /// <param name="setPosAndRot">Set true if want to set entity position, orientation</param>
        /// <returns></returns>
        public static MyEntity CreateEntityAndAdd(MyDefinitionId entityContainerId, bool setPosAndRot = false, Vector3? position = null, Vector3? up = null, Vector3? forward = null)
        {
            MyContainerDefinition definition;
            if (MyDefinitionManager.Static.TryGetContainerDefinition( entityContainerId, out definition))
            {
                var ob = MyObjectBuilderSerializer.CreateNewObject( entityContainerId) as MyObjectBuilder_EntityBase;

                if (ob != null)
                {
                    if (setPosAndRot)
                    {
                        ob.PositionAndOrientation = new VRage.MyPositionAndOrientation(position.HasValue ? position.Value : Vector3.Zero, forward.HasValue ? forward.Value : Vector3.Forward, up.HasValue ? up.Value : Vector3.Up);
                    }

                    var entity = MyEntities.CreateFromObjectBuilderAndAdd(ob);
                    Debug.Assert(entity != null, "Entity wasn't created!");
                    return entity;
                }
                else
                {
                    System.Diagnostics.Debug.Fail("Entity Creation Error: Couldn't create an object builder and cast is as MyObjectBuilder_EntityBase");
                }

                return null;
            }
            return null;
        }

        public static MyEntity CreateEntity(MyDefinitionId entityContainerId, bool setPosAndRot = false, Vector3? position = null, Vector3? up = null, Vector3? forward = null)
        {
            MyContainerDefinition definition;
            if (MyDefinitionManager.Static.TryGetContainerDefinition(entityContainerId, out definition))
            {
                var ob = MyObjectBuilderSerializer.CreateNewObject(entityContainerId) as MyObjectBuilder_EntityBase;

                if (ob != null)
                {
                    if (setPosAndRot)
                    {
                        ob.PositionAndOrientation = new VRage.MyPositionAndOrientation(position.HasValue ? position.Value : Vector3.Zero, forward.HasValue ? forward.Value : Vector3.Forward, up.HasValue ? up.Value : Vector3.Up);
    }

                    var entity = MyEntities.CreateFromObjectBuilder(ob);
                    Debug.Assert(entity != null, "Entity wasn't created!");
                    return entity;
                }
                else
                {
                    System.Diagnostics.Debug.Fail("Entity Creation Error: Couldn't create an object builder and cast is as MyObjectBuilder_EntityBase");
                }

                return null;
            }
            return null;
        }
    }
}
