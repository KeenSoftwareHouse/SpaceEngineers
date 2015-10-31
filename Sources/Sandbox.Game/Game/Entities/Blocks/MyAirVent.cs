using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.ModAPI.Ingame;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Common;
using Sandbox.Game.EntityComponents;
using VRage;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Import;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRage.Components;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_AirVent))]
    class MyAirVent : MyFunctionalBlock, IMyAirVent, IMyGasBlock
    {
        private static readonly string[] m_emissiveNames = { "Emissive1", "Emissive2", "Emissive3", "Emissive4" };

        MyModelDummy VentDummy
        {
            get
            {
                MyModelDummy dummy;
                Model.Dummies.TryGetValue("vent_001", out dummy);
                return dummy;
            }
        }

        private Color m_prevColor = Color.White;
        private int m_prevFillCount = -1;

        private int m_lastOutputUpdateTime;
        private int m_lastInputUpdateTime;
        private int m_updateCounter;
        private bool m_updateSink = false;
        private float m_nextGasTransfer;

        private bool m_isProducing;
        private bool m_producedSinceLastUpdate;
        private MyParticleEffect m_effect;

        private MyToolbarItem m_onFullAction;
        private MyToolbarItem m_onEmptyAction;
        private MyToolbar m_actionToolbar;

        private bool? m_wasRoomFull;
        private bool? m_wasRoomEmpty;
        private readonly MyDefinitionId m_oxygenGasId = new MyDefinitionId(typeof (MyObjectBuilder_GasProperties), "Oxygen");

        public bool CanVent { get { return (MySession.Static.Settings.EnableOxygen) && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && IsWorking && Enabled && IsFunctional && !IsDepressurizing; } }

        private float GasOutputPerSecond { get { return (SourceComp.ProductionEnabledByType(m_oxygenGasId) ? SourceComp.CurrentOutputByType(m_oxygenGasId) : 0f); } }
        private float GasInputPerSecond { get { return ResourceSink.CurrentInputByType(m_oxygenGasId); } }
        private float GasOutputPerUpdate { get { return GasOutputPerSecond * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS; } }
        private float GasInputPerUpdate { get { return GasInputPerSecond * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS; } }

        private bool m_isDepressurizing;
        public bool IsDepressurizing { get { return m_isDepressurizing; } set { SetDepressurizing(value); } }

        private MyResourceSourceComponent m_sourceComp;
        public MyResourceSourceComponent SourceComp
        {
            get { return m_sourceComp; }
            set { if (Components.Contains(typeof(MyResourceSourceComponent))) Components.Remove<MyResourceSourceComponent>(); Components.Add<MyResourceSourceComponent>(value); m_sourceComp = value; }
        }


        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint { get { return m_conveyorEndpoint; } }

        private new MyAirVentDefinition BlockDefinition { get { return (MyAirVentDefinition)base.BlockDefinition; } }

        #region Initialization
        static MyAirVent()
        {
            var isDepressurizing = new MyTerminalControlOnOffSwitch<MyAirVent>("Depressurize", MySpaceTexts.BlockPropertyTitle_Depressurize, MySpaceTexts.BlockPropertyDescription_Depressurize);
            isDepressurizing.Getter = (x) => x.IsDepressurizing;
            isDepressurizing.Setter = (x, v) => x.SyncObject.ChangeDepressurizationMode(v);
            isDepressurizing.EnableToggleAction();
            isDepressurizing.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(isDepressurizing);

            var toolbarButton = new MyTerminalControlButton<MyAirVent>("Open Toolbar", MySpaceTexts.BlockPropertyTitle_SensorToolbarOpen, MySpaceTexts.BlockPropertyDescription_SensorToolbarOpen,
                delegate(MyAirVent self)
                {
                    if (self.m_onFullAction != null)
                    {
                        self.m_actionToolbar.SetItemAtIndex(0, self.m_onFullAction);
                    }
                    if (self.m_onEmptyAction != null)
                    {
                        self.m_actionToolbar.SetItemAtIndex(1, self.m_onEmptyAction);
                    }

                    self.m_actionToolbar.ItemChanged += self.Toolbar_ItemChanged;
                    if (MyGuiScreenCubeBuilder.Static == null)
                    {
                        MyToolbarComponent.CurrentToolbar = self.m_actionToolbar;
                        MyGuiScreenBase screen = MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.ToolbarConfigScreen, 0, self);
                        MyToolbarComponent.AutoUpdate = false;

                        screen.Closed += (source) =>
                            {
                                MyToolbarComponent.AutoUpdate = true;
                                self.m_actionToolbar.ItemChanged -= self.Toolbar_ItemChanged;
                                self.m_actionToolbar.Clear();
                            };
                        MyGuiSandbox.AddScreen(screen);
                    }
                });
            toolbarButton.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(toolbarButton);
        }

        public MyAirVent()
        {
            ResourceSink = new MyResourceSinkComponent(2);
            SourceComp = new MyResourceSourceComponent();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            base.Init(objectBuilder, cubeGrid);

            var builder = (MyObjectBuilder_AirVent)objectBuilder;

            InitializeConveyorEndpoint();
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

            SourceComp.Init(BlockDefinition.ResourceSourceGroup, new MyResourceSourceInfo { ResourceTypeId = m_oxygenGasId, DefinedOutput = BlockDefinition.VentilationCapacityPerSecond, ProductionToCapacityMultiplier = 1 });
            SourceComp.OutputChanged += Source_OutputChanged;
            var sinkDataList = new List<MyResourceSinkInfo>
	        {
				new MyResourceSinkInfo {ResourceTypeId = MyResourceDistributorComponent.ElectricityId, MaxRequiredInput = BlockDefinition.OperationalPowerConsumption, RequiredInputFunc = ComputeRequiredPower},
				new MyResourceSinkInfo {ResourceTypeId = m_oxygenGasId, MaxRequiredInput = BlockDefinition.VentilationCapacityPerSecond, RequiredInputFunc = () => VentingCapacity(1f)},
	        };

            ResourceSink.Init(
                BlockDefinition.ResourceSinkGroup,
                sinkDataList);
			ResourceSink.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
            ResourceSink.CurrentInputChanged += Sink_CurrentInputChanged;

            m_updateCounter = 0;
            m_lastOutputUpdateTime = m_updateCounter;
            m_lastInputUpdateTime = m_updateCounter;
            m_nextGasTransfer = 0f;

            m_actionToolbar = new MyToolbar(MyToolbarType.ButtonPanel, 2, 1);
            m_actionToolbar.DrawNumbers = false;
            m_actionToolbar.Init(null, this);

            if (builder.OnFullAction != null)
                m_onFullAction = MyToolbarItemFactory.CreateToolbarItem(builder.OnFullAction);

            if (builder.OnEmptyAction != null)
                m_onEmptyAction = MyToolbarItemFactory.CreateToolbarItem(builder.OnEmptyAction);

            UpdateEmissivity();
            UdpateTexts();

            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            IsWorkingChanged += MyAirVent_IsWorkingChanged;

            SetDepressurizing(builder.IsDepressurizing);
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_AirVent)base.GetObjectBuilderCubeBlock(copy);

            builder.IsDepressurizing = m_isDepressurizing;
            if (m_onFullAction != null)
                builder.OnFullAction = m_onFullAction.GetObjectBuilder();

            if (m_onEmptyAction != null)
                builder.OnEmptyAction = m_onEmptyAction.GetObjectBuilder();

            return builder;
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

            int sourceUpdateFrames = (m_updateCounter - m_lastOutputUpdateTime);
            int sinkUpdateFrames = (m_updateCounter - m_lastInputUpdateTime);

            float gasInput = GasInputPerUpdate * sinkUpdateFrames;
            float gasOutput = GasOutputPerUpdate * sourceUpdateFrames;

            float totalTransfer = gasInput - gasOutput + m_nextGasTransfer;
            if (CheckTransfer(totalTransfer))
            {
                Transfer(totalTransfer);
                ResourceSink.Update();
                m_lastOutputUpdateTime = m_updateCounter;
                m_lastInputUpdateTime = m_updateCounter;
            }
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            if (Sync.IsServer && CanVent)
                UpdateActions();

            UpdateEmissivity();
        }
        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            if (m_producedSinceLastUpdate)
            {
                if (m_effect == null)
                {
                    CreateEffect();
                }
            }
            else
            {
                if (m_effect != null)
                {
                    m_effect.Stop();
                    m_effect = null;
                }
            }

            if (MyFakes.ENABLE_OXYGEN_SOUNDS)
                UpdateSound();

            m_isProducing = m_producedSinceLastUpdate;
            m_producedSinceLastUpdate = false;
            var block = GetOxygenBlock();

            int sourceUpdateFrames = (m_updateCounter - m_lastOutputUpdateTime);
            int sinkUpdateFrames = (m_updateCounter - m_lastInputUpdateTime);

            float gasInput = GasInputPerUpdate * sinkUpdateFrames;
            float gasOutput = GasOutputPerUpdate * sourceUpdateFrames;
            float totalTransfer = gasInput - gasOutput + m_nextGasTransfer;
            Transfer(totalTransfer);

            SourceComp.SetRemainingCapacityByType(m_oxygenGasId, (float)(block.Room != null ? block.Room.OxygenAmount : 0));

            m_updateCounter = 0;
            m_lastOutputUpdateTime = m_updateCounter;
            m_lastInputUpdateTime = m_updateCounter;
            ResourceSink.Update();

            UdpateTexts();
        }

        private void Source_OutputChanged(MyDefinitionId changedResourceId, float oldOutput, MyResourceSourceComponent source)
        {
            float timeSinceLastUpdateSeconds = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastOutputUpdateTime) / 1000f * MyEngineConstants.UPDATE_STEPS_PER_SECOND;
            float outputAmount = oldOutput * timeSinceLastUpdateSeconds;
            m_nextGasTransfer -= outputAmount;
            m_lastOutputUpdateTime = m_updateCounter;
        }

        private void Sink_CurrentInputChanged(MyDefinitionId resourceTypeId, float oldInput, MyResourceSinkComponent sink)
        {
            if (resourceTypeId != m_oxygenGasId)
                return;

            float timeSinceLastUpdateSeconds = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastInputUpdateTime) / 1000f * MyEngineConstants.UPDATE_STEPS_PER_SECOND;
            float inputAmount = oldInput * timeSinceLastUpdateSeconds;
            m_nextGasTransfer += inputAmount;
            m_lastInputUpdateTime = m_updateCounter;
        }

        private void Transfer(float transferAmount)
        {
            if (transferAmount > 0)
                VentToRoom(transferAmount);
            else if (transferAmount < 0)
                DrainFromRoom(-transferAmount);
        }

        private bool CheckTransfer(float testTransfer)
        {
            if (testTransfer == 0f)
                return false;

            float remainingCapacity = SourceComp.RemainingCapacityByType(m_oxygenGasId);
            float nextCapacity = remainingCapacity + testTransfer;
            float gasTransferPerUpdate = GasInputPerUpdate - GasOutputPerUpdate;
            float paddedNextCapacity = nextCapacity + gasTransferPerUpdate*15;
            var maxCapacity = paddedNextCapacity + 1;
            var block = GetOxygenBlock();
            if (block.Room != null)
                maxCapacity = (float)block.Room.MaxOxygen(CubeGrid.GridSize);
            return (paddedNextCapacity <= 0f || paddedNextCapacity >= maxCapacity);
        }

        private void UpdateSound()
        {
            if (IsWorking)
            {
                if (m_producedSinceLastUpdate)
                {
                    if (IsDepressurizing)
                    {
                        if (m_soundEmitter.SoundId != BlockDefinition.DepressurizeSound.SoundId)
                        {
                            m_soundEmitter.PlaySound(BlockDefinition.DepressurizeSound, true);
                        }
                    }
                    else if (m_soundEmitter.SoundId != BlockDefinition.PressurizeSound.SoundId)
                    {
                        m_soundEmitter.PlaySound(BlockDefinition.PressurizeSound, true);
                    }
                }
                else if (m_soundEmitter.SoundId != BlockDefinition.IdleSound.SoundId)
                {
                    m_soundEmitter.PlaySound(BlockDefinition.IdleSound, true, false);
                }
            }
            else if (m_soundEmitter.IsPlaying)
            {
                m_soundEmitter.StopSound(false);
            }

            m_soundEmitter.Update();
        }

        private void CreateEffect()
        {
            if (m_effect != null)
            {
                m_effect.Stop();
                m_effect = null;
            }

            if (MyParticlesManager.TryCreateParticleEffect(48, out m_effect))
            {
                Matrix mat;
                Orientation.GetMatrix(out mat);

                var orientation = mat;

                if (IsDepressurizing)
                {
                    orientation.Left = mat.Right;
                    orientation.Up = mat.Down;
                    orientation.Forward = mat.Backward;
                }

                orientation = Matrix.Multiply(orientation, CubeGrid.PositionComp.WorldMatrix.GetOrientation());
                orientation.Translation = CubeGrid.GridIntegerToWorld(Position + mat.Forward / 4f);

                m_effect.WorldMatrix = orientation;
                m_effect.AutoDelete = false;

                m_effect.UserScale = 3f;
            }
        }

        private void UpdateActions()
        {   
            float oxygenLevel = GetRoomOxygen();

            if (!m_wasRoomEmpty.HasValue || !m_wasRoomFull.HasValue)
            {
                m_wasRoomEmpty = false;
                m_wasRoomFull = false;

                if (oxygenLevel > 0.99f)
                    m_wasRoomFull = true;
                else if (oxygenLevel < 0.01f)
                    m_wasRoomEmpty = true;
                return;
            }

            if (oxygenLevel > 0.99f)
            {
                if (!m_wasRoomFull.Value)
                {
                    ExecuteAction(m_onFullAction);
                    m_wasRoomFull = true;
                }
            }
            else if (oxygenLevel < 0.01f)
            {
                if (!m_wasRoomEmpty.Value)
                {
                    ExecuteAction(m_onEmptyAction);
                    m_wasRoomEmpty = true;
                }
            }
            else
            {
                m_wasRoomFull = false;
                m_wasRoomEmpty = false;
            }
        }

        private void ExecuteAction(MyToolbarItem action)
        {
            m_actionToolbar.SetItemAtIndex(0, action);
            m_actionToolbar.UpdateItem(0);
            m_actionToolbar.ActivateItemAtSlot(0);
            m_actionToolbar.Clear();
        }

        private void Toolbar_ItemChanged(MyToolbar self, MyToolbar.IndexArgs index)
        {
            SyncObject.SendToolbarItemChanged(ToolbarItem.FromItem(self.GetItemAtIndex(index.ItemIndex)), index.ItemIndex);
        }

        private float ComputeRequiredPower()
        {
            if (!MySession.Static.Settings.EnableOxygen && Enabled && IsFunctional)
                return 0f;

            return m_isProducing ? BlockDefinition.OperationalPowerConsumption : BlockDefinition.StandbyPowerConsumption;
        }

        void PowerReceiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        void ComponentStack_IsFunctionalChanged()
        {
            SourceComp.Enabled = IsWorking;
            ResourceSink.Update();
            UpdateEmissivity();
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();
            SourceComp.Enabled = IsWorking;
            ResourceSink.Update();
            UpdateEmissivity();
        }

        void MyAirVent_IsWorkingChanged(MyCubeBlock obj)
        {
            SourceComp.Enabled = IsWorking;
            ResourceSink.Update();
            UpdateEmissivity();
        }

        protected override bool CheckIsWorking()
        {
            return base.CheckIsWorking() && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId);
        }

        public override void OnModelChange()
        {
            base.OnModelChange();

            m_prevFillCount = -1;
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            UpdateEmissivity();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();

            UpdateEmissivity();
        }

        private float GetRoomOxygen()
        {
            var oxygenBlock = GetOxygenBlock();
            if (oxygenBlock.Room != null)
            {
                float roomLevel = oxygenBlock.OxygenLevel(CubeGrid.GridSize);
                if (oxygenBlock.Room.IsPressurized)
                {
                    return roomLevel;
                }
                else
                {
                    return Math.Max(roomLevel, oxygenBlock.Room.EnvironmentOxygen);
                }
            }
            return 0f;
        }

        private void UpdateEmissivity()
        {
            if (CanVent || IsDepressurizing)
            {
                var oxygenBlock = GetOxygenBlock();
                if (oxygenBlock.Room != null && oxygenBlock.Room.IsPressurized)
                {
                    SetEmissive(m_isDepressurizing ? Color.Teal : Color.Green, oxygenBlock.OxygenLevel(CubeGrid.GridSize));
                }
                else
                {
                    if (oxygenBlock.Room != null)
                    {
                        float oxygenLevel = oxygenBlock.OxygenLevel(CubeGrid.GridSize);
                        oxygenLevel = Math.Max(oxygenLevel, oxygenBlock.Room.EnvironmentOxygen);
                        SetEmissive(Color.Yellow, oxygenLevel);
                    }
                }
            }
            else
            {
                SetEmissive(Color.Red, 1f);
            }
        }

        private void SetDepressurizing(bool newValue)
        {
            m_isDepressurizing = newValue;

            SourceComp.SetProductionEnabledByType(m_oxygenGasId, newValue);
            ResourceSink.Update();
        }

        void UdpateTexts()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
			MyValueFormatter.AppendWorkInBestUnit(ResourceSink.MaxRequiredInput, DetailedInfo);
            DetailedInfo.Append("\n");

            if (!MySession.Static.Settings.EnableOxygen)
            {
                DetailedInfo.Append("Oxygen disabled in world settigns!");
            }
            else
            {
                var oxygenBlock = GetOxygenBlock();
                if (oxygenBlock.Room == null || !oxygenBlock.Room.IsPressurized)
                {
                    DetailedInfo.Append("Room pressure: Not pressurized");
                }
                else
                {
                    DetailedInfo.Append("Room pressure: " + (oxygenBlock.Room.OxygenLevel(CubeGrid.GridSize) * 100f).ToString("F") + "%");
                }
            }
        }

        private void SetEmissive(Color color, float fill)
        {
            int fillCount = (int)(fill * m_emissiveNames.Length);

            if (Render.RenderObjectIDs[0] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED && (color != m_prevColor || fillCount != m_prevFillCount))
            {
                for (int i = 0; i < m_emissiveNames.Length; i++)
                {
                    VRageRender.MyRenderProxy.UpdateColorEmissivity(Render.RenderObjectIDs[0], 0, m_emissiveNames[i], i <= fillCount ? color : Color.Black, 0);
                }
                m_prevColor = color;
                m_prevFillCount = fillCount;
            }
        }

        protected override void Closing()
        {
            base.Closing();

            if (m_effect != null)
                m_effect.Stop();

            m_soundEmitter.StopSound(true);
        }
        #endregion

        #region Venting
        private MyOxygenBlock GetOxygenBlock()
        {
            if (!MySession.Static.Settings.EnableOxygen || VentDummy == null)
            {
                return new MyOxygenBlock();
            }

            MatrixD dummyLocal = MatrixD.Normalize(VentDummy.Matrix);
            MatrixD worldMatrix = MatrixD.Multiply(dummyLocal, WorldMatrix);

            return CubeGrid.GridSystems.GasSystem.GetOxygenBlock(worldMatrix.Translation);
        }

        bool IMyGasBlock.IsWorking()
        {
            return CanVent;
        }

        float VentingCapacity(float deltaTime)
        {
            if (!CanVent)
                return 0f;

            var oxygenBlock = GetOxygenBlock();
            if (oxygenBlock.Room == null || !oxygenBlock.Room.IsPressurized)
                return 0f;

            float neededOxygen = (float)oxygenBlock.Room.MissingOxygen(CubeGrid.GridSize);

            if (neededOxygen <= 0f)
                neededOxygen = 0f;

            return Math.Min(neededOxygen, BlockDefinition.VentilationCapacityPerSecond * deltaTime);
        }

        void VentToRoom(float amount)
        {
            if (amount == 0f || IsDepressurizing)
                return;

            Debug.Assert(!IsDepressurizing, "Vent asked to vent when it is supposed to depressurize");
            Debug.Assert(amount >= 0f);

            var oxygenBlock = GetOxygenBlock();
            if (oxygenBlock.Room == null || !oxygenBlock.Room.IsPressurized)
                return;

            oxygenBlock.Room.OxygenAmount += amount;
            if (oxygenBlock.Room.OxygenLevel(CubeGrid.GridSize) > 1f)
                oxygenBlock.Room.OxygenAmount = oxygenBlock.Room.MaxOxygen(CubeGrid.GridSize);

            SourceComp.SetRemainingCapacityByType(m_oxygenGasId, (float)oxygenBlock.Room.OxygenAmount);

            m_nextGasTransfer = 0;

            if (amount > 0)
                m_producedSinceLastUpdate = true;
        }

        void DrainFromRoom(float amount)
        {
            if (amount == 0f || !IsDepressurizing)
                return;

            Debug.Assert(IsDepressurizing, "Vent asked to depressurize when it is supposed to pressurize");
            Debug.Assert(amount >= 0f);

            var oxygenBlock = GetOxygenBlock();
            float oxygenInRoom = oxygenBlock.Room == null ? 0f : (float)oxygenBlock.Room.OxygenAmount;
            SourceComp.SetRemainingCapacityByType(m_oxygenGasId, oxygenInRoom);
            if (oxygenBlock.Room == null)
                return;

            if (oxygenBlock.Room.IsPressurized)
            {
                oxygenBlock.Room.OxygenAmount -= amount;
                if (oxygenBlock.Room.OxygenAmount < 0f)
                {
                    oxygenBlock.Room.OxygenAmount = 0f;
                }

                if (amount > 0)
                    m_producedSinceLastUpdate = true;
            }
            else
            {
                //Take from environment, nothing to do
                m_producedSinceLastUpdate = true;
            }

            m_nextGasTransfer = 0f;
        }
        #endregion

        /// <summary>
        /// Compatibility method
        /// </summary>
        public bool IsPressurized()
        {
            return CanPressurize;
        }

        public bool CanPressurize
        {
            get
            {

                var oxygenBlock = GetOxygenBlock();
                if (oxygenBlock.Room == null)
                    return false;

                return oxygenBlock.Room.IsPressurized;
            }
        }

        public float GetOxygenLevel()
        {
            var oxygenBlock = GetOxygenBlock();
            if (oxygenBlock.Room == null)
                return 0f;

            return oxygenBlock.OxygenLevel(CubeGrid.GridSize);
        }

        #region Sync
        protected override MySyncEntity OnCreateSync()
        {
            return new MySyncAirVent(this);
        }

        internal new MySyncAirVent SyncObject { get { return (MySyncAirVent)base.SyncObject; } }

        [PreloadRequired]
        internal class MySyncAirVent : MySyncEntity
        {
            [MessageIdAttribute(8000, P2PMessageEnum.Reliable)]
            protected struct ChangeDepressurizationModeMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public BoolBlit IsDepressurizing;
            }

            [ProtoContract]
            [MessageIdAttribute(8001, P2PMessageEnum.Reliable)]
            protected struct ChangeToolbarItemMsg : IEntityMessage
            {
                [ProtoMember]
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                [ProtoMember]
                public ToolbarItem Item;

                [ProtoMember]
                public int Index;

                public override string ToString()
                {
                    return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
                }
            }

            private readonly MyAirVent m_airVent;

            static MySyncAirVent()
            {
                MySyncLayer.RegisterEntityMessage<MySyncAirVent, ChangeDepressurizationModeMsg>(OnStockipleModeChanged, MyMessagePermissions.FromServer|MyMessagePermissions.ToServer | MyMessagePermissions.ToSelf);
                MySyncLayer.RegisterEntityMessage<MySyncAirVent, ChangeToolbarItemMsg>(OnToolbarItemChanged, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer | MyMessagePermissions.ToSelf);
            }

            public MySyncAirVent(MyAirVent airVent)
                : base(airVent)
            {
                m_airVent = airVent;
            }

            public void ChangeDepressurizationMode(bool newDepressurizationMode)
            {
                var msg = new ChangeDepressurizationModeMsg();
                msg.EntityId = m_airVent.EntityId;
                msg.IsDepressurizing = newDepressurizationMode;

                Sync.Layer.SendMessageToServerAndSelf(ref msg);
            }

            private static void OnStockipleModeChanged(MySyncAirVent syncObject, ref ChangeDepressurizationModeMsg message, World.MyNetworkClient sender)
            {
                syncObject.m_airVent.SetDepressurizing(message.IsDepressurizing);
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref message, sender.SteamUserId);
                }
            }

            public void SendToolbarItemChanged(ToolbarItem item, int index)
            {
                var msg = new ChangeToolbarItemMsg();
                msg.EntityId = m_airVent.EntityId;

                msg.Item = item;
                msg.Index = index;

                Sync.Layer.SendMessageToServerAndSelf(ref msg);
            }

            private static void OnToolbarItemChanged(MySyncAirVent sync, ref ChangeToolbarItemMsg msg, MyNetworkClient sender)
            {
                MyToolbarItem item = null;
                if (msg.Item.EntityID != 0)
                {
                    if (string.IsNullOrEmpty(msg.Item.GroupName))
                    {
                        MyTerminalBlock block;
                        if (MyEntities.TryGetEntityById<MyTerminalBlock>(msg.Item.EntityID, out block))
                        {
                            var builder = MyToolbarItemFactory.TerminalBlockObjectBuilderFromBlock(block);
                            builder.Action = msg.Item.Action;
                            builder.Parameters = msg.Item.Parameters;
                            item = MyToolbarItemFactory.CreateToolbarItem(builder);
                        }
                    }
                    else
                    {
                        MyAirVent parent;
                        if (MyEntities.TryGetEntityById<MyAirVent>(msg.Item.EntityID, out parent))
                        {
                            var grid = parent.CubeGrid;
                            var groupName = msg.Item.GroupName;
                            var group = grid.GridSystems.TerminalSystem.BlockGroups.Find((x) => x.Name.ToString() == groupName);
                            if (group != null)
                            {
                                var builder = MyToolbarItemFactory.TerminalGroupObjectBuilderFromGroup(group);
                                builder.Action = msg.Item.Action;
                                builder.BlockEntityId = msg.Item.EntityID;
                                builder.Parameters = msg.Item.Parameters;
                                item = MyToolbarItemFactory.CreateToolbarItem(builder);
                            }
                        }
                    }
                }

                if (msg.Index == 0)
                {
                    sync.m_airVent.m_onFullAction = item;
                }
                else
                {
                    sync.m_airVent.m_onEmptyAction = item;
                }
                sync.m_airVent.RaisePropertiesChanged();

                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
                }
            }

        }
        #endregion
    }
}
