#define DYNAMIC_CHARACTER_CONTROLLER

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
using Sandbox.AppCode.Game;
using Sandbox.Game.Utils;
using Sandbox.Engine.Models;
using Havok;
using Sandbox.Graphics;
using Sandbox.Common;
using Sandbox.Game.World;
using Sandbox.Game.Gui;
using Sandbox.Game.Entities.Character;
using VRage;
using Sandbox.Common.Components;
using VRageMath.Spatial;
using Sandbox.Game;

#endregion

namespace Sandbox.Engine.Physics
{
    //using MyHavokCluster = MyClusterTree<HkWorld>;
    using MyHavokCluster = MyClusterTree;
    using Sandbox.ModAPI;
    using Sandbox.Game.Entities.Cube;
    using Sandbox.Game.GameSystems;
    using Sandbox.Game.Multiplayer;
    using VRage.Library.Utils;
    using System;
    using Sandbox.Definitions;
    using VRage.ModAPI;
    using VRage.Components;

    /// <summary>
    /// Abstract engine physics body object.
    /// </summary>
    public class MyPhysicsBody : MyPhysicsComponentBase, MyClusterTree.IMyActivationHandler
    {
        public class MyWeldInfo
        {
            public MyPhysicsBody Parent = null;
            public Matrix Transform;
            public readonly HashSet<MyPhysicsBody> Children = new HashSet<MyPhysicsBody>();
        }

        private Vector3 m_lastLinearVelocity;
        private Vector3 m_lastAngularVelocity;

        #region Properties

        public int HavokCollisionSystemID = 0;
        private HkRigidBody m_rigidBody;
        public virtual HkRigidBody RigidBody
        {
            get { return WeldInfo.Parent != null ? WeldInfo.Parent.RigidBody : m_rigidBody; }
            protected set
            {
                if (m_rigidBody != value)
                {
                    if (m_rigidBody != null && !m_rigidBody.IsDisposed)
                    {
                        m_rigidBody.ContactSoundCallback -= MyPhysicsBody_ContactSoundCallback;
                        m_rigidBody.ContactPointCallback -= OnContactPointCallback;
                    }
                    m_rigidBody = value;
                    if (m_rigidBody != null)
                    {
                        m_rigidBody.ContactPointCallback += OnContactPointCallback;
                        m_rigidBody.ContactSoundCallback += MyPhysicsBody_ContactSoundCallback;
                    }
                }
            }
        }
        public virtual HkRigidBody RigidBody2 { get; protected set; }

        public delegate void PhysicsContactHandler(ref MyPhysics.MyContactPointEvent e);
        public event PhysicsContactHandler ContactPointCallback;

        protected HashSet<HkConstraint> m_constraints = new HashSet<HkConstraint>();
        protected HashSet<HkConstraint> m_constraintsAddBatch = new HashSet<HkConstraint>();
        HashSet<HkConstraint> m_constraintsRemoveBatch = new HashSet<HkConstraint>();

        /// <summary>
        /// Gets or sets the mass.
        /// </summary>
        /// <value>
        /// The mass.
        /// </value>
        public override float Mass
        {
            get
            {
                if (CharacterProxy != null)
                {
                    return CharacterProxy.Mass;
                }

                if (RigidBody != null)
                    return RigidBody.Mass;
                if (Ragdoll != null)
                {
                    return Ragdoll.Mass;
                }
                return 0;
            }
        }

        /// <summary>
        /// Gets or sets the speed.
        /// </summary>
        /// <value>
        /// The speed.
        /// </value>
        public override float Speed
        {
            get
            {
                return RigidBody.GetRigidBodyInfo().LinearVelocity.Length();
            }
        }


        public override float Friction
        {
            get
            {
                return RigidBody.Friction;
            }
            set
            {
                RigidBody.Friction = value;
            }
        }

        public override bool IsStatic
        {
            get
            {
                if(RigidBody != null)
                    return RigidBody.IsFixed;
                return false;
            }
        }

        public override bool IsKinematic
        {
            get
            {
                if (RigidBody != null)
                    return !RigidBody.IsFixed && RigidBody.IsFixedOrKeyframed;
                return false;
            }
        }

        protected override void CloseRigidBody()
        {
            if (RigidBody != null)
            {
                // RigidBody.CollisionShape.Dispose();
                // RigidBody.Dispose();
                //RigidBody.ContactPointCallbackEnabled = false;
                RigidBody.Dispose();
                RigidBody = null;
            }

            if (RigidBody2 != null)
            {
                // RigidBody.CollisionShape.Dispose();
                // RigidBody.Dispose();
                RigidBody2.Dispose();
                RigidBody2 = null;
            }

            if (BreakableBody != null)
            {
                BreakableBody.Dispose();
                BreakableBody = null;
            }
        }

        //public abstract void CreateFromCollisionObject(HkShape shape, Vector3 center, Matrix worldTransform, HkMassProperties massProperties = null, int collisionFilter = 15/*MyPhysics.DefaultCollisionLayer*/); //TODO!

        /// <summary>
        /// Must be set before creating rigid body
        /// </summary>
        public HkSolverDeactivation InitialSolverDeactivation = HkSolverDeactivation.Low;


        #endregion

        #region Fields

        //MyMotionState m_motionState;
        protected float m_angularDamping;
        protected float m_linearDamping;

        protected ulong ClusterObjectID = MyHavokCluster.CLUSTERED_OBJECT_ID_UNITIALIZED;

        protected HkWorld m_world;
        public HkWorld HavokWorld
        {
            get { return IsWelded ? WeldInfo.Parent.HavokWorld : m_world; }
        }

        #endregion

        #region Properties

        public MyCharacterProxy CharacterProxy { get; set; }

        /// This system group id will is used to set's this character rigid bodies collision filters        
        public int CharacterSystemGroupCollisionFilterID { get; private set; }
        /// This is character collision filter, use this to avoid collisions with character and character holding bodies
        public uint CharacterCollisionFilter { get; private set; }
        /// This System Collision ID is used for ragdoll in non-dead mode to avoid collision with character's rigid body
        public int RagdollSystemGroupCollisionFilterID { get; private set; }


        /// <summary>
        /// Gets or sets the linear velocity.
        /// </summary>
        /// <value>
        /// The linear velocity.
        /// </value>
        public override Vector3 LinearVelocity
        {
            get
            {
                if (!Enabled)
                    return Vector3.Zero;

                if (RigidBody != null)
                    return this.RigidBody.LinearVelocity;

                if (CharacterProxy != null)
                    return CharacterProxy.LinearVelocity;

                return Vector3.Zero;
            }
            set
            {
                Debug.Assert(!float.IsNaN(value.X));
                Debug.Assert(value.Length() < 1000000);

                if (RigidBody != null)
                {
                    this.RigidBody.LinearVelocity = value;
                }

                if (CharacterProxy != null)
                    CharacterProxy.LinearVelocity = value;

            }
        }

        /// <summary>
        /// Gets or sets the linear damping.
        /// </summary>
        /// <value>
        /// The linear damping.
        /// </value>
        public override float LinearDamping
        {
            get
            {
                return this.RigidBody.LinearDamping;
            }
            set
            {
                Debug.Assert(!float.IsNaN(value));
                m_linearDamping = value;
            }
        }

        /// <summary>
        /// Gets or sets the angular damping.
        /// </summary>
        /// <value>
        /// The angular damping.
        /// </value>
        public override float AngularDamping
        {
            get
            {
                return this.RigidBody.AngularDamping;
            }
            set
            {
                Debug.Assert(!float.IsNaN(value));
                m_angularDamping = value;
            }
        }

