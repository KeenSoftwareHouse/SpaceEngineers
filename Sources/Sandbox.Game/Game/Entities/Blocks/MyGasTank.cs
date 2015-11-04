using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRageMath;
using System.Text;
using Sandbox.Common;
using VRage.Utils;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.GameSystems;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.EntityComponents;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ModAPI;
using VRageRender;
using VRage.Components;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_OxygenTank))]
    class MyGasTank : MyFunctionalBlock, IMyInventoryOwner, IMyGasBlock, IMyOxygenTank
    {
        private static readonly string[] m_emissiveNames = { "Emissive1", "Emissive2", "Emissive3", "Emissive4" };
        
        private Color m_prevColor = Color.White;
        private int m_prevFillCount = -1;
	    private MyInventory m_inventory;
        private bool m_autoRefill;
	    private int m_updateCounter = 0;
	    private int m_lastOutputUpdateTime;
	    private int m_lastInputUpdateTime;
	    private float m_nextGasTransfer = 0f;

        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint { get { return m_conveyorEndpoint; } }

	    private bool m_isStockpiling;
	    public bool IsStockpiling { get { return m_isStockpiling; } private set { SetStockpilingState(value); } }

		public bool CanStore { get { return (MySession.Static.Settings.EnableOxygen || BlockDefinition.StoredGasId != m_oxygenGasId) && IsWorking && Enabled && IsFunctional; } }

		private MyResourceSourceComponent m_sourceComp;
		public MyResourceSourceComponent SourceComp
		{
			get { return m_sourceComp; }
			set { if (Components.Contains(typeof(MyResourceSourceComponent))) Components.Remove<MyResourceSourceComponent>(); Components.Add<MyResourceSourceComponent>(value); m_sourceComp = value; }
		}

	    readonly MyDefinitionId m_oxygenGasId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Oxygen");	// Required for oxygen MyFake checks

        public new MyGasTankDefinition BlockDefinition { get { return (MyGasTankDefinition)base.BlockDefinition; } }

		private float GasOutputPerSecond { get { return (SourceComp.ProductionEnabledByType(BlockDefinition.StoredGasId) ? SourceComp.CurrentOutputByType(BlockDefinition.StoredGasId) : 0f); } }
		private float GasInputPerSecond { get { return ResourceSink.CurrentInputByType(BlockDefinition.StoredGasId); } }
		private float GasOutputPerUpdate { get { return GasOutputPerSecond*MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS; } }
		private float GasInputPerUpdate { get { return GasInputPerSecond*MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS; } }

        public float Capacity { get { return BlockDefinition.Capacity; } }
        public float FilledRatio { get; private set; }

        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyMultilineConveyorEndpoint(this);
        }

        static MyGasTank()
        {
	        var isStockpiling = new MyTerminalControlOnOffSwitch<MyGasTank>("Stockpile", MySpaceTexts.BlockPropertyTitle_Stockpile, MySpaceTexts.BlockPropertyDescription_Stockpile)
	        {
		        Getter = (x) => x.IsStockpiling,
		        Setter = (x, v) => x.SyncObject.ChangeStockpileMode(v)
	        };
	        isStockpiling.EnableToggleAction();
            isStockpiling.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(isStockpiling);

	        var refillButton = new MyTerminalControlButton<MyGasTank>("Refill", MySpaceTexts.BlockPropertyTitle_Refill, MySpaceTexts.BlockPropertyTitle_Refill, OnRefillButtonPressed)
	        {
		        Enabled = (x) => x.CanRefill()
	        };
	        refillButton.EnableAction();
            MyTerminalControlFactory.AddControl(refillButton);

	        var autoRefill = new MyTerminalControlCheckbox<MyGasTank>("Auto-Refill", MySpaceTexts.BlockPropertyTitle_AutoRefill, MySpaceTexts.BlockPropertyTitle_AutoRefill)
	        {
		        Getter = (x) => x.m_autoRefill,
		        Setter = (x, v) => x.SyncObject.ChangeAutoRefill(v)
	        };
	        autoRefill.EnableAction();
            MyTerminalControlFactory.AddControl(autoRefill);

        }

	    public MyGasTank()
	    {
			SourceComp = new MyResourceSourceComponent();
			ResourceSink = new MyResourceSinkComponent(2);
	    }

		public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
		{
			SyncFlag = true;

			base.Init(objectBuilder, cubeGrid);

			var builder = (MyObjectBuilder_OxygenTank)objectBuilder;
			IsStockpiling = builder.IsStockpiling;

			InitializeConveyorEndpoint();

			NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
			m_inventory = new MyInventory(
				BlockDefinition.InventoryMaxVolume,
					BlockDefinition.InventorySize,
					MyInventoryFlags.CanReceive,
					this)
			{
				Constraint = BlockDefinition.InputInventoryConstraint
			};
			m_inventory.Init(builder.Inventory);
			m_inventory.ContentsChanged += m_inventory_ContentsChanged;

			m_autoRefill = builder.AutoRefill;

			var sourceDataList = new List<MyResourceSourceInfo>
			{
				new MyResourceSourceInfo {ResourceTypeId = BlockDefinition.StoredGasId, DefinedOutput = 0.05f*BlockDefinition.Capacity},
			};
			SourceComp.Init(BlockDefinition.ResourceSourceGroup, sourceDataList);
			SourceComp.OutputChanged += Source_OutputChanged;

			var sinkDataList = new List<MyResourceSinkInfo>
	        {
				new MyResourceSinkInfo {ResourceTypeId = MyResourceDistributorComponent.ElectricityId, MaxRequiredInput = BlockDefinition.OperationalPowerConsumption, RequiredInputFunc = ComputeRequiredPower},
				new MyResourceSinkInfo {ResourceTypeId = BlockDefinition.StoredGasId, MaxRequiredInput = Capacity, RequiredInputFunc = ComputeRequiredGas},
	        };

			ResourceSink.Init(
				BlockDefinition.ResourceSinkGroup,
				sinkDataList);
			ResourceSink.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
			ResourceSink.CurrentInputChanged += Sink_CurrentInputChanged;

			m_lastOutputUpdateTime = m_updateCounter;
			m_lastInputUpdateTime = m_updateCounter;
			m_nextGasTransfer = 0f;

			ChangeFilledRatio(builder.FilledRatio);
			ResourceSink.Update();

			AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));

			SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
			IsWorkingChanged += MyOxygenTank_IsWorkingChanged;
		}

		public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
		{
			var builder = (MyObjectBuilder_OxygenTank)base.GetObjectBuilderCubeBlock(copy);

			builder.IsStockpiling = IsStockpiling;
			builder.FilledRatio = FilledRatio;
			builder.AutoRefill = m_autoRefill;
			builder.Inventory = m_inventory.GetObjectBuilder();

			return builder;
		}

        public void RefillBottles()
        {
            var items = m_inventory.GetItems();
            bool changed = false;
            float newFilledRatio = FilledRatio;
            foreach (var item in items)
            {
                if (FilledRatio == 0f)
                    break;

                var gasContainer = item.Content as MyObjectBuilder_GasContainerObject;
	            if (gasContainer == null || gasContainer.GasLevel >= 1f)
					continue;

	            var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(gasContainer) as MyOxygenContainerDefinition;
				Debug.Assert(physicalItem != null);

	            float bottleGasAmount = gasContainer.GasLevel * physicalItem.Capacity;
	            float tankGasAmount = FilledRatio * Capacity;

	            float transferredAmount = Math.Min(physicalItem.Capacity - bottleGasAmount, tankGasAmount);
				gasContainer.GasLevel = Math.Min((bottleGasAmount + transferredAmount) / physicalItem.Capacity, 1f);
                        
	            newFilledRatio = Math.Max(FilledRatio - transferredAmount / Capacity, 0f);

				m_inventory.UpdateGasAmount();
	            changed = true;
            }

            if (changed)
                ChangeFilledRatio(newFilledRatio, true);
        }

        private static void OnRefillButtonPressed(MyGasTank tank)
        {
            if (tank.IsWorking)
                tank.SyncObject.SendRefillRequest();
        }

        private bool CanRefill()
        {
            if (!CanStore || !ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) || FilledRatio == 0)
                return false;

            var items = m_inventory.GetItems();
            foreach (var item in items)
            {
                var gasContainer = item.Content as MyObjectBuilder_GasContainerObject;
	            if (gasContainer == null)
					continue;

	            if (gasContainer.GasLevel < 1f)
		            return true;
            }

            return false;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            ++m_updateCounter;

			int sourceUpdateFrames = (m_updateCounter - m_lastOutputUpdateTime);
			int sinkUpdateFrames = (m_updateCounter - m_lastInputUpdateTime);

			float gasOutput = GasOutputPerUpdate * sourceUpdateFrames;
			float gasInput = GasInputPerUpdate * sinkUpdateFrames;

	        float totalTransfer = gasInput - gasOutput + m_nextGasTransfer;
	        if (CheckTransfer(totalTransfer))
	        {
		        Transfer(totalTransfer);
				m_updateCounter = 0;
				m_lastOutputUpdateTime = m_updateCounter;
				m_lastInputUpdateTime = m_updateCounter;
	        }
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

	        if (!Sync.IsServer || !IsWorking)
				return;

	        if (FilledRatio > 0f && UseConveyorSystem && m_inventory.VolumeFillFactor < 0.6f)
		        MyGridConveyorSystem.PullAllRequest(this, m_inventory, OwnerId, m_inventory.Constraint);

	        if (m_autoRefill && CanRefill())
		        RefillBottles();

			int sinkUpdateFrames = (m_updateCounter - m_lastInputUpdateTime);
			int sourceUpdateFrames = (m_updateCounter - m_lastOutputUpdateTime);

			float gasInput = GasInputPerUpdate * sinkUpdateFrames;
			float gasOutput = GasOutputPerUpdate * sourceUpdateFrames;
	        float totalTransfer = gasInput - gasOutput + m_nextGasTransfer;
			Transfer(totalTransfer);

			m_updateCounter = 0;
			m_lastOutputUpdateTime = m_updateCounter;
			m_lastInputUpdateTime = m_updateCounter;
			ResourceSink.Update();
        }

        protected override bool CheckIsWorking()
        {
	        return base.CheckIsWorking() && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId);
        }

        private float ComputeRequiredPower()
        {
            if (!MySession.Static.Settings.EnableOxygen && BlockDefinition.StoredGasId == m_oxygenGasId || !Enabled || !IsFunctional)
                return 0f;

            return (SourceComp.CurrentOutputByType(BlockDefinition.StoredGasId) > 0 || ResourceSink.CurrentInputByType(BlockDefinition.StoredGasId) > 0)
                    ? BlockDefinition.OperationalPowerConsumption : BlockDefinition.StandbyPowerConsumption;
        }

	    private float ComputeRequiredGas()
	    {
	        if (!CanStore)
	            return 0f;

	        float neededRatioToFillInUpdate = (1 - FilledRatio)*MyEngineConstants.UPDATE_STEPS_PER_SECOND;
            return Math.Min(neededRatioToFillInUpdate, 0.05f) * Capacity;
	    }

        void m_inventory_ContentsChanged(MyInventoryBase obj)
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
            SourceComp.Enabled = CanStore;
            ResourceSink.Update();
			ChangeFilledRatio(0f, Sync.IsServer);
        }

        void MyOxygenTank_IsWorkingChanged(MyCubeBlock obj)
        {
			SourceComp.Enabled = CanStore;
            ResourceSink.Update();
            UpdateEmissivity();
        }

		protected override void OnEnabledChanged()
		{
			base.OnEnabledChanged();
			SourceComp.Enabled = CanStore;
            ResourceSink.Update();
			UpdateEmissivity();
		}

		private void Source_OutputChanged(MyDefinitionId changedResourceId, float oldOutput, MyResourceSourceComponent source)
		{
			if (changedResourceId != BlockDefinition.StoredGasId)
				return;

			float timeSinceLastUpdateSeconds = (m_updateCounter - m_lastOutputUpdateTime) / MyEngineConstants.UPDATE_STEPS_PER_SECOND;
			float outputAmount = oldOutput*timeSinceLastUpdateSeconds;
			m_nextGasTransfer -= outputAmount;
			m_lastOutputUpdateTime = m_updateCounter;
		}

		private void Sink_CurrentInputChanged(MyDefinitionId resourceTypeId, float oldInput, MyResourceSinkComponent sink)
	    {
			if (resourceTypeId != BlockDefinition.StoredGasId)
				return;

			float timeSinceLastUpdateSeconds = (m_updateCounter - m_lastInputUpdateTime) / MyEngineConstants.UPDATE_STEPS_PER_SECOND;
			float inputAmount = oldInput*timeSinceLastUpdateSeconds;
			m_nextGasTransfer += inputAmount;
			m_lastInputUpdateTime = m_updateCounter;
	    }

	    private bool CheckTransfer(float testTransfer)
	    {
			if(testTransfer == 0f)
				return false;

		    float remainingCapacity = SourceComp.RemainingCapacityByType(BlockDefinition.StoredGasId);
			float nextCapacity = remainingCapacity + testTransfer;
			float gasTransferPerUpdate = GasInputPerUpdate - GasOutputPerUpdate;
			return (nextCapacity + gasTransferPerUpdate * 10 <= 0f || nextCapacity + gasTransferPerUpdate * 10 >= Capacity);
	    }

        public override void UpdateVisual()
        {
            base.UpdateVisual();

            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (CanStore)
	            SetEmissive(IsStockpiling ? Color.Teal : Color.Green, FilledRatio);
            else
                SetEmissive(Color.Red, 1f);
        }

        private void UdpateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
			MyValueFormatter.AppendWorkInBestUnit(ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId), DetailedInfo);
            DetailedInfo.Append("\n");
            if (!(MySession.Static.Settings.EnableOxygen || BlockDefinition.StoredGasId != m_oxygenGasId))
            {
                DetailedInfo.Append("Oxygen disabled in world settings!");
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

	        if (Render.RenderObjectIDs[0] == MyRenderProxy.RENDER_ID_UNASSIGNED || (color == m_prevColor && fillCount == m_prevFillCount))
				return;

	        for (int nameIndex = 0; nameIndex < m_emissiveNames.Length; ++nameIndex)
	        {
		        MyRenderProxy.UpdateColorEmissivity(Render.RenderObjectIDs[0], 0, m_emissiveNames[nameIndex], nameIndex < fillCount ? color : Color.Black, 0);
	        }
	        m_prevColor = color;
	        m_prevFillCount = fillCount;
        }

        public override void OnModelChange()
        {
            base.OnModelChange();

            m_prevFillCount = -1;
        }

        #region Inventory
        public int InventoryCount { get { return 1; } }
        public MyInventory GetInventory(int index)
        {
            return m_inventory;
        }

        Sandbox.ModAPI.Interfaces.IMyInventory Sandbox.ModAPI.Interfaces.IMyInventoryOwner.GetInventory(int index)
        {
            return GetInventory(index);
        }

        public MyInventoryOwnerTypeEnum InventoryOwnerType { get { return MyInventoryOwnerTypeEnum.System; } }
        public bool UseConveyorSystem { get; set; }

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

        public void SetInventory(Sandbox.Game.MyInventory inventory, int index)
        {
            throw new NotImplementedException("TODO Dusan inventory sync");
        }

        #endregion

        bool IMyGasBlock.IsWorking()
        {
            return CanStore;
        }

	    private void SetStockpilingState(bool newState)
	    {
		    m_isStockpiling = newState;

            SourceComp.SetProductionEnabledByType(BlockDefinition.StoredGasId, !m_isStockpiling && CanStore);
	    }

	    private void Transfer(float transferAmount)
	    {
			if (transferAmount > 0)
				Fill(transferAmount);
			else if (transferAmount < 0)
				Drain(-transferAmount);
	    }

        internal void Fill(float amount)
        {
            if (amount == 0f)
                return;

			ChangeFilledRatio(Math.Min(1f, FilledRatio + amount / Capacity), Sync.IsServer);
        }

		internal void Drain(float amount)
		{
			if (amount == 0f)
				return;

			ChangeFilledRatio(Math.Max(0f, FilledRatio - amount / Capacity), Sync.IsServer);
		}

        internal void ChangeFilledRatio(float newFilledRatio, bool updateSync = false)
        {
	        m_nextGasTransfer = 0f;
            float oldFilledRatio = FilledRatio;

			if (oldFilledRatio != newFilledRatio)
			{
				if(updateSync)
					SyncObject.ChangeFillRatioAmount(newFilledRatio);
				FilledRatio = newFilledRatio;
				SourceComp.SetRemainingCapacityByType(BlockDefinition.StoredGasId, FilledRatio*Capacity);
				ResourceSink.Update();
				UpdateEmissivity();
				UdpateText();
			}

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

        internal new MySyncOxygenTank SyncObject { get { return (MySyncOxygenTank)base.SyncObject; } }

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

            private readonly MyGasTank m_tank;

            static MySyncOxygenTank()
            {
                //MySyncLayer.RegisterEntityMessage<MySyncOxygenTank, ChangeStockpileModeMsg>(OnStockipleModeChanged, MyMessagePermissions.Any);
                //MySyncLayer.RegisterEntityMessage<MySyncOxygenTank, ChangeAutoRefillMsg>(OnAutoRefillChanged, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncOxygenTank, FilledRatioMsg>(OnFilledRatioChanged, MyMessagePermissions.FromServer);
                MySyncLayer.RegisterEntityMessage<MySyncOxygenTank, RefillRequestMsg>(OnRefillRequest, MyMessagePermissions.ToServer);
            }

            public MySyncOxygenTank(MyGasTank tank)
                : base(tank)
            {
                m_tank = tank;
            }

            public void ChangeStockpileMode(bool newStockpileMode)
            {
                var msg = new ChangeStockpileModeMsg();
                msg.EntityId = m_tank.EntityId;
                msg.IsStockpiling = newStockpileMode;

                Sync.Layer.SendMessageToServerAndSelf(ref msg);
            }

            public void ChangeFillRatioAmount(float newFilledRatio)
            {
                var msg = new FilledRatioMsg();
                msg.EntityId = m_tank.EntityId;
                msg.FilledRatio = newFilledRatio;

                Sync.Layer.SendMessageToAll(ref msg);
            }

            public void ChangeAutoRefill(bool newAutoRefill)
            {
                var msg = new ChangeAutoRefillMsg();
                msg.EntityId = m_tank.EntityId;
                msg.AutoRefill = newAutoRefill;

                Sync.Layer.SendMessageToServerAndSelf(ref msg);
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
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref message, sender.SteamUserId);
                }
            }

            private static void OnFilledRatioChanged(MySyncOxygenTank syncObject, ref FilledRatioMsg message, World.MyNetworkClient sender)
            {
                syncObject.m_tank.ChangeFilledRatio(message.FilledRatio);
            }

            private static void OnAutoRefillChanged(MySyncOxygenTank syncObject, ref ChangeAutoRefillMsg message, World.MyNetworkClient sender)
            {
                syncObject.m_tank.m_autoRefill = message.AutoRefill;
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref message, sender.SteamUserId);
                }
            }

            private static void OnRefillRequest(MySyncOxygenTank syncObject, ref RefillRequestMsg message, MyNetworkClient sender)
            {
                syncObject.m_tank.RefillBottles();
            }
        }
        #endregion
    }
}
