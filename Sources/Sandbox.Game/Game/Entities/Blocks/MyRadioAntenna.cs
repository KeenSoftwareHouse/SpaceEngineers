#region Using

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using VRageMath;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.Game.Localization;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using VRage.ModAPI;
using VRage.Game.Gui;
using VRage.Sync;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_RadioAntenna))]
    public class MyRadioAntenna : MyFunctionalBlock, IMyGizmoDrawableObject, IMyRadioAntenna
    {
        protected Color m_gizmoColor;
        protected const float m_maxGizmoDrawDistance = 10000.0f;

        MyRadioBroadcaster RadioBroadcaster
        {
            get { return (MyRadioBroadcaster)Components.Get<MyDataBroadcaster>(); }
            set { Components.Add<MyDataBroadcaster>(value); }
        }
        MyRadioReceiver RadioReceiver
        {
            get { return (MyRadioReceiver)Components.Get<MyDataReceiver>(); }
            set { Components.Add<MyDataReceiver>(value); }
        }

        readonly Sync<float> m_radius;
        private bool onceUpdated = false;
        readonly Sync<bool> m_enableBroadcasting;

        private Sync<bool> m_showShipName;
        public bool ShowShipName
        {
            get
            {
                return m_showShipName;
            }
            set
            {
                m_showShipName.Value = value;
            }
        }

        public Color GetGizmoColor()
        {
            return m_gizmoColor;
        }

        public Vector3 GetPositionInGrid()
        {
            return Position;
        }

        public bool CanBeDrawed()
        {
            if (false == MyCubeGrid.ShowAntennaGizmos || false == this.IsWorking || false == this.HasLocalPlayerAccess() ||
              GetDistanceBetweenCameraAndBoundingSphere() > m_maxGizmoDrawDistance)
            {
                return false;
            }
            return Entities.Cube.MyRadioAntenna.IsRecievedByPlayer(this);
        }

        public BoundingBox? GetBoundingBox()
        {
            return null;
        }

        public float GetRadius()
        {
            return RadioBroadcaster.BroadcastRadius;
        }

        public MatrixD GetWorldMatrix()
        {
            return PositionComp.WorldMatrix;
        }

        public bool EnableLongDrawDistance()
        {
            return true;
        }

        public static bool IsRecievedByPlayer(MyCubeBlock cubeBlock)
        {
            var player = MySession.Static.LocalCharacter;
            if (player == null)
            {
                return false;
            }

            var playerReciever = player.RadioReceiver;
            foreach (var broadcaster in playerReciever.RelayedBroadcasters)
            {
                if (broadcaster.Entity is MyRadioAntenna)
                {
                    MyRadioAntenna antenna = broadcaster.Entity as MyRadioAntenna;
                    var ownerCubeGrid = (broadcaster.Entity as MyCubeBlock).CubeGrid;
                    if(antenna.HasLocalPlayerAccess() && MyCubeGridGroups.Static.Physical.HasSameGroup(ownerCubeGrid, cubeBlock.CubeGrid))
                    {
                        return true;
                    }
                }

            }
            return false;
        }

        public MyRadioAntenna()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_radius = SyncType.CreateAndAddProp<float>();
            m_enableBroadcasting = SyncType.CreateAndAddProp<bool>();
            m_showShipName = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            CreateTerminalControls();

            m_radius.ValueChanged += (obj) => ChangeRadius();
            m_enableBroadcasting.ValueChanged += (obj) => ChangeEnableBroadcast();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyRadioAntenna>())
                return;
            base.CreateTerminalControls();
            MyTerminalControlFactory.RemoveBaseClass<MyRadioAntenna, MyTerminalBlock>();

            var show = new MyTerminalControlOnOffSwitch<MyRadioAntenna>("ShowInTerminal", MySpaceTexts.Terminal_ShowInTerminal, MySpaceTexts.Terminal_ShowInTerminalToolTip);
            show.Getter = (x) => x.ShowInTerminal;
            show.Setter = (x, v) => x.ShowInTerminal = v;
            MyTerminalControlFactory.AddControl(show);

            var showConfig = new MyTerminalControlOnOffSwitch<MyRadioAntenna>("ShowInToolbarConfig", MySpaceTexts.Terminal_ShowInToolbarConfig, MySpaceTexts.Terminal_ShowInToolbarConfigToolTip);
            showConfig.Getter = (x) => x.ShowInToolbarConfig;
            showConfig.Setter = (x, v) => x.ShowInToolbarConfig = v;
            MyTerminalControlFactory.AddControl(showConfig);

            var customName = new MyTerminalControlTextbox<MyRadioAntenna>("CustomName", MyCommonTexts.Name, MySpaceTexts.Blank);
            customName.Getter = (x) => x.CustomName;
            customName.Setter = (x, v) => x.SetCustomName(v);
            customName.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(customName);
            MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyRadioAntenna>());

            var broadcastRadius = new MyTerminalControlSlider<MyRadioAntenna>("Radius", MySpaceTexts.BlockPropertyTitle_BroadcastRadius, MySpaceTexts.BlockPropertyDescription_BroadcastRadius);
            broadcastRadius.SetLogLimits((block) => 1, (block) => block.CubeGrid.GridSizeEnum == MyCubeSize.Large ? MyEnergyConstants.MAX_RADIO_POWER_RANGE : MyEnergyConstants.MAX_SMALL_RADIO_POWER_RANGE);
            broadcastRadius.DefaultValueGetter = (block) => block.CubeGrid.GridSizeEnum == MyCubeSize.Large ? 10000 : 500;
            broadcastRadius.Getter = (x) => x.RadioBroadcaster.BroadcastRadius;
            broadcastRadius.Setter = (x, v) => x.m_radius.Value = v;
            //broadcastRadius.Writer = (x, result) => result.Append(x.RadioBroadcaster.BroadcastRadius < MyEnergyConstants.MAX_RADIO_POWER_RANGE ? new StringBuilder().AppendDecimal(x.RadioBroadcaster.BroadcastRadius, 0).Append(" m") : MyTexts.Get(MySpaceTexts.ScreenTerminal_Infinite));
            broadcastRadius.Writer = (x, result) =>
            {
                result.Append(new StringBuilder().AppendDecimal(x.RadioBroadcaster.BroadcastRadius, 0).Append(" m"));
            };
            broadcastRadius.EnableActions();
            MyTerminalControlFactory.AddControl(broadcastRadius);

            var enableBroadcast = new MyTerminalControlCheckbox<MyRadioAntenna>("EnableBroadCast", MySpaceTexts.Antenna_EnableBroadcast, MySpaceTexts.Antenna_EnableBroadcast);
            enableBroadcast.Getter = (x) => x.RadioBroadcaster.Enabled;
            enableBroadcast.Setter = (x, v) => x.m_enableBroadcasting.Value = v;
            enableBroadcast.EnableAction();
            MyTerminalControlFactory.AddControl(enableBroadcast);

            var showShipName = new MyTerminalControlCheckbox<MyRadioAntenna>("ShowShipName", MySpaceTexts.BlockPropertyTitle_ShowShipName, MySpaceTexts.BlockPropertyDescription_ShowShipName);
            showShipName.Getter = (x) => x.ShowShipName;
            showShipName.Setter = (x, v) => x.ShowShipName = v;
            showShipName.EnableAction();
            MyTerminalControlFactory.AddControl(showShipName);

        }

        void ChangeRadius()
        {
            RadioBroadcaster.BroadcastRadius = m_radius;
        }

        void ChangeEnableBroadcast()
        {
            RadioBroadcaster.Enabled = m_enableBroadcasting;
            RaisePropertiesChanged();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            RadioBroadcaster = new MyRadioBroadcaster();
            RadioReceiver = new MyRadioReceiver();

            var antennaDefinition = BlockDefinition as MyRadioAntennaDefinition;
            Debug.Assert(antennaDefinition != null);

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                antennaDefinition.ResourceSinkGroup,
                MyEnergyConstants.MAX_REQUIRED_POWER_ANTENNA,
                UpdatePowerInput);
            sinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;
            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);

            MyObjectBuilder_RadioAntenna antennaBuilder = (MyObjectBuilder_RadioAntenna)objectBuilder;

            if (antennaBuilder.BroadcastRadius != 0)
            {
                RadioBroadcaster.BroadcastRadius = antennaBuilder.BroadcastRadius;
            }
            else
            {
                RadioBroadcaster.BroadcastRadius = CubeGrid.GridSizeEnum == MyCubeSize.Large ? 10000 : 500;
            }
            ResourceSink.Update();
            RadioBroadcaster.WantsToBeEnabled = antennaBuilder.EnableBroadcasting;

            m_showShipName.Value = antennaBuilder.ShowShipName;

            //if (Sync.IsServer)
            //{
            //    this.IsWorkingChanged += UpdatePirateAntenna;
            //    this.CustomNameChanged += UpdatePirateAntenna;
            //    this.OwnershipChanged += UpdatePirateAntenna;
            //    UpdatePirateAntenna(this);
            //}

            ShowOnHUD = false;

            m_gizmoColor = new Vector4(0.2f, 0.2f, 0.0f, 0.5f);

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            m_enableBroadcasting.Value = RadioBroadcaster.WantsToBeEnabled;
            RadioBroadcaster.OnBroadcastRadiusChanged += OnBroadcastRadiusChanged;

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);

            RadioBroadcaster.OnBroadcastRadiusChanged -= OnBroadcastRadiusChanged;

            SlimBlock.ComponentStack.IsFunctionalChanged -= ComponentStack_IsFunctionalChanged;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            onceUpdated = true;
            if (Sync.IsServer)
            {
                this.IsWorkingChanged += UpdatePirateAntenna;
                this.CustomNameChanged += UpdatePirateAntenna;
                this.OwnershipChanged += UpdatePirateAntenna;
                UpdatePirateAntenna(this);
            }
        }

        protected override void Closing()
        {
            if (Sync.IsServer)
            {
                UpdatePirateAntenna(remove: true);
            }

            base.Closing();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_RadioAntenna objectBuilder = (MyObjectBuilder_RadioAntenna)base.GetObjectBuilderCubeBlock(copy);
            objectBuilder.BroadcastRadius = RadioBroadcaster.BroadcastRadius;
            objectBuilder.ShowShipName = this.ShowShipName;
            objectBuilder.EnableBroadcasting = RadioBroadcaster.Enabled;
            return objectBuilder;
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();
            RadioReceiver.UpdateBroadcastersInRange();
        }

        protected override void WorldPositionChanged(object source)
        {
            base.WorldPositionChanged(source);
            if (RadioBroadcaster != null)
                RadioBroadcaster.MoveBroadcaster();
        }

        public override List<MyHudEntityParams> GetHudParams(bool allowBlink)
        {
            m_hudParams.Clear();

            if (IsWorking)
            {
                var hudParams = base.GetHudParams(allowBlink && this.HasLocalPlayerAccess());

                if (ShowShipName)
                {
                    hudParams[0].Text.Clear();
                    hudParams[0].Text.Append(CubeGrid.DisplayName);
                    hudParams[0].Text.Append(" - ").Append(this.CustomName);
                }

                m_hudParams.AddList(hudParams);

                // add the others if the player is friendly with this antenna
                if (this.HasLocalPlayerAccess())
                {
                    foreach (var terminalBlock in SlimBlock.CubeGrid.GridSystems.TerminalSystem.Blocks)
                    {
                        if (terminalBlock == this)
                            continue;

                        if (terminalBlock.HasLocalPlayerAccess() && (terminalBlock.ShowOnHUD || (terminalBlock.IsBeingHacked && terminalBlock.IDModule.Owner != 0) || (terminalBlock is MyCockpit && (terminalBlock as MyCockpit).Pilot != null)))
                        {
                            m_hudParams.AddList(terminalBlock.GetHudParams(true));
                        }

                        if (terminalBlock.HasLocalPlayerAccess() && terminalBlock.IDModule != null && terminalBlock.IDModule.Owner != 0)
                        {
                            var oreDetectorOwner = terminalBlock as IMyComponentOwner<MyOreDetectorComponent>;
                            if (oreDetectorOwner != null)
                            {
                                MyOreDetectorComponent oreDetector;
                                if (oreDetectorOwner.GetComponent(out oreDetector) && oreDetector.BroadcastUsingAntennas)
                                {
                                    oreDetector.Update(terminalBlock.PositionComp.GetPosition(), false);
                                    oreDetector.SetRelayedRequest = true;
                                }
                            }
                        }
                    }
                }
            }
            return m_hudParams;
        }

        #region Pirates

        void UpdatePirateAntenna(MyCubeBlock obj)
        {
            UpdatePirateAntenna();
        }

        public void UpdatePirateAntenna(bool remove = false)
        {
            bool isActive = IsWorking && Sync.Players.GetNPCIdentities().Contains(OwnerId);
            MyPirateAntennas.UpdatePirateAntenna(this.EntityId, remove, isActive, this.CustomName);
        }

        #endregion

        protected override void OnEnabledChanged()
        {
			ResourceSink.Update();
            if (onceUpdated)
                RadioReceiver.UpdateBroadcastersInRange();
            base.OnEnabledChanged();
        }

        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
        }

        void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();

            if (RadioBroadcaster != null)
            {
                if (IsWorking)
                    RadioBroadcaster.Enabled = m_enableBroadcasting;
                else
                    RadioBroadcaster.Enabled = false;
            }

            if(RadioReceiver != null)
                RadioReceiver.Enabled = IsWorking;
            UpdateText();
        }

        void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
            UpdateText();
        }

        void OnBroadcastRadiusChanged()
        {
			ResourceSink.Update();
            RaisePropertiesChanged();
            UpdateText();
        }

        float UpdatePowerInput()
        {
            float powerPer500m = RadioBroadcaster.BroadcastRadius / 500f;
            UpdateText();
            return (Enabled && IsFunctional) ? powerPer500m * MyEnergyConstants.MAX_REQUIRED_POWER_ANTENNA : 0.0f;
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) ? ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0, DetailedInfo);
            RaisePropertiesChanged();
        }

        float ModAPI.Ingame.IMyRadioAntenna.Radius
        {
            get { return GetRadius(); }
        }

		bool IsBroadcasting()
		{
			return (RadioBroadcaster != null) ? RadioBroadcaster.WantsToBeEnabled : false;
		}

        bool ModAPI.Ingame.IMyRadioAntenna.IsBroadcasting
		{
			get {  return IsBroadcasting(); }
		}
    }
}
