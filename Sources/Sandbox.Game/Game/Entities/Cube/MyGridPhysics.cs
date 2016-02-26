#region Usings
using Havok;
using Sandbox.Common;
using Sandbox.Common.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Debris;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.Game.Components;
using VRage.ModAPI;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Game.Components;
using Sandbox.Engine.Voxels;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Replication;
using VRage.Game.Entity;
using VRage.Game;
#endregion

namespace Sandbox.Game.Entities.Cube
{
    public partial class MyGridPhysics : MyPhysicsBody
    {
        public struct BoundingBoxI
        {
            public Vector3I Min, Max;
        }

        struct ExplosionInfo
        {
            public Vector3D Position;
            public MyExplosionTypeEnum ExplosionType;
            public float Radius;
            public bool ShowParticles;
            public bool GenerateDebris;

        }

        static readonly float LargeGridDeformationRatio = 1;
        static readonly float SmallGridDeformationRatio = 2.5f;

        static readonly int MaxEffectsPerFrame = 3;

        public static readonly float LargeShipMaxAngularVelocityLimit = MathHelper.ToRadians(18000); // 80 degrees/s
        public static readonly float SmallShipMaxAngularVelocityLimit = MathHelper.ToRadians(36000); // 180 degrees/s

        private const float SPEED_OF_LIGHT_IN_VACUUM = 299792458.0f; // m/s
        public const float MAX_SHIP_SPEED = SPEED_OF_LIGHT_IN_VACUUM / 2.0f;// m/s

        public static float ShipMaxLinearVelocity()
        {
            return Math.Max(LargeShipMaxLinearVelocity(), SmallShipMaxLinearVelocity());
        }


        public static float LargeShipMaxLinearVelocity()
        {
            return Math.Max(0, Math.Min(MAX_SHIP_SPEED, MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed));
        }

        public static float SmallShipMaxLinearVelocity()
        {
            return Math.Max(0, Math.Min(MAX_SHIP_SPEED, MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed));
        }

        public static float GetShipMaxAngularVelocity(MyCubeSize size)
        {
            return size == MyCubeSize.Large ? GetLargeShipMaxAngularVelocity() : GetSmallShipMaxAngularVelocity();
        }

        public static float GetLargeShipMaxAngularVelocity()
        {
            return Math.Max(0, Math.Min(LargeShipMaxAngularVelocityLimit, MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxAngularSpeedInRadians));
        }

        public static float GetSmallShipMaxAngularVelocity()
        {
            if(MyFakes.TESTING_VEHICLES)
                return float.MaxValue;
            return Math.Max(0, Math.Min(SmallShipMaxAngularVelocityLimit, MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxAngularSpeedInRadians));
        }

        public int DisableGravity = 0;

        static readonly int SparksEffectDelayPerContactMs = 1000;

        static readonly Dictionary<Vector3I, MySlimBlock> m_tmpBoneList = new Dictionary<Vector3I, MySlimBlock>(Vector3I.Comparer);

        //nodes in weld group for destruction
        private HashSet<MyEntity> m_tmpEntities = new HashSet<MyEntity>();
        List<ushort> m_tmpContactId = new List<ushort>();
        Dictionary<ushort, int> m_lastContacts = new Dictionary<ushort, int>();

        MyCubeGrid m_grid;
        MyGridShape m_shape;

        public MyGridShape Shape { get { return m_shape; } }

        bool m_potentialDisconnects = false;

        List<ExplosionInfo> m_explosions = new List<ExplosionInfo>();

        int m_numContacts = 0;

        const int MAX_NUM_CONTACTS_PER_FRAME = 10;

        /// <summary>
        /// Information about dirty (added, removed,...) blocks.
        /// </summary>
        public class MyDirtyBlocksInfo
        {
            // Dirty areas (no necessarilly all blocks in area are dirty)
            public List<BoundingBoxI> DirtyParts = new List<BoundingBoxI>();

            // List of dirty blocks
            public HashSet<Vector3I> DirtyBlocks = new HashSet<Vector3I>();


            public void Clear()
            {
                DirtyParts.Clear();
                DirtyBlocks.Clear();
            }
        }

        // Dirty and added cube blocks.
        MyDirtyBlocksInfo m_dirtyCubesInfo = new MyDirtyBlocksInfo();

        public HashSetReader<Vector3I> DirtyCubes { get { return new HashSetReader<Vector3I>(m_dirtyCubesInfo.DirtyBlocks); } }

        List<Vector3I> m_tmpCubeList = new List<Vector3I>(8);

        int m_effectsPerFrame = 0;

        public MyGridPhysics(MyCubeGrid grid, MyGridShape shape = null)
            : base(grid, GetFlags(grid))
        {
            m_grid = grid;
            m_shape = shape;
            DeformationRatio = m_grid.GridSizeEnum == MyCubeSize.Large ? LargeGridDeformationRatio : SmallGridDeformationRatio;
            MaterialType = MyMaterialType.SHIP;
            CreateBody();

            if (MyFakes.ENABLE_PHYSICS_HIGH_FRICTION)
                Friction = MyFakes.PHYSICS_HIGH_FRICTION;
        }

        public float DeformationRatio;

        public override void Close()
        {
            base.Close();

            if (m_shape != null)
            {
                m_shape.Dispose();
                m_shape = null;
            }
        }

        public override int HavokCollisionSystemID
        {
            get
            {
                return base.HavokCollisionSystemID;
            }
            protected set
            {
                if (HavokCollisionSystemID != value)
                {
                    base.HavokCollisionSystemID = value;
                    m_grid.HavokSystemIDChanged(value);
                }
            }
        }

        static RigidBodyFlag GetFlags(MyCubeGrid grid)
        {
            return grid.IsStatic ? RigidBodyFlag.RBF_STATIC : (grid.GridSizeEnum == MyCubeSize.Large ? MyPerGameSettings.LargeGridRBFlag : RigidBodyFlag.RBF_DEFAULT);
        }

        void CreateBody()
        {
            if (m_shape == null)
                m_shape = new MyGridShape(m_grid);

            // TODO: this is hack, but cannot be handled better, this fixes bug...ship won't rotate
            if (m_grid.GridSizeEnum == MyCubeSize.Large && !IsStatic)
            {
                InitialSolverDeactivation = HkSolverDeactivation.Off;
            }
            ContactPointDelay = 0;
            CreateFromCollisionObject(m_shape, Vector3.Zero, MatrixD.Identity, m_shape.MassProperties);

            ProfilerShort.Begin("Setup");
            {
                RigidBody.CallbackLimit = MAX_NUM_CONTACTS_PER_FRAME;
                RigidBody.ContactPointCallbackEnabled = true;
                RigidBody.ContactSoundCallbackEnabled = true;
                if (!MyPerGameSettings.Destruction)
                {
                    RigidBody.ContactPointCallback += RigidBody_ContactPointCallback;
                }
                else
                {
                    RigidBody.ContactPointCallback += RigidBody_ContactPointCallback_Destruction;
                    BreakableBody.BeforeControllerOperation += BreakableBody_BeforeControllerOperation;
                    BreakableBody.AfterControllerOperation += BreakableBody_AfterControllerOperation;
                }

                RigidBody.LinearDamping = MyPerGameSettings.DefaultLinearDamping;
                RigidBody.AngularDamping = MyPerGameSettings.DefaultAngularDamping;


                if (m_grid.BlocksDestructionEnabled)
                {
                    RigidBody.BreakLogicHandler = BreakLogicHandler;
                    RigidBody.BreakPartsHandler = BreakPartsHandler;
                }

                if (RigidBody2 != null)
                {
                    RigidBody2.ContactPointCallbackEnabled = true;
                    if (!MyPerGameSettings.Destruction)
                    {
                        RigidBody2.ContactPointCallback += RigidBody_ContactPointCallback;
                    }
                    if (m_grid.BlocksDestructionEnabled)
                    {
                        RigidBody2.BreakPartsHandler = BreakPartsHandler;
                        RigidBody2.BreakLogicHandler = BreakLogicHandler;
                    }
                }
            }

            var flags = GetFlags(m_grid);

            if (IsStatic)
            {
                RigidBody.Layer = MyPhysics.CollisionLayers.StaticCollisionLayer;
                RigidBody.MaxAngularVelocity = GetLargeShipMaxAngularVelocity();
                RigidBody.MaxLinearVelocity = LargeShipMaxLinearVelocity();
            }
            else if (m_grid.GridSizeEnum == MyCubeSize.Large)
            {
                RigidBody.MaxAngularVelocity = GetLargeShipMaxAngularVelocity();
                RigidBody.MaxLinearVelocity = LargeShipMaxLinearVelocity();
                RigidBody.Layer = flags == RigidBodyFlag.RBF_DOUBLED_KINEMATIC && MyFakes.ENABLE_DOUBLED_KINEMATIC ? MyPhysics.CollisionLayers.DynamicDoubledCollisionLayer : MyPhysics.CollisionLayers.DefaultCollisionLayer;
            }
            else if (m_grid.GridSizeEnum == MyCubeSize.Small)
            {
                RigidBody.MaxAngularVelocity = GetSmallShipMaxAngularVelocity();
                RigidBody.MaxLinearVelocity = SmallShipMaxLinearVelocity();
                RigidBody.Layer = MyPhysics.CollisionLayers.DefaultCollisionLayer;
            }

            if (RigidBody2 != null)
            {
                RigidBody2.Layer = MyPhysics.CollisionLayers.KinematicDoubledCollisionLayer;
            }

            if (MyPerGameSettings.BallFriendlyPhysics)
            {
                RigidBody.Restitution = 0;
                if (RigidBody2 != null)
                    RigidBody2.Restitution = 0;
            }

            ProfilerShort.End();
            ProfilerShort.Begin("Enable");
            Enabled = true;
            ProfilerShort.End();
        }

