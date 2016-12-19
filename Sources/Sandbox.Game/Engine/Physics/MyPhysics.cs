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
using Sandbox.Engine.Voxels;
using Sandbox.Game.Multiplayer;
using VRage.Profiler;

#endregion

namespace Sandbox.Engine.Physics
{
    //using MyHavokCluster = VRageMath.Spatial.MyClusterTree<HkWorld>;
    using MyHavokCluster = VRageMath.Spatial.MyClusterTree;
    using VRage.Game.Components;
    using VRage.Game.Entity;
    using VRage.Game;
    using ParallelTasks;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRage.Network;
    using Sandbox.Engine.Multiplayer;
    using VRageMath.Spatial;

    [MySessionComponentDescriptor(MyUpdateOrder.Simulation, 500)]
    [StaticEventOwnerAttribute]
    public class MyPhysics : MySessionComponentBase
    {
        public struct HitInfo : IHitInfo
        {
            public HitInfo(HkWorld.HitInfo hi, Vector3D worldPosition)
            {
                HkHitInfo = hi;
                Position = worldPosition;
            }

            public HkWorld.HitInfo HkHitInfo;
            public Vector3D Position;

            Vector3D IHitInfo.Position
            {
                get { return Position; }
            }

            IMyEntity IHitInfo.HitEntity
            {
                get { return HkHitInfo.GetHitEntity(); }
            }

            public override string ToString()
            {
                //return base.ToString();
                var hitEntity = HkHitInfo.GetHitEntity();
                if (hitEntity != null)
                {
                    return hitEntity.ToString();
        }
                return base.ToString();
            }
        }

        public struct MyContactPointEvent
        {
            public HkContactPointEvent ContactPointEvent;
            public Vector3D Position;

            public Vector3 Normal
            {
                get { return ContactPointEvent.ContactPoint.Normal; }
            }
        }

        public struct FractureImpactDetails
        {
            public HkdFractureImpactDetails Details;
            public HkWorld World;
            public Vector3D ContactInWorld;
            public MyEntity Entity;
            //public StackTrace dbgTrace;
        }

        /// <summary>
        /// Collision layers that can be used to filter what collision should be found for casting methods.
        /// </summary>
        /// <remarks>!!** If new layer is added then also add conversion to "GetCollisionLayer" function **!! 
        /// Also max layer number is 31!!</remarks>
        public struct CollisionLayers
        {
            /// <summary>
            /// Layer that works like 'DefaultCollisionLayer' but do not return collision with voxels (ex. Planet ground/asteroid).
            /// </summary>
            public const int NoVoxelCollisionLayer = 9;

            public const int LightFloatingObjectCollisionLayer = 10;

            // Layer that doesn't collide with static grids and voxels
            public const int VoxelLod1CollisionLayer = 11;
            public const int NotCollideWithStaticLayer = 12;
            // Static grids
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
            public const int VoxelCollisionLayer = 28;
            public const int ExplosionRaycastLayer = 29;
            public const int CollisionLayerWithoutCharacter = 30;

            // TODO: This layer should be removed, when character won't need both CharacterProxy's body with ragdoll enabled at one time i.e. jetpack
            public const int RagdollCollisionLayer = 31;
        }

        public static int ThreadId;

        public static MyHavokCluster Clusters;
        private static bool ClustersNeedSync = false;

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
                if (MyFakes.ENABLE_SIMSPEED_LOCKING || MyFakes.PRECISE_SIM_SPEED)
                {
                    return MySandboxGame.SimulationRatio;
                }
 
