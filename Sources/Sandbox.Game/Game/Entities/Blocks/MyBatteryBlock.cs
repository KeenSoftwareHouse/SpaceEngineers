using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Havok;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Physics;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.World;

using VRage.Trace;
using VRageMath;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens;
using Sandbox.Engine.Multiplayer;
using SteamSDK;

namespace Sandbox.Game.Entities
{
    using Sandbox.Game.GameSystems.Conveyors;
    using System.Reflection;
    using Sandbox.Common;
    using Sandbox.Game.GameSystems;
    using Sandbox.Game.Screens.Terminal.Controls;
    using Sandbox.Game.Localization;
    using VRage;
    using VRage.Utils;
    using VRage.ModAPI;

    [MyCubeBlockType(typeof(MyObjectBuilder_BatteryBlock))]
    class MyBatteryBlock : MyFunctionalBlock, IMyPowerProducer, IMyPowerConsumer, Sandbox.ModAPI.Ingame.IMyBatteryBlock
    {
        static string[] m_emissiveNames = new string[] { "Emissive0", "Emissive1", "Emissive2", "Emissive3" };
        private MyBatteryBlockDefinition m_batteryBlockDefinition;
        private bool m_hasRemainingCapacity;
        private float m_maxPowerOutput;
        private float m_currentPowerOutput;
        private float m_currentStoredPower;
        private float m_maxStoredPower;
        private float m_timeRemaining = 0;
        private bool m_isFull;

        private Color m_prevEmissiveColor = Color.Black;
        private int m_prevFillCount = -1;

        private new MySyncBatteryBlock SyncObject;

        protected override bool CheckIsWorking()
        {
            return HasCapacityRemaining && base.CheckIsWorking();
        }

        static MyBatteryBlock()
        {
            var recharge = new MyTerminalControlCheckbox<MyBatteryBlock>("Recharge", MySpaceTexts.BlockPropertyTitle_Recharge, MySpaceTexts.ToolTipBatteryBlock);
            recharge.Getter = (x) => !x.ProductionEnabled;
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

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            SyncObject = new MySyncBatteryBlock(this);

            MyDebug.AssertDebug(BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_BatteryBlock));
            m_batteryBlockDefinition = BlockDefinition as MyBatteryBlockDefinition;
            MyDebug.AssertDebug(m_batteryBlockDefinition != null);

            MaxStoredPower = m_batteryBlockDefinition.MaxStoredPower;

            PowerReceiver = new MyPowerReceiver(
            MyConsumerGroupEnum.BatteryBlock,
            true,
            m_batteryBlockDefinition.RequiredPowerInput,
            () => (Enabled && IsFunctional && !ProductionEnabled && (CurrentPowerOutput == 0) && !m_isFull) ? PowerReceiver.MaxRequiredInput : 0.0f);
            PowerReceiver.Update();

            CurrentPowerOutput = 0;

            var obGenerator = (MyObjectBuilder_BatteryBlock)objectBuilder;
            CurrentStoredPower = obGenerator.CurrentStoredPower;
            ProductionEnabled = obGenerator.ProducerEnabled;
            SemiautoEnabled = obGenerator.SemiautoEnabled;