        //jn:TODO this is simple enough to be moved to Native
        private HkBreakOffLogicResult BreakLogicHandler(HkRigidBody otherBody, uint shapeKey, float maxImpulse)
        {
            if(otherBody.GetSingleEntity() is MyCharacter)
                return HkBreakOffLogicResult.DoNotBreakOff;
            if (LinearVelocity.LengthSquared() < 4 && otherBody.LinearVelocity.LengthSquared() < 4)
            {
                //might be optimization, need to re-mark on motion though
                //Shape.UnmarkBreakable(HavokWorld, RigidBody);
                //if(RigidBody2 != null)
                //    Shape.UnmarkBreakable(HavokWorld, RigidBody2);

                //highly improves performance (2.5* + earlier going to sleep)
                return HkBreakOffLogicResult.DoNotBreakOff;
            }
            return HkBreakOffLogicResult.UseLimit;
        }

        protected override void ActivateCollision()
        {
            if(m_world != null)
                HavokCollisionSystemID = m_world.GetCollisionFilter().GetNewSystemGroup();//grids now have unique group, so collision between [sub]parts can be filtered out
        }

        public override void Activate(object world, ulong clusterObjectID)
        {
            if(MyPerGameSettings.Destruction)
            {
                Shape.FindConnectionsToWorld();
            }
            base.Activate(world, clusterObjectID);

            MarkBreakable((HkWorld)world);
        }

        public override void ActivateBatch(object world, ulong clusterObjectID)
        {
            if (MyPerGameSettings.Destruction)
            {
                Shape.FindConnectionsToWorld();
            }
            base.ActivateBatch(world, clusterObjectID);

            MarkBreakable((HkWorld)world);
            //DestructionBody.ConnectToWorld((HkWorld)world, 0.05f);
        }

        public override void Deactivate(object world)
        {
            UnmarkBreakable((HkWorld)world);

            base.Deactivate(world);
        }

        public override void DeactivateBatch(object world)
        {
            UnmarkBreakable((HkWorld)world);

            base.DeactivateBatch(world);
        }

        public override HkShape GetShape()
        {
            return Shape;
        }

        private void MarkBreakable(HkWorld world)
        {
            if (!m_grid.BlocksDestructionEnabled)
                return;
            m_shape.MarkBreakable(world, RigidBody);

            if (RigidBody2 != null)
            {
                m_shape.MarkBreakable(world, RigidBody2);
            }
        }

        private void UnmarkBreakable(HkWorld world)
        {
            if (!m_grid.BlocksDestructionEnabled)
                return;
            if (m_shape != null)
                    m_shape.UnmarkBreakable(world, RigidBody);

            if (RigidBody2 != null)
            {
                m_shape.UnmarkBreakable(world, RigidBody2);
            }
        }

        HashSet<MySlimBlock> m_blocksInContact = new HashSet<MySlimBlock>();
        private List<MyPhysics.HitInfo> m_hitList = new List<MyPhysics.HitInfo>();

        void RigidBody_ContactPointCallback(ref HkContactPointEvent value)
        {
            ProfilerShort.Begin("Grid Contact counter");
            ProfilerShort.End();

            var otherEntity = value.GetOtherEntity(m_grid);
            var otherPhysicsBody = value.GetPhysicsBody(0);
            var thisEntity = m_grid;
            if (otherEntity == null || thisEntity == null)
                return;

            //DA used to stop appliyng force when there is planet/ship collisions to  increase performance after ship crashes on planet
            if ((Math.Abs(value.SeparatingVelocity) < 0.3f) && (otherEntity is MyTrees || otherEntity is MyVoxelPhysics))
            {
                return;
            }


            MyGridContactInfo info = new MyGridContactInfo(ref value, m_grid);

            var myBody = RigidBody;// value.Base.BodyA.GetEntity() == m_grid.Components ? value.Base.BodyA : value.Base.BodyB;

            // CH: DEBUG


            if (info.CollidingEntity is Sandbox.Game.Entities.Character.MyCharacter || info.CollidingEntity.MarkedForClose)
                return;

            if (MyFakes.LANDING_GEAR_IGNORE_DAMAGE_CONTACTS && MyCubeGridGroups.Static.NoContactDamage.HasSameGroupAndIsGrid(otherEntity, thisEntity))
                return;


            ProfilerShort.Begin("Grid contact point callback");
            bool hitVoxel = info.CollidingEntity is MyVoxelMap || info.CollidingEntity is MyVoxelPhysics;

            if(hitVoxel && m_grid.Render != null) {
                m_grid.Render.ResetLastVoxelContactTimer();
            }

            bool doSparks = MyPerGameSettings.EnableCollisionSparksEffect && (info.CollidingEntity is MyCubeGrid || hitVoxel);

            // According to Petr, WasUsed does not work everytime
            //if (value.ContactProperties.WasUsed)
            {
                // Handle callbacks here
                info.HandleEvents();
            }

            if(MyDebugDrawSettings.DEBUG_DRAW_FRICTION)
            {
                var pos = ClusterToWorld(value.ContactPoint.Position);
                var vel = -GetVelocityAtPoint(pos);
                vel *= 0.1f;
                var fn = Math.Abs(Gravity.Dot(value.ContactPoint.Normal) * value.ContactProperties.Friction);
                if (vel.Length() > 0.5f)
                {
                    vel.Normalize();
                    MyRenderProxy.DebugDrawArrow3D(pos, pos + fn * vel, Color.Gray, Color.Gray, false);
                }
            }

            if (doSparks && value.SeparatingVelocity > 2.0f && value.ContactProperties.WasUsed && !m_lastContacts.ContainsKey(value.ContactPointId) && info.EnableParticles)
            {
                ProfilerShort.Begin("AddCollisionEffect");
                m_lastContacts[value.ContactPointId] = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                AddCollisionEffect(info.ContactPosition, value.ContactPoint.Normal);
                ProfilerShort.End();
            }

            ProfilerShort.Begin("Dust");
            bool doDust = MyPerGameSettings.EnableCollisionSparksEffect && hitVoxel;
            float force = Math.Abs(value.SeparatingVelocity * (Mass / 100000));
            if (doDust && force > 0.25f && info.EnableParticles)
            {
                float scale = MathHelper.Clamp(force / 10.0f, 0.2f, 4.0f);
                AddDustEffect(info.ContactPosition, scale);
            }
            ProfilerShort.End();
            // Large dynamic ships colliding with floating objects
            // When low separating velocity or deformation performed, disable contact point
            // Floating object will still collide with kinematic part of ship and won't push it
            if (m_grid.GridSizeEnum == MyCubeSize.Large && !myBody.IsFixedOrKeyframed && info.CollidingEntity is MyFloatingObject && (Math.Abs(value.SeparatingVelocity) < 0.2f))
            {
                var prop = value.ContactProperties;
                prop.IsDisabled = true;
            }

            ProfilerShort.End();
        }

