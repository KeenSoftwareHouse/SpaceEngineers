using System;
using System.Diagnostics;
using VRageMath;
using VRage.Utils;
using VRage.ModAPI;
using Havok;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Game.SessionComponents;

namespace VRage.Game.Components
{
    //////////////////////////////////////////////////////////////////////////
    [Flags]
    public enum RigidBodyFlag
    {
        RBF_DEFAULT = (0),      // Default flag
        RBF_KINEMATIC = (1 << 1), // Rigid body is kinematic (has to be updated (matrix) per frame, velocity etc is then computed..)
        RBF_STATIC = (1 << 2), // Rigid body is static
        RBF_DISABLE_COLLISION_RESPONSE = (1 << 6), // Rigid body has no collision response        
        RBF_DOUBLED_KINEMATIC = (1 << 7),
        RBF_BULLET = (1 << 8),
        RBF_DEBRIS = (1 << 9),
        RBF_KEYFRAMED_REPORTING = (1 << 10),
    }

    public enum IntersectionFlags
    {
        DIRECT_TRIANGLES = 0x01,
        FLIPPED_TRIANGLES = 0x02,

        ALL_TRIANGLES = DIRECT_TRIANGLES | FLIPPED_TRIANGLES
    }

    /// <summary>
    /// Force type applied to physic object.
    /// </summary>
    public enum MyPhysicsForceType : byte
    {
        /// <summary>
        /// 
        /// </summary>
        APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,

        /// <summary>
        /// 
        /// </summary>
        ADD_BODY_FORCE_AND_BODY_TORQUE,

        APPLY_WORLD_FORCE
    }

    [MyComponentType(typeof(MyPhysicsComponentBase))]
    public abstract class MyPhysicsComponentBase : MyEntityComponentBase
    {

        #region Fields

        private Vector3 m_lastLinearVelocity;
        private Vector3 m_lastAngularVelocity;

        /// <summary>
        /// Must be set before creating rigid body
        /// </summary>
        public ushort ContactPointDelay = 0xffff; // Default Havok value

        public bool ReportAllContacts
        {
            get { return ContactPointDelay == 0; }
            set { ContactPointDelay = value ? (ushort)0x0000 : (ushort)0xffff; }
        }

        #endregion

        #region Properties

        public IMyEntity Entity { get; protected set; }

        public bool CanUpdateAccelerations { get; set; }

