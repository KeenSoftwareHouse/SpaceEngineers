using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using VRageMath;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Common;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using VRage;
using Sandbox.Game.Localization;
using VRage.Audio;
using VRage.Utils;
using VRage.ModAPI;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Reactor))]
    class MyReactor : MyFunctionalBlock, IMyInventoryOwner, IMyConveyorEndpointBlock, IMyReactor
    {
        static MyReactor()
        {
            var useConveyorSystem = new MyTerminalControlOnOffSwitch<MyReactor>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConveyorSystem.Getter = (x) => (x as IMyInventoryOwner).UseConveyorSystem;
            useConveyorSystem.Setter = (x, v) => MySyncConveyors.SendChangeUseConveyorSystemRequest(x.EntityId, v);
            useConveyorSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConveyorSystem);
        }

        private MyReactorDefinition m_reactorDefinition;
		public MyReactorDefinition ReactorDefinition { get { return m_reactorDefinition; } }

        private MyInventory m_inventory;
        private bool m_hasRemainingCapacity;
        private float m_maxOutput;
        private float m_currentOutput;
        //private MyParticleEffect m_damageEffect;// = new MyParticleEffect();

	    private MyResourceSourceComponent m_sourceComp;
		public MyResourceSourceComponent SourceComp
	    {
			get { return m_sourceComp; }
			set { if (Components.Contains(typeof(MyResourceSourceComponent))) Components.Remove<MyResourceSourceComponent>(); Components.Add<MyResourceSourceComponent>(value); m_sourceComp = value; }
	    }

        protected override bool CheckIsWorking()
        {
            return SourceComp.Enabled && SourceComp.HasCapacityRemaining && SourceComp.ProductionEnabled && base.CheckIsWorking();
        }

        public MyReactor()
        {
            m_soundEmitter = new MyEntity3DSoundEmitter(this);
			SourceComp = new MyResourceSourceComponent();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {             
            base.Init(objectBuilder, cubeGrid);

            MyDebug.AssertDebug(BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_Reactor));
            m_reactorDefinition = BlockDefinition as MyReactorDefinition;
            MyDebug.AssertDebug(m_reactorDefinition != null);

            SourceComp.Init(
                m_reactorDefinition.ResourceSourceGroup, 
                new MyResourceSourceInfo
                {
                    ResourceTypeId = MyResourceDistributorComponent.ElectricityId,
                    DefinedOutput = m_reactorDefinition.MaxPowerOutput, 
                    ProductionToCapacityMultiplier = 60 * 60
                });
	        SourceComp.HasCapacityRemainingChanged += (id, source) => UpdateIsWorking();
	        SourceComp.OutputChanged += Source_OnOutputChanged;
            SourceComp.ProductionEnabledChanged += Source_ProductionEnabledChanged;
            SourceComp.Enabled = Enabled;

            m_inventory = new MyInventory(m_reactorDefinition.InventoryMaxVolume, m_reactorDefinition.InventorySize, MyInventoryFlags.CanReceive, this);

            var obGenerator = (MyObjectBuilder_Reactor)objectBuilder;
            m_inventory.Init(obGenerator.Inventory);
            m_inventory.ContentsChanged += inventory_ContentsChanged;
            m_inventory.Constraint = m_reactorDefinition.InventoryConstraint;
            RefreshRemainingCapacity();

            UpdateText();

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

            m_useConveyorSystem = obGenerator.UseConveyorSystem;
			UpdateMaxOutputAndEmissivity();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_Reactor)base.GetObjectBuilderCubeBlock(copy);
            ob.Inventory = m_inventory.GetObjectBuilder();
            ob.UseConveyorSystem = m_useConveyorSystem;
            return ob;
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            if (IsWorking)
                OnStartWorking();
            UpdateEmissivity();
        }

        protected override void Closing()
        {
            m_soundEmitter.StopSound(true);
            base.Closing();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            // Will not consume anything when disabled.
            int timeDelta = 100 * MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS;
            if (Enabled && !MySession.Static.CreativeMode)
            {
                ProfilerShort.Begin("ConsumeCondition");
                if ((Sync.IsServer && IsWorking && !CubeGrid.GridSystems.ControlSystem.IsControlled) ||
                    CubeGrid.GridSystems.ControlSystem.IsLocallyControlled)
                {
                    ProfilerShort.Begin("ConsumeFuel");
                    ConsumeFuel(timeDelta);
                    ProfilerShort.End();
                }
                ProfilerShort.End();
            }

            if (Sync.IsServer && IsFunctional && m_useConveyorSystem && m_inventory.VolumeFillFactor < 0.6f)
            {
                float consumptionPerSecond = m_reactorDefinition.MaxPowerOutput / SourceComp.ProductionToCapacityMultiplier;
                var consumedUranium = (60 * consumptionPerSecond); // Take enough uranium for one minute of operation
                consumedUranium /= m_reactorDefinition.FuelDefinition.Mass; // Convert weight to number of items
                ProfilerShort.Begin("PullRequest");
                MyGridConveyorSystem.ItemPullRequest(this, m_inventory, OwnerId, m_reactorDefinition.FuelId, (MyFixedPoint)consumedUranium);
                ProfilerShort.End();
            }
        }

        public override void OnDestroy()
        {
            ReleaseInventory(m_inventory, true);
            base.OnDestroy();
        }

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(m_inventory);
            base.OnRemovedByCubeBuilder();
        }

        protected override void OnEnabledChanged()
        {
            SourceComp.Enabled = Enabled;

            UpdateMaxOutputAndEmissivity();

            base.OnEnabledChanged();
        }

        private void Source_ProductionEnabledChanged(MyDefinitionId changedResourceId, MyResourceSourceComponent source)
        {
            Enabled = source.Enabled;
            UpdateIsWorking();
        }

        private void UpdateMaxOutputAndEmissivity()
        {
			SourceComp.SetMaxOutput(ComputeMaxPowerOutput());
            UpdateEmissivity();
        }

        internal void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxOutput));
            MyValueFormatter.AppendWorkInBestUnit(m_reactorDefinition.MaxPowerOutput * m_powerOutputMultiplier, DetailedInfo);
            DetailedInfo.Append("\n");
            if (IsFunctional) DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentOutput));
			MyValueFormatter.AppendWorkInBestUnit(SourceComp.CurrentOutput, DetailedInfo);
            RaisePropertiesChanged();
        }

        private void ConsumeFuel(int timeDelta)
        {
			if (!SourceComp.HasCapacityRemaining)
                return;
			if (SourceComp.CurrentOutput == 0.0f)
                return;

			float consumptionPerMillisecond = SourceComp.CurrentOutput / (SourceComp.ProductionToCapacityMultiplier * 1000);
            consumptionPerMillisecond /= m_reactorDefinition.FuelDefinition.Mass; // Convert weight to number of items

            MyFixedPoint consumedFuel = (MyFixedPoint)(timeDelta * consumptionPerMillisecond);
            if (consumedFuel == 0)
            {
                consumedFuel = MyFixedPoint.SmallestPossibleValue;
            }

            if (m_inventory.ContainItems(consumedFuel, m_reactorDefinition.FuelId))
            {
                m_inventory.RemoveItemsOfType(consumedFuel, m_reactorDefinition.FuelId);
            }
            else if (MyFakes.ENABLE_INFINITE_REACTOR_FUEL)
            {
                m_inventory.AddItems((MyFixedPoint)(200 / m_reactorDefinition.FuelDefinition.Mass), m_reactorDefinition.FuelItem);
            }
            else
            {
                var amountAvailable = m_inventory.GetItemAmount(m_reactorDefinition.FuelId);
                m_inventory.RemoveItemsOfType(amountAvailable, m_reactorDefinition.FuelId);
            }
        }

        private void RefreshRemainingCapacity()
        {
            var fuelAmount = m_inventory.GetItemAmount(m_reactorDefinition.FuelId);
            float remainingPowerCapacity;
            if (MySession.Static.CreativeMode && fuelAmount == 0)
                remainingPowerCapacity = m_reactorDefinition.FuelDefinition.Mass;
            else
                remainingPowerCapacity = (float) fuelAmount;
            SourceComp.SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, remainingPowerCapacity);
            UpdateMaxOutputAndEmissivity();
        }

		private void Source_OnOutputChanged(MyDefinitionId resourceTypeId, float oldOutput, MyResourceSourceComponent source)
	    {
			UpdateText();
			if ((SoundEmitter.Sound != null) && (SoundEmitter.Sound.IsPlaying))
			{
				if (SourceComp.MaxOutput != 0f)
				{
					float semitones = 4f * (SourceComp.CurrentOutput - 0.5f * SourceComp.MaxOutput) / SourceComp.MaxOutput;
					SoundEmitter.Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(semitones);
				}
				else
					SoundEmitter.Sound.FrequencyRatio = 1f;
			}
	    }

        private void UpdateEmissivity()
        {
            if (!InScene)
                return;

            if (IsWorking)
                UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Green, Color.White);
            else
                UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Red, Color.White);
        }

        private float ComputeMaxPowerOutput()
        {
            return CheckIsWorking() || (Enabled && MySession.Static.CreativeMode) ? m_reactorDefinition.MaxPowerOutput * m_powerOutputMultiplier : 0f;
        }

		private bool m_useConveyorSystem;
		private IMyConveyorEndpoint m_multilineConveyorEndpoint;

        #region IMyInventoryOwner

        public int InventoryCount { get { return 1; } }

        String IMyInventoryOwner.DisplayNameText
        {
            get { return CustomName.ToString(); }
        }

        public MyInventory GetInventory(int index = 0)
        {
            Debug.Assert(index == 0);
            return m_inventory;
        }

        public void SetInventory(MyInventory inventory, int index)
        {
            if (m_inventory != null)
            {
                m_inventory.ContentsChanged -= inventory_ContentsChanged;
            }

            m_inventory = inventory;

            if (m_inventory != null)
            {
                m_inventory.ContentsChanged += inventory_ContentsChanged;
            }
        }

        public MyInventoryOwnerTypeEnum InventoryOwnerType
        {
            get { return MyInventoryOwnerTypeEnum.Energy; }
        }

        bool IMyInventoryOwner.UseConveyorSystem
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

        bool ModAPI.Interfaces.IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return (this as IMyInventoryOwner).UseConveyorSystem;
            }
            set
            {
                (this as IMyInventoryOwner).UseConveyorSystem = value;
            }
        }

        Sandbox.ModAPI.Interfaces.IMyInventory Sandbox.ModAPI.Interfaces.IMyInventoryOwner.GetInventory(int index)
        {
            return GetInventory(index);
        }

        #endregion

        /*void damageEffect_OnDelete(object sender, EventArgs e)
        {
            m_damageEffect = null;
        }*/

        void inventory_ContentsChanged(MyInventoryBase obj)
        {
            var before = IsWorking;
            RefreshRemainingCapacity();
            if (!before && IsWorking)
                OnStartWorking();
            else if (before && !IsWorking)
                OnStopWorking();
        }

        void ComponentStack_IsFunctionalChanged()
        {
            UpdateMaxOutputAndEmissivity();
            if (IsWorking)
                OnStartWorking();
            else
                OnStopWorking();
        }

        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_multilineConveyorEndpoint; }
        }

        public void InitializeConveyorEndpoint()
        {
            m_multilineConveyorEndpoint = new MyMultilineConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_multilineConveyorEndpoint));
        }
        bool Sandbox.ModAPI.Ingame.IMyReactor.UseConveyorSystem { get { return (this as IMyInventoryOwner).UseConveyorSystem; } }

        private float m_powerOutputMultiplier = 1f;
        float Sandbox.ModAPI.IMyReactor.PowerOutputMultiplier
        {
            get
            {
                return m_powerOutputMultiplier;
            }
            set
            {
                m_powerOutputMultiplier = value;
                if (m_powerOutputMultiplier < 0.01f)
                {
                    m_powerOutputMultiplier = 0.01f;
                }

                SourceComp.SetMaxOutput(ComputeMaxPowerOutput());

                UpdateText();
            }
        }
    }
}