        private static Vector3 GetGridPosition(HkContactPointEvent value, MyCubeGrid grid, int body)
        {
            var position = value.ContactPoint.Position + (body == 0 ? 0.1f : -0.1f) * value.ContactPoint.Normal;
            var local = Vector3.Transform(position, Matrix.Invert(value.Base.GetRigidBody(body).GetRigidBodyMatrix()));
            return local;
        }

        private MyGridContactInfo ReduceVelocities(MyGridContactInfo info)
        {
            info.Event.AccessVelocities(0);
            info.Event.AccessVelocities(1);
            if (!info.CollidingEntity.Physics.IsStatic && info.CollidingEntity.Physics.Mass < 600)
                info.CollidingEntity.Physics.LinearVelocity /= 2;
            if (!this.IsStatic && MyDestructionHelper.MassFromHavok(Mass) < 600)
                LinearVelocity /= 2;
            info.Event.UpdateVelocities(0);
            info.Event.UpdateVelocities(1);
            return info;
        }

        HkBreakOffPointInfo CreateBreakOffPoint(HkContactPointEvent value, Vector3D contactPosition, float breakImpulse)
        {
            return new HkBreakOffPointInfo()
            {
                ContactPoint = value.ContactPoint,
                ContactPosition = contactPosition,
                ContactPointProperties = value.ContactProperties,
                IsContact = true,
                BreakingImpulse = breakImpulse,
                CollidingBody = value.Base.BodyA == RigidBody ? value.Base.BodyB : value.Base.BodyA,
                ContactPointDirection = value.Base.BodyB == RigidBody ? -1 : 1,
            };
        }

        private bool BreakAtPoint(ref HkBreakOffPointInfo pt, ref HkArrayUInt32 brokenKeysOut)
        {
            //if (pt.BreakingImpulse < Shape.BreakImpulse * 0.75f && MyUtils.GetRandomInt(3) != 0)
                //return false;
            if(RigidBody == null || pt.CollidingBody == null)
            {
                VRage.Trace.MyTrace.Send(VRage.Trace.TraceWindow.Analytics, string.Format("BreakAtPoint:Breaking body {0} null", RigidBody == null ? "A" : "B"));
                return false;
            }
            var separatingVelocity = Math.Abs(HkUtils.CalculateSeparatingVelocity(RigidBody, pt.CollidingBody, ref pt.ContactPoint));
            //if (separatingVelocity < 2)
            //    return false;
            var otherEntity = pt.CollidingBody.GetEntity(0);
            if (otherEntity is MyEnvironmentItems) //jn:HACK
                return false;
            pt.ContactPosition = ClusterToWorld(pt.ContactPoint.Position);

            var destroyed = PerformDeformationOnGroup((MyEntity)Entity, (MyEntity)otherEntity, ref pt, separatingVelocity);
            pt.ContactPointDirection *= -1;
            destroyed |= PerformDeformationOnGroup((MyEntity)otherEntity, (MyEntity)Entity, ref pt, separatingVelocity);
            return destroyed;
        }

        private bool PerformDeformationOnGroup(MyEntity entity, MyEntity other, ref HkBreakOffPointInfo pt, float separatingVelocity)
        {
            bool destroyed = false;
            var group = MyWeldingGroups.Static.GetGroup((MyEntity)entity);
            if (group != null)
            {
                foreach (var node in group.Nodes)
                {
                    if (node.NodeData.MarkedForClose)
                        continue;
                    m_tmpEntities.Add(node.NodeData);
                }
                foreach (var node in m_tmpEntities)
                {
                    var gp = node.Physics as MyGridPhysics;
                    if (gp == null || gp.Entity.PositionComp.WorldAABB.Contains(pt.ContactPosition) == ContainmentType.Disjoint)
                        continue;
                    destroyed |= gp.PerformDeformation(ref pt, false, separatingVelocity, other as MyEntity);
                }
                m_tmpEntities.Clear();
            }
            return destroyed;
        }

        private bool BreakPartsHandler(ref HkBreakOffPoints breakOffPoints, ref HkArrayUInt32 brokenKeysOut)
        {
            if (!Sync.IsServer)
                return true;
            if (RigidBody == null || Entity.MarkedForClose)
                return true;
            ProfilerShort.Begin("BreakPartsHandler");
            Debug.Assert(breakOffPoints.Count > 0);
            bool recollide = false;
            Vector3D pos = Vector3D.Zero;
            for (int i = 0; i < breakOffPoints.Count; i++)
            {
                var pt = breakOffPoints[i];
                if (pt.CollidingBody != null)
                {

                    var separatingVelocity = Math.Abs(HkUtils.CalculateSeparatingVelocity(RigidBody, pt.CollidingBody, ref pt.ContactPoint));
                    if (separatingVelocity < 2)
                        continue;
                    if (Vector3D.DistanceSquared(pos, pt.ContactPoint.Position) < 9)
                        continue;
                    pos = pt.ContactPoint.Position;
                    ProfilerShort.CustomValue("BreakAtPoint", 1, 1);
                    recollide |= BreakAtPoint(ref pt, ref brokenKeysOut);
                }
            }
            ProfilerShort.End();
            return true;
        }

        /// <summary>
        /// Calculates soft coeficient at target point
        /// </summary>
        private static float CalculateSoften(float softAreaPlanarInv, float softAreaVerticalInv, ref Vector3 normal, Vector3 contactToTarget)
        {
            float planeDist = Math.Abs(Vector3.Dot(normal, contactToTarget));
            float vertSoft = 1 - planeDist * softAreaVerticalInv;
            if (vertSoft < 0)
                return 0;
            float flatDist = (float)Math.Sqrt(Math.Max(0, contactToTarget.LengthSquared() - planeDist * planeDist));

            float flatSoft = Math.Max(0, 1 - flatDist * softAreaPlanarInv);

            return vertSoft * flatSoft;
        }