        /// <summary>
        /// Gets or sets the type of the material.
        /// </summary>
        /// <value>
        /// The type of the material.
        /// </value>
        public MyStringHash MaterialType { get; set; }
        public virtual MyStringHash GetMaterialAt(Vector3D worldPos) { return MaterialType; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="MyGameRigidBody"/> is static.
        /// </summary>
        /// <value>
        ///   <c>true</c> if static; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsStatic
        {
            get
            {
                // return this.RigidBody.GetRigidBodyInfo().MotionType == HkMotionType.Fixed;
                return (Flags & RigidBodyFlag.RBF_STATIC) == RigidBodyFlag.RBF_STATIC;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="MyGameRigidBody"/> is kinematic.
        /// </summary>
        /// <value>
        ///   <c>true</c> if kinematic; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsKinematic
        {
            get
            {
                //return this.RigidBody.GetRigidBodyInfo().MotionType == HkMotionType.Keyframed;
                return ((Flags & RigidBodyFlag.RBF_KINEMATIC) == RigidBodyFlag.RBF_KINEMATIC) ||
                    ((Flags & RigidBodyFlag.RBF_DOUBLED_KINEMATIC) == RigidBodyFlag.RBF_DOUBLED_KINEMATIC);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="MyPhysicsBody"/> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public virtual bool Enabled
        {
            get
            {
                return this.m_enabled;
            }
            set
            {
                if (this.m_enabled != value)
                {
                    this.m_enabled = value;
                    if (value)
                    {
                        if (Entity.InScene)
                            Activate();
                    }
                    else
                        Deactivate();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [play collision cue enabled].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [play collision cue enabled]; otherwise, <c>false</c>.
        /// </value>
        public bool PlayCollisionCueEnabled { get; set; }

        /// <summary>
        /// Gets or sets the type of the material.
        /// </summary>
        /// <value>
        /// The type of the material.
        /// </value>
        //public MyMaterialType MaterialType { get; set; }


        /// <summary>
        /// Gets or sets the mass.
        /// </summary>
        /// <value>
        /// The mass.
        /// </value>
        public abstract float Mass { get; }

        public Vector3 Center { get; set; }

        /// <summary>
        /// Gets or sets the linear velocity.
        /// </summary>
        /// <value>
        /// The linear velocity.
        /// </value>
        public abstract Vector3 LinearVelocity { get; set; }

        public virtual Vector3 LinearAcceleration
        {
            get;
            protected set;
        }

        public virtual Vector3 AngularAcceleration
        {
            get;
            protected set;
        }

        public abstract Vector3 GetVelocityAtPoint(Vector3D worldPos);

        ///// <summary>
        ///// Gets or sets max linear velocity.
        ///// </summary>
        ///// <value>
        ///// The max linear velocity.
        ///// </value>
        //public float MaxLinearVelocity
        //{
        //    get
        //    {
        //        //return this.rigidBody.MaxLinearVelocity;
        //        return 0;
        //    }
        //    set
        //    {
        //        MyUtils.AssertIsValid(value);
        //        //this.rigidBody.MaxLinearVelocity = value;
        //        //TODO
        //    }
        //}

        ///// <summary>
        ///// Gets or sets the linear acceleration.
        ///// </summary>
        ///// <value>
        ///// The linear acceleration.
        ///// </value>
        //public Vector3 LinearAcceleration
        //{
        //    get
        //    {
        //        //return rigidBody.LinearAcceleration;
        //        //return rigidBody.LinearFactor
        //        return Vector3.Zero;
        //    }
        //    set
        //    {
        //        //Debug.Assert(!float.IsNaN(value.X));
        //        //this.rigidBody.LinearAcceleration = value;
        //    }
        //}

        /// <summary>
        /// Gets or sets the linear damping.
        /// </summary>
        /// <value>
        /// The linear damping.
        /// </value>
        public abstract float LinearDamping
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the angular damping.
        /// </summary>
        /// <value>
        /// The angular damping.
        /// </value>
        public abstract float AngularDamping { get; set; }

        /// <summary>
        /// Gets or sets the angular velocity.
        /// </summary>
        /// <value>
        /// The angular velocity.
        /// </value>
        public abstract Vector3 AngularVelocity { get; set; }

        /// <summary>
        /// Gets or sets the speed.
        /// </summary>
        /// <value>
        /// The speed.
        /// </value>
        public abstract float Speed { get; }


        public abstract float Friction { get; set; }

        /// <summary>
        /// Obtain/set (default) rigid body of this physics object.
        /// </summary>
        public abstract HkRigidBody RigidBody { get; protected set; }

        /// <summary>
        /// Obtain/set secondary rigid body of this physics object (not used by default, it is used sometimes on grids for kinematic layer).
        /// </summary>
        public abstract HkRigidBody RigidBody2 { get; protected set; }

        public abstract HkdBreakableBody BreakableBody { get; set; }

        public abstract bool IsMoving { get; }

        public abstract Vector3 Gravity { get; }

        public MyPhysicsComponentDefinitionBase Definition { get; private set; }

        public MatrixD? ServerWorldMatrix { get; set; }

        #endregion

        #region Methods

        public RigidBodyFlag Flags;

        /// <summary>
        /// Use something from Havok to detect this
        /// </summary>
        public bool IsPhantom;
        protected bool m_enabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="MyPhysicsBody"/> class.
        /// </summary>
        /// <param name="entity">The entity.</param>
        //public MyPhysicsBody(MyEntity entity, RigidBodyFlag flags)
        //{
        //    //Debug.Assert(entity != null);

        //    this.m_enabled = false;
        //    this.Entity = entity;
        //    this.Flags = flags;
        //}

        public virtual void Close()
        {
            Deactivate();
            CloseRigidBody();
        }

        protected abstract void CloseRigidBody();

        /// <summary>
        /// Applies external force to the physics object.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="force">The force.</param>
        /// <param name="position">The position.</param>
        /// <param name="torque">The torque.</param>
        public abstract void AddForce(MyPhysicsForceType type, Vector3? force, Vector3D? position, Vector3? torque);

        /// <summary>
        /// Applies the impulse.
        /// </summary>
        /// <param name="dir">The dir.</param>
        /// <param name="pos">The pos.</param>
        public abstract void ApplyImpulse(Vector3 dir, Vector3D pos);

        /// <summary>
        /// Clears the speeds.
        /// </summary>
        public abstract void ClearSpeed();

        /// <summary>
        /// Clear all dynamic values of physics object.
        /// </summary>
        public abstract void Clear();

        public abstract void CreateCharacterCollision(Vector3 center, float characterWidth, float characterHeight,
            float crouchHeight, float ladderHeight, float headSize, float headHeight,
            MatrixD worldTransform, float mass, ushort collisionLayer, bool isOnlyVertical, float maxSlope, float maxLimit, float maxSpeedRelativeToShip, bool networkProxy, float? maxForce);

        /// <summary>
        /// Debug draw of this physics object.
        /// </summary>
        public abstract void DebugDraw();

        /// <summary>
        /// Activates this rigid body in physics.
        /// </summary>
        public abstract void Activate();

        /// <summary>
        /// Deactivates this rigid body in physics.
        /// </summary>
        public abstract void Deactivate();

        /// </summary>
        public abstract void ForceActivate();

        public void UpdateAccelerations()
        {
            LinearAcceleration = (LinearVelocity - m_lastLinearVelocity) * VRage.Game.MyEngineConstants.UPDATE_STEPS_PER_SECOND;
            m_lastLinearVelocity = LinearVelocity;

            AngularAcceleration = (AngularVelocity - m_lastAngularVelocity) * VRage.Game.MyEngineConstants.UPDATE_STEPS_PER_SECOND;
            m_lastAngularVelocity = AngularVelocity;
        }

        /// <summary>
        /// Set the current linear and angular velocities of this physics body.
        /// </summary>
        public void SetSpeeds(Vector3 linear, Vector3 angular)
        {
            LinearVelocity = linear;
            AngularVelocity = angular;
            ClearAccelerations();
            SetActualSpeedsAsPrevious();
        }

        private void ClearAccelerations()
        {
            LinearAcceleration = Vector3.Zero;
            AngularAcceleration = Vector3.Zero;
        }

        private void SetActualSpeedsAsPrevious()
        {
            // setting of previous speeds according to current one - elimination of acceleration that was caused by setting of speed when for example speed on server is different that speed on client
            m_lastLinearVelocity = LinearVelocity;
            m_lastAngularVelocity = AngularVelocity;
        }

        /// <summary>
        /// Converts global space position to local cluster space.
        /// </summary>
        public abstract Vector3D WorldToCluster(Vector3D worldPos);

        /// <summary>
        /// Converts local cluster position to global space.
        /// </summary>
        public abstract Vector3D ClusterToWorld(Vector3 clusterPos);

        #endregion

        #region Implementation of IMyNotifyMotion

        public abstract MatrixD GetWorldMatrix();

        public abstract bool HasRigidBody { get; }

        public abstract Vector3D CenterOfMassWorld { get; }

        public abstract void UpdateFromSystem();

        #endregion

        #region Implementation of IMyNotifyEntityChanged

        /// <summary>
        /// Called when [world position changed].
        /// </summary>
        /// <param name="source">The source object that caused this event.</param>
        public abstract void OnWorldPositionChanged(object source);

        public virtual bool IsInWorld
        {
            get;
            protected set;
        }

        #endregion

        public override string ComponentTypeDebugString
        {
            get { return "Physics"; }
        }

        public override void Init(MyComponentDefinitionBase definition)
        {
            base.Init(definition);

            Definition = definition as MyPhysicsComponentDefinitionBase;
            Debug.Assert(Definition != null);
            if (Definition != null)
            {
                Flags = Definition.RigidBodyFlags;

                if (Definition.LinearDamping != null)
                    LinearDamping = Definition.LinearDamping.Value;

                if (Definition.AngularDamping != null)
                    AngularDamping = Definition.AngularDamping.Value;
            }
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            // MyPhysicsComponentBase has its own Entity property which hides MyEntityComponentBase property!
            Entity = Container.Entity;
            Debug.Assert(Entity != null);

            if (Definition != null)
            {
                if (Definition.UpdateFlags != 0)
                    MyPhysicsComponentSystem.Static.Register(this);
            }
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();

            if (Definition != null && Definition.UpdateFlags != 0 && MyPhysicsComponentSystem.Static != null)
                MyPhysicsComponentSystem.Static.Unregister(this);
        }

        public override bool IsSerialized()
        {
            return Definition != null && Definition.Serialize;
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var builder = MyComponentFactory.CreateObjectBuilder(this) as MyObjectBuilder_PhysicsComponentBase;
            builder.LinearVelocity = LinearVelocity;
            builder.AngularVelocity = AngularVelocity;
            return builder;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase baseBuilder)
        {
            var builder = baseBuilder as MyObjectBuilder_PhysicsComponentBase;
            LinearVelocity = builder.LinearVelocity;
            AngularVelocity = builder.AngularVelocity;
        }
    }
}
