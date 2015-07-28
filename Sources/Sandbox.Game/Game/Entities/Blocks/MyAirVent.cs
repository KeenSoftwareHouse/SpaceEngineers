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
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Import;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_AirVent))]
    class MyAirVent : MyFunctionalBlock, IMyPowerConsumer, IMyOxygenConsumer, IMyOxygenProducer, IMyAirVent, IMyConveyorEndpointBlock
    {
        private static string[] m_emissiveNames = { "Emissive1", "Emissive2", "Emissive3", "Emissive4" };

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

        private bool m_isProducing;
        private bool m_producedSinceLastUpdate;
        private MyParticleEffect m_effect;

        private MyToolbarItem m_onFullAction;
        private MyToolbarItem m_onEmptyAction;
        private MyToolbar m_actionToolbar;

        private bool? m_wasRoomFull;
        private bool? m_wasRoomEmpty;

        public bool CanVent
        {
            get
            {
                return MySession.Static.Settings.EnableOxygen && PowerReceiver.IsPowered && IsWorking && Enabled && IsFunctional;
            }
        }

        private bool m_isDepressurizing;
        public bool IsDepressurizing
        {
            get
            {
                return m_isDepressurizing;
            }
            set
            {
                m_isDepressurizing = value;
            }
        }

        public MyPowerReceiver PowerReceiver
        {
            get;
            protected set;
        }

        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get
            {
                return m_conveyorEndpoint;
            }
        }

        private new MyAirVentDefinition BlockDefinition
        {
            get { return (MyAirVentDefinition)base.BlockDefinition; }
        }

        #region Initialization
        static MyAirVent()
        {
            var isDepressurizing = new MyTerminalControlOnOffSwitch<MyAirVent>("Depressurize", MySpaceTexts.BlockPropertyTitle_Depressurize, MySpaceTexts.BlockPropertyDescription_Depressurize);
            isDepressurizing.Getter = (x) => (x as MyAirVent).IsDepressurizing;
            isDepressurizing.Setter = (x, v) => (x as MyAirVent).SyncObject.ChangeDepressurizationMode(v);
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

            m_isDepressurizing = builder.IsDepressurizing;

            InitializeConveyorEndpoint();
            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Factory,
                false,
                BlockDefinition.OperationalPowerConsumption,
                ComputeRequiredPower);
            PowerReceiver.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
            PowerReceiver.Update();

            m_actionToolbar = new MyToolbar(MyToolbarType.ButtonPanel, 2, 1);
            m_actionToolbar.DrawNumbers = false;
            m_actionToolbar.Init(null, this);

            if (builder.OnFullAction != null)
            {
                m_onFullAction = MyToolbarItemFactory.CreateToolbarItem(builder.OnFullAction);
            }
            if (builder.OnEmptyAction != null)
            {
                m_onEmptyAction = MyToolbarItemFactory.CreateToolbarItem(builder.OnEmptyAction);
            }

            UpdateEmissivity();
            UdpateTexts();

            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_AirVent)base.GetObjectBuilderCubeBlock(copy);

            builder.IsDepressurizing = m_isDepressurizing;
            if (m_onFullAction != null)
            {
                builder.OnFullAction = m_onFullAction.GetObjectBuilder();
            }
            if (m_onEmptyAction != null)
            {
                builder.OnEmptyAction = m_onEmptyAction.GetObjectBuilder();
            }

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
            {
                UpdateSound();
            }

            m_isProducing = m_producedSinceLastUpdate;
            m_producedSinceLastUpdate = false;

            UdpateTexts();
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

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            if (Sync.IsServer && IsWorking && Enabled && IsFunctional)
            {
                UpdateActions();
            }
            UpdateEmissivity();
        }

        private void UpdateActions()
        {   
            float oxygenLevel = GetRoomOxygen();

            if (!m_wasRoomEmpty.HasValue || !m_wasRoomFull.HasValue)
            {
                m_wasRoomEmpty = false;
                m_wasRoomFull = false;

                if (oxygenLevel > 0.99f)
                {
                    m_wasRoomFull = true;
                }
                else if (oxygenLevel < 0.01f)
                {
                    m_wasRoomEmpty = true;
                }
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
            if (!MySession.Static.Settings.EnableOxygen)
            {
                return 0f;
            }

            return (Enabled && IsFunctional) ? (m_isProducing) ? BlockDefinition.OperationalPowerConsumption
                                                             : BlockDefinition.StandbyPowerConsumption
                                             : 0.0f;
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
            if (CanVent)
            {
                var oxygenBlock = GetOxygenBlock();
                if (oxygenBlock.Room != null && oxygenBlock.Room.IsPressurized)
                {
                    if (m_isDepressurizing)
                    {
                        SetEmissive(Color.Teal, oxygenBlock.OxygenLevel(CubeGrid.GridSize));
                    }
                    else
                    {
                        SetEmissive(Color.Green, oxygenBlock.OxygenLevel(CubeGrid.GridSize));
                    }
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

        void UdpateTexts()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.MaxRequiredInput, DetailedInfo);
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
                    if (i <= fillCount)
                    {
                        VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[i], null, color, null, null, 0);
                    }
                    else
                    {
                        VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[i], null, Color.Black, null, null, 0);
                    }
                }
                m_prevColor = color;
                m_prevFillCount = fillCount;
            }
        }

        public override void OnModelChange()
        {
            base.OnModelChange();

            m_prevFillCount = -1;
        }

        protected override void Closing()
        {
            base.Closing();

            if (m_effect != null)
            {
                m_effect.Stop();
            }
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

            return CubeGrid.GridSystems.OxygenSystem.GetOxygenBlock(worldMatrix.Translation);
        }

        bool IMyOxygenBlock.IsWorking()
        {
            return CanVent;
        }

        int IMyOxygenConsumer.GetPriority()
        {
            return 0;
        }

        float IMyOxygenConsumer.ConsumptionNeed(float deltaTime)
        {
            if (IsDepressurizing || !CanVent)
            {
                return 0f;
            }

            var oxygenBlock = GetOxygenBlock();
            if (oxygenBlock.Room == null || !oxygenBlock.Room.IsPressurized)
            {
                return 0f;
            }

            float neededOxygen = (float)oxygenBlock.Room.MissingOxygen(CubeGrid.GridSize);

            if (neededOxygen <= 0f)
            {
                neededOxygen = 0f;
            }

            return Math.Min(neededOxygen, BlockDefinition.VentilationCapacityPerSecond * deltaTime);
        }

        void IMyOxygenConsumer.Consume(float amount)
        {
            if (amount == 0f || IsDepressurizing)
            {
                return;
            }

            Debug.Assert(CanVent, "Vent asked to vent when it is unable to do so");
            Debug.Assert(!IsDepressurizing, "Vent asked to vent when it is supposed to depressurize");
            Debug.Assert(amount >= 0f);

            var oxygenBlock = GetOxygenBlock();
            if (oxygenBlock.Room == null || !oxygenBlock.Room.IsPressurized)
            {
                return;
            }

            oxygenBlock.Room.OxygenAmount += amount;
            if (oxygenBlock.Room.OxygenLevel(CubeGrid.GridSize) > 1f)
            {
                oxygenBlock.Room.OxygenAmount = oxygenBlock.Room.MaxOxygen(CubeGrid.GridSize);
            }

            if (amount > 0)
            {
                m_producedSinceLastUpdate = true;
            }
        }

        int IMyOxygenProducer.GetPriority()
        {
            return 0;
        }

        float IMyOxygenProducer.ProductionCapacity(float deltaTime)
        {
            if (!IsDepressurizing || !CanVent)
            {
                return 0f;
            }

            var oxygenBlock = GetOxygenBlock();
            if (oxygenBlock.Room == null)
            {
                return 0f;
            }

            if (oxygenBlock.Room.IsPressurized)
            {
                float oxygenLeft = (float)oxygenBlock.Room.OxygenAmount;

                if (oxygenLeft <= 0f)
                {
                    oxygenLeft = 0f;
                }

                return Math.Min(oxygenLeft, BlockDefinition.VentilationCapacityPerSecond * deltaTime);
            }
            else
            {
                return BlockDefinition.VentilationCapacityPerSecond * MyOxygenProviderSystem.GetOxygenInPoint(WorldMatrix.Translation) * deltaTime;
            }
        }

        void IMyOxygenProducer.Produce(float amount)
        {
            if (amount == 0f || !IsDepressurizing)
            {
                return;
            }
            Debug.Assert(CanVent, "Vent asked to depressurize when it is unable to do so");
            Debug.Assert(IsDepressurizing, "Vent asked to depressurize when it is supposed to pressurize");
            Debug.Assert(amount >= 0f);

            var oxygenBlock = GetOxygenBlock();
            if (oxygenBlock.Room == null)
            {
                return;
            }

            if (oxygenBlock.Room.IsPressurized)
            {
                oxygenBlock.Room.OxygenAmount -= amount;
                if (oxygenBlock.Room.OxygenAmount < 0f)
                {
                    oxygenBlock.Room.OxygenAmount = 0f;
                }

                if (amount > 0)
                {
                    m_producedSinceLastUpdate = true;
                }
            }
            else
            {
                //Take from environment, nothing to do
                Debug.Assert(MyOxygenProviderSystem.GetOxygenInPoint(WorldMatrix.Translation) > 0f);
                m_producedSinceLastUpdate = true;
            }
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
                {
                    return false;
                }

                return oxygenBlock.Room.IsPressurized;
            }
        }
        public float GetOxygenLevel()
        {
            var oxygenBlock = GetOxygenBlock();
            if (oxygenBlock.Room == null)
            {
                return 0f;
            }

            return oxygenBlock.OxygenLevel(CubeGrid.GridSize);
        }

        #region Sync
        protected override MySyncEntity OnCreateSync()
        {
            return new MySyncAirVent(this);
        }

        internal new MySyncAirVent SyncObject
        {
            get
            {
                return (MySyncAirVent)base.SyncObject;
            }
        }

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

            private MyAirVent m_airVent;

            static MySyncAirVent()
            {
                MySyncLayer.RegisterEntityMessage<MySyncAirVent, ChangeDepressurizationModeMsg>(OnStockipleModeChanged, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncAirVent, ChangeToolbarItemMsg>(OnToolbarItemChanged, MyMessagePermissions.Any);
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

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            private static void OnStockipleModeChanged(MySyncAirVent syncObject, ref ChangeDepressurizationModeMsg message, World.MyNetworkClient sender)
            {
                syncObject.m_airVent.IsDepressurizing = message.IsDepressurizing;
            }

            public void SendToolbarItemChanged(ToolbarItem item, int index)
            {
                var msg = new ChangeToolbarItemMsg();
                msg.EntityId = m_airVent.EntityId;

                msg.Item = item;
                msg.Index = index;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
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
            }

        }
        #endregion
    }
}