        /// <summary>
        /// Gets or sets the angular velocity.
        /// </summary>
        /// <value>
        /// The angular velocity.
        /// </value>
        public override Vector3 AngularVelocity
        {
            get
            {
                if (RigidBody != null)
                    return RigidBody.AngularVelocity;

                if (CharacterProxy != null)
                    return CharacterProxy.AngularVelocity;

                return Vector3.Zero;
            }
            set
            {
                Debug.Assert(!float.IsNaN(value.X));
                Debug.Assert(value.Length() < 1000000);
                if (RigidBody != null)
                {
                    this.RigidBody.AngularVelocity = value;
                }
                if (CharacterProxy != null)
                    CharacterProxy.AngularVelocity = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes a new instance of the <see cref="MyPhysicsBody"/> class.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public MyPhysicsBody(IMyEntity entity, RigidBodyFlag flags)
        {
            //Debug.Assert(entity != null);           
            this.m_enabled = false;
            this.Entity = entity;
            this.Flags = flags;
        }

        void MyPhysicsBody_ContactSoundCallback(ref HkContactPointEvent e)
        {
            ProfilerShort.Begin("PhysicsBody.ContacSoundCallback");
            byte val;
            if (e.EventType != HkContactPointEvent.Type.Manifold || MyAudioComponent.ContactSoundsPool.TryGetValue(Entity.EntityId, out val))
            {
                ProfilerShort.End();
                return;
            }
            PlayContactSound(e);
            ProfilerShort.End();
        }

        public override void Close()
        {
            MyPhysics.AssertThread();

            ProfilerShort.Begin("MyPhysicsBody::Close()");

            CloseRagdoll();

            base.Close();

            if (CharacterProxy != null)
            {
                CharacterProxy.Dispose();
                CharacterProxy = null;
            }

            ProfilerShort.End();
        }


        /// <summary>
        /// Applies external force to the physics object.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="force">The force.</param>
        /// <param name="position">The position.</param>
        /// <param name="torque">The torque.</param>
        public override void AddForce(MyPhysicsForceType type, Vector3? force, Vector3D? position, Vector3? torque)
        {
            force.AssertIsValid();
            position.AssertIsValid();
            torque.AssertIsValid();

            System.Diagnostics.Debug.Assert(IsInWorld == true || IsWelded);

            if (IsStatic)
                return;

            Matrix transform;

            switch (type)
            {
                case MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE:
                    {
                        if (RigidBody != null)
                        {
                            transform = RigidBody.GetRigidBodyMatrix();
                            AddForceTorqueBody(force, torque, RigidBody, ref transform);
                        }
                        if (CharacterProxy != null)
                        {
                            transform = Entity.WorldMatrix;
                            AddForceTorqueBody(force, torque, CharacterProxy.GetHitRigidBody(), ref transform);
                        }
                        if (Ragdoll != null && Ragdoll.IsAddedToWorld && !Ragdoll.IsKeyframed)
                        {
                            transform = Entity.WorldMatrix;
                            ApplyForceTorqueOnRagdoll(force, torque, Ragdoll, ref transform);
                        }
                    }
                    break;
                case MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE:
                    {
                        ApplyImplusesWorld(force, position, torque, RigidBody);

                        if (CharacterProxy != null && force.HasValue && position.HasValue)
                        {
                            CharacterProxy.ApplyLinearImpulse(force.Value);
                        }
                        if (Ragdoll != null && Ragdoll.IsAddedToWorld && !Ragdoll.IsKeyframed)
                        {
                            ApplyImpuseOnRagdoll(force, position, torque, Ragdoll);
                        }
                    }
                    break;
                case MyPhysicsForceType.APPLY_WORLD_FORCE:
                    {
                        ApplyForceWorld(force, position, RigidBody);

                        if (Ragdoll != null && Ragdoll.IsAddedToWorld && !Ragdoll.IsKeyframed)
                        {
                            ApplyForceOnRagdoll(force, position, Ragdoll);
                        }
                    }

                    break;
                default:
                    {
                        Debug.Fail("Unhandled enum!");
                    }
                    break;
            }
        }

        private void ApplyForceTorqueOnRagdoll(Vector3? force, Vector3? torque, HkRagdoll ragdoll, ref Matrix transform)
        {
            Debug.Assert(ragdoll != null, "Invalid parameter!");
            Debug.Assert(ragdoll.Mass != 0, "Ragdoll's mass can not be zero!");
            foreach (var rigidBody in ragdoll.RigidBodies)
            {
                if (rigidBody != null)
                {
                    Vector3 weightedForce = force.Value * rigidBody.Mass / ragdoll.Mass;
                    transform = rigidBody.GetRigidBodyMatrix();
                    AddForceTorqueBody(weightedForce, torque, rigidBody, ref transform);
                }
            }
        }

        private void ApplyImpuseOnRagdoll(Vector3? force, Vector3D? position, Vector3? torque, HkRagdoll ragdoll)
        {
            Debug.Assert(ragdoll != null, "Invalid parameter!");
            Debug.Assert(ragdoll.Mass != 0, "Ragdoll's mass can not be zero!");
            foreach (var rigidBody in ragdoll.RigidBodies)
            {
                Vector3 weightedForce = force.Value * rigidBody.Mass / ragdoll.Mass;
                ApplyImplusesWorld(weightedForce, position, torque, rigidBody);
            }
        }

        private void ApplyForceOnRagdoll(Vector3? force, Vector3D? position, HkRagdoll ragdoll)
        {
            Debug.Assert(ragdoll != null, "Invalid parameter!");
            Debug.Assert(ragdoll.Mass != 0, "Ragdoll's mass can not be zero!");
            foreach (var rigidBody in ragdoll.RigidBodies)
            {               
                Vector3 weightedForce = force.Value * rigidBody.Mass / ragdoll.Mass ;
                ApplyForceWorld(weightedForce, position, rigidBody);
            }
        }

        private void ApplyForceWorld(Vector3? force, Vector3D? position, HkRigidBody rigidBody)
        {
            if (rigidBody == null || force == null || MyUtils.IsZero(force.Value))
                return;

            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

            if (position.HasValue)
            {
                Vector3 point = position.Value - offset;
                rigidBody.ApplyForce(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, force.Value, point);
            }
            else
                rigidBody.ApplyForce(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, force.Value);
        }

        private void ApplyImplusesWorld(Vector3? force, Vector3D? position, Vector3? torque, HkRigidBody rigidBody)
        {
            if (rigidBody == null)
                return;

            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

            if (force.HasValue && position.HasValue)
                rigidBody.ApplyPointImpulse(force.Value, (Vector3)(position.Value - offset));

            if (torque.HasValue)
                rigidBody.ApplyAngularImpulse(torque.Value * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
        }

        private static void AddForceTorqueBody(Vector3? force, Vector3? torque, HkRigidBody rigidBody, ref Matrix transform)
        {
            Matrix tempM = transform;
            tempM.Translation = Vector3.Zero;

            if (force != null && !MyUtils.IsZero(force.Value))
            {
                Vector3 tmpForce = Vector3.Transform(force.Value, tempM);
                rigidBody.ApplyLinearImpulse(tmpForce * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
            }

            if (torque != null && !MyUtils.IsZero(torque.Value))
            {
                Vector3 tmpTorque = Vector3.Transform(torque.Value, tempM);
                rigidBody.ApplyAngularImpulse(tmpTorque * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
            }
        }

        /// <summary>
        /// Applies the impulse.
        /// </summary>
        /// <param name="impulse">The dir.</param>
        /// <param name="pos">The pos.</param>
        public override void ApplyImpulse(Vector3 impulse, Vector3D pos)
        {
            AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, impulse, pos, null);
        }

        /// <summary>
        /// Clears the speeds.
        /// </summary>
        public override void ClearSpeed()
        {
            //RigidBody.Clear();
            //RigidBody.LinearAcceleration = Vector3.Zero;
            if (RigidBody != null)
            {
                //  RigidBody.ClearForces();
                RigidBody.LinearVelocity = Vector3.Zero;
                RigidBody.AngularVelocity = Vector3.Zero;
            }

            if (CharacterProxy != null)
            {
                CharacterProxy.LinearVelocity = Vector3.Zero;
                CharacterProxy.AngularVelocity = Vector3.Zero;
                CharacterProxy.PosX = 0;
                CharacterProxy.PosY = 0;
                CharacterProxy.Elevate = 0;
            }

            //RigidBody.AngularAcceleration = Vector3.Zero;
        }

        /// <summary>
        /// Clear all dynamic values of physics object.
        /// </summary>
        public override void Clear()
        {
            ClearSpeed();
        }

        public override void DebugDraw()
        {
            const float alpha = 0.3f;

            //if (!Enabled)
            //    return;

            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

            if (RigidBody != null && BreakableBody != null)
            {
                Vector3D com = Vector3D.Transform((Vector3D)BreakableBody.BreakableShape.CoM, RigidBody.GetRigidBodyMatrix()) + offset;
                VRageRender.MyRenderProxy.DebugDrawSphere(RigidBody.CenterOfMassWorld + offset, 0.2f, Color.Wheat, 1, false);

                VRageRender.MyRenderProxy.DebugDrawAxis(Entity.PositionComp.WorldMatrix, 0.2f, false);
            }

            int index;
            if (RigidBody != null)
            {
                index = 0;
                Matrix rbMatrix = RigidBody.GetRigidBodyMatrix();
                MatrixD worldMatrix = MatrixD.CreateWorld(rbMatrix.Translation + offset, rbMatrix.Forward, rbMatrix.Up);

                MyPhysicsDebugDraw.DrawCollisionShape(RigidBody.GetShape(), worldMatrix, alpha, ref index);
            }

            if (RigidBody2 != null)
            {
                index = 0;
                Matrix rbMatrix = RigidBody2.GetRigidBodyMatrix();
                MatrixD worldMatrix = MatrixD.CreateWorld(rbMatrix.Translation + offset, rbMatrix.Forward, rbMatrix.Up);

                MyPhysicsDebugDraw.DrawCollisionShape(RigidBody2.GetShape(), worldMatrix, alpha, ref index);
            }

            if (CharacterProxy != null)
            {
                index = 0;
                //MatrixD characterTransform = MatrixD.CreateWorld(CharacterProxy.Position + offset, CharacterProxy.Forward, CharacterProxy.Up);
                Matrix rbMatrix = CharacterProxy.GetRigidBodyTransform();
                MatrixD worldMatrix = MatrixD.CreateWorld(rbMatrix.Translation + offset, rbMatrix.Forward, rbMatrix.Up);

                MyPhysicsDebugDraw.DrawCollisionShape(CharacterProxy.GetShape(), worldMatrix, alpha, ref index);
            }
        }

        public virtual void CreateFromCollisionObject(HkShape shape, Vector3 center, MatrixD worldTransform, HkMassProperties? massProperties = null, int collisionFilter = MyPhysics.DefaultCollisionLayer)
        {
            MyPhysics.AssertThread();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyPhysicsBody::CreateFromCollisionObject()");
            Debug.Assert(RigidBody == null, "Must be removed from scene and null");

            CloseRigidBody();

            Center = center;
            CanUpdateAccelerations = true;

            //m_motionState = new MyMotionState(worldTransform);

            CreateBody(ref shape, massProperties);

            RigidBody.UserObject = this;
            //RigidBody.SetWorldMatrix(worldTransform);
            RigidBody.Layer = collisionFilter;

            if ((int)(Flags & RigidBodyFlag.RBF_DISABLE_COLLISION_RESPONSE) > 0)
            {
                RigidBody.Layer = MyPhysics.NoCollisionLayer;
            }

            if ((int)(Flags & RigidBodyFlag.RBF_DOUBLED_KINEMATIC) > 0 && MyFakes.ENABLE_DOUBLED_KINEMATIC)
            {
                HkRigidBodyCinfo rbInfo2 = new HkRigidBodyCinfo();
                rbInfo2.AngularDamping = m_angularDamping;
                rbInfo2.LinearDamping = m_linearDamping;
                rbInfo2.Shape = shape;

                rbInfo2.MotionType = HkMotionType.Keyframed;
                rbInfo2.QualityType = HkCollidableQualityType.Keyframed;

                RigidBody2 = new HkRigidBody(rbInfo2);
                RigidBody2.UserObject = this;
                RigidBody2.SetWorldMatrix(worldTransform);
            }

            //m_motionState.Body = this;

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        protected virtual void CreateBody(ref HkShape shape, HkMassProperties? massProperties)
        {
            ProfilerShort.Begin("CreateBody");
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

            RigidBody = new HkRigidBody(rbInfo);

            ProfilerShort.End();
        }

        private List<HkdBreakableBodyInfo> m_tmpLst = new List<HkdBreakableBodyInfo>();

        public virtual void FracturedBody_AfterReplaceBody(ref HkdReplaceBodyEvent e)
        {
            System.Diagnostics.Debug.Assert(Sync.IsServer, "Client must not simulate destructions");
            if (!Sandbox.Game.Multiplayer.Sync.IsServer)
                return;

            ProfilerShort.Begin("DestructionFracture.AfterReplaceBody");
            Debug.Assert(BreakableBody != null);
            e.GetNewBodies(m_tmpLst);
            if (m_tmpLst.Count == 0)// || e.OldBody != DestructionBody)
                return;

            MyPhysics.RemoveDestructions(RigidBody);
            foreach (var b in m_tmpLst)
            {
                var bBody = MyFracturedPiecesManager.Static.GetBreakableBody(b);
                MatrixD m = bBody.GetRigidBody().GetRigidBodyMatrix();
                m.Translation = ClusterToWorld(m.Translation);
                var piece = MyDestructionHelper.CreateFracturePiece(bBody, ref m, (Entity as MyFracturedPiece).OriginalBlocks);
                if (piece == null)
                {
                    MyFracturedPiecesManager.Static.ReturnToPool(bBody);
                    continue;
                }
            }
            m_tmpLst.Clear();

            BreakableBody.AfterReplaceBody -= FracturedBody_AfterReplaceBody;

            MyFracturedPiecesManager.Static.RemoveFracturePiece(Entity as MyFracturedPiece, 0);

            ProfilerShort.End();
        }

        protected static void GetInfoFromFlags(HkRigidBodyCinfo rbInfo, RigidBodyFlag flags)
        {
            if ((int)(flags & RigidBodyFlag.RBF_STATIC) > 0)
            {
                rbInfo.MotionType = HkMotionType.Fixed;
                rbInfo.QualityType = HkCollidableQualityType.Fixed;
            }
            else if ((int)(flags & RigidBodyFlag.RBF_BULLET) > 0)
            {
                rbInfo.MotionType = HkMotionType.Dynamic;
                rbInfo.QualityType = HkCollidableQualityType.Bullet;
            }
            else if ((int)(flags & RigidBodyFlag.RBF_KINEMATIC) > 0)
            {
                rbInfo.MotionType = HkMotionType.Keyframed;
                rbInfo.QualityType = HkCollidableQualityType.Keyframed;
            }
            else if ((int)(flags & RigidBodyFlag.RBF_DOUBLED_KINEMATIC) > 0)
            {
                rbInfo.MotionType = HkMotionType.Dynamic;
                rbInfo.QualityType = HkCollidableQualityType.Moving;
            }
            else if ((int)(flags & RigidBodyFlag.RBF_DISABLE_COLLISION_RESPONSE) > 0)
            {
                rbInfo.MotionType = HkMotionType.Fixed;
                rbInfo.QualityType = HkCollidableQualityType.Fixed;
                //rbInfo.CollisionResponse = HkResponseType.None;
            }
            else if ((int)(flags & RigidBodyFlag.RBF_DEBRIS) > 0)
            {
                rbInfo.MotionType = HkMotionType.Dynamic;
                rbInfo.QualityType = HkCollidableQualityType.Debris;
            }
            else if ((int)(flags & RigidBodyFlag.RBF_KEYFRAMED_REPORTING) > 0)
            {
                rbInfo.MotionType = HkMotionType.Keyframed;
                rbInfo.QualityType = HkCollidableQualityType.KeyframedReporting;
            }
            else
            {
                rbInfo.MotionType = HkMotionType.Dynamic;
                rbInfo.QualityType = HkCollidableQualityType.Moving;
            }
        }

        public void PlayContactSound(HkContactPointEvent value, float volume = 0)
        {
            ProfilerShort.Begin("PlayContactSound");
            var bodyA = value.Base.BodyA.GetBody();
            var bodyB = value.Base.BodyB.GetBody();
            if (bodyA == null || bodyB == null)
            {
                ProfilerShort.End();
                return;
            }

            var colision = value.Base;
            Func<bool> canHear = () =>
            {
                if (MySession.ControlledEntity != null)
                {
                    var entity = MySession.ControlledEntity.Entity.GetTopMostParent();
                    return (entity == value.GetPhysicsBody(0).Entity || entity == value.GetPhysicsBody(1).Entity);
                }
                return false;
            };

            Func<bool> shouldPlay2D = () => MySession.ControlledEntity != null && MySession.ControlledEntity.Entity is MyCharacter && (
                            MySession.ControlledEntity.Entity.Components == value.GetPhysicsBody(0).Entity || MySession.ControlledEntity.Entity.Components == value.GetPhysicsBody(1).Entity);

            if (volume == 0)
            {
                var vel = value.Base.BodyA.LinearVelocity - value.Base.BodyB.LinearVelocity;
                //if (System.Math.Abs(Vector3.Normalize(vel).Dot(value.ContactPoint.Normal)) < 0.7f)\
                var val = System.Math.Abs(Vector3.Normalize(vel).Dot(value.ContactPoint.Normal)) * vel.Length();
                //var mass = value.Base.BodyA.Mass;
                //var massB = value.Base.BodyB.Mass;
                //mass = mass == 0 ? massB : massB == 0 ? mass : mass < massB ? mass : massB; // select smaller mass > 0
                //mass /= 40; //reference mass
                //val *= mass;
                if (val < 10)
                    volume = val / 10;
                else
                    volume = 1;
            }

            var worldPos = ClusterToWorld(value.ContactPoint.Position);
            var materialA = bodyA.GetMaterialAt(worldPos + value.ContactPoint.Normal * 0.1f);
            var materialB = bodyB.GetMaterialAt(worldPos - value.ContactPoint.Normal * 0.1f);


            MyAudioComponent.PlayContactSound(Entity.EntityId, worldPos, materialA, materialB, volume, canHear);

            ProfilerShort.End();
        }


        public override void CreateCharacterCollision(Vector3 center, float characterWidth, float characterHeight,
            float crouchHeight, float ladderHeight, float headSize, float headHeight,
            MatrixD worldTransform, float mass, ushort collisionLayer, bool isOnlyVertical, float maxSlope, bool networkProxy)
        {
            Center = center;
            CanUpdateAccelerations = false;

            if (networkProxy)
            { //create kinematic body for network proxy
                HkShape shape = MyCharacterProxy.CreateCharacterShape(characterHeight, characterWidth, characterHeight + headHeight, headSize, 0.0f);
                HkMassProperties massProperties = new HkMassProperties();
                massProperties.Mass = mass;
                massProperties.InertiaTensor = Matrix.Identity;
                massProperties.Volume = characterWidth * characterWidth * (characterHeight + 2 * characterWidth);
                CreateFromCollisionObject(shape, center, worldTransform, massProperties, collisionLayer);
                CanUpdateAccelerations = false;
                return;
            }


            Vector3 transformedCenter = Vector3.TransformNormal(Center, worldTransform);
            Matrix worldTransformCentered = Matrix.CreateWorld(transformedCenter + worldTransform.Translation, worldTransform.Forward, worldTransform.Up);


            CharacterProxy = new MyCharacterProxy(
#if DYNAMIC_CHARACTER_CONTROLLER
true,
#else
false,
#endif
 true, characterWidth, characterHeight,
        crouchHeight, ladderHeight, headSize, headHeight,
 worldTransformCentered.Translation,
                worldTransform.Up, worldTransform.Forward, mass,
                this,
                isOnlyVertical,
                maxSlope);

            CharacterProxy.GetRigidBody().ContactSoundCallback += MyPhysicsBody_ContactSoundCallback;
            CharacterProxy.GetRigidBody().ContactSoundCallbackEnabled = true;
            CharacterProxy.GetRigidBody().ContactPointCallbackDelay = 0;
            //CharacterProxy.Gravity = new Vector3(0, -20, 0);


        }

        protected virtual void ActivateCollision() { }

        /// <summary>
        /// Deactivates this rigid body in physics.
        /// </summary>
        public override void Deactivate()
        {
            if (ClusterObjectID != MyClusterTree.CLUSTERED_OBJECT_ID_UNITIALIZED)
            {
                MyPhysics.Clusters.RemoveObject(ClusterObjectID);
                ClusterObjectID = MyHavokCluster.CLUSTERED_OBJECT_ID_UNITIALIZED;
            }
        }

        public virtual void Deactivate(object world)
        {
            System.Diagnostics.Debug.Assert(world == m_world, "Inconsistency in clusters!");

            if (IsRagdollModeActive)
            {
                ReactivateRagdoll = true;
                CloseRagdollMode();
            }

            if (BreakableBody != null && m_world.DestructionWorld != null)
            {
                m_world.DestructionWorld.RemoveBreakableBody(BreakableBody);
            }
            else if (RigidBody != null)
            {
                m_world.RemoveRigidBody(RigidBody);
            }
            if (RigidBody2 != null)
            {
                m_world.RemoveRigidBody(RigidBody2);
            }
            if (CharacterProxy != null)
            {
                CharacterProxy.Deactivate(m_world);
            }

            foreach (var constraint in m_constraints)
            {
                m_world.RemoveConstraint(constraint);
            }

            m_world = null;
            IsInWorld = false;
        }

        public virtual void DeactivateBatch(object world)
        {
            System.Diagnostics.Debug.Assert(world == m_world, "Inconsistency in clusters!");

            if (IsRagdollModeActive)
            {
                ReactivateRagdoll = true;
                CloseRagdollMode();
            }

            if (BreakableBody != null)
            {
                m_world.DestructionWorld.RemoveBreakableBody(BreakableBody);
            }
            else if (RigidBody != null)
            {
                m_world.RemoveRigidBodyBatch(RigidBody);
            }
            if (RigidBody2 != null)
            {
                m_world.RemoveRigidBodyBatch(RigidBody2);
            }
            if (CharacterProxy != null)
            {
                CharacterProxy.Deactivate(m_world);
            }

            foreach (var constraint in m_constraints)
            {
                m_constraintsRemoveBatch.Add(constraint);
            }

            m_world = null;
            IsInWorld = false;
        }

        public void FinishAddBatch()
        {
            foreach (var constraint in m_constraintsAddBatch)
            {
                m_world.AddConstraint(constraint);
                constraint.OnAddedToWorld();
            }
            m_constraintsAddBatch.Clear();

            if (ReactivateRagdoll)
            {
                ActivateRagdoll();
                ReactivateRagdoll = false;
            }

        }

        public void FinishRemoveBatch(object userData)
        {
            HkWorld world = (HkWorld)userData;

            foreach (var constraint in m_constraintsRemoveBatch)
            {
                if (constraint.InWorld)
                {
                    //System.Diagnostics.Debug.Assert(world.RigidBodies.Contains(constraint.RigidBodyA), "Object was removed prior to constraint");
                    //System.Diagnostics.Debug.Assert(world.RigidBodies.Contains(constraint.RigidBodyB), "Object was removed prior to constraint");
                    constraint.OnRemovedFromWorld();
                    world.RemoveConstraint(constraint);
                }
            }

            if (IsRagdollModeActive)
            {
                ReactivateRagdoll = true;
                CloseRagdollMode();
            }

            m_constraintsRemoveBatch.Clear();
        }
        /// </summary>
        public override void ForceActivate()
        {
            if (IsInWorld)
            {
                if (RigidBody != null)
                {
                    RigidBody.ForceActivate();
                    m_world.ActiveRigidBodies.Add(RigidBody);
                }
            }
        }

        /// <summary>
        /// Returns true when linear velocity or angular velocity is non-zero.
        /// </summary>
        public bool IsMoving
        {
            get { return !Vector3.IsZero(LinearVelocity) || !Vector3.IsZero(AngularVelocity); }
        }

        public Vector3 Gravity
        {
            get
            {
                if (!Enabled)
                    return Vector3.Zero;

                if (RigidBody != null)
                    return this.RigidBody.Gravity;

                if (CharacterProxy != null)
                    return CharacterProxy.Gravity;

                return Vector3.Zero;
            }
        }
        #endregion

        /// <summary>
        /// Called when [motion].
        /// </summary>
        /// <param name="rbo">The rbo.</param>
        /// <param name="step">The step.</param>
        public virtual void OnMotion(HkRigidBody rbo, float step)
        {
            if (Entity == null)
                return;

            if (this.Flags == RigidBodyFlag.RBF_DISABLE_COLLISION_RESPONSE)
            {
                return;
            }

            if (IsPhantom)
            {
                return;
            }

            ProfilerShort.Begin(RigidBody2 != null ? "Double kinematic" : "Normal");

            ProfilerShort.Begin("Update acceleration");
            if (CanUpdateAccelerations)
                UpdateAccelerations();
            ProfilerShort.End();

            if (RigidBody2 != null && rbo == RigidBody)
            {
                // To prevent disconnect movement between dynamic and kinematic
                // Setting motion to prevent body activation (we don't want to activate kinematic body)
                ProfilerShort.Begin("Set doubled body");
                RigidBody2.Motion.SetWorldMatrix(rbo.GetRigidBodyMatrix());
                ProfilerShort.End();
            }

            if (LinearVelocity.LengthSquared() > 0.000005f || AngularVelocity.LengthSquared() > 0.000005f)
            {
                ProfilerShort.Begin("GetWorldMatrix");
                var matrix = GetWorldMatrix();
                ProfilerShort.End();

                ProfilerShort.Begin("SetWorldMatrix");
                this.Entity.PositionComp.SetWorldMatrix(matrix, this);
                ProfilerShort.End();
            }

            ProfilerShort.Begin("UpdateCluster");
            if(WeldInfo.Parent == null)
                UpdateCluster();
            ProfilerShort.End();

            foreach (var weldedBody in WeldInfo.Children)
                weldedBody.OnMotion(rbo, step);

            ProfilerShort.End();
        }

        public override MatrixD GetWorldMatrix()
        {
            if (WeldInfo.Parent != null)
                return WeldInfo.Transform * WeldInfo.Parent.GetWorldMatrix();
            
            Vector3 transformedCenter;
            MatrixD entityMatrix = MatrixD.Identity;
            Matrix rbWorld;
            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

            if (RigidBody2 != null)
            {
                rbWorld = RigidBody2.GetRigidBodyMatrix();
                transformedCenter = Vector3.TransformNormal(Center, rbWorld);
                entityMatrix = MatrixD.CreateWorld(rbWorld.Translation - transformedCenter + offset, rbWorld.Forward, rbWorld.Up);
            }
            else if (RigidBody != null)
            {
                rbWorld = RigidBody.GetRigidBodyMatrix();
                transformedCenter = Vector3.TransformNormal(Center, rbWorld);
                entityMatrix = MatrixD.CreateWorld(rbWorld.Translation - transformedCenter + offset, rbWorld.Forward, rbWorld.Up);
            }
            else if (CharacterProxy != null)
            {
                MatrixD characterTransform = CharacterProxy.GetRigidBodyTransform();
                //MatrixD characterTransform = MatrixD.CreateWorld(CharacterProxy.Position, CharacterProxy.Forward, CharacterProxy.Up);

                transformedCenter = Vector3.TransformNormal(Center, characterTransform);
                characterTransform.Translation = CharacterProxy.Position - transformedCenter + offset;
                entityMatrix = characterTransform;
            }
            else if (Ragdoll != null & IsRagdollModeActive)
            {
                Ragdoll.UpdateWorldMatrixAfterSimulation();
                entityMatrix = Ragdoll.GetRagdollWorldMatrix();
                entityMatrix.Translation = entityMatrix.Translation + offset;
            }

            return entityMatrix;
        }

        public override Vector3 GetVelocityAtPoint(Vector3D worldPos)
        {
            Vector3 relPos = WorldToCluster(worldPos);
            if (RigidBody != null)
                return RigidBody.GetVelocityAtPoint(relPos);

            return Vector3.Zero;
        }

        #region Implementation of IMyNotifyEntityChanged

        /// <summary>
        /// Called when [world position changed].
        /// </summary>
        /// <param name="source">The source object that caused this event.</param>
        public override void OnWorldPositionChanged(object source)
        {
            if (IsInWorld == false)
                return;

            Debug.Assert(this != source, "Recursion!");

            var oldWorld = m_world;
            Vector3 velocity = Vector3.Zero;
            IMyEntity parentEntity = Entity.GetTopMostParent();
            if (parentEntity != null && parentEntity.Physics != null)
            {
                velocity = parentEntity.Physics.LinearVelocity;
            }
            MyPhysics.Clusters.MoveObject(ClusterObjectID, Entity.WorldAABB, Entity.WorldAABB, velocity);

            //if (m_motionState != null)
            //{
            //    m_motionState.FireOnOnMotion = false;
            //    m_motionState.WorldTransform = rigidBodyMatrix;
            //    m_motionState.FireOnOnMotion = true;
            //}
            Matrix rigidBodyMatrix = GetRigidBodyMatrix();

            if (RigidBody != null)
            {
                RigidBody.SetWorldMatrix(rigidBodyMatrix);
            }

            if (RigidBody2 != null)
            {
                RigidBody2.SetWorldMatrix(rigidBodyMatrix);
            }

            if (CharacterProxy != null)
            {
                CharacterProxy.Position = rigidBodyMatrix.Translation;
                CharacterProxy.Forward = rigidBodyMatrix.Forward;
                CharacterProxy.Up = rigidBodyMatrix.Up;
                CharacterProxy.Speed = 0;

                //if (CharacterProxy.ImmediateSetWorldTransform)
                {
                    CharacterProxy.SetRigidBodyTransform(rigidBodyMatrix);
                }
            }

            // TODO: This is disabled due to world synchronization, Ragdoll if set to some position from server doesn't simulate properly
            // Ragdoll updates it's position also in AfterUpdate on MyCharacter, so now this is not needed, but should be working.
            //if (Ragdoll != null && IsRagdollModeActive && m_ragdollDeadMode && !Sync.IsServer && MyFakes.ENABLE_RAGDOLL_CLIENT_SYNC)
            //{
            //    //Ragdoll.SetToKeyframed();
            //    //Ragdoll.SwitchToLayer(MyPhysics.RagdollCollisionLayer);
            //    Ragdoll.SetWorldMatrix(rigidBodyMatrix,true);
            //}
        }

        protected Matrix GetRigidBodyMatrix()
        {
            System.Diagnostics.Debug.Assert(ClusterObjectID != MyHavokCluster.CLUSTERED_OBJECT_ID_UNITIALIZED, "Unitialized object in cluster!");

            Vector3 transformedCenter = Vector3.TransformNormal(Center, Entity.WorldMatrix);

            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

            Matrix rigidBodyMatrix = Matrix.CreateWorld((Vector3)((Vector3D)transformedCenter + Entity.GetPosition() - (Vector3D)offset), Entity.WorldMatrix.Forward, Entity.WorldMatrix.Up);
            return rigidBodyMatrix;
        }

        protected Matrix GetRigidBodyMatrix(MatrixD worldMatrix)
        {
            System.Diagnostics.Debug.Assert(ClusterObjectID != MyHavokCluster.CLUSTERED_OBJECT_ID_UNITIALIZED, "Unitialized object in cluster!");

            Vector3 transformedCenter = Vector3.TransformNormal(Center, worldMatrix);

            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

            Matrix rigidBodyMatrix = Matrix.CreateWorld((Vector3)((Vector3D)transformedCenter + worldMatrix.Translation - (Vector3D)offset), worldMatrix.Forward, worldMatrix.Up);
            return rigidBodyMatrix;
        }

        public virtual void ChangeQualityType(HkCollidableQualityType quality)
        {
            RigidBody.Quality = quality;
        }

        #endregion

        #region Clusters

        public override bool HasRigidBody { get { return RigidBody != null; } }

        public override Vector3D CenterOfMassWorld
        {
            get
            {
                var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);
                return RigidBody.CenterOfMassWorld + offset;
            }
        }

        //Vector3 GetVelocityAtPoint(Vector3D worldPos)
        //{
        //    return LinearVelocity + AngularVelocity.Cross(worldPos - CenterOfMassWorld);
        //}

        void OnContactPointCallback(ref HkContactPointEvent e)
        {
            ProfilerShort.Begin("PhysicsBody.OnContacPointCallback");
            if (ContactPointCallback != null)
            {
                var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

                MyPhysics.MyContactPointEvent ce = new MyPhysics.MyContactPointEvent()
                {
                    ContactPointEvent = e,
                    Position = e.ContactPoint.Position + offset
                };

                ContactPointCallback(ref ce);
            }
            ProfilerShort.End();
        }

        public void AddConstraint(HkConstraint constraint)
        {
            Debug.Assert(!m_constraints.Contains(constraint), "Constraint added twice");

            Debug.Assert(m_world.RigidBodies.Contains(constraint.RigidBodyA), "Object must be in the world");
            Debug.Assert(m_world.RigidBodies.Contains(constraint.RigidBodyB), "Object must be in the world");
            Debug.Assert(constraint.RigidBodyA.IsAddedToWorld);
            Debug.Assert(constraint.RigidBodyB.IsAddedToWorld);
            m_constraints.Add(constraint);

            m_world.AddConstraint(constraint);

            constraint.OnAddedToWorld();
        }

        public void RemoveConstraint(HkConstraint constraint)
        {
            m_constraints.Remove(constraint);

            if (m_world != null)
            {
                constraint.OnRemovedFromWorld();
                m_world.RemoveConstraint(constraint);
            }
        }

        public HashSet<HkConstraint> Constraints
        {
            get { return m_constraints; }
        }

        private bool m_isStaticForCluster = false;
        public virtual bool IsStaticForCluster
        {
            get { return m_isStaticForCluster; }
            set { m_isStaticForCluster = value; }
        }

        public Vector3D WorldToCluster(Vector3D worldPos)
        {
            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);
            return (Vector3D)(worldPos - offset);
        }

        public Vector3D ClusterToWorld(Vector3 clusterPos)
        {
            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);
            return (Vector3D)clusterPos + (Vector3D)offset;
        }

        /// <summary>
        /// Activates this rigid body in physics.
        /// </summary>
        public override void Activate()
        {
            if (!Enabled)
                return;

            System.Diagnostics.Debug.Assert(!IsInWorld);

            ClusterObjectID = MyPhysics.Clusters.AddObject(Entity.WorldAABB, LinearVelocity, this, null);
        }

        public virtual void Activate(object world, ulong clusterObjectID)
        {
            System.Diagnostics.Debug.Assert(m_world == null, "Cannot activate already active object!");
            System.Diagnostics.Debug.Assert(!IsInWorld, "Cannot activate already active object!");

            m_world = (HkWorld)world;
            ClusterObjectID = clusterObjectID;

            ActivateCollision();

            IsInWorld = true;

            Matrix rigidBodyMatrix = GetRigidBodyMatrix();

            if (BreakableBody != null)
            {
                RigidBody.SetWorldMatrix(rigidBodyMatrix);

                //Disable destruction on clients
                if (Sync.IsServer)
                    m_world.DestructionWorld.AddBreakableBody(BreakableBody);
                else
                    m_world.AddRigidBody(RigidBody);
            }
            else if (RigidBody != null)
            {
                RigidBody.SetWorldMatrix(rigidBodyMatrix);
                m_world.AddRigidBody(RigidBody);
            }
            if (RigidBody2 != null)
            {
                RigidBody2.SetWorldMatrix(rigidBodyMatrix);
                m_world.AddRigidBody(RigidBody2);
            }

            if (CharacterProxy != null)
            {
                // obtain this character new system group id for collision filtering
                CharacterSystemGroupCollisionFilterID = m_world.GetCollisionFilter().GetNewSystemGroup();
                // Calculate filter info for this character
                CharacterCollisionFilter = HkGroupFilter.CalcFilterInfo(MyPhysics.CharacterCollisionLayer, CharacterSystemGroupCollisionFilterID, 0, 0);
                CharacterProxy.CharacterRigidBody.SetCollisionFilterInfo(CharacterCollisionFilter);


                CharacterProxy.SetRigidBodyTransform(rigidBodyMatrix);
                CharacterProxy.Activate(m_world);
            }

            if (ReactivateRagdoll)
            {
                ActivateRagdoll();
                ReactivateRagdoll = false;
            }

            if (SwitchToRagdollModeOnActivate)
            {
                SwitchToRagdollModeOnActivate = false;
                SwitchToRagdollMode(m_ragdollDeadMode);
            }

            foreach (var constraint in m_constraints)
            {
                m_world.AddConstraint(constraint);
            }
        }

        public virtual void ActivateBatch(object world, ulong clusterObjectID)
        {
            System.Diagnostics.Debug.Assert(m_world == null, "Cannot activate already active object!");

            m_world = (HkWorld)world;
            ClusterObjectID = clusterObjectID;
            IsInWorld = true;

            ActivateCollision();

            Matrix rigidBodyMatrix = GetRigidBodyMatrix();

            if (RigidBody != null)
            {
                RigidBody.SetWorldMatrix(rigidBodyMatrix);
                m_world.AddRigidBodyBatch(RigidBody);
            }
            if (RigidBody2 != null)
            {
                RigidBody2.SetWorldMatrix(rigidBodyMatrix);
                m_world.AddRigidBodyBatch(RigidBody2);
            }

            if (CharacterProxy != null)
            {
                CharacterProxy.SetRigidBodyTransform(rigidBodyMatrix);
                CharacterProxy.Activate(m_world);
            }

            foreach (var constraint in m_constraints)
            {
                m_constraintsAddBatch.Add(constraint);
            }

            if (ReactivateRagdoll)
            {
                ActivateRagdoll();
                ReactivateRagdoll = false;
            }

        }
        public void UpdateCluster()
        {
            if (!MyPerGameSettings.LimitedWorld)
                MyPhysics.Clusters.MoveObject(ClusterObjectID, Entity.WorldAABB, Entity.WorldAABB, Entity.GetTopMostParent().Physics.LinearVelocity);
        }

        void MyHavokCluster.IMyActivationHandler.Activate(object userData, ulong clusterObjectID)
        {
            Activate(userData, clusterObjectID);
        }

        void MyHavokCluster.IMyActivationHandler.Deactivate(object userData)
        {
            Deactivate(userData);
        }

        void MyHavokCluster.IMyActivationHandler.ActivateBatch(object userData, ulong clusterObjectID)
        {
            ActivateBatch(userData, clusterObjectID);
        }

        void MyHavokCluster.IMyActivationHandler.DeactivateBatch(object userData)
        {
            DeactivateBatch(userData);
        }

        void MyHavokCluster.IMyActivationHandler.FinishAddBatch()
        {
            FinishAddBatch();
        }

        void MyHavokCluster.IMyActivationHandler.FinishRemoveBatch(object userData)
        {
            FinishRemoveBatch(userData);
        }

        bool MyHavokCluster.IMyActivationHandler.IsStaticForCluster
        {
            get { return IsStaticForCluster; }
        }

        public void ReorderClusters()
        {
            MyPhysics.Clusters.ReorderClusters(Entity.PositionComp.WorldAABB, ClusterObjectID);
        }

        #endregion

        private HkdBreakableBody m_breakableBody;
        public HkdBreakableBody BreakableBody
        {
            get { return m_breakableBody; }
            set
            {
                m_breakableBody = value;
                RigidBody = value;
            }
        }
        public override void UpdateAccelerations()
        {
            Vector3 delta = LinearVelocity - m_lastLinearVelocity;
            m_lastLinearVelocity = LinearVelocity;
            LinearAcceleration = delta / MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            Vector3 deltaAng = AngularVelocity - m_lastAngularVelocity;
            m_lastAngularVelocity = AngularVelocity;
            AngularAcceleration = deltaAng / MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
        }

        #region Ragdoll

        /// <summary>
        /// Returns true when ragdoll is in world
        /// </summary>
        public bool IsRagdollModeActive
        {
            get
            {
                if (Ragdoll == null) return false;
                return Ragdoll.IsAddedToWorld;
            }
        }

        public HkRagdoll Ragdoll
        {
            get
            {
                return m_ragdoll;
            }
            set
            {
                m_ragdoll = value;
                if (m_ragdoll != null)
                {
                    m_ragdoll.AddedToWorld += OnRagdollAddedToWorld;
                }
            }
        }

        protected HkRagdoll m_ragdoll;
        private bool m_ragdollDeadMode;

        public event EventHandler OnRagdollActivated;

        public void CloseRagdoll()
        {
            if (Ragdoll != null)
            {
                if (IsRagdollModeActive)
                {
                    CloseRagdollMode();
                }
                if (Ragdoll.IsAddedToWorld)
                {
                    HavokWorld.RemoveRagdoll(Ragdoll);
                }
                Ragdoll.Dispose();
                Ragdoll = null;
            }
        }

        public void SwitchToRagdollMode(bool deadMode = true, int firstRagdollSubID = 1)
        {
            Debug.Assert(Enabled, "Trying to switch to ragdoll mode, but Physics are not enabled");           

            if (HavokWorld == null || !Enabled)
            {
                //Activate();
                //if (HavokWorld == null)
                //{
                //Debug.Fail("Can not switch to Ragdoll mode, HavokWorld is null");
                //    return; // This PhysicsBody don't have always HavokWorld?
                //}

                // Not Activated, we need to wait and switch to ragdoll on activate
                SwitchToRagdollModeOnActivate = true;
                m_ragdollDeadMode = deadMode;
                return;
            }

            if (IsRagdollModeActive)
            {
                Debug.Fail("Ragdoll mode is already active!");
                return;
            }

            //Matrix havokMatrix = GetWorldMatrix();
            //havokMatrix.Translation = WorldToCluster(havokMatrix.Translation);

            Matrix havokMatrix = Entity.WorldMatrix;
            havokMatrix.Translation = WorldToCluster(havokMatrix.Translation);

            if (RagdollSystemGroupCollisionFilterID == 0)
            {
                RagdollSystemGroupCollisionFilterID = m_world.GetCollisionFilter().GetNewSystemGroup();
            }

            Ragdoll.SetToKeyframed();   // this will disable the bodies to get the impulse when repositioned

            Ragdoll.GenerateRigidBodiesCollisionFilters(deadMode ? MyPhysics.CharacterCollisionLayer : MyPhysics.RagdollCollisionLayer, RagdollSystemGroupCollisionFilterID, firstRagdollSubID);

            Ragdoll.ResetToRigPose();

            Ragdoll.SetWorldMatrix(havokMatrix);

            Ragdoll.SetTransforms(havokMatrix, false);

            if (deadMode) Ragdoll.SetToDynamic();

            foreach (HkRigidBody body in Ragdoll.RigidBodies)
            {
                // set the velocities for the bodies
                if (deadMode)
                {
                    body.AngularVelocity = AngularVelocity;
                    body.LinearVelocity = LinearVelocity;
                }
                else
                {
                    body.AngularVelocity = Vector3.Zero;
                    body.LinearVelocity = Vector3.Zero;
                }
            }

            if (CharacterProxy != null && deadMode)
            {
                CharacterProxy.Deactivate(HavokWorld);
                CharacterProxy.Dispose();
                CharacterProxy = null;
            }

            if (RigidBody != null && deadMode)
            {
                RigidBody.Deactivate();
                HavokWorld.RemoveRigidBody(RigidBody);
                RigidBody.Dispose();
                RigidBody = null;
            }

            if (RigidBody2 != null && deadMode)
            {
                RigidBody2.Deactivate();
                HavokWorld.RemoveRigidBody(RigidBody2);
                RigidBody2.Dispose();
                RigidBody2 = null;
            }

            foreach (var body in Ragdoll.RigidBodies)
            {
                body.UserObject = deadMode ? this : null;

                // TODO: THIS SHOULD BE SET IN THE RAGDOLL MODEL AND NOT DEFINING IT FOR EVERY MODEL HERE
                body.Motion.SetDeactivationClass(deadMode ? HkSolverDeactivation.High : HkSolverDeactivation.Medium);// - TODO: Find another way - this is deprecated by Havok
                body.Quality = HkCollidableQualityType.Moving;

            }

            Ragdoll.OptimizeInertiasOfConstraintTree();

            if (!Ragdoll.IsAddedToWorld)
            {
                HavokWorld.AddRagdoll(Ragdoll);
            }

            Ragdoll.EnableConstraints();
            Ragdoll.Activate();
            m_ragdollDeadMode = deadMode;

        }

        private void ActivateRagdoll()
        {
            if (Ragdoll == null)
            {
                Debug.Fail("Can not switch to Ragdoll mode, ragdoll is null!");
                return;
            }
            if (HavokWorld == null)
            {
                Debug.Fail("Can not swtich to Ragdoll mode, HavokWorld is null!");
                return;
            }
            if (IsRagdollModeActive)
            {
                Debug.Fail("Can not switch to ragdoll mode, ragdoll is still active!");
                return;
            }

            Matrix world = Entity.WorldMatrix;
            world.Translation = WorldToCluster(world.Translation);
            Debug.Assert(world.IsValid() && world != Matrix.Zero, "Ragdoll world matrix is invalid!");

            //Ragdoll.ResetToRigPose();

            Ragdoll.SetWorldMatrix(world);
            Ragdoll.SetTransforms(world, false);

            //foreach (var body in Ragdoll.RigidBodies)
            //{
            //    body.UserObject = this;
            //}
            //Ragdoll.OptimizeInertiasOfConstraintTree();


            HavokWorld.AddRagdoll(Ragdoll);
        }

        private void OnRagdollAddedToWorld()
        {
            Debug.Assert(Ragdoll.IsAddedToWorld, "Ragdoll was not added to world!");
            Ragdoll.Activate();
            Ragdoll.EnableConstraints();
            if (OnRagdollActivated != null)
                OnRagdollActivated(this, null);
        }

        public void CloseRagdollMode()
        {
            if (IsRagdollModeActive)
            {

                foreach (var body in Ragdoll.RigidBodies)
                {
                    body.UserObject = null;
                }

                Debug.Assert(Ragdoll.IsAddedToWorld, "Can not remove ragdoll when it's not in the world");
                Ragdoll.Deactivate();
                HavokWorld.RemoveRagdoll(Ragdoll);
                Ragdoll.ResetToRigPose();
            }
        }

        /// <summary>
        ///  Sets default values for ragdoll bodies and constraints - useful if ragdoll model is not correct
        /// </summary>
        public void SetRagdollDefaults()
        {
            foreach (var body in Ragdoll.RigidBodies)
            {
                body.MaxLinearVelocity = 1000.0f;
                body.MaxAngularVelocity = 1000.0f;

                body.Motion.SetDeactivationClass(HkSolverDeactivation.Medium);// - TODO: Find another way - this is deprecated by Havok
                body.Quality = HkCollidableQualityType.Moving;
                body.Restitution = 0.1f;

                //if (!body.IsFixedOrKeyframed) body.Mass = 10f;
                if (body.Mass == 0.0f || Ragdoll.Mass > (Entity as MyCharacter).Definition.Mass)
                {
                    body.Mass = (Entity as MyCharacter).Definition.Mass / Ragdoll.RigidBodies.Count;
                }
                body.AngularDamping = 0.5f;
                body.LinearDamping = 0.1f;
                body.Friction = 1.1f;
            }

            foreach (var constraint in Ragdoll.Constraints)
            {
                if (constraint.ConstraintData is HkRagdollConstraintData)
                {
                    var constraintData = constraint.ConstraintData as HkRagdollConstraintData;
                    constraintData.MaximumLinearImpulse = 3.40282e28f;
                    constraintData.MaximumAngularImpulse = 3.40282e28f;
                }
                else if (constraint.ConstraintData is HkFixedConstraintData)
                {
                    var constraintData = constraint.ConstraintData as HkFixedConstraintData;
                    constraintData.MaximumLinearImpulse = 3.40282e28f;
                    constraintData.MaximumAngularImpulse = 3.40282e28f;
                }
                else if (constraint.ConstraintData is HkHingeConstraintData)
                {
                    var constraintData = constraint.ConstraintData as HkHingeConstraintData;
                    constraintData.MaximumAngularImpulse = 3.40282e28f;
                    constraintData.MaximumLinearImpulse = 3.40282e28f;
                }
                else if (constraint.ConstraintData is HkLimitedHingeConstraintData)
                {
                    var constraintData = constraint.ConstraintData as HkLimitedHingeConstraintData;
                    constraintData.MaximumAngularImpulse = 3.40282e28f;
                    constraintData.MaximumLinearImpulse = 3.40282e28f;
                }
            }
        }

        public bool ReactivateRagdoll { get; set; }


        public bool SwitchToRagdollModeOnActivate { get; set; }


        #endregion

        #region Welding

        public bool IsWelded { get { return WeldInfo.Parent != null; } }

        public readonly MyWeldInfo WeldInfo = new MyWeldInfo();
        public void Weld(MyPhysicsBody other, bool recreateShape = true)
        {
            if (other.WeldInfo.Parent == this) //already welded to this
                return;

            if(other.IsWelded && !IsWelded)
            {
                other.Weld(this);
                return;
            }

            if(IsWelded)
            {
                WeldInfo.Parent.Weld(other);
                return;
            }
            
            HkShape thisShape;
            if (WeldInfo.Children.Count == 0)
            {
                thisShape = RigidBody.GetShape();
                HkShape.SetUserData(thisShape, RigidBody);
                Entity.OnPhysicsChanged += WeldedEntity_OnPhysicsChanged;
                //Entity.OnClose += Entity_OnClose;
            }
            else
                thisShape = GetShape();

            other.Deactivate();

            var transform = other.RigidBody.GetRigidBodyMatrix() * Matrix.Invert(RigidBody.GetRigidBodyMatrix());
            other.WeldInfo.Transform = transform;
            other.WeldedRigidBody = other.RigidBody;
            other.RigidBody = RigidBody;
            other.WeldInfo.Parent = this;

            WeldInfo.Children.Add(other);

            if(recreateShape)
                RecreateWeldedShape(thisShape);

            other.Entity.OnPhysicsChanged += WeldedEntity_OnPhysicsChanged;
        }

        void Entity_OnClose(IMyEntity obj)
        {
            UnweldAll(true);
        }

        void WeldedEntity_OnPhysicsChanged(IMyEntity obj)
        {
            if(Entity == null || Entity.Physics == null)
                return;
            foreach(var child in WeldInfo.Children)
            {
                if(child.Entity == null) //Physics component was replaced
                {
                    child.WeldInfo.Parent = null;
                    WeldInfo.Children.Remove(child);
                    if(obj.Physics != null)
                        Weld(obj.Physics as MyPhysicsBody);
                    return;
                }
            }

            RecreateWeldedShape(GetShape());
        }

        public void RecreateWeldedShape()
        {
            //Debug.Assert(WeldInfo.Children.Count > 0);
            if (WeldInfo.Children.Count == 0)
                return;
            RecreateWeldedShape(GetShape());
        }

        private void RecreateWeldedShape(HkShape thisShape)
        {
            m_tmpShapeList.Add(thisShape);

            if (WeldInfo.Children.Count == 0)
            {
                RigidBody.SetShape(thisShape);
                if (RigidBody2 != null)
                    RigidBody2.SetShape(thisShape);
            }
            else
            {
                foreach (var child in WeldInfo.Children)
                {
                    var transformShape = new HkTransformShape(child.WeldedRigidBody.GetShape(), ref child.WeldInfo.Transform);
                    HkShape.SetUserData(transformShape, child.WeldedRigidBody);
                    m_tmpShapeList.Add(transformShape);
                }
                var list = new HkListShape(m_tmpShapeList.ToArray(), HkReferencePolicy.None);
                RigidBody.SetShape(list);
                if (RigidBody2 != null)
                    RigidBody2.SetShape(list);
                list.Base.RemoveReference();
                m_tmpShapeList.Clear();
            }
        }

        /// <summary>
        /// Gets shape of this physics body even if its welded with other
        /// </summary>
        /// <returns></returns>
        public virtual HkShape GetShape()
        {
            Debug.Assert(RigidBody != null);
            if (WeldedRigidBody != null)
                return WeldedRigidBody.GetShape();

            var cont = RigidBody.GetShape().GetContainer();
            while(cont.IsValid)
            {
                var shape = cont.GetShape(cont.CurrentShapeKey);
                if (RigidBody.GetGcRoot() == shape.UserData)
                    return shape;
                cont.Next();
            }
            return RigidBody.GetShape();
        }

        public void UnweldAll(bool insertInWorld)
        {
            while (WeldInfo.Children.Count > 1)
                Unweld(WeldInfo.Children.First(), insertInWorld, false);
            if (WeldInfo.Children.Count > 0)
                Unweld(WeldInfo.Children.First(), insertInWorld);
        }

        private List<HkShape> m_tmpShapeList = new List<HkShape>();
        public void Unweld(MyPhysicsBody other, bool insertToWorld = true, bool recreateShape = true)
        {
            Debug.Assert(other.IsWelded, "Invalid state");
            if(IsWelded)
            {
                WeldInfo.Parent.Unweld(other, insertToWorld, recreateShape);
                return;
            }
            other.Entity.OnPhysicsChanged -= WeldedEntity_OnPhysicsChanged;

            other.WeldInfo.Parent = null;
            Debug.Assert(WeldInfo.Children.Contains(other));
            WeldInfo.Children.Remove(other);
            var body = other.RigidBody;
            other.RigidBody = other.WeldedRigidBody;
            other.RigidBody.SetWorldMatrix(other.WeldInfo.Transform * body.GetRigidBodyMatrix());
            other.RigidBody.LinearVelocity = body.LinearVelocity;
            if(insertToWorld)
                other.Activate();

            if(WeldInfo.Children.Count == 0)
            {
                Entity.OnPhysicsChanged -= WeldedEntity_OnPhysicsChanged;
                Entity.OnClose -= Entity_OnClose;
            }
            if(RigidBody != null && recreateShape)
                RecreateWeldedShape(GetShape());
        }

        public void Unweld(bool insertInWorld = true)
        {
            Debug.Assert(WeldInfo.Parent != null);
            WeldInfo.Parent.Unweld(this, insertInWorld);
        }
        #endregion

        public HkRigidBody WeldedRigidBody { get; protected set; }

        
    }
}