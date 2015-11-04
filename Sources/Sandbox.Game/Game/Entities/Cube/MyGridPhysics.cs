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
using Sandbox.Graphics.TransparentGeometry.Particles;
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
using VRage.Components;
using VRage.ModAPI;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Game.Components;
using Sandbox.Engine.Voxels;
using Sandbox.Game.EntityComponents;

namespace Sandbox.Game.Entities.Cube
{
    public class MyGridPhysics : MyPhysicsBody
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
        private const float MAX_SHIP_SPEED = SPEED_OF_LIGHT_IN_VACUUM / 2.0f;// m/s

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

        public static float CharacterFlyingMaxLinearVelocity()
        {
            return ShipMaxLinearVelocity() * 1.05f; // 100 m/s
        }
        public static float CharacterWalkingMaxLinearVelocity()
        {
            return CharacterFlyingMaxLinearVelocity() * 1.3f;
        }

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
                RigidBody.Layer = MyPhysics.StaticCollisionLayer;
                RigidBody.MaxAngularVelocity = GetLargeShipMaxAngularVelocity();
                RigidBody.MaxLinearVelocity = LargeShipMaxLinearVelocity();
            }
            else if (m_grid.GridSizeEnum == MyCubeSize.Large)
            {
                RigidBody.MaxAngularVelocity = GetLargeShipMaxAngularVelocity();
                RigidBody.MaxLinearVelocity = LargeShipMaxLinearVelocity();
                RigidBody.Layer = flags == RigidBodyFlag.RBF_DOUBLED_KINEMATIC && MyFakes.ENABLE_DOUBLED_KINEMATIC ? MyPhysics.DynamicDoubledCollisionLayer : MyPhysics.DefaultCollisionLayer;
            }
            else if (m_grid.GridSizeEnum == MyCubeSize.Small)
            {
                RigidBody.MaxAngularVelocity = GetSmallShipMaxAngularVelocity();
                RigidBody.MaxLinearVelocity = SmallShipMaxLinearVelocity();
                RigidBody.Layer = MyPhysics.DefaultCollisionLayer;
            }

            if (RigidBody2 != null)
            {
                RigidBody2.Layer = MyPhysics.KinematicDoubledCollisionLayer;
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

        void BreakableBody_AfterControllerOperation(HkdBreakableBody b)
        {
            ProfilerShort.Begin("BreakableBody_AfterControllerOperation");
            if (m_recreateBody)
                b.BreakableShape.SetStrenghtRecursively(MyDestructionConstants.STRENGTH,0.7f);
            ProfilerShort.End();
        }

        void BreakableBody_BeforeControllerOperation(HkdBreakableBody b)
        {
            ProfilerShort.Begin("BreakableBody_AfterControllerOperation");
            if (m_recreateBody)
                b.BreakableShape.SetStrenghtRecursively(float.MaxValue, 0.7f);
            ProfilerShort.End();
        }

        protected override void ActivateCollision()
        {
            HavokCollisionSystemID = m_world.GetCollisionFilter().GetNewSystemGroup();//grids now have unique group, so collision between [sub]parts can be filtered out
        }

        public override void Activate(object world, ulong clusterObjectID)
        {
            base.Activate(world, clusterObjectID);

            MarkBreakable((HkWorld)world);
        }

        public override void ActivateBatch(object world, ulong clusterObjectID)
        {
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

            if ((Math.Abs(value.SeparatingVelocity) < 0.3f) && (otherEntity is MyTrees))
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
            bool hitVoxel = info.CollidingEntity is MyVoxelMap;

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

        internal ushort? GetContactCompoundId(Vector3I position, Vector3D constactPos)
        {
            List<HkdBreakableShape> inersectingShapes = new List<HkdBreakableShape>();
            Matrix bodyMatrix = GetRigidBodyMatrix();
            Quaternion bodyRot = Quaternion.CreateFromRotationMatrix(bodyMatrix);
            HkDestructionUtils.FindAllBreakableShapesIntersectingSphere(HavokWorld.DestructionWorld, BreakableBody, bodyRot, bodyMatrix.Translation,
                WorldToCluster(constactPos), 0.1f, inersectingShapes);

            ushort? compoundId = null;

            foreach (var shape in inersectingShapes)
            {
                if (!shape.IsValid())
                    continue;

                HkVec3IProperty propGridPos = shape.GetProperty(HkdBreakableShape.PROPERTY_GRID_POSITION);
                if (!propGridPos.IsValid() || position != propGridPos.Value)
                    continue;

                HkSimpleValueProperty propCompoundId = shape.GetProperty(HkdBreakableShape.PROPERTY_BLOCK_COMPOUND_ID);
                if (!propCompoundId.IsValid())
                    continue;

                compoundId = (ushort)propCompoundId.ValueUInt;
                break;
            }

            return compoundId;
        }

        void RigidBody_ContactPointCallback_Destruction(ref HkContactPointEvent value)
        {
            ProfilerShort.Begin("Grid Contact counter");
            ProfilerShort.End();
            MyGridContactInfo info = new MyGridContactInfo(ref value, m_grid);

            if (info.IsKnown)
                return;

            var myBody = info.CurrentEntity.Physics.RigidBody;//value.Base.BodyA.GetEntity() == m_grid.Components ? value.Base.BodyA : value.Base.BodyB;
            var myEntity = info.CurrentEntity;//value.Base.BodyA.GetEntity() == m_grid.Components ? value.Base.BodyA.GetEntity() : value.Base.BodyB.GetEntity();

            // CH: DEBUG
            var entity1 = value.GetPhysicsBody(0).Entity;
            var entity2 = value.GetPhysicsBody(1).Entity;

            var rigidBody1 = value.Base.BodyA;
            var rigidBody2 = value.Base.BodyB;

            if (entity1 == null || entity2 == null || entity1.Physics == null || entity2.Physics == null)
                return;

            if (entity1 is MyFracturedPiece && entity2 is MyFracturedPiece)
                return;

            info.HandleEvents();
            if (rigidBody1.HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT) || rigidBody2.HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT))
                return;

            if (info.CollidingEntity is Sandbox.Game.Entities.Character.MyCharacter || info.CollidingEntity == null || info.CollidingEntity.MarkedForClose)
                return;

            var grid1 = entity1 as MyCubeGrid;
            var grid2 = entity2 as MyCubeGrid;

            if (grid1 != null && grid2 != null && (MyCubeGridGroups.Static.Physical.GetGroup(grid1) == MyCubeGridGroups.Static.Physical.GetGroup(grid2)))
                return;

            ProfilerShort.Begin("Grid contact point callback");

            if (Sync.IsServer)
            {
                var vel = Math.Abs(value.SeparatingVelocity);
                bool enoughSpeed = vel > 3;
                //float dot = Vector3.Dot(Vector3.Normalize(LinearVelocity), Vector3.Normalize(info.CollidingEntity.Physics.LinearVelocity));


                Vector3 velocity1 = rigidBody1.GetVelocityAtPoint(info.Event.ContactPoint.Position);
                Vector3 velocity2 = rigidBody2.GetVelocityAtPoint(info.Event.ContactPoint.Position);

                float speed1 = velocity1.Length();
                float speed2 = velocity2.Length();

                Vector3 dir1 = speed1 > 0 ? Vector3.Normalize(velocity1) : Vector3.Zero;
                Vector3 dir2 = speed2 > 0 ? Vector3.Normalize(velocity2) : Vector3.Zero;

                float mass1 = MyDestructionHelper.MassFromHavok(rigidBody1.Mass);
                float mass2 = MyDestructionHelper.MassFromHavok(rigidBody2.Mass);

                float impact1 = speed1 * mass1;
                float impact2 = speed2 * mass2;

                float dot1withNormal = speed1 > 0 ? Vector3.Dot(dir1, value.ContactPoint.Normal) : 0;
                float dot2withNormal = speed2 > 0 ? Vector3.Dot(dir2, value.ContactPoint.Normal) : 0;

                speed1 *= Math.Abs(dot1withNormal);
                speed2 *= Math.Abs(dot2withNormal);

                bool is1Static = mass1 == 0;
                bool is2Static = mass2 == 0;

                bool is1Small = entity1 is MyFracturedPiece || (grid1 != null && grid1.GridSizeEnum == MyCubeSize.Small);
                bool is2Small = entity2 is MyFracturedPiece || (grid2 != null && grid2.GridSizeEnum == MyCubeSize.Small);


                float dot = Vector3.Dot(dir1, dir2);

                float maxDestructionRadius = 0.5f;

                impact1 *= info.ImpulseMultiplier;
                impact2 *= info.ImpulseMultiplier;

                MyHitInfo hitInfo = new MyHitInfo();
                var hitPos = info.ContactPosition;
                hitInfo.Normal = value.ContactPoint.Normal;

                //direct hit
                if (dot1withNormal < 0.0f)
                {
                    if (entity1 is MyFracturedPiece)
                        impact1 /= 10;

                    impact1 *= Math.Abs(dot1withNormal); //respect angle of hit

                    if (entity2 is MyFracturedPiece)
                    {
                    }

                    if ((impact1 > 2000 && speed1 > 2 && !is2Small) ||
                        (impact1 > 500 && speed1 > 10)) //must be fast enought to destroy fracture piece (projectile)
                    {  //1 is big hitting

                        if (is2Static || impact1 / impact2 > 10)
                        {
                            hitInfo.Position = hitPos + 0.1f * hitInfo.Normal;
                            impact1 -= mass1;

                            if (impact1 > 0)
                            {
                                if (grid1 != null)
                                {
                                    var blockPos = GetGridPosition(value, grid1, 0);
                                    grid1.DoDamage(impact1, hitInfo, blockPos, grid2 != null ? grid2.EntityId : 0);
                                }
                                else
                                    MyDestructionHelper.TriggerDestruction(impact1, (MyPhysicsBody)entity1.Physics, info.ContactPosition, value.ContactPoint.Normal, maxDestructionRadius);
                                hitInfo.Position = hitPos - 0.1f * hitInfo.Normal;
                                if (grid2 != null)
                                {
                                    var blockPos = GetGridPosition(value, grid2, 1);
                                    grid2.DoDamage(impact1, hitInfo, blockPos, grid1 != null ? grid1.EntityId : 0);
                                }
                                else
                                    MyDestructionHelper.TriggerDestruction(impact1, (MyPhysicsBody)entity2.Physics, info.ContactPosition, value.ContactPoint.Normal, maxDestructionRadius);

                                ReduceVelocities(info);
                            }
                        }
                    }
                }

                if (dot2withNormal < 0.0f)
                {
                    if (entity2 is MyFracturedPiece)
                        impact2 /= 10;

                    impact2 *= Math.Abs(dot1withNormal); //respect angle of hit

                    if (impact2 > 2000 && speed2 > 2 && !is1Small ||
                        (impact2 > 500 && speed2 > 10)) //must be fast enought to destroy fracture piece (projectile)
                    {  //2 is big hitting

                        if (is1Static || impact2 / impact1 > 10)
                        {
                            hitInfo.Position = hitPos + 0.1f * hitInfo.Normal;
                            impact2 -= mass2;

                            if (impact2 > 0)
                            {
                                if (grid1 != null)
                                {
                                    var blockPos = GetGridPosition(value, grid1, 0);
                                    grid1.DoDamage(impact2, hitInfo, blockPos, grid2 != null ? grid2.EntityId : 0);
                                }
                                else
                                    MyDestructionHelper.TriggerDestruction(impact2, (MyPhysicsBody)entity1.Physics, info.ContactPosition, value.ContactPoint.Normal, maxDestructionRadius);
                                hitInfo.Position = hitPos - 0.1f * hitInfo.Normal;
                                if (grid2 != null)
                                {
                                    var blockPos = GetGridPosition(value, grid2, 1);
                                    grid2.DoDamage(impact2, hitInfo, blockPos, grid1 != null ? grid1.EntityId : 0);
                                }
                                else
                                    MyDestructionHelper.TriggerDestruction(impact2, (MyPhysicsBody)entity2.Physics, info.ContactPosition, value.ContactPoint.Normal, maxDestructionRadius);

                                ReduceVelocities(info);
                            }
                        }
                    }
                }

                //float destructionImpact = vel * (MyDestructionHelper.MassFromHavok(Mass) + MyDestructionHelper.MassFromHavok(info.CollidingEntity.Physics.Mass));
                //destructionImpact *= info.ImpulseMultiplier;

                //if (destructionImpact > 2000 && enoughSpeed)
                //{
                //    CreateDestructionFor(destructionImpact, LinearVelocity + info.CollidingEntity.Physics.LinearVelocity, this, info, value.ContactPoint.Normal);
                //    CreateDestructionFor(destructionImpact, LinearVelocity + info.CollidingEntity.Physics.LinearVelocity, info.CollidingEntity.Physics, info, value.ContactPoint.Normal);

                //    ReduceVelocities(info);
                //}
            }

            ProfilerShort.End();
        }

        private static Vector3 GetGridPosition(HkContactPointEvent value, MyCubeGrid grid, int body)
        {
            var position = value.ContactPoint.Position + (body == 0 ? 0.1f : -0.1f) * value.ContactPoint.Normal;
            var local = Vector3.Transform(value.ContactPoint.Position, Matrix.Invert(value.Base.GetRigidBody(body).GetRigidBodyMatrix()));
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
            if (Math.Abs(separatingVelocity) < 2)
                return false;
            var otherEntity = pt.CollidingBody.GetEntity(0);
            pt.ContactPosition = ClusterToWorld(pt.ContactPoint.Position);

            var destroyed = false;
            var group = MyWeldingGroups.Static.GetGroup((MyEntity)Entity);
            if (group != null)
            {
                foreach (var node in group.Nodes)
                {
                    m_tmpEntities.Add(node.NodeData);
                }
                foreach (var node in m_tmpEntities)
                {
                    if (node.MarkedForClose)
                        continue;
                    var gp = node.Physics as MyGridPhysics;
                    if (gp == null)
                        continue;
                    destroyed |= gp.PerformDeformation(ref pt, false, separatingVelocity, otherEntity as MyEntity);
                }
                m_tmpEntities.Clear();
            }
            group = MyWeldingGroups.Static.GetGroup((MyEntity)otherEntity);
            if (group != null)
            {
                foreach (var node in group.Nodes)
                {
                    m_tmpEntities.Add(node.NodeData);
                }

                pt.ContactPointDirection *= -1;
                foreach (var node in m_tmpEntities)
                {
                    if (node.MarkedForClose)
                        continue;
                    var gp = node.Physics as MyGridPhysics;
                    if (gp == null)
                        continue;
                    destroyed |= gp.PerformDeformation(ref pt, false, separatingVelocity, Entity as MyEntity);
                }
            }
            return destroyed;
        }

        private bool BreakPartsHandler(ref HkBreakOffPoints breakOffPoints, ref HkArrayUInt32 brokenKeysOut)
        {
            if (!Sync.IsServer)
                return true;
            ProfilerShort.Begin("BreakPartsHandler");
            Debug.Assert(breakOffPoints.Count > 0);
            bool recollide = false;
            for (int i = 0; i < breakOffPoints.Count; i++)
            {
                var pt = breakOffPoints[i];
                recollide |= BreakAtPoint(ref pt, ref brokenKeysOut);
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

            var invWorld = m_grid.PositionComp.GetWorldMatrixNormalizedInv();
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
                    invWorld = grid.PositionComp.GetWorldMatrixNormalizedInv();
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

            var invWorld = m_grid.PositionComp.GetWorldMatrixNormalizedInv();
            var pos = Vector3D.Transform(pt.ContactPosition, invWorld);
            var velAtPoint = GetVelocityAtPoint(pt.ContactPosition);
            if (!velAtPoint.IsValid() || velAtPoint == Vector3.Zero)
                velAtPoint = pt.ContactPoint.Normal;
            var len = velAtPoint.Normalize();
            var normal = Vector3D.TransformNormal(velAtPoint, invWorld) * pt.ContactPointDirection;
            int destroyed = ApplyDeformation(deformationOffset, softAreaPlanar, softAreaVertical, pos, normal, MyDamageType.Deformation, attackerId: otherEntity != null ? otherEntity.EntityId : 0);
            if (destroyed != 0)
            {
                if (len > 1)
                    len = len / 10;
                RigidBody.ApplyPointImpulse(-velAtPoint *0.3f* Math.Min(destroyed * len * (Mass / 5), Mass * 5), pt.ContactPoint.Position);
                RigidBody.Gravity = Vector3.Zero;
                DisableGravity = 5;
            }
            if (explosionRadius > 0 && deformationOffset > m_grid.GridSize / 2 && destroyed != 0)
            {
                var info = new ExplosionInfo()
                {
                    Position = pt.ContactPosition,
                    //ExplosionType = destroyed ? MyExplosionTypeEnum.GRID_DESTRUCTION : MyExplosionTypeEnum.GRID_DEFORMATION,
                    ExplosionType = MyExplosionTypeEnum.GRID_DESTRUCTION,
                    Radius = explosionRadius,
                    ShowParticles = (otherEntity is MyTrees) == false,
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
                m_tmpBoneList.Clear();
                m_grid.GetExistingBones( minOffset, maxOffset, m_tmpBoneList, damageInfo);
                ProfilerShort.End();

                ProfilerShort.Begin("Deform bones");
                Vector3 bone;
                Vector3I baseOffset = gridPos * m_grid.Skeleton.BoneDensity;
                float boneDensityR = 1.0f / m_grid.Skeleton.BoneDensity;
                var halfGridSize = new Vector3(m_grid.GridSize * 0.5f);

                ProfilerShort.CustomValue("Bone Count", m_tmpBoneList.Count,0);
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
                    if (MyParticlesManager.TryCreateParticleEffect(51, out effect))
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
                m_grid.Close();
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
                var t1 = wm.Translation - RigidBody.LinearVelocity * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 0.5f;
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
                ProfilerShort.Begin("CreateGridBody");

                HkdBreakableShape breakable;
                HkMassProperties massProps = massProperties.HasValue ? massProperties.Value : new HkMassProperties();

                if (!Shape.BreakableShape.IsValid())
                    Shape.CreateBreakableShape();

                breakable = Shape.BreakableShape;

                if (!breakable.IsValid())
                {
                    breakable = new HkdBreakableShape(shape);
                    if (massProperties.HasValue)
                    {
                        var mp = massProperties.Value;
                        breakable.SetMassProperties(ref mp);
                    }
                    else
                        breakable.SetMassRecursively(50);
                }
                else
                    breakable.BuildMassProperties(ref massProps);

                shape = breakable.GetShape(); //doesnt add reference
                HkRigidBodyCinfo rbInfo = new HkRigidBodyCinfo();
                rbInfo.AngularDamping = m_angularDamping;
                rbInfo.LinearDamping = m_linearDamping;
                rbInfo.SolverDeactivation = m_grid.IsStatic ? InitialSolverDeactivation : HkSolverDeactivation.Low;
                rbInfo.ContactPointCallbackDelay = ContactPointDelay;
                rbInfo.Shape = shape;
                rbInfo.SetMassProperties(massProps);
                //rbInfo.Position = Entity.PositionComp.GetPosition(); //obsolete with large worlds?
                GetInfoFromFlags(rbInfo, Flags);
                if (m_grid.IsStatic)
                {
                    rbInfo.MotionType = HkMotionType.Dynamic;
                    rbInfo.QualityType = HkCollidableQualityType.Moving;
                }
                HkRigidBody rb = new HkRigidBody(rbInfo);
                if (m_grid.IsStatic)
                {
                    rb.UpdateMotionType(HkMotionType.Fixed);
                }
                rb.EnableDeactivation = true;
                BreakableBody = new HkdBreakableBody(breakable, rb, MyPhysics.SingleWorld.DestructionWorld, Matrix.Identity);
                //DestructionBody.ConnectToWorld(HavokWorld, 0.05f);

                BreakableBody.AfterReplaceBody += FracturedBody_AfterReplaceBody;

                //RigidBody.SetWorldMatrix(Entity.PositionComp.WorldMatrix);
                //breakable.Dispose();

                ProfilerShort.End();
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

        private List<HkdBreakableBodyInfo> m_newBodies = new List<HkdBreakableBodyInfo>();
        private List<HkdShapeInstanceInfo> m_children = new List<HkdShapeInstanceInfo>();
        private static List<HkdShapeInstanceInfo> m_tmpChildren_RemoveShapes = new List<HkdShapeInstanceInfo>();
        private static List<string> m_tmpShapeNames = new List<string>();

        private List<Vector3D> m_splitPosition = new List<Vector3D>();
        private List<Sandbox.Game.Entities.Cube.MySlimBlock> m_blocksToDisconnect = new List<Sandbox.Game.Entities.Cube.MySlimBlock>();
        private bool m_recreateBody;
        private Vector3 m_oldLinVel;
        private Vector3 m_oldAngVel;

        List<HkdBreakableBody> m_newBreakableBodies = new List<HkdBreakableBody>();
        public override void FracturedBody_AfterReplaceBody(ref HkdReplaceBodyEvent e)
        {
            if (!MyFakes.ENABLE_AFTER_REPLACE_BODY)
                return;

            System.Diagnostics.Debug.Assert(Sync.IsServer, "Client must not simulate destructions");

            if (!Sync.IsServer)
                return;
            Debug.Assert(!m_recreateBody, "Only one destruction per entity per frame");
            if (m_recreateBody)
                return;
            ProfilerShort.Begin("DestructionFracture.AfterReplaceBody");
            HavokWorld.DestructionWorld.RemoveBreakableBody(e.OldBody); // To remove from HkpWorld rigidBodies list
            m_oldLinVel = RigidBody.LinearVelocity;
            m_oldAngVel = RigidBody.AngularVelocity;
            MyPhysics.RemoveDestructions(RigidBody);
            e.GetNewBodies(m_newBodies);
            if (m_newBodies.Count == 0)// || e.OldBody != DestructionBody)
                return;
            bool createdEffect = false;
            var blocksToDelete = new HashSet<MySlimBlock>();
            MySlimBlock destructionSoundBlock = null;
            ProfilerShort.Begin("ProcessBodies");
            foreach (var b in m_newBodies)
            {
                if (!b.IsFracture())// && m_grid.BlocksCount > 1)// || !IsFracture(b.Body.BreakableShape))
                {
                    m_newBreakableBodies.Add(MyFracturedPiecesManager.Static.GetBreakableBody(b));
                    ProfilerShort.Begin("FindFracturedBlocks");
                    FindFracturedBlocks(b);
                    ProfilerShort.BeginNextBlock("RemoveBBfromWorld");
                    HavokWorld.DestructionWorld.RemoveBreakableBody(b);
                    ProfilerShort.End();
                }
                else
                {
                    ProfilerShort.Begin("CreateFracture");
                    var bBody = MyFracturedPiecesManager.Static.GetBreakableBody(b);
                    var bodyMatrix = bBody.GetRigidBody().GetRigidBodyMatrix();
                    var pos = ClusterToWorld(bodyMatrix.Translation);

                    MySlimBlock cb;
                    var shape = bBody.BreakableShape;
                    ProfilerShort.Begin("Properties");
                    HkVec3IProperty prop = shape.GetProperty(HkdBreakableShape.PROPERTY_GRID_POSITION);
                    if (!prop.IsValid() && shape.IsCompound())  //TODO: this is last block in grid and is fractured
                    {                                           //its compound of childs of our block shape that has position prop, 
                        prop = shape.GetChild(0).Shape.GetProperty(HkdBreakableShape.PROPERTY_GRID_POSITION);
                    }

                    // Block id in compound.
                    ushort? compoundId = null;
                    if (MyFakes.ENABLE_FRACTURE_COMPONENT)
                    {
                        HkSimpleValueProperty compoundIdProperty = shape.GetProperty(HkdBreakableShape.PROPERTY_BLOCK_COMPOUND_ID);
                        if (!compoundIdProperty.IsValid() && shape.IsCompound())
                            compoundIdProperty = shape.GetChild(0).Shape.GetProperty(HkdBreakableShape.PROPERTY_BLOCK_COMPOUND_ID);

                        if (compoundIdProperty.IsValid())
                            compoundId = (ushort)compoundIdProperty.ValueUInt;
                    }

                    ProfilerShort.End();
                    Debug.Assert(prop.IsValid());
                    bool removePiece = false;
                    cb = m_grid.GetCubeBlock(prop.Value);

                    if (MyFakes.ENABLE_FRACTURE_COMPONENT && cb != null)
                    {
                        // Prepare block from compound
                        MyCompoundCubeBlock compoundBlock = cb.FatBlock as MyCompoundCubeBlock;
                        if (compoundBlock != null)
                        {
                            if (compoundId != null)
                            {
                                var blockInCompound = compoundBlock.GetBlock(compoundId.Value);
                                if (blockInCompound != null)
                                {
                                    cb = blockInCompound;
                                }
                                else
                                {
                                    Debug.Fail("Block with the BlockId not found in compound");
                                    cb = null;
                                }
                            }
                            else
                            {
                                Debug.Fail("CompoundId not set to internal block in compound");
                                cb = null;
                            }
                        }
                        else
                        {
                            Debug.Assert(compoundId == null, "CompoundId set to block which is not in compound");
                        }
                    }

                    if (cb != null)
                    {
                        if (destructionSoundBlock == null)
                            destructionSoundBlock = cb;

                        if (!createdEffect)
                        {
                            ProfilerShort.Begin("CreateParticle");
                            AddDestructionEffect(m_grid.GridIntegerToWorld(cb.Position), Vector3.Down);
                            createdEffect = true;
                            ProfilerShort.End();
                        }

                        MatrixD m = bodyMatrix;
                        m.Translation = pos;
                        MyFracturedPiece fp = null;
                        fp = MyDestructionHelper.CreateFracturePiece(bBody, ref m, null, cb.FatBlock);
                        if (fp == null)
                        {
                            removePiece = true;
                        }

                        if (MyFakes.ENABLE_FRACTURE_COMPONENT && cb.FatBlock.Components.Has<MyFractureComponentBase>())
                        {
                            // Block can be removed when the removed shape is the last one!
                            MyFractureComponentCubeBlock fractureComponent = cb.GetFractureComponent();
                            if (fractureComponent != null)
                            {
                                var bShape = bBody.BreakableShape;
                                if (IsBreakableShapeCompound(bShape))
                                {
                                    m_tmpShapeNames.Clear();
                                    m_tmpChildren_RemoveShapes.Clear();

                                    bShape.GetChildren(m_tmpChildren_RemoveShapes);
                                    m_tmpChildren_RemoveShapes.ForEach(c => m_tmpShapeNames.Add(c.ShapeName));

                                    // Remove on server.
                                    fractureComponent.RemoveChildShapes(m_tmpShapeNames);

                                    MySyncDestructions.RemoveShapesFromFractureComponent(cb.CubeGrid.EntityId, cb.Position, compoundId ?? 0xFFFF, m_tmpShapeNames);

                                    m_tmpChildren_RemoveShapes.Clear();
                                    m_tmpShapeNames.Clear();
                                }
                                else
                                {
                                    var name = bBody.BreakableShape.Name;

                                    // Remove on server.
                                    fractureComponent.RemoveChildShapes(new string[] { name });

                                    MySyncDestructions.RemoveShapeFromFractureComponent(cb.CubeGrid.EntityId, cb.Position, compoundId ?? 0xFFFF, name);
                                }
                            }
                        }
                        else
                        {
                            blocksToDelete.Add(cb);
                        }
                    }
                    else
                    {
                        //Debug.Fail("Fracture piece missing block!");//safe to ignore
                        removePiece = true;
                    }

                    if (removePiece)
                    {
                        HavokWorld.DestructionWorld.RemoveBreakableBody(b);
                        MyFracturedPiecesManager.Static.ReturnToPool(bBody);
                    }
                    ProfilerShort.End();
                }
            }
            m_newBodies.Clear();
            bool oldGeneratorsEnabled = m_grid.EnableGenerators(false);

            if (destructionSoundBlock != null)
                MyAudioComponent.PlayDestructionSound(destructionSoundBlock);

            if (MyFakes.ENABLE_FRACTURE_COMPONENT)
            {
                // Remove blocks from blocksToDelete which are marked for creating fracture component.
                m_fractureBlockComponentsCache.ForEach(info => blocksToDelete.Remove(((MyCubeBlock)info.Entity).SlimBlock));

                foreach (var cb in blocksToDelete)
                {
                    if (cb.FatBlock != null)
                        cb.FatBlock.OnDestroy();

                    m_grid.RemoveBlockWithId(cb, true);
                }
            }
            else
            {
                foreach (var cb in blocksToDelete)
                {
                    var b = m_grid.GetCubeBlock(cb.Position);
                    if (b != null)
                    {
                        if (b.FatBlock != null)
                            b.FatBlock.OnDestroy();
                        m_grid.RemoveBlock(b, true);
                    }
                }
            }

            m_grid.EnableGenerators(oldGeneratorsEnabled);
            ProfilerShort.End();
            //SplitGrid(e);
            m_recreateBody = true;
            ProfilerShort.End();
        }

        private static bool IsBreakableShapeCompound(HkdBreakableShape shape)
        {
            return (string.IsNullOrEmpty(shape.Name) || shape.IsCompound() || shape.GetChildrenCount() > 0);
        }

        public List<MyFracturedBlock.Info> GetFracturedBlocks() { Debug.Assert(!MyFakes.ENABLE_FRACTURE_COMPONENT); return m_fractureBlocksCache; }
        private List<MyFracturedBlock.Info> m_fractureBlocksCache = new List<MyFracturedBlock.Info>();
        Dictionary<Vector3I, List<HkdShapeInstanceInfo>> m_fracturedBlocksShapes = new Dictionary<Vector3I, List<HkdShapeInstanceInfo>>();

        public List<MyFractureComponentBase.Info> GetFractureBlockComponents() { Debug.Assert(MyFakes.ENABLE_FRACTURE_COMPONENT); return m_fractureBlockComponentsCache; }
        public void ClearFractureBlockComponents() { m_fractureBlockComponentsCache.Clear(); }
        private List<MyFractureComponentBase.Info> m_fractureBlockComponentsCache = new List<MyFractureComponentBase.Info>();
        Dictionary<MySlimBlock, List<HkdShapeInstanceInfo>> m_fracturedSlimBlocksShapes = new Dictionary<MySlimBlock, List<HkdShapeInstanceInfo>>();

        private void FindFracturedBlocks(HkdBreakableBodyInfo b)
        {
            ProfilerShort.Begin("DBHelper");
            var dbHelper = new HkdBreakableBodyHelper(b);
            ProfilerShort.BeginNextBlock("GetRBMatrix");
            var bodyMatrix = dbHelper.GetRigidBodyMatrix();
            ProfilerShort.BeginNextBlock("SearchChildren");
            dbHelper.GetChildren(m_children);
            foreach (var child in m_children)
            {
                if (!child.IsFracturePiece())
                    continue;
                //var blockPosWorld = ClusterToWorld(Vector3.Transform(child.GetTransform().Translation, bodyMatrix));
                var bShape = child.Shape;
                HkVec3IProperty pProp = bShape.GetProperty(HkdBreakableShape.PROPERTY_GRID_POSITION);
                var blockPos = pProp.Value; //Vector3I.Round(child.GetTransform().Translation / m_grid.GridSize);
                if (!m_grid.CubeExists(blockPos))
                {
                    //Debug.Fail("FindFracturedBlocks:Fracture piece missing block");//safe to ignore
                    continue;
                }

                if (MyFakes.ENABLE_FRACTURE_COMPONENT)
                {
                    var block = m_grid.GetCubeBlock(blockPos);
                    if (block == null)
                        continue;

                    if (!FindFractureComponentBlocks(block, child))
                        continue;
                }
                else
                {
                    if (!m_fracturedBlocksShapes.ContainsKey(blockPos))
                        m_fracturedBlocksShapes[blockPos] = new List<HkdShapeInstanceInfo>();
                    m_fracturedBlocksShapes[blockPos].Add(child);
                }
            }
            ProfilerShort.BeginNextBlock("CreateFreacturedBlocks");
            if (MyFakes.ENABLE_FRACTURE_COMPONENT)
            {
                foreach (var pair in m_fracturedSlimBlocksShapes)
                {
                    var slimBlock = pair.Key;
                    var shapeList = pair.Value;

                    if (slimBlock.FatBlock.Components.Has<MyFractureComponentBase>())
                    {
                        // Block has fracture component - ignore
                        continue;
                    }
                    else
                    {
                        foreach (var s in shapeList)
                        {
                            s.SetTransform(ref Matrix.Identity);
                        }
                        ProfilerShort.Begin("CreateShapeComponent");
                        HkdBreakableShape compound = new HkdCompoundBreakableShape(null, shapeList);
                        ((HkdCompoundBreakableShape)compound).RecalcMassPropsFromChildren();
                        var mp = new HkMassProperties();
                        compound.BuildMassProperties(ref mp);
                        HkdBreakableShape shape = compound;
                        var sh = compound.GetShape();
                        shape = new HkdBreakableShape(sh, ref mp);
                        //shape.SetMassProperties(mp); //important! pass mp to constructor
                        foreach (var si in shapeList)
                        {
                            var siRef = si;
                            shape.AddShape(ref siRef);
                        }
                        compound.RemoveReference();

                        ProfilerShort.BeginNextBlock("Connect");
                        //shape.SetChildrenParent(shape);
                        ConnectPiecesInBlock(shape, shapeList);

                        MyFractureComponentBase.Info info = new MyFractureComponentBase.Info() 
                        {
                            Entity = slimBlock.FatBlock,
                            Shape = shape, 
                            Compound = true
                        };

                        m_fractureBlockComponentsCache.Add(info);

                        ProfilerShort.End();
                    }
                }
            }
            else
            {
                foreach (var key in m_fracturedBlocksShapes.Keys)
                {
                    HkdBreakableShape shape;
                    var shapeList = m_fracturedBlocksShapes[key];
                    foreach (var s in shapeList)
                    {
                        var matrix = s.GetTransform();
                        matrix.Translation = Vector3.Zero;
                        s.SetTransform(ref matrix);
                    }
                    ProfilerShort.Begin("CreateShape");
                    HkdBreakableShape compound = new HkdCompoundBreakableShape(null, shapeList);
                    ((HkdCompoundBreakableShape)compound).RecalcMassPropsFromChildren();
                    var mp = new HkMassProperties();
                    compound.BuildMassProperties(ref mp);
                    shape = compound;
                    var sh = compound.GetShape();
                    shape = new HkdBreakableShape(sh, ref mp);
                    //shape.SetMassProperties(mp); //important! pass mp to constructor
                    foreach (var si in shapeList)
                    {
                        var siRef = si;
                        shape.AddShape(ref siRef);
                    }
                    compound.RemoveReference();
                    ProfilerShort.BeginNextBlock("Connect");
                    //shape.SetChildrenParent(shape);
                    ConnectPiecesInBlock(shape, shapeList);
                    ProfilerShort.End();

                    var info = new MyFracturedBlock.Info()
                    {
                        Shape = shape,
                        Position = key,
                        Compound = true,
                    };
                    var originalBlock = m_grid.GetCubeBlock(key);
                    if (originalBlock == null)
                    {
                        //Debug.Fail("Missing fracture piece original block.");//safe to ignore
                        shape.RemoveReference();
                        continue;
                    }
                    Debug.Assert(originalBlock != null);
                    if (originalBlock.FatBlock is MyFracturedBlock)
                    {
                        var fractured = originalBlock.FatBlock as MyFracturedBlock;
                        info.OriginalBlocks = fractured.OriginalBlocks;
                        info.Orientations = fractured.Orientations;
                        info.MultiBlocks = fractured.MultiBlocks;
                    }
                    else if (originalBlock.FatBlock is MyCompoundCubeBlock)
                    {
                        info.OriginalBlocks = new List<Definitions.MyDefinitionId>();
                        info.Orientations = new List<MyBlockOrientation>();
                        MyCompoundCubeBlock compoundBlock = originalBlock.FatBlock as MyCompoundCubeBlock;
                        bool hasMultiBlockPart = false;
                        var blocksInCompound = compoundBlock.GetBlocks();
                        foreach (var block in blocksInCompound)
                        {
                            info.OriginalBlocks.Add(block.BlockDefinition.Id);
                            info.Orientations.Add(block.Orientation);

                            hasMultiBlockPart = hasMultiBlockPart || block.IsMultiBlockPart;
                        }

                        if (hasMultiBlockPart)
                        {
                            info.MultiBlocks = new List<MyFracturedBlock.MultiBlockPartInfo>();

                            foreach (var block in blocksInCompound)
                            {
                                if (block.IsMultiBlockPart)
                                    info.MultiBlocks.Add(new MyFracturedBlock.MultiBlockPartInfo() { MultiBlockDefinition = block.MultiBlockDefinition.Id, MultiBlockId = block.MultiBlockId });
                                else
                                    info.MultiBlocks.Add(null);
                            }
                        }
                    }
                    else
                    {
                        info.OriginalBlocks = new List<Definitions.MyDefinitionId>();
                        info.Orientations = new List<MyBlockOrientation>();
                        info.OriginalBlocks.Add(originalBlock.BlockDefinition.Id);
                        info.Orientations.Add(originalBlock.Orientation);

                        if (originalBlock.IsMultiBlockPart)
                        {
                            info.MultiBlocks = new List<MyFracturedBlock.MultiBlockPartInfo>();
                            info.MultiBlocks.Add(new MyFracturedBlock.MultiBlockPartInfo() { MultiBlockDefinition = originalBlock.MultiBlockDefinition.Id, MultiBlockId = originalBlock.MultiBlockId });
                        }
                    }
                    m_fractureBlocksCache.Add(info);
                }
            }
            m_fracturedBlocksShapes.Clear();
            m_children.Clear();

            m_fracturedSlimBlocksShapes.Clear();

            ProfilerShort.End();
        }

        /// <summary>
        /// Searches for all children shapes of the block (or block inside compound block). The given block is always the block from grid (not from compound block).
        /// </summary>
        /// <param name="block">grid block</param>
        /// <param name="shapeInst">shape instnce for the block</param>
        /// <returns></returns>
        private bool FindFractureComponentBlocks(MySlimBlock block, HkdShapeInstanceInfo shapeInst)
        {
            var bShape = shapeInst.Shape;
            if (IsBreakableShapeCompound(bShape))
            {
                bool anyAdded = false;
                List<HkdShapeInstanceInfo> children = new List<HkdShapeInstanceInfo>();
                bShape.GetChildren(children);

                foreach (var child in children)
                    anyAdded |= FindFractureComponentBlocks(block, child); 

                return anyAdded;
            }

            ushort? blockIdInCompound = null;
            if (bShape.HasProperty(HkdBreakableShape.PROPERTY_BLOCK_COMPOUND_ID))
            {
                HkSimpleValueProperty blockIdInCompoundProperty = bShape.GetProperty(HkdBreakableShape.PROPERTY_BLOCK_COMPOUND_ID);
                blockIdInCompound = (ushort)blockIdInCompoundProperty.ValueUInt;
            }

            var compoundBlock = block.FatBlock as MyCompoundCubeBlock;
            if (compoundBlock != null)
            {
                if (blockIdInCompound != null)
                {
                    var blockInCompound = compoundBlock.GetBlock(blockIdInCompound.Value);
                    if (blockInCompound != null)
                    {
                        block = blockInCompound;
                    }
                    else
                    {
                        Debug.Fail("No block with the id in compound");
                        return false;
                    }
                }
                else
                {
                    Debug.Fail("Breakable shape has not compoundId");
                    return false;
                }
            }

            if (!m_fracturedSlimBlocksShapes.ContainsKey(block))
                m_fracturedSlimBlocksShapes[block] = new List<HkdShapeInstanceInfo>();
            m_fracturedSlimBlocksShapes[block].Add(shapeInst);

            return true;
        }

        private static void ConnectPiecesInBlock(HkdBreakableShape parent, List<HkdShapeInstanceInfo> shapeList)
        {
            for (int i = 0; i < shapeList.Count; i++)
            {
                for (int j = 0; j < shapeList.Count; j++)
                {
                    if (i == j) continue;
                    MyGridShape.ConnectShapesWithChildren(parent, shapeList[i].Shape, shapeList[j].Shape);
                }
            }
        }

        private List<HkdShapeInstanceInfo> m_childList = new List<HkdShapeInstanceInfo>();
        public int DisableGravity = 0;
        private void RecreateBreakableBody()
        {
            ProfilerShort.Begin("RecreateBody");
            bool wasfixed = RigidBody.IsFixedOrKeyframed;
            var layer = RigidBody.Layer;
            if (false)//m_newBreakableBodies.Count == 1) //jn: keeps crashing now, putting aside for release
            {
                ProfilerShort.Begin("NewReplace");
                ProfilerShort.Begin("Close");
                BreakableBody.BreakableShape.ClearConnectionsRecursive();
                BreakableBody.BreakableShape.RemoveReference();
                CloseRigidBody();
                ProfilerShort.BeginNextBlock("2");
                BreakableBody = m_newBreakableBodies[0];
                Debug.Assert(RigidBody.Layer == layer, "New body has different layer!!");
                RigidBody.UserObject = this;
                RigidBody.ContactPointCallbackEnabled = true;
                RigidBody.ContactSoundCallbackEnabled = true;
                RigidBody.ContactPointCallback += RigidBody_ContactPointCallback_Destruction;
                BreakableBody.AfterReplaceBody += FracturedBody_AfterReplaceBody;
                BreakableBody.BreakableShape.AddReference();
                ProfilerShort.BeginNextBlock("ReplaceFractures");
                BreakableBody.BreakableShape.GetChildren(m_childList);
                for (int i = 0; i < m_childList.Count; i++) //remove fractures
                {
                    var child = m_childList[i];
                    Debug.Assert(((HkVec3IProperty)child.Shape.GetProperty(HkdBreakableShape.PROPERTY_GRID_POSITION)).IsValid());
                    if (child.Shape.IsFracturePiece())
                    {
                        //int j = 0;
                        //for (; j < BreakableBody.BreakableShape.GetChildrenCount(); j++)
                        //    if (BreakableBody.BreakableShape.GetChild(j).Shape == child.Shape)
                        //        break;
                        //Debug.Assert(child.Shape == BreakableBody.BreakableShape.GetChild(j).Shape && j >= i);
                        //child.Shape.SetFlagRecursively(HkdBreakableShape.Flags.DONT_CREATE_FRACTURE_PIECE);

                        //BreakableBody.BreakableShape.RemoveChild(j);

                        //child.Shape.RemoveReference();
                        //m_childList[i].Shape.RemoveReference();
                        //m_childList[i].RemoveReference();
                        m_childList.RemoveAt(i);
                        i--;
                    }
                }
                foreach (var fb in m_fractureBlocksCache) //add fractures grouped by block
                {
                    Debug.Assert(fb.Shape.IsValid() && !((HkVec3IProperty)fb.Shape.GetProperty(HkdBreakableShape.PROPERTY_GRID_POSITION)).IsValid());
                    fb.Shape.SetPropertyRecursively(HkdBreakableShape.PROPERTY_GRID_POSITION, new HkVec3IProperty(fb.Position));
                    Matrix m = Matrix.Identity;
                    m.Translation = fb.Position * m_grid.GridSize;
                    fb.Shape.SetChildrenParent(fb.Shape);
                    var si = new HkdShapeInstanceInfo(fb.Shape, m);
                    //BreakableBody.BreakableShape.AddShape(ref si);
                    //si.RemoveReference();
                    m_childList.Add(si);
                }
                BreakableBody.BreakableShape.ReplaceChildren(m_childList);
                for (int i = m_childList.Count - m_fractureBlocksCache.Count; i < m_childList.Count; i++)
                    m_childList[i].RemoveReference();
                m_childList.Clear();
                ProfilerShort.BeginNextBlock("Connections");
                BreakableBody.BreakableShape.SetChildrenParent(BreakableBody.BreakableShape);
                Shape.BreakableShape = BreakableBody.BreakableShape;
                Shape.UpdateDirtyBlocks(m_dirtyCubesInfo.DirtyBlocks, false);
                Shape.CreateConnectionToWorld(BreakableBody);
                if (wasfixed && m_grid.GridSizeEnum == MyCubeSize.Small)
                {
                    if (MyCubeGridSmallToLargeConnection.Static.TestGridSmallToLargeConnection(m_grid))
                    {
                        RigidBody.UpdateMotionType(HkMotionType.Fixed);
                        RigidBody.Quality = HkCollidableQualityType.Fixed;
                    }
                }
                ProfilerShort.BeginNextBlock("Add");
                HavokWorld.DestructionWorld.AddBreakableBody(BreakableBody);
                ProfilerShort.End();
                ProfilerShort.End();
            }
            else
            { //Old body is removed so create new one (should use matching one from new bodies in final version)
                foreach (var b in m_newBreakableBodies)
                {
                    MyFracturedPiecesManager.Static.ReturnToPool(b);
                }
                ProfilerShort.Begin("OldReplace");
                ProfilerShort.Begin("GetPhysicsBody");
                var ph = BreakableBody.GetRigidBody();
                var linVel = ph.LinearVelocity;
                var angVel = ph.AngularVelocity;
                ph = null;
                ProfilerShort.End();
                if (m_grid.BlocksCount > 0)
                {
                    ProfilerShort.Begin("Refresh");
                    Shape.RefreshBlocks(RigidBody, RigidBody2, m_dirtyCubesInfo, BreakableBody);
                    ProfilerShort.BeginNextBlock("NewGridBody");
                    CloseRigidBody();
                    var s = (HkShape)m_shape;
                    CreateBody(ref s, null);
                    RigidBody.Layer = layer;
                    RigidBody.ContactPointCallbackEnabled = true;
                    RigidBody.ContactSoundCallbackEnabled = true;
                    RigidBody.ContactPointCallback += RigidBody_ContactPointCallback_Destruction;
                    BreakableBody.BeforeControllerOperation += BreakableBody_BeforeControllerOperation;
                    BreakableBody.AfterControllerOperation += BreakableBody_AfterControllerOperation;
                    Matrix m = Entity.PositionComp.WorldMatrix;
                    m.Translation = WorldToCluster(Entity.PositionComp.GetPosition());
                    RigidBody.SetWorldMatrix(m);
                    RigidBody.UserObject = this;
                    Entity.Physics.LinearVelocity = m_oldLinVel;
                    Entity.Physics.AngularVelocity = m_oldAngVel;
                    m_grid.DetectDisconnectsAfterFrame();
                    Shape.CreateConnectionToWorld(BreakableBody);
                    HavokWorld.DestructionWorld.AddBreakableBody(BreakableBody);
                    ProfilerShort.End();
                }
                else
                {
                    ProfilerShort.Begin("GridClose");
                    m_grid.Close();
                    ProfilerShort.End();
                }
                ProfilerShort.End();
            }
            m_newBreakableBodies.Clear();
            m_fractureBlocksCache.Clear();
            ProfilerShort.End();
        }

        //private void SplitGrid(HkdReplaceBodyEvent e)
        //{
        //    //Grid splitting WIP
        //    ProfilerShort.Begin("Destruction.SplitGrid");
        //    if (m_grid.GetBlocks().Count == 1)
        //    {
        //        ProfilerShort.End();
        //        return;
        //    }
        //    //TODO: Cluster to world
        //    var mat = RigidBody.GetWorldMatrix();
        //    var conn = e.GetBrokenConnections();
        //    for (int i = 0; i < conn.ConnectedBodiesCount; i++) //Get broken connections positions
        //    {
        //        for (int j = 0; j < conn.GetConnectedBody(i).ConnectionsCount; j++)
        //        {
        //            Vector3D t = conn.GetConnectedBody(i).GetConnectionInfo(j).Connection.PivotA;
        //            m_splitPosition.Add(t / m_grid.GridSize);
        //        }
        //    }
        //    if (Entity is MyCubeGrid && m_splitPosition.Count > 1)
        //    {
        //        var grid = Entity as MyCubeGrid;
        //        for (int i = 0; i < m_splitPosition.Count; i += 2) //positions should come in pairs (since both A->B and B->A connections broke and we got beginning pivots from both)
        //        {
        //            Sandbox.Game.Entities.Cube.MySlimBlock a = grid.GetCubeBlock(Vector3I.Round(m_splitPosition[i]));
        //            Sandbox.Game.Entities.Cube.MySlimBlock b = grid.GetCubeBlock(Vector3I.Round(m_splitPosition[i + 1]));
        //            if (a == null || b == null)
        //                continue;
        //            if (a == b)
        //            {
        //                //m_grid.RemoveBlock(a);
        //                //m_blocksToDisconnect.Remove(a);
        //                DisconnectBlock(a);
        //                //m_tmpLst3.Add(a);
        //                continue;
        //            }
        //            var ab = b.Position - a.Position;
        //            var ba = a.Position - b.Position;
        //            AddFaces(a, ab);
        //            AddFaces(b, ba);
        //            if (!m_blocksToDisconnect.Contains(a))
        //                m_blocksToDisconnect.Add(a);
        //            if (!m_blocksToDisconnect.Contains(b))
        //                m_blocksToDisconnect.Add(b);
        //        }
        //        foreach (var b in m_blocksToDisconnect)
        //            grid.UpdateBlockNeighbours(b);
        //        foreach (var b in m_blocksToDisconnect)
        //            b.DisconnectFaces.Clear();
        //    }
        //    m_blocksToDisconnect.Clear();
        //    m_splitPosition.Clear();
        //    ProfilerShort.End();
        //}

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
                MyPhysics.CastRay(MySector.MainCamera.Position, MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 25, hitlist, MyPhysics.CollisionLayerWithoutCharacter);
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
        }

        /// <summary>
        /// Checks if the last block in grid has fracture component and converts it into fracture pieces.
        /// </summary>
        public bool CheckLastDestroyedBlockFracturePieces()
        {
            Debug.Assert(MyFakes.ENABLE_FRACTURE_COMPONENT);

            if (!Sync.IsServer)
                return false;

            if (m_grid.BlocksCount == 1)
            {
                var block = m_grid.GetBlocks().First();
                if (block.FatBlock != null)
                {
                    var compoundBlock = block.FatBlock as MyCompoundCubeBlock;

                    if (compoundBlock != null)
                    {
                        bool oldGeneratorsEnabled = m_grid.EnableGenerators(false);

                        bool allHaveFracComp = true;
                        var blocksInCompound = compoundBlock.GetBlocks();
                        foreach (var blockInCompound in blocksInCompound)
                        {
                            allHaveFracComp = allHaveFracComp && blockInCompound.FatBlock.Components.Has<MyFractureComponentBase>();
                        }

                        if (allHaveFracComp)
                        {
                            foreach (var blockInCompound in blocksInCompound)
                            {
                                var fractureComponent = blockInCompound.GetFractureComponent();
                                var compoundId = compoundBlock.GetBlockId(blockInCompound);
                                Debug.Assert(fractureComponent != null);
                                if (fractureComponent != null)
                                    MyDestructionHelper.CreateFracturePiece(fractureComponent, true);

                                m_grid.RemoveBlockWithId(blockInCompound.Position, compoundId.Value, true);
                            }
                        }

                        m_grid.EnableGenerators(oldGeneratorsEnabled);

                        return allHaveFracComp;
                    }
                    else
                    {
                        var fractureComponent = block.GetFractureComponent();
                        if (fractureComponent != null)
                        {
                            bool oldGeneratorsEnabled = m_grid.EnableGenerators(false);

                            MyDestructionHelper.CreateFracturePiece(fractureComponent, true);
                            m_grid.RemoveBlock(block, true);

                            m_grid.EnableGenerators(oldGeneratorsEnabled);
                        }

                        return fractureComponent != null;
                    }
                }
            }

            return false;
        }

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
                RigidBody.Layer = MyPhysics.DynamicDoubledCollisionLayer;
                SetupRigidBody2();
            }
            else
            {
                Flags = RigidBodyFlag.RBF_DEFAULT;   
                RigidBody.Layer = MyPhysics.DefaultCollisionLayer;
            }
            UpdateMass();
            ActivateCollision();
            HavokWorld.RefreshCollisionFilterOnEntity(RigidBody);
            RigidBody.Activate();
            HavokWorld.RigidBodyActivated(RigidBody);
        }

        private void SetupRigidBody2()
        {
            RigidBody2 = HkRigidBody.Clone(RigidBody);
            RigidBody2.Layer = MyPhysics.KinematicDoubledCollisionLayer;
            if (RigidBody.InWorld)
                HavokWorld.AddRigidBody(RigidBody2);
        }
    }
}
