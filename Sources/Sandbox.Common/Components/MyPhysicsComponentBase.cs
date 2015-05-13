using Sandbox.Engine.Physics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using VRage.Utils;
using Sandbox.ModAPI;
using VRage;
using VRage.Library.Utils;

namespace Sandbox.Common.Components
{
    public abstract class MyPhysicsComponentBase : MyComponentBase
    {
        public static bool DebugDrawFlattenHierarchy = false;

        #region Fields


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

        public new IMyEntity Entity { get; set; }

        public bool CanUpdateAccelerations { get; set; }

        /// <summary>
        /// Gets or sets the type of the material.
        /// </summary>
        /// <value>
        /// The type of the material.
        /// </value>
        public MyStringId MaterialType { get; set; }
        public virtual MyStringId GetMaterialAt(Vector3D worldPos) { return MaterialType; }

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
        public bool IsKinematic
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

        #endregion

        #region Methods

        public RigidBodyFlag Flags;

        /// <summary>
        /// Use something from Havok to detect this
        /// </summary>
        public bool IsPhantom;
        private Vector3 m_lastLinearVelocity;
        private Vector3 m_lastAngularVelocity;
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
            MatrixD worldTransform, float mass, ushort collisionLayer, bool isOnlyVertical, float maxSlope, bool networkProxy);

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

        public virtual void UpdateAccelerations()
        {
            Vector3 delta = LinearVelocity - m_lastLinearVelocity;
            m_lastLinearVelocity = LinearVelocity;
            LinearAcceleration = delta / MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            Vector3 deltaAng = AngularVelocity - m_lastAngularVelocity;
            m_lastAngularVelocity = AngularVelocity;
            AngularAcceleration = deltaAng / MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
        }

        #endregion

        #region Implementation of IMyNotifyMotion

        public abstract MatrixD GetWorldMatrix();

        public abstract bool HasRigidBody { get; }

        public abstract Vector3D CenterOfMassWorld { get; }

        #endregion

        #region Implementation of IMyNotifyContact
        /*
        /// <summary>
        /// Called when [contact start].
        /// </summary>
        /// <param name="contactInfo">The contact info.</param>
        public virtual void OnContactStart(MyContactEventInfo contactInfo)
        {
            // Notify entity(script) about contact.
            this.Entity.NotifyContactStart(contactInfo);
        }

        /// <summary>
        /// Called when [contact end].
        /// </summary>
        /// <param name="contactInfo">The contact info.</param>
        public virtual void OnContactEnd(MyContactEventInfo contactInfo)
        {
            this.Entity.NotifyContactEnd(contactInfo);
        }

        /// <summary>
        /// Called when [contact touch].
        /// </summary>
        /// <param name="contactInfo">The contact info.</param>
        public virtual void OnContactTouch(MyContactEventInfo contactInfo)
        {
            this.Entity.NotifyContactTouch(contactInfo);
        }

        /// <summary>
        /// Called when contact
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns>false if contact has to be refused</returns>
        public virtual bool OnContact(ref MyRBSolverConstraint constraint)
        {
            return this.Entity.NotifyContact(ref constraint);
        }    */

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
    }
}
