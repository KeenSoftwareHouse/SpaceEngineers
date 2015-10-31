﻿using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRageMath;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage.Components;
using VRage.Game.Entity.UseObject;
using VRage.Input;
using Sandbox.Game.Entities.Inventory;
using VRage.Utils;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Gui;

namespace Sandbox.Game.Entities
{
    /// <summary>
    /// Manipulation tool - used for manipulating target entities. creates fixed constraint between owner's head pivot and target.
    /// </summary>
    [MyEntityType(typeof(MyObjectBuilder_ManipulationTool))]
    public class MyManipulationTool : MyEntity, IMyHandheldGunObject<MyDeviceBase>, IMyUseObject
    {
        #region Fields

        private static readonly List<MyPhysics.HitInfo> m_tmpHits = new List<MyPhysics.HitInfo>();
        private static readonly List<HkRigidBody> m_tmpBodies = new List<HkRigidBody>();
        private static readonly HashSet<MyEntity> m_manipulatedEntitites = new HashSet<MyEntity>();

        private const float TARGET_ANGULAR_DAMPING = 0.95f;
        private const float TARGET_LINEAR_DAMPING = 0.95f;
        private const float TARGET_RESTITUTION = 0f;

        private MyDefinitionId m_handItemDefId;
        private MyPhysicalItemDefinition m_physItemDef;

        private bool m_enabled = true;

        // Constraint - owner to target entity.
        private HkConstraint m_constraint;
        private HkConstraint SafeConstraint
        {
            get
            {
                if (m_constraint != null && !m_constraint.InWorld)
                    SyncTool.StopManipulation();
                return m_constraint;
            }
        }
        private HkFixedConstraintData m_fixedConstraintData;
        private MyTimeSpan m_constraintCreationTime = MyTimeSpan.Zero;
        private bool m_constraintInitialized;

        private Matrix m_headLocalPivotMatrix;
        private Matrix m_otherLocalPivotMatrix;
        private Vector3D m_otherWorldPivotOrigin;

        private const float MANIPULATION_DISTANCE_RATIO = 3f / 2.5f;
        private float m_manipulationDistance;
        private float m_limitingLinearVelocity;

        private MyEntity m_otherEntity;
        private float m_otherAngularDamping;
        private float m_otherLinearDamping;
        private float m_otherRestitution;
        private float m_otherMaxLinearVelocity;
        private float m_otherMaxAngularVelocity;

        private MyCharacterMovementEnum m_previousCharacterMovementState;

        public enum MyState
        {
            NONE,
            HOLD,
            PULL
        }
        private MyState m_state = MyState.NONE;
        public MyState State { get { return m_state; } }

        private MyPhysicsBody m_otherPhysicsBody;
        private HkRigidBody m_otherRigidBody;
        private HkMassChangerUtil m_massChange;

        public static event Action<MyEntity> ManipulationStarted;
        public static event Action<MyEntity> ManipulationStopped;

        #endregion

        #region Properties

        public MyObjectBuilder_PhysicalGunObject PhysicalObject { get; private set; }

        public bool IsShooting
        {
            get
            {
                return !m_enabled;
            }
        }

        public bool IsBlocking
        {
            get { return false; }
        }

        public int ShootDirectionUpdateTime
        {
            get { return 0; }
        }

        public bool EnabledInWorldRules
        {
            get { return true; }
        }

        public float BackkickForcePerSecond
        {
            get { return 0.0f; }
        }

        public float ShakeAmount
        {
            get { return 0f; }
            protected set { }
        }

        public MyDefinitionId DefinitionId
        {
            get { return m_handItemDefId; }
        }

        public MyDeviceBase GunBase
        {
            get { return null; }
        }

        public MyPhysicalItemDefinition PhysicalItemDefinition
        {
            get { return m_physItemDef; }
        }

        public MyCharacter Owner
        {
            get;
            private set;
        }

        public bool IsHoldingItem
        {
            get { return SafeConstraint != null; }
        }

        public float ManipulationDistance
        {
            get { return m_manipulationDistance; }
            set
            {
                m_manipulationDistance = value;
                m_limitingLinearVelocity = value + 0.5f;
            }
        }

        public MyEntity ManipulatedEntity
        {
            get { return m_otherEntity; }
        }

        private MySyncManipulationTool SyncTool { get; set; }

        public class MyCharacterVirtualPhysicsBody : MyPhysicsBody
        {
            public MyCharacterVirtualPhysicsBody(IMyEntity entity, RigidBodyFlag flags)
                : base(entity, flags)
            {
            }

            public override void OnWorldPositionChanged(object source)
            {
                // Do nothing
            }

            public void UpdatePositionAndOrientation()
            {
                MyCharacter character = Entity as MyCharacter;
                if (character == null || character.Physics == null || !character.Physics.IsInWorld)
                    return;

                if (!IsInWorld && character.Physics.IsInWorld)
                {
                    Enabled = true;
                    Activate();
                }

                if (IsInWorld)
                {
                    MatrixD headWorldMatrix = character.GetHeadMatrix(false, forceHeadBone: true);
                    Matrix rigidBodyMatrix = GetRigidBodyMatrix(headWorldMatrix);

                    if (RigidBody != null)
                    {
                        RigidBody.SetWorldMatrix(rigidBodyMatrix);
                    }

                    if (RigidBody2 != null)
                    {
                        RigidBody2.SetWorldMatrix(rigidBodyMatrix);
                    }
                }
            }

