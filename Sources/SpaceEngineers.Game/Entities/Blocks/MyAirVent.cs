using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Import;
using VRage.ModAPI;
using VRage.Network;
using VRage.Sync;
using VRage.Utils;
using VRageMath;
using VRageRender.Import;

namespace SpaceEngineers.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_AirVent))]
    public class MyAirVent : MyFunctionalBlock, IMyAirVent, IMyGasBlock
    {
        private static readonly string[] m_emissiveNames = { "Emissive0", "Emissive1", "Emissive2", "Emissive3" };

        MyModelDummy VentDummy
        {
            get
            {
                if (Model == null || Model.Dummies == null)
                    return null;
                MyModelDummy dummy;
                Model.Dummies.TryGetValue("vent_001", out dummy);
                return dummy;
            }
        }

        private Color m_prevColor = Color.White;
        private int m_prevFillCount = -1;

        private int m_lastOutputUpdateTime;
        private int m_lastInputUpdateTime;
        private float m_nextGasTransfer;

        private int m_productionUpdateInterval = 100;

        private bool m_isProducing;
        private bool m_producedSinceLastUpdate;
        private bool m_playVentEffect;
        private MyParticleEffect m_effect;

        private MyToolbarItem m_onFullAction;
        private MyToolbarItem m_onEmptyAction;
        private MyToolbar m_actionToolbar;

        private bool? m_wasRoomFull;
        private bool? m_wasRoomEmpty;
        private readonly MyDefinitionId m_oxygenGasId = new MyDefinitionId(typeof (MyObjectBuilder_GasProperties), "Oxygen");

        public bool CanVent { get { return (MySession.Static.Settings.EnableOxygen && MySession.Static.Settings.EnableOxygenPressurization) && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && IsWorking; } }
        public bool CanVentToRoom { get { return CanVent && !IsDepressurizing; } }
        public bool CanVentFromRoom { get { return CanVent && IsDepressurizing; } }

        private float GasOutputPerSecond { get { return (SourceComp.ProductionEnabledByType(m_oxygenGasId) ? SourceComp.CurrentOutputByType(m_oxygenGasId) : 0f); } }
        private float GasInputPerSecond { get { return ResourceSink.CurrentInputByType(m_oxygenGasId); } }
        private float GasOutputPerUpdate { get { return GasOutputPerSecond * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS; } }
        private float GasInputPerUpdate { get { return GasInputPerSecond * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS; } }

        private readonly Sync<bool> m_isDepressurizing;
        public bool IsDepressurizing { get { return m_isDepressurizing; } set { m_isDepressurizing.Value = value; } }

        private MyResourceSourceComponent m_sourceComp;
        public MyResourceSourceComponent SourceComp
        {
            get { return m_sourceComp; }
            set { if (Components.Contains(typeof(MyResourceSourceComponent))) Components.Remove<MyResourceSourceComponent>(); Components.Add<MyResourceSourceComponent>(value); m_sourceComp = value; }
        }

        MyResourceSinkInfo OxygenSinkInfo;

        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint { get { return m_conveyorEndpoint; } }

        private new MyAirVentDefinition BlockDefinition { get { return (MyAirVentDefinition)base.BlockDefinition; } }

        bool m_syncing = false;

        #region Initialization

        public MyAirVent()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_isDepressurizing = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            CreateTerminalControls();

            ResourceSink = new MyResourceSinkComponent(2);
            SourceComp = new MyResourceSourceComponent();
            m_isDepressurizing.ValueChanged += (x) => SetDepressurizing();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyAirVent>())
                return;
            base.CreateTerminalControls();
            var isDepressurizing = new MyTerminalControlOnOffSwitch<MyAirVent>("Depressurize", MySpaceTexts.BlockPropertyTitle_Depressurize, MySpaceTexts.BlockPropertyDescription_Depressurize);
            isDepressurizing.Getter = (x) => x.IsDepressurizing;
            isDepressurizing.Setter = (x, v) => { x.IsDepressurizing = v; x.UpdateEmissivity(); };
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

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            base.Init(objectBuilder, cubeGrid);

            var builder = (MyObjectBuilder_AirVent)objectBuilder;

            InitializeConveyorEndpoint();
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

            SourceComp.Init(BlockDefinition.ResourceSourceGroup, new MyResourceSourceInfo { ResourceTypeId = m_oxygenGasId, DefinedOutput = BlockDefinition.VentilationCapacityPerSecond, ProductionToCapacityMultiplier = 1 });
            SourceComp.OutputChanged += Source_OutputChanged;
            FillSinkInfo();
            var sinkDataList = new List<MyResourceSinkInfo>
	        {
				new MyResourceSinkInfo {ResourceTypeId = MyResourceDistributorComponent.ElectricityId, MaxRequiredInput = BlockDefinition.OperationalPowerConsumption, RequiredInputFunc = ComputeRequiredPower},
	        };

            ResourceSink.Init(
                BlockDefinition.ResourceSinkGroup,
                sinkDataList);
			ResourceSink.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
            ResourceSink.CurrentInputChanged += Sink_CurrentInputChanged;

            m_lastOutputUpdateTime = MySession.Static.GameplayFrameCounter;
            m_lastInputUpdateTime = MySession.Static.GameplayFrameCounter;
            m_nextGasTransfer = 0f;

            m_actionToolbar = new MyToolbar(MyToolbarType.ButtonPanel, 2, 1);
            m_actionToolbar.DrawNumbers = false;
            m_actionToolbar.Init(null, this);

            if (builder.OnFullAction != null)
                m_onFullAction = MyToolbarItemFactory.CreateToolbarItem(builder.OnFullAction);

            if (builder.OnEmptyAction != null)
                m_onEmptyAction = MyToolbarItemFactory.CreateToolbarItem(builder.OnEmptyAction);

            UpdateEmissivity();
            UpdateTexts();

            AddDebugRenderComponent(new Sandbox.Game.Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            IsWorkingChanged += MyAirVent_IsWorkingChanged;

            m_isDepressurizing.Value =  builder.IsDepressurizing;

            SetDepressurizing();
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

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            if (Sync.IsServer && IsWorking)
                UpdateActions();

            if (m_playVentEffect == false)
            {
                if (m_effect != null)
                {
                    m_effect.Stop();
                    m_effect = null;
                }
            }

            m_isProducing = m_producedSinceLastUpdate;
            m_producedSinceLastUpdate = false;
            m_playVentEffect = false;
            var block = GetOxygenBlock();

            int sourceUpdateFrames = (MySession.Static.GameplayFrameCounter - m_lastOutputUpdateTime);
            int sinkUpdateFrames = (MySession.Static.GameplayFrameCounter - m_lastInputUpdateTime);
            m_lastOutputUpdateTime = MySession.Static.GameplayFrameCounter;
            m_lastInputUpdateTime = MySession.Static.GameplayFrameCounter;

            float gasInput = GasInputPerUpdate * sinkUpdateFrames;
            float gasOutput = GasOutputPerUpdate * sourceUpdateFrames;
            float totalTransfer = gasInput - gasOutput + m_nextGasTransfer;
            if (totalTransfer != 0f)
                Transfer(totalTransfer);

            SourceComp.SetRemainingCapacityByType(m_oxygenGasId, (float)(block.Room != null && block.Room.IsPressurized ? block.Room.OxygenAmount : (MyOxygenProviderSystem.GetOxygenInPoint(WorldMatrix.Translation) != 0 ? BlockDefinition.VentilationCapacityPerSecond * 100 : 0f)));

            ResourceSink.Update();

            UpdateTexts();
            UpdateEmissivity();

            if (MyFakes.ENABLE_OXYGEN_SOUNDS)
                UpdateSound();
        }

        private void Source_OutputChanged(MyDefinitionId changedResourceId, float oldOutput, MyResourceSourceComponent source)
        {
            float timeSinceLastUpdateSeconds = (MySession.Static.GameplayFrameCounter - m_lastOutputUpdateTime) / 1000f * VRage.Game.MyEngineConstants.UPDATE_STEPS_PER_SECOND;
            m_lastOutputUpdateTime = MySession.Static.GameplayFrameCounter;
            float outputAmount = oldOutput * timeSinceLastUpdateSeconds;
            m_nextGasTransfer -= outputAmount;
        }

        private void Sink_CurrentInputChanged(MyDefinitionId resourceTypeId, float oldInput, MyResourceSinkComponent sink)
        {
            if (resourceTypeId != m_oxygenGasId)
                return;

            float timeSinceLastUpdateSeconds = (MySession.Static.GameplayFrameCounter - m_lastInputUpdateTime) / 1000f * VRage.Game.MyEngineConstants.UPDATE_STEPS_PER_SECOND;
            m_lastInputUpdateTime = MySession.Static.GameplayFrameCounter;
            float inputAmount = oldInput * timeSinceLastUpdateSeconds;
            m_nextGasTransfer += inputAmount;
        }

        private void Transfer(float transferAmount)
        {
            if (transferAmount > 0)
                VentToRoom(transferAmount);
            else if (transferAmount < 0)
                DrainFromRoom(-transferAmount);
        }

        private void UpdateSound()
        {
            if (m_soundEmitter == null)
                return;
            if (IsWorking)
            {
                if (m_playVentEffect)
                {
                    if (IsDepressurizing)
                    {
                        if (m_soundEmitter.IsPlaying == false || m_soundEmitter.SoundPair.Equals(BlockDefinition.DepressurizeSound) == false)
                        {
                            m_soundEmitter.PlaySound(BlockDefinition.DepressurizeSound, true);
                        }
                    }
                    else if (m_soundEmitter.IsPlaying == false || m_soundEmitter.SoundPair.Equals(BlockDefinition.PressurizeSound) == false)
                    {
                        m_soundEmitter.PlaySound(BlockDefinition.PressurizeSound, true);
                    }
                }
                else if (m_soundEmitter.IsPlaying == false || m_soundEmitter.SoundPair.Equals(BlockDefinition.IdleSound) == false)
                {
                    if (m_soundEmitter.IsPlaying && (m_soundEmitter.SoundPair.Equals(BlockDefinition.PressurizeSound) || m_soundEmitter.SoundPair.Equals(BlockDefinition.DepressurizeSound)))
                        m_soundEmitter.StopSound(false);
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
                m_wasRoomEmpty = false;
                if (!m_wasRoomFull.Value)
                {
                    ExecuteAction(m_onFullAction);
                    m_wasRoomFull = true;
                }
            }
            else if (oxygenLevel < 0.01f)
            {
                m_wasRoomFull = false;
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
            if (m_syncing)
            {
                return;
            }
            MyMultiplayer.RaiseEvent(this,x => x.SendToolbarItemChanged,ToolbarItem.FromItem(self.GetItemAtIndex(index.ItemIndex)), index.ItemIndex);
        }

        private float ComputeRequiredPower()
        {
            if (!MySession.Static.Settings.EnableOxygen && Enabled && IsFunctional && !MySession.Static.Settings.EnableOxygenPressurization)
                return 0f;

            return m_isProducing ? BlockDefinition.OperationalPowerConsumption : BlockDefinition.StandbyPowerConsumption;
        }

        private float Sink_ComputeRequiredGas()
        {
            if (!CanVentToRoom)
                return 0f;

            var oxygenBlock = GetOxygenBlock();
            if (oxygenBlock.Room == null || !oxygenBlock.Room.IsPressurized)
                return 0f;

            float missingOxygen = (float)oxygenBlock.Room.MissingOxygen(CubeGrid.GridSize);

            float inputToFillInUpdateInterval = missingOxygen * VRage.Game.MyEngineConstants.UPDATE_STEPS_PER_SECOND / m_productionUpdateInterval * SourceComp.ProductionToCapacityMultiplierByType(m_oxygenGasId);

            return Math.Min(inputToFillInUpdateInterval, BlockDefinition.VentilationCapacityPerSecond);
        }

        void PowerReceiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        void ComponentStack_IsFunctionalChanged()
        {
            SourceComp.Enabled = IsWorking;
            ResourceSink.Update();
            CubeGrid.GridSystems.ResourceDistributor.ConveyorSystem_OnPoweredChanged(); // Hotfix TODO
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
            if (CanVentToRoom || IsDepressurizing)
            {
                var oxygenBlock = GetOxygenBlock();
                if (oxygenBlock.Room != null && oxygenBlock.Room.IsPressurized)
                {
                    SetEmissive(m_isDepressurizing ? Color.Teal : Color.Green, oxygenBlock.OxygenLevel(CubeGrid.GridSize));
                }
                else
                {
                    if (oxygenBlock.Room != null && !oxygenBlock.Room.IsPressurized)
                    {
                        float oxygenLevel = oxygenBlock.OxygenLevel(CubeGrid.GridSize);
                        oxygenLevel = Math.Max(oxygenLevel, oxygenBlock.Room.EnvironmentOxygen);
                        SetEmissive(m_isDepressurizing ? Color.Teal: Color.Yellow, oxygenLevel);
                    }
                    else
                    {
                        float oxygenLevel = MyOxygenProviderSystem.GetOxygenInPoint(WorldMatrix.Translation);
                        SetEmissive(oxygenLevel == 0f ? Color.Yellow : m_isDepressurizing ? Color.Teal : Color.Green, oxygenLevel);
                    }
                }
            }
            else
            {
                SetEmissive(Color.Red, 1f);
            }
        }

        private void SetDepressurizing()
        {
            if (m_isDepressurizing)
            {
                var tmpGasId = m_oxygenGasId;
                ResourceSink.RemoveType(ref tmpGasId);
            }
            else
                ResourceSink.AddType(ref OxygenSinkInfo);

            SourceComp.SetProductionEnabledByType(m_oxygenGasId, m_isDepressurizing);
            ResourceSink.Update();
        }

        void UpdateTexts()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId), DetailedInfo);
            DetailedInfo.Append("\n");

            if (!MySession.Static.Settings.EnableOxygen || !MySession.Static.Settings.EnableOxygenPressurization)
            {
                DetailedInfo.Append("Oxygen disabled in world settings!");
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
            RaisePropertiesChanged();
        }

        private void SetEmissive(Color color, float fill)
        {
            int fillCount = (int)(fill * m_emissiveNames.Length);

            if (Render.RenderObjectIDs[0] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED && (color != m_prevColor || fillCount != m_prevFillCount))
            {
                for (int i = 0; i < m_emissiveNames.Length; i++)
                {
                    UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], m_emissiveNames[i], i <= fillCount ? color : Color.Black, 1.0f);
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

            if (m_soundEmitter != null)
                m_soundEmitter.StopSound(true);
        }
        #endregion

        #region Venting
        private MyOxygenBlock GetOxygenBlock()
        {
            if (!MySession.Static.Settings.EnableOxygen || !MySession.Static.Settings.EnableOxygenPressurization || VentDummy == null)
            {
                return new MyOxygenBlock();
            }

            MatrixD dummyLocal = MatrixD.Normalize(VentDummy.Matrix);
            MatrixD worldMatrix = MatrixD.Multiply(dummyLocal, WorldMatrix);

            return CubeGrid.GridSystems.GasSystem.GetOxygenBlock(worldMatrix.Translation);
        }

        bool IMyGasBlock.IsWorking()
        {
            return CanVentToRoom;
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

            if (amount > 0)//some oxygen were vented
            {
                m_producedSinceLastUpdate = true;
                if (amount > 1)//enough oxygen were vented for sound and effects
                {
                    m_playVentEffect = true;
                    if (m_effect == null)//create air vent effect
                        CreateEffect();
                }
            }
        }

        void DrainFromRoom(float amount)
        {
            if (amount == 0f || !IsDepressurizing)
                return;

            Debug.Assert(IsDepressurizing, "Vent asked to depressurize when it is supposed to pressurize");
            Debug.Assert(amount >= 0f);

            var oxygenBlock = GetOxygenBlock();
            float oxygenInRoom = oxygenBlock.Room == null ? 0f : (float)oxygenBlock.Room.OxygenAmount;
            if (oxygenBlock.Room == null)
                return;

            if (oxygenBlock.Room.IsPressurized)
            {
                SourceComp.SetRemainingCapacityByType(m_oxygenGasId, oxygenInRoom);
                oxygenBlock.Room.OxygenAmount -= amount;
                if (oxygenBlock.Room.OxygenAmount < 0f)
                {
                    oxygenBlock.Room.OxygenAmount = 0f;
                }

                
            }
            //Take from environment
            else
            {
                float oxygenInEnvironment = MyOxygenProviderSystem.GetOxygenInPoint(WorldMatrix.Translation) != 0 ? BlockDefinition.VentilationCapacityPerSecond * 100 : 0f;
                SourceComp.SetRemainingCapacityByType(m_oxygenGasId, oxygenInEnvironment);
                m_producedSinceLastUpdate = true;
            }
            if (amount > 0)//some oxygen were vented
            {
                m_producedSinceLastUpdate = true;
                if (amount > 1)//enough oxygen were vented for sound and effects
                {
                    m_playVentEffect = true;
                    if (m_effect == null)//create air vent effect
                        CreateEffect();
                }
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

        private void FillSinkInfo()
        {
            OxygenSinkInfo = new MyResourceSinkInfo { ResourceTypeId = m_oxygenGasId, MaxRequiredInput = BlockDefinition.VentilationCapacityPerSecond, RequiredInputFunc = Sink_ComputeRequiredGas }; 
        }

        private void FillSourceInfo()
        {

        }


        [Event, Reliable, Server, BroadcastExcept]
        void SendToolbarItemChanged(ToolbarItem sentItem, int index)
        {
            m_syncing = true;
            MyToolbarItem item = null;
            if (sentItem.EntityID != 0)
            {
                if (string.IsNullOrEmpty(sentItem.GroupName))
                {
                    MyTerminalBlock block;
                    if (MyEntities.TryGetEntityById<MyTerminalBlock>(sentItem.EntityID, out block))
                    {
                        var builder = MyToolbarItemFactory.TerminalBlockObjectBuilderFromBlock(block);
                        builder._Action = sentItem.Action;
                        builder.Parameters = sentItem.Parameters;
                        item = MyToolbarItemFactory.CreateToolbarItem(builder);
                    }
                }
                else
                {

                    var grid = CubeGrid;
                    var groupName = sentItem.GroupName;
                    var group = grid.GridSystems.TerminalSystem.BlockGroups.Find((x) => x.Name.ToString() == groupName);
                    if (group != null)
                    {
                        var builder = MyToolbarItemFactory.TerminalGroupObjectBuilderFromGroup(group);
                        builder._Action = sentItem.Action;
                        builder.BlockEntityId = sentItem.EntityID;
                        builder.Parameters = sentItem.Parameters;
                        item = MyToolbarItemFactory.CreateToolbarItem(builder);
                    }
                }
            }

            if (index == 0)
            {
                m_onFullAction = item;
            }
            else
            {
                m_onEmptyAction = item;
            }
            RaisePropertiesChanged();
            m_syncing = false;
        }

        #region IMyConveyorEndpointBlock implementation

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPullInformation()
        {
            return null;
        }

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPushInformation()
        {
            return null;
        }

        #endregion
    }
}
