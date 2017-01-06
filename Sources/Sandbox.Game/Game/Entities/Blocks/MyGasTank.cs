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
using Sandbox.ModAPI;
using Sandbox.Game.GameSystems;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.EntityComponents;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ModAPI;
using VRageRender;
using VRage.Game.Components;
using IMyInventoryOwner = VRage.Game.ModAPI.Ingame.IMyInventoryOwner;
using Sandbox.Engine.Utils;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.Network;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_OxygenTank))]
    public class MyGasTank : MyFunctionalBlock, IMyGasBlock, IMyOxygenTank, VRage.Game.ModAPI.Ingame.IMyInventoryOwner
    {
        private static readonly string[] m_emissiveNames = { "Emissive0", "Emissive1", "Emissive2", "Emissive3" };
        
        private Color m_prevColor = Color.White;
        private int m_prevFillCount = -1;
        private bool m_autoRefill;
        private const float m_maxFillPerSecond = 0.05f;

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
		private float GasOutputPerUpdate { get { return GasOutputPerSecond * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS; } }
		private float GasInputPerUpdate { get { return GasInputPerSecond * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS; } }

        public float Capacity { get { return BlockDefinition.Capacity; } }
        public float FilledRatio { get; private set; }

        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyMultilineConveyorEndpoint(this);
        }

	    public MyGasTank()
	    {
            CreateTerminalControls();

			SourceComp = new MyResourceSourceComponent();
			ResourceSink = new MyResourceSinkComponent(2);
	    }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyGasTank>())
                return;
            base.CreateTerminalControls();
            var isStockpiling = new MyTerminalControlOnOffSwitch<MyGasTank>("Stockpile", MySpaceTexts.BlockPropertyTitle_Stockpile, MySpaceTexts.BlockPropertyDescription_Stockpile)
            {
                Getter = (x) => x.IsStockpiling,
                Setter = (x, v) => x.ChangeStockpileMode(v)
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
                Setter = (x, v) => x.ChangeAutoRefill(v)
            };
            autoRefill.EnableAction();
            MyTerminalControlFactory.AddControl(autoRefill);

        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
		{
			SyncFlag = true;

			base.Init(objectBuilder, cubeGrid);

			var builder = (MyObjectBuilder_OxygenTank)objectBuilder;

            InitializeConveyorEndpoint();

			NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

            if (MyFakes.ENABLE_INVENTORY_FIX)
            {
                FixSingleInventory();

                if (this.GetInventory() != null)
                    this.GetInventory().Constraint = BlockDefinition.InputInventoryConstraint;
            }

            if (this.GetInventory() == null)
            {
                MyInventory inventory = new MyInventory(BlockDefinition.InventoryMaxVolume, BlockDefinition.InventorySize, MyInventoryFlags.CanReceive);
                inventory.Constraint = BlockDefinition.InputInventoryConstraint;
                Components.Add<MyInventoryBase>(inventory);
                inventory.Init(builder.Inventory);
            }
            this.GetInventory().ContentsChanged += MyGasTank_ContentsChanged;
            Debug.Assert(this.GetInventory().Owner == this, "Ownership was not set!");
            
			m_autoRefill = builder.AutoRefill;

			var sourceDataList = new List<MyResourceSourceInfo>
			{
				new MyResourceSourceInfo {ResourceTypeId = BlockDefinition.StoredGasId, DefinedOutput = 0.05f*BlockDefinition.Capacity},
			};
			SourceComp.Init(BlockDefinition.ResourceSourceGroup, sourceDataList);
			SourceComp.OutputChanged += Source_OutputChanged;

            SourceComp.Enabled = Enabled;

            //Set after SourceComp.Init otherwise overwritten
            IsStockpiling = builder.IsStockpiling;

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
         
			ChangeFilledRatio(builder.FilledRatio);
			ResourceSink.Update();

			AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));

			SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
			IsWorkingChanged += MyOxygenTank_IsWorkingChanged;
		}

        void MyGasTank_ContentsChanged(MyInventoryBase obj)
        {
            if (m_autoRefill && CanRefill())
                RefillBottles();
        }

		public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
		{
			var builder = (MyObjectBuilder_OxygenTank)base.GetObjectBuilderCubeBlock(copy);

			builder.IsStockpiling = IsStockpiling;
			builder.FilledRatio = FilledRatio;
			builder.AutoRefill = m_autoRefill;
			builder.Inventory = this.GetInventory().GetObjectBuilder();

			return builder;
		}

        public void RefillBottles()
        {
            var items = this.GetInventory().GetItems();
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

				this.GetInventory().UpdateGasAmount();
	            changed = true;
            }

            if (changed)
                ChangeFilledRatio(newFilledRatio, true);
        }

        private static void OnRefillButtonPressed(MyGasTank tank)
        {
            if (tank.IsWorking)
                tank.SendRefillRequest();
        }

        private bool CanRefill()
        {
            if (!CanStore || !ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) || FilledRatio == 0)
                return false;

            var items = this.GetInventory().GetItems();
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

            if (!Sync.IsServer)
                return;

            if (FilledRatio > 0f && UseConveyorSystem && this.GetInventory().VolumeFillFactor < 0.6f)
                MyGridConveyorSystem.PullAllRequest(this, this.GetInventory(), OwnerId, this.GetInventory().Constraint);

            if (m_autoRefill && CanRefill())
                RefillBottles();

            ExecuteGasTransfer();
        }

        private void ExecuteGasTransfer()
        {
            float totalTransfer = GasInputPerUpdate - GasOutputPerUpdate;

            if (totalTransfer != 0)
            {
                Transfer(totalTransfer);
                ResourceSink.Update();
            }
            else
                NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
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

	        float neededRatioToFillInUpdateInterval = (1 - FilledRatio) * MyEngineConstants.UPDATE_STEPS_PER_SECOND * SourceComp.ProductionToCapacityMultiplierByType(BlockDefinition.StoredGasId);
            float currentOutput = SourceComp.CurrentOutputByType(BlockDefinition.StoredGasId);
            return Math.Min(neededRatioToFillInUpdateInterval * Capacity + currentOutput, m_maxFillPerSecond * Capacity);
	    }

        void m_inventory_ContentsChanged(MyInventoryBase obj)
        {
            RaisePropertiesChanged();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            UpdateEmissivity();
            UpdateText();
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
            FilledRatio = 0;

            if (MySession.Static.CreativeMode)
                SourceComp.SetRemainingCapacityByType(BlockDefinition.StoredGasId, Capacity);
            else
                SourceComp.SetRemainingCapacityByType(BlockDefinition.StoredGasId, FilledRatio * Capacity);
    
            // ResourceDistributor could be null if the grid is falling apart for whatever reason (collisions, explosions, etc)
            if (CubeGrid != null && CubeGrid.GridSystems != null && CubeGrid.GridSystems.ResourceDistributor != null)
                CubeGrid.GridSystems.ResourceDistributor.ConveyorSystem_OnPoweredChanged(); // Hotfix TODO

            UpdateText();
        }

        void MyOxygenTank_IsWorkingChanged(MyCubeBlock obj)
        {
            SourceComp.Enabled = CanStore;
            
	        SetStockpilingState(m_isStockpiling);	    

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

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
		}

		private void Sink_CurrentInputChanged(MyDefinitionId resourceTypeId, float oldInput, MyResourceSinkComponent sink)
	    {
            if (resourceTypeId != BlockDefinition.StoredGasId)
                return;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
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

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
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
                UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], m_emissiveNames[nameIndex], nameIndex < fillCount ? color : Color.Black, 1);
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

        public bool UseConveyorSystem { get; set; }

	    public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(this.GetInventory());
            base.OnRemovedByCubeBuilder();
        }

        public override void OnDestroy()
        {
            ReleaseInventory(this.GetInventory(), true);
            base.OnDestroy();
        }

        public void SetInventory(Sandbox.Game.MyInventory inventory, int index)
        {
            throw new NotImplementedException("TODO Dusan inventory sync");
        }

        protected override void OnInventoryComponentAdded(MyInventoryBase inventory)
        {
            base.OnInventoryComponentAdded(inventory);
            Debug.Assert(this.GetInventory() != null, "Added inventory to collector, but different type than MyInventory?! Check this.");
            if (this.GetInventory() != null)
            {
                if (MyPerGameSettings.InventoryMass)
                {
                    this.GetInventory().ContentsChanged += m_inventory_ContentsChanged;
                }
            }
        }

        protected override void OnInventoryComponentRemoved(MyInventoryBase inventory)
        {
            base.OnInventoryComponentRemoved(inventory);
            var removedInventory = inventory as MyInventory;
            Debug.Assert(removedInventory != null, "Removed inventory is not MyInventory type? Check this.");
            if (removedInventory != null)
            {
                if (MyPerGameSettings.InventoryMass)
                {
                    removedInventory.ContentsChanged -= m_inventory_ContentsChanged;
                }
            }
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
            ResourceSink.Update();
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
            float oldFilledRatio = FilledRatio;

			if (oldFilledRatio != newFilledRatio || MySession.Static.CreativeMode)
			{
                if (!MySession.Static.CreativeMode)
                {
                    if (updateSync)
                    {
                        this.ChangeFillRatioAmount(newFilledRatio);
                        return;
                    }

                    FilledRatio = newFilledRatio;
                }
                else
                {
                    //AB: In creative we allways have 50% filled so we can recieve and send gas
                    FilledRatio = 0.5f;
                }

                if (MySession.Static.CreativeMode && newFilledRatio > oldFilledRatio)
                    SourceComp.SetRemainingCapacityByType(BlockDefinition.StoredGasId, Capacity);
                else
                    SourceComp.SetRemainingCapacityByType(BlockDefinition.StoredGasId, FilledRatio * Capacity);

				ResourceSink.Update();
				UpdateEmissivity();
				UpdateText();
			}

        }

        public float GetOxygenLevel()
        {
            return FilledRatio;
        }

        #region Multiplayer events

        public void ChangeStockpileMode(bool newStockpileMode)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnStockipleModeCallback, newStockpileMode);
            UpdateEmissivity();
        }

        [Event, Reliable, Server, Broadcast]
        private void OnStockipleModeCallback(bool newStockpileMode)
        {
            this.IsStockpiling = newStockpileMode;
        }

        public void ChangeAutoRefill(bool newAutoRefill)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnAutoRefillCallback, newAutoRefill);
        }

        [Event, Reliable, Server, Broadcast]
        private void OnAutoRefillCallback(bool newAutoRefill)
        {
            this.m_autoRefill = newAutoRefill;
            if (m_autoRefill && CanRefill())
                RefillBottles();
        }

        public void ChangeFillRatioAmount(float newFilledRatio)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnFilledRatioCallback, newFilledRatio);
        }

        [Event, Reliable, Server, Broadcast]
        private void OnFilledRatioCallback(float newFilledRatio)
        {
            this.ChangeFilledRatio(newFilledRatio);
        }

        public void SendRefillRequest()
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnRefillCallback);
        }

        [Event, Reliable, Server]
        private void OnRefillCallback()
        {
            this.RefillBottles();
        }

        #endregion

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
                UseConveyorSystem = value;
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
            Sandbox.Game.GameSystems.Conveyors.PullInformation pullInformation = new Sandbox.Game.GameSystems.Conveyors.PullInformation();
            pullInformation.Inventory = this.GetInventory();
            pullInformation.OwnerID = OwnerId;
            pullInformation.Constraint = pullInformation.Inventory.Constraint;
            return pullInformation;
        }

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPushInformation()
        {
            return null;
        }

        #endregion
    }
}