            public override void OnMotion(HkRigidBody rbo, float step)
            {
                // Do nothing
            }
        }

        private MyCharacterVirtualPhysicsBody OwnerVirtualPhysics;

        #endregion

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            m_manipulationDistance = 2.5f;
            m_limitingLinearVelocity = 4f;
            m_handItemDefId = objectBuilder.GetId();
            m_physItemDef = MyDefinitionManager.Static.GetPhysicalItemForHandItem(m_handItemDefId);
            base.Init(objectBuilder);
            Init(null, null/*PhysicalItemDefinition.Model*/, null, null, null);

            Save = false;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

            SyncTool = new MySyncManipulationTool(EntityId);

            //PhysicalObject = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>(m_handItemDefId.SubtypeName);
            //PhysicalObject.GunEntity = (MyObjectBuilder_EntityBase)objectBuilder.Clone();
            //PhysicalObject.GunEntity.EntityId = this.EntityId;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            var ob = base.GetObjectBuilder(copy);
            ob.SubtypeName = m_handItemDefId.SubtypeName;
            return ob;
        }

        protected override void Closing()
        {
            StopManipulation();
            base.Closing();
        }

        public VRageMath.Vector3 DirectionToTarget(VRageMath.Vector3D target)
        {
            return target;
        }

        public bool CanShoot(MyShootActionEnum action, long shooter, out MyGunStatusEnum status)
        {
            if (m_enabled)
                status = MyGunStatusEnum.OK;
            else
                status = MyGunStatusEnum.Cooldown;
            return status == MyGunStatusEnum.OK;
        }

        public void Shoot(MyShootActionEnum action, VRageMath.Vector3 direction, string gunAction)
        {
            m_enabled = false;

            MyState oldState = m_state;

            switch (action)
            {
                case MyShootActionEnum.PrimaryAction:
                    {
                        if (Sandbox.Game.Multiplayer.Sync.IsServer)
                        {
                            if (SafeConstraint != null)
                            {
                                SyncTool.StopManipulation();
                            }
                            else if (oldState == MyState.NONE)
                            {
                                MatrixD ownerWorldHeadMatrix = Owner.GetHeadMatrix(false, forceHeadBone: true);
                                Vector3D hitPosition;
                                MyEntity hitEntity = GetTargetEntity(ref ownerWorldHeadMatrix, out hitPosition);
                                if (hitEntity != null)
                                {
                                    SyncTool.StartManipulation(MyState.HOLD, hitEntity, hitPosition, ref ownerWorldHeadMatrix);
                                }
                            }
                        }
                    }
                    break;
                case MyShootActionEnum.SecondaryAction:
                    if (SafeConstraint != null)
                    {
                        if (Sandbox.Game.Multiplayer.Sync.IsServer)
                        {
                            if (m_state == MyState.HOLD)
                                ThrowEntity();
                            else
                                SyncTool.StopManipulation();
                        }
                    }
                    else if (oldState == MyState.NONE)
                    {
                        if (Sandbox.Game.Multiplayer.Sync.IsServer)
                        {
                            MatrixD ownerWorldHeadMatrix = Owner.GetHeadMatrix(false, forceHeadBone: true);
                            Vector3D hitPosition;
                            MyEntity hitEntity = GetTargetEntity(ref ownerWorldHeadMatrix, out hitPosition);
                            if (hitEntity != null)
                            {
                                SyncTool.StartManipulation(MyState.PULL, hitEntity, hitPosition, ref ownerWorldHeadMatrix);
                            }
                        }
                    }
                    break;
            }
        }

        public void EndShoot(MyShootActionEnum action)
        {
            m_enabled = true;
        }

        public void BeginFailReaction(MyShootActionEnum action, MyGunStatusEnum status)
        {
        }

        public void BeginFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        {
        }

        public void ShootFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        {
        }

        public int GetAmmunitionAmount()
        {
            return 0;
        }

        public void OnControlAcquired(Sandbox.Game.Entities.Character.MyCharacter owner)
        {
            Owner = owner;
        }

        public void OnControlReleased()
        {
            if (Sync.IsServer)
                SyncTool.StopManipulation();
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if (OwnerVirtualPhysics != null)
                OwnerVirtualPhysics.UpdatePositionAndOrientation();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            UpdateManipulation();
        }

        public void DrawHud(Sandbox.ModAPI.Interfaces.IMyCameraController camera, long playerId)
        {
        }

