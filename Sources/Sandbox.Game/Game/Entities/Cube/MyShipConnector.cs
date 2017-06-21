using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Game.EntityComponents;
using VRage;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageMath.PackedVector;
using VRage.Serialization;
using VRage.Network;
using VRage.Library.Utils;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.Profiler;
using VRage.Sync;
using IMyEntity = VRage.ModAPI.IMyEntity;
#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
using System.Reflection;
using VRage.Reflection;
#endif // XB1

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ShipConnector))]
    public partial class MyShipConnector : MyFunctionalBlock, IMyInventoryOwner, IMyConveyorEndpointBlock, IMyShipConnector
    {
        /// <summary>
        /// Represents connector state, atomic for sync, 8 B + 1b + 1b/12.5B
        /// </summary>
#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
        struct State
#else // XB1
        struct State : IMySetGetMemberDataHelper
#endif // XB1
        {
            public static readonly State Detached = new State();
            public static readonly State DetachedMaster = new State() { IsMaster = true };

            public bool IsMaster;
            public long OtherEntityId; // zero when detached, valid EntityId when approaching or connected
            public MyDeltaTransform? MasterToSlave; // relative connector-to-connector world transform MASTER * DELTA = SLAVE, null when detached/approaching, valid value when connected
            public MyDeltaTransform? MasterToSlaveGrid;

#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
            public object GetMemberData(MemberInfo m)
            {
                if (m.Name == "IsMaster")
                    return IsMaster;
                if (m.Name == "OtherEntityId")
                    return OtherEntityId;
                if (m.Name == "MasterToSlave")
                    return MasterToSlave;
                if (m.Name == "MasterToSlaveGrid")
                    return MasterToSlaveGrid;

                System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
                return null;
            }
#endif // XB1
        }

        private enum Mode
        {
            Ejector,
            Connector,
        }

        /// <summary>
        /// For this time the connector won't create aproach constraint (it's still possible to lock)
        /// </summary>
        private static readonly MyTimeSpan DisconnectSleepTime = MyTimeSpan.FromSeconds(4);

        /// <summary>
        /// Minimal strength for setting in terminal (must be > 0, it's used as log limit)
        /// </summary>
        private const float MinStrength = 0.000001f;

        public readonly Sync<bool> ThrowOut;
        public readonly Sync<bool> CollectAll;
        public readonly Sync<float> Strength;
        private readonly Sync<State> m_connectionState;
        private MyAttachableConveyorEndpoint m_attachableConveyorEndpoint;
        private int m_update10Counter;

        // Use the property instead of the field, because the block's transformation has to be applied
        private Vector3 m_connectionPosition;
        private float m_detectorRadius;
        private HkConstraint m_constraint;

        private MyShipConnector m_other;

        private bool m_defferedDisconnect = false;

        private static HashSet<MySlimBlock> m_tmpBlockSet = new HashSet<MySlimBlock>();

        private MyTimeSpan m_manualDisconnectTime;
        private MyPhysicsBody m_connectorDummy;
        private Mode m_connectorMode = Mode.Ejector;
        private bool m_hasConstraint = false;
        private List<IMyEntity> m_detectedFloaters = new List<IMyEntity>();
        private HashSet<MyEntity> m_detectedGrids = new HashSet<MyEntity>();

        /// <summary>
        /// Whether this block created the constraint and should also remove it. Only valid if Connected == true;
        /// Master is block with higher EntityId.
        /// </summary>
        /// 
        bool m_isMaster = false;
        private bool IsMaster { get { return Sync.IsServer ? m_isMaster : m_connectionState.Value.IsMaster; }  set { m_isMaster = value; } }

        public bool IsReleasing { get { return MySandboxGame.Static.UpdateTime - m_manualDisconnectTime < DisconnectSleepTime; } }

        bool m_welded = false;
        bool m_welding = false;
        public bool InConstraint { get { return m_welded || m_constraint != null; } }
        public bool Connected { get; set; }

        private bool m_isInitOnceBeforeFrameUpdate = false;

        private Vector3 ConnectionPosition { get { return Vector3.Transform(m_connectionPosition, this.PositionComp.LocalMatrix); } }

        public int DetectedGridCount { get { return m_detectedGrids.Count; } }

        public MyShipConnector()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            ThrowOut = SyncType.CreateAndAddProp<bool>();
            CollectAll = SyncType.CreateAndAddProp<bool>();
            Strength = SyncType.CreateAndAddProp<float>();
            m_connectionState = SyncType.CreateAndAddProp<State>();
#endif // XB1
            CreateTerminalControls();

            m_connectionState.ValueChanged += (o) => OnConnectionStateChanged();
            m_connectionState.ValidateNever(); // Never set by client
            m_manualDisconnectTime = new MyTimeSpan(-DisconnectSleepTime.Ticks);
            Strength.Validate = (o) => Strength >= 0 && Strength <= 1;
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyShipConnector>())
                return;
            base.CreateTerminalControls();
            var throwOut = new MyTerminalControlOnOffSwitch<MyShipConnector>("ThrowOut", MySpaceTexts.Terminal_ThrowOut);
            throwOut.Getter = (block) => block.ThrowOut;
            throwOut.Setter = (block, value) => block.ThrowOut.Value = value;
            throwOut.EnableToggleAction();
            MyTerminalControlFactory.AddControl(throwOut);

            var collectAll = new MyTerminalControlOnOffSwitch<MyShipConnector>("CollectAll", MySpaceTexts.Terminal_CollectAll);
            collectAll.Getter = (block) => block.CollectAll;
            collectAll.Setter = (block, value) => block.CollectAll.Value = value;
            collectAll.EnableToggleAction();
            MyTerminalControlFactory.AddControl(collectAll);

            var lockBtn = new MyTerminalControlButton<MyShipConnector>("Lock", MySpaceTexts.BlockActionTitle_Lock, MySpaceTexts.Blank, (b) => b.TryConnect());
            lockBtn.Enabled = (b) => b.IsWorking && b.InConstraint;
            lockBtn.Visible = (b) => b.m_connectorMode == Mode.Connector;
            var actionLock = lockBtn.EnableAction();
            actionLock.Enabled = (b) => b.m_connectorMode == Mode.Connector;
            MyTerminalControlFactory.AddControl(lockBtn);

            var unlockBtn = new MyTerminalControlButton<MyShipConnector>("Unlock", MySpaceTexts.BlockActionTitle_Unlock, MySpaceTexts.Blank, (b) => b.TryDisconnect());
            unlockBtn.Enabled = (b) => b.IsWorking && b.InConstraint;
            unlockBtn.Visible = (b) => b.m_connectorMode == Mode.Connector;
            var actionUnlock = unlockBtn.EnableAction();
            actionUnlock.Enabled = (b) => b.m_connectorMode == Mode.Connector;
            MyTerminalControlFactory.AddControl(unlockBtn);

            var title = MyTexts.Get(MySpaceTexts.BlockActionTitle_SwitchLock);
            MyTerminalAction<MyShipConnector> switchLockAction = new MyTerminalAction<MyShipConnector>("SwitchLock", title, MyTerminalActionIcons.TOGGLE);
            switchLockAction.Action = (b) => b.TrySwitch();
            switchLockAction.Writer = (b, sb) => b.WriteLockStateValue(sb);
            switchLockAction.Enabled = (b) => b.m_connectorMode == Mode.Connector;
            MyTerminalControlFactory.AddAction(switchLockAction);

            var strength = new MyTerminalControlSlider<MyShipConnector>("Strength", MySpaceTexts.BlockPropertyTitle_Connector_Strength, MySpaceTexts.BlockPropertyDescription_Connector_Strength);
            strength.Getter = (x) => x.Strength;
            strength.Setter = (x, v) => x.Strength.Value = v;
            strength.DefaultValue = MyObjectBuilder_ShipConnector.DefaultStrength;
            strength.SetLogLimits(MinStrength, 1.0f);
            strength.EnableActions(enabled: (b) => b.m_connectorMode == Mode.Connector);
            strength.Enabled = (b) => b.m_connectorMode == Mode.Connector;
            strength.Visible = (b) => b.m_connectorMode == Mode.Connector;
            strength.Writer = (x, result) =>
            {
                if (x.Strength <= MinStrength)
                    result.Append(MyTexts.Get(MyCommonTexts.Disabled));
                else
                    result.AppendFormatedDecimal("", x.Strength * 100, 4, " %");
            };
            MyTerminalControlFactory.AddControl(strength);
        }

        private void OnConnectionStateChanged()
        {
            if (Sync.IsServer == false)
            {
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

                if ((Connected || InConstraint) && m_connectionState.Value.MasterToSlave != null)
                {
                    Detach(false);
                }
            }
        }

        public void WriteLockStateValue(StringBuilder sb)
        {
            if (InConstraint && Connected)
                sb.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyValue_Locked));
            else if (InConstraint)
                sb.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyValue_ReadyToLock));
            else
                sb.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyValue_Unlocked));
        }

        public void TrySwitch()
        {
            if (InConstraint)
            {
                if (Connected)
                    TryDisconnect();
                else
                    TryConnect();
            }
        }

        [Event, Reliable, Server]
        public void TryConnect()
        {
            if (InConstraint && !Connected)
            {
                if (Sync.IsServer)
                    Weld();
                else
                    MyMultiplayer.RaiseEvent(this, x => x.TryConnect);
            }
        }

        [Event, Reliable, Server]
        public void TryDisconnect()
        {
            if (InConstraint && Connected)
            {
                m_manualDisconnectTime = m_other.m_manualDisconnectTime = MySandboxGame.Static.UpdateTime;
                if (Sync.IsServer)
                {
                    Detach();
                }
                else
                {
                    MyMultiplayer.RaiseEvent(this, x => x.TryDisconnect);
                }
            }
        }

        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
        }

        protected float GetEffectiveStrength(MyShipConnector otherConnector)
        {
            float strength = 0.0f;
            if (!IsReleasing)
            {
                strength = Math.Min(Strength, otherConnector.Strength);
                if (strength <= MinStrength)
                    strength = 0.0000001f; // Must be > 0
            }
            return strength;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            float consumption = MyEnergyConstants.MAX_REQUIRED_POWER_CONNECTOR;
            if (cubeGrid.GridSizeEnum == MyCubeSize.Small)
            {
                consumption *= 0.01f;
            }

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                MyStringHash.GetOrCompute("Conveyors"),
                consumption,
                () => base.CheckIsWorking() ? ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0f
            );
            sinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;
            ResourceSink = sinkComp;
            

            base.Init(objectBuilder, cubeGrid);

            var ob = objectBuilder as MyObjectBuilder_ShipConnector;
            Vector3 inventorySize = (BlockDefinition.Size * CubeGrid.GridSize) * 0.8f; // 0.8 ~= 0.5^(1/3) to make the inventory volume approx. one half of the block size

            if (MyFakes.ENABLE_INVENTORY_FIX)
            {
                FixSingleInventory();
            }

            if (this.GetInventory() == null)
            {
                MyInventory inventory = new MyInventory(inventorySize.Volume, inventorySize, MyInventoryFlags.CanReceive | MyInventoryFlags.CanSend);
                Components.Add<MyInventoryBase>(inventory);
                inventory.Init(ob.Inventory);
            }
            Debug.Assert(this.GetInventory().Owner == this, "Ownership was not set!");

            ThrowOut.Value = ob.ThrowOut;
            CollectAll.Value = ob.CollectAll;

            SlimBlock.DeformationRatio = ob.DeformationRatio;
         
            SlimBlock.ComponentStack.IsFunctionalChanged += UpdateReceiver;
            base.EnabledChanged += UpdateReceiver;

            ResourceSink.Update();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            if (CubeGrid.CreatePhysics)
                LoadDummies();
            if (Physics != null) Physics.Enabled = true;
            if (m_connectorDummy != null)
            {
                m_connectorDummy.Enabled = true;
            }

            Strength.Value = ob.Strength;
            if (ob.ConnectedEntityId != 0)
            {
                MyDeltaTransform? deltaTransform = ob.MasterToSlaveTransform.HasValue ? ob.MasterToSlaveTransform.Value : (MyDeltaTransform?)null;
                if (ob.Connected)
                {
                    // Old saves with connected connector, store ZERO into MasterToSlave transform
                    deltaTransform = default(MyDeltaTransform);
                }
                if(ob.IsMaster.HasValue == false)
                {
                    ob.IsMaster = ob.ConnectedEntityId < EntityId;
                }

                IsMaster = ob.IsMaster.Value;
                m_connectionState.Value = new State() {IsMaster =  ob.IsMaster.Value, OtherEntityId = ob.ConnectedEntityId, MasterToSlave = deltaTransform,MasterToSlaveGrid = ob.MasterToSlaveGrid};
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                m_isInitOnceBeforeFrameUpdate = true;
            }

            IsWorkingChanged += MyShipConnector_IsWorkingChanged;                       

            AddDebugRenderComponent(new Components.MyDebugRenderCompoonentShipConnector(this));
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var state = m_connectionState.Value;

            var ob = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_ShipConnector;
            ob.Inventory = this.GetInventory().GetObjectBuilder();
            ob.ThrowOut = ThrowOut;
            ob.CollectAll = CollectAll;
            ob.Strength = Strength;
            ob.ConnectedEntityId = state.OtherEntityId;
            ob.IsMaster = state.IsMaster;
            ob.MasterToSlaveTransform = state.MasterToSlave.HasValue ? state.MasterToSlave.Value : (MyPositionAndOrientation?)null;
            ob.MasterToSlaveGrid = state.MasterToSlaveGrid;
            return ob;
        }

        protected override void OnInventoryComponentAdded(MyInventoryBase inventory)
        {
            base.OnInventoryComponentAdded(inventory);
            Debug.Assert(this.GetInventory() != null, "Added inventory to collector, but different type than MyInventory?! Check this.");
            if (this.GetInventory() != null && MyPerGameSettings.InventoryMass)
            {
                this.GetInventory().ContentsChanged += Inventory_ContentsChanged;
            }
        }

        protected override void OnInventoryComponentRemoved(MyInventoryBase inventory)
        {
            base.OnInventoryComponentRemoved(inventory);
            var removedInventory = inventory as MyInventory;
            Debug.Assert(removedInventory != null, "Removed inventory is not MyInventory type? Check this.");
            if (removedInventory != null && MyPerGameSettings.InventoryMass)
            {
                removedInventory.ContentsChanged -= Inventory_ContentsChanged;
            }
        }

        void Inventory_ContentsChanged(MyInventoryBase obj)
        {
            CubeGrid.SetInventoryMassDirty();
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            UpdateConnectionState();
        }

        void MyShipConnector_IsWorkingChanged(MyCubeBlock obj)
        {
            Debug.Assert(obj == this);

            if (Sync.IsServer && Connected && m_welded ==false)
            {
                if (!IsFunctional || !IsWorking)
                {
                    m_connectionState.Value = State.Detached;
                    NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                }
            }

            UpdateEmissivity();
        }

        protected override void OnStopWorking()
        {
            UpdateEmissivity();
            base.OnStopWorking();
        }

        protected override void OnStartWorking()
        {
            UpdateEmissivity();
            base.OnStartWorking();
        }

        private void LoadDummies()
        {
            var finalModel = VRage.Game.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model);
            foreach (var dummy in finalModel.Dummies)
            {
                bool isConnector = dummy.Key.ToLower().Contains("connector");
                bool isEjector = isConnector || dummy.Key.ToLower().Contains("ejector");

                if (!isConnector && !isEjector) continue;

                Matrix dummyLocal = Matrix.Normalize(dummy.Value.Matrix);
                m_connectionPosition = dummyLocal.Translation;

                dummyLocal *= this.PositionComp.LocalMatrix;

                Vector3 halfExtents = dummy.Value.Matrix.Scale / 2.0f;
                halfExtents = new Vector3(halfExtents.Z, halfExtents.X, halfExtents.Y);
                m_detectorRadius = halfExtents.AbsMax();

                Vector3 center = dummy.Value.Matrix.Translation;

                if (isConnector)
                    m_connectorDummy = CreatePhysicsBody(Mode.Connector, ref dummyLocal, ref center, ref halfExtents);
                if (isEjector)
                    Physics = CreatePhysicsBody(Mode.Ejector, ref dummyLocal, ref center, ref halfExtents);

                if (isConnector) m_connectorMode = Mode.Connector;
                else m_connectorMode = Mode.Ejector;

                break;
            }
        }

        private MyPhysicsBody CreatePhysicsBody(Mode mode, ref Matrix dummyLocal, ref Vector3 center, ref Vector3 halfExtents)
        {
            // Only create physical shape for ejectors (on client and server) and for connectors on the server
            MyPhysicsBody physics = null;
            if (mode == Mode.Ejector || Sync.IsServer)
            {
                var detectorShape = CreateDetectorShape(halfExtents, mode);
                if (mode == Mode.Connector)
                {
                    physics = new Engine.Physics.MyPhysicsBody(this, RigidBodyFlag.RBF_KINEMATIC);
                    physics.IsPhantom = true;
                    physics.CreateFromCollisionObject(detectorShape, center, dummyLocal, null, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);
                }
                else
                {
                    physics = new Engine.Physics.MyPhysicsBody(this, RigidBodyFlag.RBF_KINEMATIC);
                    physics.IsPhantom = true;
                    physics.CreateFromCollisionObject(detectorShape, center, dummyLocal, null, MyPhysics.CollisionLayers.CollectorCollisionLayer);
                }
                physics.RigidBody.ContactPointCallbackEnabled = true;
                detectorShape.Base.RemoveReference();
            }
            return physics;
        }

        private HkBvShape CreateDetectorShape(Vector3 extents, Mode mode)
        {
            if (mode == Mode.Ejector)
            {
                var phantom = new HkPhantomCallbackShape(phantom_EnterEjector, phantom_LeaveEjector);
                var detectorShape = new HkBoxShape(extents);
                return new HkBvShape(detectorShape, phantom, HkReferencePolicy.TakeOwnership);
            }
            else
            {
                var phantom = new HkPhantomCallbackShape(phantom_EnterConnector, phantom_LeaveConnector);
                var detectorShape = new HkSphereShape(extents.AbsMax());
                return new HkBvShape(detectorShape, phantom, HkReferencePolicy.TakeOwnership);
            }
        }

        private void phantom_LeaveEjector(HkPhantomCallbackShape shape, HkRigidBody body)
        {
            ProfilerShort.Begin("ShipConnectorLeaveEjector");
            var updateEmissivity = (m_detectedFloaters.Count == 2);
            var entities = body.GetAllEntities();
            foreach (var entity in entities)
                m_detectedFloaters.Remove(entity);
            entities.Clear();
            if (updateEmissivity)
                UpdateEmissivity();
            ProfilerShort.End();
        }

        private void phantom_LeaveConnector(HkPhantomCallbackShape shape, HkRigidBody body)
        {
            ProfilerShort.Begin("ShipConnectorLeaveConnector");
            var entities = body.GetAllEntities();
            foreach (var entity in entities)
            {
                 m_detectedGrids.Remove(entity as MyCubeGrid);
            }
            entities.Clear();
            ProfilerShort.End();
        }

        private void phantom_EnterEjector(HkPhantomCallbackShape shape, HkRigidBody body)
        {
            ProfilerShort.Begin("ShipConnectorEnterEjector");
            bool updateEmissivity = false;
            var entities = body.GetAllEntities();
            foreach (var entity in entities)
            {
                Debug.Assert(entity is MyFloatingObject);
                if (entity is MyFloatingObject)
                {
                    updateEmissivity |= (m_detectedFloaters.Count == 1);
                    m_detectedFloaters.Add(entity);
                }
            }
            entities.Clear();

            if (updateEmissivity)
                UpdateEmissivity();
            ProfilerShort.End();
        }

        private void phantom_EnterConnector(HkPhantomCallbackShape shape, HkRigidBody body)
        {
            ProfilerShort.Begin("ShipConnectorEnterConnector");
            var entities = body.GetAllEntities();
            using (entities.GetClearToken())
            {
                foreach (var entity in entities)
                {
                    var other = entity as MyCubeGrid;
                    if (other == null || other == this.CubeGrid)
                        continue;

                    m_detectedGrids.Add(other);
                }
            }
            ProfilerShort.End();
        }

        private void GetBoxFromMatrix(Matrix m, out Vector3 halfExtents, out Vector3 position, out Quaternion orientation)
        {
            halfExtents = Vector3.Zero;
            position = Vector3.Zero;
            orientation = Quaternion.Identity;
        }

        private void UpdateReceiver(MyTerminalBlock block)
        {
            ResourceSink.Update();
        }

        private void UpdateReceiver()
        {
            ResourceSink.Update();
        }

        void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(this.GetInventory());
            base.OnRemovedByCubeBuilder();
        }

        public override void OnDestroy()
        {
            ReleaseInventory(this.GetInventory());
            base.OnDestroy();
        }

        public override void OnModelChange()
        {
            base.OnModelChange();
            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (!InScene)
                return;

            if (InConstraint)
            {
                var obj = this;
                if (m_other != null && m_other.IsMaster)
                    obj = m_other;

                if (obj.Connected)
                    UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], "Emissive", Color.ForestGreen, 1);
                else if (obj.IsReleasing)
                    UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], "Emissive", Color.RoyalBlue, 0.5f);
                else
                    UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], "Emissive", Color.Goldenrod, 1);
            }
            else
            {
                if (!IsWorking && m_connectorMode == Mode.Connector)
                    UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], "Emissive", Color.Black, 1);
                else if (IsWorking && m_connectorMode == Mode.Ejector)
                    UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], "Emissive", Color.ForestGreen, 1);
                else if (m_detectedFloaters.Count < 2 || !IsWorking)
                    UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], "Emissive", Color.Gray, 1);
                else
                    UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], "Emissive", Color.Red, 1);
            }
        }

        private void TryAttach(long? otherConnectorId = null)
        {
            var otherConnector = FindOtherConnector(otherConnectorId);
            if (otherConnector != null && otherConnector.FriendlyWithBlock(this) && CubeGrid.Physics != null  && otherConnector.CubeGrid.Physics != null)
            {
                var pos = ConstraintPositionWorld();
                var otherPos = otherConnector.ConstraintPositionWorld();
                float len = (otherPos - pos).LengthSquared();

                if (otherConnector.m_connectorMode == Mode.Connector && otherConnector.IsFunctional && (otherPos - pos).LengthSquared() < 0.35f)
                {
                    MyShipConnector master = GetMaster(this, otherConnector);
                    master.IsMaster = true;

                    if (master == this)
                    {
                        CreateConstraint(otherConnector);
                        otherConnector.IsMaster = false;
                    }
                    else
                    {
                        otherConnector.CreateConstraint(this);
                        IsMaster = false;
                    }
                }
            }
            else
            {
                m_connectionState.Value = State.DetachedMaster;
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            if (Sync.IsServer && IsWorking)
            {
                m_update10Counter++;
                if (!InConstraint && m_update10Counter % 8 == 0 && Enabled)
                {
                    if (CollectAll)
                    {
                        MyGridConveyorSystem.PullAllRequest(this, this.GetInventory(), OwnerId, true);
                    }
                    if (ThrowOut && m_detectedFloaters.Count < 2)
                    {
                        TryThrowOutItem();
                    }
                }
                if (m_detectedFloaters.Count == 0 && m_connectorMode == Mode.Connector)
                {
                    if (m_update10Counter % 4 == 0 && Enabled && !InConstraint && m_connectionState.Value.MasterToSlave == null)
                    {
                        TryAttach();
                    }
                }
            }
            else if (Sync.IsServer && !IsWorking)
            {
                // When stops working and aproaching, detach (keep connected when was connected)
                if (InConstraint && !Connected)
                {
                    Detach();
                }
            }

            if (IsWorking && InConstraint && !Connected)
            {
                var newStrength = GetEffectiveStrength(m_other);
                var data = m_constraint.ConstraintData as HkMalleableConstraintData;
                if (data != null && data.Strength != newStrength && IsMaster)
                {
                    data.Strength = newStrength;
                    CubeGrid.Physics.RigidBody.Activate();
                    UpdateEmissivity();
                    m_other.UpdateEmissivity();
                }
            }


            if (Sync.IsServer && InConstraint && !Connected && m_connectorMode == Mode.Connector)
            {
                var pos = ConstraintPositionWorld();
                var otherPos = m_other.ConstraintPositionWorld();
                if ((otherPos - pos).LengthSquared() > 0.5f)
                {
                    Detach();
                }
            }
            UpdateConnectionState();
        }

        private void UpdateConnectionState()
        {
            if (m_isInitOnceBeforeFrameUpdate)
            {
                m_isInitOnceBeforeFrameUpdate = false;
            }
            else if (m_other == null && (m_connectionState.Value.OtherEntityId != 0))
            {
                if (Sync.IsServer)
                {
                    m_connectionState.Value = State.Detached;
                }
            }
            if (!IsMaster)
                return;

            var state = m_connectionState.Value;

            // Make sure constraints correctly represents ConnectionState.
            if (state.OtherEntityId == 0) // Detached
            {
                if (InConstraint)
                {
                    Detach(false);
                }
            }
            else if (state.MasterToSlave == null) // Aproaching
            {
                if (Connected || (InConstraint && m_other.EntityId != state.OtherEntityId))
                {
                    // Detach when connected or aproaching something else
                    Detach(false);
                }

                MyShipConnector connector;
                if (!InConstraint && MyEntities.TryGetEntityById<MyShipConnector>(state.OtherEntityId, out connector) && connector.FriendlyWithBlock(this) && connector.Closed == false && connector.MarkedForClose == false)
                {
                    if (Physics != null && connector.Physics != null)
                    {
                        if (Sync.IsServer == false && state.MasterToSlaveGrid.HasValue)
                        {
                            CubeGrid.WorldMatrix = MatrixD.Multiply(state.MasterToSlaveGrid.Value, connector.WorldMatrix);
                        }
                        CreateConstraintNosync(connector);
                        UpdateEmissivity();
                        m_other.UpdateEmissivity();
                    }
                }

            }
            else // Connected
            {
                if (Connected && m_other.EntityId != state.OtherEntityId)
                {
                    Detach(false);
                }

                MyShipConnector connector;
                if (!Connected && MyEntities.TryGetEntityById<MyShipConnector>(state.OtherEntityId, out connector) && connector.FriendlyWithBlock(this))
                {
                    m_other = connector;
                    var masterToSlave = state.MasterToSlave;
                    if (masterToSlave.HasValue && masterToSlave.Value.IsZero)
                    {
                        // Special case when deserializing old saves with connected connector (old saves does not have MasterToSlave transform...it's zero)
                        masterToSlave = null;
                    }

                    if (Sync.IsServer == false && state.MasterToSlaveGrid.HasValue)
                    {
                        this.CubeGrid.WorldMatrix = MatrixD.Multiply(state.MasterToSlaveGrid.Value, connector.WorldMatrix);
                    }
                    Weld(masterToSlave);
                }
            }
        }

        private void TryThrowOutItem()
        {
            float volume = CubeGrid.GridSizeEnum == MyCubeSize.Large ? 0.25f : 0.05f;
            var items = this.GetInventory().GetItems();
            for (int i = 0; i < this.GetInventory().GetItems().Count; )
            {
                float rnd = MyUtils.GetRandomFloat(0, CubeGrid.GridSizeEnum == MyCubeSize.Large ? 0.5f : 0.07f);
                var circle = MyUtils.GetRandomVector3CircleNormalized();
                Vector3 rndPos = Vector3.Transform(ConnectionPosition, CubeGrid.PositionComp.WorldMatrix) + PositionComp.WorldMatrix.Right * circle.X * rnd + PositionComp.WorldMatrix.Up * circle.Z * rnd;

                MyPhysicalItemDefinition def;
                if (!MyDefinitionManager.Static.TryGetPhysicalItemDefinition(items[i].Content.GetId(), out def))
                    continue;
                Vector3 forward, up;
                float offset = def.Size.Max();
                if (offset == def.Size.Z)
                {
                    forward = PositionComp.WorldMatrix.Forward;
                    up = PositionComp.WorldMatrix.Up;
                }
                else if (offset == def.Size.Y)
                {
                    forward = PositionComp.WorldMatrix.Right;
                    up = PositionComp.WorldMatrix.Forward;
                }
                else
                {
                    forward = PositionComp.WorldMatrix.Up;
                    up = PositionComp.WorldMatrix.Right;
                }
                offset *= 0.5f;
                rndPos += PositionComp.WorldMatrix.Forward * offset;
                MyFixedPoint itemAmount = (MyFixedPoint)(volume / def.Volume);
                if (items[i].Content.TypeId != typeof(MyObjectBuilder_Ore) &&
                    items[i].Content.TypeId != typeof(MyObjectBuilder_Ingot))
                {
                    itemAmount = MyFixedPoint.Ceiling(itemAmount);
                }
                MyParticleEffect effect;
                MyEntity entity;
                MyFixedPoint ejectedItemCount = 0;
                if (items[i].Amount < itemAmount)
                {
                    volume -= ((float)items[i].Amount * def.Volume);
                    entity = MyFloatingObjects.Spawn(items[i], rndPos, PositionComp.WorldMatrix.Forward, PositionComp.WorldMatrix.Up, CubeGrid.Physics);
                    ejectedItemCount = items[i].Amount;
                    this.GetInventory().RemoveItems(items[i].ItemId);
                    i++;
                }
                else
                {
                    var tmpItem = new MyPhysicalInventoryItem(items[i].GetObjectBuilder());
                    tmpItem.Amount = itemAmount;
                    entity = MyFloatingObjects.Spawn(tmpItem, rndPos, PositionComp.WorldMatrix.Forward, PositionComp.WorldMatrix.Up, CubeGrid.Physics);
                    ejectedItemCount = itemAmount;
                    this.GetInventory().RemoveItems(items[i].ItemId, itemAmount);
                    volume = 0;
                }
                entity.Physics.LinearVelocity += PositionComp.WorldMatrix.Forward * (1);

                if (ejectedItemCount > 0)
                {
                    if (m_soundEmitter != null)
                    {
                        m_soundEmitter.PlaySound(m_actionSound);
                        MyMultiplayer.RaiseEvent(this, x => x.PlayActionSound);
                    }
                }

                if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Smoke_Collector, out effect))
                {
                    effect.WorldMatrix = entity.WorldMatrix;
                    effect.Velocity = CubeGrid.Physics.LinearVelocity;
                }
                break;
            }
        }

        [Event, Reliable, Broadcast]
        private void PlayActionSound()
        {
            m_soundEmitter.PlaySound(m_actionSound);
        }

        private MyShipConnector FindOtherConnector(long? otherConnectorId = null)
        {
            MyShipConnector connector = null;
            BoundingSphereD sphere = new BoundingSphereD(ConnectionPosition, m_detectorRadius);

            if (otherConnectorId.HasValue)
            {
                MyEntities.TryGetEntityById<MyShipConnector>(otherConnectorId.Value, out connector);
            }
            else
            {
                sphere = sphere.Transform(CubeGrid.PositionComp.WorldMatrix);
                connector = TryFindConnectorInGrid(ref sphere, CubeGrid, this);
            }

            if (connector != null) return connector;

            foreach (var entity in m_detectedGrids)
            {
                if (entity.MarkedForClose)
                    continue;
                Debug.Assert(entity is MyCubeGrid);
                if (!(entity is MyCubeGrid)) continue;

                var grid = entity as MyCubeGrid;
                if (grid == this.CubeGrid) continue;

                connector = TryFindConnectorInGrid(ref sphere, grid, this);
                if (connector != null) return connector;
            }

            return null;
        }

        private static MyShipConnector TryFindConnectorInGrid(ref BoundingSphereD sphere, MyCubeGrid grid, MyShipConnector thisConnector = null)
        {
            m_tmpBlockSet.Clear();
            grid.GetBlocksInsideSphere(ref sphere, m_tmpBlockSet);

            foreach (var block in m_tmpBlockSet)
            {
                if (block.FatBlock == null || !(block.FatBlock is MyShipConnector)) continue;

                var connector = block.FatBlock as MyShipConnector;
                if (connector.InConstraint) continue;
                if (connector == thisConnector) continue;
                if (!connector.IsWorking) continue;
                if (!connector.FriendlyWithBlock(thisConnector)) continue;

                m_tmpBlockSet.Clear();
                return connector;
            }

            m_tmpBlockSet.Clear();
            return null;
        }

        private void CreateConstraint(MyShipConnector otherConnector)
        {
            CreateConstraintNosync(otherConnector);
            if (Sync.IsServer)
            {
                MatrixD masterToSlave = CubeGrid.WorldMatrix * MatrixD.Invert(m_other.WorldMatrix);

                m_connectionState.Value = new State() {IsMaster = true, OtherEntityId = otherConnector.EntityId, MasterToSlave = null,MasterToSlaveGrid = masterToSlave};
                otherConnector.m_connectionState.Value = new State() { IsMaster = false, OtherEntityId = EntityId, MasterToSlave = null};
            }
        }

        private void CreateConstraintNosync(MyShipConnector otherConnector)
        {
            Debug.Assert(IsMaster, "Constraints should be created only master (entity with higher EntityId)");

            var posA = ConstraintPositionInGridSpace();
            var posB = otherConnector.ConstraintPositionInGridSpace();
            var axisA = ConstraintAxisGridSpace();
            var axisB = -otherConnector.ConstraintAxisGridSpace();

            var data = new HkHingeConstraintData();
            data.SetInBodySpace(posA, posB, axisA, axisB, CubeGrid.Physics, otherConnector.CubeGrid.Physics);
            var data2 = new HkMalleableConstraintData();
            data2.SetData(data);
            data.ClearHandle();
            data = null;
            data2.Strength = GetEffectiveStrength(otherConnector);

            var newConstraint = new HkConstraint(CubeGrid.Physics.RigidBody, otherConnector.CubeGrid.Physics.RigidBody, data2);
            SetConstraint(otherConnector, newConstraint);
            otherConnector.SetConstraint(this, newConstraint);

            AddConstraint(newConstraint);
        }

        private void SetConstraint(MyShipConnector other, HkConstraint newConstraint)
        {
            m_other = other;
            m_constraint = newConstraint;
            UpdateEmissivity();
        }

        private void UnsetConstraint()
        {
            Debug.Assert(InConstraint);
            m_other = null;
            m_constraint = null;
            UpdateEmissivity();
        }

        public Vector3 ConstraintPositionWorld()
        {
            return Vector3.Transform(ConstraintPositionInGridSpace(), CubeGrid.PositionComp.WorldMatrix);
        }

        private Vector3 ConstraintPositionInGridSpace()
        {
            var cubeCenter = (Max + Min) * CubeGrid.GridSize * 0.5f;
            Vector3 centerOffset = ConnectionPosition - cubeCenter;
            centerOffset = Vector3.DominantAxisProjection(centerOffset);

            MatrixI orientation = new MatrixI(Vector3I.Zero, this.Orientation.Forward, this.Orientation.Up);
            Vector3 outExtents;
            Vector3.Transform(ref centerOffset, ref orientation, out outExtents);

            return cubeCenter + centerOffset;
        }

        private Vector3 ConstraintAxisGridSpace()
        {
            var cubeCenter = (Max + Min) * CubeGrid.GridSize * 0.5f;
            var centerOffset = ConnectionPosition - cubeCenter;
            centerOffset = Vector3.Normalize(Vector3.DominantAxisProjection(centerOffset));

            return centerOffset;
        }

        private Vector3 ProjectPerpendicularFromWorld(Vector3 worldPerpAxis)
        {
            var axis = ConstraintAxisGridSpace();
            var localPerpAxis = Vector3.TransformNormal(worldPerpAxis, CubeGrid.PositionComp.WorldMatrixNormalizedInv);
            var projectionLength = Vector3.Dot(localPerpAxis, axis);
            var ret = Vector3.Normalize(localPerpAxis - projectionLength * axis);
            return Vector3.Normalize(localPerpAxis - projectionLength * axis);
        }

        private void Weld()
        {
            (IsMaster ? this : m_other).Weld(null);
        }

        private void Weld(Matrix? masterToSlave)
        {
            m_welding = true;
            m_welded = true;
            m_other.m_welded = true;
            Debug.Assert(IsMaster, "Only master can call connect");

            if (masterToSlave == null)
                masterToSlave = WorldMatrix * MatrixD.Invert(m_other.WorldMatrix);

            if (m_constraint != null)
            {
                RemoveConstraint(m_other, m_constraint);
                m_constraint = null;
                m_other.m_constraint = null;
            }

            WeldInternal();

            if (Sync.IsServer)
            {
                MatrixD masterToSlaveGrid = CubeGrid.WorldMatrix * MatrixD.Invert(m_other.WorldMatrix);
                m_connectionState.Value = new State() { IsMaster = true, OtherEntityId = m_other.EntityId, MasterToSlave = masterToSlave.Value, MasterToSlaveGrid = masterToSlaveGrid };
                m_other.m_connectionState.Value = new State() { IsMaster = false, OtherEntityId = EntityId, MasterToSlave = masterToSlave.Value };
            }
            m_other.m_other = this;
            m_welding = false;
        }

        private void WeldInternal()
        {
            Debug.Assert(!m_attachableConveyorEndpoint.AlreadyAttached());
            if (m_attachableConveyorEndpoint.AlreadyAttached()) m_attachableConveyorEndpoint.DetachAll();

            m_attachableConveyorEndpoint.Attach(m_other.m_attachableConveyorEndpoint);

            if (m_constraint != null)
            {
                RemoveConstraint(m_other, m_constraint);
                m_constraint = null;
                m_other.m_constraint = null;
            }

            this.Connected = true;
            m_other.Connected = true;

            MyWeldingGroups.Static.CreateLink(EntityId, CubeGrid, m_other.CubeGrid);
         
            UpdateEmissivity();
            m_other.UpdateEmissivity();

            if (CubeGrid != m_other.CubeGrid)
            {
                this.OnConstraintAdded(GridLinkTypeEnum.Logical, m_other.CubeGrid);
                this.OnConstraintAdded(GridLinkTypeEnum.Physical, m_other.CubeGrid);
            }

            CubeGrid.OnPhysicsChanged += CubeGrid_OnPhysicsChanged;

            CubeGrid.GridSystems.ConveyorSystem.FlagForRecomputation();
            m_other.CubeGrid.GridSystems.ConveyorSystem.FlagForRecomputation();
        }

        void CubeGrid_OnPhysicsChanged(MyEntity obj)
        {
            if (Sync.IsServer && m_welding == false && InConstraint && m_welded == false)
            {
                if (m_hasConstraint)
                {
                    if (MyPhysicsBody.IsConstraintValid(m_constraint) == false && m_constraint.IsDisposed == false)
                    {
                        RemoveConstraint(m_other, m_constraint);
                        this.m_constraint = null;
                        m_other.m_constraint = null;
                        m_hasConstraint = false;
                        m_other.m_hasConstraint = false;
                        
                        if (m_connectionState.Value.MasterToSlave.HasValue == false && CubeGrid.Physics != null && m_other.CubeGrid.Physics != null)
                        {
                            var posA = ConstraintPositionInGridSpace();
                            var posB = m_other.ConstraintPositionInGridSpace();
                            var axisA = ConstraintAxisGridSpace();
                            var axisB = -m_other.ConstraintAxisGridSpace();

                            var data = new HkHingeConstraintData();
                            data.SetInBodySpace(posA, posB, axisA, axisB, CubeGrid.Physics, m_other.CubeGrid.Physics);
                            var data2 = new HkMalleableConstraintData();
                            data2.SetData(data);
                            data.ClearHandle();
                            data = null;
                            data2.Strength = GetEffectiveStrength(m_other);

                            var newConstraint = new HkConstraint(CubeGrid.Physics.RigidBody, m_other.CubeGrid.Physics.RigidBody, data2);
                            this.SetConstraint(m_other, newConstraint);
                            m_other.SetConstraint(this, newConstraint);

                            AddConstraint(newConstraint);
                        }
                    }
                }
            }
        }

        private void AddConstraint(HkConstraint newConstraint)
        {
            m_hasConstraint = true;
            if(newConstraint.RigidBodyA != newConstraint.RigidBodyB)
                CubeGrid.Physics.AddConstraint(newConstraint);
        }

        public void Detach(bool synchronize = true)
        {
            if(IsMaster == false)
            {
                if(m_other != null)
                {
                    m_other.Detach(synchronize);
                }
                return;
            }

            Debug.Assert(InConstraint);
            Debug.Assert(m_other != null);
            if (!InConstraint || m_other == null) return;
       
            if (synchronize && Sync.IsServer)
            {
                m_connectionState.Value = State.DetachedMaster;
                m_other.m_connectionState.Value = State.Detached;
            }

            MyShipConnector other = m_other;
            DetachInternal();

            if (m_welded)
            {
                m_welding = true;
                m_welded = false;
                other.m_welded = false;
                UpdateEmissivity();
                other.UpdateEmissivity();
                if (MyWeldingGroups.Static.LinkExists(EntityId, CubeGrid, other.CubeGrid))
                {
                    MyWeldingGroups.Static.BreakLink(EntityId, CubeGrid, other.CubeGrid);
                }
                m_welding = false;

                if (Sync.IsServer && other.Closed == false && other.MarkedForClose == false && synchronize)
                {
                    TryAttach(other.EntityId);
                }
            }
        }

        private void DetachInternal()
        {
            if (!IsMaster)
            {
                m_other.DetachInternal();
                return;
            }

            Debug.Assert(this.InConstraint);
            Debug.Assert(this.m_other != null);
            if (!this.InConstraint || m_other == null) return;

            Debug.Assert(m_other.InConstraint);
            Debug.Assert((this.Connected && m_other.Connected) || (!this.Connected && !m_other.Connected));
            Debug.Assert(this.m_constraint == m_other.m_constraint);
            Debug.Assert(this == m_other.m_other);
            if (!m_other.InConstraint || m_other.m_other == null) return;

            var otherConnector = m_other;
            var constraint = this.m_constraint;
            bool wasConnected = this.Connected;

            this.Connected = false;
            this.UnsetConstraint();
            otherConnector.Connected = false;
            otherConnector.UnsetConstraint();

            if (constraint != null)
            {
                RemoveConstraint(otherConnector, constraint);
            }

            if (wasConnected)
            {
                m_attachableConveyorEndpoint.Detach(otherConnector.m_attachableConveyorEndpoint);
                if (CubeGrid != otherConnector.CubeGrid)
                {
                    this.OnConstraintRemoved(GridLinkTypeEnum.Logical, otherConnector.CubeGrid);
                    this.OnConstraintRemoved(GridLinkTypeEnum.Physical, otherConnector.CubeGrid);
                }

                CubeGrid.GridSystems.ConveyorSystem.FlagForRecomputation();
                otherConnector.CubeGrid.GridSystems.ConveyorSystem.FlagForRecomputation();
            }
        }

        private void RemoveConstraint(MyShipConnector otherConnector, HkConstraint constraint)
        {
            if (this.m_hasConstraint)
            {
                if (CubeGrid.Physics != null)
                    CubeGrid.Physics.RemoveConstraint(constraint);

                m_hasConstraint = false;
            }
            else
            {
                if  (otherConnector.CubeGrid.Physics != null)
                    otherConnector.CubeGrid.Physics.RemoveConstraint(constraint);

                otherConnector.m_hasConstraint = false;
            }
            constraint.Dispose();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            if (m_connectorDummy != null)
                m_connectorDummy.Activate();

            UpdateEmissivity();
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);

            if (m_connectorDummy != null)
                m_connectorDummy.Deactivate();

            if (InConstraint)
            {
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                Detach(false);
            }
        }

        protected override void OnOwnershipChanged()
        {
            base.OnOwnershipChanged();

            if (InConstraint && !m_other.FriendlyWithBlock(this))
            {
                Detach();
            }
        }

        protected override void WorldPositionChanged(object source)
        {
            base.WorldPositionChanged(source);
            if (this.m_connectorDummy != null && this.m_connectorDummy.Enabled && this.m_connectorDummy != source)
            {
                m_connectorDummy.OnWorldPositionChanged(source);
                if(CubeGrid.Physics != null)
                    m_connectorDummy.LinearVelocity = CubeGrid.Physics.GetVelocityAtPoint(WorldMatrix.Translation);
            }
        }

        protected override void Closing()
        {
            if (Connected)
            {
                Detach();
            }

            base.Closing();
        }


        protected override void BeforeDelete()
        {
            base.BeforeDelete();

            // The connector dummy won't be disposed of automatically, so we have to do it manually
            if (m_connectorDummy != null)
                m_connectorDummy.Close();
        }

        public override void DebugDrawPhysics()
        {
            base.DebugDrawPhysics();

            if (m_connectorDummy != null)
                m_connectorDummy.DebugDraw();
        }

        //connectors priority : static grid is always slave to dynamic
        // large grid is always slave to small grid
        // if grid size or physical state is same than higher entityId will be master
        // this is computed only first time when approaching and set only by server
        MyShipConnector GetMaster(MyShipConnector first, MyShipConnector second)
        {
            MyCubeGrid firstGrid = first.CubeGrid;
            MyCubeGrid secondGrid = second.CubeGrid;

            //static grids are always slaves
            if (firstGrid.IsStatic != secondGrid.IsStatic)
            {
                if (firstGrid.IsStatic)
                {
                    return second;
                }

                if (secondGrid.IsStatic)
                {
                    return first;
                }
            }
            else if (firstGrid.GridSize != secondGrid.GridSize)
            {
                if (firstGrid.GridSizeEnum == MyCubeSize.Large)
                {
                    return second;
                }

                if (secondGrid.GridSizeEnum == MyCubeSize.Large)
                {
                    return first;
                }

            }

            return first.EntityId < second.EntityId ? second : first; 
        }

        #region IMyConveyorEndpointBlock
        IMyConveyorEndpoint IMyConveyorEndpointBlock.ConveyorEndpoint
        {
            get { return m_attachableConveyorEndpoint; }
        }

        void IMyConveyorEndpointBlock.InitializeConveyorEndpoint()
        {
            m_attachableConveyorEndpoint = new MyAttachableConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_attachableConveyorEndpoint));
        }
        #endregion

        #region IMyShipConnector
        bool ModAPI.Ingame.IMyShipConnector.ThrowOut { get { return ThrowOut; } }
        bool ModAPI.Ingame.IMyShipConnector.CollectAll { get { return CollectAll; } }
        bool ModAPI.Ingame.IMyShipConnector.IsLocked { get { return IsWorking && InConstraint; } }
        bool ModAPI.Ingame.IMyShipConnector.IsConnected { get { return Connected; } }
        ModAPI.Ingame.IMyShipConnector ModAPI.Ingame.IMyShipConnector.OtherConnector { get { return m_other; } }
        IMyShipConnector ModAPI.IMyShipConnector.OtherConnector { get { return m_other; } }
        #endregion

        public bool UseConveyorSystem
        {
            get { return true; }
            set {  }
        }

        #region IMyInventoryOwner implementation

        int IMyInventoryOwner.InventoryCount
        {
            get { return InventoryCount; }
        }

        long IMyInventoryOwner.EntityId
        {
            get { return EntityId; }
        }

        bool IMyInventoryOwner.HasInventory
        {
            get { return HasInventory; }
        }

        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return UseConveyorSystem;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        IMyInventory IMyInventoryOwner.GetInventory(int index)
        {
            return this.GetInventory(index);
        }

        #endregion

        #region IMyConveyorEndpointBlock implementation

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPullInformation()
        {
            Sandbox.Game.GameSystems.Conveyors.PullInformation pullInformation = new PullInformation();
            pullInformation.Inventory = this.GetInventory(0);
            pullInformation.OwnerID = OwnerId;
            pullInformation.Constraint = new MyInventoryConstraint("Empty Constraint");
            return pullInformation;
        }

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPushInformation()
        {
            return null;
        }

        #endregion
    }
}