        public void PerformMeteoritDeformation(ref HkBreakOffPointInfo pt, float separatingVelocity)
        {
            ProfilerShort.Begin("PerformDeformation");

            Debug.Assert(Sync.IsServer, "Function PerformDeformation should not be called from client");

            //MyGridContactInfo info = new MyGridContactInfo();

            //var block = MyGridContactInfo.GetContactBlock(m_grid, pt.ContactPosition, pt.ContactPoint.NormalAndDistance.W);
            //block.FatBlock.ContactPointCallback()

            // Calculate deformation offset
            float deformationOffset = 0.3f + Math.Max(0, ((float)Math.Sqrt(Math.Abs(separatingVelocity) + Math.Pow(pt.CollidingBody.Mass, 0.72))) / 10);
            deformationOffset *= 6;

            const float maxDeformationHardLimit = 5; // Max offset is 5 for meteors
            deformationOffset = Math.Min(deformationOffset, maxDeformationHardLimit);

            float softAreaPlanar = (float)Math.Pow(pt.CollidingBody.Mass, 0.15f);
            softAreaPlanar -= 0.3f;
            softAreaPlanar *= m_grid.GridSizeEnum == MyCubeSize.Large ? 4 : 1f; // About 4 meters for large grid and 1m for small
            float softAreaVertical = deformationOffset;
            softAreaVertical *= m_grid.GridSizeEnum == MyCubeSize.Large ? 1 : 0.2f;

            var invWorld = m_grid.PositionComp.WorldMatrixNormalizedInv;
            var pos = Vector3D.Transform(pt.ContactPosition, invWorld);
            var normal = Vector3.TransformNormal(pt.ContactPoint.Normal, invWorld) * pt.ContactPointDirection;

            int destroyed = ApplyDeformation(deformationOffset, softAreaPlanar, softAreaVertical, pos, normal, MyDamageType.Deformation, 0, m_grid.GridSizeEnum == MyCubeSize.Large ? 0.6f : 0.16f);

            MyPhysics.CastRay(pt.ContactPoint.Position, pt.ContactPoint.Position - softAreaVertical * Vector3.Normalize(pt.ContactPoint.Normal), m_hitList);
            foreach (var hit in m_hitList)
            {
                var entity = hit.HkHitInfo.GetHitEntity();
                if (entity != m_grid.Components && entity is MyCubeGrid)
                {
                    var grid = entity as MyCubeGrid;
                    invWorld = grid.PositionComp.WorldMatrixNormalizedInv;
                    pos = Vector3D.Transform(pt.ContactPosition, invWorld);
                    normal = Vector3.TransformNormal(pt.ContactPoint.Normal, invWorld) * pt.ContactPointDirection;
                    grid.Physics.ApplyDeformation(deformationOffset,
                        softAreaPlanar * (m_grid.GridSizeEnum == grid.GridSizeEnum ? 1 : grid.GridSizeEnum == MyCubeSize.Large ? 2 : 0.25f),
                        softAreaVertical * (m_grid.GridSizeEnum == grid.GridSizeEnum ? 1 : grid.GridSizeEnum == MyCubeSize.Large ? 2.5f : 0.2f),
                        pos, normal, MyDamageType.Deformation, 0, grid.GridSizeEnum == MyCubeSize.Large ? 0.6f : 0.16f);
                }
            }
            m_hitList.Clear();

            float explosionRadius = Math.Max(m_grid.GridSize, deformationOffset * (m_grid.GridSizeEnum == MyCubeSize.Large ? 0.25f : 0.05f));
            if (explosionRadius > 0 && deformationOffset > m_grid.GridSize / 2 && destroyed != 0)
            {
                var info = new ExplosionInfo()
                {
                    Position = pt.ContactPosition,
                    ExplosionType = MyExplosionTypeEnum.GRID_DESTRUCTION,
                    Radius = explosionRadius,
                    ShowParticles = true,
                    GenerateDebris = true,
                };

                m_explosions.Add(info);
            }
            else
            {
                AddCollisionEffect(pt.ContactPosition, normal);
            }

            ProfilerShort.End();
        }

        private bool PerformDeformation(ref HkBreakOffPointInfo pt, bool fromBreakParts, float separatingVelocity, MyEntity otherEntity)
        {
            ProfilerShort.Begin("PerformDeformation");

            Debug.Assert(Sync.IsServer, "Function PerformDeformation should not be called from client");

            var mass = this.Mass;
            if (IsStatic)
            {
                mass = pt.CollidingBody.Mass;
            }

            float velocity = separatingVelocity;
            float deltaV = pt.BreakingImpulse / mass;

            // Calculate deformation offset
            float deformationOffset = 0.3f + Math.Max(0, (Math.Abs(velocity) - deltaV) / 15);
            deformationOffset *= 3;
            deformationOffset *= MyFakes.DEFORMATION_RATIO;

            const float maxDeformationHardLimit = 20;
            // Happens during copy pasting
            //Debug.Assert(deformationOffset < maxDeformationHardLimit, "Deformation is bigger than maximum.");
            deformationOffset = Math.Min(deformationOffset, maxDeformationHardLimit);

            float explosionRadius = Math.Max(m_grid.GridSize, deformationOffset);
            explosionRadius = Math.Min(explosionRadius, 10);
            bool hitVoxel = otherEntity is MyVoxelBase;
            if (hitVoxel)
            {
                // Hardness from 0 to 1
                float hardness = 0.0f;
                deformationOffset *= 1 + hardness;
                explosionRadius *= 1 - hardness;
            }

            float softAreaPlanar = m_grid.GridSizeEnum == MyCubeSize.Large ? 4 : 1.2f; // About 4 meters for large grid and 1.2m for small
            float softAreaVertical = 1.5f * deformationOffset;

            var invWorld = m_grid.PositionComp.WorldMatrixNormalizedInv;
            var pos = Vector3D.Transform(pt.ContactPosition, invWorld);
            var velAtPoint = GetVelocityAtPoint(pt.ContactPosition);
            if (!velAtPoint.IsValid() || velAtPoint == Vector3.Zero)
                velAtPoint = pt.ContactPoint.Normal;
            var len = velAtPoint.Normalize();
            Vector3 normal;
            if (len > 5)
                normal = Vector3D.TransformNormal(velAtPoint, invWorld) * pt.ContactPointDirection;
            else
                normal = pt.ContactPointDirection * pt.ContactPoint.Normal;
            int destroyed = ApplyDeformation(deformationOffset, softAreaPlanar, softAreaVertical, pos, normal, MyDamageType.Deformation, attackerId: otherEntity != null ? otherEntity.EntityId : 0);
            if (destroyed != 0)
            {
                if (len > 1)
                    len = len / 10;
                RigidBody.ApplyPointImpulse(-velAtPoint *0.3f* Math.Min(destroyed * len * (Mass / 5), Mass * 5), pt.ContactPoint.Position);
                RigidBody.Gravity = Vector3.Zero;
                DisableGravity = 1;
            }
            if (explosionRadius > 0 && deformationOffset > m_grid.GridSize / 2 && destroyed != 0)
            {
                var info = new ExplosionInfo()
                {
                    Position = pt.ContactPosition + pt.ContactPointDirection * pt.ContactPoint.Normal * explosionRadius * 0.5f,
                    //ExplosionType = destroyed ? MyExplosionTypeEnum.GRID_DESTRUCTION : MyExplosionTypeEnum.GRID_DEFORMATION,
                    ExplosionType = MyExplosionTypeEnum.GRID_DESTRUCTION,
                    Radius = explosionRadius,
                    ShowParticles = (otherEntity is MyVoxelPhysics) == false && (otherEntity is MyTrees) == false,
                    GenerateDebris = true
                };
                m_explosions.Add(info);
            }
            else
            {
                //cannot be here since its onlyexecuted on server
                //AddCollisionEffect(pt.ContactPoint.Position, normal);
            }
            ProfilerShort.End();
            return destroyed != 0;
        }

        //public List<object> m_debugBones = new List<object>();