        private void ThrowEntity()
        {
            Debug.Assert(m_constraint != null);
            Debug.Assert(Sync.IsServer);

            MyEntity otherEntity = m_otherEntity;

            SyncTool.StopManipulation();

            MatrixD ownerWorldMatrix = Owner.GetHeadMatrix(false, forceHeadBone: true);
            Vector3 ownerWorldForward = ownerWorldMatrix.Forward;

            Vector3 force = 400 * ownerWorldForward * MyFakes.SIMULATION_SPEED;
            otherEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, force, null, null);
        }

        public void StartManipulation(MyState state, MyEntity otherEntity, Vector3D hitPosition, ref MatrixD ownerWorldHeadMatrix, bool fromServer = false)
        {
            Debug.Assert(m_constraintInitialized == false);

            // Commenting this out to allow picking up dead bodies and characters
            if (otherEntity is MyCharacter)
                return;

            if (Owner == null)
            {
                Debug.Assert(!fromServer, "Desync!");
                return;
            }

            if (otherEntity.Physics == null) return;

            m_otherRigidBody = otherEntity.Physics.RigidBody;
            m_otherPhysicsBody = otherEntity.Physics;

            if (otherEntity is MyCharacter)
            {
                if (!(otherEntity as MyCharacter).IsDead || !((otherEntity as MyCharacter).Components.Has<MyCharacterRagdollComponent>() && (otherEntity as MyCharacter).Components.Get<MyCharacterRagdollComponent>().IsRagdollActivated))
                {
                    Debug.Assert(!fromServer, "Desync!");
                    return;
                }

                m_otherRigidBody = (otherEntity as MyCharacter).Physics.Ragdoll.GetRootRigidBody();
            }
            // else (!otherEntity.Physics.RigidBody.InWorld) // Do we need to check if the body is in the world when this was returned byt RayCast ? Commenting this out for now..

            var characterMovementState = Owner.GetCurrentMovementState();
            if (!CanManipulate(characterMovementState))
            {
                Debug.Assert(!fromServer, "Desync!");
                return;
            }

            if (!CanManipulateEntity(otherEntity))
            {
                Debug.Assert(!fromServer, "Desync!");
                return;
            }

            m_otherRigidBody.Activate();

            Vector3D hitUp = Vector3D.Up;
            Vector3D hitRight;
            double dot = Vector3D.Dot(ownerWorldHeadMatrix.Forward, hitUp);
            if (dot == 1 || dot == -1)
            {
                hitRight = ownerWorldHeadMatrix.Right;
            }
            else
            {
                hitRight = Vector3D.Cross(ownerWorldHeadMatrix.Forward, hitUp);
                hitRight.Normalize();
            }

            Vector3D hitForward = Vector3D.Cross(hitUp, hitRight);
            hitForward.Normalize();

            // Matrix of constraint for other body in world
            MatrixD otherWorldMatrix = MatrixD.Identity;
            otherWorldMatrix.Forward = hitForward;
            otherWorldMatrix.Right = hitRight;
            otherWorldMatrix.Up = hitUp;
            otherWorldMatrix.Translation = hitPosition;

            const float headPivotOffset = 1.5f;

            MatrixD headPivotWorldMatrix = ownerWorldHeadMatrix;
            headPivotWorldMatrix.Translation = ownerWorldHeadMatrix.Translation + headPivotOffset * ownerWorldHeadMatrix.Forward;
            //float distanceToHead = (float)(headPivotWorldMatrix.Translation - otherWorldMatrix.Translation).Length();
            //distanceToHead = MathHelper.Clamp(distanceToHead, 0.6f, 2.5f);

            // Matrix of constraint for other body in local
            m_otherLocalPivotMatrix = (Matrix)(otherWorldMatrix * otherEntity.PositionComp.WorldMatrixNormalizedInv);
            m_otherWorldPivotOrigin = otherWorldMatrix.Translation;

            MatrixD ownerWorldMatrixInverse = MatrixD.Normalize(MatrixD.Invert(ownerWorldHeadMatrix));
            m_headLocalPivotMatrix = Matrix.Identity;
            m_headLocalPivotMatrix.Translation = Vector3D.Transform(headPivotWorldMatrix.Translation, ownerWorldMatrixInverse);

            HkConstraintData data;
            if (state == MyState.HOLD)
            {
                if (!fromServer)
                {
                    float mass = 0;
                    if (otherEntity is MyCubeGrid)
                    {
                        var group = MyCubeGridGroups.Static.Physical.GetGroup((otherEntity as MyCubeGrid));
                        foreach (var node in group.Nodes)
                        {
                            if (node.NodeData.IsStatic) //fixed constraint on part connected to static grid isnt good idea
                                return;
                            mass += node.NodeData.Physics.Mass;
                        }
                        mass = GetRealMass(mass);
                    }
                    else if (otherEntity is MyCharacter)
                    {
                        mass = (otherEntity as MyCharacter).Definition.Mass;
                    }
                    else 
                        mass = GetRealMass(m_otherRigidBody.Mass);

                    // Player can hold large projectile (~222kg)
                    if ((mass > 210) || ((otherEntity is MyCharacter) && (otherEntity.Physics.Mass > 210)))
                        return;
                }

                if (!CreateOwnerVirtualPhysics())
                    return;

                if (IsOwnerLocalPlayer()) 
                {
                    var controllingPlayer = Sync.Players.GetControllingPlayer(otherEntity);
                    if (controllingPlayer != null) 
                    {
                        RemoveOwnerVirtualPhysics();
                        return;
                    }

                    if (!(otherEntity is MyCharacter)) // this would cause to start controlling the dead body..
                    {
                        Sync.Players.TrySetControlledEntity(Owner.ControllerInfo.Controller.Player.Id, otherEntity);
                    }
                }

                SetMotionOnClient(otherEntity, HkMotionType.Dynamic);

                data = CreateFixedConstraintData(ref m_otherLocalPivotMatrix, headPivotOffset);
                if (otherEntity is MyCharacter)
                {
                    if (MyFakes.MANIPULATION_TOOL_VELOCITY_LIMIT)
                    {
                        HkMalleableConstraintData mcData = data as HkMalleableConstraintData;
                        mcData.Strength = 0.005f;
                    }
                    else
                    {
                        HkFixedConstraintData fcData = data as HkFixedConstraintData;
                        fcData.MaximumAngularImpulse = 0.0005f;
                        fcData.MaximumLinearImpulse = 0.0005f;
                        fcData.BreachImpulse = 0.0004f;
                    }
                }
            }
            else
            {

                if (!CreateOwnerVirtualPhysics())
                    return;

                data = CreateBallAndSocketConstraintData(ref m_otherLocalPivotMatrix, ref m_headLocalPivotMatrix);
                if (otherEntity is MyCharacter)
                {
                    HkMalleableConstraintData mcData = data as HkMalleableConstraintData;
                    mcData.Strength = 0.005f;
                }
            }

            m_otherEntity = otherEntity;
            m_otherEntity.OnClosing += OtherEntity_OnClosing;

            //m_otherAngularDamping = m_otherRigidBody.AngularDamping;
            //m_otherLinearDamping = m_otherRigidBody.LinearDamping;
            m_otherRestitution = m_otherRigidBody.Restitution;
            m_otherMaxLinearVelocity = m_otherRigidBody.MaxLinearVelocity;
            m_otherMaxAngularVelocity = m_otherRigidBody.MaxAngularVelocity;

            SetManipulated(m_otherEntity, true);
            if (state == MyState.HOLD)
            {                
                if (m_otherEntity is MyCharacter)
                {
                    foreach (var body in m_otherEntity.Physics.Ragdoll.RigidBodies)
                    {
                        //body.AngularDamping = TARGET_ANGULAR_DAMPING;
                        //body.LinearDamping = TARGET_LINEAR_DAMPING;
                        //body.Mass = 5;
                        body.Restitution = TARGET_RESTITUTION;
                        body.MaxLinearVelocity = m_limitingLinearVelocity;
                        body.MaxAngularVelocity = (float)Math.PI*0.1f;
                    }
                }
                else
                {
                    m_massChange = HkMassChangerUtil.Create(m_otherRigidBody, int.MaxValue, 1, 0.001f);
                    //m_otherRigidBody.AngularDamping = TARGET_ANGULAR_DAMPING;
                    //m_otherRigidBody.LinearDamping = TARGET_LINEAR_DAMPING;
                    m_otherRigidBody.Restitution = TARGET_RESTITUTION;
                    m_otherRigidBody.MaxLinearVelocity = m_limitingLinearVelocity;
                    m_otherRigidBody.MaxAngularVelocity = (float)Math.PI;
                }

                const float holdingTransparency = 0.4f;
                SetTransparent(otherEntity, holdingTransparency);
            }

            m_constraint = new HkConstraint(m_otherRigidBody, OwnerVirtualPhysics.RigidBody, data);

            OwnerVirtualPhysics.AddConstraint(m_constraint);

			m_constraintCreationTime = MySandboxGame.Static.UpdateTime;

            m_state = state;

            m_previousCharacterMovementState  = Owner.GetCurrentMovementState();

            if (m_state == MyState.HOLD)
                Owner.PlayCharacterAnimation("PickLumber", MyBlendOption.Immediate, MyFrameOption.PlayOnce, 0.2f, 1f);
            else
                Owner.PlayCharacterAnimation("PullLumber", MyBlendOption.Immediate, MyFrameOption.PlayOnce, 0.2f, 1f);

            m_manipulatedEntitites.Add(m_otherEntity);

            Owner.ManipulatedEntity = m_otherEntity;

            var handler = ManipulationStarted;
            if (handler != null)
                handler(m_otherEntity);
        }

        private void SetMotionOnClient(MyEntity otherEntity, HkMotionType motion)
        {
            // PetrM:TODO: Make all client grids dynamic.
            if (!Sync.IsServer
                && otherEntity.Physics != null && m_otherRigidBody != null && !m_otherRigidBody.IsDisposed
                && !(otherEntity is MyCharacter && (otherEntity as MyCharacter).Components.Has<MyCharacterRagdollComponent>() && (otherEntity as MyCharacter).Components.Get<MyCharacterRagdollComponent>().IsRagdollActivated))  // In case of picking up a ragdoll don't turn off and on the physics
            {
                otherEntity.Physics.Enabled = false;
                otherEntity.SyncFlag = false;
                m_otherRigidBody.UpdateMotionType(motion);
                otherEntity.Physics.Enabled = true;

                // HACK: This is here only because disabling and enabling physics puts constraints into inconsistent state.
                otherEntity.RaisePhysicsChanged();
            }
        }

        private void SetManipulated(MyEntity entity, bool value)
        {
            var otherBody = entity.Physics.RigidBody;
            if (entity is MyCharacter && entity.Physics.Ragdoll.IsActive)
                otherBody = entity.Physics.Ragdoll.GetRootRigidBody();
            if(otherBody == null) return;
            if (value)
            {
                otherBody.SetProperty(HkCharacterRigidBody.MANIPULATED_OBJECT, 0);
                if(entity is MyCubeGrid)
                {
                    foreach(var b in (entity as MyCubeGrid).GetBlocks())
                    {
                        if (b.FatBlock == null) continue;
                        var useObject = b.FatBlock.Components.Get<MyUseObjectsComponentBase>();
                        if(useObject == null || useObject.DetectorPhysics == null) continue;
                        (useObject.DetectorPhysics as MyPhysicsBody).RigidBody.SetProperty(HkCharacterRigidBody.MANIPULATED_OBJECT, 0);
                    }
                }
            }
            else
            {
                otherBody.RemoveProperty(HkCharacterRigidBody.MANIPULATED_OBJECT);
                if (entity is MyCubeGrid)
                {
                    foreach (var b in (entity as MyCubeGrid).GetBlocks())
                    {
                        if (b.FatBlock == null) continue;
                        var useObject = b.FatBlock.Components.Get<MyUseObjectsComponentBase>();
                        if (useObject == null || useObject.DetectorPhysics == null) continue;
                        (useObject.DetectorPhysics as MyPhysicsBody).RigidBody.RemoveProperty(HkCharacterRigidBody.MANIPULATED_OBJECT);
                    }
                }
            }
        }

        private void SetTransparent(MyEntity entity, float dithering = 0)
        {
            if (entity is MyFracturedPiece)
            {
                foreach (var id in (entity as MyFracturedPiece).Render.RenderObjectIDs)
                {
                    VRageRender.MyRenderProxy.UpdateRenderEntity(id, null, null, dithering);
                }
                return;
            }

            foreach (var c in entity.Hierarchy.Children)
            {
                VRageRender.MyRenderProxy.UpdateRenderEntity((uint)c.Container.Entity.Render.GetRenderObjectID(), null, null, dithering);
            }
            VRageRender.MyRenderProxy.UpdateRenderEntity( (uint)entity.Render.GetRenderObjectID(), null, null, dithering);
        }

        private static float GetRealMass(float physicsMass)
        {
            return MyDestructionHelper.MassFromHavok(physicsMass);
        }

        private HkConstraintData CreateFixedConstraintData(ref Matrix otherLocalMatrix, float headDistance)
        {
            m_fixedConstraintData = new HkFixedConstraintData();
            Matrix headPivotLocalMatrix;
            GetHeadPivotLocalMatrix(headDistance, out headPivotLocalMatrix);
            m_fixedConstraintData.SetInBodySpace(otherLocalMatrix, headPivotLocalMatrix, m_otherPhysicsBody, OwnerVirtualPhysics);
            if (MyFakes.MANIPULATION_TOOL_VELOCITY_LIMIT)
            {
                m_fixedConstraintData.MaximumLinearImpulse = 0.5f;
                m_fixedConstraintData.MaximumAngularImpulse = 0.5f;

                HkMalleableConstraintData mcData = new HkMalleableConstraintData();
                mcData.SetData(m_fixedConstraintData);
                mcData.Strength = 0.0001f;
                m_fixedConstraintData.Dispose();
                return mcData;
            }
            else
            {
                m_fixedConstraintData.MaximumLinearImpulse = 0.005f; //7500 * MyDestructionHelper.MASS_REDUCTION_COEF * (MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
                m_fixedConstraintData.MaximumAngularImpulse = 0.005f; //7500 * MyDestructionHelper.MASS_REDUCTION_COEF * (MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
            }
          

            return m_fixedConstraintData;

            //HkBreakableConstraintData bcData = new HkBreakableConstraintData(m_fixedConstraintData);
            //bcData.ReapplyVelocityOnBreak = false;
            //bcData.RemoveFromWorldOnBrake = true;
            //bcData.Threshold = /*100000;*/ 2500 * MyDestructionHelper.MASS_REDUCTION_COEF;

            //return bcData;
        }

        private void GetHeadPivotLocalMatrix(float headDistance, out Matrix headPivotLocalMatrix)
        {
            headPivotLocalMatrix = Matrix.Identity;
            headPivotLocalMatrix.Translation = headDistance * headPivotLocalMatrix.Forward;
        }

        private HkConstraintData CreateBallAndSocketConstraintData(ref Matrix otherLocalMatrix, ref Matrix headPivotLocalMatrix)
        {
            HkBallAndSocketConstraintData data = new HkBallAndSocketConstraintData();
            Vector3 otherPivot = otherLocalMatrix.Translation;
            Vector3 headPivot = headPivotLocalMatrix.Translation;
            data.SetInBodySpace(otherPivot, headPivot, m_otherPhysicsBody, OwnerVirtualPhysics);

            HkMalleableConstraintData mcData = new HkMalleableConstraintData();
            mcData.SetData(data);
            mcData.Strength = 0.00006f;
            data.Dispose();

            return mcData;
        }

        public void StopManipulation()
        {
            if (m_state != MyState.NONE && Owner != null)
            {
                var characterMovementState = Owner.GetCurrentMovementState();
                switch (characterMovementState)
                {
                    case MyCharacterMovementEnum.Walking:
                    case MyCharacterMovementEnum.BackWalking:
                    case MyCharacterMovementEnum.WalkingLeftFront:
                    case MyCharacterMovementEnum.WalkingRightFront:
                    case MyCharacterMovementEnum.WalkingLeftBack:
                    case MyCharacterMovementEnum.WalkingRightBack:
                    case MyCharacterMovementEnum.WalkStrafingLeft:
                    case MyCharacterMovementEnum.WalkStrafingRight:
                    case MyCharacterMovementEnum.Running:
                    case MyCharacterMovementEnum.Backrunning:
                    case MyCharacterMovementEnum.RunStrafingLeft:
                    case MyCharacterMovementEnum.RunStrafingRight:
                    case MyCharacterMovementEnum.RunningRightFront:
                    case MyCharacterMovementEnum.RunningRightBack:
                    case MyCharacterMovementEnum.RunningLeftFront:
                    case MyCharacterMovementEnum.RunningLeftBack:
                        Owner.PlayCharacterAnimation("WalkBack", MyBlendOption.Immediate, MyFrameOption.Loop, 0.2f, 1f);
                        break;
                    case MyCharacterMovementEnum.Standing:
                    case MyCharacterMovementEnum.RotatingLeft:
                    case MyCharacterMovementEnum.RotatingRight:
                    case MyCharacterMovementEnum.Flying:
                        Owner.PlayCharacterAnimation("Idle", MyBlendOption.Immediate, MyFrameOption.Loop, 0.2f, 1f);
                        break;
                }
            }


            if (m_constraint != null)
            {
                if (OwnerVirtualPhysics != null)
                    OwnerVirtualPhysics.RemoveConstraint(m_constraint);

                m_constraint.Dispose();
                m_constraint = null;
            }

            if (m_fixedConstraintData != null)
            {
                if (!m_fixedConstraintData.IsDisposed)
                    m_fixedConstraintData.Dispose();

                m_fixedConstraintData = null;
            }

            m_headLocalPivotMatrix = Matrix.Zero;
            m_otherLocalPivotMatrix = Matrix.Zero;
            if (m_otherEntity != null)
            {
                SetTransparent(m_otherEntity);

                if (m_state == MyState.HOLD)
                {
                    SetMotionOnClient(m_otherEntity, HkMotionType.Dynamic);

                    // Do not send when disconnecting
                    if (IsOwnerLocalPlayer() && !MyEntities.CloseAllowed && !(m_otherEntity is MyCharacter))
                    {
                        Sync.Players.RemoveControlledEntity(m_otherEntity);
                    }
                }

                m_manipulatedEntitites.Remove(m_otherEntity);
                var handler = ManipulationStopped;
                if (handler != null)
                    handler(m_otherEntity);

                m_otherEntity.SyncFlag = true;

                if (m_otherEntity.Physics != null && m_otherRigidBody != null && !m_otherRigidBody.IsDisposed)
                {                    
                    SetManipulated(m_otherEntity, false);
                    //m_otherRigidBody.AngularDamping = m_otherAngularDamping;
                    //m_otherRigidBody.LinearDamping = m_otherLinearDamping;
                    if (m_otherEntity is MyCharacter)
                    {
                        m_otherEntity.Physics.SetRagdollDefaults();
                    }
                    else
                    {
                        m_otherRigidBody.Restitution = m_otherRestitution;
                        m_otherRigidBody.MaxLinearVelocity = m_otherMaxLinearVelocity;
                        m_otherRigidBody.MaxAngularVelocity = m_otherMaxAngularVelocity;
                        if (m_massChange != null)
                            m_massChange.Remove();
                        m_massChange = null;
                        // Clamp output velocity
                        m_otherRigidBody.LinearVelocity = Vector3.Clamp(m_otherRigidBody.LinearVelocity, -2 * Vector3.One, 2 * Vector3.One);
                        m_otherRigidBody.AngularVelocity = Vector3.Clamp(m_otherRigidBody.AngularVelocity, -Vector3.One * (float)Math.PI, Vector3.One * (float)Math.PI);
                        if (!m_otherRigidBody.IsActive)
                            m_otherRigidBody.Activate();
                        m_otherRigidBody.EnableDeactivation = false;
                        m_otherRigidBody.EnableDeactivation = true; //resets deactivation counter                        

                    }
                    m_otherRigidBody = null;                    
                }

                m_otherEntity.OnClosing -= OtherEntity_OnClosing;
                m_otherEntity = null;                
            }

            m_constraintInitialized = false;

            RemoveOwnerVirtualPhysics();

            if (Owner != null) Owner.ManipulatedEntity = null;
            m_state = MyState.NONE;
        }

        private void UpdateManipulation()
        {
            if (m_state != MyState.NONE && SafeConstraint != null)
            {
                const float fixedConstraintMaxValue = 1000;
                const float fixedConstraintMaxDistance = 2f;
                const float ballAndSocketMaxDistance = 2f;

                MyTimeSpan constraintPrepareTime = MyTimeSpan.FromSeconds(1.0f);
				MyTimeSpan currentTimeDelta = MySandboxGame.Static.UpdateTime - m_constraintCreationTime;

                MatrixD headWorldMatrix = Owner.GetHeadMatrix(false, forceHeadBone: true);
                MatrixD worldHeadPivotMatrix = m_headLocalPivotMatrix * headWorldMatrix;
                MatrixD worldOtherPivotMatrix = m_otherLocalPivotMatrix * m_otherEntity.PositionComp.WorldMatrix;

                double length = (worldOtherPivotMatrix.Translation - worldHeadPivotMatrix.Translation).Length();
                double checkDst = m_fixedConstraintData != null ? fixedConstraintMaxDistance : ballAndSocketMaxDistance;

				if (currentTimeDelta > constraintPrepareTime)
                {
                    var characterMovementState = Owner.GetCurrentMovementState();
                    bool currentCanUseIdle = CanManipulate(characterMovementState);
                    bool previousCanUseIdle = CanManipulate(m_previousCharacterMovementState);

                    if ((!m_constraintInitialized && currentCanUseIdle) || (currentCanUseIdle && !previousCanUseIdle))
                    {
                        if (m_state == MyState.HOLD)
                            Owner.PlayCharacterAnimation("PickLumberIdle", MyBlendOption.Immediate, MyFrameOption.Loop, 0.2f, 1f);
                        else
                            Owner.PlayCharacterAnimation("PullLumberIdle", MyBlendOption.Immediate, MyFrameOption.Loop, 0.2f, 1f);
                    }

                    m_previousCharacterMovementState = characterMovementState;

                    m_constraintInitialized = true;

                    if (m_otherRigidBody != null && !MyFakes.MANIPULATION_TOOL_VELOCITY_LIMIT)
                    {
                        m_otherRigidBody.MaxLinearVelocity = m_otherMaxLinearVelocity;
                        m_otherRigidBody.MaxAngularVelocity = m_otherMaxAngularVelocity;
                    }

                    if(m_fixedConstraintData != null && (MyScreenManager.GetScreenWithFocus() is MyGuiScreenGamePlay))
                    {
                        float rotationSpeed = MyInput.Static.IsAnyShiftKeyPressed() ? - 0.01f : 0.01f;
                        var tran = m_otherLocalPivotMatrix.Translation;
                        if(MyInput.Static.IsKeyPress(MyKeys.E))
                        {
                            m_otherLocalPivotMatrix = m_otherLocalPivotMatrix * Matrix.CreateFromAxisAngle(Vector3.Up, rotationSpeed);
                            m_otherLocalPivotMatrix.Translation = tran;
                            m_fixedConstraintData.SetInBodySpace( m_otherLocalPivotMatrix,  m_headLocalPivotMatrix, m_otherPhysicsBody, OwnerVirtualPhysics);
                        }
                        if (MyInput.Static.IsKeyPress(MyKeys.Q))
                        {
                            m_otherLocalPivotMatrix = m_otherLocalPivotMatrix * Matrix.CreateFromAxisAngle(Vector3.Forward, rotationSpeed);
                            m_otherLocalPivotMatrix.Translation = tran;
                            m_fixedConstraintData.SetInBodySpace( m_otherLocalPivotMatrix,  m_headLocalPivotMatrix, m_otherPhysicsBody, OwnerVirtualPhysics);
                        }
                        if (MyInput.Static.IsKeyPress(MyKeys.R))
                        {
                            m_otherLocalPivotMatrix = m_otherLocalPivotMatrix * Matrix.CreateFromAxisAngle(Vector3.Right, rotationSpeed);
                            m_otherLocalPivotMatrix.Translation = tran;
                            m_fixedConstraintData.SetInBodySpace( m_otherLocalPivotMatrix,  m_headLocalPivotMatrix, m_otherPhysicsBody, OwnerVirtualPhysics);
                        }
                    }

                    if (m_fixedConstraintData != null && !MyFakes.MANIPULATION_TOOL_VELOCITY_LIMIT)
                    {
                        m_fixedConstraintData.MaximumAngularImpulse = fixedConstraintMaxValue;
                        m_fixedConstraintData.MaximumLinearImpulse = fixedConstraintMaxValue;

                        // Check angle between pivots
                        float upDot = Math.Abs(Vector3.Dot(worldHeadPivotMatrix.Up, worldOtherPivotMatrix.Up));
                        float rightDot = Math.Abs(Vector3.Dot(worldHeadPivotMatrix.Right, worldOtherPivotMatrix.Right));
                        if (upDot < 0.5f || rightDot < 0.5f)
                        {
                            // Synced from local player because lagged server can drop manipulated items with fast moves
                            if (!(m_otherEntity is MyCharacter) && IsOwnerLocalPlayer())
                                SyncTool.StopManipulation();

                            return;
                        }
                    }

                    // Check length between pivots
                    if (length > checkDst)
                    {
                        // Synced from local player because lagged server can drop manipulated items with fast moves
                        if (!(m_otherEntity is MyCharacter) && IsOwnerLocalPlayer())
                            SyncTool.StopManipulation();

                        return;
                    }
                }
                else
                {
                    if (m_fixedConstraintData != null && !MyFakes.MANIPULATION_TOOL_VELOCITY_LIMIT)
                    {
                        float t = (float)(currentTimeDelta.Miliseconds / constraintPrepareTime.Miliseconds);
                        t *= t;
                        t *= t; //pow4
                        float value = t * fixedConstraintMaxValue;
                        m_fixedConstraintData.MaximumAngularImpulse = value;
                        m_fixedConstraintData.MaximumLinearImpulse = value;

                        if (length > checkDst)
                        {
                            var characterMovementState = Owner.GetCurrentMovementState();
                            if (!CanManipulate(characterMovementState))
                            {
                                // Synced from local player because lagged server can drop manipulated items with fast moves
                                if (!(m_otherEntity is MyCharacter) && IsOwnerLocalPlayer())
                                    SyncTool.StopManipulation();

                                return;
                            }
                        }
                    }
                }
            }
        }

        private bool CanManipulate(MyCharacterMovementEnum characterMovementState)
        {
            if (characterMovementState == MyCharacterMovementEnum.Standing
                || characterMovementState == MyCharacterMovementEnum.RotatingLeft
                || characterMovementState == MyCharacterMovementEnum.RotatingRight
                || characterMovementState == MyCharacterMovementEnum.Crouching
                || characterMovementState == MyCharacterMovementEnum.Flying)
                return true;

            return false;
        }

        private MyEntity GetTargetEntity(ref MatrixD ownerWorldHeadMatrix, out Vector3D hitPosition)
        {
            hitPosition = Vector3D.MaxValue;

            if (Owner == null)
                return null;

            Vector3D ownerPosition = ownerWorldHeadMatrix.Translation;
            Vector3 ownerForward = ownerWorldHeadMatrix.Forward;

            Vector3D rayStart = ownerPosition;
            Vector3D rayEnd = rayStart + m_manipulationDistance * ownerForward;

            m_tmpHits.Clear();
            MyPhysics.CastRay(rayStart, rayEnd, m_tmpHits, MyPhysics.ObjectDetectionCollisionLayer);

            foreach (var hitInfo in m_tmpHits)
            {
                if (hitInfo.HkHitInfo.GetHitEntity() == Owner)
                {
                    continue;
                }
                else
                {
                    hitPosition = hitInfo.Position;
                    return hitInfo.HkHitInfo.GetHitEntity() as MyEntity;
                }
            }
           
            return null;
        }

        private bool CanManipulateEntity(MyEntity entity)
        {
            if (entity is MyVoxelMap)
                return false;
            else if (entity is MyCubeGrid && ((MyCubeGrid)entity).IsStatic)
                return false;
            else if (m_manipulatedEntitites.Contains(entity))
                return false;            

            return true;
        }

        public static bool IsEntityManipulated(MyEntity entity)
        {
            return m_manipulatedEntitites.Contains(entity);
        }

        private void OtherEntity_OnClosing(MyEntity obj)
        {
            StopManipulation();
        }

        public void StartManipulationSynced(MyState myState, MyEntity spawned, Vector3D position, ref MatrixD ownerWorldHeadMatrix)
        {
            Debug.Assert(Sync.IsServer);
            SyncTool.StartManipulation(myState, spawned, position, ref ownerWorldHeadMatrix);
        }

        public void StopManipulationSynced()
        {
            Debug.Assert(Sync.IsServer);
            SyncTool.StopManipulation();
        }

        float IMyUseObject.InteractiveDistance
        {
            get { return 0; }
        }

        MatrixD IMyUseObject.ActivationMatrix
        {
            get { return WorldMatrix; }
        }

        MatrixD IMyUseObject.WorldMatrix
        {
            get { return WorldMatrix; }
        }

        int IMyUseObject.RenderObjectID
        {
            get { return Render.RenderObjectIDs != null ? (int)Render.RenderObjectIDs[0] : 0; }
        }

        bool IMyUseObject.ShowOverlay
        {
            get { return false; }
        }

        UseActionEnum IMyUseObject.SupportedActions
        {
            get { return UseActionEnum.None; }
        }

        bool IMyUseObject.ContinuousUsage
        {
            get { return false; }
        }

        void IMyUseObject.Use(UseActionEnum actionEnum, IMyEntity user)
        {
            if (actionEnum == UseActionEnum.Manipulate && user is MyCharacter)
            {
                TakeManipulatedItem();
            }
        }

        private void TakeManipulatedItem()
        {
            if (State == MyManipulationTool.MyState.HOLD && Owner != null)
            {                
                var inventoryAggregate = Owner.Components.Get<MyInventoryBase>() as MyInventoryAggregate;
                
                if (inventoryAggregate == null)
                {
                    return;
                }
                var inventory = inventoryAggregate.GetInventory(MyStringId.Get("Inventory")) as MyInventory;
                
                if (ManipulatedEntity != null && inventory != null)
                {
                    inventory.AddEntity(ManipulatedEntity, false);
                }
            }

        }
        

        MyActionDescription IMyUseObject.GetActionInfo(UseActionEnum actionEnum)
        {
            return new MyActionDescription()
            {
                //Text = MyStringId.
                FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.USE), Name },
                //IsTextControlHint = true,
                //JoystickFormatParams = new object[] { MyControllerHelper.GetCodeForControl(MyMedievalBindingCreator.CX_CHARACTER, MyControlsMedieval.USE) },
            };
        }

        bool IMyUseObject.HandleInput()
        {
            return true;
        }

        void IMyUseObject.OnSelectionLost()
        {
           
        }

        bool IMyUseObject.PlayIndicatorSound
        {
            get { return false; }
        }

        private bool CreateOwnerVirtualPhysics()
        {
            if (Owner == null)
                return false;

            OwnerVirtualPhysics = new MyCharacterVirtualPhysicsBody(Owner, RigidBodyFlag.RBF_KINEMATIC);
            var massProperties = HkInertiaTensorComputer.ComputeSphereVolumeMassProperties(0.1f, MyPerGameSettings.Destruction ? MyDestructionHelper.MassToHavok(Owner.Definition.Mass) : Owner.Definition.Mass);
            HkShape sh = new HkSphereShape(0.1f);
            OwnerVirtualPhysics.InitialSolverDeactivation = HkSolverDeactivation.Off;
            MatrixD headWorldMatrix = Owner.GetHeadMatrix(false, forceHeadBone: true);
            OwnerVirtualPhysics.CreateFromCollisionObject(sh, Vector3.Zero, headWorldMatrix, massProperties, Sandbox.Engine.Physics.MyPhysics.NoCollisionLayer);
            OwnerVirtualPhysics.RigidBody.EnableDeactivation = false;
            // Character ray casts includes also NoCollision layer shapes so setup property for ignoring the body
            OwnerVirtualPhysics.RigidBody.SetProperty(HkCharacterRigidBody.MANIPULATED_OBJECT, 0);
            sh.RemoveReference();

            OwnerVirtualPhysics.Enabled = true;

            return true;
        }

        private void RemoveOwnerVirtualPhysics()
        {
            if (OwnerVirtualPhysics == null)
                return;

            OwnerVirtualPhysics.RigidBody.RemoveProperty(HkCharacterRigidBody.MANIPULATED_OBJECT);
            OwnerVirtualPhysics.Close();
            OwnerVirtualPhysics = null;
        }

        private bool IsOwnerLocalPlayer()
        {
            return Owner != null
                && Owner.ControllerInfo.Controller != null
                && Owner.ControllerInfo.Controller.Player.IsLocalPlayer;
        }
    }
}
