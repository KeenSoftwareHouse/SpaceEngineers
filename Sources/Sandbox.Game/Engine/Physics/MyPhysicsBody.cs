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
        public static bool HkGridShapeCellDebugDraw = false;

        private Vector3 m_lastLinearVelocity;
        private Vector3 m_lastAngularVelocity;


        #region Properties

        public static HkGeometry DebugGeometry;
        Dictionary<string, Vector3D> DebugShapesPositions = new Dictionary<string, Vector3D>();

        public int HavokCollisionSystemID=0;
        private HkRigidBody m_rigidBody;
        public virtual HkRigidBody RigidBody 
        { 
            get { return m_rigidBody; }
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
        static List<HkShape> m_tmpShapeList = new List<HkShape>();

        protected ulong ClusterObjectID = MyHavokCluster.CLUSTERED_OBJECT_ID_UNITIALIZED;

        protected HkWorld m_world;
        public HkWorld HavokWorld
        {
            get { return m_world; }
        }

        protected HkRagdoll m_ragdoll;

        public event EventHandler OnRagdollActivated;

        #endregion

        #region Properties

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
            }
        }
        
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

            System.Diagnostics.Debug.Assert(IsInWorld == true);

            if (IsStatic)
                return;

            switch (type)
            {
                case MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE:
                    {
                        if (RigidBody != null)
                        {
                            Matrix tempM = RigidBody.GetRigidBodyMatrix();
                            tempM.Translation = Vector3.Zero;

                            if (force != null && !MyUtils.IsZero(force.Value))
                            {
                                Vector3 tmpForce = Vector3.Transform(force.Value, tempM);

                                //RigidBody.Activate(true);
                                //RigidBody.ApplyForce(MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS, tmpForce * 0.0001f);
                                RigidBody.ApplyLinearImpulse(tmpForce * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
                                //RigidBody.ApplyCentralImpulse(tmpForce);
                            }

                            if (torque != null && !MyUtils.IsZero(torque.Value))
                            {
                                Vector3 tmpTorque = Vector3.Transform(torque.Value, tempM);
                                //SharpDX.Vector3 tmpTorque = SharpDXHelper.ToSharpDX(torque.Value);

                                // RigidBody.Activate(true);
                                //RigidBody.UpdateInertiaTensor();
                                //RigidBody.ApplyTorque(MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS, tmpTorque * 0.0001f);
                                RigidBody.ApplyAngularImpulse(tmpTorque * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
                                //RigidBody.ApplyTorqueImpulse(tmpTorque);
                            }
                        }
                        if (CharacterProxy != null)
                        {
                            Matrix tempM = Entity.WorldMatrix;
                            tempM.Translation = Vector3.Zero;

                            if (force != null && !MyUtils.IsZero(force.Value))
                            {
                                Vector3 tmpForce = Vector3.Transform(force.Value, tempM);

                                CharacterProxy.ApplyLinearImpulse(tmpForce * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
                            }
                            if (torque != null && !MyUtils.IsZero(torque.Value))
                            {
                                Vector3 tmpTorque = Vector3.Transform(torque.Value, tempM);

                                CharacterProxy.ApplyAngularImpulse(tmpTorque * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
                            }
                        }
                        if (Ragdoll != null && Ragdoll.IsAddedToWorld && !Ragdoll.IsKeyframed)
                        {
                            
                            foreach (var rigidBody in Ragdoll.RigidBodies)
                            {

                                if (rigidBody != null)
                                {
                                    Matrix tempM = rigidBody.GetRigidBodyMatrix();
                                    tempM.Translation = Vector3.Zero;                                    
                                    if (force != null && !MyUtils.IsZero(force.Value))
                                    {
                                        Vector3 tmpForce = Vector3.Transform(force.Value, tempM) * rigidBody.Mass / Ragdoll.Mass;

                                        //RigidBody.Activate(true);
                                        //RigidBody.ApplyForce(MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS, tmpForce * 0.0001f);
                                        rigidBody.ApplyLinearImpulse(tmpForce * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
                                        //RigidBody.ApplyCentralImpulse(tmpForce);
                                    }

                                    if (torque != null && !MyUtils.IsZero(torque.Value))
                                    {
                                        Vector3 tmpTorque = Vector3.Transform(torque.Value, tempM) * rigidBody.Mass / Ragdoll.Mass;
                                        //SharpDX.Vector3 tmpTorque = SharpDXHelper.ToSharpDX(torque.Value);

                                        // RigidBody.Activate(true);
                                        //RigidBody.UpdateInertiaTensor();
                                        //RigidBody.ApplyTorque(MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS, tmpTorque * 0.0001f);
                                        rigidBody.ApplyAngularImpulse(tmpTorque * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
                                        //RigidBody.ApplyTorqueImpulse(tmpTorque);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE:
                    {
                        if (RigidBody != null)
                        {
                            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

                            if (force.HasValue && position.HasValue)
                            {
                                //this.RigidBody.ApplyImpulse(force.Value, position.Value);
                                //RigidBody.Activate(true);
                                RigidBody.ApplyPointImpulse(force.Value, (Vector3)(position.Value - offset));
                            }

                            if (torque.HasValue)
                            {
                                //RigidBody.Activate(true);
                                //RigidBody.ApplyTorque(MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS, torque.Value * 0.0001f);
                                RigidBody.ApplyAngularImpulse(torque.Value * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
                            }
                        }

                        if (CharacterProxy != null && force.HasValue && position.HasValue)
                        {
                            CharacterProxy.ApplyLinearImpulse(force.Value);
                        }
                        if (Ragdoll != null && Ragdoll.IsAddedToWorld && !Ragdoll.IsKeyframed)
                        {
                            foreach (var rigidBody in Ragdoll.RigidBodies)
                            {
                                if (rigidBody != null)
                                {
                                    var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

                                    if (force.HasValue && position.HasValue)
                                    {
                                        //this.RigidBody.ApplyImpulse(force.Value, position.Value);
                                        //RigidBody.Activate(true);
                                        rigidBody.ApplyPointImpulse(force.Value * rigidBody.Mass / Ragdoll.Mass, (Vector3)(position.Value - offset));
                                    }

                                    if (torque.HasValue)
                                    {
                                        //RigidBody.Activate(true);
                                        //RigidBody.ApplyTorque(MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS, torque.Value * 0.0001f);
                                        rigidBody.ApplyAngularImpulse(torque.Value * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED * rigidBody.Mass / Ragdoll.Mass);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case MyPhysicsForceType.APPLY_WORLD_FORCE:
                    {
                        if (RigidBody != null)
                        {
                            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

                            if (force != null && !MyUtils.IsZero(force.Value))
                            {
                                if (position.HasValue)
                                {
                                    Vector3 point = position.Value - offset;
                                    RigidBody.ApplyForce(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, force.Value, point);
                                }
                                else
                                    RigidBody.ApplyForce(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, force.Value);
                            }                                
                        }
                        if (Ragdoll != null && Ragdoll.IsAddedToWorld && !Ragdoll.IsKeyframed)
                        {
                            foreach (var rigidBody in Ragdoll.RigidBodies)
                            {
                                if (rigidBody != null)
                                {
                                    var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

                                    if (force != null && !MyUtils.IsZero(force.Value))
                                    {
                                        if (position.HasValue)
                                        {
                                            Vector3 point = position.Value - offset;
                                            rigidBody.ApplyForce(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, force.Value * rigidBody.Mass / Ragdoll.Mass, point);
                                        }
                                        else
                                            rigidBody.ApplyForce(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, force.Value * rigidBody.Mass / Ragdoll.Mass);
                                    }
                                }
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

        /// <summary>
        /// Applies the impulse.
        /// </summary>
        /// <param name="impulse">The dir.</param>
        /// <param name="pos">The pos.</param>
        public override void ApplyImpulse(Vector3 impulse, Vector3D pos)
        {
            impulse.AssertIsValid();
            pos.AssertIsValid();
            System.Diagnostics.Debug.Assert(IsInWorld == true);

            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);
            var posF = (Vector3)(pos - offset);

            RigidBody.ApplyPointImpulse(impulse, posF);
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
                //VRageRender.MyRenderProxy.DebugDrawSphere(com, 0.2f, Color.Wheat, 1, false);

                VRageRender.MyRenderProxy.DebugDrawAxis(Entity.PositionComp.WorldMatrix, 0.2f, false);

                //var mp = new HkMassProperties();
                //BreakableBody.BreakableShape.BuildMassProperties(ref mp);

               // BreakableBody.BreakableShape.SetMassProperties(mp);

                //RigidBody.SetMassProperties(ref mp);
                
            }

            int index;
            if (RigidBody != null)
            {
                index = 0;
                Matrix rbMatrix = RigidBody.GetRigidBodyMatrix();
                MatrixD worldMatrix = MatrixD.CreateWorld(rbMatrix.Translation + offset, rbMatrix.Forward, rbMatrix.Up);

                DrawCollisionShape(RigidBody.GetShape(), worldMatrix, alpha, ref index);
            }

            if (RigidBody2 != null)
            {
                index = 0;
                Matrix rbMatrix = RigidBody2.GetRigidBodyMatrix();
                MatrixD worldMatrix = MatrixD.CreateWorld(rbMatrix.Translation + offset, rbMatrix.Forward, rbMatrix.Up);

                DrawCollisionShape(RigidBody2.GetShape(), worldMatrix, alpha, ref index);
            }

            if (CharacterProxy != null)
            {
                index = 0;
                //MatrixD characterTransform = MatrixD.CreateWorld(CharacterProxy.Position + offset, CharacterProxy.Forward, CharacterProxy.Up);
                Matrix rbMatrix = CharacterProxy.GetRigidBodyTransform();
                MatrixD worldMatrix = MatrixD.CreateWorld(rbMatrix.Translation + offset, rbMatrix.Forward, rbMatrix.Up);

                DrawCollisionShape(CharacterProxy.GetShape(), worldMatrix, alpha, ref index);
            }
        }


        public void DebugDrawBreakable()
        {
            const float alpha = 0.3f;

            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

            DebugShapesPositions.Clear();

            if (BreakableBody != null)
            {
                int index = 0;
                Matrix rbMatrix = BreakableBody.GetRigidBody().GetRigidBodyMatrix();
                MatrixD worldMatrix = MatrixD.CreateWorld(rbMatrix.Translation + offset, rbMatrix.Forward, rbMatrix.Up);

                DrawBreakableShape(BreakableBody.BreakableShape, worldMatrix, alpha, ref index);
                DrawConnections(BreakableBody.BreakableShape, worldMatrix, alpha, ref index);
            }

        }

        private void DrawBreakableShape(HkdBreakableShape breakableShape, MatrixD worldMatrix, float alpha, ref int shapeIndex, string customText = null, bool isPhantom = false)
        {
            //VRageRender.MyRenderProxy.DebugDrawText3D(worldMatrix.Translation, , Color.White, 1, false);
            DrawCollisionShape(breakableShape.GetShape(), worldMatrix, alpha, ref shapeIndex, breakableShape.Name + " Strength: " + breakableShape.GetStrenght() + " Static:" + breakableShape.IsFixed());

            if (!string.IsNullOrEmpty(breakableShape.Name) && breakableShape.Name != "PineTree175m_v2_001" && breakableShape.IsFixed())
            {
            }

            DebugShapesPositions[breakableShape.Name] = worldMatrix.Translation;

            List<HkdShapeInstanceInfo> children = new List<HkdShapeInstanceInfo>();
            breakableShape.GetChildren(children);

            Vector3 parentCom =  breakableShape.CoM;

            foreach (var shapeInst in children)
            {
                Matrix transform = shapeInst.GetTransform();
              //  transform.Translation += (shapeInst.Shape.CoM - parentCom);
                Matrix trWorld = transform * worldMatrix * Matrix.CreateTranslation(Vector3.Right * 2);
                DrawBreakableShape(shapeInst.Shape, trWorld, alpha, ref shapeIndex);
            }
        }

        private void DrawConnections(HkdBreakableShape breakableShape, MatrixD worldMatrix, float alpha, ref int shapeIndex, string customText = null, bool isPhantom = false)
        {
            List<HkdConnection> connections = new List<HkdConnection>();
            breakableShape.GetConnectionList(connections);

            List<HkdShapeInstanceInfo> children = new List<HkdShapeInstanceInfo>();
            breakableShape.GetChildren(children);

            foreach (var conn in connections)
            {
                var posA = DebugShapesPositions[conn.ShapeAName];
                var posB = DebugShapesPositions[conn.ShapeBName];

                bool cont = false;
                foreach (var child in children)
                {
                    if ((child.ShapeName == conn.ShapeAName) || (child.ShapeName == conn.ShapeBName))
                        cont = true;
                }

                if (cont)
                    VRageRender.MyRenderProxy.DebugDrawLine3D(posA, posB, Color.White, Color.White, false);
            }
        }


        static Color[] boxColors = MyUtils.GenerateBoxColors();

        static Color GetShapeColor(HkShapeType shapeType, ref int shapeIndex, bool isPhantom)
        {
            if (isPhantom)
                return Color.LightGreen;

            switch (shapeType)
            {
                case HkShapeType.Sphere:
                    return Color.White;
                case HkShapeType.Capsule:
                    return Color.Yellow;
                case HkShapeType.Cylinder:
                    return Color.Orange;
                case HkShapeType.ConvexVertices:
                    return Color.Red;

                default:
                case HkShapeType.Box:
                    return boxColors[(++shapeIndex) % (boxColors.Length - 1)];
            }
        }

        public static void DrawCollisionShape(HkShape shape, MatrixD worldMatrix, float alpha, ref int shapeIndex, string customText = null, bool isPhantom = false)
        {
            var color = GetShapeColor(shape.ShapeType, ref shapeIndex, isPhantom);
            if (isPhantom) alpha *= alpha;
            color.A = (byte)(alpha * 255);

            bool shaded = true;

            float expandSize = 0.02f;
            float expandRatio = 1.035f;

            bool drawCustomText = false;

            switch (shape.ShapeType)
            {
                case HkShapeType.Sphere:
                    {
                        var sphere = (HkSphereShape)shape;
                        float radius = sphere.Radius;

                        VRageRender.MyRenderProxy.DebugDrawSphere(worldMatrix.Translation, radius, color, alpha, true, shaded);

                        if (isPhantom)
                        {
                            VRageRender.MyRenderProxy.DebugDrawSphere(worldMatrix.Translation, radius, color, 1.0f, true, false);
                            VRageRender.MyRenderProxy.DebugDrawSphere(worldMatrix.Translation, radius, color, 1.0f, true, false, false);
                        }

                        drawCustomText = true;
                        break;
                    }

                case HkShapeType.Capsule:
                    {
                        // Sphere and OBB to show cylinder space
                        var capsule = (HkCapsuleShape)shape;
                        Vector3D vertexA = Vector3.Transform(capsule.VertexA, worldMatrix);
                        Vector3D vertexB = Vector3.Transform(capsule.VertexB, worldMatrix);
                        VRageRender.MyRenderProxy.DebugDrawCapsule(vertexA, vertexB, capsule.Radius, color, true, shaded);
                        drawCustomText = true;
                        break;
                    }

                case HkShapeType.Cylinder:
                    {
                        // Sphere and OBB to show cylinder space
                        var cylinder = (HkCylinderShape)shape;
                        VRageRender.MyRenderProxy.DebugDrawCylinder(worldMatrix, cylinder.VertexA, cylinder.VertexB, cylinder.Radius, color, alpha, true, shaded);
                        drawCustomText = true;
                        break;
                    }


                case HkShapeType.Box:
                    {
                        var box = (HkBoxShape)shape;

                        VRageRender.MyRenderProxy.DebugDrawOBB(MatrixD.CreateScale(box.HalfExtents * 2 + new Vector3(expandSize)) * worldMatrix, color, alpha, true, shaded);
                        if (isPhantom)
                        {
                            VRageRender.MyRenderProxy.DebugDrawOBB(Matrix.CreateScale(box.HalfExtents * 2 + new Vector3(expandSize)) * worldMatrix, color, 1.0f, true, false);
                            VRageRender.MyRenderProxy.DebugDrawOBB(Matrix.CreateScale(box.HalfExtents * 2 + new Vector3(expandSize)) * worldMatrix, color, 1.0f, true, false, false);
                        }
                        drawCustomText = true;
                        break;
                    }

                case HkShapeType.ConvexVertices:
                    {
                        var convexShape = (HkConvexVerticesShape)shape;
                        Vector3 center;
                        convexShape.GetGeometry(DebugGeometry, out center);
                        Vector3D transformedCenter = Vector3D.Transform(center, worldMatrix.GetOrientation());

                        var matrix = worldMatrix;
                        matrix = MatrixD.CreateScale(expandRatio) * matrix;
                        matrix.Translation -= transformedCenter * (expandRatio - 1);

                        //matrix.Translation += transformedCenter;
                        DrawGeometry(DebugGeometry, matrix, color, true, true);

                        drawCustomText = true;
                        break;
                    }

                case HkShapeType.ConvexTranslate:
                    {
                        var translateShape = (HkConvexTranslateShape)shape;
                        DrawCollisionShape((HkShape)translateShape.ChildShape, Matrix.CreateTranslation(translateShape.Translation) * worldMatrix, alpha, ref shapeIndex, customText);
                        break;
                    }

                case HkShapeType.ConvexTransform:
                    {
                        var transformShape = (HkConvexTransformShape)shape;
                        DrawCollisionShape(transformShape.ChildShape, transformShape.Transform * worldMatrix, alpha, ref shapeIndex, customText);
                        break;
                    }

                case HkShapeType.Mopp:
                    {
                        var compoundShape = (HkMoppBvTreeShape)shape;
                        DrawCollisionShape(compoundShape.ShapeCollection, worldMatrix, alpha, ref shapeIndex, customText);
                        break;
                    }

                case HkShapeType.List:
                    {
                        var listShape = (HkListShape)shape;
                        var iterator = listShape.GetIterator();
                        while (iterator.IsValid)
                        {
                            //string text = (customText ?? string.Empty) + "[" + iterator.CurrentShapeKey + "]";
                            DrawCollisionShape(iterator.CurrentValue, worldMatrix, alpha, ref shapeIndex, customText);
                            iterator.Next();
                        }
                        break;
                    }

                case HkShapeType.StaticCompound:
                    {
                        var compoundShape = (HkStaticCompoundShape)shape;

                        if (DebugDrawFlattenHierarchy)
                        {
                            var it = compoundShape.GetIterator();
                            while (it.IsValid)
                            {
                                if (compoundShape.IsShapeKeyEnabled(it.CurrentShapeKey))
                                {
                                    string text = (customText ?? string.Empty) + "-" + it.CurrentShapeKey + "-";
                                    DrawCollisionShape(it.CurrentValue, worldMatrix, alpha, ref shapeIndex, text);
                                }
                                it.Next();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < compoundShape.InstanceCount; i++)
                            {
                                bool enabled = compoundShape.IsInstanceEnabled(i);
                                string text;
                                if (enabled)
                                    text = (customText ?? string.Empty) + "<" + i + ">";
                                else
                                    text = (customText ?? string.Empty) + "(" + i + ")";

                                if (enabled)
                                    DrawCollisionShape(compoundShape.GetInstance(i), compoundShape.GetInstanceTransform(i) * worldMatrix, alpha, ref shapeIndex, text);
                            }
                        }

                        break;
                    }

                case HkShapeType.Triangle:
                    {
                        HkTriangleShape tri = (HkTriangleShape)shape;
                        VRageRender.MyRenderProxy.DebugDrawTriangle(tri.Pt0, tri.Pt1, tri.Pt2, Color.Green, false, false);
                        break;
                    }

                case HkShapeType.BvTree:
                    {
                        var gridShape = (HkGridShape)shape;
                        if (HkGridShapeCellDebugDraw && !gridShape.Base.IsZero)
                        {
                            Vector3S min, max;
                            var cellSize = gridShape.CellSize;

                            int count = gridShape.GetShapeInfoCount();
                            for (int i = 0; i < count; i++)
                            {
                                try
                                {
                                    gridShape.GetShapeInfo(i, out min, out max, m_tmpShapeList);
                                    Vector3 size = max * cellSize - min * cellSize;
                                    Vector3 center = (max * cellSize + min * cellSize) / 2.0f;
                                    size += Vector3.One * cellSize;
                                    var clr = color;
                                    if (min == max)
                                    {
                                        clr = new Color(1.0f, 0.2f, 0.1f);
                                    }
                                    VRageRender.MyRenderProxy.DebugDrawOBB(Matrix.CreateScale(size + new Vector3(expandSize)) * Matrix.CreateTranslation(center) * worldMatrix, clr, alpha, true, shaded);
                                }
                                finally
                                {
                                    m_tmpShapeList.Clear();
                                }
                            }
                        }
                        else
                        {
                            var msg = MyRenderProxy.PrepareDebugDrawTriangles();

                            try
                            {
                                using (HkShapeBuffer buf = new HkShapeBuffer())
                                {
                                    var treeShape = (HkBvTreeShape)shape;
                                    for (var i = treeShape.GetIterator(buf); i.IsValid; i.Next())
                                    {
                                        var child = i.CurrentValue;
                                        if (child.ShapeType == HkShapeType.Triangle)
                                        {
                                            var tri = (HkTriangleShape)child;
                                            msg.AddTriangle(tri.Pt0, tri.Pt1, tri.Pt2);
                                        }
                                        else
                                        {
                                            DrawCollisionShape(child, worldMatrix, alpha, ref shapeIndex);
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                MyRenderProxy.DebugDrawTriangles(msg, worldMatrix, color, false, false);
                            }
                        }
                        break;
                    }

                case HkShapeType.BvCompressedMesh:
                    {
                        if (MyDebugDrawSettings.DEBUG_DRAW_TRIANGLE_PHYSICS)
                        {
                            var meshShape = (HkBvCompressedMeshShape)shape;
                            meshShape.GetGeometry(DebugGeometry);
                            DrawGeometry(DebugGeometry, worldMatrix, Color.Green, false, false);
                            drawCustomText = true;
                        }
                        break;
                    }

                case HkShapeType.Bv:
                    {
                        var bvShape = (HkBvShape)shape;
                        DrawCollisionShape(bvShape.BoundingVolumeShape, worldMatrix, alpha, ref shapeIndex, null, true);
                        DrawCollisionShape(bvShape.ChildShape, worldMatrix, alpha, ref shapeIndex);
                        break;
                    }

                case HkShapeType.PhantomCallback:
                    {
                        // Nothing to draw, it's just shape with events
                        MyRenderProxy.DebugDrawText3D(worldMatrix.Translation, "Phantom", Color.Green, 0.75f, false);
                        break;
                    }

                default:
                    break;
            }

            if (drawCustomText && customText != null)
            {
                color.A = 255;
                MyRenderProxy.DebugDrawText3D(worldMatrix.Translation, customText, color, 0.8f, false);
            }
        }

        public static void DrawGeometry(HkGeometry geometry, MatrixD worldMatrix, Color color, bool depthRead = false, bool shaded = false)
        {
            var msg = MyRenderProxy.PrepareDebugDrawTriangles();

            try
            {
                for (int i = 0; i < geometry.TriangleCount; i++)
                {
                    int a, b, c, m;
                    geometry.GetTriangle(i, out a, out b, out c, out m);
                    msg.AddIndex(a);
                    msg.AddIndex(b);
                    msg.AddIndex(c);
                }

                for (int i = 0; i < geometry.VertexCount; i++)
                {
                    msg.AddVertex(geometry.GetVertex(i));
                }
            }
            finally
            {
                MyRenderProxy.DebugDrawTriangles(msg, worldMatrix, color, depthRead, shaded);
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
            
            if ((int)(Flags & RigidBodyFlag.RBF_DOUBLED_KINEMATIC) > 0)
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

        protected static MyStringId m_destructionSound = MyStringId.GetOrCompute("Destruction");
        private void PlayDestructionSound()
        {
            var bDef = MyDefinitionManager.Static.GetCubeBlockDefinition((Entity as MyFracturedPiece).OriginalBlocks[0]);
            
            if (bDef == null)
                return;
            MyPhysicalMaterialDefinition def = bDef.PhysicalMaterial;

            MySoundPair destructionCue;
            if (def.GeneralSounds.TryGetValue(m_destructionSound, out destructionCue) && !destructionCue.SoundId.IsNull)
            {
                var emmiter = MyAudioComponent.TryGetSoundEmitter();
                if (emmiter == null)
                    return;
                Vector3D pos = Entity.PositionComp.GetPosition();
                emmiter.SetPosition(pos);
                emmiter.PlaySound(destructionCue);
            }
        }


        protected void GetInfoFromFlags(HkRigidBodyCinfo rbInfo, RigidBodyFlag flags)
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

        static MyStringId m_startCue = MyStringId.GetOrCompute("Start");

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
            MySoundPair cue = null;
            var worldPos = ClusterToWorld(value.ContactPoint.Position);
            cue = MyMaterialSoundsHelper.Static.GetCollisionCue(m_startCue, bodyA.GetMaterialAt(worldPos + value.ContactPoint.Normal * 0.1f), bodyB.GetMaterialAt(worldPos - value.ContactPoint.Normal * 0.1f));
            //cue = MyMaterialsConstants.GetCollisionCue(MyMaterialsConstants.MyMaterialCollisionType.Start, value.Base.BodyA.GetBody().MaterialType, value.Base.BodyB.GetBody().MaterialType);

            if (!cue.SoundId.IsNull)
            {
                MyEntity3DSoundEmitter emitter;
                {
                    emitter = MyAudioComponent.TryGetSoundEmitter();
                    if (emitter == null)
                    {
                        ProfilerShort.End();
                        return;
                    }
                    //emitter = new MyEntity3DSoundEmitter(null);
                    MyAudioComponent.ContactSoundsPool.TryAdd(Entity.EntityId, 0);
                    emitter.StoppedPlaying += (e) => 
                    { 
                        byte val;
                        MyAudioComponent.ContactSoundsPool.TryRemove(Entity.EntityId, out val); 
                    };
                    if (MySession.Static.Settings.RealisticSound && MyFakes.ENABLE_NEW_SOUNDS)
                    {
                        var colision = value.Base;
                        Func<bool> canHear = () =>
                        {
                            if (MySession.ControlledEntity != null)
                            {
                                var entity = MySession.ControlledEntity.Entity.GetTopMostParent();
                                return (entity == colision.BodyA.GetEntity() || entity == colision.BodyB.GetEntity());
                            }
                            return false;
                        };
                        Func<bool> shouldPlay2D = () => MySession.ControlledEntity != null && MySession.ControlledEntity.Entity is MyCharacter && (
                                MySession.ControlledEntity.Entity.Components == colision.BodyA.GetEntity() || MySession.ControlledEntity.Entity.Components == colision.BodyB.GetEntity());
                        Action<MyEntity3DSoundEmitter> remove = null;
                        remove = (e) =>
                            {
                                emitter.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.CanHear].Remove(canHear);
                                emitter.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.ShouldPlay2D].Remove(shouldPlay2D);
                                emitter.StoppedPlaying -= remove;
                            };
                        emitter.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.CanHear].Add(canHear);
                        emitter.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.ShouldPlay2D].Add(shouldPlay2D);
                        emitter.StoppedPlaying += remove;
                    }
                }
                emitter.SetPosition(ClusterToWorld(value.ContactPoint.Position));
                emitter.PlaySound(cue, true);
                if (emitter.Sound != null)
                {
                    if (volume != 0)
                    {
                        emitter.Sound.SetVolume(volume);
                    }
                    else
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
                            emitter.Sound.SetVolume(val / 10);
                        else
                            emitter.Sound.SetVolume(1);
                    }
                }
            }
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

        protected virtual void ActivateCollision(){}

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


        private bool m_isStaticForCluster = false;
        public virtual bool IsStaticForCluster
        {
            get { return m_isStaticForCluster; }
            set { m_isStaticForCluster = value; }
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

        #region Implementation of IMyNotifyMotion

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
            UpdateCluster();
            ProfilerShort.End();

            ProfilerShort.End();
        }

        public void UpdateCluster()
        {
            if (!MyPerGameSettings.LimitedWorld)
                MyPhysics.Clusters.MoveObject(ClusterObjectID, Entity.WorldAABB, Entity.WorldAABB, Entity.GetTopMostParent().Physics.LinearVelocity);
        }


        public override MatrixD GetWorldMatrix()
        {
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

        //public MatrixD GetRigidBodyMatrix()
        //{
        //    MatrixD rigidBodyMatrix = MatrixD.Identity;

        //    var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);

        //    if (RigidBody != null)
        //    {
        //        rigidBodyMatrix = RigidBody2.GetWorldMatrix();
        //    }
        //    else if (RigidBody2 != null)
        //    {
        //        rigidBodyMatrix = RigidBody2.GetWorldMatrix();
        //    }
        //    else
        //    {
        //        rigidBodyMatrix = MatrixD.CreateWorld(CharacterProxy.Position, CharacterProxy.Forward, CharacterProxy.Up);
        //    }

        //    rigidBodyMatrix.Translation += offset;

        //    return rigidBodyMatrix;
        //}
        
        public Vector3 WorldToCluster(Vector3D worldPos)
        {
            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);
            return (Vector3)(worldPos - offset);
        }

        public Vector3D ClusterToWorld(Vector3 clusterPos)
        {
            var offset = MyPhysics.Clusters.GetObjectOffset(ClusterObjectID);
            return (Vector3D)clusterPos + (Vector3D)offset;
        }

        public Vector3 GetVelocityAtPoint(Vector3D worldPos)
        {
            Vector3 relPos = WorldToCluster(worldPos);
            if (RigidBody != null)
                return RigidBody.GetVelocityAtPoint(relPos);

            return Vector3.Zero;
        }


        #endregion

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

        #endregion

        private HkdBreakableBody m_breakableBody;
        private bool m_ragdollDeadMode;
        public HkdBreakableBody BreakableBody
        {
            get { return m_breakableBody; }
            set
            {
                m_breakableBody = value;
                RigidBody = value;
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
            MyPhysics.Clusters.ReorderClusters(Entity.PositionComp.WorldAABB, ClusterObjectID);
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

        public HkRigidBody SwitchRigidBody(HkRigidBody newBody)
        {
            HkRigidBody old = RigidBody;
            RigidBody = newBody;
            return old;
        }

        public void SwitchToRagdollMode(bool deadMode = true, int firstRagdollSubID = 1 )
        {
            if (!Enabled) return;

            if (HavokWorld == null)
            {
                //Activate();
                //if (HavokWorld == null)
                //{
                //    Debug.Fail("Can not switch to Ragdoll mode, HavokWorld is null");
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
                body.UserObject = deadMode? this : null;

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
            Ragdoll.EnableConstraints();
            Ragdoll.Activate();
            
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

                if (!body.IsFixedOrKeyframed) body.Mass = 10f;
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
    }
}