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
    using VRage.Game.Components;
    using VRage.Trace;

    /// <summary>
    /// Abstract engine physics body object.
    /// </summary>
    public partial class MyPhysicsBody
    {
        #region Ragdoll

        /// This System Collision ID is used for ragdoll in non-dead mode to avoid collision with character's rigid body
        public int RagdollSystemGroupCollisionFilterID { get; private set; }

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
                    CloseRagdollMode(HavokWorld);
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

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyPhysicsBody.SwitchToRagdollMode");
                MyLog.Default.WriteLine("MyPhysicsBody.SwitchToRagdollMode");
            }

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

            Ragdoll.GenerateRigidBodiesCollisionFilters(deadMode ? MyPhysics.CollisionLayers.CharacterCollisionLayer : MyPhysics.CollisionLayers.RagdollCollisionLayer, RagdollSystemGroupCollisionFilterID, firstRagdollSubID);

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

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyPhysicsBody.SwitchToRagdollMode - FINISHED");
                MyLog.Default.WriteLine("MyPhysicsBody.SwitchToRagdollMode - FINISHED");
            }
        }

        private void ActivateRagdoll(Matrix worldMatrix)
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyPhysicsBody.ActivateRagdoll");
                MyLog.Default.WriteLine("MyPhysicsBody.ActivateRagdoll");
            }

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
                        
            // Because after cluster's reorder, the bodies can collide!            
            HavokWorld.AddRagdoll(Ragdoll);
            DisableRagdollBodiesCollisions();

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyPhysicsBody.ActivateRagdoll - FINISHED");
                MyLog.Default.WriteLine("MyPhysicsBody.ActivateRagdoll - FINISHED");
            }
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
            CloseRagdollMode(this.HavokWorld);
        }

        public void CloseRagdollMode(HkWorld world)
        {
            Debug.Assert((IsRagdollModeActive && world != null) || !IsRagdollModeActive, "Can not deactivate ragdoll, because the world is null");
            if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("MyPhysicsBody.CloseRagdollMode");
            if (IsRagdollModeActive && world != null)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("CloseRagdollMode");
                foreach (var body in Ragdoll.RigidBodies)
                {
                    body.UserObject = null;
                }

                Debug.Assert(Ragdoll.InWorld, "Can not remove ragdoll when it's not in the world");
                Ragdoll.Deactivate();
                world.RemoveRagdoll(Ragdoll);
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
            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyPhysicsBody.SetRagdollDefaults");
                MyLog.Default.WriteLine("MyPhysicsBody.SetRagdollDefaults");
            }

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

                Debug.Assert(body.Mass != 0.0f, "Body's mass was set to 0!");
                body.AngularDamping = 0.005f;
                body.LinearDamping = 0.0f;
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
                    constraintData.MaxFrictionTorque = MyPerGameSettings.Destruction ? MyDestructionHelper.MassToHavok(0.5f) : 3f;
                }
            }

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyPhysicsBody.SetRagdollDefaults FINISHED");
                MyLog.Default.WriteLine("MyPhysicsBody.SetRagdollDefaults FINISHED");
            }
        }

        public bool ReactivateRagdoll { get; set; }


        public bool SwitchToRagdollModeOnActivate { get; set; }

        internal void DisableRagdollBodiesCollisions()
        {
            Debug.Assert(Ragdoll != null, "Ragdoll is null!");
            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                var world = HavokWorld;
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.StaticCollisionLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.VoxelCollisionLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.DefaultCollisionLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.CharacterCollisionLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.CharacterNetworkCollisionLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.DynamicDoubledCollisionLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.KinematicDoubledCollisionLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.DebrisCollisionLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.FloatingObjectCollisionLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.LightFloatingObjectCollisionLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.GravityPhantomLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.VirtualMassLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.NoCollisionLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.ExplosionRaycastLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.CollisionLayerWithoutCharacter), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.CollideWithStaticLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.CollectorCollisionLayer), "Collision isn't disabled!");
                Debug.Assert(!world.IsCollisionEnabled(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.AmmoLayer), "Collision isn't disabled!");
            }
            if (Ragdoll != null)
            {
                foreach (var body in Ragdoll.RigidBodies)
                {
                    var info = HkGroupFilter.CalcFilterInfo(MyPhysics.CollisionLayers.RagdollCollisionLayer, 0, 0, 0);
                    //HavokWorld.DisableCollisionsBetween(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.RagdollCollisionLayer);
                    //HavokWorld.DisableCollisionsBetween(MyPhysics.CollisionLayers.RagdollCollisionLayer, MyPhysics.CollisionLayers.CharacterCollisionLayer);
                    body.SetCollisionFilterInfo(info);
                    body.LinearVelocity = Vector3.Zero;// Character.Physics.LinearVelocity;
                    body.AngularVelocity = Vector3.Zero;
                    HavokWorld.RefreshCollisionFilterOnEntity(body);
                    Debug.Assert(body.InWorld, "Body isn't in world!");
                    Debug.Assert(MyPhysics.CollisionLayers.RagdollCollisionLayer == HkGroupFilter.GetLayerFromFilterInfo(body.GetCollisionFilterInfo()), "Body is in wrong layer!");
                }
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
                Vector3 weightedForce = force.Value * rigidBody.Mass / ragdoll.Mass;
                ApplyForceWorld(weightedForce, position, rigidBody);
            }
        }
        #endregion

    }
}