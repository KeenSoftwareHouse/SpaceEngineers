#region Using

using System.Diagnostics;
using Sandbox.Engine.Physics;
using VRageMath;

using Sandbox.Game.Entities;
using Sandbox.Engine.Utils;
using VRage.Utils;
using System.Linq;
using System.Collections.Generic;

using VRageRender;
using Sandbox.Game.World;
using Sandbox.Engine.Models;

using Havok;
using System.Threading;
using Sandbox.Common;
using Sandbox.Graphics;
using Sandbox.Game;
using VRage;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using Sandbox.Game.Multiplayer;


#endregion

namespace Sandbox.Engine.Physics
{
    //using MyHavokCluster = VRageMath.Spatial.MyClusterTree<HkWorld>;
    using MyHavokCluster = VRageMath.Spatial.MyClusterTree;

    [MySessionComponentDescriptor(MyUpdateOrder.Simulation, 500)]
    public class MyPhysics : MySessionComponentBase
    {
        public struct HitInfo
        {
            public HkWorld.HitInfo HkHitInfo;
            public Vector3D Position;
        }

        public struct MyContactPointEvent
        {
            public HkContactPointEvent ContactPointEvent;
            public Vector3D Position;
        }

        public struct FractureImpactDetails
        {
            public HkdFractureImpactDetails Details;
            public HkWorld World;
            public Vector3D ContactInWorld;
            public MyEntity Entity;
        }
        public const int StaticCollisionLayer = 13;
        public const int CollideWithStaticLayer = 14;
        public const int DefaultCollisionLayer = 15;
        public const int DynamicDoubledCollisionLayer = 16;
        public const int KinematicDoubledCollisionLayer = 17;
        public const int CharacterCollisionLayer = 18;
        public const int NoCollisionLayer = 19;

        public const int DebrisCollisionLayer = 20;
        public const int GravityPhantomLayer = 21;

        public const int CharacterNetworkCollisionLayer = 22;
        public const int FloatingObjectCollisionLayer = 23;

        public const int ObjectDetectionCollisionLayer = 24;

        public const int VirtualMassLayer = 25;
        public const int CollectorCollisionLayer = 26;

        public const int AmmoLayer = 27;

        public const int ExplosionRaycastLayer = 29;
        public const int CollisionLayerWithoutCharacter = 30;

        // TODO: This layer should be removed, when character won't need both CharacterProxy's body with ragdoll enabled at one time i.e. jetpack
        public const int RagdollCollisionLayer = 31;

        public static int ThreadId;

        public static MyHavokCluster Clusters;

        private static HkJobThreadPool m_threadPool;
        private static HkJobQueue m_jobQueue;

        private List<HkRigidBody> m_iterationBodies = new List<HkRigidBody>();
        private List<HkCharacterRigidBody> m_characterIterationBodies = new List<HkCharacterRigidBody>();
        private static List<MyEntity> m_tmpEntityResults = new List<MyEntity>();

        private static Queue<long> m_timestamps = new Queue<long>(120);

        /// <summary>
        /// Number of physics steps done in last second
        /// </summary>
        public static int StepsLastSecond { get { return m_timestamps.Count; } }

        /// <summary>
        /// Simulation ratio, when physics cannot keep up, this is smaller than 1
        /// </summary>
        public static float SimulationRatio
        {
            get 
            {
                return Math.Max(0.5f, StepsLastSecond) / (float)MyEngineConstants.UPDATE_STEPS_PER_SECOND;
            }
        }

        public static float RestingVelocity
        {
            get
            {
                return MyPerGameSettings.BallFriendlyPhysics ? 3 : float.MaxValue;
            }
        }

