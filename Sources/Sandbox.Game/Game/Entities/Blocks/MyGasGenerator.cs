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
using VRage.Game.Components;
using IMyInventoryOwner = VRage.Game.ModAPI.Ingame.IMyInventoryOwner;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Network;
using VRage.Sync;
using IMyInventory = VRage.Game.ModAPI.Ingame.IMyInventory;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_OxygenGenerator))]
    public class MyGasGenerator : MyFunctionalBlock, IMyGasBlock, IMyOxygenGenerator, VRage.Game.ModAPI.Ingame.IMyInventoryOwner, IMyEventProxy
    {
        private Color? m_prevEmissiveColor = null;
        private readonly Sync<bool> m_useConveyorSystem;
        private bool m_isProducing;
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
        public MyGasGenerator()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_useConveyorSystem = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            CreateTerminalControls();
            SourceComp = new MyResourceSourceComponent(2);
            ResourceSink = new MyResourceSinkComponent();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyGasGenerator>())
                return;
            base.CreateTerminalControls();
            var useConveyorSystem = new MyTerminalControlOnOffSwitch<MyGasGenerator>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConveyorSystem.Getter = (x) => x.UseConveyorSystem;
            useConveyorSystem.Setter = (x, v) => x.UseConveyorSystem = v;
            useConveyorSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConveyorSystem);

            var refillButton = new MyTerminalControlButton<MyGasGenerator>("Refill", MySpaceTexts.BlockPropertyTitle_Refill, MySpaceTexts.BlockPropertyTitle_Refill, OnRefillButtonPressed);
            refillButton.Enabled = (x) => x.CanRefill();
            refillButton.EnableAction();
            MyTerminalControlFactory.AddControl(refillButton);

            var autoRefill = new MyTerminalControlCheckbox<MyGasGenerator>("Auto-Refill", MySpaceTexts.BlockPropertyTitle_AutoRefill, MySpaceTexts.BlockPropertyTitle_AutoRefill);
            autoRefill.Getter = (x) => x.AutoRefill;
            autoRefill.Setter = (x, v) => x.ChangeAutoRefill(v);
            autoRefill.EnableAction();
            MyTerminalControlFactory.AddControl(autoRefill);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            var sourceDataList = new List<MyResourceSourceInfo>();

            foreach (var producedInfo in BlockDefinition.ProducedGases)
                sourceDataList.Add(new MyResourceSourceInfo
                {
                    ResourceTypeId = producedInfo.Id,
                    DefinedOutput = BlockDefinition.IceConsumptionPerSecond * producedInfo.IceToGasRatio * (MySession.Static.CreativeMode ? 10f : 1f),
                    ProductionToCapacityMultiplier = 1
                });

            SourceComp.Init(BlockDefinition.ResourceSourceGroup, sourceDataList);

            base.Init(objectBuilder, cubeGrid);

            var generatorBuilder = objectBuilder as MyObjectBuilder_OxygenGenerator;

            InitializeConveyorEndpoint();
            m_useConveyorSystem.Value = generatorBuilder.UseConveyorSystem;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

            if (this.GetInventory() == null) // can be already initialized as deserialized component
            {
                MyInventory inventory = new MyInventory(BlockDefinition.InventoryMaxVolume, BlockDefinition.InventorySize, MyInventoryFlags.CanReceive);
                inventory.Constraint = BlockDefinition.InputInventoryConstraint;
                Components.Add<MyInventoryBase>(inventory);
            }
            else
            {
                this.GetInventory().Constraint = BlockDefinition.InputInventoryConstraint;
            }
            Debug.Assert(this.GetInventory().Owner == this, "Ownership was not set!");

            m_oreConstraint = new MyInventoryConstraint(this.GetInventory().Constraint.Description, this.GetInventory().Constraint.Icon, this.GetInventory().Constraint.IsWhitelist);
            foreach (var id in this.GetInventory().Constraint.ConstrainedIds)
            {
                if (id.TypeId != typeof(MyObjectBuilder_GasContainerObject))
                    m_oreConstraint.Add(id);
            }

            if (MyFakes.ENABLE_INVENTORY_FIX)
            {
                FixSingleInventory();
            }

            if (this.GetInventory() != null)
            {
                this.GetInventory().Init(generatorBuilder.Inventory);
            }
            else
            {
                Debug.Fail("Trying to init inventory, but it's null!");
            }

            AutoRefill = generatorBuilder.AutoRefill;



            SourceComp.Enabled = Enabled;
            if (Sync.IsServer)
                SourceComp.OutputChanged += Source_OutputChanged;
            float iceAmount = IceAmount();
            foreach (var gasId in SourceComp.ResourceTypes)
            {
                var tmpGasId = gasId;
                m_sourceComp.SetRemainingCapacityByType(gasId, IceToGas(ref tmpGasId, iceAmount));
            }

            m_lastSourceUpdate = MySession.Static.GameplayFrameCounter;

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

            m_useConveyorSystem.Value = generatorBuilder.UseConveyorSystem;

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            IsWorkingChanged += MyGasGenerator_IsWorkingChanged;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_OxygenGenerator)base.GetObjectBuilderCubeBlock(copy);
            builder.Inventory = this.GetInventory().GetObjectBuilder();
            builder.UseConveyorSystem = m_useConveyorSystem;
            builder.AutoRefill = AutoRefill;
            return builder;
        }

        public void RefillBottles()
        {
            var items = this.GetInventory().GetItems();

            foreach (var gasId in SourceComp.ResourceTypes)
            {
                var tmpGasId = gasId;
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
                            gasProductionAmount += IceToGas(ref tmpGasId, (float)item.Amount) * (this as IMyOxygenGenerator).ProductionCapacityMultiplier;
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
                    float bottleGasAmount = gasContainer.GasLevel * physicalItem.Capacity;

                    float transferredAmount = Math.Min(physicalItem.Capacity - bottleGasAmount, gasProductionAmount);
                    gasContainer.GasLevel = Math.Min((bottleGasAmount + transferredAmount) / physicalItem.Capacity, 1f);

                    toProduce += transferredAmount;
                    gasProductionAmount -= transferredAmount;
                }

                if (toProduce > 0f)
                {
                    ProduceGas(ref tmpGasId, toProduce);
                    this.GetInventory().UpdateGasAmount();
                }
            }
        }

        private static void OnRefillButtonPressed(MyGasGenerator generator)
        {
            if (generator.IsWorking)
                generator.SendRefillRequest();
        }

        private bool CanRefill()
        {
            if (!CanProduce || !HasIce())
                return false;

            var items = this.GetInventory().GetItems();
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

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            ResourceSink.Update();

            int updatesSinceSourceUpdate = (MySession.Static.GameplayFrameCounter - m_lastSourceUpdate);
            foreach (var gasId in SourceComp.ResourceTypes)
            {
                var tmpGasId = gasId;
                float gasOutput = GasOutputPerUpdate(ref tmpGasId) * updatesSinceSourceUpdate;
                ProduceGas(ref tmpGasId, gasOutput);
            }

            if (Sync.IsServer && IsWorking)
            {
                if (m_useConveyorSystem && this.GetInventory().VolumeFillFactor < 0.6f)
                    MyGridConveyorSystem.PullAllRequest(this, this.GetInventory(), OwnerId, HasIce() ? this.GetInventory().Constraint : m_oreConstraint);

                if (AutoRefill && CanRefill())
                    RefillBottles();
            }

            UpdateEmissivity();

            if (MyFakes.ENABLE_OXYGEN_SOUNDS)
                UpdateSounds();

            m_isProducing = m_producedSinceLastUpdate;
            m_producedSinceLastUpdate = false;
            foreach (var gasId in SourceComp.ResourceTypes)
                m_producedSinceLastUpdate = m_producedSinceLastUpdate || (SourceComp.CurrentOutputByType(gasId) > 0);

            m_lastSourceUpdate = MySession.Static.GameplayFrameCounter;
        }

        private void UpdateSounds()
        {
            if (m_soundEmitter == null)
                return;
            if (IsWorking)
            {
                if (m_producedSinceLastUpdate)
                {
                    if (m_soundEmitter.SoundId != BlockDefinition.GenerateSound.Arcade && m_soundEmitter.SoundId != BlockDefinition.GenerateSound.Realistic)
                        m_soundEmitter.PlaySound(BlockDefinition.GenerateSound, true);
                }
                else if (m_soundEmitter.SoundId != BlockDefinition.IdleSound.Arcade && m_soundEmitter.SoundId != BlockDefinition.IdleSound.Realistic)
                {
                    if ((m_soundEmitter.SoundId == BlockDefinition.GenerateSound.Arcade || m_soundEmitter.SoundId == BlockDefinition.GenerateSound.Realistic) && m_soundEmitter.Loop)
                        m_soundEmitter.StopSound(false);

                    if (m_soundEmitter.IsPlaying == false)
                        m_soundEmitter.PlaySound(BlockDefinition.IdleSound, true);
                }
            }
            else if (m_soundEmitter.IsPlaying)
            {
                m_soundEmitter.StopSound(false);
            }
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

            return powerConsumption * m_powerConsumptionMultiplier;
        }

        void Inventory_ContentsChanged(MyInventoryBase obj)
        {
            float iceAmount = IceAmount();
            foreach (var gasId in SourceComp.ResourceTypes)
            {
                var tmpGasId = gasId;
                m_sourceComp.SetRemainingCapacityByType(gasId, IceToGas(ref tmpGasId, iceAmount));
            }

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
            Debug.Assert(CubeGrid.GridSystems.ResourceDistributor != null, "ResourceDistributor can't be null!");
            if (CubeGrid.GridSystems.ResourceDistributor != null)
                CubeGrid.GridSystems.ResourceDistributor.ConveyorSystem_OnPoweredChanged(); // Hotfix TODO
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

            float updatesSinceSourceUpdate = (MySession.Static.GameplayFrameCounter - m_lastSourceUpdate);
            m_lastSourceUpdate = MySession.Static.GameplayFrameCounter;
            float secondsSinceSourceUpdate = updatesSinceSourceUpdate * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            foreach (var producedGas in BlockDefinition.ProducedGases)
            {
                var producedGasId = producedGas.Id;
                float gasToProduce;
                if (producedGas.Id == changedResourceId)
                    gasToProduce = oldOutput * secondsSinceSourceUpdate;
                else
                    gasToProduce = GasOutputPerUpdate(ref producedGasId) * updatesSinceSourceUpdate;

                ProduceGas(ref producedGasId, gasToProduce);
            }
        }

        protected override void Closing()
        {
            base.Closing();
            if (m_soundEmitter != null)
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
                if (this.GetInventory() == null)
                {
                    Debug.Fail("Inventory is null");
                    return;
                }
                if (this.GetInventory().GetItems().Count > 0)
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
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId), DetailedInfo);

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

        public bool UseConveyorSystem { get { return m_useConveyorSystem; } set { m_useConveyorSystem.Value = value; } }

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
                    this.GetInventory().ContentsChanged += Inventory_ContentsChanged;
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
                    removedInventory.ContentsChanged -= Inventory_ContentsChanged;
                }
            }
        }
        #endregion

        #region Production
        bool IMyGasBlock.IsWorking()
        {
            return CanProduce;
        }

        float IceAmount()
        {
            if (MySession.Static.CreativeMode)
                return 10000f;

            var items = this.GetInventory().GetItems();

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
            if (MySession.Static.CreativeMode)
                return true;

            var items = this.GetInventory().GetItems();

            foreach (var item in items)
            {
                if (!(item.Content is MyObjectBuilder_GasContainerObject))
                    return true;
            }

            return false;
        }

        private void ProduceGas(ref MyDefinitionId gasId, float gasAmount)
        {
            if (gasAmount <= 0)
                return;

            float iceConsumed = GasToIce(ref gasId, gasAmount);
            ConsumeFuel(ref gasId, iceConsumed);
        }

        private void ConsumeFuel(ref MyDefinitionId gasTypeId, float amount)
        {
            if (!(Sync.IsServer && CubeGrid.GridSystems.ControlSystem != null))
                return;

            if (amount <= 0f)
                return;

            m_producedSinceLastUpdate = true;

            if (MySession.Static.CreativeMode)
                return;

            //  Debug.Assert(CanProduce, "Generator asked to produce gas when it is unable to do so");

            var items = this.GetInventory().GetItems();
            if (items.Count > 0 && amount > 0f)
            {
                float iceAmount = GasToIce(ref gasTypeId, amount);
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
                        // Fixes possible "run without resources" bug
                        MyFixedPoint clampedIceAmount = MyFixedPoint.Max((MyFixedPoint)iceAmount, MyFixedPoint.SmallestPossibleValue);
                        this.GetInventory().RemoveItems(item.ItemId, clampedIceAmount);
                        return;
                    }
                    else
                    {
                        iceAmount -= (float)item.Amount;
                        this.GetInventory().RemoveItems(item.ItemId);
                    }
                }
            }
        }
        #endregion

        private float GasOutputPerSecond(ref MyDefinitionId gasId)
        {
            var currentOutput = SourceComp.CurrentOutputByType(gasId) * (this as IMyOxygenGenerator).ProductionCapacityMultiplier;
            Debug.Assert(SourceComp.ProductionEnabledByType(gasId) || currentOutput <= 0f, "Gas generator has output when production is disabled!");
            return currentOutput;
        }

        private float GasOutputPerUpdate(ref MyDefinitionId gasId)
        {
            return GasOutputPerSecond(ref gasId) * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
        }

        private float IceToGas(ref MyDefinitionId gasId, float iceAmount)
        {
            return iceAmount * IceToGasRatio(ref gasId);
        }

        private float GasToIce(ref MyDefinitionId gasId, float gasAmount)
        {
            return gasAmount / IceToGasRatio(ref gasId);
        }

        private float IceToGasRatio(ref MyDefinitionId gasId)
        {
            return SourceComp.DefinedOutputByType(gasId) / BlockDefinition.IceConsumptionPerSecond;
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

        #region Multiplayer events

        public void ChangeAutoRefill(bool newAutoRefill)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnAutoRefillCallback, newAutoRefill);
        }

        [Event, Reliable, Server, Broadcast]
        private void OnAutoRefillCallback(bool newAutoRefill)
        {
            this.AutoRefill = newAutoRefill;
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