        /// <summary>
        /// Applies deformation, returns true when block was destroyed (explosion should be generated)
        /// </summary>
        /// <param name="deformationOffset">Amount of deformation in the localPos</param>
        /// <param name="offsetThreshold">When deformation offset for bone is lower then threshold, it won't move the bone at all or do damage</param>
        public int ApplyDeformation(float deformationOffset, float softAreaPlanar, float softAreaVertical, Vector3 localPos, Vector3 localNormal, MyStringHash damageType, float offsetThreshold = 0, float lowerRatioLimit = 0, long attackerId = 0)
        {
            int blocksDeformed = 0;
            offsetThreshold /= m_grid.GridSizeEnum == MyCubeSize.Large ? 1 : 5;
            float roundSize = m_grid.GridSize / m_grid.Skeleton.BoneDensity;
            Vector3I roundedPos = Vector3I.Round((localPos + new Vector3(m_grid.GridSize / 2)) / roundSize);
            Vector3I gridPos = Vector3I.Round((localPos + new Vector3(m_grid.GridSize / 2)) / m_grid.GridSize);
            Vector3I gridOffset = roundedPos - gridPos * m_grid.Skeleton.BoneDensity;

            float breakOffset = m_grid.GridSize * 0.7f;
            float breakOffsetDestruction = breakOffset;

            Vector3I min = Vector3I.MaxValue;
            Vector3I max = Vector3I.MinValue;
            bool isDirty = false;

            bool destructionDone = false;

            Vector3 absNormal = Vector3.Abs(localNormal);
            float maxNorm = Math.Max(Math.Max(absNormal.X, absNormal.Y), absNormal.Z);
            float maxDef = maxNorm * deformationOffset;

            float destructionPotencial = (1 - breakOffsetDestruction / maxDef);
            float minDeformationRatio = 1;

            var softAreaPlanarR = 1.0f / softAreaPlanar;
            var softAreaVerticalR = 1.0f / softAreaVertical;
            var gridSizeR = 1.0f / m_grid.GridSize;
            Vector3D forward = localNormal;
            var up = MyUtils.GetRandomPerpendicularVector(ref forward);
            float minLength = Math.Min(m_grid.GridSize / 256.0f, deformationOffset * 0.06f);

            // When we're sure that there will be destroyed blocks, it's not necessary to do deformations, just do destruction
            MyDamageInformation damageInfo = new MyDamageInformation(true, 1f, MyDamageType.Deformation, attackerId);
            if (destructionPotencial > 0)
            {
                float critVertical = destructionPotencial * softAreaVertical;
                float critPlanar = destructionPotencial * softAreaPlanar;

                var he = new Vector3(critPlanar, critPlanar, critVertical);
                MyOrientedBoundingBox obb = new MyOrientedBoundingBox(gridPos,he, Quaternion.CreateFromForwardUp(forward, up));
                var aabb = obb.GetAABB();

                //float maxCritDist = Math.Max(critPlanar, critVertical);
                //Vector3I distCubes = new Vector3I((int)Math.Ceiling(maxCritDist / m_grid.GridSize));
                //Vector3I minOffset = gridPos - distCubes;
                //Vector3I maxOffset = gridPos + distCubes;
                var minOffset = Vector3I.Floor(aabb.Min);
                var maxOffset = Vector3I.Ceiling(aabb.Max);
                minOffset = Vector3I.Max(minOffset, m_grid.Min);
                maxOffset = Vector3I.Min(maxOffset, m_grid.Max);

                ProfilerShort.Begin("Update destruction");

                Vector3I offset;
                for (offset.X = minOffset.X; offset.X <= maxOffset.X; offset.X++)
                {
                    for (offset.Y = minOffset.Y; offset.Y <= maxOffset.Y; offset.Y++)
                    {
                        for (offset.Z = minOffset.Z; offset.Z <= maxOffset.Z; offset.Z++)
                        {
                            Vector3 closestCorner = m_grid.GetClosestCorner(offset, localPos);
                            
                            float soften = 1.0f;
                            if (offset != gridPos)
                            {
                                soften = CalculateSoften(softAreaPlanarR, softAreaVerticalR, ref localNormal, closestCorner - localPos);
                            }
                            float deformation = maxDef * soften;

                            if (deformation > breakOffsetDestruction)
                            {
                                var block = m_grid.GetCubeBlock(offset);
                                if (block != null)
                                {
                                    if (block.UseDamageSystem)
                                    {
                                        damageInfo.Amount = 1;
                                        MyDamageSystem.Static.RaiseBeforeDamageApplied(block, ref damageInfo);
                                        if (damageInfo.Amount == 0f)
                                            continue;
                                    }

                                    minDeformationRatio = Math.Min(minDeformationRatio, block.DeformationRatio);

                                    if (Math.Max(lowerRatioLimit, block.DeformationRatio) * deformation > breakOffsetDestruction)
                                    {
                                        ProfilerShort.Begin("Remove destroyed blocks");
                                        min = Vector3I.Min(min, block.Min - Vector3I.One);
                                        max = Vector3I.Max(max, block.Max + Vector3I.One);
                                        isDirty = true;
                                        m_grid.RemoveDestroyedBlock(block);
                                        ProfilerShort.End();
                                        destructionDone = true;
                                    }
                                    blocksDeformed++;
                                }
                            }
                        }
                    }
                }
                ProfilerShort.End();

                // When there was no destruction, reduce area and do deformation
                minDeformationRatio = Math.Max(minDeformationRatio, 0.2f);
                softAreaPlanar *= minDeformationRatio;
                softAreaVertical *= minDeformationRatio;
            }

            if (!destructionDone)
            {
                //m_debugBones.Clear();
                var boneDensity = m_grid.Skeleton.BoneDensity;
                ProfilerShort.Begin("Update deformation");
                MyOrientedBoundingBox obb = new MyOrientedBoundingBox(
                    gridPos * boneDensity + gridOffset,
                    new Vector3(softAreaPlanar * gridSizeR * boneDensity, softAreaPlanar * gridSizeR * boneDensity , softAreaVertical * gridSizeR * boneDensity),
                    Quaternion.CreateFromForwardUp(forward, up));
                var aabb = obb.GetAABB();

                //float softArea = Math.Max(softAreaPlanar, softAreaVertical);
                //var distBones = new Vector3I((int)Math.Ceiling(softArea / m_grid.GridSize * m_grid.Skeleton.BoneDensity));

                //Vector3I minOffset = gridPos * m_grid.Skeleton.BoneDensity + gridOffset - distBones;
                //Vector3I maxOffset = gridPos * m_grid.Skeleton.BoneDensity + gridOffset + distBones;
                var minOffset = Vector3I.Floor(aabb.Min);
                var maxOffset = Vector3I.Ceiling(aabb.Max);
                minOffset = Vector3I.Max(minOffset, m_grid.Min * m_grid.Skeleton.BoneDensity);
                maxOffset = Vector3I.Min(maxOffset, m_grid.Max * m_grid.Skeleton.BoneDensity);

                Vector3I minDirtyBone = Vector3I.MaxValue;
                Vector3I maxDirtyBone = Vector3I.MinValue;

                ProfilerShort.Begin("Get bones");
                Debug.Assert(m_tmpBoneList.Count == 0, "Temporary dictionary not cleared properly");
                m_grid.GetExistingBones(minOffset, maxOffset, m_tmpBoneList, damageInfo);
                ProfilerShort.End();

                ProfilerShort.Begin("Deform bones");
                Vector3 bone;
                Vector3I baseOffset = gridPos * m_grid.Skeleton.BoneDensity;
                float boneDensityR = 1.0f / m_grid.Skeleton.BoneDensity;
                var halfGridSize = new Vector3(m_grid.GridSize * 0.5f);

                ProfilerShort.CustomValue("Bone Count", m_tmpBoneList.Count, 0);
                foreach (var b in m_tmpBoneList)
                {
                    var boneIndex = b.Key;

                    var baseBonePos = boneIndex * m_grid.GridSize * boneDensityR - halfGridSize;

                    m_grid.Skeleton.GetBone(ref boneIndex, out bone);
                    var bonePos = bone + baseBonePos;

                    float soften = CalculateSoften(softAreaPlanarR, softAreaVerticalR, ref localNormal, bonePos - localPos);

                    if (soften == 0)
                        continue;

                    min = Vector3I.Min(min, Vector3I.Floor(bonePos * gridSizeR - Vector3.One * boneDensityR));
                    max = Vector3I.Max(max, Vector3I.Ceiling(bonePos *gridSizeR + Vector3.One * boneDensityR));
                    isDirty = true;

                    float deformationRatio = 1.0f;
                    bool doDeformation = true;
                    var block2 = b.Value;
                    {
                        Debug.Assert(block2 != null, "Block cannot be null");
                        deformationRatio = Math.Max(lowerRatioLimit, block2.DeformationRatio); // + some deformation coeficient based on integrity

                        float maxAxisDeformation = maxNorm * deformationOffset * soften;
                        doDeformation = block2.UsesDeformation;

                        if (block2.IsDestroyed) // ||  block2.DoDamage(maxAxisDeformation / m_grid.GridSize, damageType, addDirtyParts: false))
                        {
                            destructionDone = true;
                        }
                    }

                    if (deformationOffset * deformationRatio < offsetThreshold)
                        continue;

                    float deformationLength = deformationOffset * soften * deformationRatio;
                    var deformation = localNormal * deformationLength;

                    bool canDeform = damageType != MyDamageType.Bullet || (Math.Abs(bone.X + deformation.X) < breakOffset && Math.Abs(bone.Y + deformation.Y) < breakOffset && Math.Abs(bone.Z + deformation.Z) < breakOffset);

                    if (canDeform && deformationLength > minLength)
                    {
                        bone += deformation;

                        //m_debugBones.Add(new Tuple<Vector3, float>(bonePos, deformationRatio));
                        var offset = boneIndex - baseOffset;

                        if (Math.Abs(bone.X) > breakOffset || Math.Abs(bone.Y) > breakOffset || Math.Abs(bone.Z) > breakOffset)
                        {
                            m_tmpCubeList.Clear();

                            Vector3I wrappedBoneOffset = offset;
                            Vector3I wrappedGridPos = gridPos;
                            m_grid.Skeleton.Wrap(ref wrappedGridPos, ref wrappedBoneOffset);
                            m_grid.Skeleton.GetAffectedCubes(wrappedGridPos, wrappedBoneOffset, m_tmpCubeList, m_grid);

                            foreach (var c in m_tmpCubeList)
                            {
                                var block = m_grid.GetCubeBlock(c);
                                if (block != null)
                                {
                                    ProfilerShort.Begin("Remove destroyed blocks");
                                    m_grid.RemoveDestroyedBlock(block);
                                    AddDirtyBlock(block);
                                    ProfilerShort.End();
                                    destructionDone = true;
                                    blocksDeformed++;
                                }
                            }
                        }
                        else if (doDeformation && Sync.IsServer)
                        {
                            minDirtyBone = Vector3I.Min(minDirtyBone, boneIndex);
                            maxDirtyBone = Vector3I.Max(maxDirtyBone, boneIndex);

                            m_grid.Skeleton.SetBone(ref boneIndex, ref bone);
                            m_grid.AddDirtyBone(gridPos, offset);

                            m_grid.BonesToSend.AddInput(boneIndex);
                        }
                    }
                }
                m_tmpBoneList.Clear();
                ProfilerShort.End();
                ProfilerShort.End();
            }

            if (isDirty)
            {
                m_dirtyCubesInfo.DirtyParts.Add(new BoundingBoxI() { Min = min, Max = max });
            }
            return blocksDeformed;
        }

