using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using VRageMath;
using Sandbox.Game.Multiplayer;
using Sandbox.Engine.Multiplayer;
using SteamSDK;
using Sandbox.Common;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Localization;
using VRage;
using VRage.Utils;
using VRage.ModAPI;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_BatteryBlock))]
    class MyBatteryBlock : MyFunctionalBlock, Sandbox.ModAPI.Ingame.IMyBatteryBlock
    {
        static readonly string[] m_emissiveNames = new string[] { "Emissive0", "Emissive1", "Emissive2", "Emissive3" };
        private MyBatteryBlockDefinition m_batteryBlockDefinition;
		public MyBatteryBlockDefinition BatteryBlockDefinition { get { return m_batteryBlockDefinition; } }
        private bool m_hasRemainingCapacity;
        private float m_maxOutput;
        private float m_currentOutput;
        private float m_currentStoredPower;
        private float m_maxStoredPower;
        private float m_timeRemaining = 0;
        private bool m_isFull;

        private Color m_prevEmissiveColor = Color.Black;
        private int m_prevFillCount = -1;

        private new MySyncBatteryBlock SyncObject;

		private MyResourceSourceComponent m_sourceComp;
		public MyResourceSourceComponent SourceComp
		{
			get { return m_sourceComp; }
			set { if (Components.Contains(typeof(MyResourceSourceComponent))) Components.Remove<MyResourceSourceComponent>(); Components.Add<MyResourceSourceComponent>(value); m_sourceComp = value; }
		}

        protected override bool CheckIsWorking()
        {
			return SourceComp.HasCapacityRemaining && base.CheckIsWorking();
        }

        static MyBatteryBlock()
        {
            var recharge = new MyTerminalControlCheckbox<MyBatteryBlock>("Recharge", MySpaceTexts.BlockPropertyTitle_Recharge, MySpaceTexts.ToolTipBatteryBlock);
			recharge.Getter = (x) => !x.SourceComp.ProductionEnabled;
            recharge.Setter = (x, v) => x.SyncObject.SendProducerEnableChange(!v);
            recharge.Enabled = (x) => !x.SemiautoEnabled;
            recharge.EnableAction();

            var semiAuto = new MyTerminalControlCheckbox<MyBatteryBlock>("SemiAuto", MySpaceTexts.BlockPropertyTitle_Semiauto, MySpaceTexts.ToolTipBatteryBlock_Semiauto);
            semiAuto.Getter = (x) => x.SemiautoEnabled;
            semiAuto.Setter = (x, v) => x.SyncObject.SendSemiautoEnableChange(v);
            semiAuto.EnableAction();

            MyTerminalControlFactory.AddControl(recharge);
            MyTerminalControlFactory.AddControl(semiAuto);
        }

	    public MyBatteryBlock()
	    {
			SourceComp = new MyResourceSourceComponent();
			ResourceSink = new MyResourceSinkComponent();
	    }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            SyncObject = new MySyncBatteryBlock(this);

            MyDebug.AssertDebug(BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_BatteryBlock));
            m_batteryBlockDefinition = BlockDefinition as MyBatteryBlockDefinition;
            MyDebug.AssertDebug(m_batteryBlockDefinition != null);

	        var sourceDataList = new List<MyResourceSourceInfo>
	        {
		        new MyResourceSourceInfo { ResourceTypeId = MyResourceDistributorComponent.ElectricityId, DefinedOutput = m_batteryBlockDefinition.MaxPowerOutput, ProductionToCapacityMultiplier = 60*60}
	        };

			SourceComp.Init(m_batteryBlockDefinition.ResourceSourceGroup, sourceDataList);
			SourceComp.HasCapacityRemainingChanged += (id, source) => UpdateIsWorking();

            MaxStoredPower = m_batteryBlockDefinition.MaxStoredPower;

			ResourceSink.Init(
            m_batteryBlockDefinition.ResourceSinkGroup, 
            m_batteryBlockDefinition.RequiredPowerInput,
			() => (Enabled && IsFunctional && !SourceComp.ProductionEnabled && !m_isFull) ? ResourceSink.MaxRequiredInput : 0.0f);
			ResourceSink.Update();

            var obGenerator = (MyObjectBuilder_BatteryBlock)objectBuilder;
            CurrentStoredPower = obGenerator.CurrentStoredPower;
			SourceComp.SetProductionEnabledByType(MyResourceDistributorComponent.ElectricityId, obGenerator.ProducerEnabled);
            SemiautoEnabled = obGenerator.SemiautoEnabled;

            UpdateMaxOutputAndEmissivity();

            UpdateText();

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            this.IsWorkingChanged += MyBatteryBlock_IsWorkingChanged;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

            if (IsWorking)
                OnStartWorking();
        }

        void MyBatteryBlock_IsWorkingChanged(MyCubeBlock obj)
        {
            UpdateMaxOutputAndEmissivity();
			ResourceSink.Update();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_BatteryBlock)base.GetObjectBuilderCubeBlock(copy);
            ob.CurrentStoredPower = CurrentStoredPower;
			ob.ProducerEnabled = SourceComp.ProductionEnabled;
            ob.SemiautoEnabled = SemiautoEnabled;
            return ob;
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
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

        private void CalculateOutputTimeRemaining()
        {
			if (CurrentStoredPower != 0 && SourceComp.CurrentOutput != 0)
				TimeRemaining = CurrentStoredPower / (SourceComp.CurrentOutput / SourceComp.ProductionToCapacityMultiplier);
            else
                TimeRemaining = 0;
        }

        private void CalculateInputTimeRemaining()
        {
			if (ResourceSink.CurrentInput != 0)
				TimeRemaining = (MaxStoredPower - CurrentStoredPower) / (ResourceSink.CurrentInput / SourceComp.ProductionToCapacityMultiplier);
            else
                TimeRemaining = 0;
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            if (IsFunctional)
            {
                if (SemiautoEnabled)
                {
                    if (CurrentStoredPower == 0)
                    {
						SourceComp.SetProductionEnabledByType(MyResourceDistributorComponent.ElectricityId, false);
                        SyncObject.SendProducerEnableChange(false);
                        SyncObject.SendSemiautoEnableChange(SemiautoEnabled);
                    }
                    if (CurrentStoredPower == MaxStoredPower)
                    {
						SourceComp.SetProductionEnabledByType(MyResourceDistributorComponent.ElectricityId, true);
                        SyncObject.SendProducerEnableChange(true);
                        SyncObject.SendSemiautoEnableChange(SemiautoEnabled);
                    }
                }

				ResourceSink.Update();
                UpdateMaxOutputAndEmissivity();

                int timeDelta = 100 * MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS;
                if (!MySession.Static.CreativeMode)
                {
                    if ((Sync.IsServer && !CubeGrid.GridSystems.ControlSystem.IsControlled) ||
                        CubeGrid.GridSystems.ControlSystem.IsLocallyControlled)
                    {
						if (!SourceComp.ProductionEnabled)
							StorePower(timeDelta, ResourceSink.CurrentInput);
                        else
                            ConsumePower(timeDelta);
                    }
                }
                else
                {
                    if ((Sync.IsServer && IsFunctional && !CubeGrid.GridSystems.ControlSystem.IsControlled) ||
                        CubeGrid.GridSystems.ControlSystem.IsLocallyControlled)
                    {
                        if (!SourceComp.ProductionEnabled)
                            StorePower(timeDelta, (SourceComp.ProductionToCapacityMultiplier*MaxStoredPower)/8f);
                        else
                        {
                            UpdateIsWorking();
                            if (!SourceComp.HasCapacityRemaining)
                                return;
                            ResourceSink.Update();
                            CalculateOutputTimeRemaining();
                        }
                    }
                }

				if (!SourceComp.ProductionEnabled)
                    CalculateInputTimeRemaining();
                else
                    CalculateOutputTimeRemaining();
            }
        }

        protected override void OnEnabledChanged()
        {
            UpdateMaxOutputAndEmissivity();
			ResourceSink.Update();
            base.OnEnabledChanged();
        }

        private void UpdateMaxOutputAndEmissivity()
        {
			SourceComp.SetMaxOutput(ComputeMaxPowerOutput());
            UpdateEmissivity();
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BatteryBlock));
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxOutput));
            MyValueFormatter.AppendWorkInBestUnit(m_batteryBlockDefinition.MaxPowerOutput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(m_batteryBlockDefinition.RequiredPowerInput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxStoredPower));
            MyValueFormatter.AppendWorkHoursInBestUnit(MaxStoredPower, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
			MyValueFormatter.AppendWorkInBestUnit(ResourceSink.CurrentInput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentOutput));
			MyValueFormatter.AppendWorkInBestUnit(SourceComp.CurrentOutput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_StoredPower));
            MyValueFormatter.AppendWorkHoursInBestUnit(CurrentStoredPower, DetailedInfo);
            DetailedInfo.Append("\n");
			if (!SourceComp.ProductionEnabled)
            {
                DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_RechargedIn));
                MyValueFormatter.AppendTimeInBestUnit(m_timeRemaining, DetailedInfo);
            }
            else
            {
                DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_DepletedIn));
                MyValueFormatter.AppendTimeInBestUnit(m_timeRemaining, DetailedInfo);
            }
            RaisePropertiesChanged();
        }

        private void StorePower(int timeDelta, float input)
        {
            float inputPowerPerMillisecond = input / (SourceComp.ProductionToCapacityMultiplier * 1000);
            float increment = (timeDelta * inputPowerPerMillisecond) * 0.80f;

            if ((CurrentStoredPower + increment) < MaxStoredPower)
            {
                CurrentStoredPower += increment;
            }
            else
            {
                CurrentStoredPower = MaxStoredPower;
                TimeRemaining = 0;
                if (!m_isFull)
                {
                    m_isFull = true;
                    SyncObject.IsFullChange(m_isFull);
                }
            }

            SyncObject.CapacityChange(CurrentStoredPower);
        }

        private void ConsumePower(int timeDelta)
        {
            if (!SourceComp.HasCapacityRemaining)
                return;

			float consumptionPerMillisecond = SourceComp.CurrentOutput / (SourceComp.ProductionToCapacityMultiplier * 1000);
            float consumedPower = timeDelta * consumptionPerMillisecond;

            if (consumedPower == 0)
                return;

            if ((CurrentStoredPower - consumedPower) <= 0)
            {
				SourceComp.SetMaxOutput(0);
				SourceComp.SetOutput(0);
                CurrentStoredPower = 0;
                TimeRemaining = 0;
            }
            else
            {
                CurrentStoredPower -= consumedPower;
                if (m_isFull)
                {
                    m_isFull = false;
                    SyncObject.IsFullChange(m_isFull);
                }
            }
            SyncObject.CapacityChange(CurrentStoredPower);
        }

        internal void UpdateEmissivity()
        {
            if (!InScene)
                return;

            if (IsFunctional && Enabled)
            {
                if (IsWorking)
                {

                    float percentage = (CurrentStoredPower / MaxStoredPower);

                    if (SourceComp.ProductionEnabled)
                    {
                        SetEmissive(Color.Green, percentage);
                    }
                    else
                    {
                        SetEmissive(Color.SteelBlue, percentage);
                    }
                }
                else
                {
                    SetEmissive(Color.Red, 0.25f);
                }
            }
            else
            {
                SetEmissive(Color.Red, 1f);
            }
        }

        private void SetEmissive(Color color, float fill)
        {
            int fillCount = (int)(fill * m_emissiveNames.Length);

            if (Render.RenderObjectIDs[0] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED && (color != m_prevEmissiveColor || fillCount != m_prevFillCount))
            {
                for (int i = 0; i < m_emissiveNames.Length; i++)
                {
                    if (i < fillCount)
                    {
                        VRageRender.MyRenderProxy.UpdateColorEmissivity(Render.RenderObjectIDs[0], 0, m_emissiveNames[i], color, 1f);
                    }
                    else
                    {
                        VRageRender.MyRenderProxy.UpdateColorEmissivity(Render.RenderObjectIDs[0], 0, m_emissiveNames[i], Color.Black, 0f);
                    }
                }
                m_prevEmissiveColor = color;
                m_prevFillCount = fillCount;
            }
        }

        public override void OnModelChange()
        {
            base.OnModelChange();

            m_prevFillCount = -1;
        }

        private float ComputeMaxPowerOutput()
        {
            return IsWorking && SourceComp.ProductionEnabled ? m_batteryBlockDefinition.MaxPowerOutput : 0f;
        }

        public float TimeRemaining
        {
            get { return m_timeRemaining; }
            set
            {
                m_timeRemaining = value;
                UpdateText();
            }
        }

		public bool HasCapacityRemaining { get { return SourceComp.HasCapacityRemaining; } }

        public float MaxStoredPower
        {
            get { return m_maxStoredPower; }
            private set
            {
                if (m_maxStoredPower != value)
                    m_maxStoredPower = value;
            }
        }

        public float CurrentStoredPower
        {
            get { return SourceComp.RemainingCapacity; }
            set
            {
                MyDebug.AssertDebug(value <= MaxStoredPower && value >= 0.0f);
                SourceComp.SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, value);
                UpdateMaxOutputAndEmissivity();
            }
        }

        public bool SemiautoEnabled
        {
            get { return m_semiautoEnabled; }
            set
            {
                m_semiautoEnabled = value;

                UpdateMaxOutputAndEmissivity();
				ResourceSink.Update();
            }
        }
        private bool m_semiautoEnabled;

        void ComponentStack_IsFunctionalChanged()
        {
            UpdateMaxOutputAndEmissivity();
        }

        [PreloadRequired]
        class MySyncBatteryBlock
        {
            MyBatteryBlock m_batteryBlock;

            public MySyncBatteryBlock(MyBatteryBlock batteryBlock)
            {
                m_batteryBlock = batteryBlock;
            }

            static MySyncBatteryBlock()
            {

                MySyncLayer.RegisterMessage<ProducerEnabledMsg>(OnProducerEnableChange, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
                MySyncLayer.RegisterMessage<SemiautoEnabledMsg>(OnSemiautoEnableChange, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
                MySyncLayer.RegisterMessage<CapacityMsg>(CapacityChange, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
                MySyncLayer.RegisterMessage<IsFullMsg>(IsFullChange, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
            }

            [MessageIdAttribute(15870, P2PMessageEnum.Reliable)]
            protected struct ProducerEnabledMsg : IEntityMessage
            {
                public long EntityId;

                public long GetEntityId() { return EntityId; }

                public BoolBlit ProducerEnabled;

                public override string ToString()
                {
                    return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
                }
            }

            static void OnProducerEnableChange(ref ProducerEnabledMsg msg, MyNetworkClient sender)
            {
                MyBatteryBlock batteryBlock;
                if (MyEntities.TryGetEntityById<MyBatteryBlock>(msg.EntityId, out batteryBlock))
                {
                    batteryBlock.SourceComp.SetProductionEnabledByType(MyResourceDistributorComponent.ElectricityId, msg.ProducerEnabled);
                    if (Sync.IsServer)
                        Sync.Layer.SendMessageToAll(ref msg);
                }
            }

            public void SendProducerEnableChange(bool producerEnabled)
            {
                var msg = new ProducerEnabledMsg();
                msg.EntityId = m_batteryBlock.EntityId;
                msg.ProducerEnabled = producerEnabled;

                Sync.Layer.SendMessageToServer(ref msg);
            }

            [MessageIdAttribute(15871, P2PMessageEnum.Reliable)]
            protected struct SemiautoEnabledMsg : IEntityMessage
            {
                public long EntityId;

                public long GetEntityId() { return EntityId; }

                public BoolBlit SemiautoEnabled;

                public override string ToString()
                {
                    return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
                }
            }

            static void OnSemiautoEnableChange(ref SemiautoEnabledMsg msg, MyNetworkClient sender)
            {
                MyBatteryBlock batteryBlock;
                if (MyEntities.TryGetEntityById<MyBatteryBlock>(msg.EntityId, out batteryBlock))
                {
                    batteryBlock.SemiautoEnabled = msg.SemiautoEnabled;
                    if (Sync.IsServer)
                        Sync.Layer.SendMessageToAll(ref msg);
                }
            }

            public void SendSemiautoEnableChange(bool semiautoEnabled)
            {
                var msg = new SemiautoEnabledMsg();
                msg.EntityId = m_batteryBlock.EntityId;
                msg.SemiautoEnabled = semiautoEnabled;

                Sync.Layer.SendMessageToServer(ref msg);
            }

            [MessageIdAttribute(1587, P2PMessageEnum.Reliable)]
            protected struct CapacityMsg : IEntityMessage
            {
                public long EntityId;

                public long GetEntityId() { return EntityId; }

                public float capacity;

                public override string ToString()
                {
                    return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
                }
            }

            static void CapacityChange(ref CapacityMsg msg, MyNetworkClient sender)
            {
                MyBatteryBlock batteryBlock;
                if (MyEntities.TryGetEntityById<MyBatteryBlock>(msg.EntityId, out batteryBlock))
                {
                    batteryBlock.CurrentStoredPower = msg.capacity;
                    if (Sync.IsServer)
                        Sync.Layer.SendMessageToAll(ref msg);
                }
            }

            public void CapacityChange(float capacity)
            {
                var msg = new CapacityMsg();
                msg.EntityId = m_batteryBlock.EntityId;
                msg.capacity = capacity;

                Sync.Layer.SendMessageToServer(ref msg);
            }

        [MessageIdAttribute(1588, P2PMessageEnum.Reliable)]
            protected struct IsFullMsg : IEntityMessage
            {
                public long EntityId;

                public long GetEntityId() { return EntityId; }

                public BoolBlit isFull;

                public override string ToString()
                {
                    return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
                }
            }

            static void IsFullChange(ref IsFullMsg msg, MyNetworkClient sender)
            {
                MyBatteryBlock batteryBlock;
                if (MyEntities.TryGetEntityById<MyBatteryBlock>(msg.EntityId, out batteryBlock))
                {
                    batteryBlock.m_isFull = msg.isFull;
                    if (Sync.IsServer)
                        Sync.Layer.SendMessageToAll(ref msg);
                }
            }

            public void IsFullChange(bool isFull)
            {
                var msg = new IsFullMsg();
                msg.EntityId = m_batteryBlock.EntityId;
                msg.isFull = isFull;

                Sync.Layer.SendMessageToServer(ref msg);
            }
        }
    }
}
