using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using VRageMath;
using VRageRender;
using Sandbox.Game.Entities.Interfaces;
using System.Diagnostics;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Multiplayer;
using SteamSDK;

using System.Reflection;
using Sandbox.Common;

using VRage.Groups;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Terminal.Controls;
using VRage.Utils;
using Sandbox.Definitions;
using VRageMath.PackedVector;
using Sandbox.Game.Localization;
using VRage;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.ModAPI;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_LandingGear))]
    class MyLandingGear : MyFunctionalBlock, IMyLandingGear, Sandbox.ModAPI.IMyLandingGear
    {
        private MySoundPair m_lockSound;
        private MySoundPair m_unlockSound;
        private MySoundPair m_failedAttachSound;

        // This value is taken somewhere from Havok
        const float MaxSolverImpulse = 1e8f;
        static List<HkBodyCollision> m_penetrations = new List<HkBodyCollision>();

        Matrix[] m_lockPositions;

        public Matrix[] LockPositions { get { return m_lockPositions; } }

        HkConstraint m_constraint;

        private HkConstraint SafeConstraint
        {
            get
            {
                if (m_constraint != null && !m_constraint.InWorld)
                {
                    Detach();
                }
                return m_constraint;
            }
        }

        LandingGearMode m_lockMode = LandingGearMode.Unlocked;

        Action<IMyEntity> m_physicsChangedHandler;

        IMyEntity m_attachedTo;

        private bool m_needsToRetryLock = false;
        private int m_autolockTimer = 0;

        private new MySyncLandingGear SyncObject;

        public LandingGearMode LockMode
        {
            get
            {
                return m_lockMode;
            }
            private set
            {
                if (m_lockMode != value)
                {
                    var old = m_lockMode;

                    m_lockMode = value;
                    UpdateEmissivity();
                    UpdateText();

                    var handler = LockModeChanged;
                    if (handler != null)
                    {
                        handler(this, old);
                    }
                }
            }
        }
        public bool IsLocked { get { return LockMode == LandingGearMode.Locked; } }

        public event LockModeChangedHandler LockModeChanged;
        private float m_breakForce;
        private bool m_autoLock;

        static MyLandingGear()
        {
            var stateWriter = new MyTerminalControl<MyLandingGear>.WriterDelegate((b, sb) => b.WriteLockStateValue(sb));

            var lockBtn = new MyTerminalControlButton<MyLandingGear>("Lock", MySpaceTexts.BlockActionTitle_Lock, MySpaceTexts.Blank, (b) => b.RequestLandingGearLock());
            lockBtn.Enabled = (b) => b.IsWorking;
            lockBtn.EnableAction(MyTerminalActionIcons.TOGGLE, (MyStringId?)null, stateWriter);
            MyTerminalControlFactory.AddControl(lockBtn);

            var unlockBtn = new MyTerminalControlButton<MyLandingGear>("Unlock", MySpaceTexts.BlockActionTitle_Unlock, MySpaceTexts.Blank, (b) => b.RequestLandingGearUnlock());
            unlockBtn.Enabled = (b) => b.IsWorking;
            unlockBtn.EnableAction(MyTerminalActionIcons.TOGGLE, (MyStringId?)null, stateWriter);
            MyTerminalControlFactory.AddControl(unlockBtn);

            var title = MyTexts.Get(MySpaceTexts.BlockActionTitle_SwitchLock);
            MyTerminalAction<MyLandingGear> switchLockAction = new MyTerminalAction<MyLandingGear>("SwitchLock", title, MyTerminalActionIcons.TOGGLE);
            switchLockAction.Action = (b) => b.RequestLandingGearSwitch();
            switchLockAction.Writer = stateWriter;
            MyTerminalControlFactory.AddAction(switchLockAction);

            var autoLock = new MyTerminalControlCheckbox<MyLandingGear>("Autolock", MySpaceTexts.BlockPropertyTitle_LandGearAutoLock, MySpaceTexts.Blank);
            autoLock.Getter = (b) => b.m_autoLock;
            autoLock.Setter = (b, v) => b.SyncObject.SendAutoLockChange(v);
            autoLock.EnableAction();
            MyTerminalControlFactory.AddControl(autoLock);

            if (MyFakes.LANDING_GEAR_BREAKABLE)
            {
                var brakeForce = new MyTerminalControlSlider<MyLandingGear>("BreakForce", MySpaceTexts.BlockPropertyTitle_BreakForce, MySpaceTexts.BlockPropertyDescription_BreakForce);
                brakeForce.Getter = (x) => x.BreakForce;
                brakeForce.Setter = (x, v) => x.SyncObject.SendBrakeForceChange(v);
                brakeForce.DefaultValue = 1;
                brakeForce.Writer = (x, result) =>
                {
                    if (x.BreakForce >= MaxSolverImpulse) result.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyValue_MotorAngleUnlimited));
                    else MyValueFormatter.AppendForceInBestUnit(x.BreakForce, result);
                };
                brakeForce.Normalizer = (b, v) => ThresholdToRatio(v);
                brakeForce.Denormalizer = (b, v) => RatioToThreshold(v);
                brakeForce.EnableActions();
                MyTerminalControlFactory.AddControl(brakeForce);
            }
        }

        public bool IsBreakable
        {
            get { return BreakForce < MaxSolverImpulse; }
        }

        public void RequestLandingGearSwitch()
        {
            if (LockMode == LandingGearMode.Locked)
                RequestLandingGearUnlock();
            else
                RequestLandingGearLock();
        }

        public void RequestLandingGearLock()
        {
            if (LockMode == LandingGearMode.ReadyToLock)
                RequestLock(true);
        }

        public void RequestLandingGearUnlock()
        {
            if (LockMode == LandingGearMode.Locked)
                RequestLock(false);
        }

        public float BreakForce
        {
            get { return m_breakForce; }
            set
            {
                if (m_breakForce != value)
                {
                    bool wasBreakable = IsBreakable;
                    m_breakForce = value;

                    if (wasBreakable != IsBreakable)
                    {
                        m_breakForce = value;
                        ResetLockConstraint(LockMode == LandingGearMode.Locked);
                        RaisePropertiesChanged();
                    }
                    else if (IsBreakable)
                    {
                        UpdateBrakeThreshold();
                        RaisePropertiesChanged();
                    }
                }
            }
        }

        private static float RatioToThreshold(float ratio)
        {
            return ratio >= 1 ? MaxSolverImpulse : MathHelper.InterpLog(ratio, 500f, MaxSolverImpulse);
        }

        private static float ThresholdToRatio(float threshold)
        {
            return threshold >= MaxSolverImpulse ? 1 : MathHelper.InterpLogInv(threshold, 500f, MaxSolverImpulse);
        }

        private void UpdateBrakeThreshold()
        {
            if (SafeConstraint != null && m_constraint.ConstraintData is HkBreakableConstraintData)
            {
                ((HkBreakableConstraintData)m_constraint.ConstraintData).Threshold = BreakForce;
                if (this.m_attachedTo != null && this.m_attachedTo.Physics != null)
                    ((MyPhysicsBody)this.m_attachedTo.Physics).RigidBody.Activate();
            }
        }

        private bool CanAutoLock { get { return m_autoLock && m_autolockTimer == 0; } }

        public bool AutoLock
        {
            get { return m_autoLock; }
            set
            {
                m_autoLock = value;
                m_autolockTimer = 0;
                UpdateEmissivity();
                RaisePropertiesChanged();
            }
        }

        public MyLandingGear()
        {
            m_physicsChangedHandler = new Action<IMyEntity>(PhysicsChanged);
            m_soundEmitter = new MyEntity3DSoundEmitter(this);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            if (BlockDefinition is MyLandingGearDefinition)
            {
                var landingGearDefinition = (MyLandingGearDefinition)BlockDefinition;
                m_lockSound = new MySoundPair(landingGearDefinition.LockSound);
                m_unlockSound = new MySoundPair(landingGearDefinition.UnlockSound);
                m_failedAttachSound = new MySoundPair(landingGearDefinition.FailedAttachSound);
            }
            else
            {
                m_lockSound = new MySoundPair("ShipLandGearOn");
                m_unlockSound = new MySoundPair("ShipLandGearOff");
                m_failedAttachSound = new MySoundPair("ShipLandGearNothing01");
            }

            SyncObject = new MySyncLandingGear(this);

            Flags |= EntityFlags.NeedsUpdate10 | EntityFlags.NeedsUpdate;
            LoadDummies();
            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            var builder = objectBuilder as MyObjectBuilder_LandingGear;
            if (builder.IsLocked)
            {
                // This mode will be applied during one-time update, when we have scene prepared.
                LockMode = LandingGearMode.Locked;
                m_needsToRetryLock = true;
            }

            BreakForce = RatioToThreshold(builder.BrakeForce);
            AutoLock = builder.AutoLock;

            IsWorkingChanged += MyLandingGear_IsWorkingChanged;
            UpdateText();
            AddDebugRenderComponent(new Components.MyDebugRenderComponentLandingGear(this));
        }

        void MyLandingGear_IsWorkingChanged(MyCubeBlock obj)
        {
            RaisePropertiesChanged();
            UpdateEmissivity();
        }

        public override void ContactPointCallback(ref MyGridContactInfo info)
        {
            if (info.CollidingEntity != null && m_attachedTo == info.CollidingEntity)
            {
                info.EnableDeformation = false;
                info.EnableParticles = false;
            }
        }

        private void LoadDummies()
        {
            m_lockPositions = Model.Dummies.Where(s => s.Key.ToLower().Contains("gear_lock")).Select(s => s.Value.Matrix).ToArray();
        }

        public override void OnModelChange()
        {
            base.OnModelChange();
            UpdateEmissivity();
        }


        public void GetBoxFromMatrix(MatrixD m, out Vector3 halfExtents, out Vector3D position, out Quaternion orientation)
        {
            var world = MatrixD.Normalize(m) * this.WorldMatrix;
            orientation = Quaternion.CreateFromRotationMatrix(world);
            halfExtents = Vector3.Abs(m.Scale) / 2;
            position = world.Translation;
        }

        private MyEntity FindBody(out Vector3D pivot)
        {
            pivot = Vector3D.Zero;
            if (CubeGrid.Physics == null)
                return null;
            Quaternion orientation;
            Vector3 halfExtents;
            foreach (var m in m_lockPositions)
            {
                GetBoxFromMatrix(m, out halfExtents, out pivot, out orientation);
                try
                {
                    halfExtents *= new Vector3(2.0f, 1.0f, 2.0f);
                    orientation.Normalize();
                    MyPhysics.GetPenetrationsBox(ref halfExtents, ref pivot, ref orientation, m_penetrations, MyPhysics.ObjectDetectionCollisionLayer);
                    var boxShape = new HkBoxShape(halfExtents);
                    Matrix tranform = Matrix.CreateFromQuaternion(orientation);
                    //tranform.Translation = pivot;
                    //MyOrientedBoundingBoxD obb = new MyOrientedBoundingBoxD(new BoundingBoxD(-halfExtents, halfExtents),tranform);
                    //tranform.Translation = Vector3D.Zero;
                    //MyRenderProxy.DebugDrawOBB(obb, Color.Red, 1, false, false);

                    foreach (var obj in m_penetrations)
                    {
                        var entity = obj.GetCollisionEntity() as MyEntity;

                        if (entity == null)// || entity.Parent != null)
                            continue;

                        if(entity.Physics.WeldInfo.Children.Count > 0)
                        {
                            Matrix t2;
                            foreach(var child in entity.Physics.WeldInfo.Children)
                            {
                                var childEnt = child.Entity as MyEntity;
                                t2 = childEnt.Physics.WeldInfo.Transform * entity.Physics.RigidBody.GetRigidBodyMatrix();
                                t2.Translation = entity.Physics.ClusterToWorld(t2.Translation);
                                //obb = new MyOrientedBoundingBoxD((BoundingBoxD)childEnt.PositionComp.LocalAABB, t2);
                                //MyRenderProxy.DebugDrawOBB(obb, Color.Green, 1, false, false);
                                t2.Translation = t2.Translation -pivot;
                                if(MyPhysics.IsPenetratingShapeShape(boxShape, ref tranform, child.WeldedRigidBody.GetShape(), ref t2) )
                                    if(
                                    CanAttachTo(obj, child.Entity as MyEntity))
                                {
                                    return child.Entity as MyEntity;
                                }
                            }
                            t2 = entity.Physics.RigidBody.GetRigidBodyMatrix();
                            t2.Translation = entity.Physics.ClusterToWorld(t2.Translation) - pivot;

                            if (MyPhysics.IsPenetratingShapeShape(boxShape, ref tranform, entity.Physics.GetShape(), ref t2)
                                && CanAttachTo(obj, entity))
                            {
                                return entity;
                            }

                        }
                        else if(CanAttachTo(obj, entity))
                            return entity;
                    }
                }
                finally
                {
                    m_penetrations.Clear();
                }
            }
            return null;
        }

        private bool CanAttachTo(HkBodyCollision obj, MyEntity entity)
        {
            if (entity == CubeGrid)
            {
                return false;
            }

            var grid = entity as MyCubeGrid;
            if (grid == null)
            {
                grid = entity.Parent as MyCubeGrid;
                if ((grid != null && !grid.IsStatic && obj.Body.IsFixedOrKeyframed))
                    return false;
            }

            // Dont want to lock to fixed/keyframed object
            if (entity is Sandbox.Game.Entities.Character.MyCharacter)
            {
                return false;
            }
            return true;
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (InScene)
            {
                switch (LockMode)
                {
                    case LandingGearMode.Locked:
                        MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.ForestGreen, Color.White);
                        break;

                    case LandingGearMode.ReadyToLock:
                        MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Goldenrod, Color.White);
                        break;

                    case LandingGearMode.Unlocked:
                        if (CanAutoLock && Enabled)
                            MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.SteelBlue, Color.SteelBlue);
                        else
                            MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Black, Color.Black);
                        break;
                }
            }
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_LockState));
            WriteLockStateValue(DetailedInfo);
            RaisePropertiesChanged();
        }

        private void WriteLockStateValue(StringBuilder sb)
        {
            if (LockMode == LandingGearMode.Locked)
                sb.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyValue_Locked));
            else if (LockMode == LandingGearMode.ReadyToLock)
                sb.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyValue_ReadyToLock));
            else
                sb.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyValue_Unlocked));
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var gear = (MyObjectBuilder_LandingGear)base.GetObjectBuilderCubeBlock(copy);
            gear.IsLocked = LockMode == LandingGearMode.Locked;
            gear.BrakeForce = ThresholdToRatio(BreakForce);
            gear.AutoLock = AutoLock;
            gear.LockSound = m_lockSound.ToString();
            gear.UnlockSound = m_unlockSound.ToString();
            gear.FailedAttachSound = m_failedAttachSound.ToString();
            return gear;
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateEmissivity();
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);
            //for when new grid is created from splitting an existing grid
            EnqueueRetryLock();
            Detach();
        }

        public override void UpdateBeforeSimulation()
        {
            RetryLock();
            base.UpdateBeforeSimulation();
        }

        public override void UpdateAfterSimulation10()
        {
            // TODO: change to phantom
            base.UpdateAfterSimulation10();


            if (LockMode != LandingGearMode.Locked)
            {
                if (IsWorking)
                {
                    Vector3D pivot;
                    var entity = FindBody(out pivot);
                    if (entity != null)
                    {
                        if (CanAutoLock && Sync.IsServer)
                            SyncObject.InvokeAttachRequest(true);
                        else
                            LockMode = LandingGearMode.ReadyToLock;
                    }
                    else
                    {
                        LockMode = LandingGearMode.Unlocked;
                    }
                }
                else
                    LockMode = LandingGearMode.Unlocked;
            }
            if (m_autolockTimer != 0 && MySandboxGame.TotalGamePlayTimeInMilliseconds - m_autolockTimer > 3 * 1000)
                AutoLock = true;
        }

        private int m_retryCounter=0;
        private void RetryLock()
        {
            if (m_needsToRetryLock)
            {
                Vector3D pivot;
                var body = FindBody(out pivot);
                if (body==null && m_retryCounter < 3)
                {
                    m_retryCounter++;//when loading game, subpart which this gear is attached to may not exist yet (depends on UpdateBeforeSim block order)
                    return;
                }
                m_retryCounter = 0;
                if (body != null)
                {
                    if (m_attachedTo == null || m_attachedTo.Physics==null || body.Physics.RigidBody != ((MyPhysicsBody)m_attachedTo.Physics).RigidBody)
                        ResetLockConstraint(locked: true);
                }
                else
                {
                    ResetLockConstraint(locked: false);
                    StartSound(m_unlockSound);
                }
                m_needsToRetryLock = false;
            }
        }

        protected override void Closing()
        {
            Detach();
            base.Closing();
        }

        public void ResetAutolock()
        {
            //AutoLock = false;
            m_autolockTimer = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            UpdateEmissivity();
        }

        private void AttachFailed()
        {
            StartSound(m_failedAttachSound);
        }

        private void Attach(MyEntity entity, Vector3 gearSpacePivot, Matrix otherBodySpacePivot)
        {
            if (CubeGrid.Physics.Enabled)
            {
                var body = entity.Physics.RigidBody;
                var handle = StateChanged;

                if (MyFakes.WELD_LANDING_GEARS && CanWeldTo(entity, ref otherBodySpacePivot))
                {
                    if (m_attachedTo != null || entity == null)
                        return;

                    MyWeldingGroups.Static.CreateLink(EntityId, CubeGrid, entity);
                    //OnConstraintAdded(GridLinkTypeEnum.LandingGear, entity);
                    LockMode = LandingGearMode.Locked;
                    m_attachedTo = entity;
                    m_attachedTo.OnPhysicsChanged += m_physicsChangedHandler;
                    this.OnPhysicsChanged += m_physicsChangedHandler;
                    if (CanAutoLock)
                        ResetAutolock();

                    OnConstraintAdded(GridLinkTypeEnum.Physical, entity);
                    //OnConstraintAdded(GridLinkTypeEnum.NoContactDamage, entity);

                    if (!m_needsToRetryLock)
                        StartSound(m_lockSound);

                    if (handle != null) handle(true);
                    return;
                }


                //var entity = body.GetBody().Entity;
                Debug.Assert(m_attachedTo == null, "Already attached");
                Debug.Assert(entity != null, "Landing gear is attached to body which has no entity");
                Debug.Assert(m_constraint == null);

                if (m_attachedTo != null || entity == null || m_constraint != null)
                    return;

                body.Activate();
                CubeGrid.Physics.RigidBody.Activate();

                m_attachedTo = entity;

                m_attachedTo.OnPhysicsChanged += m_physicsChangedHandler;

                this.OnPhysicsChanged += m_physicsChangedHandler;

                Matrix gearLocalSpacePivot = Matrix.Identity;
                gearLocalSpacePivot.Translation = gearSpacePivot;

                var fixedData = new HkFixedConstraintData();
                if (MyFakes.OVERRIDE_LANDING_GEAR_INERTIA)
                {
                    fixedData.SetInertiaStabilizationFactor(MyFakes.LANDING_GEAR_INTERTIA);
                }
                else
                {
                    fixedData.SetInertiaStabilizationFactor(1);
                }

                fixedData.SetSolvingMethod(HkSolvingMethod.MethodStabilized);
                fixedData.SetInBodySpace(gearLocalSpacePivot, otherBodySpacePivot, CubeGrid.Physics, entity.Physics);

                HkConstraintData data = fixedData;

                if (MyFakes.LANDING_GEAR_BREAKABLE && BreakForce < MaxSolverImpulse)
                {
                    var breakData = new HkBreakableConstraintData(fixedData);
                    fixedData.Dispose();

                    breakData.Threshold = BreakForce;
                    breakData.ReapplyVelocityOnBreak = true;
                    breakData.RemoveFromWorldOnBrake = true;

                    data = breakData;
                }

                if (!m_needsToRetryLock)
                    StartSound(m_lockSound);

                m_constraint = new HkConstraint(CubeGrid.Physics.RigidBody, body, data);
                CubeGrid.Physics.AddConstraint(m_constraint);
                m_constraint.Enabled = true;

                LockMode = LandingGearMode.Locked;
                if (CanAutoLock)
                    ResetAutolock();

                OnConstraintAdded(GridLinkTypeEnum.Physical, entity);
                OnConstraintAdded(GridLinkTypeEnum.NoContactDamage, entity);

                if (handle != null) handle(true);
            }
        }

        private bool CanWeldTo(MyEntity entity, ref Matrix otherBodySpacePivot)
        {
            if (BreakForce < MaxSolverImpulse)
                return false;
            var grid = entity as MyCubeGrid;
            if (grid != null)
            {
                Vector3I cube;
                grid.FixTargetCube(out cube, otherBodySpacePivot.Translation * grid.GridSizeR);
                var block = grid.GetCubeBlock(cube);
                if (block != null && block.FatBlock is Sandbox.Game.Entities.Blocks.MyAirtightHangarDoor)
                    return false;
            }
            if (entity.Parent != null || entity is MyVoxelBase)
                return false;
            return true;
        }

        private void Detach()
        {
            if (CubeGrid.Physics == null || m_attachedTo == null)
                return;
            Debug.Assert(m_attachedTo != null, "Attached entity is null");
            LockMode = LandingGearMode.Unlocked;
            var attachedTo = m_attachedTo;
            if (m_attachedTo != null)
            {
                m_attachedTo.OnPhysicsChanged -= m_physicsChangedHandler;
            }
            this.OnPhysicsChanged -= m_physicsChangedHandler;

            m_attachedTo = null;
            if (MyFakes.WELD_LANDING_GEARS && MyWeldingGroups.Static.LinkExists(EntityId, CubeGrid, (MyEntity)m_attachedTo))
            {
                MyWeldingGroups.Static.BreakLink(EntityId, CubeGrid, (MyEntity)attachedTo);
            }
            else
            {
                if (m_constraint != null)
                {
                    CubeGrid.Physics.RemoveConstraint(m_constraint);
                    m_constraint.Dispose();
                    m_constraint = null;
                }
                OnConstraintRemoved(GridLinkTypeEnum.NoContactDamage, attachedTo);
            }
            OnConstraintRemoved(GridLinkTypeEnum.Physical, attachedTo);
            if (!m_needsToRetryLock && !MarkedForClose)
                StartSound(m_unlockSound); 
            var handle = StateChanged;
            if (handle != null) handle(false);
        }

        void PhysicsChanged(IMyEntity entity)
        {
            if (entity.Physics == null)
            {
                Detach();
            }
            else if (LockMode == LandingGearMode.Locked)
            {
                m_needsToRetryLock = true;
            }
        }

        public void EnqueueRetryLock()
        {
            if (m_needsToRetryLock)
                return;
            if (LockMode == LandingGearMode.Locked)
            {
                m_needsToRetryLock = true;
            }
        }

        void ComponentStack_IsFunctionalChanged()
        {
        }

        private void ResetLockConstraint(bool locked)
        {
            if (CubeGrid.Physics == null)
                return;

            Detach();

            if (locked)
            {
                Vector3D pivot;
                var entity = FindBody(out pivot);
                if (entity != null)
                {
                    var gearClusterMatrix = this.CubeGrid.Physics.RigidBody.GetRigidBodyMatrix();
                    var otherClusterMatrix = entity.Physics.RigidBody.GetRigidBodyMatrix();

                    // Calculate world (cluser) matrix of pivot
                    Matrix pivotCluster = gearClusterMatrix;
                    pivotCluster.Translation = this.CubeGrid.Physics.WorldToCluster(pivot);

                    // Convert cluser-space to local space
                    Vector3 gearSpacePivot = (pivotCluster * Matrix.Invert(gearClusterMatrix)).Translation;
                    Matrix otherBodySpacePivot = pivotCluster * Matrix.Invert(otherClusterMatrix);

                    Attach(entity, gearSpacePivot, otherBodySpacePivot);
                }
            }
            else
            {
                LockMode = LandingGearMode.Unlocked;
            }
            m_needsToRetryLock = false;
        }

        public void RequestLock(bool enable)
        {
            if (IsWorking)
                SyncObject.SendAttachRequest(enable);
        }

        private void StartSound(MySoundPair cueEnum)
        {
            m_soundEmitter.PlaySound(cueEnum, true);
        }

        event Action<bool> StateChanged;
        event Action<bool> Sandbox.ModAPI.IMyLandingGear.StateChanged
        {
            add { StateChanged += value; }
            remove { StateChanged -= value; }
        }

        IMyEntity Sandbox.ModAPI.Ingame.IMyLandingGear.GetAttachedEntity()
        {
            return m_attachedTo;
        }

        #region Sync class

        [PreloadRequired]
        class MySyncLandingGear
        {
            MyLandingGear m_landingGear;

            [MessageIdAttribute(15270, P2PMessageEnum.Reliable)]
            protected struct AttachMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public long OtherEntity;

                public Vector3D GearPivotPosition;
                public CompressedPositionOrientation OtherPivot;

                public BoolBlit Enable;
                
                public override string ToString()
                {
                    return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
                }
            }

            [MessageIdAttribute(14371, P2PMessageEnum.Reliable)]
            protected struct BrakeForceMsg : IEntityMessage
            {
                public long EntityId;

                public long GetEntityId() { return EntityId; }

                public float BrakeForce;

                public override string ToString()
                {
                    return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
                }
            }

            [MessageIdAttribute(14372, P2PMessageEnum.Reliable)]
            protected struct AutoLockMsg : IEntityMessage
            {
                public long EntityId;

                public long GetEntityId() { return EntityId; }

                public BoolBlit AutoLock;

                public override string ToString()
                {
                    return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
                }
            }

            static MySyncLandingGear()
            {
                MySyncLayer.RegisterMessage<AttachMsg>(AttachRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
                MySyncLayer.RegisterMessage<AttachMsg>(AttachSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
                MySyncLayer.RegisterMessage<AttachMsg>(AttachFailure, MyMessagePermissions.FromServer, MyTransportMessageEnum.Failure);
                MySyncLayer.RegisterMessage<BrakeForceMsg>(OnBrakeForceChange, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
                MySyncLayer.RegisterMessage<AutoLockMsg>(OnAutoLockChange, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
            }

            private static void AttachFailure(ref AttachMsg msg, MyNetworkClient sender)
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(msg.EntityId, out entity);
                if (entity is MyLandingGear)
                {
                    (entity as MyLandingGear).AttachFailed();
                }
            }

            public MySyncLandingGear(MyLandingGear gear)
            {
                m_landingGear = gear;
            }

            public void SendBrakeForceChange(float brakeForce)
            {
                var msg = new BrakeForceMsg();
                msg.EntityId = m_landingGear.EntityId;
                msg.BrakeForce = brakeForce;

                Sync.Layer.SendMessageToServer(ref msg);
            }

            static void OnBrakeForceChange(ref BrakeForceMsg msg, MyNetworkClient sender)
            {
                MyLandingGear gear;
                if (MyEntities.TryGetEntityById<MyLandingGear>(msg.EntityId, out gear))
                {
                    gear.BreakForce = msg.BrakeForce;
                    if (Sync.IsServer)
                        Sync.Layer.SendMessageToAll(ref msg);
                }
            }

            public void SendAutoLockChange(bool autoLock)
            {
                var msg = new AutoLockMsg();
                msg.EntityId = m_landingGear.EntityId;
                msg.AutoLock = autoLock;

                Sync.Layer.SendMessageToServer(ref msg);
            }

            static void OnAutoLockChange(ref AutoLockMsg msg, MyNetworkClient sender)
            {
                MyLandingGear gear;
                if (MyEntities.TryGetEntityById<MyLandingGear>(msg.EntityId, out gear))
                {
                    gear.AutoLock = msg.AutoLock;
                    if (Sync.IsServer)
                        Sync.Layer.SendMessageToAll(ref msg);
                }
            }

            public virtual void SendAttachRequest(bool enable)
            {
                var msg = new AttachMsg();
                msg.EntityId = ((MyEntity)m_landingGear).EntityId;
                msg.Enable = enable;

                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }

            public virtual void InvokeAttachRequest(bool enable)
            {
                Debug.Assert(Sync.IsServer);
                var msg = new AttachMsg();
                msg.EntityId = ((MyEntity)m_landingGear).EntityId;
                msg.Enable = enable;

                AttachRequest(ref msg, Sync.Clients.LocalClient);
            }

            static void AttachRequest(ref AttachMsg msg, MyNetworkClient sender)
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(msg.EntityId, out entity);
                if (entity is MyLandingGear)
                {
                    var landingGear = entity as MyLandingGear;
                    if (!landingGear.IsFunctional)
                        return;
                    if (msg.Enable)
                    {
                        Vector3D pivot;
                        var otherEntity = landingGear.FindBody(out pivot);
                        if (otherEntity != null)
                        {
                            var gearClusterMatrix = landingGear.CubeGrid.Physics.RigidBody.GetRigidBodyMatrix();
                            var otherClusterMatrix = otherEntity.Physics.RigidBody.GetRigidBodyMatrix();

                            // Calculate world (cluser) matrix of pivot
                            Matrix pivotCluster = gearClusterMatrix;
                            pivotCluster.Translation = landingGear.CubeGrid.Physics.WorldToCluster(pivot);

                            // Convert cluser-space to local space
                            msg.GearPivotPosition = (pivotCluster * Matrix.Invert(gearClusterMatrix)).Translation;
                            msg.OtherPivot.Matrix = pivotCluster * Matrix.Invert(otherClusterMatrix);
                            msg.OtherEntity = otherEntity.EntityId;
                            AttachSuccess(ref msg, Sync.Clients.LocalClient);
                            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                        }
                        else
                        {
                            AttachFailure(ref msg, Sync.Clients.LocalClient);
                            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Failure);
                        }
                    }
                    else
                    {
                        AttachSuccess(ref msg, Sync.Clients.LocalClient);
                        Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                    }
                }
            }

            static void AttachSuccess(ref AttachMsg msg, MyNetworkClient sender)
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(msg.EntityId, out entity);
                if (entity is MyLandingGear)
                {
                    var landingGear = (MyLandingGear)entity;
                    if (msg.Enable)
                    {
                        MyEntity otherEntity;
                        if (MyEntities.TryGetEntityById(msg.OtherEntity, out otherEntity))
                        {
                            landingGear.Attach(otherEntity, msg.GearPivotPosition, msg.OtherPivot.Matrix);
                        }
                    }
                    else
                    {
                        landingGear.ResetLockConstraint(locked: false);
                    }
                }
            }
        }

        #endregion
    }
}