        enum GridEffectType
        {
            Collision,
            Destruction,
            Dust
        }

        struct GridEffect
        {
            public GridEffectType Type;
            public Vector3D position;
            public Vector3 directionScale;
        }

        private MyConcurrentQueue<GridEffect> m_gridEffects = new MyConcurrentQueue<GridEffect>();

        void AddCollisionEffect(Vector3D position, Vector3 direction)
        {
            if (!MyFakes.ENABLE_COLLISION_EFFECTS)
                return;

            if (m_gridEffects.Count >= MaxEffectsPerFrame)
                return;

            m_gridEffects.Enqueue(new GridEffect() { Type = GridEffectType.Collision, position = position, directionScale = direction });
            //VRageRender.MyRenderProxy.DebugDrawSphere(effect.WorldMatrix.Translation, 0.2f, Color.Red.ToVector3(), 1.0f, false);
        }

        void AddDustEffect(Vector3D position, float scale)
        {
            if (m_gridEffects.Count >= MaxEffectsPerFrame)
                return;

            m_gridEffects.Enqueue(new GridEffect() { Type = GridEffectType.Dust, position = position, directionScale = Vector3.One * scale });
        }

        void AddDestructionEffect(Vector3D position, Vector3 direction)
        {
            if (!MyFakes.ENABLE_DESTRUCTION_EFFECTS)
                return;

            if (m_gridEffects.Count >= MaxEffectsPerFrame)
                return;

            m_gridEffects.Enqueue(new GridEffect() { Type = GridEffectType.Destruction, position = position, directionScale = direction });
        }

