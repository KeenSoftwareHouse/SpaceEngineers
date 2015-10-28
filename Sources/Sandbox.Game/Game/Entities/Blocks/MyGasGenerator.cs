using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Diagnostics;
using VRage;
using VRageMath;
using System.Text;
using VRage.Utils;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using Sandbox.Common;
using Sandbox.Engine.Multiplayer;
using SteamSDK;
using Sandbox.Engine.Utils;
using Sandbox.Game.EntityComponents;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ModAPI;
using VRage.Components;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_OxygenGenerator))]
    class MyGasGenerator : MyFunctionalBlock, IMyInventoryOwner, IMyGasBlock, IMyOxygenGenerator
    {
        private Color? m_prevEmissiveColor = null;
        private bool m_useConveyorSystem;
	    private MyInventory m_inventory;
        private bool m_isProducing;
        private int m_updateCounter;
        private int m_lastSourceUpdate;
        private bool m_producedSinceLastUpdate;
        private MyInventoryConstraint m_oreConstraint;
        private MyMultilineConveyorEndpoint m_conveyorEndpoint;

        public IMyConveyorEndpoint ConveyorEndpoint { get { return m_conveyorEndpoint; } }

        readonly MyDefinitionId m_oxygenGasId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Oxygen");	// Required for oxygen MyFake checks

        public bool CanProduce 
        { 
            get 
            {
                return (MySession.Static.Settings.EnableOxygen || !BlockDefinition.ProducedGases.TrueForAll((info) => info.Id == m_oxygenGasId))
                        && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId)
                        && IsWorking 
                        && Enabled 
                        && IsFunctional; 
            } 
        }

        public bool AutoRefill { get; private set; }

		private MyResourceSourceComponent m_sourceComp;
		public MyResourceSourceComponent SourceComp
		{
			get { return m_sourceComp; }
			set { if (Components.Contains(typeof(MyResourceSourceComponent))) Components.Remove<MyResourceSourceComponent>(); Components.Add<MyResourceSourceComponent>(value); m_sourceComp = value; }
		}

        private new MyOxygenGeneratorDefinition BlockDefinition { get { return (MyOxygenGeneratorDefinition)base.BlockDefinition; } }

        #region Initialization
        static MyGasGenerator()
        {
            var useConveyorSystem = new MyTerminalControlOnOffSwitch<MyGasGenerator>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConveyorSystem.Getter = (x) => (x as IMyInventoryOwner).UseConveyorSystem;
            useConveyorSystem.Setter = (x, v) => MySyncConveyors.SendChangeUseConveyorSystemRequest(x.EntityId, v);
            useConveyorSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConveyorSystem);

            var refillButton = new MyTerminalControlButton<MyGasGenerator>("Refill", MySpaceTexts.BlockPropertyTitle_Refill, MySpaceTexts.BlockPropertyTitle_Refill, OnRefillButtonPressed);
            refillButton.Enabled = (x) => x.CanRefill();
            refillButton.EnableAction();
            MyTerminalControlFactory.AddControl(refillButton);

            var autoRefill = new MyTerminalControlCheckbox<MyGasGenerator>("Auto-Refill", MySpaceTexts.BlockPropertyTitle_AutoRefill, MySpaceTexts.BlockPropertyTitle_AutoRefill);
            autoRefill.Getter = (x) => x.AutoRefill;
            autoRefill.Setter = (x, v) => x.SyncObject.ChangeAutoRefill(v);
            autoRefill.EnableAction();
            MyTerminalControlFactory.AddControl(autoRefill);
        }

	    public MyGasGenerator()
	    {
			SourceComp = new MyResourceSourceComponent(2);
			ResourceSink = new MyResourceSinkComponent();
	    }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            base.Init(objectBuilder, cubeGrid);

            var generatorBuilder = objectBuilder as MyObjectBuilder_OxygenGenerator;

            InitializeConveyorEndpoint();
            m_useConveyorSystem = generatorBuilder.UseConveyorSystem;

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

	        m_inventory = new MyInventory(
		        BlockDefinition.InventoryMaxVolume,
			        BlockDefinition.InventorySize,
			        MyInventoryFlags.CanReceive,
			        this)
	        {
		        Constraint = BlockDefinition.InputInventoryConstraint
	        };
	        m_oreConstraint = new MyInventoryConstraint(m_inventory.Constraint.Description, m_inventory.Constraint.Icon, m_inventory.Constraint.IsWhitelist);
            foreach (var id in m_inventory.Constraint.ConstrainedIds)
            {
                if (id.TypeId != typeof(MyObjectBuilder_GasContainerObject))
                    m_oreConstraint.Add(id);
            }

            m_inventory.Init(generatorBuilder.Inventory);

            m_inventory.ContentsChanged += Inventory_ContentsChanged;

            AutoRefill = generatorBuilder.AutoRefill;

	        var sourceDataList = new List<MyResourceSourceInfo>();

	        foreach (var producedInfo in BlockDefinition.ProducedGases)
				sourceDataList.Add(new MyResourceSourceInfo
				{
				    ResourceTypeId = producedInfo.Id,
                    DefinedOutput = BlockDefinition.IceConsumptionPerSecond * producedInfo.IceToGasRatio,
                    ProductionToCapacityMultiplier = 1
				});

			SourceComp.Init(BlockDefinition.ResourceSourceGroup, sourceDataList);
            if (Sync.IsServer)
                SourceComp.OutputChanged += Source_OutputChanged;
            float iceAmount = IceAmount();
            foreach (var gasId in SourceComp.ResourceTypes)
                m_sourceComp.SetRemainingCapacityByType(gasId, IceToGas(gasId, iceAmount));

            m_updateCounter = 0;
            m_lastSourceUpdate = m_updateCounter;

	        ResourceSink.Init(BlockDefinition.ResourceSinkGroup, new MyResourceSinkInfo
				{
				    ResourceTypeId = MyResourceDistributorComponent.ElectricityId,
                    MaxRequiredInput = BlockDefinition.OperationalPowerConsumption,
                    RequiredInputFunc = ComputeRequiredPower
				});
			ResourceSink.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
			ResourceSink.Update();

            UpdateEmissivity();
            UpdateText();

            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            IsWorkingChanged += MyGasGenerator_IsWorkingChanged;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_OxygenGenerator)base.GetObjectBuilderCubeBlock(copy);
            builder.Inventory = m_inventory.GetObjectBuilder();
            builder.AutoRefill = AutoRefill;
            return builder;
        }

        public void RefillBottles()
        {
            var items = m_inventory.GetItems();

			foreach (var gasId in SourceComp.ResourceTypes)
			{
				float gasProductionAmount = 0f;

            if (MySession.Static.CreativeMode)
            {
					gasProductionAmount = float.MaxValue;
            }
            else
            {
                foreach (var item in items)
                {
                    if (!(item.Content is MyObjectBuilder_GasContainerObject))
                            gasProductionAmount += IceToGas(gasId, (float)item.Amount) * (this as IMyOxygenGenerator).ProductionCapacityMultiplier;
                }
            }

            float toProduce = 0f;

            foreach (var item in items)
            {
					if (gasProductionAmount <= 0f)
                    return;

				var gasContainer = item.Content as MyObjectBuilder_GasContainerObject;
	            if (gasContainer == null || gasContainer.GasLevel >= 1f)
					continue;

	            var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(gasContainer) as MyOxygenContainerDefinition;
					if (physicalItem.StoredGasId != gasId)
						continue;

	            Debug.Assert(physicalItem != null);
					float bottleGasAmount = gasContainer.GasLevel*physicalItem.Capacity;

					float transferredAmount = Math.Min(physicalItem.Capacity - bottleGasAmount, gasProductionAmount);
					gasContainer.GasLevel = Math.Min((bottleGasAmount + transferredAmount)/physicalItem.Capacity, 1f);

                // TODO: Dusan sync
	            //if (transferredAmount > 0f)
		            //m_inventory.SyncGasContainerLevel(item.ItemId, gasContainer.GasLevel);

	            toProduce += transferredAmount;
					gasProductionAmount -= transferredAmount;
            }
            
            if (toProduce > 0f)
            {
                    ProduceGas(gasId, toProduce);
                m_inventory.UpdateGasAmount();
            }
        }
		}

        private static void OnRefillButtonPressed(MyGasGenerator generator)
        {
            if (generator.IsWorking)
                generator.SyncObject.SendRefillRequest();
        }

        private bool CanRefill()
        {
            if (!CanProduce || !HasIce())
                return false;

            var items = m_inventory.GetItems();
            foreach (var item in items)
            {
                var oxygenContainer = item.Content as MyObjectBuilder_GasContainerObject;
	            if (oxygenContainer == null)
					continue;

	            if (oxygenContainer.GasLevel < 1f)
		            return true;
            }

            return false;
        }

        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyMultilineConveyorEndpoint(this);
        }
        #endregion

        #region Update, power and functionality

        public override void UpdateAfterSimulation()
	        {
            base.UpdateAfterSimulation();

            ++m_updateCounter;
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            ResourceSink.Update();

            int updatesSinceSourceUpdate = (m_updateCounter - m_lastSourceUpdate);
	        foreach (var gasId in SourceComp.ResourceTypes)
        {
                float gasOutput = GasOutputPerUpdate(gasId) * updatesSinceSourceUpdate;
	            ProduceGas(gasId, gasOutput);
        }

            if (Sync.IsServer && IsWorking)
            {
                if (m_useConveyorSystem && m_inventory.VolumeFillFactor < 0.6f)
	                MyGridConveyorSystem.PullAllRequest(this, m_inventory, OwnerId, HasIce() ? m_inventory.Constraint : m_oreConstraint);
             
                if (AutoRefill && CanRefill())
                    RefillBottles();
                }

            UpdateEmissivity();

            if (MyFakes.ENABLE_OXYGEN_SOUNDS)
                UpdateSounds();

            m_isProducing = m_producedSinceLastUpdate;
            m_producedSinceLastUpdate = false;
			foreach(var gasId in SourceComp.ResourceTypes)
				m_producedSinceLastUpdate = m_producedSinceLastUpdate || (SourceComp.CurrentOutputByType(gasId) > 0);

            m_updateCounter = 0;
            m_lastSourceUpdate = m_updateCounter;
        }

        private void UpdateSounds()
        {
            if (IsWorking)
            {
                if (m_producedSinceLastUpdate)
                {
                    if (m_soundEmitter.SoundId != BlockDefinition.GenerateSound.SoundId)
                        m_soundEmitter.PlaySound(BlockDefinition.GenerateSound, true);
                }
                else if (m_soundEmitter.SoundId != BlockDefinition.IdleSound.SoundId)
                {
                    m_soundEmitter.PlaySound(BlockDefinition.IdleSound, true);
                }
            }
            else if (m_soundEmitter.IsPlaying)
            {
                m_soundEmitter.StopSound(false);
            }

            m_soundEmitter.Update();
        }

        protected override bool CheckIsWorking()
        {
            return base.CheckIsWorking() && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId);
        }

        private float ComputeRequiredPower()
        {
            if ((!MySession.Static.Settings.EnableOxygen && BlockDefinition.ProducedGases.TrueForAll((info) => info.Id == m_oxygenGasId))
                        || !Enabled 
                        || !IsFunctional)
                return 0f;

            bool isProducing = false;
            foreach (var producedGas in BlockDefinition.ProducedGases)
                isProducing = isProducing || (SourceComp.CurrentOutputByType(producedGas.Id) > 0 && (MySession.Static.Settings.EnableOxygen || producedGas.Id != m_oxygenGasId));

            float powerConsumption = isProducing ? BlockDefinition.OperationalPowerConsumption : BlockDefinition.StandbyPowerConsumption;

            return powerConsumption*m_powerConsumptionMultiplier;
        }

        void Inventory_ContentsChanged(MyInventoryBase obj)
        {
            float iceAmount = IceAmount();
            foreach(var gasId in SourceComp.ResourceTypes)
                m_sourceComp.SetRemainingCapacityByType(gasId, IceToGas(gasId, iceAmount));

            RaisePropertiesChanged();
        }

        void MyGasGenerator_IsWorkingChanged(MyCubeBlock obj)
        {
            SourceComp.Enabled = CanProduce;
            UpdateEmissivity();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            UpdateEmissivity();
        }

        void PowerReceiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            UpdateEmissivity();
        }

        void ComponentStack_IsFunctionalChanged()
        {
            SourceComp.Enabled = CanProduce;
			ResourceSink.Update();
            UpdateEmissivity();
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();
            SourceComp.Enabled = CanProduce;
			ResourceSink.Update();
            UpdateEmissivity();
        }

        private void Source_OutputChanged(MyDefinitionId changedResourceId, float oldOutput, MyResourceSourceComponent source)
        {
            if (BlockDefinition.ProducedGases.TrueForAll((info) => info.Id != changedResourceId))
                return;

            float updatesSinceSourceUpdate = (m_updateCounter - m_lastSourceUpdate);
            float secondsSinceSourceUpdate = updatesSinceSourceUpdate*MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            foreach (var producedGas in BlockDefinition.ProducedGases)
        {
                float gasToProduce;
                if (producedGas.Id == changedResourceId)
                    gasToProduce = oldOutput*secondsSinceSourceUpdate;
                else
                    gasToProduce = GasOutputPerUpdate(producedGas.Id)*updatesSinceSourceUpdate;

                ProduceGas(producedGas.Id, gasToProduce);
        }
            m_lastSourceUpdate = m_updateCounter;
        }

        protected override void Closing()
        {
            base.Closing();
            m_soundEmitter.StopSound(true);
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            
            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (CanProduce)
            {
                if (m_inventory.GetItems().Count > 0)
                {
	                SetEmissive(m_isProducing ? Color.Teal : Color.Green);
                }
                else
                {
                    SetEmissive(Color.Yellow);
                }
            }
            else
            {
                SetEmissive(Color.Red);
            }
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
			MyValueFormatter.AppendWorkInBestUnit(ResourceSink.MaxRequiredInput, DetailedInfo);

            if (!MySession.Static.Settings.EnableOxygen)
            {
                DetailedInfo.Append("\n");
                DetailedInfo.Append("Oxygen disabled in world settings!");
            }
        }

        private void SetEmissive(Color color)
        {
	        if (m_prevEmissiveColor == color)
				return;

	        UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, color, Color.White);
	        m_prevEmissiveColor = color;
        }

        public override void OnModelChange()
        {
            base.OnModelChange();

            m_prevEmissiveColor = null;
        }
        #endregion

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

        public bool UseConveyorSystem { get { return m_useConveyorSystem; } set { m_useConveyorSystem = value; } }

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

        #region Production
        bool IMyGasBlock.IsWorking()
        {
            return CanProduce;
        }

	    float IceAmount()
        {
            var items = m_inventory.GetItems();

		    MyFixedPoint amount = 0;
            foreach (var item in items)
            {
                if (!(item.Content is MyObjectBuilder_GasContainerObject))
					amount += item.Amount;
            }

			return (float)amount;
        }

        bool HasIce()
        {
            var items = m_inventory.GetItems();

            foreach (var item in items)
            {
                if (!(item.Content is MyObjectBuilder_GasContainerObject))
                    return true;
                }
	            
            return false;
            }

        private void ProduceGas(MyDefinitionId gasId, float gasAmount)
        {
            if (gasAmount <= 0)
                return;

            float iceConsumed = GasToIce(gasId, gasAmount);
            ConsumeFuel(gasId, iceConsumed);
        }

        private void ConsumeFuel(MyDefinitionId gasTypeId, float amount)
        {
            if (!((Sync.IsServer && !CubeGrid.GridSystems.ControlSystem.IsControlled) || CubeGrid.GridSystems.ControlSystem.IsLocallyControlled))
                return;

            if (amount <= 0f)
                return;

            m_producedSinceLastUpdate = true;

            if (MySession.Static.CreativeMode)
                return;

          //  Debug.Assert(CanProduce, "Generator asked to produce gas when it is unable to do so");

            var items = m_inventory.GetItems();
            if (items.Count > 0 && amount > 0f)
            {
				float iceAmount = GasToIce(gasTypeId, amount);
                int index = 0;
                while (index < items.Count)
                {
                    var item = items[index];

					if (item.Content is MyObjectBuilder_GasContainerObject)
                    {
                        index++;
                        continue;
                    }

                    if (iceAmount < (float)item.Amount)
                    {
                        m_inventory.RemoveItems(item.ItemId, (MyFixedPoint)iceAmount);
                        return;
                    }
                    else
                    {
                        iceAmount -= (float)item.Amount;
                        m_inventory.RemoveItems(item.ItemId);
                    }
                }
            }
        }
        #endregion

        private float GasOutputPerSecond(MyDefinitionId gasId)
        {
            var currentOutput = SourceComp.CurrentOutputByType(gasId) * (this as IMyOxygenGenerator).ProductionCapacityMultiplier;
            Debug.Assert(SourceComp.ProductionEnabledByType(gasId) || currentOutput <= 0f, "Gas generator has output when production is disabled!");
            return currentOutput;
        }

        private float GasOutputPerUpdate(MyDefinitionId gasId)
        {
            return GasOutputPerSecond(gasId) * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
        }

        private float IceToGas(MyDefinitionId gasId, float iceAmount)
        {
            return iceAmount*IceToGasRatio(gasId);
        }

        private float GasToIce(MyDefinitionId gasId, float gasAmount)
        {
            return gasAmount/IceToGasRatio(gasId);
        }

	    private float IceToGasRatio(MyDefinitionId gasId)
	    {
		    return SourceComp.DefinedOutputByType(gasId)/BlockDefinition.IceConsumptionPerSecond;
	    }

        private float m_productionCapacityMultiplier = 1f;
        float Sandbox.ModAPI.IMyOxygenGenerator.ProductionCapacityMultiplier
        {
            get
            {
                return m_productionCapacityMultiplier;
            }
            set
            {
                m_productionCapacityMultiplier = value;
                if (m_productionCapacityMultiplier < 0.01f)
                {
                    m_productionCapacityMultiplier = 0.01f;
                }
            }
        }

        private float m_powerConsumptionMultiplier = 1f;
        float Sandbox.ModAPI.IMyOxygenGenerator.PowerConsumptionMultiplier
        {
            get
            {
                return m_powerConsumptionMultiplier;
            }
            set
            {
                m_powerConsumptionMultiplier = value;
                if (m_powerConsumptionMultiplier < 0.01f)
                {
                    m_powerConsumptionMultiplier = 0.01f;
                }

				if (ResourceSink != null)
                {
					ResourceSink.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, BlockDefinition.OperationalPowerConsumption * m_powerConsumptionMultiplier);
					ResourceSink.Update();
                }
            }
        }

        #region Sync
        protected override MySyncEntity OnCreateSync()
        {
            return new MySyncOxygenGenerator(this);
        }

        internal new MySyncOxygenGenerator SyncObject { get { return (MySyncOxygenGenerator)base.SyncObject; } }


        internal class MySyncOxygenGenerator : MySyncEntity
        {
            [MessageIdAttribute(8100, P2PMessageEnum.Reliable)]
            protected struct ChangeAutoRefillMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public BoolBlit AutoRefill;
            }

            [MessageIdAttribute(8101, P2PMessageEnum.Reliable)]
            protected struct RefillRequestMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }
            }

            private MyGasGenerator m_generator;

            static MySyncOxygenGenerator()
            {
                //MySyncLayer.RegisterEntityMessage<MySyncOxygenGenerator, ChangeAutoRefillMsg>(OnAutoRefillChanged, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncOxygenGenerator, RefillRequestMsg>(OnRefillRequest, MyMessagePermissions.ToServer);
            }

            public MySyncOxygenGenerator(MyGasGenerator generator)
                : base(generator)
            {
                m_generator = generator;
            }

            public void ChangeAutoRefill(bool newAutoRefill)
            {
                var msg = new ChangeAutoRefillMsg();
                msg.EntityId = m_generator.EntityId;
                msg.AutoRefill = newAutoRefill;

                Sync.Layer.SendMessageToServerAndSelf(ref msg);
            }

            public void SendRefillRequest()
            {
                if (Sync.IsServer)
                {
                    m_generator.RefillBottles();
                }
                else
                {
                    var msg = new RefillRequestMsg();
                    msg.EntityId = m_generator.EntityId;

                    Sync.Layer.SendMessageToServer(ref msg);
                }
            }

            private static void OnAutoRefillChanged(MySyncOxygenGenerator syncObject, ref ChangeAutoRefillMsg message, MyNetworkClient sender)
            {
                syncObject.m_generator.AutoRefill = message.AutoRefill;
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref message, sender.SteamUserId);
                }
            }

            private static void OnRefillRequest(MySyncOxygenGenerator syncObject, ref RefillRequestMsg message, MyNetworkClient sender)
            {
                syncObject.m_generator.RefillBottles();
            }
        }
        #endregion
    }
}
