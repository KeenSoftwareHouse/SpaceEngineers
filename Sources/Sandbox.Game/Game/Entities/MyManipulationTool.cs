using Havok;
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
using VRage.Game.Components;
using VRage.Game.Entity.UseObject;
using VRage.Input;
using Sandbox.Game.Entities.Inventory;
using VRage.Utils;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Gui;
using Sandbox.Game.Components;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Import;
using VRage.Network;
using Sandbox.Engine.Multiplayer;
using VRage.Game.ModAPI.Interfaces;
using VRage.Profiler;
using VRage.Serialization;
using VRageRender.Import;

namespace Sandbox.Game.Entities
{
    /// <summary>
    /// Manipulation tool - used for manipulating target entities. creates fixed constraint between owner's head pivot and target.
    /// </summary>
    [StaticEventOwner]
    [MyEntityType(typeof(MyObjectBuilder_ManipulationTool))]
    public class MyManipulationTool : MyEntity, IMyHandheldGunObject<MyDeviceBase>, IMyUseObject
    {
        #region Fields

        private static readonly List<MyPhysics.HitInfo> m_tmpHits = new List<MyPhysics.HitInfo>();
        private static readonly List<HkRigidBody> m_tmpBodies = new List<HkRigidBody>();
        private static readonly HashSet<MyEntity> m_manipulatedEntitites = new HashSet<MyEntity>();
        private static readonly List<HkBodyCollision> m_tmpCollisions = new List<HkBodyCollision>();
        private static readonly List<MyEntity> m_tmpEntities = new List<MyEntity>();

        private const float HEAD_PIVOT_OFFSET = 1.5f;
        private const float HEAD_PIVOT_MOVE_DISTANCE = 0.4f;

        private const float TARGET_ANGULAR_DAMPING = 0.95f;
        private const float TARGET_LINEAR_DAMPING = 0.95f;
        private const float TARGET_RESTITUTION = 0f;

        private const float PULL_MALLEABLE_CONSTRAINT_STRENGTH = 0.005f;

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
                    StopManipulationSyncedInternal();

