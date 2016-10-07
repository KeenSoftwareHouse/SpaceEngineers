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
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Ingame;
using VRage.Profiler;
using VRage.Sync;
using IMyInventory = VRage.Game.ModAPI.Ingame.IMyInventory;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Reactor))]
    public class MyReactor : MyFunctionalBlock, IMyConveyorEndpointBlock, IMyReactor, IMyInventoryOwner
    {
        private MyReactorDefinition m_reactorDefinition;
		public MyReactorDefinition ReactorDefinition { get { return m_reactorDefinition; } }

        private bool m_hasRemainingCapacity;
        private float m_maxOutput;
        private float m_currentOutput;
        //private MyParticleEffect m_damageEffect;// = new MyParticleEffect();

        readonly Sync<float> m_remainingPowerCapacity;

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
#if XB1 // XB1_SYNC_NOREFLECTION
            m_remainingPowerCapacity = SyncType.CreateAndAddProp<float>();
            m_useConveyorSystem = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            CreateTerminalControls();

			SourceComp = new MyResourceSourceComponent();
            m_remainingPowerCapacity.ValueChanged += (x) => RemainingCapacityChanged();
            m_remainingPowerCapacity.ValidateNever();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyReactor>())
                return;
            base.CreateTerminalControls();
            var useConveyorSystem = new MyTerminalControlOnOffSwitch<MyReactor>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConveyorSystem.Getter = (x) => (x).UseConveyorSystem;
            useConveyorSystem.Setter = (x, v) => (x).UseConveyorSystem = v;
            useConveyorSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConveyorSystem);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
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

            base.Init(objectBuilder, cubeGrid);
         
            var obGenerator = (MyObjectBuilder_Reactor)objectBuilder;

            if (MyFakes.ENABLE_INVENTORY_FIX)
            {
                FixSingleInventory();
            }

            if (this.GetInventory() == null)
            {
                MyInventory inventory = new MyInventory(m_reactorDefinition.InventoryMaxVolume, m_reactorDefinition.InventorySize, MyInventoryFlags.CanReceive);
                Components.Add<MyInventoryBase>(inventory);
                inventory.Init(obGenerator.Inventory);
            }
            Debug.Assert(this.GetInventory().Owner == this, "Ownership was not set!");

            this.GetInventory().Constraint = m_reactorDefinition.InventoryConstraint;

            if (Sync.IsServer)
            {
                RefreshRemainingCapacity();
            }
            
            UpdateText();

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

            m_useConveyorSystem.Value = obGenerator.UseConveyorSystem;
			UpdateMaxOutputAndEmissivity();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_Reactor)base.GetObjectBuilderCubeBlock(copy);
            ob.Inventory = this.GetInventory().GetObjectBuilder();
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
            if (m_soundEmitter!= null)
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
            int timeDelta = 100 * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS;
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

            if (Sync.IsServer && IsFunctional && m_useConveyorSystem && this.GetInventory().VolumeFillFactor < 0.6f)
            {
                float consumptionPerSecond = m_reactorDefinition.MaxPowerOutput / SourceComp.ProductionToCapacityMultiplier;
                var consumedUranium = (60 * consumptionPerSecond); // Take enough uranium for one minute of operation
                consumedUranium /= m_reactorDefinition.FuelDefinition.Mass; // Convert weight to number of items
                ProfilerShort.Begin("PullRequest");
                MyGridConveyorSystem.ItemPullRequest(this, this.GetInventory(), OwnerId, m_reactorDefinition.FuelId, (MyFixedPoint)consumedUranium);
                ProfilerShort.End();
            }
        }

        public override void OnDestroy()
        {
            ReleaseInventory(this.GetInventory(), true);
            base.OnDestroy();
        }

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(this.GetInventory());
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
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
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

            if (this.GetInventory().ContainItems(consumedFuel, m_reactorDefinition.FuelId))
            {
                this.GetInventory().RemoveItemsOfType(consumedFuel, m_reactorDefinition.FuelId);
            }
            else if (MyFakes.ENABLE_INFINITE_REACTOR_FUEL)
            {
                this.GetInventory().AddItems((MyFixedPoint)(200 / m_reactorDefinition.FuelDefinition.Mass), m_reactorDefinition.FuelItem);
            }
            else
            {
                var amountAvailable = this.GetInventory().GetItemAmount(m_reactorDefinition.FuelId);
                this.GetInventory().RemoveItemsOfType(amountAvailable, m_reactorDefinition.FuelId);
            }
        }

        private void RefreshRemainingCapacity()
        {
            if (this.GetInventory() == null)
            {
                Debug.Fail("Inventory component is missing! Can not refresh capacity.");
                return;
            }
            var fuelAmount = this.GetInventory().GetItemAmount(m_reactorDefinition.FuelId);
            if (MySession.Static.CreativeMode && fuelAmount == 0)
                m_remainingPowerCapacity.Value = m_reactorDefinition.FuelDefinition.Mass;
            else
                m_remainingPowerCapacity.Value = (float) fuelAmount;
            SourceComp.SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, m_remainingPowerCapacity);
            UpdateMaxOutputAndEmissivity();
        }

		private void Source_OnOutputChanged(MyDefinitionId resourceTypeId, float oldOutput, MyResourceSourceComponent source)
	    {
			UpdateText();
            if ((SoundEmitter != null) && (SoundEmitter.Sound != null) && (SoundEmitter.Sound.IsPlaying))
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
            return CheckIsWorking() || (MySession.Static.CreativeMode && base.CheckIsWorking()) ? m_reactorDefinition.MaxPowerOutput * m_powerOutputMultiplier : 0f;
        }

		private readonly Sync<bool> m_useConveyorSystem;
		private IMyConveyorEndpoint m_multilineConveyorEndpoint;

        #region Inventory        

        public bool UseConveyorSystem
        {
            get
            {
                return m_useConveyorSystem;
            }
            set
            {
                m_useConveyorSystem.Value = value;
            }
        }

        protected override void OnInventoryComponentAdded(MyInventoryBase inventory)
        {
            base.OnInventoryComponentAdded(inventory);
            Debug.Assert(this.GetInventory() != null, "Inventory component added to container, but different type than MyInventory?! This is not expected!");
            if (Sync.IsServer && this.GetInventory() != null)
            {                
                this.GetInventory().ContentsChanged += inventory_ContentsChanged;
            }
        }

        protected override void OnInventoryComponentRemoved(MyInventoryBase inventory)
        {
            base.OnInventoryComponentRemoved(inventory);
            var inventoryRemoved = inventory as MyInventory;
            Debug.Assert(inventoryRemoved != null, "Inventory component removed from container, but different type than MyInventory?! This is not expected!");
            if (Sync.IsServer && inventoryRemoved != null)
            {
                inventoryRemoved.ContentsChanged -= inventory_ContentsChanged;
            }
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
        bool Sandbox.ModAPI.Ingame.IMyReactor.UseConveyorSystem { get { return UseConveyorSystem; } }

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

        public float CurrentOutput
        {
            get { if (SourceComp != null) return SourceComp.CurrentOutput; return 0; }
        }

        public float MaxOutput
        {
            get
            {
                return m_reactorDefinition.MaxPowerOutput;
            }
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
                UseConveyorSystem = value;
            }
        }

        IMyInventory IMyInventoryOwner.GetInventory(int index)
        {
            return this.GetInventory(index);
        }

        #endregion

        void RemainingCapacityChanged()
        {
            var before = IsWorking;

            SourceComp.SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, m_remainingPowerCapacity);
            UpdateMaxOutputAndEmissivity();

            if (!before && IsWorking)
                OnStartWorking();
            else if (before && !IsWorking)
                OnStopWorking();
        }

        #region IMyConveyorEndpointBlock implementation

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPullInformation()
        {
            Sandbox.Game.GameSystems.Conveyors.PullInformation pullInformation = new Sandbox.Game.GameSystems.Conveyors.PullInformation();
            pullInformation.Inventory = this.GetInventory();
            pullInformation.OwnerID = OwnerId;
            pullInformation.ItemDefinition = m_reactorDefinition.FuelId;
            return pullInformation;
        }

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPushInformation()
        {
            return null;
        }

        #endregion
    }
}
