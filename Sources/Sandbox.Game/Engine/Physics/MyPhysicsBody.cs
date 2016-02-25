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
using VRage.Game;

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
    using VRage.Game.Components;
    using VRage.Trace;
    using VRage.Game.Entity;
    using Sandbox.Game.EntityComponents;
    using VRage.Game.ObjectBuilders.ComponentSystem;
    using Sandbox.Game.EntityComponents.Systems;

    /// <summary>
    /// Abstract engine physics body object.
    /// </summary>
    [MyComponentBuilder(typeof(MyObjectBuilder_PhysicsBodyComponent))]
    public partial class MyPhysicsBody : MyPhysicsComponentBase, MyClusterTree.IMyActivationHandler
    {
        #region Fields
        static MyStringId m_startCue = MyStringId.GetOrCompute("Start");

        private Vector3 m_lastLinearVelocity;
        private Vector3 m_lastAngularVelocity;
        private int m_motionCounter = 0;
        //MyMotionState m_motionState;
        protected float m_angularDamping;
        protected float m_linearDamping;

        protected ulong ClusterObjectID = MyHavokCluster.CLUSTERED_OBJECT_ID_UNITIALIZED;

        public new MyPhysicsBodyComponentDefinition Definition { get; private set; }

        protected HkWorld m_world;
        public HkWorld HavokWorld
        {
            get { return IsWelded ? WeldInfo.Parent.m_world : m_world; }
        }

        #endregion

        #region Properties

        public virtual int HavokCollisionSystemID
        {
            get
            {
                return RigidBody != null ? HkGroupFilter.GetSystemGroupFromFilterInfo(RigidBody.GetCollisionFilterInfo()) : 0;
            }

            protected set
            {
                if (RigidBody != null)
                {
                    RigidBody.SetCollisionFilterInfo(HkGroupFilter.CalcFilterInfo(RigidBody.Layer, value, 1, 1));
                    //HavokWorld.RefreshCollisionFilterOnEntity(RigidBody);//not here, its not in world yet
                }

                if (RigidBody2 != null)
                {
                    RigidBody2.SetCollisionFilterInfo(HkGroupFilter.CalcFilterInfo(RigidBody2.Layer, value, 1, 1));
                    //HavokWorld.RefreshCollisionFilterOnEntity(RigidBody2);
                }
            }
        }
        private HkRigidBody m_rigidBody;
        public override HkRigidBody RigidBody
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
        public override HkRigidBody RigidBody2 { get; protected set; }

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
                if (RigidBody != null)
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
            if(IsWelded)
            {
                WeldInfo.Parent.Unweld(this, false);
            }
            if(WeldInfo.Children.Count != 0)
            {
                MyWeldingGroups.ReplaceParent(MyWeldingGroups.Static.GetGroup((MyEntity)Entity), (MyEntity)Entity, null);
            }

            Debug.Assert(WeldInfo.Children.Count == 0, "Closing weld parent!");
            Debug.Assert(IsWelded == false, "Closing welded physics");
            CheckRBNotInWorld();
            if (RigidBody != null)
            {
                // RigidBody.CollisionShape.Dispose();
                // RigidBody.Dispose();
                //RigidBody.ContactPointCallbackEnabled = false;
                if (!RigidBody.IsDisposed) //welded
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

            if(WeldedRigidBody != null)
            {
                WeldedRigidBody.Dispose();
                WeldedRigidBody = null;
            }
        }

        //public abstract void CreateFromCollisionObject(HkShape shape, Vector3 center, Matrix worldTransform, HkMassProperties massProperties = null, int collisionFilter = 15/*MyPhysics.CollisionLayers.DefaultCollisionLayer*/); //TODO!

        /// <summary>
        /// Must be set before creating rigid body
        /// </summary>
        public HkSolverDeactivation InitialSolverDeactivation = HkSolverDeactivation.Low;

        public MyCharacterProxy CharacterProxy { get; set; }

        /// This system group id will is used to set's this character rigid bodies collision filters        
        public int CharacterSystemGroupCollisionFilterID { get; private set; }
        /// This is character collision filter, use this to avoid collisions with character and character holding bodies
        public uint CharacterCollisionFilter { get; private set; }

        private bool m_isInWorld = false;

        public override bool IsInWorld
        {
            get
            {
                //if (WeldInfo.Parent != null)
                //    return WeldInfo.Parent.IsInWorld;
                return m_isInWorld;
            }
            protected set
            {
                m_isInWorld = value;
            }
        }

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

                if (Ragdoll != null && Ragdoll.IsActive)
                    return Ragdoll.GetRootRigidBody().LinearVelocity;

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

                if (Ragdoll != null && Ragdoll.IsActive)
                {
                    foreach (var body in Ragdoll.RigidBodies)
                    {
                        body.LinearVelocity = value;
                    }
                }
            }
        }

        public override void SetSpeeds(Vector3 linear, Vector3 angular)
        {
            LinearVelocity = linear;
            AngularVelocity = angular;
            ClearAccelerations();
            SetActualSpeedsAsPrevious();
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
                if (RigidBody != null)
                    this.RigidBody.LinearDamping = value;
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
                if(RigidBody != null)
                    this.RigidBody.AngularDamping = value;
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


                if (Ragdoll != null && Ragdoll.IsActive)
                    return Ragdoll.GetRootRigidBody().AngularVelocity;

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

                if (Ragdoll != null && Ragdoll.IsActive)
                {
                    foreach (var body in Ragdoll.RigidBodies)
                    {
                        body.AngularVelocity = value;
                    }
                }
            }
        }

        #endregion

        #region Methods

        // Parameterless constructor for component initializer.
        public MyPhysicsBody()
        {
        }

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
            //MyPhysics.AssertThread();

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

            //System.Diagnostics.Debug.Assert(IsInWorld == true || IsWelded);

            if (IsStatic)
                return;
            if (MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_FORCES)
                MyPhysicsDebugDraw.DebugDrawAddForce(this, type, force, position, torque);

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
                        if (Ragdoll != null && Ragdoll.InWorld && !Ragdoll.IsKeyframed)
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
                        if (Ragdoll != null && Ragdoll.InWorld && !Ragdoll.IsKeyframed)
                        {
                            ApplyImpuseOnRagdoll(force, position, torque, Ragdoll);
                        }
                    }
                    break;
                case MyPhysicsForceType.APPLY_WORLD_FORCE:
                    {
                        ApplyForceWorld(force, position, RigidBody);

                        if (Ragdoll != null && Ragdoll.InWorld && !Ragdoll.IsKeyframed)
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

        private void ApplyForceWorld(Vector3? force, Vector3D? position, HkRigidBody rigidBody)
        {
            if (rigidBody == null || force == null || MyUtils.IsZero(force.Value))
                return;

            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

            if (position.HasValue)
            {
                Vector3 point = position.Value - offset;
                rigidBody.ApplyForce(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, force.Value, point);
            }
            else
                rigidBody.ApplyForce(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, force.Value);
        }

        private void ApplyImplusesWorld(Vector3? force, Vector3D? position, Vector3? torque, HkRigidBody rigidBody)
        {
            if (rigidBody == null)
                return;

            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

            if (force.HasValue && position.HasValue)
                rigidBody.ApplyPointImpulse(force.Value, (Vector3)(position.Value - offset));

            if (torque.HasValue)
                rigidBody.ApplyAngularImpulse(torque.Value * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
        }

        private static void AddForceTorqueBody(Vector3? force, Vector3? torque, HkRigidBody rigidBody, ref Matrix transform)
        {
            Matrix tempM = transform;
            tempM.Translation = Vector3.Zero;

            if (force != null && !MyUtils.IsZero(force.Value))
            {
                Vector3 tmpForce = Vector3.Transform(force.Value, tempM);
                rigidBody.ApplyLinearImpulse(tmpForce * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
            }

            if (torque != null && !MyUtils.IsZero(torque.Value))
            {
                Vector3 tmpTorque = Vector3.Transform(torque.Value, tempM);
                rigidBody.ApplyAngularImpulse(tmpTorque * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
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

            if (MyDebugDrawSettings.DEBUG_DRAW_CONSTRAINTS)
            {
                int i = 0;
                foreach (var c in Constraints)
                {
                    if (c.IsDisposed)
                        continue;

                    Color col = Color.Green;

                    if (!IsConstraintValid(c))
                        col = Color.Red;

                    Vector3 pivotA, pivotB;
                    c.GetPivotsInWorld(out pivotA, out pivotB);
                    var pos = ClusterToWorld(pivotA);
                    MyRenderProxy.DebugDrawSphere(pos, 0.2f, col, 1, false);
                    MyRenderProxy.DebugDrawText3D(pos, i.ToString(), Color.White, 0.7f, true);
                    i++;
                    pos = ClusterToWorld(pivotB);
                    MyRenderProxy.DebugDrawSphere(pos, 0.2f, col, 1, false);
                }
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_INERTIA_TENSORS && RigidBody != null)
            {
                var com = ClusterToWorld(RigidBody.CenterOfMassWorld);
                VRageRender.MyRenderProxy.DebugDrawLine3D(com, com + RigidBody.AngularVelocity, Color.Blue, Color.Red, false);
                var invMass = 1/RigidBody.Mass;
                var diag = RigidBody.InertiaTensor.Scale;
                var betSq = (diag.X - diag.Y + diag.Z) * invMass * 6;
                var gamaSq = diag.X * invMass * 12 - betSq;
                var alpSq = diag.Z * invMass * 12 - betSq;

                var scale = 1.01f * 0.5f;
                var he = new Vector3(Math.Sqrt(alpSq), Math.Sqrt(betSq), Math.Sqrt(gamaSq)) * scale;

                MyOrientedBoundingBoxD obb = new MyOrientedBoundingBoxD(new BoundingBoxD(-he, he), MatrixD.Identity);
                obb.Transform(RigidBody.GetRigidBodyMatrix());
                obb.Center = CenterOfMassWorld;// RigidBody.GetBody().ClusterToWorld(RigidBody.CenterOfMassWorld);
                VRageRender.MyRenderProxy.DebugDrawOBB(obb, Color.Purple, 0.05f, false, false);
                //VRageRender.MyRenderProxy.DebugDrawText3D(obb.Center, Entity.ToString(), Color.Purple, 0.5f, false);
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_SHAPES && !IsWelded)
            {
                const float alpha = 0.3f;

                //if (!Enabled)
                //    return;

                var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

                if (RigidBody != null && BreakableBody != null)
                {
                    Vector3D com = Vector3D.Transform((Vector3D)BreakableBody.BreakableShape.CoM, RigidBody.GetRigidBodyMatrix()) + offset;
                    Color color = RigidBody.GetMotionType() != Havok.HkMotionType.Box_Inertia ? Color.Gray : RigidBody.IsActive ? Color.Red : Color.Blue;
                    VRageRender.MyRenderProxy.DebugDrawSphere(RigidBody.CenterOfMassWorld + offset, 0.2f, color, 1, false);

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
        }

        public virtual void CreateFromCollisionObject(HkShape shape, Vector3 center, MatrixD worldTransform, HkMassProperties? massProperties = null, int collisionFilter = MyPhysics.CollisionLayers.DefaultCollisionLayer)
        {
            //jn:TODO: is this safe? repro: destruction of cockpit -> character gets spawned
            //MyPhysics.AssertThread();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyPhysicsBody::CreateFromCollisionObject()");
            Debug.Assert(RigidBody == null, "Must be removed from scene and null");

            CloseRigidBody();

            Center = center;
            CanUpdateAccelerations = true;

            //m_motionState = new MyMotionState(worldTransform);

            CreateBody(ref shape, massProperties);

            RigidBody.UserObject = this;
            RigidBody.SetWorldMatrix(worldTransform);
            RigidBody.Layer = collisionFilter;

            if ((int)(Flags & RigidBodyFlag.RBF_DISABLE_COLLISION_RESPONSE) > 0)
            {
                RigidBody.Layer = MyPhysics.CollisionLayers.NoCollisionLayer;
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
            ProfilerShort.Begin("Lambdas");
            var colision = value.Base;
            Func<bool> canHear = () =>
            {
                if (MySession.Static.ControlledEntity != null)
                {
                    var entity = MySession.Static.ControlledEntity.Entity.GetTopMostParent();
                    return (entity == value.GetPhysicsBody(0).Entity || entity == value.GetPhysicsBody(1).Entity);
                }
                return false;
            };

            Func<bool> shouldPlay2D = () => MySession.Static.ControlledEntity != null && MySession.Static.ControlledEntity.Entity is MyCharacter && (
                            MySession.Static.ControlledEntity.Entity.Components == value.GetPhysicsBody(0).Entity || MySession.Static.ControlledEntity.Entity.Components == value.GetPhysicsBody(1).Entity);

            ProfilerShort.BeginNextBlock("Volume");
            if (volume == 0)
            {
                //var vel = value.Base.BodyA.LinearVelocity - value.Base.BodyB.LinearVelocity;
                //if (System.Math.Abs(Vector3.Normalize(vel).Dot(value.ContactPoint.Normal)) < 0.7f)\
                //var val = System.Math.Abs(Vector3.Normalize(vel).Dot(value.ContactPoint.Normal)) * vel.Length();
                //var mass = value.Base.BodyA.Mass;
                //var massB = value.Base.BodyB.Mass;
                //mass = mass == 0 ? massB : massB == 0 ? mass : mass < massB ? mass : massB; // select smaller mass > 0
                //mass /= 40; //reference mass
                //val *= mass;
                if (value.SeparatingVelocity < 10)
                    volume = value.SeparatingVelocity / 10;
                else
                    volume = 1;
            }

            ProfilerShort.BeginNextBlock("GetMaterial");
            var worldPos = ClusterToWorld(value.ContactPoint.Position);
            var materialA = bodyA.GetMaterialAt(worldPos + value.ContactPoint.Normal * 0.1f);
            var materialB = bodyB.GetMaterialAt(worldPos - value.ContactPoint.Normal * 0.1f);
            ProfilerShort.BeginNextBlock("PlaySound");
            MyAudioComponent.PlayContactSound(Entity.EntityId, m_startCue, worldPos, materialA, materialB, volume, canHear);
            ProfilerShort.End();
            ProfilerShort.End();
        }


        public override void CreateCharacterCollision(Vector3 center, float characterWidth, float characterHeight,
                                                    float crouchHeight, float ladderHeight, float headSize, float headHeight,
                                                    MatrixD worldTransform, float mass, ushort collisionLayer, bool isOnlyVertical, float maxSlope, float maxImpulse, float maxSpeedRelativeToShip, bool networkProxy,
                                                    float? maxForce = null)
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
                maxSlope,
                maxImpulse,
                maxSpeedRelativeToShip,
                maxForce);

            //Unreliable, using fall sounds only now (when hiting ground with feet)
            //CharacterProxy.GetRigidBody().ContactSoundCallback += MyPhysicsBody_ContactSoundCallback;
            //CharacterProxy.GetRigidBody().ContactSoundCallbackEnabled = true;
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
                UnweldAll(true);
                if (IsWelded)
                    Unweld(false);
                else
                {
                    MyPhysics.Clusters.RemoveObject(ClusterObjectID);
                    ClusterObjectID = MyHavokCluster.CLUSTERED_OBJECT_ID_UNITIALIZED;
                    CheckRBNotInWorld();
                }
            }
        }

        private void CheckRBNotInWorld()
        {
            if (RigidBody != null && RigidBody.InWorld)
            {
                Debug.Fail("RB in world after deactivation.");
                Sandbox.Engine.Networking.MyAnalyticsHelper.ReportActivityStart((MyEntity)Entity, "RigidBody in world after deactivation", "", "DevNote", "", false);
                RigidBody.RemoveFromWorld();
            }
            if (RigidBody2 != null && RigidBody2.InWorld)
            {
                Debug.Fail("RB in world after deactivation.");
                RigidBody2.RemoveFromWorld();
            }
        }

        public virtual void Deactivate(object world)
        {
            System.Diagnostics.Debug.Assert(world == m_world, "Inconsistency in clusters!");

            if (IsRagdollModeActive)
            {
                ReactivateRagdoll = true;
                CloseRagdollMode(world as HkWorld);
            }

            // MW: activate simulation island when a physics body is removed
            if (IsInWorld && RigidBody != null && !RigidBody.IsActive)
            {
                if (!RigidBody.IsFixed)
                {
                    RigidBody.Activate();
                }
                else
                {
                    BoundingBoxD worldBox = Entity.PositionComp.WorldAABB;
                    worldBox.Inflate(0.5f);
                    MyPhysics.ActivateInBox(ref worldBox);
                }
            }

            if (BreakableBody != null && m_world.DestructionWorld != null)
            {
                m_world.DestructionWorld.RemoveBreakableBody(BreakableBody);
            }
            else if (RigidBody != null && !RigidBody.IsDisposed)
            {
                m_world.RemoveRigidBody(RigidBody);                
            }
            if (RigidBody2 != null && !RigidBody2.IsDisposed)
            {
                m_world.RemoveRigidBody(RigidBody2);
            }
            if (CharacterProxy != null)
            {
                CharacterProxy.Deactivate(m_world);
            }
            
            foreach (var constraint in m_constraints)
            {
               if (constraint.IsDisposed) 
                   continue;

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
                CloseRagdollMode(world as HkWorld);
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
            ActivateCollision();

            foreach (var constraint in m_constraintsAddBatch)
            {
                if (IsConstraintValid(constraint))
                {
                    m_world.AddConstraint(constraint);
                    constraint.OnAddedToWorld();
                }
                else
                    Debug.Fail("Trying to add invalid constraint!");
            }
            m_constraintsAddBatch.Clear();

            if (ReactivateRagdoll)
            {
                ActivateRagdoll(GetRigidBodyMatrix());
                ReactivateRagdoll = false;
            }

        }

        public void FinishRemoveBatch(object userData)
        {
            HkWorld world = (HkWorld)userData;

            foreach (var constraint in m_constraintsRemoveBatch)
            {
                if (IsConstraintValid(constraint) && constraint.InWorld)
                {
                    //System.Diagnostics.Debug.Assert(world.RigidBodies.Contains(constraint.RigidBodyA), "Object was removed prior to constraint");
                    //System.Diagnostics.Debug.Assert(world.RigidBodies.Contains(constraint.RigidBodyB), "Object was removed prior to constraint");
                    constraint.OnRemovedFromWorld();
                    world.RemoveConstraint(constraint);
                }
                else
                    Debug.Fail("Trying to remove invalid constraint!");
            }

            if (IsRagdollModeActive)
            {
                ReactivateRagdoll = true;
                CloseRagdollMode(world);
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
        public override bool IsMoving
        {
            get { return !Vector3.IsZero(LinearVelocity) || !Vector3.IsZero(AngularVelocity); }
        }

        public override Vector3 Gravity
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
            if (rbo == RigidBody2)
                return;
            Debug.Assert(rbo == RigidBody);

            foreach(var child in WeldInfo.Children)
            {
                child.OnMotion(rbo, step);
            }

            if (Entity == null)
                return;

            if (Entity.Parent != null)//Parent should take care of moving children
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

            if (RigidBody2 != null)
            {
                // To prevent disconnect movement between dynamic and kinematic
                // Setting motion to prevent body activation (we don't want to activate kinematic body)
                ProfilerShort.Begin("Set doubled body");
                RigidBody2.Motion.SetWorldMatrix(rbo.GetRigidBodyMatrix());
                ProfilerShort.End();
            }
            const int MaxIgnoredMovements = 5;
            const float MinVelocitySq = 0.00000001f;
            m_motionCounter++;
            if (m_motionCounter > MaxIgnoredMovements ||
                LinearVelocity.LengthSquared() > MinVelocitySq || AngularVelocity.LengthSquared() > MinVelocitySq)
            {
                ProfilerShort.Begin("GetWorldMatrix");
                var matrix = GetWorldMatrix();
                ProfilerShort.End();

                ProfilerShort.Begin("SetWorldMatrix");
                this.Entity.PositionComp.SetWorldMatrix(matrix, this, true);
                ProfilerShort.End();
                m_motionCounter = 0;
            }

            ProfilerShort.Begin("UpdateCluster");
            if(WeldInfo.Parent == null)
                UpdateCluster();
            ProfilerShort.End();

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
                entityMatrix = Ragdoll.WorldMatrix;               
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
            //Debug.Assert(Entity.Parent == null || RigidBody.IsFixedOrKeyframed);

            Vector3 velocity = Vector3.Zero;
            IMyEntity parentEntity = Entity.GetTopMostParent();
            if (parentEntity != null && parentEntity.Physics != null)
            {
                velocity = parentEntity.Physics.LinearVelocity;
            }
            if(!IsWelded)
                MyPhysics.Clusters.MoveObject(ClusterObjectID, parentEntity.WorldAABB, parentEntity.WorldAABB, velocity);

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
            //    //Ragdoll.SwitchToLayer(MyPhysics.CollisionLayers.RagdollCollisionLayer);
            //    Ragdoll.SetWorldMatrix(rigidBodyMatrix,true);
            //}

            if (Ragdoll != null && IsRagdollModeActive && source is MyCockpit)
            {
                Debug.Assert(rigidBodyMatrix.IsValid() && rigidBodyMatrix != Matrix.Zero, "Ragdoll world matrix is invalid!");
                Ragdoll.ResetToRigPose();                
                Ragdoll.SetWorldMatrix(rigidBodyMatrix);
                Ragdoll.ResetVelocities();
            }
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

        public override bool HasRigidBody { get { return RigidBody != null; } }

        public override Vector3D CenterOfMassWorld
        {
            get
            {
                var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);
                return RigidBody.CenterOfMassWorld + offset;
            }
        }

        #region Clusters

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

        public static bool IsConstraintValid(HkConstraint constraint)
        {
            if (constraint == null) return false;
            if (constraint.IsDisposed) return false;
            if (constraint.RigidBodyA == null | constraint.RigidBodyB == null) return false;
            if (!constraint.RigidBodyA.InWorld | !constraint.RigidBodyB.InWorld) return false;
            return true;
        }
        public void AddConstraint(HkConstraint constraint)
        {
            if (IsWelded)
            {
                WeldInfo.Parent.AddConstraint(constraint);
                return;
            }
            if (HavokWorld == null || RigidBody == null)
                return;
            if (constraint.UserData == 0)
                constraint.UserData = (uint)(WeldedRigidBody == null ? RigidBody.GetGcRoot() : WeldedRigidBody.GetGcRoot());

            Debug.Assert(!m_constraints.Contains(constraint), "Constraint added twice");

            Debug.Assert(HavokWorld.RigidBodies.Contains(constraint.RigidBodyA), "Object must be in the world");
            Debug.Assert(HavokWorld.RigidBodies.Contains(constraint.RigidBodyB), "Object must be in the world");
            Debug.Assert(constraint.RigidBodyA.IsAddedToWorld);
            Debug.Assert(constraint.RigidBodyB.IsAddedToWorld);
            m_constraints.Add(constraint);

            HavokWorld.AddConstraint(constraint);

            if (constraint.InWorld)
                constraint.OnAddedToWorld();
            else
                Debug.Fail("Constraint not added!");
        }

        public void RemoveConstraint(HkConstraint constraint)
        {
            constraint.UserData = 0;

            if(IsWelded)
            {
                WeldInfo.Parent.RemoveConstraint(constraint);
                return;
            }

            m_constraints.Remove(constraint);

            if (HavokWorld != null)
            {
                constraint.OnRemovedFromWorld();
                HavokWorld.RemoveConstraint(constraint);
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

        public override Vector3D WorldToCluster(Vector3D worldPos)
        {
            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);
            return (Vector3D)(worldPos - offset);
        }

        public override Vector3D ClusterToWorld(Vector3 clusterPos)
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
            System.Diagnostics.Debug.Assert(!IsWelded, "Activating welded body!");

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
                CharacterCollisionFilter = HkGroupFilter.CalcFilterInfo(MyPhysics.CollisionLayers.CharacterCollisionLayer, CharacterSystemGroupCollisionFilterID, 0, 0);
                CharacterProxy.CharacterRigidBody.SetCollisionFilterInfo(CharacterCollisionFilter);


                CharacterProxy.SetRigidBodyTransform(rigidBodyMatrix);
                CharacterProxy.Activate(m_world);
            }

            //if (ReactivateRagdoll)
            //{
            //    if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("MyPhysicsBody.Activate.ReactivateRagdoll");
            //    ActivateRagdoll(rigidBodyMatrix);
            //    ReactivateRagdoll = false;
            //}

            if (SwitchToRagdollModeOnActivate)
            {
                if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("MyPhysicsBody.Activate.SwitchToRagdollModeOnActivate");
                SwitchToRagdollModeOnActivate = false;
                SwitchToRagdollMode(m_ragdollDeadMode);
            }

            m_world.LockCriticalOperations();
                      
            foreach (var constraint in m_constraints)
            {
                if (!IsConstraintValid(constraint)) 
                    continue;

                m_world.AddConstraint(constraint);               
            }

            m_world.UnlockCriticalOperations();
           
        }

        public virtual void ActivateBatch(object world, ulong clusterObjectID)
        {
            System.Diagnostics.Debug.Assert(m_world == null, "Cannot activate already active object!");
            System.Diagnostics.Debug.Assert(!IsWelded, "Activating welded body!");

            m_world = (HkWorld)world;
            ClusterObjectID = clusterObjectID;
            IsInWorld = true;


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
                if (!IsConstraintValid(constraint)) continue;
                m_constraintsAddBatch.Add(constraint);
            }

            if (ReactivateRagdoll)
            {
                if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("MyPhysicsBody.ActivateBatch.ReactivateRagdoll");
                ActivateRagdoll(rigidBodyMatrix);
                ReactivateRagdoll = false;
            }

        }
        public void UpdateCluster()
        {
            Debug.Assert(Entity != null && !Entity.Closed && Entity.GetTopMostParent().Physics != null);
            if (!MyPerGameSettings.LimitedWorld && Entity != null && !Entity.Closed)
            {
                var physics = Entity.GetTopMostParent().Physics;
                if(physics != null)
                    MyPhysics.Clusters.MoveObject(ClusterObjectID, Entity.WorldAABB, Entity.WorldAABB,
                        physics.LinearVelocity);
            }
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
            if (IsWelded)
                WeldInfo.Parent.ReorderClusters();
            else
                MyPhysics.Clusters.ReorderClusters(Entity.PositionComp.WorldAABB, ClusterObjectID);
        }

        #endregion

        public override void UpdateAccelerations()
        {
            Vector3 delta = LinearVelocity - m_lastLinearVelocity;
            m_lastLinearVelocity = LinearVelocity;
            LinearAcceleration = delta / VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            Vector3 deltaAng = AngularVelocity - m_lastAngularVelocity;
            m_lastAngularVelocity = AngularVelocity;
            AngularAcceleration = deltaAng / VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
        }

        public void ClearAccelerations()
        {
            LinearAcceleration = Vector3.Zero;
            AngularAcceleration = Vector3.Zero;
        }

        public void SetActualSpeedsAsPrevious()
        {
            // setting of previous speeds according to current one - elimination of acceleration that was caused by setting of speed when for example speed on server is different that speed on client
            m_lastLinearVelocity = LinearVelocity;
            m_lastAngularVelocity = AngularVelocity;
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

            var shape = RigidBody.GetShape();
            if (shape.ShapeType == HkShapeType.List)
            {
                var cont = RigidBody.GetShape().GetContainer();
                while (cont.IsValid)
                {
                    var shape2 = cont.GetShape(cont.CurrentShapeKey);
                    if (RigidBody.GetGcRoot() == shape2.UserData)
                        return shape2;
                    cont.Next();
                }
            }
            return shape;
        }

        private static HkMassProperties? GetMassPropertiesFromDefinition(MyPhysicsBodyComponentDefinition physicsBodyComponentDefinition, MyModelComponentDefinition modelComponentDefinition)
        {
            HkMassProperties? massProperties = null;

            switch (physicsBodyComponentDefinition.MassPropertiesComputation) 
            {
                case MyObjectBuilder_PhysicsComponentDefinitionBase.MyMassPropertiesComputationType.None:
                    break;
                case MyObjectBuilder_PhysicsComponentDefinitionBase.MyMassPropertiesComputationType.Box:
                    massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(
                        modelComponentDefinition.Size / 2, (MyPerGameSettings.Destruction ? MyDestructionHelper.MassToHavok(modelComponentDefinition.Mass) : modelComponentDefinition.Mass));
                    break;
                default:
                    Debug.Fail("Not implemented");
                    break;
            }

            return massProperties;
        }

        private void OnModelChanged(MyEntityContainerEventExtensions.EntityEventParams eventParams)
        {
            Close();
            InitializeRigidBodyFromModel();
        }

        public override void Init(MyComponentDefinitionBase definition)
        {
            base.Init(definition);

            Definition = definition as MyPhysicsBodyComponentDefinition;
            Debug.Assert(Definition != null);
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            if (Definition != null) 
            {
                InitializeRigidBodyFromModel();

                this.RegisterForEntityEvent(MyModelComponent.ModelChanged, OnModelChanged);
            }
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            if (Definition != null)
            {
                Enabled = true;
                if (Definition.ForceActivate)
                    ForceActivate();
            }
        }

        private void InitializeRigidBodyFromModel()
        {
            if (Definition != null && RigidBody == null && Definition.CreateFromCollisionObject && Container.Has<MyModelComponent>())
            {
                MyModelComponent modelComponent = Container.Get<MyModelComponent>();
                if (modelComponent.Definition != null && modelComponent.ModelCollision != null && modelComponent.ModelCollision.HavokCollisionShapes.Length >= 1)
                {
                    HkMassProperties? massProperties = GetMassPropertiesFromDefinition(Definition, modelComponent.Definition);
                    int collisionFilter = Definition.CollisionLayer != null ? MyPhysics.GetCollisionLayer(Definition.CollisionLayer) : MyPhysics.CollisionLayers.DefaultCollisionLayer;
                    CreateFromCollisionObject(modelComponent.ModelCollision.HavokCollisionShapes[0], Vector3.Zero, Entity.WorldMatrix, massProperties, collisionFilter: collisionFilter);
                }
            }
        }

        public override void UpdateFromSystem()
        {
            if (Definition != null && (Definition.UpdateFlags & MyObjectBuilder_PhysicsComponentDefinitionBase.MyUpdateFlags.Gravity) != 0
                && MyFakes.ENABLE_PLANETS && Entity != null && Entity.PositionComp != null && Enabled && RigidBody != null)
            {
                RigidBody.Gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(Entity.PositionComp.GetPosition());
            }
        }
    }
}