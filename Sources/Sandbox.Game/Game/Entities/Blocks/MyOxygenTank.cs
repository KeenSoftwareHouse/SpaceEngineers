using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using SteamSDK;
using System;
using System.Diagnostics;
using VRage;
using VRageMath;
using System.Text;
using VRage.Utils;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.GameSystems;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.ModAPI;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_OxygenTank))]
    class MyOxygenTank : MyFunctionalBlock, IMyPowerConsumer, IMyInventoryOwner, IMyOxygenBlock, IMyOxygenTank, IMyConveyorEndpointBlock
    {
        private static string[] m_emissiveNames = { "Emissive1", "Emissive2", "Emissive3", "Emissive4" };
        
        private Color m_prevColor = Color.White;
        private int m_prevFillCount = -1;
        private bool m_useConveyorSystem;
        private MyInventory m_inventory;
        private bool m_autoRefill;

        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get
            {
                return m_conveyorEndpoint;
            }
        }

        private bool m_isStockpiling;
        public bool IsStockpiling
        {
            get
            {
                return m_isStockpiling;
            }
            set
            {
                m_isStockpiling = value;
            }
        }

        public bool CanStore
        {
            get
            {
                return MySession.Static.Settings.EnableOxygen && PowerReceiver.IsPowered && IsWorking && Enabled && IsFunctional;
            }
        }
        public MyPowerReceiver PowerReceiver
        {
            get;
            protected set;
        }
        private new MyOxygenTankDefinition BlockDefinition
        {
            get { return (MyOxygenTankDefinition)base.BlockDefinition; }
        }
        public float Capacity { get { return BlockDefinition.Capacity; } }
        public float FilledRatio { get; private set; }

        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyMultilineConveyorEndpoint(this);
        }

        static MyOxygenTank()
        {
            var isStockpiling = new MyTerminalControlOnOffSwitch<MyOxygenTank>("Stockpile", MySpaceTexts.BlockPropertyTitle_Stockpile, MySpaceTexts.BlockPropertyDescription_Stockpile);
            isStockpiling.Getter = (x) => x.IsStockpiling;
            isStockpiling.Setter = (x, v) => x.SyncObject.ChangeStockpileMode(v);
            isStockpiling.EnableToggleAction();
            isStockpiling.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(isStockpiling);

            var refillButton = new MyTerminalControlButton<MyOxygenTank>("Refill", MySpaceTexts.BlockPropertyTitle_Refill, MySpaceTexts.BlockPropertyTitle_Refill, OnRefillButtonPressed);
            refillButton.Enabled = (x) => x.CanRefill();
            refillButton.EnableAction();
            MyTerminalControlFactory.AddControl(refillButton);

            var autoRefill = new MyTerminalControlCheckbox<MyOxygenTank>("Auto-Refill", MySpaceTexts.BlockPropertyTitle_AutoRefill, MySpaceTexts.BlockPropertyTitle_AutoRefill);
            autoRefill.Getter = (x) => x.m_autoRefill;
            autoRefill.Setter = (x, v) => x.SyncObject.ChangeAutoRefill(v);
            autoRefill.EnableAction();
            MyTerminalControlFactory.AddControl(autoRefill);
        }

        public void RefillBottles()
        {
            var items = m_inventory.GetItems();
            bool changed = false;
            foreach (var item in items)
            {
                if (FilledRatio == 0f)
                {
                    break;
                }
                var oxygenContainer = item.Content as MyObjectBuilder_OxygenContainerObject;
                if (oxygenContainer != null)
                {
                    if (oxygenContainer.OxygenLevel < 1f)
                    {
                        var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(oxygenContainer) as MyOxygenContainerDefinition;
                        float bottleOxygenAmount = oxygenContainer.OxygenLevel * physicalItem.Capacity;
                        float tankOxygenAmount = FilledRatio * Capacity;


                        float transferredAmount = Math.Min(physicalItem.Capacity - bottleOxygenAmount, tankOxygenAmount);
                        oxygenContainer.OxygenLevel = (bottleOxygenAmount + transferredAmount) / physicalItem.Capacity;

                        if (oxygenContainer.OxygenLevel > 1f)
                        {
                            oxygenContainer.OxygenLevel = 1f;
                        }
                        
                        FilledRatio -= transferredAmount / Capacity;
                        if (FilledRatio < 0f)
                        {
                            FilledRatio = 0f;
                        }
                        m_inventory.UpdateOxygenAmount();
                        m_inventory.SyncOxygenContainerLevel(item.ItemId, oxygenContainer.OxygenLevel);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                SyncObject.ChangeFillRatioAmount(FilledRatio);

                UpdateEmissivity();
                UdpateText();
            }
        }

        private static void OnRefillButtonPressed(MyOxygenTank tank)
        {
            if (tank.IsWorking)
            {
                tank.SyncObject.SendRefillRequest();
            }
        }

        private bool CanRefill()
        {
            if (!CanStore || this.FilledRatio == 0)
            {
                return false;
            }

            var items = m_inventory.GetItems();
            foreach (var item in items)
            {
                var oxygenContainer = item.Content as MyObjectBuilder_OxygenContainerObject;
                if (oxygenContainer != null)
                {
                    if (oxygenContainer.OxygenLevel < 1f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            base.Init(objectBuilder, cubeGrid);

            var builder = (MyObjectBuilder_OxygenTank)objectBuilder;
            m_isStockpiling = builder.IsStockpiling;
            FilledRatio = builder.FilledRatio;

            InitializeConveyorEndpoint();

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
            m_inventory = new MyInventory(
                BlockDefinition.InventoryMaxVolume,
                BlockDefinition.InventorySize,
                MyInventoryFlags.CanReceive,
                this);
            m_inventory.Constraint = BlockDefinition.InputInventoryConstraint;
            m_inventory.Init(builder.Inventory);

            m_inventory.ContentsChanged += m_inventory_ContentsChanged;

            m_autoRefill = builder.AutoRefill;

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Factory,
                false,
                BlockDefinition.OperationalPowerConsumption,
                ComputeRequiredPower);
            PowerReceiver.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
            PowerReceiver.Update();

            UpdateEmissivity();
            UdpateText();

            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            IsWorkingChanged += MyOxygenTank_IsWorkingChanged;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_OxygenTank)base.GetObjectBuilderCubeBlock(copy);

            builder.IsStockpiling = m_isStockpiling;
            builder.FilledRatio = FilledRatio;
            builder.AutoRefill = m_autoRefill;
            builder.Inventory = m_inventory.GetObjectBuilder();

            return builder;
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            if (Sync.IsServer && IsWorking)
            {
                if (FilledRatio > 0f && m_useConveyorSystem && m_inventory.VolumeFillFactor < 0.6f)
                {
                    MyGridConveyorSystem.PullAllRequest(this, m_inventory, OwnerId, m_inventory.Constraint);
                }

                if (m_autoRefill && CanRefill())
                {
                    RefillBottles();
                }
            }
        }

        protected override bool CheckIsWorking()
        {
            return base.CheckIsWorking() && PowerReceiver.IsPowered;
        }

        private float ComputeRequiredPower()
        {
            if (!MySession.Static.Settings.EnableOxygen)
            {
                return 0f;
            }

            return (Enabled && IsFunctional) ? BlockDefinition.OperationalPowerConsumption
                                             : 0.0f;
        }

        void m_inventory_ContentsChanged(MyInventory obj)
        {
            RaisePropertiesChanged();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            UpdateEmissivity();
            UdpateText();
        }

        void PowerReceiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            UpdateEmissivity();
        }

        void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
            UpdateEmissivity();

            FilledRatio = 0f;

            if (Sync.IsServer)
            {
                SyncObject.ChangeFillRatioAmount(FilledRatio);
            }
        }

        void MyOxygenTank_IsWorkingChanged(MyCubeBlock obj)
        {
            PowerReceiver.Update();
            UpdateEmissivity();
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();
            PowerReceiver.Update();
            UpdateEmissivity();
        }

        public override void OnRegisteredToGridSystems()
        {
            base.OnRegisteredToGridSystems();

            if (CubeGrid.GridSystems.OxygenSystem != null)
            {
                CubeGrid.GridSystems.OxygenSystem.RegisterOxygenBlock(this);
            }
        }

        public override void OnUnregisteredFromGridSystems()
        {
            base.OnUnregisteredFromGridSystems();

            if (CubeGrid.GridSystems.OxygenSystem != null)
            {
                CubeGrid.GridSystems.OxygenSystem.UnregisterOxygenBlock(this);
            }
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();

            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (CanStore)
            {
                if (IsStockpiling)
                {
                    SetEmissive(Color.Teal, FilledRatio);
                }
                else
                {
                    SetEmissive(Color.Green, FilledRatio);
                }
            }
            else
            {
                SetEmissive(Color.Red, 1f);
            }
        }

        private void UdpateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.MaxRequiredInput, DetailedInfo);
            DetailedInfo.Append("\n");
            if (!MySession.Static.Settings.EnableOxygen)
            {
                DetailedInfo.Append("Oxygen disabled in world settigns!");
            }
            else
            {
                DetailedInfo.Append("Filled: " + (FilledRatio * 100f).ToString("F4") + "%");
            }

            RaisePropertiesChanged();
        }


        private void SetEmissive(Color color, float fill)
        {
            int fillCount = (int)(fill * m_emissiveNames.Length);

            if (Render.RenderObjectIDs[0] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED && (color != m_prevColor || fillCount != m_prevFillCount))
            {
                for (int i = 0; i < m_emissiveNames.Length; i++)
                {
                    if (i <= fillCount)
                    {
                        VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[i], null, color, null, null, 0);
                    }
                    else
                    {
                        VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[i], null, Color.Black, null, null, 0);
                    }
                }
                m_prevColor = color;
                m_prevFillCount = fillCount;
            }
        }

        public override void OnModelChange()
        {
            base.OnModelChange();

            m_prevFillCount = -1;
        }

        #region Inventory
        public int InventoryCount
        {
            get
            {
                return 1;
            }
        }
        public MyInventory GetInventory(int index)
        {
            return m_inventory;
        }

        Sandbox.ModAPI.Interfaces.IMyInventory Sandbox.ModAPI.Interfaces.IMyInventoryOwner.GetInventory(int index)
        {
            return GetInventory(index);
        }

        public MyInventoryOwnerTypeEnum InventoryOwnerType
        {
            get
            {
                return MyInventoryOwnerTypeEnum.System;
            }
        }
        public bool UseConveyorSystem
        {
            get
            {
                return m_useConveyorSystem;
            }
            set
            {
                m_useConveyorSystem = value;
            }
        }

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(m_inventory);
            base.OnRemovedByCubeBuilder();
        }

        public override void OnDestroy()
        {
            ReleaseInventory(m_inventory, true);
            base.OnDestroy();
        }
        #endregion

        bool IMyOxygenBlock.IsWorking()
        {
            return CanStore;
        }

        internal void Fill(float amount)
        {
            FilledRatio += amount / Capacity;
            FilledRatio = Math.Min(1f, FilledRatio);

            if (Sync.IsServer)
            {
                SyncObject.ChangeFillRatioAmount(FilledRatio);
            }

            UpdateEmissivity();
            UdpateText();
        }

        internal void ChangeFilledRatio(float newFilledRatio)
        {
            FilledRatio = newFilledRatio;

            UpdateEmissivity();
            UdpateText();
        }

        internal void Drain(float amount)
        {
            Debug.Assert(!IsStockpiling, "Stockpiling tank should not be drained");
            FilledRatio -= amount / Capacity;
            FilledRatio = Math.Max(0f, FilledRatio);

            if (Sync.IsServer)
            {
                SyncObject.ChangeFillRatioAmount(FilledRatio);
            }

            UpdateEmissivity();
            UdpateText();
        }

        public float GetOxygenLevel()
        {
            return FilledRatio;
        }

        #region Sync
        protected override MySyncEntity OnCreateSync()
        {
            return new MySyncOxygenTank(this);
        }

        internal new MySyncOxygenTank SyncObject
        {
            get
            {
                return (MySyncOxygenTank)base.SyncObject;
            }
        }

        [PreloadRequired]
        internal class MySyncOxygenTank : MySyncEntity
        {
            [MessageIdAttribute(7700, P2PMessageEnum.Reliable)]
            protected struct ChangeStockpileModeMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public BoolBlit IsStockpiling;
            }

            [MessageIdAttribute(7701, P2PMessageEnum.Unreliable)]
            protected struct FilledRatioMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public float FilledRatio;
            }
            [MessageIdAttribute(7702, P2PMessageEnum.Reliable)]
            protected struct ChangeAutoRefillMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public BoolBlit AutoRefill;
            }

            [MessageIdAttribute(7703, P2PMessageEnum.Reliable)]
            protected struct RefillRequestMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }
            }

            private MyOxygenTank m_tank;

            static MySyncOxygenTank()
            {
                MySyncLayer.RegisterEntityMessage<MySyncOxygenTank, ChangeStockpileModeMsg>(OnStockipleModeChanged, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncOxygenTank, ChangeAutoRefillMsg>(OnAutoRefillChanged, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncOxygenTank, FilledRatioMsg>(OnFilledRatioChanged, MyMessagePermissions.FromServer);
                MySyncLayer.RegisterEntityMessage<MySyncOxygenTank, RefillRequestMsg>(OnRefillRequest, MyMessagePermissions.ToServer);
            }

            public MySyncOxygenTank(MyOxygenTank tank)
                : base(tank)
            {
                m_tank = tank;
            }

            public void ChangeStockpileMode(bool newStockpileMode)
            {
                var msg = new ChangeStockpileModeMsg();
                msg.EntityId = m_tank.EntityId;
                msg.IsStockpiling = newStockpileMode;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            public void ChangeFillRatioAmount(float newFilledRatio)
            {
                var msg = new FilledRatioMsg();
                msg.EntityId = m_tank.EntityId;
                msg.FilledRatio = newFilledRatio;

                Sync.Layer.SendMessageToAll(msg);
            }

            public void ChangeAutoRefill(bool newAutoRefill)
            {
                var msg = new ChangeAutoRefillMsg();
                msg.EntityId = m_tank.EntityId;
                msg.AutoRefill = newAutoRefill;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            public void SendRefillRequest()
            {
                if (Sync.IsServer)
                {
                    m_tank.RefillBottles();
                }
                else
                {
                    var msg = new RefillRequestMsg();
                    msg.EntityId = m_tank.EntityId;

                    Sync.Layer.SendMessageToServer(ref msg);
                }
            }

            private static void OnStockipleModeChanged(MySyncOxygenTank syncObject, ref ChangeStockpileModeMsg message, World.MyNetworkClient sender)
            {
                syncObject.m_tank.IsStockpiling = message.IsStockpiling;
            }

            private static void OnFilledRatioChanged(MySyncOxygenTank syncObject, ref FilledRatioMsg message, World.MyNetworkClient sender)
            {
                syncObject.m_tank.ChangeFilledRatio(message.FilledRatio);
            }

            private static void OnAutoRefillChanged(MySyncOxygenTank syncObject, ref ChangeAutoRefillMsg message, World.MyNetworkClient sender)
            {
                syncObject.m_tank.m_autoRefill = message.AutoRefill;
            }

            private static void OnRefillRequest(MySyncOxygenTank syncObject, ref RefillRequestMsg message, MyNetworkClient sender)
            {
                syncObject.m_tank.RefillBottles();
            }
        }
        #endregion
    }
}
