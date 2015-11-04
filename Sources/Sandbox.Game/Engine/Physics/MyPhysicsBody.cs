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
            public Matrix Transform = Matrix.Identity;
            public readonly HashSet<MyPhysicsBody> Children = new HashSet<MyPhysicsBody>();
            public HkMassElement MassElement;

            internal void UpdateMassProps(HkRigidBody rb)
            {
                var mp = new HkMassProperties();
                mp.InertiaTensor = rb.InertiaTensor;
                mp.Mass = rb.Mass;
                mp.CenterOfMass = rb.CenterOfMassLocal;
                MassElement = new HkMassElement();
                MassElement.Properties = mp;
                MassElement.Tranform = Transform;
                //MassElement.Tranform.Translation = Vector3.Transform(rb.CenterOfMassLocal, Transform);
            }

            internal void SetMassProps(HkMassProperties mp)
            {
                MassElement = new HkMassElement();
                MassElement.Properties = mp;
                MassElement.Tranform = Transform;
            }
        }

        private Vector3 m_lastLinearVelocity;
        private Vector3 m_lastAngularVelocity;
        private int m_motionCounter = 0;

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
            Debug.Assert(WeldInfo.Children.Count == 0, "Closing weld parent!");
            Debug.Assert(IsWelded == false, "Closing welded physics");
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

            System.Diagnostics.Debug.Assert(IsInWorld == true || IsWelded);

            if (IsStatic)
                return;
            if (MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_FORCES)
                DebugDrawAddForce(type, force, position, torque);

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

        private void DebugDrawAddForce(MyPhysicsForceType type, Vector3? force, Vector3D? position, Vector3? torque)
        {
            Matrix transform;

            const float scale = 0.1f; 
            switch (type)
            {
                case MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE:
                    {
                        if (RigidBody != null)
                        {
                            transform = RigidBody.GetRigidBodyMatrix();
                            Vector3D p = CenterOfMassWorld + LinearVelocity * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;// ClusterToWorld(transform.Translation);//ClusterToWorld(transform.Translation);

                            if(force.HasValue)
                            {
                                Vector3 f = Vector3.TransformNormal(force.Value, transform) * scale;
                                MyRenderProxy.DebugDrawArrow3D(p, p + f, Color.Blue, Color.Red, false);
                            }
                            if (torque.HasValue)
                            {
                                Vector3 f = Vector3.TransformNormal(torque.Value, transform) * scale;
                                MyRenderProxy.DebugDrawArrow3D(p, p + f, Color.Blue, Color.Purple, false);
                            }
                        }
                    }
                    break;
                case MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE:
                    {
                        Vector3D p = position.Value + LinearVelocity * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                        if (force.HasValue)
                        {
                            MyRenderProxy.DebugDrawArrow3D(p, p + force.Value * scale, Color.Blue, Color.Red, false);
                        }
                        if (torque.HasValue)
                        {
                            MyRenderProxy.DebugDrawArrow3D(p, p + torque.Value * scale, Color.Blue, Color.Purple, false);
                        }
                    }
                    break;
                case MyPhysicsForceType.APPLY_WORLD_FORCE:
                    {
                        if (position.HasValue)
                        {
                            Vector3D p = position.Value + LinearVelocity * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                            if (force.HasValue)
                            {
                                MyRenderProxy.DebugDrawArrow3D(p, p + force.Value * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * scale, Color.Blue, Color.Red, false);
                            }
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

            if (MyDebugDrawSettings.DEBUG_DRAW_CONSTRAINTS)
            {
                int i = 0;
                foreach (var c in Constraints)
                {
                    if (c.IsDisposed)
                        continue;
                    var pos = Vector3D.Transform((Vector3D)(c.ConstraintData as HkLimitedHingeConstraintData).BodyAPos, Entity.WorldMatrix);
                    MyRenderProxy.DebugDrawSphere(pos, 0.2f, Color.Red, 1, false);
                    MyRenderProxy.DebugDrawText3D(pos, i.ToString(), Color.White, 0.7f, true);
                    i++;
                    pos = ClusterToWorld(Vector3D.Transform((Vector3D)(c.ConstraintData as HkLimitedHingeConstraintData).BodyBPos, c.RigidBodyB.GetRigidBodyMatrix()));
                    MyRenderProxy.DebugDrawSphere(pos, 0.2f, Color.Red, 1, false);
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
        }

        public virtual void CreateFromCollisionObject(HkShape shape, Vector3 center, MatrixD worldTransform, HkMassProperties? massProperties = null, int collisionFilter = MyPhysics.DefaultCollisionLayer)
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
            ProfilerShort.Begin("Lambdas");
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

            ProfilerShort.BeginNextBlock("Volume");
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

            ProfilerShort.BeginNextBlock("GetMaterial");
            var worldPos = ClusterToWorld(value.ContactPoint.Position);
            var materialA = bodyA.GetMaterialAt(worldPos + value.ContactPoint.Normal * 0.1f);
            var materialB = bodyB.GetMaterialAt(worldPos - value.ContactPoint.Normal * 0.1f);
            ProfilerShort.BeginNextBlock("PlaySound");

            MyAudioComponent.PlayContactSound(Entity.EntityId, worldPos, materialA, materialB, volume, canHear);
            ProfilerShort.End();
            ProfilerShort.End();
        }


        public override void CreateCharacterCollision(Vector3 center, float characterWidth, float characterHeight,
            float crouchHeight, float ladderHeight, float headSize, float headHeight,
            MatrixD worldTransform, float mass, ushort collisionLayer, bool isOnlyVertical, float maxSlope, float maxImpulse, bool networkProxy)
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
                maxImpulse);

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

            // MW: activate simulation island when a physics body is removed
            if (IsInWorld && RigidBody != null && !RigidBody.IsActive)
                RigidBody.Activate();

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
            ActivateCollision();

            foreach (var constraint in m_constraintsAddBatch)
            {
                m_world.AddConstraint(constraint);
                constraint.OnAddedToWorld();
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
            if (rbo == RigidBody2)
                return;

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
                Debug.Assert(rbo == RigidBody);
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
                this.Entity.PositionComp.SetWorldMatrix(matrix, this);
                ProfilerShort.End();
                m_motionCounter = 0;
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

            var oldWorld = m_world;
            Vector3 velocity = Vector3.Zero;
            IMyEntity parentEntity = Entity.GetTopMostParent();
            if (parentEntity != null && parentEntity.Physics != null)
            {
                velocity = parentEntity.Physics.LinearVelocity;
            }
            if(!IsWelded)
                MyPhysics.Clusters.MoveObject(ClusterObjectID, parentEntity.WorldAABB, parentEntity.WorldAABB, velocity);

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

        private static bool IsConstraintValid(HkConstraint constraint)
        {
            if (constraint.IsDisposed) return false;
            if (constraint.RigidBodyA == null | constraint.RigidBodyB == null) return false;
            if (!constraint.RigidBodyA.InWorld | !constraint.RigidBodyB.InWorld) return false;
            return true;
        }
        public void AddConstraint(HkConstraint constraint)
        {
            if (constraint.UserData == 0)
                constraint.UserData = (uint) (WeldedRigidBody == null ? RigidBody.GetGcRoot() : WeldedRigidBody.GetGcRoot());

            if (IsWelded)
            {
                WeldInfo.Parent.AddConstraint(constraint);
                return;
            }
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
            constraint.UserData = 0;

            if(IsWelded)
            {
                WeldInfo.Parent.RemoveConstraint(constraint);
                return;
            }

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

            foreach (var constraint in m_constraints)
            {
                if (!IsConstraintValid(constraint)) continue;
                m_world.AddConstraint(constraint);
            }
        }

        public virtual void ActivateBatch(object world, ulong clusterObjectID)
        {
            System.Diagnostics.Debug.Assert(m_world == null, "Cannot activate already active object!");

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
            if (IsWelded)
                WeldInfo.Parent.ReorderClusters();
            else
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
                return Ragdoll.InWorld;
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

        public void CloseRagdoll()
        {
            if (Ragdoll != null)
            {
                if (IsRagdollModeActive)
                {
                    CloseRagdollMode();
                }
                if (Ragdoll.InWorld)
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
            if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("MyPhysicsBody.SwitchToRagdollMode");

            if (HavokWorld == null || !Enabled)
            {                
                SwitchToRagdollModeOnActivate = true;
                m_ragdollDeadMode = deadMode;
                return;
            }

            if (IsRagdollModeActive)
            {
                Debug.Fail("Ragdoll mode is already active!");
                return;
            }                     

            Matrix havokMatrix = Entity.WorldMatrix;
            havokMatrix.Translation = WorldToCluster(havokMatrix.Translation);
            Debug.Assert(havokMatrix.IsValid() && havokMatrix != Matrix.Zero, "Invalid world matrix!");

            if (RagdollSystemGroupCollisionFilterID == 0)
            {
                RagdollSystemGroupCollisionFilterID = m_world.GetCollisionFilter().GetNewSystemGroup();
            }

            Ragdoll.SetToKeyframed();   // this will disable the bodies to get the impulse when repositioned

            Ragdoll.GenerateRigidBodiesCollisionFilters(deadMode ? MyPhysics.CharacterCollisionLayer : MyPhysics.RagdollCollisionLayer, RagdollSystemGroupCollisionFilterID, firstRagdollSubID);

            Ragdoll.ResetToRigPose();

            Ragdoll.SetWorldMatrix(havokMatrix);
            
            if (deadMode) Ragdoll.SetToDynamic();

            if (deadMode)
            {
                foreach (HkRigidBody body in Ragdoll.RigidBodies)
                {
                    // set the velocities for the bodies
                    body.AngularVelocity = AngularVelocity;
                    body.LinearVelocity = LinearVelocity;
                }
            }
            else
            {
                Ragdoll.ResetVelocities();
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

            if (!Ragdoll.InWorld)
            {
                //Ragdoll.RecreateConstraints();
                HavokWorld.AddRagdoll(Ragdoll);
            }

            Ragdoll.EnableConstraints();
            Ragdoll.Activate();
            m_ragdollDeadMode = deadMode;                        
        }

        private void ActivateRagdoll(Matrix worldMatrix)
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("MyPhysicsBody.ActivateRagdoll");
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
            //Matrix world = Entity.WorldMatrix;
            //world.Translation = WorldToCluster(world.Translation);
            Debug.Assert(worldMatrix.IsValid() && worldMatrix != Matrix.Zero, "Ragdoll world matrix is invalid!");

            Ragdoll.SetWorldMatrix(worldMatrix);

            foreach (var body in Ragdoll.RigidBodies)
            {
                body.LinearVelocity = LinearVelocity;
                body.AngularVelocity = Vector3.Zero;
            }
                        
            // Because after cluster's reorder, the bodies can collide!            
            HavokWorld.AddRagdoll(Ragdoll);
            DisableRagdollBodiesCollisions();
        }

        private void OnRagdollAddedToWorld(HkRagdoll ragdoll)
        {
            Debug.Assert(Ragdoll.InWorld, "Ragdoll was not added to world!");
            if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("MyPhysicsBody.OnRagdollAddedToWorld");
            Ragdoll.Activate();
            Ragdoll.EnableConstraints();
            HkConstraintStabilizationUtil.StabilizeRagdollInertias(ragdoll, 1, 0);
        }

        public void CloseRagdollMode()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("MyPhysicsBody.CloseRagdollMode");
            if (IsRagdollModeActive)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("CloseRagdollMode");
                foreach (var body in Ragdoll.RigidBodies)
                {
                    body.UserObject = null;
                }

                Debug.Assert(Ragdoll.InWorld, "Can not remove ragdoll when it's not in the world");
                Ragdoll.Deactivate();
                HavokWorld.RemoveRagdoll(Ragdoll);
                Ragdoll.ResetToRigPose();
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("MyPhysicsBody.CloseRagdollMode Closed");
            }
        }

        /// <summary>
        ///  Sets default values for ragdoll bodies and constraints - useful if ragdoll model is not correct
        /// </summary>
        public void SetRagdollDefaults()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("MyPhysicsBody.SetRagdollDefaults");
            var wasKeyframed = Ragdoll.IsKeyframed;
            Ragdoll.SetToDynamic();
            
            // Compute total mass of the character and distribute it amongs ragdoll bodies
            var definedMass = (Entity as MyCharacter).Definition.Mass;       
            if (definedMass <= 1)
            {
                definedMass = 80;
            }

            float totalVolume = 0f;
            foreach (var body in Ragdoll.RigidBodies)
            {
                float bodyLength = 0;
                
                var shape = body.GetShape();

                Vector4 min, max;
                
                shape.GetLocalAABB(0.01f, out min, out max);

                bodyLength = (max - min).Length();

                totalVolume += bodyLength;
            }

            // correcting the total volume
            if (totalVolume <= 0)
            {
                totalVolume = 1;
            }

            // bodies default settings            
            foreach (var body in Ragdoll.RigidBodies)
            {
                body.MaxLinearVelocity = 1000.0f;
                body.MaxAngularVelocity = 1000.0f;

                body.Quality = HkCollidableQualityType.Moving;               
                               
                var shape = body.GetShape();

                Vector4 min, max;

                shape.GetLocalAABB(0.01f, out min, out max);

                float bodyLength = (max - min).Length();

                float computedMass = definedMass / totalVolume * bodyLength;

                body.Mass = MyPerGameSettings.Destruction ? MyDestructionHelper.MassToHavok(computedMass) : computedMass;

                float radius = shape.ConvexRadius;
                if (shape.ShapeType == HkShapeType.Capsule)
                {
                    HkCapsuleShape capsule = (HkCapsuleShape)shape;
                    HkMassProperties massProperties = HkInertiaTensorComputer.ComputeCapsuleVolumeMassProperties(capsule.VertexA, capsule.VertexB, radius, body.Mass);
                    body.InertiaTensor = massProperties.InertiaTensor;
                }
                else
                {
                    HkMassProperties massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(Vector3.One * bodyLength * 0.5f, body.Mass);
                    body.InertiaTensor = massProperties.InertiaTensor;
                }

                body.AngularDamping = 0.005f;
                body.LinearDamping = 0.05f;
                body.Friction = 6f;
                body.AllowedPenetrationDepth = 0.1f;
                body.Restitution = 0.05f;
            }

            Ragdoll.OptimizeInertiasOfConstraintTree();
            
            if (wasKeyframed)
            {
                Ragdoll.SetToKeyframed();
            }

            // Constraints default settings
            foreach (var constraint in Ragdoll.Constraints)
            {
                if (constraint.ConstraintData is HkRagdollConstraintData)
                {
                    var constraintData = constraint.ConstraintData as HkRagdollConstraintData;
                    constraintData.MaximumLinearImpulse = 3.40282e28f;
                    constraintData.MaximumAngularImpulse = 3.40282e28f;
                    constraintData.MaxFrictionTorque = MyPerGameSettings.Destruction ? MyDestructionHelper.MassToHavok(0.5f) : 3f;
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
                    constraintData.MaxFrictionTorque = MyPerGameSettings.Destruction ? MyDestructionHelper.MassToHavok(0.5f) : 3f;
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
            ProfilerShort.Begin("Weld");
            HkShape thisShape;
            bool firstWelded = WeldInfo.Children.Count == 0;
            if (firstWelded)
            {
                RemoveConstraints(RigidBody);
                WeldedRigidBody = RigidBody;
                thisShape = RigidBody.GetShape();
                if (HavokWorld != null) 
                    HavokWorld.RemoveRigidBody(RigidBody);
                RigidBody = HkRigidBody.Clone(RigidBody);
                if (HavokWorld != null)
                    HavokWorld.AddRigidBody(RigidBody);
                HkShape.SetUserData(thisShape, RigidBody);
                Entity.OnPhysicsChanged += WeldedEntity_OnPhysicsChanged;
                WeldInfo.UpdateMassProps(RigidBody);
                //Entity.OnClose += Entity_OnClose;
            }
            else
                thisShape = GetShape();

            other.Deactivate();

            other.RemoveConstraints(other.RigidBody);//jn:TODO check if this is OK

            var transform = other.RigidBody.GetRigidBodyMatrix() * Matrix.Invert(RigidBody.GetRigidBodyMatrix());
            other.WeldInfo.Transform = transform;
            other.WeldInfo.UpdateMassProps(other.RigidBody);
            Debug.Assert(other.WeldedRigidBody == null);
            other.WeldedRigidBody = other.RigidBody;
            other.RigidBody = RigidBody;
            other.WeldInfo.Parent = this;
            other.ClusterObjectID = ClusterObjectID;
            WeldInfo.Children.Add(other);

            //if(recreateShape)
            //    RecreateWeldedShape(thisShape);

            ProfilerShort.BeginNextBlock("OnPhysicsChanged");
            //(other.Entity as MyEntity).RaisePhysicsChanged();
            //other.Entity.OnPhysicsChanged += WeldedEntity_OnPhysicsChanged;
            //Debug.Assert(other.m_constraints.Count == 0, "Constraints left in welded body");
            ProfilerShort.BeginNextBlock("RemoveConstraints");
            ProfilerShort.End();
            OnWelded(other);
            other.OnWelded(this);
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
                    break;
                }
            }
            //this breaks welded MP computation since bodys inertia tensor is diagonalized
            //(obj as MyEntity).Physics.WeldInfo.UpdateMassProps((obj as MyEntity).Physics.WeldedRigidBody);

            RecreateWeldedShape(GetShape());
        }

        public void RecreateWeldedShape()
        {
            //Debug.Assert(WeldInfo.Children.Count > 0);
            if (WeldInfo.Children.Count == 0)
                return;
            RecreateWeldedShape(GetShape());
        }

        private List<HkMassElement> m_tmpElements = new List<HkMassElement>();

        public void UpdateMassProps()
        {
            Debug.Assert(m_tmpElements.Count == 0, "mass elements not cleared!");
            if (RigidBody.IsFixedOrKeyframed)
                return;
            if(WeldInfo.Parent !=  null)
            {
                WeldInfo.Parent.UpdateMassProps();
                return;
            }
            m_tmpElements.Add(WeldInfo.MassElement);
            foreach (var child in WeldInfo.Children)
            {
                m_tmpElements.Add(child.WeldInfo.MassElement);
            }
            var mp = HkInertiaTensorComputer.CombineMassProperties(m_tmpElements);
            RigidBody.SetMassProperties(ref mp);
            m_tmpElements.Clear();
        }

        private void RecreateWeldedShape(HkShape thisShape)
        {
            ProfilerShort.Begin("RecreateWeldedShape");
            //me.Tranform.Translation = Entity.PositionComp.LocalAABB.Center;

            if (WeldInfo.Children.Count == 0)
            {
                RigidBody.SetShape(thisShape);
                if (RigidBody2 != null)
                    RigidBody2.SetShape(thisShape);
            }
            else
            {
                ProfilerShort.Begin("Create shapes");
                //m_tmpElements.Add(WeldInfo.MassElement);
                m_tmpShapeList.Add(thisShape);
                foreach (var child in WeldInfo.Children)
                {
                    var transformShape = new HkTransformShape(child.WeldedRigidBody.GetShape(), ref child.WeldInfo.Transform);
                    HkShape.SetUserData(transformShape, child.WeldedRigidBody);
                    m_tmpShapeList.Add(transformShape);
                    //m_tmpElements.Add(child.WeldInfo.MassElement);
                }
                //var list = new HkListShape(m_tmpShapeList.ToArray(), HkReferencePolicy.None);
                var list = new HkSmartListShape(0);
                foreach (var shape in m_tmpShapeList)
                    list.AddShape(shape);
                RigidBody.SetShape(list);
                if (RigidBody2 != null)
                    RigidBody2.SetShape(list);
                list.Base.RemoveReference();

                WeldedMarkBreakable();

                for (int i = 1; i < m_tmpShapeList.Count; i++)
                    m_tmpShapeList[i].RemoveReference();
                m_tmpShapeList.Clear();
                ProfilerShort.End();

                ProfilerShort.Begin("CalcMassProps");
                UpdateMassProps();
                //m_tmpElements.Clear();
                ProfilerShort.End();
            }
            ProfilerShort.End();
        }

        private void WeldedMarkBreakable()
        {
            if (HavokWorld == null)
                return;
            MyGridPhysics gp = this as MyGridPhysics;
            if (gp != null && (gp.Entity as MyCubeGrid).BlocksDestructionEnabled)
            {
                HavokWorld.BreakOffPartsUtil.MarkPieceBreakable(RigidBody, 0, gp.Shape.BreakImpulse);
            }

            uint shapeKey = 1;
            foreach (var child in WeldInfo.Children) 
            {
                gp = child as MyGridPhysics;
                if (gp != null && (gp.Entity as MyCubeGrid).BlocksDestructionEnabled)
                    HavokWorld.BreakOffPartsUtil.MarkPieceBreakable(RigidBody, shapeKey, gp.Shape.BreakImpulse);
                shapeKey++;
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
            if (IsWelded)
            {
                WeldInfo.Parent.Unweld(other, insertToWorld, recreateShape);
                Debug.Assert(other.IsWelded);
                return;
            }
            var rbWorldMatrix = RigidBody.GetRigidBodyMatrix();
            //other.Entity.OnPhysicsChanged -= WeldedEntity_OnPhysicsChanged;

            other.WeldInfo.Parent = null;
            Debug.Assert(WeldInfo.Children.Contains(other));
            WeldInfo.Children.Remove(other);

            var body = other.RigidBody;
            Debug.Assert(body == RigidBody);
            other.RigidBody = other.WeldedRigidBody;
            other.WeldedRigidBody = null;
            if (!other.RigidBody.IsDisposed)
            {
                other.RigidBody.SetWorldMatrix(other.WeldInfo.Transform * rbWorldMatrix);
                other.RigidBody.LinearVelocity = body.LinearVelocity;
                other.WeldInfo.MassElement.Tranform = Matrix.Identity;
                other.WeldInfo.Transform = Matrix.Identity;
            }
            else
            {
                Debug.Fail("Disposed welded body");
            }
            //RemoveConstraints(other.RigidBody);

            other.ClusterObjectID = MyHavokCluster.CLUSTERED_OBJECT_ID_UNITIALIZED;
            if (insertToWorld)
            {
                other.Activate();
                other.OnMotion(other.RigidBody, 0);
            }

            if(WeldInfo.Children.Count == 0)
            {
                Entity.OnPhysicsChanged -= WeldedEntity_OnPhysicsChanged;
                Entity.OnClose -= Entity_OnClose;
                WeldedRigidBody.LinearVelocity = RigidBody.LinearVelocity;
                WeldedRigidBody.AngularVelocity = RigidBody.AngularVelocity;
                if (HavokWorld != null)
                    HavokWorld.RemoveRigidBody(RigidBody);
                RigidBody.Dispose();
                RigidBody = WeldedRigidBody;
                WeldedRigidBody = null;
                RigidBody.SetWorldMatrix(rbWorldMatrix);
                WeldInfo.Transform = Matrix.Identity;
                if (HavokWorld != null)
                    HavokWorld.AddRigidBody(RigidBody);
                //(Entity as MyEntity).RaisePhysicsChanged();
            }
            if (RigidBody != null && recreateShape)
                RecreateWeldedShape(GetShape());
            OnUnwelded(other);
            other.OnUnwelded(this);
            //(other.Entity as MyEntity).RaisePhysicsChanged();
            Debug.Assert(!other.IsWelded);
        }

        private void RemoveConstraints(HkRigidBody hkRigidBody)
        {
            foreach(var constraint in m_constraints)
            {
                if (constraint.IsDisposed || (constraint.RigidBodyA == hkRigidBody || constraint.RigidBodyB == hkRigidBody))
                    m_constraintsRemoveBatch.Add(constraint);
            }
            foreach (var constraint in m_constraintsRemoveBatch)
            {
                m_constraints.Remove(constraint);
                if (!constraint.IsDisposed && constraint.InWorld)
                {
                    //System.Diagnostics.Debug.Assert(world.RigidBodies.Contains(constraint.RigidBodyA), "Object was removed prior to constraint");
                    //System.Diagnostics.Debug.Assert(world.RigidBodies.Contains(constraint.RigidBodyB), "Object was removed prior to constraint");
                    constraint.OnRemovedFromWorld();
                    HavokWorld.RemoveConstraint(constraint);
                }
            }
            m_constraintsRemoveBatch.Clear();
        }

        public void Unweld(bool insertInWorld = true)
        {
            Debug.Assert(WeldInfo.Parent != null);
            WeldInfo.Parent.Unweld(this, insertInWorld);
            Debug.Assert(!IsWelded);
        }
        #endregion

        public HkRigidBody WeldedRigidBody { get; protected set; }

        protected virtual void OnWelded(MyPhysicsBody other)
        {

        }

        protected virtual void OnUnwelded(MyPhysicsBody other)
        {

        }


        internal void DisableRagdollBodiesCollisions()
        {
            Debug.Assert(Ragdoll != null, "Ragdoll is null!");
            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                var world = HavokWorld;
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.StaticCollisionLayer),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.VoxelCollisionLayer),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.DefaultCollisionLayer),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.CharacterCollisionLayer),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.CharacterNetworkCollisionLayer),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.DynamicDoubledCollisionLayer),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.KinematicDoubledCollisionLayer),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.DebrisCollisionLayer),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.FloatingObjectCollisionLayer),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.GravityPhantomLayer),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.ObjectDetectionCollisionLayer),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.VirtualMassLayer),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.NoCollisionLayer),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.ExplosionRaycastLayer),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.CollisionLayerWithoutCharacter),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.CollideWithStaticLayer),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.CollectorCollisionLayer),"Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.RagdollCollisionLayer, MyPhysics.AmmoLayer),"Collision isn't disabled!");
            }
            if (Ragdoll != null)
            {
                foreach (var body in Ragdoll.RigidBodies)
                {
                    var info = HkGroupFilter.CalcFilterInfo(MyPhysics.RagdollCollisionLayer, 0, 0, 0);
                    //HavokWorld.DisableCollisionsBetween(MyPhysics.RagdollCollisionLayer, MyPhysics.RagdollCollisionLayer);
                    //HavokWorld.DisableCollisionsBetween(MyPhysics.RagdollCollisionLayer, MyPhysics.CharacterCollisionLayer);
                    body.SetCollisionFilterInfo(info);
                    body.LinearVelocity = Vector3.Zero;// Character.Physics.LinearVelocity;
                    body.AngularVelocity = Vector3.Zero;
                    HavokWorld.RefreshCollisionFilterOnEntity(body);
                    Debug.Assert(body.InWorld,"Body isn't in world!");
                    Debug.Assert(MyPhysics.RagdollCollisionLayer == HkGroupFilter.GetLayerFromFilterInfo(body.GetCollisionFilterInfo()),"Body is in wrong layer!");
                }
            }           
        }
    }
}