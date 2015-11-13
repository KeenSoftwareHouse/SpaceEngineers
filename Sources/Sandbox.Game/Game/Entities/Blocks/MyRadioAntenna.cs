#region Using

using System.Collections.Generic;
using System.Collections.Concurrent;
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
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Localization;
using VRage;
using VRage.Utils;
using VRage.ModAPI;
using Sandbox.Game.GameSystems;
using Sandbox.Common;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    struct Antenna2AntennaMessage
    {
        public long SenderId;
        public string Message;

        public Antenna2AntennaMessage(long senderId, string message)
        {
            this.SenderId = senderId;
            this.Message  = message;
        }
    }

    [MyCubeBlockType(typeof(MyObjectBuilder_RadioAntenna))]
    class MyRadioAntenna : MyFunctionalBlock, IMyComponentOwner<MyDataBroadcaster>, IMyComponentOwner<MyDataReceiver>, IMyGizmoDrawableObject, IMyRadioAntenna
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

            var showConfig = new MyTerminalControlOnOffSwitch<MyRadioAntenna>("ShowInToolbarConfig", MySpaceTexts.Terminal_ShowInToolbarConfig, MySpaceTexts.Terminal_ShowInToolbarConfigToolTip);
            showConfig.Getter = (x) => x.ShowInToolbarConfig;
            showConfig.Setter = (x, v) => x.RequestShowInToolbarConfig(v);
            MyTerminalControlFactory.AddControl(showConfig);

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

            /* Init Nearby Antenna Patch.
            */ StaticInitNearbyAntennaPatch();
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

	        var antennaDefinition = BlockDefinition as MyRadioAntennaDefinition;
			Debug.Assert(antennaDefinition != null);

			var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                antennaDefinition.ResourceSinkGroup,
                MyEnergyConstants.MAX_REQUIRED_POWER_ANTENNA,
                UpdatePowerInput);
            sinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;
	        ResourceSink = sinkComp;
            ResourceSink.Update();

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            if (Sync.IsServer)
            {
                this.IsWorkingChanged += UpdatePirateAntenna;
                this.CustomNameChanged += UpdatePirateAntenna;
                this.OwnershipChanged += UpdatePirateAntenna;
                UpdatePirateAntenna(this);
            }

            ShowOnHUD = false;

            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;

            /* Hook for Nearby Antenna Patch
            */ this.InitNearbyAntennaPatch(antennaBuilder);
        }

        protected override void Closing()
        {
            if (Sync.IsServer)
            {
                UpdatePirateAntenna(forceRemove: true);
            }

            /* Nearby Antenna Patch
            */ this.RemoveFromAntennaList();

            base.Closing();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_RadioAntenna objectBuilder = (MyObjectBuilder_RadioAntenna)base.GetObjectBuilderCubeBlock(copy);
            objectBuilder.BroadcastRadius = m_radioBroadcaster.BroadcastRadius;
            objectBuilder.ShowShipName = this.ShowShipName;
            objectBuilder.EnableBroadcasting = m_radioBroadcaster.WantsToBeEnabled;
            /* Nearby Antenna Patch
            */ this.SaveNearbyAntennaPatch(objectBuilder);
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

        #region Pirates

        void UpdatePirateAntenna(MyCubeBlock obj)
        {
            UpdatePirateAntenna();
        }

        void UpdatePirateAntenna(bool forceRemove = false)
        {
            bool isActive = IsWorking && Sync.Players.GetNPCIdentities().Contains(OwnerId);
            bool doRemove = !isActive || forceRemove;
            MyPirateAntennas.UpdatePirateAntenna(this.EntityId, doRemove, this.CustomName);
        }

        #endregion

        protected override void OnEnabledChanged()
        {
			ResourceSink.Update();
            m_radioReceiver.UpdateBroadcastersInRange();
            base.OnEnabledChanged();
        }

        protected override bool CheckIsWorking()
        {
			return ResourceSink.IsPowered && base.CheckIsWorking();
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
			ResourceSink.Update();
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
			ResourceSink.Update();
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
			MyValueFormatter.AppendWorkInBestUnit(ResourceSink.IsPowered ? ResourceSink.RequiredInput : 0, DetailedInfo);
            RaisePropertiesChanged();
        }

        float IMyRadioAntenna.Radius
        {
            get { return GetRadius(); }
        }

		bool IsBroadcasting()
		{
			return (m_radioBroadcaster != null) ? m_radioBroadcaster.WantsToBeEnabled : false;
		}

		bool IMyRadioAntenna.IsBroadcasting
		{
			get {  return IsBroadcasting(); }
		}
        
        // Cache all existing antennas. This drastically reduces lookup
        // time, as I do not have to iterate over all existing grids and
        // their blocks, where not all grids have antennas on them and
        // where barely any block is an antenna.
        //
        // TODO: No concurrent access ever? Use Dictionary<>.
        private static ConcurrentDictionary<long,MyRadioAntenna> allExistingAntennas = null;
        
        // I expect that most programs that query multiple pieces of
        // information about nearby antennas sequentially will do so
        // antenna by antenna. I.e. take antenna 1, query all there is
        // to know about it, then go on with antenna 2, etc.
        // Thus, this caching trick might reduce an otherwise high amount
        // of redundant checks. This cache is invalidated each update.
        private long m_cachedId = -1;
        private long m_cachedDetailId = -1;
        private MyRadioAntenna m_cachedAntenna = null;
        private MyRadioAntenna m_cachedDetailAntenna = null;

        // Call this or die! D:<
        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            this.ClearNearbyAntennaCache();
        }

        private void ClearNearbyAntennaCache()
        {
            m_cachedId = -1;
            m_cachedDetailId = -1;
            m_cachedAntenna = null;
            m_cachedDetailAntenna = null;
        }
        
        private MyRadioAntenna FindAntenna(long antennaId)
        {
            Debug.Assert(allExistingAntennas != null);
            MyRadioAntenna found;
            allExistingAntennas.TryGetValue(antennaId, out found);
            return found;
        }
        
        private bool IsAntennaDetailReachable(IMyRadioAntenna antenna)
        {
            if(antenna != null)
            {
                Vector3D myPos  = this.CubeGrid.GridIntegerToWorld(this.GetPositionInGrid());
                double   distSq = Vector3D.DistanceSquared(antenna.GetPosition(),myPos);
                double   radSq  = this.DetailScanRange;
                radSq *= radSq;
                if(radSq >= distSq) { return true; }
            }
            return false;
        }
        
        private bool IsAntennaReachable(IMyRadioAntenna antenna)
        {
            if(antenna != null)
            {
                Vector3D myPos  = this.CubeGrid.GridIntegerToWorld(this.GetPositionInGrid());
                double   distSq = Vector3D.DistanceSquared(antenna.GetPosition(),myPos);
                double   radSq  = this.GetRadius();
                radSq *= radSq;
                if(radSq >= distSq) { return true; }
            }
            return false;
        }
        
        private bool IsAntennaResponseReachable(MyRadioAntenna antenna)
        {
            if(antenna != null)
            {
                Vector3D myPos   = this.CubeGrid.GridIntegerToWorld(this.GetPositionInGrid());
                Vector3D pos     = antenna.CubeGrid.GridIntegerToWorld(antenna.GetPositionInGrid());
                double   distSq  = Vector3D.DistanceSquared(pos,myPos);
                double   myRadSq = this.GetRadius();
                double   radSq   = antenna.GetRadius();
                myRadSq *= myRadSq;
                radSq   *= radSq;
                if((myRadSq >= distSq) & (radSq >= distSq)) { return true; }
            }
            return false;
        }
        
        private MyRadioAntenna FindAntennaInDetailRange(long antennaId)
        {
            if(this.m_cachedDetailId != antennaId)
            {
                var found = this.FindAntenna(antennaId);
                this.m_cachedDetailAntenna = this.IsAntennaDetailReachable(found) ? found : null;
                this.m_cachedDetailId      = antennaId;
            }
            return this.m_cachedDetailAntenna;
        }
        
        private MyRadioAntenna FindAntennaInRange(long antennaId)
        {
            if(this.m_cachedId != antennaId)
            {
                var found = this.FindAntenna(antennaId);
                this.m_cachedAntenna = this.IsAntennaReachable(found) ? found : null;
                this.m_cachedId      = antennaId;
            }
            return this.m_cachedAntenna;
        }
        
        private MyRadioAntenna FindAntennaInResponseRange(long antennaId)
        {
            var found = this.FindAntenna(antennaId);
            return this.IsAntennaResponseReachable(found) ? found : null;
        }
        
        long IMyRadioAntenna.AntennaId { get { return this.EntityId; } }
        
        float DetailScanRange { get { return this.GetRadius() * 0.1f; } }

        float IMyRadioAntenna.DetailScanRange { get { return this.DetailScanRange; } }
        
        private bool m_dataTransferEnabled = false;
        private Queue<Antenna2AntennaMessage> m_dataQueue = null;
        
        bool DataTransferEnabled
        {
            get { return m_dataTransferEnabled; }
            set
            {
                if(this.m_dataTransferEnabled != value)
                {
                    this.m_dataTransferEnabled = value;
                    // Create the queue on demand, as most antennas won't make use of this patch.
                    if(value) { this.m_dataQueue = new Queue<Antenna2AntennaMessage>(); }
                    // Allow the queue to be collected if no longer needed.
                    else { this.m_dataQueue = null; }
                }
            }
        }

        bool IMyRadioAntenna.DataTransferEnabled
        {
            get { return this.DataTransferEnabled; }
            set { this.DataTransferEnabled = value; }
        }
        
        public bool ReceiveData(long senderId, string message)
        {
            if(this.m_dataTransferEnabled)
            {
                Debug.Assert(this.m_dataQueue != null);
                this.m_dataQueue.Enqueue(new Antenna2AntennaMessage(senderId,message));
                return true;
            }
            return false;
        }
        
        bool IMyRadioAntenna.SendToNearbyAntenna(long antennaId, string message)
        {
            var target = this.FindAntennaInRange(antennaId);
            return target == null ? false : target.ReceiveData(this.EntityId, message);
        }
        
        void IMyRadioAntenna.BroadcastToNearbyAntennas(string message)
        {
            Debug.Assert(allExistingAntennas != null);
            long id = this.EntityId;
            foreach(var en in allExistingAntennas)
            {
                if(this.IsAntennaReachable(en.Value)) { en.Value.ReceiveData(id, message); }
            }
        }
        
        private string GetReceivedData(out long senderId)
        {
            senderId = 0xDEADBEEF;
            if(this.m_dataTransferEnabled)
            {
                Debug.Assert(this.m_dataQueue != null);
                if(this.m_dataQueue.Count != 0)
                {
                    var msg  = this.m_dataQueue.Dequeue();
                    senderId = msg.SenderId;
                    return msg.Message;
                }
            }
            return null;
        }
        
        string IMyRadioAntenna.GetReceivedData(out long senderId) { return this.GetReceivedData(out senderId); }
        
        void IMyRadioAntenna.FindNearbyAntennas(ref List<long> found)
        {
            Debug.Assert(allExistingAntennas != null);
            if(found == null) { found = new List<long>(); }
            foreach(var en in allExistingAntennas)
            {
                if((en.Key != this.EntityId) && this.IsAntennaReachable(en.Value)) { found.Add(en.Key); }
            }
        }
        
        bool IMyRadioAntenna.IsNearbyAntennaInReach(long antennaId)
        {
            return null != this.FindAntennaInRange(antennaId);
        }
        
        bool IMyRadioAntenna.IsNearbyAntennaInResponseReach(long antennaId)
        {
            return null != this.FindAntennaInResponseRange(antennaId);
        }
        
        float? IMyRadioAntenna.GetNearbyAntennaRadius(long antennaId)
        {
            var antenna = this.FindAntennaInRange(antennaId);
            if(antenna != null) { return antenna.GetRadius(); }
            return null;
        }

        private static MyCubeGrid MainShipGrid(MyRadioAntenna antenna)
        {
            return MyAntennaSystem.GetLogicalGroupRepresentative(antenna.CubeGrid);
        }
        
        string IMyRadioAntenna.GetNearbyAntennaShipName(long antennaId)
        {
            var antenna = this.FindAntennaInRange(antennaId);
            if(antenna != null && antenna.ShowShipName) { return MainShipGrid(antenna).DisplayName ?? string.Empty; }
            return null;
        }
        
        Vector3D? IMyRadioAntenna.GetNearbyAntennaPosition(long antennaId)
        {
            var antenna = this.FindAntennaInRange(antennaId);
            if(antenna != null)
            {
                return antenna.CubeGrid.GridIntegerToWorld(antenna.GetPositionInGrid());
            }
            return null;
        }
        
        long? IMyRadioAntenna.GetNearbyAntennaOwnerId(long antennaId)
        {
            var antenna = this.FindAntennaInRange(antennaId);
            if(antenna != null) { return antenna.OwnerId; }
            return null;
        }
        
        MyRelationsBetweenPlayerAndBlock? IMyRadioAntenna.GetNearbyAntennaPlayerRelationToOwner(long antennaId)
        {
            var antenna = this.FindAntennaInRange(antennaId);
            if(antenna != null) { return antenna.GetPlayerRelationToOwner(); }
            return null;
        }
        
        MyRelationsBetweenPlayerAndBlock? IMyRadioAntenna.GetNearbyAntennaUserRelationToOwner(long antennaId, long playerId)
        {
            var antenna = this.FindAntennaInRange(antennaId);
            if(antenna != null) { return antenna.GetUserRelationToOwner(playerId); }
            return null;
        }
        
        MyCubeSize? IMyRadioAntenna.GetNearbyAntennaCubeSize(long antennaId)
        {
            var antenna = this.FindAntennaInDetailRange(antennaId);
            if(antenna != null) { return MainShipGrid(antenna).GridSizeEnum; }
            return null;
        }
        
        bool? IMyRadioAntenna.GetNearbyAntennaIsStatic(long antennaId)
        {
            var antenna = this.FindAntennaInDetailRange(antennaId);
            if(antenna != null) { return MainShipGrid(antenna).IsStatic; }
            return null;
        }
        
        float? IMyRadioAntenna.GetNearbyAntennaMass(long antennaId)
        {
            var antenna = this.FindAntennaInDetailRange(antennaId);
            if(antenna != null) { return MainShipGrid(antenna).Physics.Mass; }
            return null;
        }
        
        BoundingSphereD? IMyRadioAntenna.GetNearbyAntennaWorldVolume(long antennaId)
        {
            var antenna = this.FindAntennaInDetailRange(antennaId);
            if(antenna != null) { return MainShipGrid(antenna).PositionComp.WorldVolume; }
            return null;
        }
        
        BoundingBoxD? IMyRadioAntenna.GetNearbyAntennaWorldAABB(long antennaId)
        {
            var antenna = this.FindAntennaInDetailRange(antennaId);
            if(antenna != null) { return MainShipGrid(antenna).PositionComp.WorldAABB; }
            return null;
        }
        
        private static void StaticInitNearbyAntennaPatch()
        {
            Debug.Assert(allExistingAntennas == null);
            allExistingAntennas = new ConcurrentDictionary<long,MyRadioAntenna>();
        }
        
        private void InitNearbyAntennaPatch(MyObjectBuilder_RadioAntenna builder)
        {
            Debug.Assert(allExistingAntennas != null);
            bool success = allExistingAntennas.TryAdd(this.EntityId,this);
            Debug.Assert(success);
            this.m_dataTransferEnabled = builder.DataTransferEnabled;
            // If data transfer is enabled, ensure there is a valid queue.
            if(builder.DataTransferEnabled)
            {
                if(builder.PendingDataPacks != null & builder.PendingSenderIds != null)
                {
                    // Get the minimum length of both arrays in case of broken data.
                    int capacity = builder.PendingDataPacks.Length;
                    if(builder.PendingSenderIds.Length < capacity) { capacity = builder.PendingSenderIds.Length; }
                    this.m_dataQueue = new Queue<Antenna2AntennaMessage>(capacity);
                    // Enqueue old data, if any.
                    for(int i=0; i<capacity; ++i)
                    {
                        this.m_dataQueue.Enqueue(new Antenna2AntennaMessage(
                            builder.PendingSenderIds[i]
                        ,   builder.PendingDataPacks[i]
                        ));
                    }
                }
                else { this.m_dataQueue = new Queue<Antenna2AntennaMessage>(); }
            }
        }
        
        private void SaveNearbyAntennaPatch(MyObjectBuilder_RadioAntenna builder)
        {
            builder.EnableBroadcasting = this.m_dataTransferEnabled;
            if((this.m_dataQueue != null) && (this.m_dataQueue.Count != 0))
            {
                builder.PendingSenderIds = new long[this.m_dataQueue.Count];
                builder.PendingDataPacks = new string[this.m_dataQueue.Count];
                int i=0;
                foreach(var msg in this.m_dataQueue)
                {
                    builder.PendingSenderIds[i] = msg.SenderId;
                    builder.PendingDataPacks[i] = msg.Message;
                    ++i;
                }
            }
        }
        
        private void RemoveFromAntennaList()
        {
            Debug.Assert(allExistingAntennas != null);
            Debug.Assert(allExistingAntennas.Count > 0);
            MyRadioAntenna trash;
            bool success = allExistingAntennas.TryRemove(this.EntityId, out trash);
            Debug.Assert(success);
        }


    }
}