            RefreshRemainingCapacity();

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
            PowerReceiver.Update();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_BatteryBlock)base.GetObjectBuilderCubeBlock(copy);
            ob.CurrentStoredPower = CurrentStoredPower;
            ob.ProducerEnabled = ProductionEnabled;
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

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
        }

        private void CalculateOutputTimeRemaining()
        {
            if (CurrentStoredPower != 0 && CurrentPowerOutput != 0)
                TimeRemaining = CurrentStoredPower / (CurrentPowerOutput / (60 * 60));
            else
                TimeRemaining = 0;
        }

        private void CalculateInputTimeRemaining()
        {
            if (PowerReceiver.CurrentInput != 0)
                TimeRemaining = (MaxStoredPower - CurrentStoredPower) / (PowerReceiver.CurrentInput / (60 * 60));
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
                        ProductionEnabled = false;
                        SyncObject.SendProducerEnableChange(false);
                        SyncObject.SendSemiautoEnableChange(SemiautoEnabled);
                    }
                    if (CurrentStoredPower == MaxStoredPower)
                    {
                        ProductionEnabled = true;
                        SyncObject.SendProducerEnableChange(true);
                        SyncObject.SendSemiautoEnableChange(SemiautoEnabled);
                    }
                }

                PowerReceiver.Update();
                RefreshRemainingCapacity();

                int timeDelta = 100 * MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS;
                if (!MySession.Static.CreativeMode)
                {
                    if ((Sync.IsServer && !CubeGrid.GridSystems.ControlSystem.IsControlled) ||
                        CubeGrid.GridSystems.ControlSystem.IsLocallyControlled)
                    {
                        if (!ProductionEnabled)
                            StorePower(timeDelta, PowerReceiver.CurrentInput);
                        else
                            ConsumePower(timeDelta);
                    }
                }
                else
                {
                    if ((Sync.IsServer && IsFunctional && !CubeGrid.GridSystems.ControlSystem.IsControlled) ||
                        CubeGrid.GridSystems.ControlSystem.IsLocallyControlled)
                    {
                        if (!ProductionEnabled)
                            StorePower(timeDelta, (3600 * MaxStoredPower) / 8f );
                        else
                            if (ProductionEnabled)
                            {
                                RefreshRemainingCapacity();
                                UpdateIsWorking();
                                if (!HasCapacityRemaining)
                                    return;
                                PowerReceiver.Update();
                                CalculateOutputTimeRemaining();
                            }
                    }
                }

                if (!ProductionEnabled)
                    CalculateInputTimeRemaining();
                else
                    CalculateOutputTimeRemaining();
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override void OnRemovedByCubeBuilder()
        {
            base.OnRemovedByCubeBuilder();
        }

        protected override void OnEnabledChanged()
        {
            UpdateMaxOutputAndEmissivity();
            PowerReceiver.Update();
            base.OnEnabledChanged();
        }

        private void UpdateMaxOutputAndEmissivity()
        {
            MaxPowerOutput = ComputeMaxPowerOutput();
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
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.CurrentInput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentOutput));
            MyValueFormatter.AppendWorkInBestUnit(CurrentPowerOutput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_StoredPower));
            MyValueFormatter.AppendWorkHoursInBestUnit(CurrentStoredPower, DetailedInfo);
            DetailedInfo.Append("\n");
            if (!ProductionEnabled)
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
            float inputPowerPerMillisecond = input / (60 * 60 * 1000);
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
            if (!HasCapacityRemaining)
                return;

            float consumptionPerMillisecond = CurrentPowerOutput / (60 * 60 * 1000);
            float consumedPower = (float)(timeDelta * consumptionPerMillisecond);

            if (consumedPower == 0)
            {
                return;
            }

            if ((CurrentStoredPower - consumedPower) <= 0)
            {
                MaxPowerOutput = 0;
                CurrentPowerOutput = 0;
                CurrentStoredPower = 0;
                TimeRemaining = 0;
                HasCapacityRemaining = false;
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
        private void RefreshRemainingCapacity()
        {
            if (CurrentStoredPower != 0)
                HasCapacityRemaining = true;
            UpdateMaxOutputAndEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (!InScene)
                return;

            if (IsFunctional && Enabled)
            {
                if (IsWorking)
                {

                    float percentage = (CurrentStoredPower / MaxStoredPower);

                    if (ProductionEnabled)
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
                        VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[i], null, color, null, null, 1f);
                    }
                    else
                    {
                        VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[i], null, Color.Black, null, null, 0f);
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
            return IsWorking && ProductionEnabled ? m_batteryBlockDefinition.MaxPowerOutput : 0f;
        }

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        #region IMyPowerProducer

        public event Action<IMyPowerProducer> MaxPowerOutputChanged;
        public event Action<IMyPowerProducer> HasCapacityRemainingChanged;

        bool IMyPowerProducer.Enabled
        {
            get { return ProductionEnabled; }
            set { ProductionEnabled = value; }
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

        public bool HasCapacityRemaining
        {
            get { return m_hasRemainingCapacity; }
            private set
            {
                if (m_hasRemainingCapacity != value)
                {
                    m_hasRemainingCapacity = value;
                    UpdateIsWorking();
                    if (HasCapacityRemainingChanged != null)
                        HasCapacityRemainingChanged(this);
                }
            }
        }

        public float MaxPowerOutput
        {
            get { return m_maxPowerOutput; }
            private set
            {
                if (m_maxPowerOutput != value)
                {
                    m_maxPowerOutput = value;
                    if (MaxPowerOutputChanged != null)
                        MaxPowerOutputChanged(this);
                }
            }
        }

        float ModAPI.Ingame.IMyPowerProducer.DefinedPowerOutput
        {
            get { return m_batteryBlockDefinition.MaxPowerOutput; }
        }

        public float CurrentPowerOutput
        {
            get { return m_currentPowerOutput; }
            set
            {
                MyDebug.AssertRelease(!float.IsNaN(value), "Reactor Power Output is NaN.");
                MyDebug.AssertDebug(value <= MaxPowerOutput && value >= 0.0f);
                m_currentPowerOutput = value;
            }
        }

        public float MaxStoredPower
        {
            get { return m_maxStoredPower; }
            private set
            {
                if (m_maxStoredPower != value)
                {
                    m_maxStoredPower = value;
                }
            }
        }

        public float CurrentStoredPower
        {
            get { return m_currentStoredPower; }
            set
            {
                MyDebug.AssertDebug(value <= MaxStoredPower && value >= 0.0f);
                m_currentStoredPower = value;
            }
        }

        public float RemainingCapacity { get { return m_currentStoredPower; } private set { ;} }

        MyProducerGroupEnum IMyPowerProducer.Group
        {
            get { return MyProducerGroupEnum.Battery; }
        }

        public bool ProductionEnabled
        {
            get { return m_producerEnabled; }
            set
            {
                m_producerEnabled = value;

                if (!m_producerEnabled)
                {
                    CurrentPowerOutput = 0.0f;
                }

                UpdateEmissivity();
            }
        }
        private bool m_producerEnabled;

        public bool SemiautoEnabled
        {
            get { return m_semiautoEnabled; }
            set
            {
                m_semiautoEnabled = value;

                UpdateMaxOutputAndEmissivity();
                PowerReceiver.Update();
            }
        }
        private bool m_semiautoEnabled;

        #endregion

        void ComponentStack_IsFunctionalChanged()
        {
            UpdateMaxOutputAndEmissivity();
        }

        #region Sync class

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
                    batteryBlock.ProductionEnabled = msg.ProducerEnabled;
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
        #endregion
    }
}