                return m_constraint;
            }
        }
        private HkFixedConstraintData m_fixedConstraintData;
        private MyTimeSpan m_constraintCreationTime = MyTimeSpan.Zero;
        private bool m_constraintInitialized;

        private Matrix m_headLocalPivotMatrix;
        private Matrix m_otherLocalPivotMatrix;

        private const float MANIPULATION_DISTANCE_RATIO = 3f / 2.5f;
        private float m_manipulationDistance;
        private float m_limitingLinearVelocity;

        private MyEntity m_otherEntity;

        private struct MyManipulationInitData
        {
            public long OtherEntityId;
            public MyState State;
        }
        private MyManipulationInitData? m_manipulationInitData;

        private struct MyOtherPhysicsData
        {
            public float m_otherAngularDamping;
            public float m_otherLinearDamping;
            public float m_otherRestitution;
            public float m_otherMaxLinearVelocity;
            public float m_otherMaxAngularVelocity;
            public HkMotionType m_motionType;

            public bool m_entitySyncFlag;

            public HkMassChangerUtil m_massChange;
            public HkRigidBody m_massChangeBody;
        }
        private Dictionary<long, MyOtherPhysicsData> m_otherPhysicsData = new Dictionary<long, MyOtherPhysicsData>();

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

        public static event Action<MyEntity> ManipulationStarted;
        public static event Action<MyEntity> ManipulationStopped;

        private bool m_manipulationStopped;

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

        public bool ForceAnimationInsteadOfIK { get { return true; } }

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

        public new MyDefinitionId DefinitionId
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

            public override void OnMotion(HkRigidBody rbo, float step, bool fromParent)
            {
                // Do nothing
            }

            public void GetCharacterLinearAndAngularVelocity(out Vector3 linearVelocity, out Vector3 angularVelocity)
            {
                linearVelocity = Vector3.Zero;
                angularVelocity = Vector3.Zero;

                MyCharacter character = Entity as MyCharacter;
                if (character != null && character.Physics != null)
                {
                    linearVelocity = character.Physics.LinearVelocity;
                    angularVelocity = character.Physics.AngularVelocity;
                }
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

            var ob = objectBuilder as MyObjectBuilder_ManipulationTool;
            var state = (MyState)ob.State;
            if (!Sync.IsServer && state != MyState.NONE && ob.OtherEntityId != 0)
            {
                m_manipulationInitData = new MyManipulationInitData() { OtherEntityId = ob.OtherEntityId, State = state };

                m_headLocalPivotMatrix = Matrix.CreateFromQuaternion(ob.HeadLocalPivotOrientation);
                m_headLocalPivotMatrix.Translation = ob.HeadLocalPivotPosition;

                m_otherLocalPivotMatrix = Matrix.CreateFromQuaternion(ob.OtherLocalPivotOrientation);
                m_otherLocalPivotMatrix.Translation = ob.OtherLocalPivotPosition;
            }

            Save = false;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            var ob = (MyObjectBuilder_ManipulationTool)base.GetObjectBuilder(copy);
            ob.SubtypeName = m_handItemDefId.SubtypeName;
            ob.State = (byte)m_state;
            if (m_state != MyState.NONE)
            {
                ob.OtherEntityId = m_otherEntity.EntityId;

                ob.HeadLocalPivotPosition = m_headLocalPivotMatrix.Translation;
                ob.HeadLocalPivotOrientation = Quaternion.CreateFromRotationMatrix(m_headLocalPivotMatrix);

                ob.OtherLocalPivotPosition = m_otherLocalPivotMatrix.Translation;
                ob.OtherLocalPivotOrientation = Quaternion.CreateFromRotationMatrix(m_otherLocalPivotMatrix);
            }

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

        public void Shoot(MyShootActionEnum action, VRageMath.Vector3 direction, Vector3D? overrideWeaponPos, string gunAction)
        {
            m_enabled = false;

            MyState oldState = m_state;

            switch (action)
            {
                case MyShootActionEnum.PrimaryAction:
                    {
                        if (Sync.IsServer)
                        {
                            if (SafeConstraint != null)
                            {
                                StopManipulationSyncedInternal();
                            }
                            else if (oldState == MyState.NONE)
                            {
                                MatrixD ownerWorldHeadMatrix = Owner.GetHeadMatrix(false, forceHeadBone: true);
                                Vector3D hitPosition;
                                MyEntity hitEntity = GetTargetEntity(ref ownerWorldHeadMatrix, out hitPosition);
                                if (hitEntity != null)
                                {
                                    StartManipulationSyncedInternal(MyState.HOLD, hitEntity, hitPosition, ref ownerWorldHeadMatrix);
                                }
                            }
                        }
                    }
                    break;
                case MyShootActionEnum.SecondaryAction:
                    if (SafeConstraint != null)
                    {
                        if (Sync.IsServer)
                        {
                            if (m_state == MyState.HOLD)
                                ThrowEntity();
                            else
                                StopManipulationSyncedInternal();
                        }
                    }
                    else if (oldState == MyState.NONE)
                    {
                        if (Sync.IsServer)
                        {
                            MatrixD ownerWorldHeadMatrix = Owner.GetHeadMatrix(false, forceHeadBone: true);
                            Vector3D hitPosition;
                            MyEntity hitEntity = GetTargetEntity(ref ownerWorldHeadMatrix, out hitPosition);
                            if (hitEntity != null)
                            {
                                StartManipulationSyncedInternal(MyState.PULL, hitEntity, hitPosition, ref ownerWorldHeadMatrix);
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
                StopManipulationSyncedInternal();
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            InitManipulationOnClient();

            if (OwnerVirtualPhysics != null)
                OwnerVirtualPhysics.UpdatePositionAndOrientation();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            UpdateManipulation();
        }

        public void DrawHud(IMyCameraController camera, long playerId)
        {
        }

        private void ThrowEntity()
        {
            Debug.Assert(m_constraint != null);
            Debug.Assert(Sync.IsServer);

            MyEntity otherEntity = m_otherEntity;

            StopManipulationSyncedInternal();

            MatrixD ownerWorldMatrix = Owner.GetHeadMatrix(false, forceHeadBone: true);
            Vector3 ownerWorldForward = ownerWorldMatrix.Forward;

            Vector3 force = 400 * ownerWorldForward * MyFakes.SIMULATION_SPEED;
            otherEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, force, null, null);
        }

        public bool StartManipulation(MyState state, MyEntity otherEntity, Vector3D hitPosition, ref MatrixD ownerWorldHeadMatrix, bool fromServer = false)
        {
            Debug.Assert(m_constraintInitialized == false);

            if (otherEntity.MarkedForClose)
                return false;

            // Commenting this out to allow picking up dead bodies and characters
            if (otherEntity is MyCharacter)
                return false;

            if (Owner == null)
            {
                Debug.Assert(!fromServer, "Desync!");
                return false;
            }

            if (otherEntity.Physics == null) 
                return false;

            if (otherEntity is MyCharacter)
            {
                if (!(otherEntity as MyCharacter).IsDead || !((otherEntity as MyCharacter).Components.Has<MyCharacterRagdollComponent>() && (otherEntity as MyCharacter).Components.Get<MyCharacterRagdollComponent>().IsRagdollActivated))
                {
                    Debug.Assert(!fromServer, "Desync!");
                    return false;
                }
            }
            // else (!otherEntity.Physics.RigidBody.InWorld) // Do we need to check if the body is in the world when this was returned byt RayCast ? Commenting this out for now..

            var characterMovementState = Owner.GetCurrentMovementState();
            if (!fromServer && !CanManipulate(characterMovementState))
                return false;

            if (!CanManipulateEntity(otherEntity, fromServer: fromServer))
            {
                Debug.Assert(!fromServer, "Desync!");
                return false;
            }

            float distanceFromHeadToOther = (float)(ownerWorldHeadMatrix.Translation - hitPosition).Length();
            float headPivotOffset = HEAD_PIVOT_OFFSET;
            if (distanceFromHeadToOther < HEAD_PIVOT_OFFSET + HEAD_PIVOT_MOVE_DISTANCE)
            {
                headPivotOffset = distanceFromHeadToOther - HEAD_PIVOT_MOVE_DISTANCE;
            }

            if (!fromServer && headPivotOffset < 0.4f)
                return false;

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
            // Matrix of constraint for other body in local
            m_otherLocalPivotMatrix = (Matrix)(otherWorldMatrix * otherEntity.PositionComp.WorldMatrixNormalizedInv);

            m_headLocalPivotMatrix = Matrix.Identity;
            m_headLocalPivotMatrix.Translation = headPivotOffset * m_headLocalPivotMatrix.Forward;

            return StartManipulationInternal(state, otherEntity, fromServer);
        }

        private bool StartManipulationInternal(MyState state, MyEntity otherEntity, bool fromServer)
        {
            m_manipulationStopped = false;

            m_otherEntity = otherEntity;
            MyPhysicsBody physicsBody = m_otherEntity.Physics as MyPhysicsBody;
            m_otherRigidBody = physicsBody.RigidBody;
            m_otherPhysicsBody = m_otherEntity.Physics as MyPhysicsBody;

            if (otherEntity is MyCharacter)
            {
                Clean();
                return false;
            }

            m_otherRigidBody.Activate();

            HkConstraintData data;
            if (state == MyState.HOLD)
            {
                if (!fromServer)
                {
                    if (!IsManipulatedEntityMassValid())
                    {
                        Clean();
                        return false;
                    }
                }

                if (!CreateOwnerVirtualPhysics())
                {
                    Clean();
                    return false;
                }

                if (IsOwnerLocalPlayer())
                {
                    var controllingPlayer = Sync.Players.GetControllingPlayer(m_otherEntity);
                    if (controllingPlayer != null)
                    {
                        Clean();
                        return false;
                    }

                    if (!(m_otherEntity is MyCharacter)) // this would cause to start controlling the dead body..
                    {
                        Sync.Players.TrySetControlledEntity(Owner.ControllerInfo.Controller.Player.Id, m_otherEntity);
                    }
                }

                const float holdingTransparency = 0.4f;

                if (m_otherEntity is MyCharacter)
                {
                    SetManipulated(m_otherEntity, true);

                    foreach (var body in (m_otherEntity.Physics as MyPhysicsBody).Ragdoll.RigidBodies)
                    {
                        //body.AngularDamping = TARGET_ANGULAR_DAMPING;
                        //body.LinearDamping = TARGET_LINEAR_DAMPING;
                        //body.Mass = 5;
                        body.Restitution = TARGET_RESTITUTION;
                        body.MaxLinearVelocity = m_limitingLinearVelocity;
                        body.MaxAngularVelocity = (float)Math.PI * 0.1f;
                    }

                    //TODO: don't know if this is needed for character
                    SetMotionOnClient(m_otherEntity, HkMotionType.Dynamic, false);
                }
                else if (m_otherEntity is MyCubeGrid)
                {
                    var group = MyCubeGridGroups.Static.Physical.GetGroup((m_otherEntity as MyCubeGrid));
                    if (group != null)
                    {
                        // Need to fill group entities because SetMotionOnClient raises physics change and this reacreates the group!
                        m_tmpEntities.Clear();
                        foreach (var node in group.Nodes)
                            m_tmpEntities.Add(node.NodeData);

                        foreach (var entity in m_tmpEntities)
                        {
                            SetManipulated(entity, true);
                            SetStartManipulationHoldRigidBodyData(entity);
                            // Must be before setting of event OnPhysicsChanged!
                            SetMotionOnClient(entity, HkMotionType.Dynamic, false);
                        }

                        // Set events
                        foreach (var entity in m_tmpEntities)
                        {
                            entity.OnMarkForClose += OtherEntity_OnMarkForClose;
                            entity.OnClosing += OtherEntity_OnClosing;
                            entity.OnPhysicsChanged += OtherEntity_OnPhysicsChanged;
                        }
                        m_tmpEntities.Clear();
                    }
                }
                else
                {
                    SetManipulated(m_otherEntity, true);
                    SetStartManipulationHoldRigidBodyData(m_otherEntity);
                    SetMotionOnClient(m_otherEntity, HkMotionType.Dynamic, false);
                }

                SetTransparent(m_otherEntity, holdingTransparency);

                data = CreateFixedConstraintData();
                if (m_otherEntity is MyCharacter)
                {
                    if (MyFakes.MANIPULATION_TOOL_VELOCITY_LIMIT)
                    {
                        HkMalleableConstraintData mcData = data as HkMalleableConstraintData;
                        mcData.Strength = PULL_MALLEABLE_CONSTRAINT_STRENGTH;
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
                {
                    Clean();
                    return false;
                }

                data = CreateBallAndSocketConstraintData();
                if (m_otherEntity is MyCharacter)
                {
                    HkMalleableConstraintData mcData = data as HkMalleableConstraintData;
                    mcData.Strength = PULL_MALLEABLE_CONSTRAINT_STRENGTH;
                }
                else if (m_otherEntity is MyCubeGrid)
                {
                    MyCubeGrid grid = m_otherEntity as MyCubeGrid;
                    bool hasStatic = false;
                    float totalMass = CalculateGridGroupMass(grid, GridLinkTypeEnum.Physical, out hasStatic);
                    if (!hasStatic && MyRopeComponent.Static != null && MyRopeComponent.Static.HasGridAttachedRope(grid))
                    {
                        // Higher strength is needed for manipulating of grid connected with rope to other grid. Manipulated grid has to be lightweight.
                        float gridMass = GetRealMass(m_otherRigidBody.Mass);
                        float coef = gridMass / totalMass;
                        if (coef < 0.2f)
                        {
                            HkMalleableConstraintData mcData = data as HkMalleableConstraintData;
                            float strength = PULL_MALLEABLE_CONSTRAINT_STRENGTH + coef * (mcData.Strength - PULL_MALLEABLE_CONSTRAINT_STRENGTH);
                            mcData.Strength = strength;
                        }
                    }
                }
            }

            m_otherEntity.OnMarkForClose += OtherEntity_OnMarkForClose;
            m_otherEntity.OnClosing += OtherEntity_OnClosing;
            m_otherEntity.OnPhysicsChanged += OtherEntity_OnPhysicsChanged;

            m_constraint = new HkConstraint(m_otherRigidBody, OwnerVirtualPhysics.RigidBody, data);

            OwnerVirtualPhysics.AddConstraint(m_constraint);

            m_constraintCreationTime = MySandboxGame.Static.UpdateTime;

            m_state = state;

            m_previousCharacterMovementState = Owner.GetCurrentMovementState();

            if (m_state == MyState.HOLD)
            {
                if (!MyFakes.DISABLE_MANIPULATION_TOOL_HOLD_VOXEL_CONTACT)
                    Owner.PlayCharacterAnimation("PickLumber", MyBlendOption.Immediate, MyFrameOption.PlayOnce, 0.2f, 1f);
            }
            else
            {
                Owner.PlayCharacterAnimation("PullLumber", MyBlendOption.Immediate, MyFrameOption.PlayOnce, 0.2f, 1f);
            }

            m_manipulatedEntitites.Add(m_otherEntity);

            Owner.ManipulatedEntity = m_otherEntity;

            var handler = ManipulationStarted;
            if (handler != null)
                handler(m_otherEntity);

            return true;
        }

        private void SetStartManipulationHoldRigidBodyData(MyEntity entity)
        {
            if (entity.Physics != null && entity.Physics.RigidBody != null && !entity.Physics.RigidBody.IsDisposed)
            {
                var rigidBody = entity.Physics.RigidBody;
                MyOtherPhysicsData otherPhysData = new MyOtherPhysicsData();
                //otherPhysData.m_otherAngularDamping = rigidBody.AngularDamping;
                //otherPhysData.m_otherLinearDamping = rigidBody.LinearDamping;
                otherPhysData.m_otherRestitution = rigidBody.Restitution;
                otherPhysData.m_otherMaxLinearVelocity = rigidBody.MaxLinearVelocity;
                otherPhysData.m_otherMaxAngularVelocity = rigidBody.MaxAngularVelocity;
                otherPhysData.m_motionType = rigidBody.GetMotionType();
                otherPhysData.m_entitySyncFlag = entity.SyncFlag;
                otherPhysData.m_massChange = HkMassChangerUtil.Create(rigidBody, int.MaxValue, 1, 0.001f);
                otherPhysData.m_massChangeBody = rigidBody;

                m_otherPhysicsData.Add(entity.EntityId, otherPhysData);

                //rigidBody.AngularDamping = TARGET_ANGULAR_DAMPING;
                //rigidBody.LinearDamping = TARGET_LINEAR_DAMPING;
                rigidBody.Restitution = TARGET_RESTITUTION;
                rigidBody.MaxLinearVelocity = m_limitingLinearVelocity;
                rigidBody.MaxAngularVelocity = (float)Math.PI;
            }
        }

        private void SetStopManipulationRigidBodyData(MyEntity entity)
        {
            bool writeData = !entity.MarkedForClose && entity.Physics != null && entity.Physics.RigidBody != null && !entity.Physics.RigidBody.IsDisposed;

            MyOtherPhysicsData otherPhysicsData;
            if (m_otherPhysicsData.TryGetValue(entity.EntityId, out otherPhysicsData))
            {
                if (otherPhysicsData.m_massChange != null)
                    otherPhysicsData.m_massChange.Remove();
                otherPhysicsData.m_massChange = null;
                otherPhysicsData.m_massChangeBody = null;

                if (writeData)
                {
                    var rigidBody = entity.Physics.RigidBody;
                    rigidBody.Restitution = otherPhysicsData.m_otherRestitution;
                    rigidBody.MaxLinearVelocity = otherPhysicsData.m_otherMaxLinearVelocity;
                    rigidBody.MaxAngularVelocity = otherPhysicsData.m_otherMaxAngularVelocity;
                }
            }

            if (writeData)
            {
                var rigidBody = entity.Physics.RigidBody;
                if (m_state == MyState.HOLD)
                {
                    Vector3 linearVelocity, angularVelocity;
                    OwnerVirtualPhysics.GetCharacterLinearAndAngularVelocity(out linearVelocity, out angularVelocity);
                    rigidBody.LinearVelocity = linearVelocity;
                    rigidBody.AngularVelocity = angularVelocity;
                }
                else
                {
                    rigidBody.LinearVelocity = Vector3.Clamp(rigidBody.LinearVelocity, -2 * Vector3.One, 2 * Vector3.One);
                    rigidBody.AngularVelocity = Vector3.Clamp(rigidBody.AngularVelocity, -Vector3.One * (float)Math.PI, Vector3.One * (float)Math.PI);
                }

                if (!rigidBody.IsActive)
                    rigidBody.Activate();
                rigidBody.EnableDeactivation = false;
                rigidBody.EnableDeactivation = true; //resets deactivation counter                        
            }
        }

        private void InitManipulationOnClient()
        {
            if (!Sync.IsServer && m_manipulationInitData != null)
            {
                MyEntity otherEntity;
                if (MyEntities.TryGetEntityById(m_manipulationInitData.Value.OtherEntityId, out otherEntity) && otherEntity != null)
                {
                    StartManipulationInternal(m_manipulationInitData.Value.State, otherEntity, true);

                    m_manipulationInitData = null;
                }
            }
        }

        private void SetMotionOnClient(MyEntity otherEntity, HkMotionType motion, bool syncFlag)
        {
            // PetrM:TODO: Make all client grids dynamic.
            if (!Sync.IsServer
                && otherEntity.Physics != null && m_otherRigidBody != null && !m_otherRigidBody.IsDisposed
                && !(otherEntity is MyCharacter && (otherEntity as MyCharacter).Components.Has<MyCharacterRagdollComponent>() && (otherEntity as MyCharacter).Components.Get<MyCharacterRagdollComponent>().IsRagdollActivated))  // In case of picking up a ragdoll don't turn off and on the physics
            {
                otherEntity.Physics.Enabled = false;
                otherEntity.SyncFlag = syncFlag;
                m_otherRigidBody.UpdateMotionType(motion);
                otherEntity.Physics.Enabled = true;

                // HACK: This is here only because disabling and enabling physics puts constraints into inconsistent state.
                otherEntity.RaisePhysicsChanged();
            }
        }

        private void SetManipulated(MyEntity entity, bool value)
        {
            var otherBody = entity.Physics.RigidBody;
            if (entity is MyCharacter && entity.GetPhysicsBody().Ragdoll.IsActive)
                otherBody = entity.GetPhysicsBody().Ragdoll.GetRootRigidBody();
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
            if (entity.MarkedForClose)
                return;

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

        private HkConstraintData CreateFixedConstraintData()
        {
            m_fixedConstraintData = new HkFixedConstraintData();
            Matrix headPivotLocalMatrix = m_headLocalPivotMatrix;
            m_fixedConstraintData.SetInBodySpace(m_otherLocalPivotMatrix, m_headLocalPivotMatrix, m_otherPhysicsBody, OwnerVirtualPhysics);
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
                m_fixedConstraintData.MaximumLinearImpulse = 0.005f; //7500 * MyDestructionHelper.MASS_REDUCTION_COEF * (VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
                m_fixedConstraintData.MaximumAngularImpulse = 0.005f; //7500 * MyDestructionHelper.MASS_REDUCTION_COEF * (VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyFakes.SIMULATION_SPEED);
            }
          

            return m_fixedConstraintData;

            //HkBreakableConstraintData bcData = new HkBreakableConstraintData(m_fixedConstraintData);
            //bcData.ReapplyVelocityOnBreak = false;
            //bcData.RemoveFromWorldOnBrake = true;
            //bcData.Threshold = /*100000;*/ 2500 * MyDestructionHelper.MASS_REDUCTION_COEF;

            //return bcData;
        }

        private HkConstraintData CreateBallAndSocketConstraintData()
        {
            HkBallAndSocketConstraintData data = new HkBallAndSocketConstraintData();
            Vector3 otherPivot = m_otherLocalPivotMatrix.Translation;
            Vector3 headPivot = m_headLocalPivotMatrix.Translation;
            data.SetInBodySpace(otherPivot, headPivot, m_otherPhysicsBody, OwnerVirtualPhysics);

            HkMalleableConstraintData mcData = new HkMalleableConstraintData();
            mcData.SetData(data);
            mcData.Strength = 0.00006f;
            data.Dispose();

            return mcData;
        }

        private void StopManipulation()
        {
            // Manipulation stopped might be recursivelly called when SetMotionOnClient is called which can raise physics change event!
            if (m_manipulationStopped)
                return;

            m_manipulationStopped = true;

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
                m_otherEntity.OnMarkForClose -= OtherEntity_OnMarkForClose;
                m_otherEntity.OnClosing -= OtherEntity_OnClosing;
                m_otherEntity.OnPhysicsChanged -= OtherEntity_OnPhysicsChanged;

                SetTransparent(m_otherEntity);

                if (m_state == MyState.HOLD)
                {
                    // Do not send when disconnecting
                    if (IsOwnerLocalPlayer() && !(m_otherEntity is MyCharacter))
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
                    if (m_otherEntity is MyCharacter)
                    {
                        SetManipulated(m_otherEntity, false);
                        m_otherEntity.GetPhysicsBody().SetRagdollDefaults();
                        //TODO: don't know if this is needed for character
                        //SetMotionOnClient(m_otherEntity, HkMotionType.Dynamic, false);
                    }
                    else if (m_otherEntity is MyCubeGrid)
                    {
                        var group = MyCubeGridGroups.Static.Physical.GetGroup((m_otherEntity as MyCubeGrid));
                        if (group != null)
                        {
                            m_tmpEntities.Clear();
                            foreach (var node in group.Nodes)
                                m_tmpEntities.Add(node.NodeData);

                            foreach (var entity in m_tmpEntities)
                            {
                                entity.OnMarkForClose -= OtherEntity_OnMarkForClose;
                                entity.OnClosing -= OtherEntity_OnClosing;
                                entity.OnPhysicsChanged -= OtherEntity_OnPhysicsChanged;
                            }

                            foreach (var entity in m_tmpEntities)
                            {
                                SetManipulated(entity, false);
                                SetStopManipulationRigidBodyData(entity);
                                if (m_state == MyState.HOLD)
                                {
                                    MyOtherPhysicsData otherPhysicsData;
                                    if (m_otherPhysicsData.TryGetValue(entity.EntityId, out otherPhysicsData))
                                        SetMotionOnClient(entity, otherPhysicsData.m_motionType, otherPhysicsData.m_entitySyncFlag);
                                }
                            }
                            m_tmpEntities.Clear();
                        }
                    }
                    else
                    {
                        SetManipulated(m_otherEntity, false);
                        SetStopManipulationRigidBodyData(m_otherEntity);

                        if (m_state == MyState.HOLD)
                        {
                            MyOtherPhysicsData otherPhysicsData;
                            if (m_otherPhysicsData.TryGetValue(m_otherEntity.EntityId, out otherPhysicsData))
                                SetMotionOnClient(m_otherEntity, otherPhysicsData.m_motionType, otherPhysicsData.m_entitySyncFlag);
                        }
                    }
                    m_otherRigidBody = null;                    
                }

                m_otherEntity = null;                
            }

            Clean();
        }

        private void Clean()
        {
            RemoveOwnerVirtualPhysics();

            if (Owner != null)
                Owner.ManipulatedEntity = null;

            m_state = MyState.NONE;
            m_constraintInitialized = false;
            m_manipulationInitData = null;
            m_otherEntity = null;
            m_otherRigidBody = null;
            m_otherPhysicsData.Clear();
            m_headLocalPivotMatrix = Matrix.Zero;
            m_otherLocalPivotMatrix = Matrix.Zero;

            Debug.Assert(m_constraint == null);
            Debug.Assert(m_fixedConstraintData == null);
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

                if (m_constraintInitialized || currentTimeDelta > constraintPrepareTime)
                {
                    var characterMovementState = Owner.GetCurrentMovementState();
                    bool currentCanUseIdle = CanManipulate(characterMovementState);
                    bool previousCanUseIdle = CanManipulate(m_previousCharacterMovementState);

                    if ((!m_constraintInitialized && currentCanUseIdle) || (currentCanUseIdle && !previousCanUseIdle))
                    {
                        if (m_state == MyState.HOLD)
                        {
                            if (!MyFakes.DISABLE_MANIPULATION_TOOL_HOLD_VOXEL_CONTACT)
                                Owner.PlayCharacterAnimation("PickLumberIdle", MyBlendOption.Immediate, MyFrameOption.Loop, 0.2f, 1f);
                        }
                        else
                            Owner.PlayCharacterAnimation("PullLumberIdle", MyBlendOption.Immediate, MyFrameOption.Loop, 0.2f, 1f);
                    }

                    m_previousCharacterMovementState = characterMovementState;

                    m_constraintInitialized = true;

                    if (m_otherRigidBody != null && !MyFakes.MANIPULATION_TOOL_VELOCITY_LIMIT)
                    {
                        if (m_otherEntity is MyCubeGrid)
                        {
                            var group = MyCubeGridGroups.Static.Physical.GetGroup((m_otherEntity as MyCubeGrid));
                            foreach (var node in group.Nodes)
                            {
                                if (node.NodeData.Physics != null && node.NodeData.Physics.RigidBody != null && !node.NodeData.Physics.RigidBody.IsDisposed)
                                {
                                    var rigidBody = node.NodeData.Physics.RigidBody;
                                    MyOtherPhysicsData otherPhysicsData;
                                    if (m_otherPhysicsData.TryGetValue(node.NodeData.EntityId, out otherPhysicsData))
                                    {
                                        rigidBody.MaxLinearVelocity = otherPhysicsData.m_otherMaxLinearVelocity;
                                        rigidBody.MaxAngularVelocity = otherPhysicsData.m_otherMaxAngularVelocity;
                                    }
                                }
                            }
                        }
                        else
                        {
                            MyOtherPhysicsData otherPhysicsData;
                            if (m_otherPhysicsData.TryGetValue(m_otherEntity.EntityId, out otherPhysicsData))
                            {
                                m_otherRigidBody.MaxLinearVelocity = otherPhysicsData.m_otherMaxLinearVelocity;
                                m_otherRigidBody.MaxAngularVelocity = otherPhysicsData.m_otherMaxAngularVelocity;
                            }
                        }
                    }

                    UpdateInput();

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
                                StopManipulationSyncedInternal();

                            return;
                        }
                    }

                    // Check length between pivots
                    if (length > checkDst)
                    {
                        // Synced from local player because lagged server can drop manipulated items with fast moves
                        if (!(m_otherEntity is MyCharacter) && IsOwnerLocalPlayer())
                        {
                            StopManipulationSyncedInternal();
                            return;
                        }
                    }
                }
                else
                {
                    if (m_fixedConstraintData != null && !MyFakes.MANIPULATION_TOOL_VELOCITY_LIMIT)
                    {
                        float t = (float)(currentTimeDelta.Milliseconds / constraintPrepareTime.Milliseconds);
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
                                {
                                    StopManipulationSyncedInternal();
                                    return;
                                }
                            }
                        }
                        else if (m_state == MyState.HOLD && length < 0.05f)
                        {
                            m_constraintInitialized = true;
                        }
                    }
                }

                if (m_state == MyState.HOLD && Sync.IsServer)
                {
                    if (!IsManipulatedEntityMassValid())
                    {
                        StopManipulationSyncedInternal();
                        return;
                    }
                }

                ProfilerShort.Begin("CheckOwnerPenetration");
                if (IsOwnerLocalPlayer() && CheckManipulatedObjectPenetration())
                {
                    ProfilerShort.End();
                    StopManipulationSyncedInternal();
                    return;
                }

                ProfilerShort.End();
            }
        }

        private void UpdateInput()
        {
            // Check if you can manipulate the tool (if you are the controller)
            bool isInputAllowed = IsOwnerLocalPlayer();

            if (isInputAllowed && m_fixedConstraintData != null && (MyScreenManager.GetScreenWithFocus() is MyGuiScreenGamePlay))
            {

                float rotationSpeed = MyInput.Static.IsAnyShiftKeyPressed() ? -0.04f : 0.04f;

                var tran = m_otherLocalPivotMatrix.Translation;
                Quaternion? rotation = null;
                if (MyInput.Static.IsKeyPress(MyKeys.E))
                {
                    m_otherLocalPivotMatrix = m_otherLocalPivotMatrix * Matrix.CreateFromAxisAngle(Vector3.Up, rotationSpeed);
                    m_otherLocalPivotMatrix.Translation = tran;
                    rotation = Quaternion.CreateFromRotationMatrix(m_otherLocalPivotMatrix);
                    m_fixedConstraintData.SetInBodySpace(m_otherLocalPivotMatrix, m_headLocalPivotMatrix, m_otherPhysicsBody, OwnerVirtualPhysics);
                }
                if (MyInput.Static.IsKeyPress(MyKeys.Q))
                {
                    m_otherLocalPivotMatrix = m_otherLocalPivotMatrix * Matrix.CreateFromAxisAngle(Vector3.Forward, rotationSpeed);
                    m_otherLocalPivotMatrix.Translation = tran;
                    rotation = Quaternion.CreateFromRotationMatrix(m_otherLocalPivotMatrix);
                    m_fixedConstraintData.SetInBodySpace(m_otherLocalPivotMatrix, m_headLocalPivotMatrix, m_otherPhysicsBody, OwnerVirtualPhysics);
                }
                if (MyInput.Static.IsKeyPress(MyKeys.R))
                {
                    m_otherLocalPivotMatrix = m_otherLocalPivotMatrix * Matrix.CreateFromAxisAngle(Vector3.Right, rotationSpeed);
                    m_otherLocalPivotMatrix.Translation = tran;
                    rotation = Quaternion.CreateFromRotationMatrix(m_otherLocalPivotMatrix);
                    m_fixedConstraintData.SetInBodySpace(m_otherLocalPivotMatrix, m_headLocalPivotMatrix, m_otherPhysicsBody, OwnerVirtualPhysics);
                }

                if (rotation != null)
                {
                    MyMultiplayer.RaiseStaticEvent(s => MyManipulationTool.OnRotateManipulatedEntity, EntityId, rotation.Value);
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

        /// <summary>
        /// Checks penetration of manipulated entity with other entities.
        /// </summary>
        private bool CheckManipulatedObjectPenetration()
        {
            if (IsOwnerLocalPlayer() && (m_state == MyState.PULL || m_state == MyState.HOLD))
            {
                if (m_otherEntity is MyCubeGrid)
                {
                    var grid = m_otherEntity as MyCubeGrid;
                    var gridGroup = MyCubeGridGroups.Static.Logical.GetGroup(grid);

                    m_tmpEntities.Clear();
                    var otherSphere = m_otherEntity.PositionComp.WorldVolume;
                    MyGamePruningStructure.GetAllEntitiesInSphere(ref otherSphere, m_tmpEntities, MyEntityQueryType.Dynamic);

                    foreach (var entity in m_tmpEntities)
                    {
                        var otherGrid = entity as MyCubeGrid;
                        if (otherGrid == null) continue;
                        if (MyCubeGridGroups.Static.Logical.GetGroup(otherGrid) != gridGroup) continue;

                        if (CheckManipulatedObjectPenetration(otherGrid, null, Owner, m_constraintInitialized))
                            return true;
                    }

                    m_tmpEntities.Clear();
                }
                else
                {
                    return CheckManipulatedObjectPenetration(m_otherEntity, m_otherRigidBody, Owner, m_constraintInitialized);
                }
            }

            return false;
        }

        private static bool CheckManipulatedObjectPenetration(MyEntity otherEntity, HkRigidBody otherRigidBody, MyCharacter owner, bool constraintInitialized)
        {
            if (otherRigidBody == null)
            {
                if (otherEntity.Physics != null && otherEntity.Physics.RigidBody != null)
                    otherRigidBody = otherEntity.Physics.RigidBody;
                else
                    return false;
            }

            Vector3D otherTranslation = otherEntity.WorldMatrix.Translation;
            Quaternion otherRotation = Quaternion.CreateFromRotationMatrix(otherEntity.WorldMatrix);
            Debug.Assert(m_tmpCollisions.Count == 0);
            MyPhysics.GetPenetrationsShape(otherRigidBody.GetShape(), ref otherTranslation, ref otherRotation, m_tmpCollisions, MyPhysics.CollisionLayers.DefaultCollisionLayer);

            try
            {
                foreach (var collision in m_tmpCollisions)
                {
                    var entity = collision.GetCollisionEntity();
                    if (entity == otherEntity)
                        continue;
                    else if (entity == owner && owner.IsPlayer)
                        return true;

                    if (constraintInitialized)
                    {
                        if (IsEntityManipulated(entity as MyEntity))
                            return true;
                        else if ((entity is MyCharacter) && entity != owner)
                            return true;
                        else if (MyFakes.DISABLE_MANIPULATION_TOOL_HOLD_VOXEL_CONTACT && (entity is MyVoxelMap))
                            return true;
                        else if ((otherEntity is MyCubeGrid) && (entity is MyCubeGrid)) // Force stop manipulation when other entity is on rope with contact entity.
                        {
                            if (MyRopeComponent.AreGridsConnected(otherEntity as MyCubeGrid, entity as MyCubeGrid))
                                return true;
                        }
                    }
                }
            }
            finally
            {
                m_tmpCollisions.Clear();
            }

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
            MyPhysics.CastRay(rayStart, rayEnd, m_tmpHits, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);

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

        private static bool CanManipulateEntity(MyEntity entity, bool fromServer = false)
        {
            if (entity.Physics == null || entity.Physics.RigidBody == null || (!fromServer && entity.Physics.IsStatic))
                return false;
            else if (entity is MyInventoryBagEntity) // currently only loot bags..
                return false;
            else if (IsEntityManipulated(entity))
                return false;            

            return true;
        }

        public static bool IsEntityManipulated(MyEntity entity)
        {
            return m_manipulatedEntitites.Contains(entity);
        }

        void OtherEntity_OnMarkForClose(MyEntity obj)
        {
            if (Sync.IsServer)
                StopManipulationSyncedInternal();
            else 
                StopManipulation();
        }

        private void OtherEntity_OnClosing(MyEntity obj)
        {
            if (Sync.IsServer)
                StopManipulationSyncedInternal();
            else
                StopManipulation();
        }

        void OtherEntity_OnPhysicsChanged(MyEntity obj)
        {
            if (Sync.IsServer)
                StopManipulationSyncedInternal();
        }

        public void StartManipulationSynced(MyState myState, MyEntity spawned, Vector3D position, ref MatrixD ownerWorldHeadMatrix)
        {
            Debug.Assert(Sync.IsServer);
            StartManipulationSyncedInternal(myState, spawned, position, ref ownerWorldHeadMatrix);
        }

        public void StopManipulationSynced()
        {
            Debug.Assert(Sync.IsServer);
            StopManipulationSyncedInternal();
        }

        IMyEntity IMyUseObject.Owner
        {
            get { return this; }
        }

        MyModelDummy IMyUseObject.Dummy
        {
            get { return null; }
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

        void IMyUseObject.SetRenderID(uint id)
        {
        }

        int IMyUseObject.InstanceID
        {
            get { return -1; }
        }

        void IMyUseObject.SetInstanceID(int id)
        {
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
                var inventory = inventoryAggregate.GetInventory(MyStringHash.Get("Inventory")) as MyInventory;
                
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
            OwnerVirtualPhysics.CreateFromCollisionObject(sh, Vector3.Zero, headWorldMatrix, massProperties, Sandbox.Engine.Physics.MyPhysics.CollisionLayers.NoCollisionLayer);
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

        private void RotateManipulatedEntity(ref Quaternion rotation)
        {
            if (m_state == MyState.HOLD && IsHoldingItem && m_fixedConstraintData != null) 
            {
                var tran = m_otherLocalPivotMatrix.Translation;
                m_otherLocalPivotMatrix = Matrix.CreateFromQuaternion(rotation);
                m_otherLocalPivotMatrix.Translation = tran;
                m_fixedConstraintData.SetInBodySpace(m_otherLocalPivotMatrix, m_headLocalPivotMatrix, m_otherPhysicsBody, OwnerVirtualPhysics);
            }
        }

        private bool IsManipulatedEntityMassValid()
        {
            float mass = 0;
            if (m_otherEntity is MyCubeGrid)
            {
                bool hasStatic = false;
                mass = CalculateGridGroupMass(m_otherEntity as MyCubeGrid, GridLinkTypeEnum.Physical, out hasStatic);
                if (hasStatic)
                    return false;
            }
            else if (m_otherEntity is MyCharacter)
                mass = (m_otherEntity as MyCharacter).Definition.Mass;
            else
                mass = GetRealMass(m_otherRigidBody.Mass);

            // Player can't hold large projectile
            if ((mass > 210) || ((m_otherEntity is MyCharacter) && (m_otherEntity.Physics.Mass > 210)))
                return false;

            return true;
        }

        private static float CalculateGridGroupMass(MyCubeGrid grid, GridLinkTypeEnum gridLinkType, out bool hasStaticGrid)
        {
            hasStaticGrid = false;

            var groupNodes = MyCubeGridGroups.Static.GetGroups(gridLinkType).GetGroupNodes(grid);
            if (groupNodes == null)
            {
                hasStaticGrid = grid.IsStatic;
                return GetRealMass(grid.Physics.Mass);
            }

            float mass = 0f;
            foreach (var node in groupNodes)
            {
                if (node.IsStatic)
                {
                    hasStaticGrid = true;
                    continue;
                }
                mass += node.Physics.Mass;
            }

            mass = GetRealMass(mass);
            return mass;
        }

        public int CurrentAmmunition { set; get; }
        public int CurrentMagazineAmmunition { set; get; }

        private void StartManipulationSyncedInternal(MyManipulationTool.MyState state, MyEntity otherEntity, Vector3D hitPosition, ref MatrixD ownerWorldHeadMatrix)
        {
            bool manipulationStarted = StartManipulation(state, otherEntity, hitPosition, ref ownerWorldHeadMatrix);
            if (manipulationStarted && IsHoldingItem)
            {
                MyMultiplayer.RaiseStaticEvent(s => MyManipulationTool.OnStartManipulation, EntityId, state, otherEntity.EntityId, hitPosition, ownerWorldHeadMatrix);
            }
        }

        private void StopManipulationSyncedInternal()
        {
            StopManipulation();
            MyMultiplayer.RaiseStaticEvent(s => MyManipulationTool.OnStopManipulation, EntityId);
        }

        [Event, Reliable, Broadcast]
        static void OnStartManipulation(long entityId, MyManipulationTool.MyState state, long otherEntityId, Vector3D hitPosition, MatrixD ownerWorldHeadMatrix)
        {
            MyManipulationTool manipulationTool;
            MyEntity otherEntity;
            if (MyEntities.TryGetEntityById(entityId, out manipulationTool) && MyEntities.TryGetEntityById(otherEntityId, out otherEntity))
            {
                manipulationTool.StartManipulation(state, otherEntity, hitPosition, ref ownerWorldHeadMatrix, true);
            }
        }

        [Event, Reliable, Server, BroadcastExcept]
        static void OnStopManipulation(long entityId)
        {
            if (Sync.IsServer)
                return;

            MyManipulationTool manipulationTool;
            if (MyEntities.TryGetEntityById(entityId, out manipulationTool))
            {
                manipulationTool.StopManipulation();
            }
        }

        [Event, Reliable, Server, BroadcastExcept]
        static void OnRotateManipulatedEntity(long entityId, [Serialize(MyPrimitiveFlags.Normalized)] Quaternion rotation)
        {
            if (MyEventContext.Current.IsLocallyInvoked)
                return;

            MyManipulationTool manipulationTool;
            if (MyEntities.TryGetEntityById(entityId, out manipulationTool))
            {
                manipulationTool.RotateManipulatedEntity(ref rotation);
            }
        }

        public void UpdateSoundEmitter(){ }
    }
}