        void CreateEffect(GridEffect e)
        {
            var position = e.position;
            var direction = e.directionScale;
            float scale = e.directionScale.X;
            MyParticleEffect effect;
            switch (e.Type)
            {
                case GridEffectType.Collision:
                    float cameraDistSq = (float)Vector3D.DistanceSquared(MySector.MainCamera.Position, position);
                    MyParticleEffectsIDEnum effectId;
                    scale = MyPerGameSettings.CollisionParticle.Scale;
                    if (m_grid.GridSizeEnum == MyCubeSize.Large)
                    {
                        effectId = cameraDistSq > MyPerGameSettings.CollisionParticle.CloseDistanceSq ? MyPerGameSettings.CollisionParticle.LargeGridDistant : MyPerGameSettings.CollisionParticle.LargeGridClose;
                    }
                    else
                    {
                        effectId = MyPerGameSettings.CollisionParticle.SmallGridClose;
                        scale = 0.05f;
                    }

                    MatrixD dirMatrix = MatrixD.CreateFromDir(direction);
                    if (MyParticlesManager.TryCreateParticleEffect((int)effectId, out effect))
                    {
                        effect.UserScale = scale;
                        effect.WorldMatrix = MatrixD.CreateWorld(position, dirMatrix.Forward, dirMatrix.Up);
                    }
                    break;

                case GridEffectType.Destruction:
                    scale = MyPerGameSettings.DestructionParticle.Scale;
                    if (m_grid.GridSizeEnum != MyCubeSize.Large)
                        scale = 0.05f;
                    MySyncDestructions.AddDestructionEffect(MyPerGameSettings.DestructionParticle.DestructionSmokeLarge, position, direction, scale);
                    break;

                case GridEffectType.Dust:
                    if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Collision_Meteor, out effect))
                    {
                        effect.SetPreload(1f);
                        effect.UserScale = scale;
                        effect.WorldMatrix = MatrixD.CreateFromTransformScale(Quaternion.Identity, position, Vector3D.One);
                    }
                    break;
            }
        }

        public static void CreateDestructionEffect(MyParticleEffectsIDEnum effectId, Vector3D position, Vector3 direction, float scale)
        {
            MatrixD dirMatrix = MatrixD.CreateFromDir(direction);
            MyParticleEffect effect;
            if (MyParticlesManager.TryCreateParticleEffect((int)effectId, out effect))
            {
                effect.UserScale = scale;
                effect.WorldMatrix = MatrixD.CreateWorld(position, dirMatrix.Forward, dirMatrix.Up);
            }
        }

        public override MyStringHash GetMaterialAt(Vector3D worldPos)
        {
            var pos = Vector3.Transform(worldPos, m_grid.PositionComp.WorldMatrixNormalizedInv) / m_grid.GridSize;
            Vector3I cubePos;
            m_grid.FixTargetCube(out cubePos, pos);
            var cube = m_grid.GetCubeBlock(cubePos);
            if (cube == null)
                return base.GetMaterialAt(worldPos);
            if (cube.FatBlock is MyCompoundCubeBlock)
                cube = (cube.FatBlock as MyCompoundCubeBlock).GetBlocks()[0];
            var blockMaterial = cube.BlockDefinition.PhysicalMaterial.Id.SubtypeId;
            return blockMaterial != MyStringHash.NullOrEmpty ? blockMaterial : base.GetMaterialAt(worldPos);
        }

        /// <summary>
        /// For large continuous areas use AddDirtyArea, internal processing is much faster because taking advantage of aabb tests
        /// </summary>
        public void AddDirtyBlock(MySlimBlock block)
        {
            Vector3I pos;
            for (pos.X = block.Min.X; pos.X <= block.Max.X; pos.X++)
            {
                for (pos.Y = block.Min.Y; pos.Y <= block.Max.Y; pos.Y++)
                {
                    for (pos.Z = block.Min.Z; pos.Z <= block.Max.Z; pos.Z++)
                    {
                        m_dirtyCubesInfo.DirtyBlocks.Add(pos);
                    }
                }
            }
            m_dirtyCubesInfo.DirtyParts.Add(new BoundingBoxI() { Min = block.Min, Max = block.Max });
        }

        public void AddDirtyArea(Vector3I min, Vector3I max)
        {
            Vector3I pos;

            for (pos.X = min.X; pos.X <= max.X; pos.X++)
            {
                for (pos.Y = min.Y; pos.Y <= max.Y; pos.Y++)
                {
                    for (pos.Z = min.Z; pos.Z <= max.Z; pos.Z++)
                    {
                        m_dirtyCubesInfo.DirtyBlocks.Add(pos);
                    }
                }
            }
            m_dirtyCubesInfo.DirtyParts.Add(new BoundingBoxI() { Min = min, Max = max });
        }

        private int m_debrisPerFrame = 0;
        private const int MaxDebrisPerFrame = 3;
        public void UpdateShape()
        {
            RemovePenetratingShapes();

            m_debrisPerFrame = 0;
            if(RigidBody != null)
                RigidBody.ResetLimit();

            ProfilerShort.Begin("MyGridPhysics.UpdateShape");

            bool physicsChanged = false;

            while(m_gridEffects.Count > 0)
            {
                CreateEffect(m_gridEffects.Dequeue());
            }

            m_effectsPerFrame = 0;

            if (m_lastContacts.Count > 0)
            {
                m_tmpContactId.Clear();
                foreach (var contact in m_lastContacts)
                {
                    if (MySandboxGame.TotalGamePlayTimeInMilliseconds > contact.Value + SparksEffectDelayPerContactMs)
                    {
                        m_tmpContactId.Add(contact.Key);
                    }
                }

                foreach (var id in m_tmpContactId)
                {
                    m_lastContacts.Remove(id);
                }
            }

            if (m_explosions.Count > 0)
            {
                float speed = m_grid.Physics.LinearVelocity.Length();
                foreach (var ex in m_explosions)
                {
                    if (m_grid.AddExplosion(ex.Position, ex.ExplosionType, ex.Radius,ex.ShowParticles,ex.GenerateDebris)/* && ex.ModelDebris*/)
                    {
                        // Create model debris
                        if (speed > 0 && ex.GenerateDebris && m_debrisPerFrame < MaxDebrisPerFrame)
                        {
                            MyDebris.Static.CreateDirectedDebris(ex.Position, m_grid.Physics.LinearVelocity / speed, m_grid.GridSize, m_grid.GridSize * 1.5f, 0, MathHelper.PiOver2, 6, m_grid.GridSize * 1.5f, speed);
                            m_debrisPerFrame++;
                        }
                    }
                }
                m_explosions.Clear();
            }

            if (!m_grid.CanHavePhysics())
            {
                ProfilerShort.End();
                return;
            }

            BoundingBox dirtyBox = BoundingBox.CreateInvalid();
            foreach (var part in m_dirtyCubesInfo.DirtyParts)
            {
                Vector3I pos;
                for (pos.X = part.Min.X; pos.X <= part.Max.X; pos.X++)
                {
                    for (pos.Y = part.Min.Y; pos.Y <= part.Max.Y; pos.Y++)
                    {
                        for (pos.Z = part.Min.Z; pos.Z <= part.Max.Z; pos.Z++)
                        {
                            m_dirtyCubesInfo.DirtyBlocks.Add(pos);
                            dirtyBox = dirtyBox.Include(pos * m_grid.GridSize);
                        }
                    }
                }
            }
            if (!m_recreateBody)
            {
                if (m_dirtyCubesInfo.DirtyBlocks.Count > 0)
                {
                    m_shape.RefreshBlocks(WeldedRigidBody != null ? WeldedRigidBody : RigidBody, WeldedRigidBody != null ? null : RigidBody2, m_dirtyCubesInfo, BreakableBody);
                    if (RigidBody.IsActive && !HavokWorld.ActiveRigidBodies.Contains(RigidBody))
                    {
                        HavokWorld.ActiveRigidBodies.Add(RigidBody);
                    }
                    physicsChanged = true;
                }

                if (m_dirtyCubesInfo.DirtyParts.Count > 0)
                {
                    dirtyBox.Inflate(0.5f + m_grid.GridSize);
                    BoundingBoxD bd = dirtyBox.Transform(m_grid.WorldMatrix);
                    MyPhysics.ActivateInBox(ref bd);
                }
            }

            if (m_recreateBody)
            {
                RecreateBreakableBody();
                m_recreateBody = false;
                physicsChanged = true;
            }
            m_dirtyCubesInfo.Clear();

            if (physicsChanged)
            {
                m_grid.RaisePhysicsChanged();
            }
            ProfilerShort.End();
        }

        private List<HkShapeCollision> m_shapePenetrations = new List<HkShapeCollision>();
        private MyPhysicsBody m_testPenetration;
        private HashSet<HkShapeCollision> m_processedCollision = new HashSet<HkShapeCollision>();
        private void RemovePenetratingShapes()
        {
            if (m_testPenetration != null)
            {
                var wm = RigidBody.GetRigidBodyMatrix();
                var t1 = wm.Translation - RigidBody.LinearVelocity * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 0.5f;
                var q1 = Quaternion.CreateFromRotationMatrix(wm.GetOrientation());
                var otherWm = m_testPenetration.RigidBody.GetRigidBodyMatrix();
                var t2 = otherWm.Translation;
                var q2 = Quaternion.CreateFromRotationMatrix(otherWm.GetOrientation());
                ProfilerShort.Begin("GetPenetrations");
                HavokWorld.GetPenetrationsShapeShape(m_testPenetration.RigidBody.GetShape(), ref t2, ref q2,
                    Shape, ref t1, ref q1,
                    m_shapePenetrations);
                ProfilerShort.BeginNextBlock("ApplyDamage");
                int removed = 0;
                foreach (var pair in m_shapePenetrations)
                {
                    if (!m_processedCollision.Add(pair))
                        continue;
                    Vector3I? min = null;
                    var shape = RigidBody.GetShape();
                    for (int i = 0; i < pair.ShapeKeyCount; i++)
                    {
                        if (shape.ShapeType == HkShapeType.BvTree)
                        {
                            Vector3I min2;
                            ((HkGridShape)shape).GetShapeMin(pair.GetShapeKey(i), out min2);
                            min = min2;
                            break;
                        }
                        if (shape.IsContainer())
                        {
                            var shape2 = shape.GetContainer().GetShape(pair.GetShapeKey(i));
                            if (shape2.IsValid)
                                shape = shape2;
                            else
                                break;
                        }
                    }
                    if (!min.HasValue)
                        continue;
                    var block = m_grid.GetCubeBlock(min.Value);
                    if (block != null)
                    {
                        m_grid.ApplyDestructionDeformation(block);
                        removed++;
                    }

                }
                ProfilerShort.End(m_shapePenetrations.Count);
                ProfilerShort.CustomValue("PenetrationsCount", m_shapePenetrations.Count, 0);
                m_shapePenetrations.Clear();
                m_processedCollision.Clear();
                m_testPenetration = null;
            }
        }

        public void UpdateMass()
        {
            if (RigidBody.GetMotionType() != HkMotionType.Keyframed)
            {
                float currentMass = RigidBody.Mass;
                m_shape.RefreshMass();
                float newMass = RigidBody.Mass;
                //Debug.Assert(currentMass != newMass, "Mass was not modified!");
                if (newMass != currentMass && !RigidBody.IsActive)
                    RigidBody.Activate();
                m_grid.RaisePhysicsChanged();
            }
        }

        public void AddBlock(MySlimBlock block)
        {
            Vector3I pos;
            for (pos.X = block.Min.X; pos.X <= block.Max.X; pos.X++)
            {
                for (pos.Y = block.Min.Y; pos.Y <= block.Max.Y; pos.Y++)
                {
                    for (pos.Z = block.Min.Z; pos.Z <= block.Max.Z; pos.Z++)
                    {
                        m_dirtyCubesInfo.DirtyBlocks.Add(pos);
                    }
                }
            }
        }

        protected override void CreateBody(ref HkShape shape, HkMassProperties? massProperties)
        {
            if (MyPerGameSettings.Destruction)// && shape.ShapeType == HkShapeType.StaticCompound)
            {
                shape = CreateBreakableBody(shape, massProperties);
            }
            else
            {
                HkRigidBodyCinfo rbInfo = new HkRigidBodyCinfo();

                rbInfo.AngularDamping = m_angularDamping;
                rbInfo.LinearDamping = m_linearDamping;
                rbInfo.Shape = shape;
                rbInfo.SolverDeactivation = InitialSolverDeactivation;
                rbInfo.ContactPointCallbackDelay = ContactPointDelay;

                if (massProperties.HasValue)
                {
                    rbInfo.SetMassProperties(massProperties.Value);
                }

                GetInfoFromFlags(rbInfo, Flags);
                if (m_grid.IsStatic)
                {
                    rbInfo.MotionType = HkMotionType.Dynamic;
                    rbInfo.QualityType = HkCollidableQualityType.Moving;
                }
                RigidBody = new HkRigidBody(rbInfo);

                if (m_grid.IsStatic)
                {
                    RigidBody.UpdateMotionType(HkMotionType.Fixed);
                }
                //RigidBody.UpdateMotionType(HkMotionType.Dynamic);

                //base.CreateBody(ref shape, massProperties);
            }
        }

        private static void DisconnectBlock(Sandbox.Game.Entities.Cube.MySlimBlock a)
        {
            a.DisconnectFaces.Add(Vector3I.Left);
            a.DisconnectFaces.Add(Vector3I.Right);
            a.DisconnectFaces.Add(Vector3I.Forward);
            a.DisconnectFaces.Add(Vector3I.Backward);
            a.DisconnectFaces.Add(Vector3I.Up);
            a.DisconnectFaces.Add(Vector3I.Down);
        }

        private void AddFaces(Game.Entities.Cube.MySlimBlock a, Vector3I ab)
        {
            if (!a.DisconnectFaces.Contains(ab * Vector3I.UnitX))
                a.DisconnectFaces.Add(ab * Vector3I.UnitX);
            if (!a.DisconnectFaces.Contains(ab * Vector3I.UnitY))
                a.DisconnectFaces.Add(ab * Vector3I.UnitY);
            if (!a.DisconnectFaces.Contains(ab * Vector3I.UnitZ))
                a.DisconnectFaces.Add(ab * Vector3I.UnitZ);
        }

        public override void DebugDraw()
        {
            if (MyDebugDrawSettings.BREAKABLE_SHAPE_CONNECTIONS && BreakableBody != null)
            {
                MySlimBlock bl = null;
                var hitlist = new List<MyPhysics.HitInfo>();
                MyPhysics.CastRay(MySector.MainCamera.Position, MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 25, hitlist, MyPhysics.CollisionLayers.CollisionLayerWithoutCharacter);
                foreach (var h in hitlist)
                {
                    if (!(h.HkHitInfo.GetHitEntity() is MyCubeGrid))
                        continue;
                    var g = h.HkHitInfo.GetHitEntity() as MyCubeGrid;
                    bl = g.GetCubeBlock(g.WorldToGridInteger(h.Position + MySector.MainCamera.ForwardVector * 0.2f));
                    break;
                }
                int i = 0;
                var lst = new List<HkdConnection>();
                BreakableBody.BreakableShape.GetConnectionList(lst);
                foreach (var c in lst)
                {
                    //i++;
                    var a = ClusterToWorld(Vector3.Transform(c.PivotA, RigidBody.GetRigidBodyMatrix()));
                    var b = ClusterToWorld(Vector3.Transform(c.PivotB, RigidBody.GetRigidBodyMatrix()));
                    if (bl != null && bl.CubeGrid.WorldToGridInteger(a) == bl.Position)
                    {
                        a = a + (b - a) * 0.05f;
                        MyRenderProxy.DebugDrawLine3D(a, b, Color.Red, Color.Blue, false);
                        MyRenderProxy.DebugDrawSphere(b, 0.075f, Color.White, 1, false);
                    }
                    if (bl != null && bl.CubeGrid.WorldToGridInteger(b) == bl.Position)
                    {
                        a += Vector3.One * 0.02f;
                        b += Vector3.One * 0.02f;
                        MyRenderProxy.DebugDrawLine3D(a, b, Color.Red, Color.Green, false);
                        MyRenderProxy.DebugDrawSphere(b, 0.025f, Color.Green, 1, false);
                    }
                    if (i > 1000)
                        break;
                }
            }

            Shape.DebugDraw();
            base.DebugDraw();
        }

        protected override void OnWelded(MyPhysicsBody other)
        {
            base.OnWelded(other);
            Shape.RefreshMass();

            if (m_grid.BlocksDestructionEnabled)
            {
                if (HavokWorld != null)
                    HavokWorld.BreakOffPartsUtil.MarkEntityBreakable(RigidBody, Shape.BreakImpulse);
                if (Sync.IsServer)
                {
                    if (RigidBody.BreakLogicHandler == null)
                        RigidBody.BreakLogicHandler = BreakLogicHandler;
                    if (RigidBody.BreakPartsHandler == null)
                        RigidBody.BreakPartsHandler = BreakPartsHandler;
                }
            }
            m_grid.HavokSystemIDChanged(other.HavokCollisionSystemID);
        }

        protected override void OnUnwelded(MyPhysicsBody other)
        {
            base.OnUnwelded(other);
            Shape.RefreshMass();
            m_grid.HavokSystemIDChanged(HavokCollisionSystemID);
            if(m_grid.IsStatic == false)
            {
                m_grid.RecalculateGravity();
            }
        }

        /// <summary>
        /// Checks if the last block in grid has fracture component and converts it into fracture pieces.
        /// </summary>

        public void ConvertToDynamic(bool doubledKinematic)
        {
            RigidBody.UpdateMotionType(HkMotionType.Dynamic);
            RigidBody.Quality = HkCollidableQualityType.Moving;
            if (WeldedRigidBody != null && WeldedRigidBody.Quality == HkCollidableQualityType.Fixed)
            {
                WeldedRigidBody.UpdateMotionType(HkMotionType.Dynamic);
                WeldedRigidBody.Quality = HkCollidableQualityType.Moving;
            }

            if (doubledKinematic && !MyPerGameSettings.Destruction)
            {
                Flags = RigidBodyFlag.RBF_DOUBLED_KINEMATIC;
                RigidBody.Layer = MyPhysics.CollisionLayers.DynamicDoubledCollisionLayer;
                SetupRigidBody2();
            }
            else
            {
                Flags = RigidBodyFlag.RBF_DEFAULT;   
                RigidBody.Layer = MyPhysics.CollisionLayers.DefaultCollisionLayer;
            }
            UpdateMass();
            ActivateCollision();
            HavokWorld.RefreshCollisionFilterOnEntity(RigidBody);
            RigidBody.Activate();
            if (RigidBody.InWorld)
                HavokWorld.RigidBodyActivated(RigidBody);
        }

        private void SetupRigidBody2()
        {
            RigidBody2 = HkRigidBody.Clone(RigidBody);
            RigidBody2.Layer = MyPhysics.CollisionLayers.KinematicDoubledCollisionLayer;
            RigidBody2.UpdateMotionType(HkMotionType.Keyframed);
            if (RigidBody.InWorld)
                HavokWorld.AddRigidBody(RigidBody2);
        }

        public void ConvertToStatic()
        {
            RigidBody.UpdateMotionType(HkMotionType.Fixed);
            RigidBody.Quality = HkCollidableQualityType.Fixed;
            if (WeldedRigidBody != null && WeldedRigidBody.Quality != HkCollidableQualityType.Fixed)
            {
                WeldedRigidBody.UpdateMotionType(HkMotionType.Fixed);
                WeldedRigidBody.Quality = HkCollidableQualityType.Fixed;
            }

            RigidBody.Layer = MyPhysics.CollisionLayers.StaticCollisionLayer;

            UpdateMass();
            ActivateCollision();
            HavokWorld.RefreshCollisionFilterOnEntity(RigidBody);
            RigidBody.Activate();
            if (RigidBody.InWorld)
                HavokWorld.RigidBodyActivated(RigidBody);

            if(RigidBody2  != null)
            {
                if(RigidBody2.InWorld)
                {
                    HavokWorld.RemoveRigidBody(RigidBody2);
                }
                RigidBody2.Dispose();       
            }

            RigidBody2 = null;

        }

    }
}