                return (float)Math.Round(Math.Max(0.5f, StepsLastSecond) / (float)MyEngineConstants.UPDATE_STEPS_PER_SECOND,2);
            }
        }

        public static float RestingVelocity
        {
            get
            {
                return MyPerGameSettings.BallFriendlyPhysics ? 3 : float.MaxValue;
            }
        }

        private static SpinLockRef m_raycastLock = new SpinLockRef();

        static void InitCollisionFilters(HkWorld world)
        {
            // Floating objects intentionally collide with both DynamicDoubledCollisionLayer and KinematicDoubledCollisionLayer
            // DynamicDoubledCollisionLayer is necessary, because we won't deformations
            // KinematicDoubledCollisionLayer is necessary, because we don't want to push it when floating object is resting on it

            world.DisableCollisionsBetween(CollisionLayers.DynamicDoubledCollisionLayer, CollisionLayers.KinematicDoubledCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.DynamicDoubledCollisionLayer, CollisionLayers.CharacterCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.DynamicDoubledCollisionLayer, CollisionLayers.DebrisCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.DynamicDoubledCollisionLayer, CollisionLayers.LightFloatingObjectCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.DynamicDoubledCollisionLayer, CollisionLayers.CharacterNetworkCollisionLayer);

            world.DisableCollisionsBetween(CollisionLayers.NotCollideWithStaticLayer, CollisionLayers.StaticCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NotCollideWithStaticLayer, CollisionLayers.VoxelCollisionLayer);

            world.DisableCollisionsBetween(CollisionLayers.KinematicDoubledCollisionLayer, CollisionLayers.DefaultCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.KinematicDoubledCollisionLayer, CollisionLayers.StaticCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.KinematicDoubledCollisionLayer, CollisionLayers.VoxelCollisionLayer);
            //world.DisableCollisionsBetween(KinematicDoubledCollisionLayer, AmmoLayer);

            world.DisableCollisionsBetween(CollisionLayers.GravityPhantomLayer, CollisionLayers.StaticCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.GravityPhantomLayer, CollisionLayers.VoxelCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.GravityPhantomLayer, CollisionLayers.DefaultCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.GravityPhantomLayer, CollisionLayers.DynamicDoubledCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.GravityPhantomLayer, CollisionLayers.KinematicDoubledCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.GravityPhantomLayer, CollisionLayers.CharacterCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.GravityPhantomLayer, CollisionLayers.CharacterNetworkCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.GravityPhantomLayer, CollisionLayers.ObjectDetectionCollisionLayer);
            //world.DisableCollisionsBetween(GravityPhantomLayer, AmmoLayer);

            world.DisableCollisionsBetween(CollisionLayers.VirtualMassLayer, CollisionLayers.StaticCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.VirtualMassLayer, CollisionLayers.VoxelCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.VirtualMassLayer, CollisionLayers.DefaultCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.VirtualMassLayer, CollisionLayers.CharacterCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.VirtualMassLayer, CollisionLayers.CharacterNetworkCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.VirtualMassLayer, CollisionLayers.DynamicDoubledCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.VirtualMassLayer, CollisionLayers.KinematicDoubledCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.VirtualMassLayer, CollisionLayers.DebrisCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.VirtualMassLayer, CollisionLayers.FloatingObjectCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.VirtualMassLayer, CollisionLayers.LightFloatingObjectCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.VirtualMassLayer, CollisionLayers.ObjectDetectionCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.VirtualMassLayer, CollisionLayers.VirtualMassLayer);
            //world.DisableCollisionsBetween(VirtualMassLayer, AmmoLayer);

            world.DisableCollisionsBetween(CollisionLayers.NoCollisionLayer, CollisionLayers.StaticCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoCollisionLayer, CollisionLayers.VoxelCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoCollisionLayer, CollisionLayers.DefaultCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoCollisionLayer, CollisionLayers.CharacterCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoCollisionLayer, CollisionLayers.CharacterNetworkCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoCollisionLayer, CollisionLayers.DynamicDoubledCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoCollisionLayer, CollisionLayers.KinematicDoubledCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoCollisionLayer, CollisionLayers.DebrisCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoCollisionLayer, CollisionLayers.FloatingObjectCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoCollisionLayer, CollisionLayers.LightFloatingObjectCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoCollisionLayer, CollisionLayers.GravityPhantomLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoCollisionLayer, CollisionLayers.ObjectDetectionCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoCollisionLayer, CollisionLayers.VirtualMassLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoCollisionLayer, CollisionLayers.NoCollisionLayer);

            if (MyPerGameSettings.PhysicsNoCollisionLayerWithDefault)
                world.DisableCollisionsBetween(CollisionLayers.NoCollisionLayer, 0);
            //world.DisableCollisionsBetween(NoCollisionLayer, AmmoLayer);

            world.DisableCollisionsBetween(CollisionLayers.ObjectDetectionCollisionLayer, CollisionLayers.ObjectDetectionCollisionLayer);

            world.DisableCollisionsBetween(CollisionLayers.CollectorCollisionLayer, CollisionLayers.StaticCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollectorCollisionLayer, CollisionLayers.VoxelCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollectorCollisionLayer, CollisionLayers.DefaultCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollectorCollisionLayer, CollisionLayers.CharacterCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollectorCollisionLayer, CollisionLayers.CharacterNetworkCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollectorCollisionLayer, CollisionLayers.DynamicDoubledCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollectorCollisionLayer, CollisionLayers.KinematicDoubledCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollectorCollisionLayer, CollisionLayers.DebrisCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollectorCollisionLayer, CollisionLayers.GravityPhantomLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollectorCollisionLayer, CollisionLayers.ObjectDetectionCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollectorCollisionLayer, CollisionLayers.VirtualMassLayer);
            //world.DisableCollisionsBetween(CollectorCollisionLayer, AmmoLayer);

            if (!Sandbox.Game.Multiplayer.Sync.IsServer)
            {
                world.DisableCollisionsBetween(CollisionLayers.DefaultCollisionLayer, CollisionLayers.CharacterNetworkCollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.StaticCollisionLayer, CollisionLayers.CharacterNetworkCollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.VoxelCollisionLayer, CollisionLayers.CharacterNetworkCollisionLayer);
            }

            if (!MyFakes.ENABLE_CHARACTER_AND_DEBRIS_COLLISIONS)
            {
                world.DisableCollisionsBetween(CollisionLayers.DebrisCollisionLayer, CollisionLayers.CharacterCollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.DebrisCollisionLayer, CollisionLayers.CharacterNetworkCollisionLayer);
            }

            //Disable collisions with anything but ships and stations
            world.DisableCollisionsBetween(CollisionLayers.ExplosionRaycastLayer, CollisionLayers.VoxelCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.ExplosionRaycastLayer, CollisionLayers.CharacterCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.ExplosionRaycastLayer, CollisionLayers.NoCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.ExplosionRaycastLayer, CollisionLayers.DebrisCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.ExplosionRaycastLayer, CollisionLayers.GravityPhantomLayer);
            world.DisableCollisionsBetween(CollisionLayers.ExplosionRaycastLayer, CollisionLayers.CharacterNetworkCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.ExplosionRaycastLayer, CollisionLayers.FloatingObjectCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.ExplosionRaycastLayer, CollisionLayers.LightFloatingObjectCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.ExplosionRaycastLayer, CollisionLayers.ObjectDetectionCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.ExplosionRaycastLayer, CollisionLayers.VirtualMassLayer);
            world.DisableCollisionsBetween(CollisionLayers.ExplosionRaycastLayer, CollisionLayers.CollectorCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.ExplosionRaycastLayer, CollisionLayers.AmmoLayer);

            world.DisableCollisionsBetween(CollisionLayers.CollisionLayerWithoutCharacter, CollisionLayers.CharacterCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollisionLayerWithoutCharacter, CollisionLayers.NoCollisionLayer);

            world.DisableCollisionsBetween(CollisionLayers.CollideWithStaticLayer, CollisionLayers.CollideWithStaticLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollideWithStaticLayer, CollisionLayers.DynamicDoubledCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollideWithStaticLayer, CollisionLayers.DefaultCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollideWithStaticLayer, CollisionLayers.CharacterCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollideWithStaticLayer, CollisionLayers.NoCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollideWithStaticLayer, CollisionLayers.DebrisCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollideWithStaticLayer, CollisionLayers.GravityPhantomLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollideWithStaticLayer, CollisionLayers.CharacterNetworkCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollideWithStaticLayer, CollisionLayers.FloatingObjectCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollideWithStaticLayer, CollisionLayers.LightFloatingObjectCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollideWithStaticLayer, CollisionLayers.ObjectDetectionCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollideWithStaticLayer, CollisionLayers.VirtualMassLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollideWithStaticLayer, CollisionLayers.CollectorCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.CollideWithStaticLayer, CollisionLayers.AmmoLayer);


            // TODO: This should be removed, when ragdoll won't need to be simulated on separate layer when partial simulation is enabled
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.StaticCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.VoxelCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.DefaultCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.CharacterCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.CharacterNetworkCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.DynamicDoubledCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.KinematicDoubledCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.DebrisCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.FloatingObjectCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.LightFloatingObjectCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.GravityPhantomLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.ObjectDetectionCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.VirtualMassLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.NoCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.ExplosionRaycastLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.CollisionLayerWithoutCharacter);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.CollideWithStaticLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.CollectorCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.AmmoLayer);
            if (!MyFakes.ENABLE_JETPACK_RAGDOLL_COLLISIONS)
            {
                world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.RagdollCollisionLayer);
            }


            if (MyVoxelPhysicsBody.UseLod1VoxelPhysics)
            {
                //large and low quality objects dont collide with lod0 voxel physics
                world.DisableCollisionsBetween(CollisionLayers.DynamicDoubledCollisionLayer, CollisionLayers.VoxelCollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.KinematicDoubledCollisionLayer, CollisionLayers.VoxelCollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.DefaultCollisionLayer, CollisionLayers.VoxelCollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.CollideWithStaticLayer, CollisionLayers.VoxelCollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.DebrisCollisionLayer, CollisionLayers.VoxelCollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.FloatingObjectCollisionLayer, CollisionLayers.VoxelCollisionLayer); // normal fo(now large) should collide with lod1

                world.DisableCollisionsBetween(CollisionLayers.ObjectDetectionCollisionLayer, CollisionLayers.VoxelLod1CollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.CharacterCollisionLayer, CollisionLayers.VoxelLod1CollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.CharacterNetworkCollisionLayer, CollisionLayers.VoxelLod1CollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.LightFloatingObjectCollisionLayer, CollisionLayers.VoxelLod1CollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.RagdollCollisionLayer, CollisionLayers.VoxelLod1CollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.ExplosionRaycastLayer, CollisionLayers.VoxelLod1CollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.CollectorCollisionLayer, CollisionLayers.VoxelLod1CollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.GravityPhantomLayer, CollisionLayers.VoxelLod1CollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.NoCollisionLayer, CollisionLayers.VoxelLod1CollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.VirtualMassLayer, CollisionLayers.VoxelLod1CollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.KinematicDoubledCollisionLayer, CollisionLayers.VoxelLod1CollisionLayer);
                world.DisableCollisionsBetween(CollisionLayers.NotCollideWithStaticLayer, CollisionLayers.VoxelLod1CollisionLayer);
            }

            // NoVoxelCollisionLayer collision filters
            world.DisableCollisionsBetween(CollisionLayers.NoVoxelCollisionLayer, CollisionLayers.KinematicDoubledCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoVoxelCollisionLayer, CollisionLayers.GravityPhantomLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoVoxelCollisionLayer, CollisionLayers.VirtualMassLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoVoxelCollisionLayer, CollisionLayers.NoCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoVoxelCollisionLayer, CollisionLayers.CollectorCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoVoxelCollisionLayer, CollisionLayers.CollideWithStaticLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoVoxelCollisionLayer, CollisionLayers.RagdollCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoVoxelCollisionLayer, CollisionLayers.VoxelCollisionLayer);
            world.DisableCollisionsBetween(CollisionLayers.NoVoxelCollisionLayer, CollisionLayers.VoxelLod1CollisionLayer);
            if (!Sandbox.Game.Multiplayer.Sync.IsServer)
                world.DisableCollisionsBetween(CollisionLayers.NoVoxelCollisionLayer, CollisionLayers.CharacterNetworkCollisionLayer);

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
            HkBaseSystem.EnableAssert(-1383504214, false); //we have more shapeKeys in contact point data

            //frequent, removing to clean logs
            HkBaseSystem.EnableAssert(-265005969, false); //calling set transform on body trying to deactivate
            HkBaseSystem.EnableAssert(1976984315, false);
            HkBaseSystem.EnableAssert(-252450131, false);
            HkBaseSystem.EnableAssert(-1400416854, false);

            ThreadId = Thread.CurrentThread.ManagedThreadId;

            Clusters = new MyHavokCluster(MySession.Static.WorldBoundaries, MyFakes.MP_SYNC_CLUSTERTREE && !Sync.IsServer);

            Clusters.OnClusterCreated += OnClusterCreated;
            Clusters.OnClusterRemoved += OnClusterRemoved;
            Clusters.OnFinishBatch += OnFinishBatch;
            Clusters.OnClustersReordered += Tree_OnClustersReordered;
            Clusters.GetEntityReplicableExistsById += GetEntityReplicableExistsById;

            if (MyFakes.ENABLE_HAVOK_MULTITHREADING)
            {
                m_threadPool = new HkJobThreadPool();
                m_jobQueue = new HkJobQueue(m_threadPool.ThreadCount + 1);
            }

            //Needed for smooth wheel movement
            HkCylinderShape.SetNumberOfVirtualSideSegments(32);
        }

        HkWorld OnClusterCreated(int clusterId, BoundingBoxD bbox)
        {
            float broadPhaseSize = (float)bbox.Size.Max();
            //Debug.Assert(false, "bbox.Center: " + bbox.Center + ", bbox.Size: " + bbox.Size + ", broadPhaseSize: " + broadPhaseSize);
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
            var hkWorld = new HkWorld(MyPerGameSettings.EnableGlobalGravity, broadphaseSize, MyFakes.WHEEL_SOFTNESS ? float.MaxValue : RestingVelocity, MyFakes.ENABLE_HAVOK_MULTITHREADING, MySession.Static.Settings.PhysicsIterations);

            hkWorld.MarkForWrite();

            if (MySession.Static.Settings.WorldSizeKm > 0)
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
                hkWorld.VisualDebuggerPort = Sync.IsServer ? 25001 : 25002;
                hkWorld.VisualDebuggerEnabled = true;
            }
            InitCollisionFilters(hkWorld);

            return hkWorld;
        }

        static void HavokWorld_EntityLeftWorld(HkEntity hkEntity)
        {
            var entities = hkEntity.GetAllEntities();
            foreach (var entity in entities)
            {
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
                    else if (entity.SyncObject != null)
                    {
                        entity.SyncObject.SendCloseRequest();
                    }
                }
            }
            entities.Clear();
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
            m_destructionQueue.Clear();

            if (MyPerGameSettings.Destruction)
            {
                //Dispose material otherwise memory is corrupted on DS service and memory leaks
                HkdBreakableShape.DisposeSharedMaterial();
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

            MySimpleProfiler.Begin("Physics");
            AddTimestamp();

            InsideSimulation = true;
            ProcessDestructions();

            ProfilerShort.Begin("HavokWorld.Step");

            if (MyFakes.CLIENTS_SIMULATE_SINGLE_WORLD && !Sync.IsServer)
            {
                var world = Clusters.GetClusterForPosition(MySector.MainCamera.Position);
                if(world != null)
                    StepWorld((HkWorld)world);
            }
            else
            {
                foreach (HkWorld world in Clusters.GetList())
                {
                    StepWorld(world);
                }
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

            EnsureClusterSpace();

            foreach (var rb in m_iterationBodies)
            {
                MyPhysicsBody body = (MyPhysicsBody)rb.UserObject;
                if (body == null)
                    continue;
                body.OnMotion(rb, VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
            }

            foreach (HkCharacterRigidBody rb in m_characterIterationBodies)
            {
                var body = (MyPhysicsBody)rb.GetHitRigidBody().UserObject;
                if (Vector3D.DistanceSquared(body.Entity.WorldMatrix.Translation, body.GetWorldMatrix().Translation) > 0.0001f)
                {
                    body.UpdateCluster();
                }
            }

            m_iterationBodies.Clear();
            m_characterIterationBodies.Clear();

            if (Sync.IsServer && MyFakes.MP_SYNC_CLUSTERTREE && ClustersNeedSync)
            {
                List<BoundingBoxD> list = new List<BoundingBoxD>();
                SerializeClusters(list);

                VRage.Trace.MyTrace.Send(VRage.Trace.TraceWindow.MPositions2, "Tree_OnClustersReordered Server (" + list.Count + ")");
                MyMultiplayer.RaiseStaticEvent(s => OnClustersReorderer, list);

                ClustersNeedSync = false;
            }

            ProfilerShort.End();

            ProfilerShort.Begin("HavokWorld.StepVDB");

            if (MySession.Static.ControlledEntity != null
                    && MySession.Static.ControlledEntity.Entity.GetTopMostParent().GetPhysicsBody() != null)
            {
                foreach (HkWorld world in Clusters.GetList())
                {
                    if (MySession.Static.ControlledEntity.Entity.GetTopMostParent().GetPhysicsBody().HavokWorld == world)
                    {
                        world.VisualDebuggerEnabled = true;
                        world.StepVDB(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
                    } 
                    else
                        world.VisualDebuggerEnabled = false;
                }
            }
            else
            {
                foreach (HkWorld world in Clusters.GetList())
                {
                    world.StepVDB(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
                    break;
                }
            }

            ProfilerShort.End();
            MySimpleProfiler.End("Physics");
        }

        private static void StepWorld(HkWorld world)
        {
            world.ExecutePendingCriticalOperations();
            
            world.UnmarkForWrite();
            world.StepSimulation(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
            world.MarkForWrite();
        }


        private static void ProcessDestructions()
        {
            ProfilerShort.Begin("Destruction");
            int counter = 0;
            while (m_destructionQueue.Count > 0)
            {
                counter++;
                var destructionInfo = m_destructionQueue.Dequeue();
                Debug.Assert(destructionInfo.Entity.Physics.RigidBody == destructionInfo.Details.GetBreakingBody());
                var details = destructionInfo.Details;
                if (details.IsValid())
                {
                    details.Flag = details.Flag | HkdFractureImpactDetails.Flags.FLAG_DONT_DELAY_OPERATION;
                    // this is here to give us more details about a crash, it is to be removed later
                    Debug.WriteLineIf(details.GetBreakingBody() == null, "Oops! Breaking Body is null");
                    Debug.WriteLineIf(details.GetBreakingBody().BreakableBody == null, "Oops! Breakable Body is null");
                    Debug.WriteLineIf(details.GetBreakingBody().BreakableBody.BreakableShape == null, "Oops! Breakable Shape is null");
                    for (int i = 0; i < details.GetBreakingBody().BreakableBody.BreakableShape.GetChildrenCount(); i++)
                    {
                        var child = details.GetBreakingBody().BreakableBody.BreakableShape.GetChild(i);
                        Debug.Assert(child.Shape.IsValid());
                        var strength = child.Shape.GetStrenght();
                        for(int j = 0; j < child.Shape.GetChildrenCount(); j++)
                        {
                            var child2 = child.Shape.GetChild(j);
                            Debug.Assert(child2.Shape.IsValid());
                            strength = child2.Shape.GetStrenght();
                        }
                    }

                    // this is here to give us more details about a crash, it is to be removed later
                    Debug.WriteLineIf(destructionInfo.World == null, "Oops! World is null");
                    Debug.WriteLineIf(destructionInfo.World.DestructionWorld == null, "Oops! Destruction World is null");
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
                MyGamePruningStructure.GetTopMostEntitiesInBox(ref box, m_tmpEntityResults, MyEntityQueryType.Dynamic);
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
            //details.dbgTrace = new System.Diagnostics.StackTrace();
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
            ProfilerShort.Begin("MyPhysics.RemoveDestructions");
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
            ProfilerShort.End();
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
                MyRenderProxy.DebugDrawOBB(oriented, Color.Green, .2f, false, false);

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

                MyRenderProxy.DebugDrawOBB(oriented, Color.Blue, .2f, false, false);
            }

            foreach (var res in m_resultWorlds)
            {
                BoundingBoxD scaledBox = new BoundingBoxD((res.AABB.Min - center) * scaleAxis, (res.AABB.Max - center) * scaleAxis);

                MyOrientedBoundingBoxD oriented = new MyOrientedBoundingBoxD(scaledBox, previewMatrix);

                MyRenderProxy.DebugDrawOBB(oriented, Color.White, .2f, false, false);

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
                    if (rb.GetEntity(0) == null)
                        continue;

                    MyOrientedBoundingBoxD rbbb = new MyOrientedBoundingBoxD((BoundingBoxD)rb.GetEntity(0).LocalAABB, rb.GetEntity(0).WorldMatrix);
                    rbbb.Center = (rbbb.Center - center) * scaleAxis;
                    rbbb.HalfExtent *= scaleAxis;
                    rbbb.Transform(previewMatrix);

                    Color color = Color.Yellow;

                    if (rb.GetEntity(0).LocalAABB.Size.Max() > 1000)
                    {
                        color = Color.Red;
                    }

                    MyRenderProxy.DebugDrawOBB(rbbb, color, 1, false, false);

                    Vector3D velocity = rb.LinearVelocity;
                    velocity = Vector3D.TransformNormal(velocity, previewMatrix) * 10;
                    MyRenderProxy.DebugDrawLine3D(rbbb.Center, rbbb.Center + velocity, Color.Red, Color.White, false);
                }
            }
        }

        #region Havok worlds wrapper

        [ThreadStatic]
        static List<MyHavokCluster.MyClusterQueryResult> m_resultWorldsPerThread;

        static List<MyHavokCluster.MyClusterQueryResult> m_resultWorlds
        {
            get
            {
                if (m_resultWorldsPerThread == null)
                    m_resultWorldsPerThread = new List<MyHavokCluster.MyClusterQueryResult>();
                return m_resultWorldsPerThread;
            }
        }

        [ThreadStatic]
        static List<HkWorld.HitInfo> m_resultHitsPerThread;

        static List<HkWorld.HitInfo> m_resultHits
        {
            get
            {
                if (m_resultHitsPerThread == null)
                    m_resultHitsPerThread = new List<HkWorld.HitInfo>();
                return m_resultHitsPerThread;
            }
        }

        public static void CastRay(Vector3D from, Vector3D to, List<HitInfo> toList, int raycastFilterLayer = 0)
        {
            using (m_raycastLock.Acquire())
            {
            m_resultWorlds.Clear();
            Clusters.CastRay(from, to, m_resultWorlds);

            toList.Clear();

            foreach (var world in m_resultWorlds)
            {
                Vector3D fromF = (Vector3D)(from - world.AABB.Center);
                Vector3D toF = (Vector3D)(to - world.AABB.Center);

                m_resultHits.Clear();


                    HkWorld havokWorld = (HkWorld)(world.UserData);
                    if (havokWorld != null)
                    {
                    havokWorld.CastRay(fromF, toF, m_resultHits, raycastFilterLayer);
                    }

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
            }

            m_resultWorlds.Clear();
        }

        public static HitInfo? CastRay(Vector3D from, Vector3D to, int raycastFilterLayer = 0)
        {
            m_resultWorlds.Clear();
            Clusters.CastRay(from, to, m_resultWorlds);

            HitInfo? hitInfo = CastRayInternal(from, to, m_resultWorlds, raycastFilterLayer);

            m_resultWorlds.Clear();

            return hitInfo;
        }

        /// <summary>
        /// Cast a ray on given worlds and returns closest one from all of the worlds. WARNING: It does not clear worlds list after.
        /// </summary>
        /// <param name="from">Start of ray.</param>
        /// <param name="to">End of ray.</param>
        /// <param name="worlds">Worlds to make test on.</param>
        /// <param name="raycastFilterLayer">Collision filter.</param>
        /// <returns>Hit info. Null if no hit registered.</returns>
        private static HitInfo? CastRayInternal(Vector3D from, Vector3D to, List<MyHavokCluster.MyClusterQueryResult> worlds, int raycastFilterLayer = 0)
        {
            float closestHitFraction = float.MaxValue;
            HitInfo? hitInfo = null;

            foreach (var world in m_resultWorlds)
            {
                Vector3 fromF = (Vector3)(from - world.AABB.Center);
                Vector3 toF = (Vector3)(to - world.AABB.Center);

                HkWorld.HitInfo? info = ((HkWorld)world.UserData).CastRay(fromF, toF, raycastFilterLayer);

                if (info.HasValue && info.Value.HitFraction < closestHitFraction)
                {
                    Vector3D pos = (Vector3D)info.Value.Position + world.AABB.Center;
                    hitInfo = new HitInfo(info.Value, pos);
                    closestHitFraction = info.Value.HitFraction;

                    return hitInfo;
                }
            }

            return null;
        }

        public static bool CastRay(Vector3D from, Vector3D to, out HitInfo hitInfo, uint raycastCollisionFilter, bool ignoreConvexShape)
        {
            m_resultWorlds.Clear();
            Clusters.CastRay(from, to, m_resultWorlds);

            hitInfo = new HitInfo();
            foreach (var world in m_resultWorlds)
            {
                Vector3 fromF = from - world.AABB.Center;
                Vector3 toF = to - world.AABB.Center;

                m_resultHits.Clear();
                HkWorld.HitInfo info = new HkWorld.HitInfo();
                bool hit = ((HkWorld)world.UserData).CastRay(fromF, toF, out info, raycastCollisionFilter, ignoreConvexShape);

                if (hit)
                {
                    hitInfo.Position = (Vector3D)info.Position + world.AABB.Center;
                    hitInfo.HkHitInfo = info;
                    m_resultWorlds.Clear();
                    return hit;
                }
            }
            m_resultWorlds.Clear();

            return false;
        }

        private static bool nPressed = false;
        /// <summary>
        /// Used for saving result of search in CastLongRay. (For optimalisation rules)
        /// </summary>
        private static List<MyLineSegmentOverlapResult<MyVoxelBase>> m_foundEntities = new List<MyLineSegmentOverlapResult<MyVoxelBase>>();
        /// <summary>
        /// Finds closest or any object on the path of the ray from->to. Uses Storage for voxels for faster 
        /// search but only good for long rays (more or less more than 50m). Use it only in such cases.
        /// </summary>
        /// <param name="from">Start of the ray.</param>
        /// <param name="to">End of the ray.</param>
        /// <param name="any">Indicates if method should return any object found (May not be closest)</param>
        /// <returns>Hit info.</returns>
        public static HitInfo? CastLongRay(Vector3D from, Vector3D to, bool any = false)
        {

            //Debug.Assert((to - from).LengthSquared() >= 2500, "You are using ray shorter than 50m. It may be not efficient. Use CastRay instead.");

            m_resultWorlds.Clear();
            Clusters.CastRay(from, to, m_resultWorlds);

            HitInfo? info = null;

            //Raycas physics without voxels
            info = CastRayInternal(from, to, m_resultWorlds, CollisionLayers.NoVoxelCollisionLayer);

            // trim original ray to be only long as hitted position
            if (info.HasValue)
            {
                // If any than return
                if(any)
                {
                    m_resultWorlds.Clear();
                    return info;
                }

                to = (Vector3D)info.Value.Position + info.Value.Position;
            }

            // Find intersection with the new ray in Voxel Storage.
            LineD rayLine = new LineD(from, to);

            MyGamePruningStructure.GetVoxelMapsOverlappingRay(ref rayLine, m_foundEntities);

            double startOffset = 1.0;
            double endOffset = 0;
            bool cont = false;

            foreach (var voxelResult in m_foundEntities)
            {
                if (voxelResult.Element.GetOrePriority() != -1) continue;

                MyVoxelBase foundVoxel = voxelResult.Element.RootVoxel;
                ProfilerShort.Begin("MyGamePruningStructure::FoundVoxelAlgo");
                if (foundVoxel.Storage.DataProvider != null)
                {
                    Vector3D start = Vector3D.Transform(rayLine.From, foundVoxel.PositionComp.WorldMatrixInvScaled);
                    start += foundVoxel.SizeInMetresHalf;
                    var end = Vector3D.Transform(rayLine.To, foundVoxel.PositionComp.WorldMatrixInvScaled);
                    end += foundVoxel.SizeInMetresHalf;
                    var localVoxRay = new LineD(start, end);

                    double localStartOffset;
                    double localEndOffset;

                    // Intersect provider for nau
                    cont = foundVoxel.Storage.DataProvider.Intersect(ref localVoxRay, out localStartOffset, out localEndOffset);

                    if (cont)
                    {
                        // Trim original ray to be only as long as predicted ray from intersection on Storage.
                        if (localStartOffset < startOffset)
                        {
                            startOffset = localStartOffset;
                            
                        }

                        if (localEndOffset > endOffset)
                        {
                            endOffset = localEndOffset;
                            
                        }
                    }
                }
                ProfilerShort.End();
            }

            if (!cont)
                return info;
            
            to = from + rayLine.Direction * rayLine.Length * endOffset;
            from = from + rayLine.Direction * rayLine.Length * startOffset;

            m_foundEntities.Clear();
            // Make final raycast to find final hit entity (either voxel or something else)
            ProfilerShort.Begin("MyGamePruningStructure::VoxelCollisionLayer");
            HitInfo? infoVoxel = CastRayInternal(from, to, m_resultWorlds, CollisionLayers.VoxelCollisionLayer);

            if (info == null)
                return infoVoxel;

            if (infoVoxel.HasValue && info.HasValue)
            {
                if (infoVoxel.Value.HkHitInfo.HitFraction < info.Value.HkHitInfo.HitFraction)
                    return infoVoxel;
            }

            ProfilerShort.End();

            m_resultWorlds.Clear();

            return info;
        }

        public static void GetPenetrationsShape(HkShape shape, ref Vector3D translation, ref Quaternion rotation, List<HkBodyCollision> results, int filter)
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

        public static float? CastShapeInAllWorlds(Vector3D to, HkShape shape, ref MatrixD transform, int filterLayer, float extraPenetration = 0)
        {
            m_resultWorlds.Clear();
            Clusters.CastRay(transform.Translation, to, m_resultWorlds);

            foreach (var world in m_resultWorlds)
            {
                Matrix transformF = transform;
                transformF.Translation = (Vector3)(transform.Translation - world.AABB.Center);

                Vector3 toF = (Vector3)(to - world.AABB.Center);

                var hitValue = ((HkWorld)world.UserData).CastShape(toF, shape, ref transformF, filterLayer, extraPenetration);
                if (hitValue.HasValue)
                {
                    m_resultWorlds.Clear();
                    return hitValue;
                }
            }

            m_resultWorlds.Clear();

            return null;
        }

        public static void GetPenetrationsBox(ref Vector3 halfExtents, ref Vector3D translation, ref Quaternion rotation, List<HkBodyCollision> results, int filter)
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

        public static HitInfo? CastShapeReturnContactBodyData(Vector3D to, HkShape shape, ref MatrixD transform, uint collisionFilter, float extraPenetration, bool ignoreConvexShape = true)
        {
            m_resultWorlds.Clear();
            Clusters.Intersects(to, m_resultWorlds);

            if (m_resultWorlds.Count == 0)
                return null;

            var world = m_resultWorlds[0];

            Matrix transformF = transform;
            transformF.Translation = (Vector3)(transform.Translation - world.AABB.Center);

            Vector3 toF = (Vector3)(to - world.AABB.Center);

            HkWorld.HitInfo? result = ((HkWorld)world.UserData).CastShapeReturnContactBodyData(toF, shape, ref transformF, collisionFilter, extraPenetration);
            if (result == null)
            {
                return null;
            }
            HkWorld.HitInfo cpd = result.Value;
            HitInfo hitInfo = new HitInfo(cpd, cpd.Position + world.AABB.Center);
            return hitInfo;
        }


        static List<HkWorld.HitInfo?> m_resultShapeCasts = new List<HkWorld.HitInfo?>();

        public static bool CastShapeReturnContactBodyDatas(Vector3D to, HkShape shape, ref MatrixD transform, uint collisionFilter, float extraPenetration, List<HitInfo> result, bool ignoreConvexShape = true)
        {
            m_resultWorlds.Clear();
            Clusters.Intersects(to, m_resultWorlds);

            if (m_resultWorlds.Count == 0)
                return false;

            var world = m_resultWorlds[0];

            Matrix transformF = transform;
            transformF.Translation = (Vector3)(transform.Translation - world.AABB.Center);

            Vector3 toF = (Vector3)(to - world.AABB.Center);

            m_resultShapeCasts.Clear();

            if (((HkWorld)world.UserData).CastShapeReturnContactBodyDatas(toF, shape, ref transformF, collisionFilter, extraPenetration, m_resultShapeCasts))
            {
                foreach (var res in m_resultShapeCasts)
                {
                    HkWorld.HitInfo cpd = res.Value;
                    HitInfo hitInfo = new HitInfo() 
                    { 
                        HkHitInfo = cpd, 
                        Position = cpd.Position + world.AABB.Center 
                    };
                    result.Add(hitInfo);
                }

                return true;
            }
            
         
            return false;
        }


        public static bool IsPenetratingShapeShape(HkShape shape1, ref Vector3D translation1, ref Quaternion rotation1, HkShape shape2, ref Vector3D translation2, ref Quaternion rotation2)
        {
            //rotations have to be normalized
            rotation1.Normalize();
            rotation2.Normalize();

            //jn: TODO this is world independent test, just transform so shape1 is on zero and querry on any world
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

        public static bool IsPenetratingShapeShape(HkShape shape1, ref Matrix transform1, HkShape shape2, ref Matrix transform2)
        {
            return (Clusters.GetList().First() as HkWorld).IsPenetratingShapeShape(shape1, ref transform1, shape2, ref transform2);
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

        public static int GetCollisionLayer(string strLayer)
        {
            if (strLayer == "LightFloatingObjectCollisionLayer")
                return CollisionLayers.LightFloatingObjectCollisionLayer;
            else if (strLayer == "VoxelLod1CollisionLayer")
                return CollisionLayers.VoxelLod1CollisionLayer;
            else if (strLayer == "NotCollideWithStaticLayer")
                return CollisionLayers.NotCollideWithStaticLayer;
            else if (strLayer == "StaticCollisionLayer")
                return CollisionLayers.StaticCollisionLayer;
            else if (strLayer == "CollideWithStaticLayer")
                return CollisionLayers.CollideWithStaticLayer;
            else if (strLayer == "DefaultCollisionLayer")
                return CollisionLayers.DefaultCollisionLayer;
            else if (strLayer == "DynamicDoubledCollisionLayer")
                return CollisionLayers.DynamicDoubledCollisionLayer;
            else if (strLayer == "KinematicDoubledCollisionLayer")
                return CollisionLayers.KinematicDoubledCollisionLayer;
            else if (strLayer == "CharacterCollisionLayer")
                return CollisionLayers.CharacterCollisionLayer;
            else if (strLayer == "NoCollisionLayer")
                return CollisionLayers.NoCollisionLayer;
            else if (strLayer == "DebrisCollisionLayer")
                return CollisionLayers.DebrisCollisionLayer;
            else if (strLayer == "GravityPhantomLayer")
                return CollisionLayers.GravityPhantomLayer;
            else if (strLayer == "CharacterNetworkCollisionLayer")
                return CollisionLayers.CharacterNetworkCollisionLayer;
            else if (strLayer == "FloatingObjectCollisionLayer")
                return CollisionLayers.FloatingObjectCollisionLayer;
            else if (strLayer == "ObjectDetectionCollisionLayer")
                return CollisionLayers.ObjectDetectionCollisionLayer;
            else if (strLayer == "VirtualMassLayer")
                return CollisionLayers.VirtualMassLayer;
            else if (strLayer == "CollectorCollisionLayer")
                return CollisionLayers.CollectorCollisionLayer;
            else if (strLayer == "AmmoLayer")
                return CollisionLayers.AmmoLayer;
            else if (strLayer == "VoxelCollisionLayer")
                return CollisionLayers.VoxelCollisionLayer;
            else if (strLayer == "ExplosionRaycastLayer")
                return CollisionLayers.ExplosionRaycastLayer;
            else if (strLayer == "CollisionLayerWithoutCharacter")
                return CollisionLayers.CollisionLayerWithoutCharacter;
            else if (strLayer == "RagdollCollisionLayer")
                return CollisionLayers.RagdollCollisionLayer;
            else if (strLayer == "NoVoxelCollisionLayer")
                return CollisionLayers.NoVoxelCollisionLayer;

            Debug.Fail("Cannot convert collision layer string - layer not found");
            return CollisionLayers.DefaultCollisionLayer;
        }

        /// <summary>
        /// Ensure aabb is inside only one subspace. If no, reorder.
        /// </summary>
        /// <param name="aabb"></param>
        public static void EnsurePhysicsSpace(BoundingBoxD aabb)
        {
            Clusters.EnsureClusterSpace(aabb);
        }

        /// <summary>
        /// Change position of object in world. Move object between subspaces if necessary.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="oldAabb"></param>
        /// <param name="aabb"></param>
        /// <param name="velocity"></param>
        public static void MoveObject(ulong id, BoundingBoxD aabb, Vector3 velocity)
        {
            Clusters.MoveObject(id, aabb, velocity);
        }

        /// <summary>
        /// Remove object from world, remove also subspace if empty.
        /// </summary>
        /// <param name="id"></param>
        public static void RemoveObject(ulong id)
        {
            Clusters.RemoveObject(id);
        }

        /// <summary>
        /// Return offset of objects subspace center.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Vector3D GetObjectOffset(ulong id)
        {
            return Clusters.GetObjectOffset(id);
        }

        /// <summary>
        /// Try add object to some subspace.
        /// Create new subspace if allowed (!SingleCluster.HasValue) and needed (object is outside of existing subspaces).
        /// If not allowed, mark object as left the world.
        /// </summary>
        /// <param name="bbox"></param>
        /// <param name="velocity"></param>
        /// <param name="activationHandler"></param>
        /// <param name="customId"></param>
        /// <returns></returns>
        public static ulong AddObject(BoundingBoxD bbox, MyPhysicsBody activationHandler, ulong? customId, string tag, long entityId)
        {
            ulong tmp = Clusters.AddObject(bbox, activationHandler, customId, tag, entityId);

            if (tmp == MyHavokCluster.CLUSTERED_OBJECT_ID_UNITIALIZED)
            {
                HavokWorld_EntityLeftWorld(activationHandler.RigidBody);
                return MyHavokCluster.CLUSTERED_OBJECT_ID_UNITIALIZED;
            }
            return tmp;
            
        }

        public static VRage.Collections.ListReader<object>? GetClusterList()
        {
            if (Clusters == null)
                return null;
            return Clusters.GetList();
        }

        public static void GetAll(List<VRageMath.Spatial.MyClusterTree.MyClusterQueryResult> results)
        {
            Clusters.GetAll(results);
        }

        void EnsureClusterSpace()
        {
            foreach (var actRB in m_iterationBodies)
            {
                if (actRB.LinearVelocity.LengthSquared() > 0.1f)
                {
                    var aabb = MyClusterTree.AdjustAABBByVelocity(actRB.GetEntity(0).WorldAABB, actRB.LinearVelocity);
                    Clusters.EnsureClusterSpace(aabb);
                }
            }

            foreach (var charRB in m_characterIterationBodies)
            {
                if (charRB.LinearVelocity.LengthSquared() > 0.1f)
                {
                    var aabb = MyClusterTree.AdjustAABBByVelocity(((MyPhysicsBody)charRB.GetHitRigidBody().UserObject).Entity.PositionComp.WorldAABB,charRB.LinearVelocity);
                    Clusters.EnsureClusterSpace(aabb);
                }
            }
        }

        public static void SerializeClusters(List<BoundingBoxD> list)
        {
            Clusters.Serialize(list);
        }

        public static void DeserializeClusters(List<BoundingBoxD> list)
        {
            System.Diagnostics.Debug.Assert(MyFakes.MP_SYNC_CLUSTERTREE, "Cannot deserialize clusters if sync is not enabled");

            Clusters.Deserialize(list);
        }

        void Tree_OnClustersReordered()
        {
            ClustersNeedSync = true;
        }
        
        [Event, Reliable, Broadcast]
        private static void OnClustersReorderer(List<BoundingBoxD> list)
        {
            VRage.Trace.MyTrace.Send(VRage.Trace.TraceWindow.MPositions2, "Tree_OnClustersReordered Client (" + list.Count + ")");
            DeserializeClusters(list);
        }

        internal static void ForceClustersReorder()
        {
            Clusters.ReorderClusters(new BoundingBoxD(Vector3D.MinValue, Vector3D.MaxValue));
        }

        //Entirely debug function that has no place here. Remove once a crash with inconsistent clusters is resolved
        public bool GetEntityReplicableExistsById(long entityId)
        {
            var entity = MyEntities.GetEntityByIdOrDefault(entityId);
            if (entity != null)
            {
                return Sandbox.Game.Replication.MyExternalReplicable.FindByObject(entity) != null;
            }
            return false;
        }
    }

}