        static void InitCollisionFilters(HkWorld world)
        {
            // Floating objects intentionally collide with both DynamicDoubledCollisionLayer and KinematicDoubledCollisionLayer
            // DynamicDoubledCollisionLayer is necessary, because we won't deformations
            // KinematicDoubledCollisionLayer is necessary, because we don't want to push it when floating object is resting on it

            world.DisableCollisionsBetween(DynamicDoubledCollisionLayer, KinematicDoubledCollisionLayer);
            world.DisableCollisionsBetween(DynamicDoubledCollisionLayer, CharacterCollisionLayer);
            world.DisableCollisionsBetween(DynamicDoubledCollisionLayer, DebrisCollisionLayer);
            world.DisableCollisionsBetween(DynamicDoubledCollisionLayer, CharacterNetworkCollisionLayer);

            world.DisableCollisionsBetween(KinematicDoubledCollisionLayer, DefaultCollisionLayer);
            world.DisableCollisionsBetween(KinematicDoubledCollisionLayer, StaticCollisionLayer);
            //world.DisableCollisionsBetween(KinematicDoubledCollisionLayer, AmmoLayer);

            world.DisableCollisionsBetween(GravityPhantomLayer, StaticCollisionLayer);
            world.DisableCollisionsBetween(GravityPhantomLayer, DefaultCollisionLayer);
            world.DisableCollisionsBetween(GravityPhantomLayer, DynamicDoubledCollisionLayer);
            world.DisableCollisionsBetween(GravityPhantomLayer, KinematicDoubledCollisionLayer);
            world.DisableCollisionsBetween(GravityPhantomLayer, CharacterCollisionLayer);
            world.DisableCollisionsBetween(GravityPhantomLayer, CharacterNetworkCollisionLayer);
            world.DisableCollisionsBetween(GravityPhantomLayer, ObjectDetectionCollisionLayer);
            //world.DisableCollisionsBetween(GravityPhantomLayer, AmmoLayer);

            world.DisableCollisionsBetween(VirtualMassLayer, StaticCollisionLayer);
            world.DisableCollisionsBetween(VirtualMassLayer, DefaultCollisionLayer);
            world.DisableCollisionsBetween(VirtualMassLayer, CharacterCollisionLayer);
            world.DisableCollisionsBetween(VirtualMassLayer, CharacterNetworkCollisionLayer);
            world.DisableCollisionsBetween(VirtualMassLayer, DynamicDoubledCollisionLayer);
            world.DisableCollisionsBetween(VirtualMassLayer, KinematicDoubledCollisionLayer);
            world.DisableCollisionsBetween(VirtualMassLayer, DebrisCollisionLayer);
            world.DisableCollisionsBetween(VirtualMassLayer, FloatingObjectCollisionLayer);
            world.DisableCollisionsBetween(VirtualMassLayer, ObjectDetectionCollisionLayer);
            world.DisableCollisionsBetween(VirtualMassLayer, VirtualMassLayer);
            //world.DisableCollisionsBetween(VirtualMassLayer, AmmoLayer);

            world.DisableCollisionsBetween(NoCollisionLayer, StaticCollisionLayer);
            world.DisableCollisionsBetween(NoCollisionLayer, DefaultCollisionLayer);
            world.DisableCollisionsBetween(NoCollisionLayer, CharacterCollisionLayer);
            world.DisableCollisionsBetween(NoCollisionLayer, CharacterNetworkCollisionLayer);
            world.DisableCollisionsBetween(NoCollisionLayer, DynamicDoubledCollisionLayer);
            world.DisableCollisionsBetween(NoCollisionLayer, KinematicDoubledCollisionLayer);
            world.DisableCollisionsBetween(NoCollisionLayer, DebrisCollisionLayer);
            world.DisableCollisionsBetween(NoCollisionLayer, FloatingObjectCollisionLayer);
            world.DisableCollisionsBetween(NoCollisionLayer, GravityPhantomLayer);
            world.DisableCollisionsBetween(NoCollisionLayer, ObjectDetectionCollisionLayer);
            world.DisableCollisionsBetween(NoCollisionLayer, VirtualMassLayer);

            if (MyPerGameSettings.PhysicsNoCollisionLayerWithDefault)
                world.DisableCollisionsBetween(NoCollisionLayer, 0);
            //world.DisableCollisionsBetween(NoCollisionLayer, AmmoLayer);

            world.DisableCollisionsBetween(ObjectDetectionCollisionLayer, ObjectDetectionCollisionLayer);

            world.DisableCollisionsBetween(CollectorCollisionLayer, StaticCollisionLayer);
            world.DisableCollisionsBetween(CollectorCollisionLayer, DefaultCollisionLayer);
            world.DisableCollisionsBetween(CollectorCollisionLayer, CharacterCollisionLayer);
            world.DisableCollisionsBetween(CollectorCollisionLayer, CharacterNetworkCollisionLayer);
            world.DisableCollisionsBetween(CollectorCollisionLayer, DynamicDoubledCollisionLayer);
            world.DisableCollisionsBetween(CollectorCollisionLayer, KinematicDoubledCollisionLayer);
            world.DisableCollisionsBetween(CollectorCollisionLayer, DebrisCollisionLayer);
            world.DisableCollisionsBetween(CollectorCollisionLayer, GravityPhantomLayer);
            world.DisableCollisionsBetween(CollectorCollisionLayer, ObjectDetectionCollisionLayer);
            world.DisableCollisionsBetween(CollectorCollisionLayer, VirtualMassLayer);
            //world.DisableCollisionsBetween(CollectorCollisionLayer, AmmoLayer);

            if (!Sandbox.Game.Multiplayer.Sync.IsServer)
            {
                world.DisableCollisionsBetween(DefaultCollisionLayer, CharacterNetworkCollisionLayer);
                world.DisableCollisionsBetween(StaticCollisionLayer, CharacterNetworkCollisionLayer);
            }

            if (!MyFakes.ENABLE_CHARACTER_AND_DEBRIS_COLLISIONS)
            {
                world.DisableCollisionsBetween(DebrisCollisionLayer, CharacterCollisionLayer);
                world.DisableCollisionsBetween(DebrisCollisionLayer, CharacterNetworkCollisionLayer);
            }

            //Disable collisions with anything but ships, stations and voxels
            world.DisableCollisionsBetween(ExplosionRaycastLayer, CharacterCollisionLayer);
            world.DisableCollisionsBetween(ExplosionRaycastLayer, NoCollisionLayer);
            world.DisableCollisionsBetween(ExplosionRaycastLayer, DebrisCollisionLayer);
            world.DisableCollisionsBetween(ExplosionRaycastLayer, GravityPhantomLayer);
            world.DisableCollisionsBetween(ExplosionRaycastLayer, CharacterNetworkCollisionLayer);
            world.DisableCollisionsBetween(ExplosionRaycastLayer, FloatingObjectCollisionLayer);
            world.DisableCollisionsBetween(ExplosionRaycastLayer, ObjectDetectionCollisionLayer);
            world.DisableCollisionsBetween(ExplosionRaycastLayer, VirtualMassLayer);
            world.DisableCollisionsBetween(ExplosionRaycastLayer, CollectorCollisionLayer);
            world.DisableCollisionsBetween(ExplosionRaycastLayer, AmmoLayer);

            world.DisableCollisionsBetween(CollisionLayerWithoutCharacter, CharacterCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayerWithoutCharacter, NoCollisionLayer);

            world.DisableCollisionsBetween(CollideWithStaticLayer, CollideWithStaticLayer);
            world.DisableCollisionsBetween(CollideWithStaticLayer, DynamicDoubledCollisionLayer);
            world.DisableCollisionsBetween(CollideWithStaticLayer, DefaultCollisionLayer);
            world.DisableCollisionsBetween(CollideWithStaticLayer, CharacterCollisionLayer);
            world.DisableCollisionsBetween(CollideWithStaticLayer, NoCollisionLayer);
            world.DisableCollisionsBetween(CollideWithStaticLayer, DebrisCollisionLayer);
            world.DisableCollisionsBetween(CollideWithStaticLayer, GravityPhantomLayer);
            world.DisableCollisionsBetween(CollideWithStaticLayer, CharacterNetworkCollisionLayer);
            world.DisableCollisionsBetween(CollideWithStaticLayer, FloatingObjectCollisionLayer);
            world.DisableCollisionsBetween(CollideWithStaticLayer, ObjectDetectionCollisionLayer);
            world.DisableCollisionsBetween(CollideWithStaticLayer, VirtualMassLayer);
            world.DisableCollisionsBetween(CollideWithStaticLayer, CollectorCollisionLayer);
            world.DisableCollisionsBetween(CollideWithStaticLayer, AmmoLayer);


            // TODO: This should be removed, when ragdoll won't need to be simulated on separate layer when partial simulation is enabled
            world.DisableCollisionsBetween(RagdollCollisionLayer, StaticCollisionLayer);
            world.DisableCollisionsBetween(RagdollCollisionLayer, DefaultCollisionLayer);
            world.DisableCollisionsBetween(RagdollCollisionLayer, CharacterCollisionLayer);
            world.DisableCollisionsBetween(RagdollCollisionLayer, CharacterNetworkCollisionLayer);
            world.DisableCollisionsBetween(RagdollCollisionLayer, DynamicDoubledCollisionLayer);
            world.DisableCollisionsBetween(RagdollCollisionLayer, KinematicDoubledCollisionLayer);
            world.DisableCollisionsBetween(RagdollCollisionLayer, DebrisCollisionLayer);
            world.DisableCollisionsBetween(RagdollCollisionLayer, FloatingObjectCollisionLayer);
            world.DisableCollisionsBetween(RagdollCollisionLayer, GravityPhantomLayer);
            world.DisableCollisionsBetween(RagdollCollisionLayer, ObjectDetectionCollisionLayer);
            world.DisableCollisionsBetween(RagdollCollisionLayer, VirtualMassLayer);
            world.DisableCollisionsBetween(RagdollCollisionLayer, NoCollisionLayer);
            world.DisableCollisionsBetween(RagdollCollisionLayer, ExplosionRaycastLayer);
            world.DisableCollisionsBetween(RagdollCollisionLayer, CollisionLayerWithoutCharacter);
            world.DisableCollisionsBetween(RagdollCollisionLayer, CollideWithStaticLayer);
            world.DisableCollisionsBetween(RagdollCollisionLayer, CollectorCollisionLayer);
            world.DisableCollisionsBetween(RagdollCollisionLayer, AmmoLayer);
            

                    }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public static void AssertThread()
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == ThreadId, "Calling method from invalid thread, physics can be accessed only from thread where it was created.");
        }

        public override void LoadData()
        {
            //HkBaseSystem.EnableAssert((int)0xd8279a05, false);
            HkBaseSystem.EnableAssert(-668493307, false);
            //HkBaseSystem.EnableAssert((int)3626473989, false);
            //float broadphaseSize = 100000.0f; // For unlimited worlds
            
            //Angular velocities and impulses
            HkBaseSystem.EnableAssert(952495168, false);
            HkBaseSystem.EnableAssert(1501626980, false);
            HkBaseSystem.EnableAssert(-258736554, false);
            HkBaseSystem.EnableAssert(524771844, false);
            HkBaseSystem.EnableAssert(1081361407, false);

            ThreadId = Thread.CurrentThread.ManagedThreadId;

            if(MyPerGameSettings.SingleCluster)
                Clusters = new MyHavokCluster(MySession.Static.WorldBoundaries);
            else
                Clusters = new MyHavokCluster(null);
            Clusters.OnClusterCreated += OnClusterCreated;
            Clusters.OnClusterRemoved += OnClusterRemoved;
            Clusters.OnFinishBatch += OnFinishBatch;

            if (MyFakes.ENABLE_HAVOK_MULTITHREADING)
            {
                m_threadPool = new HkJobThreadPool();
                m_jobQueue = new HkJobQueue(m_threadPool.ThreadCount + 1);
            }

            //Needed for smooth wheel movement
            HkCylinderShape.SetNumberOfVirtualSideSegments(128);
        }

        HkWorld OnClusterCreated(int clusterId, BoundingBoxD bbox)
        {
            float broadPhaseSize = (float)bbox.Size.Max();
            System.Diagnostics.Debug.Assert(broadPhaseSize > 10 && broadPhaseSize < 1000000);
            return CreateHkWorld(broadPhaseSize);
        }

        void OnClusterRemoved(object world)
        {
            var hkWorld = (HkWorld)world;
            if (hkWorld.DestructionWorld != null)
            {
                hkWorld.DestructionWorld.Dispose();
                hkWorld.DestructionWorld = null;
            }
            hkWorld.Dispose();
        }

        void OnFinishBatch(object world)
        {
            ((HkWorld)world).FinishBatch();
        }

        public static HkWorld CreateHkWorld(float broadphaseSize = 100000)
        {
            var hkWorld = new HkWorld(MyPerGameSettings.EnableGlobalGravity, broadphaseSize, RestingVelocity, MyFakes.ENABLE_HAVOK_MULTITHREADING, MySession.Static.Settings.PhysicsIterations);

            hkWorld.MarkForWrite();

            if (MySession.Static.Settings.WorldSizeKm > 0 || MyPerGameSettings.SingleCluster)
            {
                hkWorld.EntityLeftWorld += HavokWorld_EntityLeftWorld;
            }
            if (MyPerGameSettings.Destruction && Sandbox.Game.Multiplayer.Sync.IsServer)
            {
                hkWorld.DestructionWorld = new HkdWorld(hkWorld);
            }
            if (MyFakes.ENABLE_HAVOK_MULTITHREADING)
            {
                hkWorld.InitMultithreading(m_threadPool, m_jobQueue);
            }
            // Some ship won't rotate when this clip speed is too large
            hkWorld.DeactivationRotationSqrdA /= 3;
            hkWorld.DeactivationRotationSqrdB /= 3;
            if (!MyFinalBuildConstants.IS_OFFICIAL)
            {
                hkWorld.VisualDebuggerEnabled = true;
            }
            InitCollisionFilters(hkWorld);

            return hkWorld;
        }

        static void HavokWorld_EntityLeftWorld(HkEntity hkEntity)
        {
            var entity = hkEntity.GetEntity();
            if (Sandbox.Game.Multiplayer.Sync.IsServer && entity != null)
            {
                // HACK: due to not working Close or MarkForClose correctly
                if (entity is Sandbox.Game.Entities.Character.MyCharacter)
                {
                    ((Sandbox.Game.Entities.Character.MyCharacter)entity).DoDamage(1000, MyDamageType.Suicide, true);
                }
                else if (entity is MyVoxelMap || entity is MyCubeBlock)
                {
                }
                else if (entity is MyCubeGrid)
                {
                    var grid = ((MyCubeGrid)entity);
                    if (entity.SyncObject != null)
                    {
                        grid.SyncObject.SendCloseRequest();
                    }
                    else
                    {
                        grid.Close();
                    }
                }
                else if (entity is MyFloatingObject)
                {
                    MyFloatingObjects.RemoveFloatingObject((MyFloatingObject)entity);
                }
                else if (entity is MyFracturedPiece)
                {
                    Sandbox.Game.GameSystems.MyFracturedPiecesManager.Static.RemoveFracturePiece((MyFracturedPiece)entity, 0);
                }
                else if(entity.SyncObject != null)
                {
                    entity.SyncObject.SendCloseRequest();
                }
            }
        }

        protected override void UnloadData()
        {
            Clusters.Dispose();

            Clusters.OnClusterCreated -= OnClusterCreated;
            Clusters.OnClusterRemoved -= OnClusterRemoved;
            Clusters = null;

            if (MyFakes.ENABLE_HAVOK_MULTITHREADING)
            {
                m_threadPool.RemoveReference();
                m_threadPool = null;

                m_jobQueue.Dispose();
                m_jobQueue = null;
            }
        }

        void AddTimestamp()
        {
            // Sliding window, keep timestamps for last second
            long now = Stopwatch.GetTimestamp();
            m_timestamps.Enqueue(now);
            long secondAgo = now - Stopwatch.Frequency;
            while (m_timestamps.Peek() < secondAgo)
            {
                m_timestamps.Dequeue();
            }
        }

        public override void Simulate()
        {
            if (MyFakes.PAUSE_PHYSICS && !MyFakes.STEP_PHYSICS)
                return;
            MyFakes.STEP_PHYSICS = false;

            if (!MySandboxGame.IsGameReady)
                return;

            AddTimestamp();

            InsideSimulation = true;
            ProcessDestructions();

            ProfilerShort.Begin("HavokWorld.Step");

            foreach (HkWorld world in Clusters.GetList())
            {
                world.UnmarkForWrite();
                world.StepSimulation(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
                world.MarkForWrite();
            }

            ProfilerShort.End();
            InsideSimulation = false;

            ProfilerShort.Begin("Update rigid bodies");

            long activeRigidBodies = 0;
            foreach (HkWorld world in Clusters.GetList())
            {
                activeRigidBodies += world.ActiveRigidBodies.Count;
            }

            VRageRender.MyPerformanceCounter.PerCameraDrawWrite["Active rigid bodies"] = activeRigidBodies;

            ProfilerShort.CustomValue("Active bodies", activeRigidBodies, null);

            foreach (HkWorld world in Clusters.GetList())
            {
                IterateBodies(world);
            }


            //ParallelTasks.Parallel.For(0, m_iterationBodies.Count, (rb) =>
            //{
            //    MyPhysicsBody body = (MyPhysicsBody)m_iterationBodies[rb].UserObject;
            //    if (body == null)
            //        return;
            //    body.OnMotion(m_iterationBodies[rb], MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
            //}, Math.Max(1, m_iterationBodies.Count / 16));

            foreach (var rb in m_iterationBodies)
            {
                MyPhysicsBody body = (MyPhysicsBody)rb.UserObject;
                if (body == null)
                    return;
                body.OnMotion(rb, MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
            }

            foreach (HkCharacterRigidBody rb in m_characterIterationBodies)
            {
                var body = (MyPhysicsBody)rb.GetHitRigidBody().UserObject;
                if (body.Entity.WorldMatrix.Translation != body.GetWorldMatrix().Translation)
                {
                    body.UpdateCluster();
                }
            }

            m_iterationBodies.Clear();
            m_characterIterationBodies.Clear();

            ProfilerShort.End();

            ProfilerShort.Begin("HavokWorld.StepVDB");
            foreach (HkWorld world in Clusters.GetList())
            {
                //jn: peaks with Render profiling and destruction
                world.StepVDB(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
            }

            ProfilerShort.End();
        }


        private static void ProcessDestructions()
        {
            ProfilerShort.Begin("Destruction");
            while (m_destructionQueue.Count > 0)
            {
                var destructionInfo = m_destructionQueue.Dequeue();

                var details = destructionInfo.Details;
                if (details.IsValid())
                {
                    details.Flag = details.Flag | HkdFractureImpactDetails.Flags.FLAG_DONT_DELAY_OPERATION;
                    destructionInfo.World.DestructionWorld.TriggerDestruction(ref details);
                    
                    MySyncDestructions.AddDestructionEffect(MyPerGameSettings.CollisionParticle.LargeGridClose, destructionInfo.ContactInWorld, Vector3D.Forward,0.2f);

                    MySyncDestructions.AddDestructionEffect(MyPerGameSettings.DestructionParticle.DestructionHit, destructionInfo.ContactInWorld, Vector3D.Forward, 0.1f);
                }
                
                destructionInfo.Details.RemoveReference();
            }
            ProfilerShort.End();
        }

        private void IterateBodies(HkWorld world)
        {
            foreach (var actRB in world.ActiveRigidBodies)
            {
                m_iterationBodies.Add(actRB);
            }

            foreach (var charRB in world.CharacterRigidBodies)
            {
                m_characterIterationBodies.Add(charRB);
            }
        }

        public static void ActivateInBox(ref BoundingBoxD box)
        {
            using (m_tmpEntityResults.GetClearToken())
            {
                MyGamePruningStructure.GetAllEntitiesInBox(ref box, m_tmpEntityResults);
                foreach (var entity in m_tmpEntityResults)
                {
                    if (entity.Physics != null && entity.Physics.Enabled && entity.Physics.RigidBody != null)
                    {
                        entity.Physics.RigidBody.Activate();
                    }
                }
            }
        }

        //private static List<FractureImpactDetails> m_destructionQueue = new List<FractureImpactDetails>();
        private static Queue<FractureImpactDetails> m_destructionQueue = new Queue<FractureImpactDetails>();
        public static void EnqueueDestruction(FractureImpactDetails details)
        {
            System.Diagnostics.Debug.Assert(Sandbox.Game.Multiplayer.Sync.IsServer, "Clients cannot create destructions");
            m_destructionQueue.Enqueue(details);
        }

        public static void RemoveDestructions(MyEntity entity)
        {
            var list = m_destructionQueue.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Entity == entity)
                {
                    list[i].Details.RemoveReference();
                    list.RemoveAt(i);
                    i--;
                }
            }

            m_destructionQueue.Clear();
            foreach (var details in list)
            {
                m_destructionQueue.Enqueue(details);
            }
        }

        public static void RemoveDestructions(HkRigidBody body)
        {
            var list = m_destructionQueue.ToList();

            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].Details.IsValid() || list[i].Details.GetBreakingBody() == body)
                {
                    list[i].Details.RemoveReference();
                    list.RemoveAt(i);
                    i--;
                }
            }

            m_destructionQueue.Clear();
            foreach (var details in list)
            {
                m_destructionQueue.Enqueue(details);
            }
        }

        public static bool DebugDrawClustersEnable = false;
        public static MatrixD DebugDrawClustersMatrix = MatrixD.Identity;
        static List<BoundingBoxD> m_clusterStaticObjects = new List<BoundingBoxD>();

        public static void DebugDrawClusters()
        {
            if (Clusters == null)
                return;

            double previewScale = 2000;
            MatrixD previewMatrix = MatrixD.CreateWorld(DebugDrawClustersMatrix.Translation + previewScale * DebugDrawClustersMatrix.Forward, Vector3D.Forward, Vector3D.Up);

            m_resultWorlds.Clear();

            Clusters.GetAll(m_resultWorlds);

            BoundingBoxD totalBox = BoundingBoxD.CreateInvalid();

            foreach (var res in m_resultWorlds)
            {
                totalBox = totalBox.Include(res.AABB);
            }

            double maxAxis = totalBox.Size.AbsMax();
            //double scaleAxis = 0.057142857142857141;
            double scaleAxis = previewScale / maxAxis;

            //Vector3D scale = new Vector3D(totalBox.Size.X * scaleAxis, totalBox.Size.Y * scaleAxis, totalBox.Size.Z * scaleAxis);

            Vector3D center = totalBox.Center;
            totalBox.Min -= center;
            totalBox.Max -= center;

            {
                BoundingBoxD scaledBox = new BoundingBoxD(totalBox.Min * scaleAxis * 1.02f, totalBox.Max * scaleAxis * 1.02f);
                MyOrientedBoundingBoxD oriented = new MyOrientedBoundingBoxD(scaledBox, previewMatrix);
                MyRenderProxy.DebugDrawOBB(oriented, Vector3.Up, 1, false, false);

                MyRenderProxy.DebugDrawAxis(previewMatrix, 50, false);

                if (MySession.Static != null)
                {
                    foreach (var player in Sandbox.Game.Multiplayer.Sync.Players.GetOnlinePlayers())
                    {
                        if (player.Character != null)
                        {
                            var playerPos = Vector3D.Transform((player.Character.PositionComp.GetPosition() - center) * scaleAxis, previewMatrix);
                            MyRenderProxy.DebugDrawSphere(playerPos, 10, Vector3.One, 1, false);
                        }
                    }
                }
            }

            Clusters.GetAllStaticObjects(m_clusterStaticObjects);
            foreach (var staticBB in m_clusterStaticObjects)
            {
                BoundingBoxD scaledBox = new BoundingBoxD((staticBB.Min - center) * scaleAxis, (staticBB.Max - center) * scaleAxis);

                MyOrientedBoundingBoxD oriented = new MyOrientedBoundingBoxD(scaledBox, previewMatrix);

                MyRenderProxy.DebugDrawOBB(oriented, Color.Blue, 1, false, false);
            }

            foreach (var res in m_resultWorlds)
            {
                BoundingBoxD scaledBox = new BoundingBoxD((res.AABB.Min - center) * scaleAxis, (res.AABB.Max - center) * scaleAxis);

                MyOrientedBoundingBoxD oriented = new MyOrientedBoundingBoxD(scaledBox, previewMatrix);

                MyRenderProxy.DebugDrawOBB(oriented, Vector3.One, 1, false, false);

                foreach (var rb in ((HkWorld)res.UserData).CharacterRigidBodies)
                {
                    Vector3D rbCenter = res.AABB.Center + rb.Position;
                    rbCenter = (rbCenter - center) * scaleAxis;
                    rbCenter = Vector3D.Transform(rbCenter, previewMatrix);

                    Vector3D velocity = rb.LinearVelocity;
                    velocity = Vector3D.TransformNormal(velocity, previewMatrix) * 10;
                    MyRenderProxy.DebugDrawLine3D(rbCenter, rbCenter + velocity, Color.Blue, Color.White, false);
                }

                foreach (var rb in ((HkWorld)res.UserData).RigidBodies)
                {
                    MyOrientedBoundingBoxD rbbb = new MyOrientedBoundingBoxD((BoundingBoxD)rb.GetEntity().LocalAABB, rb.GetEntity().WorldMatrix);
                    rbbb.Center = (rbbb.Center - center) * scaleAxis;
                    rbbb.HalfExtent *= scaleAxis;
                    rbbb.Transform(previewMatrix);
                    MyRenderProxy.DebugDrawOBB(rbbb, Color.Yellow, 1, false, false);

                    //BoundingBoxD rbaa = rb.GetEntity().WorldAABB;
                    //rbaa.Min = (rbaa.Min - center) * scaleAxis;
                    //rbaa.Max = (rbaa.Max - center) * scaleAxis;
                    //MyRenderProxy.DebugDrawAABB(rbaa, new Vector3(0.8f, 0.8f, 0.8f), 1, 1, false);

                    Vector3D velocity = rb.LinearVelocity;
                    velocity = Vector3D.TransformNormal(velocity, previewMatrix) * 10;
                    MyRenderProxy.DebugDrawLine3D(rbbb.Center, rbbb.Center + velocity, Color.Red, Color.White, false);

                    if (velocity.Length() > 1)
                    {
                        BoundingBoxD ideal = new BoundingBoxD(rb.GetEntity().WorldAABB.Center - MyHavokCluster.IdealClusterSize / 2, rb.GetEntity().WorldAABB.Center + MyHavokCluster.IdealClusterSize / 2);
                        MyOrientedBoundingBoxD idealObb = new MyOrientedBoundingBoxD(ideal, MatrixD.Identity);
                        idealObb.Center = (ideal.Center - center) * scaleAxis;
                        idealObb.HalfExtent *= scaleAxis;
                        idealObb.Transform(previewMatrix);
                        MyRenderProxy.DebugDrawOBB(idealObb, new Vector3(0, 0, 1), 1, false, false);
                    }
                }
            }
        }

        public void Debug_ReorderClusters()
        {
            MySession.ControlledEntity.Entity.Physics.ReorderClusters();
        }

        #region Havok worlds wrapper

        static List<MyHavokCluster.MyClusterQueryResult> m_resultWorlds = new List<MyHavokCluster.MyClusterQueryResult>();
        static List<HkWorld.HitInfo> m_resultHits = new List<HkWorld.HitInfo>();

        public static void CastRay(Vector3D from, Vector3D to, List<HitInfo> toList, int raycastFilterLayer = 0)
        {
            m_resultWorlds.Clear();
            Clusters.CastRay(from, to, m_resultWorlds);

            toList.Clear();

            foreach (var world in m_resultWorlds)
            {
                Vector3 fromF = from - world.AABB.Center;
                Vector3 toF = to - world.AABB.Center;

                m_resultHits.Clear();
                ((HkWorld)world.UserData).CastRay(fromF, toF, m_resultHits, raycastFilterLayer);

                foreach (var hit in m_resultHits)
                {
                    toList.Add(new HitInfo()
                    {
                        HkHitInfo = hit,
                        Position = hit.Position + world.AABB.Center
                    }
                    );
                }
            }

            m_resultWorlds.Clear();
        }

        public static HkRigidBody CastRay(Vector3D from, Vector3D to, out Vector3D position, out Vector3 normal, int raycastFilterLayer = 0)
        {
            m_resultWorlds.Clear();
            Clusters.CastRay(from, to, m_resultWorlds);

            foreach (var world in m_resultWorlds)
            {
                Vector3 fromF = from - world.AABB.Center;
                Vector3 toF = to - world.AABB.Center;

                m_resultHits.Clear();
                Vector3 hitPos;
                HkRigidBody rb = ((HkWorld)world.UserData).CastRay(fromF, toF, out hitPos, out normal, raycastFilterLayer);

                if (rb != null)
                {
                    position = (Vector3D)hitPos + world.AABB.Center;

                    m_resultWorlds.Clear();
                    return rb;
                }
            }

            position = Vector3D.Zero;
            normal = Vector3D.Up;
            m_resultWorlds.Clear();

            return null;
        }

        public static bool CastRay(Vector3D from, Vector3D to, out Vector3D position, out Vector3 normal, uint raycastCollisionFilter, bool ignoreConvexShape)
        {            
            m_resultWorlds.Clear();
            Clusters.CastRay(from, to, m_resultWorlds);

            foreach (var world in m_resultWorlds)
            {
                Vector3 fromF = from - world.AABB.Center;
                Vector3 toF = to - world.AABB.Center;

                m_resultHits.Clear();
                Vector3 hitPos;                
                bool hit = ((HkWorld)world.UserData).CastRay(fromF, toF, out hitPos, out normal, raycastCollisionFilter, ignoreConvexShape);

                if (hit)
                {
                    position = (Vector3D)hitPos + world.AABB.Center;                    
                    m_resultWorlds.Clear();
                    return hit;
                }
            }

            position = Vector3D.Zero;
            normal = Vector3D.Up;
            m_resultWorlds.Clear();

            return false;
        }
        public static void GetPenetrationsShape(HkShape shape, ref Vector3D translation, ref Quaternion rotation, List<HkRigidBody> results, int filter)
        {
            m_resultWorlds.Clear();
            Clusters.Intersects(translation, m_resultWorlds);

            foreach (var world in m_resultWorlds)
            {
                Vector3 translationF = translation - world.AABB.Center;
                ((HkWorld)world.UserData).GetPenetrationsShape(shape, ref translationF, ref rotation, results, filter);
            }
        }

        public static float? CastShape(Vector3D to, HkShape shape, ref MatrixD transform, int filterLayer, float extraPenetration = 0)
        {
            m_resultWorlds.Clear();
            Clusters.Intersects(to, m_resultWorlds);

            if (m_resultWorlds.Count == 0)
                return null;

            var world = m_resultWorlds[0];

            Matrix transformF = transform;
            transformF.Translation = (Vector3)(transform.Translation - world.AABB.Center);

            Vector3 toF = (Vector3)(to - world.AABB.Center);

            return ((HkWorld)world.UserData).CastShape(toF, shape, ref transformF, filterLayer, extraPenetration);
        }

        public static void GetPenetrationsBox(ref Vector3 halfExtents, ref Vector3D translation, ref Quaternion rotation, List<HkRigidBody> results, int filter)
        {
            m_resultWorlds.Clear();
            Clusters.Intersects(translation, m_resultWorlds);

            foreach (var world in m_resultWorlds)
            {
                Vector3 translationF = translation - world.AABB.Center;
                ((HkWorld)world.UserData).GetPenetrationsBox(ref halfExtents, ref translationF, ref rotation, results, filter);
            }
        }

        public static Vector3D? CastShapeReturnPoint(Vector3D to, HkShape shape, ref MatrixD transform, int filterLayer, float extraPenetration)
        {
            m_resultWorlds.Clear();
            Clusters.Intersects(to, m_resultWorlds);

            if (m_resultWorlds.Count == 0)
                return null;

            var world = m_resultWorlds[0];

            Matrix transformF = transform;
            transformF.Translation = (Vector3)(transform.Translation - world.AABB.Center);

            Vector3 toF = (Vector3)(to - world.AABB.Center);

            var result = ((HkWorld)world.UserData).CastShapeReturnPoint(toF, shape, ref transformF, filterLayer, extraPenetration);
            if (result == null)
            {
                return null;
            }
            return (Vector3D)result + world.AABB.Center;
        }

        public static HkContactPoint? CastShapeReturnContact(Vector3D to, HkShape shape, ref MatrixD transform, int filterLayer, float extraPenetration, out Vector3 worldTranslation)
        {
            m_resultWorlds.Clear();
            Clusters.Intersects(to, m_resultWorlds);

            worldTranslation = Vector3.Zero;

            if (m_resultWorlds.Count == 0)
                return null;

            var world = m_resultWorlds[0];

            worldTranslation = world.AABB.Center;

            Matrix transformF = transform;
            transformF.Translation = (Vector3)(transform.Translation - world.AABB.Center);

            Vector3 toF = (Vector3)(to - world.AABB.Center);                       

            HkContactPoint? result = ((HkWorld)world.UserData).CastShapeReturnContact(toF, shape, ref transformF, filterLayer, extraPenetration);
            if (result == null)
            {
                return null;
            }            
            return result;
        }

        public static HkContactPointData? CastShapeReturnContactData(Vector3D to, HkShape shape, ref MatrixD transform, uint collisionFilter, float extraPenetration, bool ignoreConvexShape = true)
        {
            m_resultWorlds.Clear();
            Clusters.Intersects(to, m_resultWorlds);                       

            if (m_resultWorlds.Count == 0)
                return null;

            var world = m_resultWorlds[0];                      

            Matrix transformF = transform;
            transformF.Translation = (Vector3)(transform.Translation - world.AABB.Center);

            Vector3 toF = (Vector3)(to - world.AABB.Center);

            HkContactPointData? result = ((HkWorld)world.UserData).CastShapeReturnContactData(toF, shape, ref transformF, collisionFilter, extraPenetration);
            if (result == null)
            {
                return null;
            }
            HkContactPointData cpd = result.Value;
            cpd.HitPosition += world.AABB.Center;
            return cpd;
        }

        public static HkContactBodyData? CastShapeReturnContactBodyData(Vector3D to, HkShape shape, ref MatrixD transform, uint collisionFilter, float extraPenetration, bool ignoreConvexShape = true)
        {
            m_resultWorlds.Clear();
            Clusters.Intersects(to, m_resultWorlds);

            if (m_resultWorlds.Count == 0)
                return null;

            var world = m_resultWorlds[0];

            Matrix transformF = transform;
            transformF.Translation = (Vector3)(transform.Translation - world.AABB.Center);

            Vector3 toF = (Vector3)(to - world.AABB.Center);

            HkContactBodyData? result = ((HkWorld)world.UserData).CastShapeReturnContactBodyData(toF, shape, ref transformF, collisionFilter, extraPenetration);
            if (result == null)
            {
                return null;
            }
            HkContactBodyData cpd = result.Value;
            cpd.HitPosition += world.AABB.Center;
            return cpd;
        }


        public static bool IsPenetratingShapeShape(HkShape shape1, ref Vector3D translation1, ref Quaternion rotation1, HkShape shape2, ref Vector3D translation2, ref Quaternion rotation2)
        {
            m_resultWorlds.Clear();
            Clusters.Intersects(translation1, m_resultWorlds);

            foreach (var world in m_resultWorlds)
            {
                if (world.AABB.Contains(translation2) != ContainmentType.Contains)
                    return false;

                Vector3 translation1F = translation1 - world.AABB.Center;
                Vector3 translation2F = translation2 - world.AABB.Center;

                if (((HkWorld)world.UserData).IsPenetratingShapeShape(shape1, ref translation1F, ref rotation1, shape2, ref translation2F, ref rotation2))
                    return true;
            }

            return false;
        }

        public static HkWorld SingleWorld
        {
            get
            {
                System.Diagnostics.Debug.Assert(Clusters.SingleCluster.HasValue, "SingleWorld exists only with SingleCluster setting");
                return Clusters.GetList().First() as HkWorld;
            }
        }

        #endregion

        public static bool InsideSimulation { get; private set; }
    }
}