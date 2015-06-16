using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems.Electricity;
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
using Sandbox.Engine.Multiplayer;
using SteamSDK;
using Sandbox.Engine.Utils;
using VRage.ModAPI;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_OxygenGenerator))]
    class MyOxygenGenerator : MyFunctionalBlock, IMyPowerConsumer, IMyInventoryOwner, IMyOxygenProducer, IMyOxygenGenerator, IMyConveyorEndpointBlock
    {
        private Color? m_prevEmissiveColor = null;
        private bool m_useConveyorSystem;
        private bool m_autoRefill;
        private MyInventory m_inventory;
        private bool m_isProducing;
        private bool m_producedSinceLastUpdate;
        private MyInventoryConstraint m_oreConstraint;
        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get
            {
                return m_conveyorEndpoint;
            }
        }

        public bool CanProduce
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

        private new MyOxygenGeneratorDefinition BlockDefinition
        {
            get { return (MyOxygenGeneratorDefinition)base.BlockDefinition; }
        }

        #region Initialization
        static MyOxygenGenerator()
        {
            var useConveyorSystem = new MyTerminalControlOnOffSwitch<MyOxygenGenerator>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConveyorSystem.Getter = (x) => (x as IMyInventoryOwner).UseConveyorSystem;
            useConveyorSystem.Setter = (x, v) => MySyncConveyors.SendChangeUseConveyorSystemRequest(x.EntityId, v);
            useConveyorSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConveyorSystem);

            var refillButton = new MyTerminalControlButton<MyOxygenGenerator>("Refill", MySpaceTexts.BlockPropertyTitle_Refill, MySpaceTexts.BlockPropertyTitle_Refill, OnRefillButtonPressed);
            refillButton.Enabled = (x) => x.CanRefill();
            refillButton.EnableAction();
            MyTerminalControlFactory.AddControl(refillButton);

            var autoRefill = new MyTerminalControlCheckbox<MyOxygenGenerator>("Auto-Refill", MySpaceTexts.BlockPropertyTitle_AutoRefill, MySpaceTexts.BlockPropertyTitle_AutoRefill);
            autoRefill.Getter = (x) => x.m_autoRefill;
            autoRefill.Setter = (x, v) => x.m_autoRefill = v;
            autoRefill.EnableAction();
            MyTerminalControlFactory.AddControl(autoRefill);
        }

        public void RefillBottles()
        {
            var items = m_inventory.GetItems();

            float productionAmount = 0f;

            if (MySession.Static.CreativeMode)
            {
                productionAmount = float.MaxValue;
            }
            else
            {
                foreach (var item in items)
                {
                    if (!(item.Content is MyObjectBuilder_OxygenContainerObject))
                    {
                        productionAmount += (float)item.Amount * BlockDefinition.IceToOxygenRatio;
                    }
                }
            }

            float toProduce = 0f;

            foreach (var item in items)
            {
                if (productionAmount <= 0f)
                {
                    return;
                }
                var oxygenContainer = item.Content as MyObjectBuilder_OxygenContainerObject;
                if (oxygenContainer != null)
                {
                    if (oxygenContainer.OxygenLevel < 1f)
                    {
                        var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(oxygenContainer) as MyOxygenContainerDefinition;
                        float bottleOxygenAmount = oxygenContainer.OxygenLevel * physicalItem.Capacity;

                        float transferredAmount = Math.Min(physicalItem.Capacity - bottleOxygenAmount, productionAmount);
                        oxygenContainer.OxygenLevel = (bottleOxygenAmount + transferredAmount) / physicalItem.Capacity;

                        if (oxygenContainer.OxygenLevel > 1f)
                        {
                            oxygenContainer.OxygenLevel = 1f;
                        }

                        if (transferredAmount > 0f)
                        {
                            m_inventory.SyncOxygenContainerLevel(item.ItemId, oxygenContainer.OxygenLevel);
                        }

                        toProduce += transferredAmount;
                        productionAmount -= transferredAmount;
                    }
                }
            }
            
            if (toProduce > 0f)
            {
                (this as IMyOxygenProducer).Produce(toProduce);
                m_inventory.UpdateOxygenAmount();
            }
        }

        private static void OnRefillButtonPressed(MyOxygenGenerator generator)
        {
            if (generator.IsWorking)
            {
                generator.SyncObject.SendRefillRequest();
            }
        }

        private bool CanRefill()
        {
            if (!CanProduce || !HasIce())
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

            var generatorBuilder = objectBuilder as MyObjectBuilder_OxygenGenerator;

            InitializeConveyorEndpoint();
            m_useConveyorSystem = generatorBuilder.UseConveyorSystem;

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;

            m_inventory = new MyInventory(
                BlockDefinition.InventoryMaxVolume,
                BlockDefinition.InventorySize,
                MyInventoryFlags.CanReceive,
                this);
            m_inventory.Constraint = BlockDefinition.InputInventoryConstraint;
            m_oreConstraint = new MyInventoryConstraint(m_inventory.Constraint.Description, m_inventory.Constraint.Icon, m_inventory.Constraint.IsWhitelist);
            foreach (var id in m_inventory.Constraint.ConstrainedIds)
            {
                if (id.TypeId != typeof(MyObjectBuilder_OxygenContainerObject))
                {
                    m_oreConstraint.Add(id);
                }
            }

            m_inventory.Init(generatorBuilder.Inventory);

            m_inventory.ContentsChanged += m_inventory_ContentsChanged;

            m_autoRefill = generatorBuilder.AutoRefill;

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Factory,
                false,
                BlockDefinition.OperationalPowerConsumption,
                ComputeRequiredPower);
            PowerReceiver.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
            PowerReceiver.Update();

            UpdateEmissivity();
            UpdateText();

            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_OxygenGenerator)base.GetObjectBuilderCubeBlock(copy);
            builder.Inventory = m_inventory.GetObjectBuilder();
            builder.AutoRefill = m_autoRefill;
            return builder;
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

            if (Sync.IsServer && IsWorking)
            {
                if (m_useConveyorSystem && m_inventory.VolumeFillFactor < 0.6f)
                {
                    if (HasIce())
                    {
                        MyGridConveyorSystem.PullAllRequest(this, m_inventory, OwnerId, m_inventory.Constraint);
                    }
                    else
                    {
                        MyGridConveyorSystem.PullAllRequest(this, m_inventory, OwnerId, m_oreConstraint);
                    }
                }
             
                if (m_autoRefill && CanRefill())
                {
                    RefillBottles();
                }
            }

            UpdateEmissivity();

            if (MyFakes.ENABLE_OXYGEN_SOUNDS)
            {
                UpdateSounds();
            }

            m_isProducing = m_producedSinceLastUpdate;
            m_producedSinceLastUpdate = false;
        }

        private void UpdateSounds()
        {
            if (IsWorking)
            {
                if (m_producedSinceLastUpdate)
                {
                    if (m_soundEmitter.SoundId != BlockDefinition.GenerateSound.SoundId)
                    {
                        m_soundEmitter.PlaySound(BlockDefinition.GenerateSound, true);
                    }
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
            return base.CheckIsWorking() && PowerReceiver.IsPowered;
        }

        private float ComputeRequiredPower()
        {
            if (!MySession.Static.Settings.EnableOxygen)
            {
                return 0;
            }

            return (Enabled && IsFunctional) ? (m_isProducing) ? BlockDefinition.OperationalPowerConsumption * m_powerConsumptionMultiplier
                                                             : BlockDefinition.StandbyPowerConsumption * m_powerConsumptionMultiplier
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
                    if (m_isProducing)
                    {
                        SetEmissive(Color.Teal);
                    }
                    else
                    {
                        SetEmissive(Color.Green);
                    }
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
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.MaxRequiredInput, DetailedInfo);

            if (!MySession.Static.Settings.EnableOxygen)
            {
                DetailedInfo.Append("\n");
                DetailedInfo.Append("Oxygen disabled in world settings!");
            }
        }

        private void SetEmissive(Color color)
        {
            if (m_prevEmissiveColor != color)
            {
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, color, Color.White);
                m_prevEmissiveColor = color;
            }
        }

        public override void OnModelChange()
        {
            base.OnModelChange();

            m_prevEmissiveColor = null;
        }
        #endregion

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

        #region Production
        bool IMyOxygenBlock.IsWorking()
        {
            return CanProduce;
        }

        int IMyOxygenProducer.GetPriority()
        {
            return 2;
        }

        bool HasIce()
        {
            var items = m_inventory.GetItems();

            foreach (var item in items)
            {
                if (!(item.Content is MyObjectBuilder_OxygenContainerObject))
                {
                    return true;
                }
            }

            return false;
        }

        float IMyOxygenProducer.ProductionCapacity(float deltaTime)
        {
            if (!CanProduce)
            {
                return 0f;
            }

            float productionCapacity = BlockDefinition.OxygenProductionPerSecond * m_productionCapacityMultiplier * deltaTime;

            if (MySession.Static.CreativeMode)
            {
                return productionCapacity;
            }

            var items = m_inventory.GetItems();
            if (items.Count <= 0)
            {
                return 0f;
            }


            float amountForMaxCapacity = (productionCapacity / BlockDefinition.IceToOxygenRatio);
            foreach (var item in items)
            {
                if (item.Content is MyObjectBuilder_OxygenContainerObject)
                {
                    continue;
                }

                if ((float)item.Amount > amountForMaxCapacity)
                {
                    return productionCapacity;
                }
                else
                {
                    float ratio = (float)item.Amount / amountForMaxCapacity;
                    return productionCapacity * ratio;
                }
            }

            return 0f;
        }

        void IMyOxygenProducer.Produce(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            m_producedSinceLastUpdate = true;

            if (MySession.Static.CreativeMode)
            {
                return;
            }

            Debug.Assert(CanProduce, "Generator asked to produce oxygen when it is unable to do so");

            var items = m_inventory.GetItems();
            if (items.Count > 0 && amount > 0f)
            {
                float iceAmount = amount / BlockDefinition.IceToOxygenRatio;
                int index = 0;
                while (index < items.Count)
                {
                    var item = items[index];
                 
                    if (item.Content is MyObjectBuilder_OxygenContainerObject)
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

                if (PowerReceiver != null)
                {
                    PowerReceiver.MaxRequiredInput = BlockDefinition.OperationalPowerConsumption * m_powerConsumptionMultiplier;
                    PowerReceiver.Update();
                }
            }
        }

        #region Sync
        protected override MySyncEntity OnCreateSync()
        {
            return new MySyncOxygenGenerator(this);
        }

        internal new MySyncOxygenGenerator SyncObject
        {
            get
            {
                return (MySyncOxygenGenerator)base.SyncObject;
            }
        }


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

            private MyOxygenGenerator m_generator;

            static MySyncOxygenGenerator()
            {
                MySyncLayer.RegisterEntityMessage<MySyncOxygenGenerator, ChangeAutoRefillMsg>(OnAutoRefillChanged, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncOxygenGenerator, RefillRequestMsg>(OnRefillRequest, MyMessagePermissions.ToServer);
            }

            public MySyncOxygenGenerator(MyOxygenGenerator generator)
                : base(generator)
            {
                m_generator = generator;
            }

            public void ChangeAutoRefill(bool newAutoRefill)
            {
                var msg = new ChangeAutoRefillMsg();
                msg.EntityId = m_generator.EntityId;
                msg.AutoRefill = newAutoRefill;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
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
                syncObject.m_generator.m_autoRefill = message.AutoRefill;
            }

            private static void OnRefillRequest(MySyncOxygenGenerator syncObject, ref RefillRequestMsg message, MyNetworkClient sender)
            {
                syncObject.m_generator.RefillBottles();
            }
        }
        #endregion
    }
}
