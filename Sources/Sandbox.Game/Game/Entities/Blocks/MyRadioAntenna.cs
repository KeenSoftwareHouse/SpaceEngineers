#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Graphics.GUI;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;

using Sandbox.Game.Components;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Localization;
using VRage;
using VRage.Utils;

using Sandbox.Game.Entities.Blocks;
#endregion

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_RadioAntenna))]
    class MyRadioAntenna : MyFunctionalBlock, IMyPowerConsumer, IMyComponentOwner<MyDataBroadcaster>, IMyComponentOwner<MyDataReceiver>, IMyGizmoDrawableObject, IMyRadioAntenna
    {
        protected Color m_gizmoColor = new Vector4(0.1f, 0.1f, 0.0f, 0.1f);
        protected const float m_maxGizmoDrawDistance = 10000.0f;

        MyRadioBroadcaster m_radioBroadcaster;
        MyRadioReceiver m_radioReceiver;
        private bool m_showShipName;
        public bool ShowShipName
        {
            get
            {
                return m_showShipName;
            }
            set
            {
                if (m_showShipName != value)
                {
                    m_showShipName = value;
                    RaisePropertiesChanged();
                }
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
            return m_radioBroadcaster.BroadcastRadius;
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
            var player = MySession.LocalCharacter;
            if (player == null)
            {
                return false;
            }

            var playerReciever = player.RadioReceiver;
            foreach (var broadcaster in playerReciever.RelayedBroadcasters)
            {
                if (broadcaster.Parent is MyRadioAntenna)
                {
                    MyRadioAntenna antenna = broadcaster.Parent as MyRadioAntenna;
                    var ownerCubeGrid = (broadcaster.Parent as MyCubeBlock).CubeGrid;
                    if(antenna.HasLocalPlayerAccess() && MyCubeGridGroups.Static.Physical.HasSameGroup(ownerCubeGrid, cubeBlock.CubeGrid))
                    {
                        return true;
                    }
                }

            }
            return false;
        }

        static MyRadioAntenna()
        {
            MyTerminalControlFactory.RemoveBaseClass<MyRadioAntenna, MyTerminalBlock>();

            var show = new MyTerminalControlOnOffSwitch<MyRadioAntenna>("ShowInTerminal", MySpaceTexts.Terminal_ShowInTerminal, MySpaceTexts.Terminal_ShowInTerminalToolTip);
            show.Getter = (x) => x.ShowInTerminal;
            show.Setter = (x, v) => x.RequestShowInTerminal(v);
            MyTerminalControlFactory.AddControl(show);

            var customName = new MyTerminalControlTextbox<MyRadioAntenna>("CustomName", MySpaceTexts.Name, MySpaceTexts.Blank);
            customName.Getter = (x) => x.CustomName;
            customName.Setter = (x, v) => MySyncBlockHelpers.SendChangeNameRequest(x, v);
            customName.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(customName);
            MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyRadioAntenna>());

            var broadcastRadius = new MyTerminalControlSlider<MyRadioAntenna>("Radius", MySpaceTexts.BlockPropertyTitle_BroadcastRadius, MySpaceTexts.BlockPropertyDescription_BroadcastRadius);
            broadcastRadius.SetLogLimits((block) => 1, (block) => block.CubeGrid.GridSizeEnum == MyCubeSize.Large ? MyEnergyConstants.MAX_RADIO_POWER_RANGE : MyEnergyConstants.MAX_SMALL_RADIO_POWER_RANGE);
            broadcastRadius.DefaultValueGetter = (block) => block.CubeGrid.GridSizeEnum == MyCubeSize.Large ? 10000 : 500;
            broadcastRadius.Getter = (x) => x.RadioBroadcaster.BroadcastRadius;
            broadcastRadius.Setter = (x, v) => x.RadioBroadcaster.SyncObject.SendChangeRadioAntennaRequest(v, x.RadioBroadcaster.Enabled);
            //broadcastRadius.Writer = (x, result) => result.Append(x.RadioBroadcaster.BroadcastRadius < MyEnergyConstants.MAX_RADIO_POWER_RANGE ? new StringBuilder().AppendDecimal(x.RadioBroadcaster.BroadcastRadius, 0).Append(" m") : MyTexts.Get(MySpaceTexts.ScreenTerminal_Infinite));
            broadcastRadius.Writer = (x, result) =>
            {
                result.Append(new StringBuilder().AppendDecimal(x.RadioBroadcaster.BroadcastRadius, 0).Append(" m"));
            };
            broadcastRadius.EnableActions();
            MyTerminalControlFactory.AddControl(broadcastRadius);

            var enableBroadcast = new MyTerminalControlCheckbox<MyRadioAntenna>("EnableBroadCast", MySpaceTexts.Antenna_EnableBroadcast, MySpaceTexts.Antenna_EnableBroadcast);
            enableBroadcast.Getter = (x) => x.RadioBroadcaster.Enabled;
            enableBroadcast.Setter = (x, v) => x.RadioBroadcaster.SyncObject.SendChangeRadioAntennaRequest(x.RadioBroadcaster.BroadcastRadius, v);
            enableBroadcast.EnableAction();
            MyTerminalControlFactory.AddControl(enableBroadcast);

            var showShipName = new MyTerminalControlCheckbox<MyRadioAntenna>("ShowShipName", MySpaceTexts.BlockPropertyTitle_ShowShipName, MySpaceTexts.BlockPropertyDescription_ShowShipName);
            showShipName.Getter = (x) => x.ShowShipName;
            showShipName.Setter = (x, v) => x.RadioBroadcaster.SyncObject.SendChangeRadioAntennaDisplayName(v);
            showShipName.EnableAction();
            MyTerminalControlFactory.AddControl(showShipName);

            var enableListen = new MyTerminalControlCheckbox<MyRadioAntenna>("EnableListen", MySpaceTexts.Antenna_EnableListen, MySpaceTexts.Antenna_EnableListen);
            enableListen.Getter = (x) => x.Listen;
            enableListen.Setter = (x, v) => x.Listen = v;
            MyTerminalControlFactory.AddControl(enableListen);

            var showReceivedMessages = new MyTerminalControlButton<MyRadioAntenna>("ShowReceivedMessages", MySpaceTexts.Antenna_DisplayReceivedMessages, MySpaceTexts.Antenna_DisplayReceivedMessagesTooltipp, (b) => b.OpenReceivedMessagesDisplay() );
            MyTerminalControlFactory.AddControl(showReceivedMessages);
        }

        public MyRadioAntenna()
        {
            //((this as MyEntity).Position = 
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            m_radioBroadcaster = new MyRadioBroadcaster(this);
            MyObjectBuilder_RadioAntenna antennaBuilder = (MyObjectBuilder_RadioAntenna)objectBuilder;

            if (antennaBuilder.BroadcastRadius != 0)
            {
                m_radioBroadcaster.BroadcastRadius = antennaBuilder.BroadcastRadius;
            }
            else
            {
                m_radioBroadcaster.BroadcastRadius = CubeGrid.GridSizeEnum == MyCubeSize.Large ? 10000 : 500;
            }
            RadioBroadcaster.WantsToBeEnabled = antennaBuilder.EnableBroadcasting;

            m_showShipName = antennaBuilder.ShowShipName;
            m_radioReceiver = new MyRadioReceiver(this);

            m_radioBroadcaster.OnBroadcastRadiusChanged += OnBroadcastRadiusChanged;

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Utility,
                false,
                MyEnergyConstants.MAX_REQUIRED_POWER_ANTENNA,
                UpdatePowerInput);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            PowerReceiver.Update();

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            ShowOnHUD = false;

            NeedsUpdate = Common.MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_RadioAntenna objectBuilder = (MyObjectBuilder_RadioAntenna)base.GetObjectBuilderCubeBlock(copy);
            objectBuilder.BroadcastRadius = m_radioBroadcaster.BroadcastRadius;
            objectBuilder.ShowShipName = this.ShowShipName;
            objectBuilder.EnableBroadcasting = m_radioBroadcaster.WantsToBeEnabled;
            return objectBuilder;
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();
            m_radioReceiver.UpdateBroadcastersInRange();
        }

        protected override void WorldPositionChanged(object source)
        {
            base.WorldPositionChanged(source);
            if (m_radioBroadcaster != null)
                m_radioBroadcaster.MoveBroadcaster();
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

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        protected override void OnEnabledChanged()
        {
            PowerReceiver.Update();
            m_radioReceiver.UpdateBroadcastersInRange();
            base.OnEnabledChanged();
        }

        protected override bool CheckIsWorking()
        {
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            if (IsWorking)
                m_radioBroadcaster.Enabled = m_radioBroadcaster.WantsToBeEnabled;
            else
                m_radioBroadcaster.Enabled = false;

            m_radioReceiver.Enabled = IsWorking;
            UpdateText();
        }

        void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
            UpdateText();
        }

        public MyRadioBroadcaster RadioBroadcaster
        {
            get { return m_radioBroadcaster; }
        }

        public MyRadioReceiver RadioReceiver
        {
            get { return m_radioReceiver; }
        }

        bool IMyComponentOwner<MyDataBroadcaster>.GetComponent(out MyDataBroadcaster component)
        {
            component = m_radioBroadcaster;
            return m_radioBroadcaster != null;
        }

        void OnBroadcastRadiusChanged()
        {
            PowerReceiver.Update();
            RaisePropertiesChanged();
            UpdateText();
        }

        float UpdatePowerInput()
        {
            float powerPer500m = m_radioBroadcaster.BroadcastRadius / 500f;
            UpdateText();
            return (Enabled && IsFunctional) ? powerPer500m * MyEnergyConstants.MAX_REQUIRED_POWER_ANTENNA : 0.0f;
        }

        bool IMyComponentOwner<MyDataReceiver>.GetComponent(out MyDataReceiver component)
        {
            component = m_radioReceiver;
            return m_radioReceiver != null;
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.IsPowered ? PowerReceiver.RequiredInput : 0, DetailedInfo);

            RaisePropertiesChanged();
        }

        float IMyRadioAntenna.Radius
        {
            get { return GetRadius(); }
        }

        //mine
        private int PushMessageToStorage(string shipsender, string shipreceiver, string pbreceiver, string message)
        {
            //localisation! Magic 12 has to be adapted also!
            StringBuilder text = new StringBuilder(shipsender);
            text.Append(MyTexts.Get(MySpaceTexts.Antenna_MsgPart_to)); // " to "
            text.Append(pbreceiver);
            text.Append(MyTexts.Get(MySpaceTexts.Antenna_MsgPart_on)); // " on "
            text.Append(shipreceiver);
            text.Append(": "); //I think this is in all languages the same...
            text.Append(message);
            text.Append("\n");

            return MyRadioAntennaMessages.Add(text);
        }

        public void ReceiveMessage(int hash)
        {
            if (m_messageHashs == null)
                m_messageHashs = new List<int>();

            m_messageHashs.Add(hash);
        }

        public bool SendMessage(string shipreceiver, string pbreceiver, string message)
        {

            if (shipreceiver.Length + pbreceiver.Length + message.Length + MESSAGE_PART_LENGTH > MyRadioAntennaMessages.MAX_MESSAGE_LENGTH)
                return false; //Message was to long. The magic 12 comes from the added strings: " to " + " on " + ": " + "\n"

            if (m_sendMessageTimestamp == 0)
            {
                //First message sent.
                m_sendMessageTimestamp = Stopwatch.GetTimestamp();
            }
            else
            {
                var elapsedTime = (Stopwatch.GetTimestamp() - m_sendMessageTimestamp) * Sync.RelativeSimulationRatio;
                elapsedTime *= STOPWATCH_FREQUENCY;

                if (elapsedTime >= SEND_MESSAGES_COOLDOWN)
                {
                    //We waited long enough, allow the send. And set the timestamp
                    m_sendMessageTimestamp = Stopwatch.GetTimestamp();
                }
                else
                {
                    //We haven't yet waited long enough. Don't send the message.
                    return false;
                }
            }

            //Create the message in the storage and keep the reference.
            int hash = PushMessageToStorage(this.CubeGrid.DisplayName, shipreceiver, pbreceiver, message);

            foreach (var broadcaster in RadioReceiver.RelayedBroadcasters)
            {
                if (broadcaster.Parent is MyRadioAntenna)
                {
                    MyRadioAntenna antenna = broadcaster.Parent as MyRadioAntenna;
                    if (!antenna.Listen || antenna == this) //Antenna is not listening to open chatter, thus ignore it.
                        continue;

                    //Send the reference to the listening antenna
                    antenna.ReceiveMessage(hash);

                    if (antenna.CubeGrid.DisplayName == shipreceiver)
                    {
                        var gridGroup = MyCubeGridGroups.Static.Logical.GetGroup(antenna.CubeGrid);
                        var terminalSystem = gridGroup.GroupData.TerminalSystem;
                        terminalSystem.UpdateGridBlocksOwnership(this.OwnerId);

                        IMyGridTerminalSystem grid = (IMyGridTerminalSystem)terminalSystem;
                        if (grid == null)
                            continue;

                        MyProgrammableBlock pb = (MyProgrammableBlock)grid.GetBlockWithName(pbreceiver); //Get the programmable block with the specified name.
                        if (pb == null) //If the block with the name does not exist, or if the player has no permission this will be null.
                            continue;

                        pb.Run(message);
                    }
                }
            }

            return true;
        }

        public void OpenReceivedMessagesDisplay() 
        {
            if (m_messageHashs == null)
                m_messageHashs = new List<int>();

            StringBuilder desc = MyRadioAntennaMessages.GetMessages(this);
            MyGuiScreenTextPanel textBox;

            if (desc != null)
            {
                textBox = new MyGuiScreenTextPanel(missionTitle: "Received Messages",
                           currentObjectivePrefix: "",
                           currentObjective: "",
                           description: MyRadioAntennaMessages.GetMessages(this).ToString(),
                           editable: false,
                           resultCallback: null);
            }
            else
            {
                textBox = new MyGuiScreenTextPanel(missionTitle: "Received Messages",
                           currentObjectivePrefix: "",
                           currentObjective: "",
                           description: "",
                           editable: false,
                           resultCallback: null);
            }

            MyScreenManager.AddScreen(textBox);
        }

        /// <summary>
        /// This method makes sure that only one antenna per ship has the listen attribute enabled.
        /// </summary>
        private void AssertOnlyOneListener()
        {
            var gridGroup = MyCubeGridGroups.Static.Logical.GetGroup(this.CubeGrid);
            var terminalGrid = (IMyGridTerminalSystem)gridGroup.GroupData.TerminalSystem;

            var blocks = terminalGrid.Blocks;
            foreach (var block in blocks)
            {
                if (block is MyRadioAntenna && !block.Equals(this))
                    ((MyRadioAntenna)block).Listen = false;
            }
        }

        private const float SEND_MESSAGES_COOLDOWN = 0.75f;
        private static readonly float STOPWATCH_FREQUENCY = 1.0f / Stopwatch.Frequency;
        private static readonly int MESSAGE_PART_LENGTH = MyTexts.Get(MySpaceTexts.Antenna_MsgPart_to).Length + MyTexts.GetString(MySpaceTexts.Antenna_MsgPart_on).Length;

        private long m_sendMessageTimestamp = 0;

        private bool m_Listen = false;
        public bool Listen
        {
            get { return m_Listen; }
            set 
            {
                if (m_Listen != value)
                {
                    if (value == true)
                        AssertOnlyOneListener();

                    m_Listen = value;
                    RaisePropertiesChanged();
                }
            }
        }

        public List<int> m_messageHashs;
        //not mine

		bool IsBroadcasting()
		{
			return (m_radioBroadcaster != null) ? m_radioBroadcaster.WantsToBeEnabled : false;
		}

		bool IMyRadioAntenna.IsBroadcasting
		{
			get {  return IsBroadcasting(); }
		}
    }
}
