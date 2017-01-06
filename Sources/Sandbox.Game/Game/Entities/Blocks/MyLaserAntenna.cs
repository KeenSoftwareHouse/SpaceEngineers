#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Gui;
using VRageMath;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.Localization;
using VRage;
using System.Threading;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using VRage.Game;
using VRage.Utils;
using VRage.ModAPI;
using VRage.Game.Entity;
using VRage.Network;
using Sandbox.Engine.Multiplayer;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_LaserAntenna))]
    public partial class MyLaserAntenna : MyFunctionalBlock, IMyGizmoDrawableObject
    {
        protected Color m_gizmoColor = new Vector4(0.1f, 0.1f, 0.0f, 0.1f);
        protected const float m_maxGizmoDrawDistance = 10000.0f;

        MyLaserBroadcaster Broadcaster
        {
            get { return (MyLaserBroadcaster)Components.Get<MyDataBroadcaster>(); }
            set { Components.Add<MyDataBroadcaster>(value); }
        }
        MyLaserReceiver Receiver
        {
            get { return (MyLaserReceiver)Components.Get<MyDataReceiver>(); }
            set { Components.Add<MyDataReceiver>(value); }
        }

        public enum StateEnum : byte
        {
            idle = 0x00,
            rot_GPS,
            search_GPS,
            rot_Rec,
            contact_Rec,
            connected
        }
        StateEnum m_state;
        private StateEnum State
        {
            get
            {
                return m_state;
            }
            set
            {
                m_state = value;
                if (m_state == StateEnum.idle || m_state == StateEnum.rot_GPS)
                    m_targetId = null;
            }
        }

        long? m_targetId;
        StringBuilder m_lastKnownTargetName=new StringBuilder();

        StringBuilder m_myStateText = new StringBuilder();//this will be shown in the list of possible receivers in terminal of other lasers

        static string m_clipboardText;
        StringBuilder m_termGpsName=new StringBuilder();
        Vector3D? m_termGpsCoords;

        long? m_selectedEntityId=null;//entity ID selected in list of possible receivers

        bool m_rotationFinished = true;//direction to intended target reached

        float m_needRotation=0;
        float m_needElevation = 0;

        float m_minElevationRadians = 0;                        // This is combined with m_MinElevation during clamping
        float m_maxElevationRadians = (float)(2.0 * Math.PI);
        float m_minAzimuthRadians = 0;
        float m_maxAzimuthRadians = (float)(2.0 * Math.PI);
        bool m_outsideLimits = false;                           // Rotation/elevation is out of bounds

        Vector3D m_targetCoords;//where is or where we think is receiver

        float m_maxRange;
        protected static float m_Max_LosDist = 10000;

        bool m_IsPermanent = false;
        bool m_OnlyPermanentExists = false;

        public bool m_needLineOfSight = true;

        public Vector3D HeadPos{
            get{
                if (m_base2!=null)
                    return m_base2.PositionComp.GetPosition();
                return PositionComp.GetPosition();
               }
        }

        public MatrixD InitializationMatrix { get; private set; }

        public Color GetGizmoColor()
        {
            return m_gizmoColor;
        }

        public Vector3 GetPositionInGrid()
        {
            return Position;
        }

        public float GetRadius()
        {
            return 100;
        }

        public bool CanBeDrawed()
        {
            if (false == MyCubeGrid.ShowAntennaGizmos || false == this.IsWorking || false == this.HasLocalPlayerAccess() ||
              GetDistanceBetweenCameraAndBoundingSphere() > m_maxGizmoDrawDistance)
            {
                return false;
            }
            return true;//Entities.Cube.MyLaserAntenna.IsRecievedByPlayer(this);
        }

        public BoundingBox? GetBoundingBox()
        {
            return null;
        }

        public MatrixD GetWorldMatrix()
        {
            return PositionComp.WorldMatrix;
        }

        public bool EnableLongDrawDistance()
        {
            return true;
        }

        public new MyLaserAntennaDefinition BlockDefinition
        {
            get { return (MyLaserAntennaDefinition)base.BlockDefinition; }
        }
        
        public MyLaserAntenna()
        {
            CreateTerminalControls();
        }

        static MyLaserAntenna()
        {
            m_Max_LosDist=MySession.Static.Settings.ViewDistance;

        }
        static MyTerminalControlButton<MyLaserAntenna> idleButton;
        static MyTerminalControlButton<MyLaserAntenna> connectGPS;
        static MyTerminalControlListbox<MyLaserAntenna> receiversList;
        static MyTerminalControlTextbox<MyLaserAntenna> gpsCoords;
        static MyTerminalControlButton<MyLaserAntenna> PasteGpsCoords;
        static MyTerminalControlButton<MyLaserAntenna> ConnectReceiver;

        protected override void CreateTerminalControls()
        {
            /*MyTerminalControlFactory.RemoveBaseClass<MyLaserAntenna, MyTerminalBlock>();

            var customName = new MyTerminalControlTextbox<MyLaserAntenna>("CustomName", MySpaceTexts.Name, MySpaceTexts.Blank);
            customName.Getter = (x) => x.CustomName;
            customName.Setter = (x, v) => MySyncBlockHelpers.SendChangeNameRequest(x, v);
            customName.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(customName);*/

            if (MyTerminalControlFactory.AreControlsCreated<MyLaserAntenna>())
                return;
            base.CreateTerminalControls();
            idleButton = new MyTerminalControlButton<MyLaserAntenna>("Idle", MySpaceTexts.LaserAntennaIdleButton, MySpaceTexts.Blank,
                delegate (MyLaserAntenna self)
                {
                    self.SetIdle();
                    idleButton.UpdateVisual();
                });
            idleButton.Enabled = (x) => x.m_state != StateEnum.idle;
            idleButton.EnableAction();
            MyTerminalControlFactory.AddControl(idleButton);

            //--------------------------------------------------------------------------------------
            MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyLaserAntenna>());

            var copyCoordsButton = new MyTerminalControlButton<MyLaserAntenna>("CopyCoords", MySpaceTexts.LaserAntennaCopyCoords, MySpaceTexts.LaserAntennaCopyCoordsHelp,
                delegate (MyLaserAntenna self)
                {
                    StringBuilder sanitizedName = new StringBuilder(self.DisplayNameText);
                    sanitizedName.Replace(':', ' ');
                    StringBuilder sb = new StringBuilder("GPS:", 256);
                    sb.Append(sanitizedName); sb.Append(":");
                    sb.Append(Math.Round(self.HeadPos.X, 2).ToString(System.Globalization.CultureInfo.InvariantCulture)); sb.Append(":");
                    sb.Append(Math.Round(self.HeadPos.Y, 2).ToString(System.Globalization.CultureInfo.InvariantCulture)); sb.Append(":");
                    sb.Append(Math.Round(self.HeadPos.Z, 2).ToString(System.Globalization.CultureInfo.InvariantCulture)); sb.Append(":");
#if !XB1
                    Thread thread = new Thread(() => System.Windows.Forms.Clipboard.SetText(sb.ToString()));
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();
#else
                    Debug.Assert(false, "Not Clipboard support on XB1!");
#endif
                });
            MyTerminalControlFactory.AddControl(copyCoordsButton);

            var copyTargetCoordsButton = new MyTerminalControlButton<MyLaserAntenna>("CopyTargetCoords", MySpaceTexts.LaserAntennaCopyTargetCoords, MySpaceTexts.LaserAntennaCopyTargetCoordsHelp,
                delegate (MyLaserAntenna self)
                {
                    if (self.m_targetId == null)
                        return;
                    StringBuilder sanitizedName = new StringBuilder(self.m_lastKnownTargetName.ToString());
                    sanitizedName.Replace(':', ' ');
                    StringBuilder sb = new StringBuilder("GPS:", 256);
                    sb.Append(sanitizedName); sb.Append(":");
                    sb.Append(Math.Round(self.m_targetCoords.X, 2).ToString(System.Globalization.CultureInfo.InvariantCulture)); sb.Append(":");
                    sb.Append(Math.Round(self.m_targetCoords.Y, 2).ToString(System.Globalization.CultureInfo.InvariantCulture)); sb.Append(":");
                    sb.Append(Math.Round(self.m_targetCoords.Z, 2).ToString(System.Globalization.CultureInfo.InvariantCulture)); sb.Append(":");
#if !XB1
                    Thread thread = new Thread(() => System.Windows.Forms.Clipboard.SetText(sb.ToString()));
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();
#else
                    Debug.Assert(false, "Not Clipboard support on XB1!");
#endif
                });
            copyTargetCoordsButton.Enabled = (x) => x.m_targetId != null;
            MyTerminalControlFactory.AddControl(copyTargetCoordsButton);

            PasteGpsCoords = new MyTerminalControlButton<MyLaserAntenna>("PasteGpsCoords", MySpaceTexts.LaserAntennaPasteGPS, MySpaceTexts.Blank,
                delegate (MyLaserAntenna self)
                {
                    Thread thread = new Thread(() => PasteFromClipboard());
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();
                    self.PasteCoordinates(m_clipboardText);
                });
            //PasteGpsCoords.Enabled = (x) => x.P2PTargetCoords;
            PasteGpsCoords.EnableAction();
            MyTerminalControlFactory.AddControl(PasteGpsCoords);

            gpsCoords = new MyTerminalControlTextbox<MyLaserAntenna>("gpsCoords", MySpaceTexts.LaserAntennaSelectedCoords, MySpaceTexts.Blank);
            gpsCoords.Getter = (x) => x.m_termGpsName;
            gpsCoords.Enabled = (x) => false;
            MyTerminalControlFactory.AddControl(gpsCoords);

            connectGPS = new MyTerminalControlButton<MyLaserAntenna>("ConnectGPS", MySpaceTexts.LaserAntennaConnectGPS, MySpaceTexts.Blank,
                delegate (MyLaserAntenna self)
                {
                    if (self.m_termGpsCoords == null)
                        return;//should not get here anyway
                    self.ConnectToGps();
                });
            connectGPS.Enabled = (x) => x.CanConnectToGPS();
            connectGPS.EnableAction();
            MyTerminalControlFactory.AddControl(connectGPS);

            var isPerm = new MyTerminalControlCheckbox<MyLaserAntenna>("isPerm", MySpaceTexts.LaserAntennaPermanentCheckbox, MySpaceTexts.Blank);
            isPerm.Getter = (self) => self.m_IsPermanent;
            isPerm.Setter = (self, v) =>
            {
                self.ChangePerm(v);
            };
            isPerm.Enabled = (self) => self.State == StateEnum.connected;
            isPerm.EnableAction();
            MyTerminalControlFactory.AddControl(isPerm);

            //--------------------------------------------------------------------------------------
            MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyLaserAntenna>());

            receiversList = new MyTerminalControlListbox<MyLaserAntenna>("receiversList", MySpaceTexts.LaserAntennaReceiversList, MySpaceTexts.LaserAntennaReceiversListHelp);
            receiversList.ListContent = (x, population, selected) => x.PopulatePossibleReceivers(population, selected);
            receiversList.ItemSelected = (x, y) => x.ReceiverSelected(y);
            //receiversList.Enabled = (x) => !x.P2PTargetCoords;
            MyTerminalControlFactory.AddControl(receiversList);

            ConnectReceiver = new MyTerminalControlButton<MyLaserAntenna>("ConnectReceiver", MySpaceTexts.LaserAntennaConnectButton, MySpaceTexts.Blank,
                delegate (MyLaserAntenna self)
                {
                    self.ConnectToId();
                });
            ConnectReceiver.Enabled = (x) => x.m_selectedEntityId != null;
            MyTerminalControlFactory.AddControl(ConnectReceiver);
        }

        static void UpdateVisuals()
        {
            gpsCoords.UpdateVisual();
            idleButton.UpdateVisual();
            connectGPS.UpdateVisual();
            receiversList.UpdateVisual();
            ConnectReceiver.UpdateVisual();
        }
        bool CanConnectToGPS()
        {
            if (m_termGpsCoords == null)
                return false;
            if (Dist2To(m_termGpsCoords) < 1f)
                return false;
            return true;
        }
        static void PasteFromClipboard()
        {
#if !XB1
           m_clipboardText = System.Windows.Forms.Clipboard.GetText();
#else
            Debug.Assert(false, "Not Clipboard support on XB1!");
#endif
           
        }
        
        Vector3D m_temp;
        public void DoPasteCoords(string str)
        {

            if (MyGpsCollection.ParseOneGPS(str, m_termGpsName, ref m_temp))
            {
                if (m_termGpsCoords == null)
                    m_termGpsCoords = new Vector3D?(m_temp);
                m_termGpsCoords = m_temp;
                m_termGpsName.Append(" ").Append(m_temp.X).Append(":").Append(m_temp.Y).Append(":").Append(m_temp.Z);
            }
            UpdateVisuals();            
        }
        
        protected void UpdateMyStateText()
        {
            m_myStateText.Clear().Append(CustomName);
            m_myStateText.Append(" [");
            switch (State)
            {
                case StateEnum.idle:
                    m_myStateText.Append(State);
                    break;
                case StateEnum.connected:
                    if (m_IsPermanent)
                        m_myStateText.Append("#=>");
                    else
                        m_myStateText.Append("=>");
                    break;
                case StateEnum.rot_GPS:
                case StateEnum.rot_Rec:
                    if (m_IsPermanent)
                        m_myStateText.Append("#>>");
                    else
                        m_myStateText.Append(">>");
                    break;
                case StateEnum.search_GPS:
                    m_myStateText.Append("?>");
                    break;
                case StateEnum.contact_Rec:
                    if (m_IsPermanent)
                        m_myStateText.Append("#~>");
                    else
                        m_myStateText.Append("~>");
                    break;
            }
            if (State == StateEnum.connected
                || State == StateEnum.contact_Rec
                || State == StateEnum.rot_Rec)
            {
                m_myStateText.Append(m_lastKnownTargetName);
            }
            else if (State == StateEnum.rot_GPS
                || State == StateEnum.search_GPS)
            {
                m_myStateText.Append(m_termGpsName);
                m_myStateText.Append(" ");
                m_myStateText.Append(m_termGpsCoords);
            }
            m_myStateText.Append("]");

        }

        protected StringBuilder m_tempSB=new StringBuilder();
        protected void PopulatePossibleReceivers(ICollection<MyGuiControlListbox.Item> population,ICollection<MyGuiControlListbox.Item> selected)
        {
            if(MySession.Static == null|| this.Closed)
            {
                return;
            }

            foreach (var laser in MySession.Static.LaserAntennas)
            {
                if (laser.Key == this.EntityId)
                    continue;
				if (!(laser.Value.Enabled && laser.Value.IsFunctional && ResourceSink.SuppliedRatioByType(MyResourceDistributorComponent.ElectricityId) > 0.99f))
                    continue;
                if (!Receiver.CanIUseIt(laser.Value.Broadcaster, this.OwnerId)
                    || !laser.Value.Receiver.CanIUseIt(Broadcaster, laser.Value.OwnerId))
                    continue;
                if (!Receiver.RelayedBroadcasters.Contains(laser.Value.Broadcaster))
                    continue;

                var item = new MyGuiControlListbox.Item(ref laser.Value.m_myStateText, null,null, laser.Value);
                population.Add(item);

                if (m_selectedEntityId == laser.Value.EntityId)
                    selected.Add(item);
            }
            //m_selectedEntityId = null;
            ConnectReceiver.UpdateVisual();
        }

        protected void ReceiverSelected(List<MyGuiControlListbox.Item> y)
        {
            m_selectedEntityId = (y.First().UserData as MyLaserAntenna).EntityId;
            ConnectReceiver.UpdateVisual();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            ResourceSink = new MyResourceSinkComponent();
            ResourceSink.Init(
                BlockDefinition.ResourceSinkGroup,
                BlockDefinition.PowerInputLasing,
                UpdatePowerInput);

            Broadcaster = new MyLaserBroadcaster();
            Receiver = new MyLaserReceiver();

            base.Init(objectBuilder, cubeGrid);

            MyObjectBuilder_LaserAntenna ob = (MyObjectBuilder_LaserAntenna)objectBuilder;

            State=(StateEnum)(ob.State & 0x7);
            m_IsPermanent = (ob.State & 0x8) != 0;
            m_targetId = ob.targetEntityId;
            m_lastKnownTargetName.Append(ob.LastKnownTargetName);
            if (ob.gpsTarget!=null)
                m_termGpsCoords=ob.gpsTarget;
            m_termGpsName.Clear().Append(ob.gpsTargetName);
            m_rotation = ob.HeadRotation.X;
            m_elevation = ob.HeadRotation.Y;
            m_targetCoords = ob.LastTargetPosition;

            m_maxRange = BlockDefinition.MaxRange;
            m_needLineOfSight = BlockDefinition.RequireLineOfSight;

            if (BlockDefinition != null)
            {
                m_minElevationRadians = MathHelper.ToRadians(NormalizeAngle(BlockDefinition.MinElevationDegrees));
                m_maxElevationRadians = MathHelper.ToRadians(NormalizeAngle(BlockDefinition.MaxElevationDegrees));

                if (m_minElevationRadians > m_maxElevationRadians)
                {
                    m_minElevationRadians -= MathHelper.TwoPi;
                }

                m_minAzimuthRadians = MathHelper.ToRadians(NormalizeAngle(BlockDefinition.MinAzimuthDegrees));
                m_maxAzimuthRadians = MathHelper.ToRadians(NormalizeAngle(BlockDefinition.MaxAzimuthDegrees));

                if (m_minAzimuthRadians > m_maxAzimuthRadians)
                {
                    m_minAzimuthRadians -= MathHelper.TwoPi;
                }

                ClampRotationAndElevation();
            }

            InitializationMatrix = PositionComp.LocalMatrix;

		
			ResourceSink.IsPoweredChanged += IsPoweredChanged;
            ResourceSink.Update();
            OnClose += delegate { OnClosed(); };
            
            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            if (SlimBlock.CubeGrid.GridSize > 1.5f)//large grid variant has wider limit
                m_MinElevation = -30 * (float)Math.PI / 180;

        
            Receiver.Enabled = IsWorking;

            UpdateEmissivity();
            UpdateMyStateText();

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        protected float NormalizeAngle(int angle)
        {
            int retVal = angle % 360;
            if (retVal == 0 && angle != 0)
            {
                return 360;
            }
            return retVal;
        }

        protected void ClampRotationAndElevation()
        {
            var newRotation = ClampRotation(m_rotation);
            var newElevation = ClampElevation(m_elevation);

            if (newRotation != m_rotation || newElevation != m_elevation)
                m_outsideLimits = true;
            else
                m_outsideLimits = false;

            m_rotation = newRotation;
            m_elevation = newElevation;
        }

        private float ClampRotation(float value)
        {
            if (IsRotationLimited())
            {
                value = Math.Min(m_maxAzimuthRadians, Math.Max(m_minAzimuthRadians, value));
            }
            return value;
        }

        private bool IsRotationLimited()
        {
            return Math.Abs((m_maxAzimuthRadians - m_minAzimuthRadians) - MathHelper.TwoPi) > 0.01;
        }

        private float ClampElevation(float value)
        {
            if (IsElevationLimited())
            {
                value = Math.Min(m_maxElevationRadians, Math.Max(Math.Max(m_minElevationRadians, m_MinElevation), value));
            }
            return value;
        }

        private bool IsElevationLimited()
        {
            return Math.Abs((m_maxElevationRadians - Math.Max(m_minElevationRadians, m_MinElevation)) - MathHelper.TwoPi) > 0.01;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            MySession.Static.LaserAntennas.Add(EntityId, this);
            ResourceSink.Update();
            UpdateEmissivity();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateEmissivity();
        }

        protected override void OnOwnershipChanged()
        {
            base.OnOwnershipChanged();
            Receiver.UpdateBroadcastersInRange();
            UpdateVisuals();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_LaserAntenna objectBuilder = (MyObjectBuilder_LaserAntenna)base.GetObjectBuilderCubeBlock(copy);
            objectBuilder.State=(byte)(((int)State) | (m_IsPermanent?8:0));
            objectBuilder.targetEntityId = m_targetId;
            objectBuilder.gpsTarget = m_termGpsCoords;
            objectBuilder.gpsTargetName = m_termGpsName.ToString();
            objectBuilder.HeadRotation=new Vector2(m_rotation,m_elevation);
            objectBuilder.LastTargetPosition = m_targetCoords;
            objectBuilder.LastKnownTargetName = m_lastKnownTargetName.ToString();
            return objectBuilder;
        }
        
        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
			if (!(Enabled && IsFunctional && ResourceSink.SuppliedRatioByType(MyResourceDistributorComponent.ElectricityId) > 0.99f))
                return;
            if (State != StateEnum.idle)
                GetRotationAndElevation(m_targetCoords, ref m_needRotation, ref m_needElevation);
            RotationAndElevation(m_needRotation, m_needElevation);
            TryLaseTargetCoords();
        }
        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();
            if (!(Enabled && IsFunctional && ResourceSink.SuppliedRatioByType(MyResourceDistributorComponent.ElectricityId) > 0.99f))
                return;
            TryUpdateTargetCoords();
        }
        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            if (!(Enabled && IsFunctional && ResourceSink.SuppliedRatioByType(MyResourceDistributorComponent.ElectricityId) > 0.99f))
                return;
            Receiver.UpdateBroadcastersInRange();
            TryUpdateTargetCoords();
            m_canLaseTargetCoords = false;
            switch (State)
            {
                case StateEnum.idle:
                    break;
                case StateEnum.rot_GPS:
                    if (m_rotationFinished)
                        ShiftModeSync(StateEnum.search_GPS);
                    break;
                case StateEnum.search_GPS:
                    if (!m_rotationFinished)//rotation lost
                    {
                        ShiftModeSync(StateEnum.rot_GPS);
                        break;
                    }
                    //search for laser there
                    if (Sync.MultiplayerActive && !Sync.IsServer)
                        break;//MP server makes connection
                    MyLaserAntenna anyLaser=null;
                    double dist2=double.MaxValue;
                    float minDist2 = float.MaxValue;
                    bool PermanentExists = false;
                    MyLaserAntenna permanentFound = null;
                    foreach (var laser in MySession.Static.LaserAntennas)
                    {
                        MyLaserAntenna other = laser.Value;
                        if (!(other.Enabled && other.IsFunctional && other.ResourceSink.SuppliedRatioByType(MyResourceDistributorComponent.ElectricityId) > 0.99f))
                            continue;
                        if (other.m_IsPermanent && PermanentExists)//&&already found one
                            continue;
                        //is mine/factions?
                        if (!Receiver.CanIUseIt(other.Broadcaster, this.OwnerId)
                            || !other.Receiver.CanIUseIt(Broadcaster, other.OwnerId))
                            continue;
                        if (other.EntityId == this.EntityId)
                            continue;//thats me!
                        dist2 = other.Dist2To(m_targetCoords);
                        if (dist2<10*10)
                        {
                            if (other.m_IsPermanent)
                            {
                                PermanentExists = true;
                                permanentFound = laser.Value;
                                continue;
                            }
                            if (other.State == StateEnum.idle)
                            {
                                anyLaser = other;
                                break;//search no more, this one is enough
                            }
                            if (dist2 < minDist2)
                            {
                                dist2 = minDist2;
                                anyLaser = other;
                            }
                        }
                    }
                    if (anyLaser == null)//nothing available
                    {
                        if (m_OnlyPermanentExists)
                        {
                            if(!PermanentExists)
                            {
                                m_OnlyPermanentExists = false;
                                UpdateText();
                            }
                        }
                        else
                        {
                            if (PermanentExists && IsInRange(permanentFound) && LosTests(permanentFound))
                            {
                                m_OnlyPermanentExists = true;
                                UpdateText();
                            }
                        }
                        break;
                    }
                    if (!IsInRange(anyLaser))
                        break;
                    if (!LosTests(anyLaser))
                        break;//no visibility. Sorry
                    //modify connection to connect to receiver:
                    ConnectToRec(anyLaser.EntityId);
                    break;
                case StateEnum.rot_Rec:
                    if (m_rotationFinished)
                        ShiftModeSync(StateEnum.contact_Rec);
                    break;
                case StateEnum.contact_Rec:
                    if (m_targetId == null)
                        break;//legal - laser could be destroyed
                    if (!m_rotationFinished)//rotation lost
                    {
                        ShiftModeSync(StateEnum.rot_Rec);
                        break;
                    }
                    MyLaserAntenna target=GetLaserById((long)m_targetId);
                    //Debug.Assert(target!=null,"Trying to contact NULL laser");
                    if (target!=null &&
                        (target.State == StateEnum.contact_Rec || target.State == StateEnum.connected || target.State == StateEnum.rot_Rec) &&
                        target.m_targetId == EntityId &&
                        target.Enabled && target.IsFunctional && target.ResourceSink.SuppliedRatioByType(MyResourceDistributorComponent.ElectricityId) > 0.99f &&
                        IsInRange(target)
                        )
                    {
                        if (!Receiver.CanIUseIt(target.Broadcaster, this.OwnerId)
                            || !target.Receiver.CanIUseIt(Broadcaster, target.OwnerId))
                            break;
                        if (target.Dist2To(this.m_targetCoords) > 10 * 10)//is target still where expected?
                            break;
                        if (LosTests(target))
                        {
                            if (this.Dist2To(target.m_targetCoords) > 10 * 10//I am not where I am expected - just push my updated coordinates
                                || !target.m_rotationFinished)               //next tick he will be rotating with unfinished rotation....
                            {
                                SetupLaseTargetCoords();
                                target.m_targetCoords = HeadPos;
                                target.m_rotationFinished = false;
                                break;
                            }
                            ShiftModeSync(StateEnum.connected);
                        }
                    }
                    break;
                case StateEnum.connected:
                    //if rotation lost go to rot_Rec
                    if (!m_rotationFinished)
                        ShiftModeSync(StateEnum.rot_Rec);
                    if (m_targetId == null)
                        ShiftModeSync(StateEnum.contact_Rec);//other side MIA - legal - laser could be destroyed
                    target = GetLaserById((long)m_targetId);
                    if (target == null
                        || target.m_targetId != EntityId
                        || target.State != StateEnum.connected
                        || (!(target.Enabled && target.IsFunctional && target.ResourceSink.SuppliedRatioByType(MyResourceDistributorComponent.ElectricityId) > 0.99f))
                        || !target.m_rotationFinished
                        || !IsInRange(target)
                        || !Receiver.CanIUseIt(target.Broadcaster, this.OwnerId)
                        || !target.Receiver.CanIUseIt(Broadcaster, target.OwnerId)
                        || !LosTest(target.HeadPos)//target will make other half of line in its update
                        )
                        ShiftModeSync(StateEnum.contact_Rec);//other side MIA
                    else
                    {
                        m_targetCoords = target.HeadPos;
                        m_canLaseTargetCoords = true;
                    }
                    break;
            }
        }

        protected bool m_canLaseTargetCoords=false;
        protected void SetupLaseTargetCoords()
        {
            m_canLaseTargetCoords = false;
            if (!m_rotationFinished)
                return;
            if (!m_wasVisible)
                return;
            if (m_targetId == null)
                return;
            var target = GetLaserById((long)m_targetId);
            if (target == null
                        || target.m_targetId != EntityId
                        || (!(target.Enabled && target.IsFunctional && target.ResourceSink.SuppliedRatioByType(MyResourceDistributorComponent.ElectricityId) > 0.99f))
                        || !IsInRange(target)
                        || !Receiver.CanIUseIt(target.Broadcaster, this.OwnerId)
                        || !target.Receiver.CanIUseIt(Broadcaster, target.OwnerId)
                        )
                return;
            m_canLaseTargetCoords = true;
        }

        protected void TryLaseTargetCoords()
        {//tries to beam my coordinates to current target
            if (!m_canLaseTargetCoords)
                return;
            if (m_targetId == null)
                return;
            var target = GetLaserById((long)m_targetId);
            if (target != null)
                target.m_targetCoords = HeadPos;
        }

        protected void TryUpdateTargetCoords()
        {//tries to update target position over radio
            if (m_targetId!=null)
            {
                var target = GetLaserById((long)m_targetId);
                if (target != null)
                {
                    if (target.Enabled && target.IsFunctional && ResourceSink.SuppliedRatioByType(MyResourceDistributorComponent.ElectricityId) > 0.99f)
                        if (target.m_targetId != EntityId)
                        {
                            ShiftModeSync(StateEnum.idle);
                            return;//partner cheated us - we did not heard of each other for a while and he is with someone else now :-(
                        }
                    if (Receiver.RelayedBroadcasters.Contains(target.Broadcaster))
                    {
                        m_targetCoords = target.HeadPos;
                        if (0 != m_lastKnownTargetName.CompareTo(target.CustomName))
                        {
                            m_lastKnownTargetName.Clear().Append(target.CustomName);
                            UpdateMyStateText();
                        }
                    }
                }
            }

        }

        private double Dist2To(Vector3D? here)
        {
            if (here!=null)
                return Vector3D.DistanceSquared((Vector3D)here, HeadPos);
            return float.MaxValue;
        }
        protected bool IsInRange(MyLaserAntenna target)
        {
            float maxRange=(target.m_maxRange+m_maxRange)*0.5f;
            if (Dist2To(target.HeadPos) > maxRange * maxRange)
                return false;
            return true;
        }

        public MyLaserAntenna GetOther()
        {
            if (State == StateEnum.connected)
            {
                MyLaserAntenna la = GetLaserById((long)m_targetId);
                return la;
            }
            return null;
        }
        public MyLaserBroadcaster GetOthersBroadcaster()
        {
            MyLaserAntenna la=GetOther();
            if (la != null)
                return la.Broadcaster;
            return null;
        }

        public void AddBroadcastersContactingMe(ref HashSet<MyDataBroadcaster> broadcasters)
        {//adds all broadcasters trying to contact me
        //these will be received and updated, but they do not relay information about others (only two way established link can do that)
            foreach (var laser in MySession.Static.LaserAntennas)
            {
                if (laser.Key == this.EntityId)
                    continue;
                if (!(laser.Value.Enabled && laser.Value.IsFunctional && laser.Value.ResourceSink.SuppliedRatioByType(MyResourceDistributorComponent.ElectricityId) > 0.99f))
                    continue;
                if (laser.Value.m_targetId != EntityId)
                    continue;
                if(laser.Value.State==StateEnum.contact_Rec)
                    if (Dist2To(laser.Value.m_targetCoords) < 10 * 10)//I am still where he expects
                        if (!broadcasters.Contains(laser.Value.Broadcaster))
                            broadcasters.Add((MyDataBroadcaster)Broadcaster);
            }
        }

        protected override void OnEnabledChanged()
        {
			ResourceSink.Update();
            if (Enabled && State == StateEnum.connected)
                ShiftModeSync(StateEnum.rot_Rec);
            Receiver.UpdateBroadcastersInRange();
            base.OnEnabledChanged();
        }

        protected override void OnStopWorking()
        {
            UpdateEmissivity();
            base.OnStopWorking();
        }

        protected override void OnStartWorking()
        {
            UpdateEmissivity();
            base.OnStartWorking();
        }

        protected override bool CheckIsWorking()
        {
			return base.CheckIsWorking() && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId);
        }

        void IsPoweredChanged()
        {
            UpdateIsWorking();
            if (State == StateEnum.connected && !IsWorking)
                ShiftModeSync(StateEnum.rot_Rec);
            if (Receiver!=null)
                Receiver.Enabled = IsWorking;
            m_rotationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            UpdateText();
            UpdateEmissivity();
        }

        void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
            UpdateText();
            UpdateEmissivity();
        }

        internal override void OnIntegrityChanged(float buildIntegrity, float integrity, bool setOwnership, long owner, MyOwnershipShareModeEnum sharing = MyOwnershipShareModeEnum.Faction)
        {
            base.OnIntegrityChanged(buildIntegrity, integrity, setOwnership, owner, sharing);
            m_termGpsCoords = null;
            m_termGpsName.Clear();
            ShiftModeSync(StateEnum.idle);
        }

        public void OnClosed()
        {
            MySession.Static.LaserAntennas.Remove(EntityId);
        }

        float UpdatePowerInput()
        {
            float input=0.0f;
            switch (State)
            {
                case StateEnum.idle:
                    if (!m_rotationFinished)
                        input = BlockDefinition.PowerInputTurning;
                    else
                        input = BlockDefinition.PowerInputIdle;
                    break;
                case StateEnum.rot_GPS:
                    input = BlockDefinition.PowerInputTurning;
                    break;
                case StateEnum.rot_Rec:
                    input = BlockDefinition.PowerInputTurning;
                    break;
                case StateEnum.search_GPS:
                    input = BlockDefinition.PowerInputLasing;
                    break;
                case StateEnum.contact_Rec:
                    input = BlockDefinition.PowerInputLasing;
                    break;
                case StateEnum.connected:
                    input = BlockDefinition.PowerInputLasing;
                    break;
            }
            UpdateText();
            return (Enabled && IsFunctional) ? input : 0.0f;
        }

        private void UpdateEmissivity()
        {
            if (!InScene || m_base2 == null || m_base2.Render == null)
                return;
            if (!IsWorking)
            {
                UpdateNamedEmissiveParts(m_base2.Render.RenderObjectIDs[0], "Emissive", Color.Red, 0);
                return;
            }
            switch (State)
            {
                case StateEnum.idle:
                    UpdateNamedEmissiveParts(m_base2.Render.RenderObjectIDs[0], "Emissive", Color.Green, 1);
                    break;
                case StateEnum.rot_GPS:
                case StateEnum.rot_Rec:
                    UpdateNamedEmissiveParts(m_base2.Render.RenderObjectIDs[0], "Emissive", Color.Yellow, 1);
                    break;
                case StateEnum.connected:
                    UpdateNamedEmissiveParts(m_base2.Render.RenderObjectIDs[0], "Emissive", Color.SteelBlue, 1);
                    break;
                default:
                    UpdateNamedEmissiveParts(m_base2.Render.RenderObjectIDs[0], "Emissive", Color.GreenYellow, 1);
                    return;
            }
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) ? ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0, DetailedInfo);
            DetailedInfo.Append("\n");
            if (!Enabled)
            {

            }
            else
            {
                switch (State)
                {
                    case StateEnum.idle:
                        DetailedInfo.Append(MyTexts.Get(MySpaceTexts.LaserAntennaModeIdle));
                        break;
                    case StateEnum.rot_GPS:
                        DetailedInfo.Append(MyTexts.Get(MySpaceTexts.LaserAntennaModeRotGPS));
                        break;
                    case StateEnum.search_GPS:
                        DetailedInfo.Append(MyTexts.Get(MySpaceTexts.LaserAntennaModeSearchGPS));
                        if (m_OnlyPermanentExists)
                        {
                            DetailedInfo.Append("\n");
                            DetailedInfo.Append(MyTexts.Get(MySpaceTexts.LaserAntennaOnlyPerm));
                        }
                        break;
                    case StateEnum.rot_Rec:
                        DetailedInfo.Append(MyTexts.Get(MySpaceTexts.LaserAntennaModeRotRec));
                        DetailedInfo.Append(m_lastKnownTargetName);
                        break;
                    case StateEnum.contact_Rec:
                        DetailedInfo.Append(MyTexts.Get(MySpaceTexts.LaserAntennaModeContactRec));
                        DetailedInfo.Append(m_lastKnownTargetName);
                        break;
                    case StateEnum.connected:
                        DetailedInfo.Append(MyTexts.Get(MySpaceTexts.LaserAntennaModeConnectedTo));
                        DetailedInfo.Append(m_lastKnownTargetName);
                        break;
                }
                if (m_outsideLimits)
                {
                    DetailedInfo.Append("\n");
                    DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.LaserAntennaOutsideLimits));
                }
            }
            RaisePropertiesChanged();
        }
        
        protected void SetIdle()
        {
            ChangeModeSync(StateEnum.idle);
            receiversList.UpdateVisual();
        }

        protected void ConnectToId()
        {
            Debug.Assert(m_selectedEntityId != null);
            ConnectToRec((long)m_selectedEntityId);
        }
        protected void ConnectToGps()
        {
            ChangeModeSync(StateEnum.rot_GPS);
        }

        internal void ChangeMode(StateEnum Mode)
        {//server side/SP only
            switch (Mode)
            {
                case StateEnum.idle:
                    m_state = StateEnum.idle;//to avoid recursion
                    IdleOther();
                    break;
                case StateEnum.rot_GPS:
                    m_state = StateEnum.idle;//to avoid recursion
                    IdleOther();
                    break;
            }
            DoChangeMode(Mode);
            Receiver.UpdateBroadcastersInRange();
        }

        internal void DoChangeMode(StateEnum Mode)
        {
            State = Mode;
            m_OnlyPermanentExists = false;
            Receiver.UpdateBroadcastersInRange();
            if (MySession.Static.LocalCharacter != null)
                MySession.Static.LocalCharacter.RadioReceiver.UpdateBroadcastersInRange();
            receiversList.UpdateVisual();
            if(m_targetId!=null)
            {
                var laser = GetLaserById((long)m_targetId);
                if (laser!=null)
                {
                    laser.UpdateVisual();
					laser.ResourceSink.Update();
                }
            }
			ResourceSink.Update();
            UpdateVisual();
            UpdateText();
            UpdateEmissivity();
            UpdateMyStateText();
            
            switch (Mode)
            {
                case StateEnum.idle:
                    m_needRotation=0;
                    m_needElevation=0;
                    m_rotationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                    m_lastKnownTargetName.Clear();
                    m_IsPermanent = false;
                    break;
                /*case StateEnum.connected:
                    break;*/
                case StateEnum.rot_GPS:
                    Debug.Assert(m_termGpsCoords!=null,"rotating to NULL");
                    m_targetCoords = (Vector3D)m_termGpsCoords;
                    m_lastKnownTargetName.Clear().Append(m_termGpsName).Append(" ").Append(m_termGpsCoords);
                    m_IsPermanent = false;
                    break;
                /*case StateEnum.rot_Rec:
                    break;
                case StateEnum.search_GPS:
                    break;
                case StateEnum.contact_Rec:
                    break;*/
            }
        }
        protected bool IsInContact(MyLaserAntenna la)
        {
            if (la == null)
                return false;
            return Receiver.RelayedBroadcasters.Contains(la.Broadcaster);
        }
        protected bool IdleOther()
        {//switchech other side of link to idle if possible
            if (m_targetId != null)
            {
                var other=GetLaserById((long)m_targetId);
                if (other == null)
                    return false;
				if (!(other.Enabled && other.IsFunctional && other.ResourceSink.SuppliedRatioByType(MyResourceDistributorComponent.ElectricityId) > 0.99f))
                    return false;
                if (other.State == StateEnum.idle)
                    return true;
                if (other.m_targetId == EntityId)
                {
                    if (IsInContact(other))
                    {
                        //inform other side that we are breaking connection, switch it to idle
                        other.ChangeModeSync(StateEnum.idle);
                        return true;
                    }
                    //other side not in contact, bad luck, will stay searching for us
                    //other side's update on server will switch it
                }
            }
            return true;
        }
        internal bool ConnectTo(long DestId)
        {//server side/SP only
            MyLaserAntenna target = GetLaserById(DestId);
            if (target == null)
                return false;
            if (!Receiver.CanIUseIt(target.Broadcaster, this.OwnerId)
                || !target.Receiver.CanIUseIt(Broadcaster, target.OwnerId))
                return false;
            IdleOther();
            DoConnectTo(DestId);
            //if hes not already pointing at me:
            if (target!=null && target.m_targetId!=EntityId)
            {
                //other.IdleOther();
                target.ConnectToRec(EntityId);
            }
            return true;
        }
        internal void DoConnectTo(long DestId)
        {
            State = StateEnum.rot_Rec;
            m_IsPermanent = false;
            m_targetId = DestId;
            m_rotationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            MyLaserAntenna la = GetLaserById(DestId);
            if (la != null)
            {
                m_targetCoords = la.HeadPos;
                m_lastKnownTargetName.Clear().Append(la.CustomName);
            }
            else
            {//now this will never happen in normal game (server checks if target exists), but some mod can and probably did screw it
                Debug.Assert(false, "DoConnectTo connecting to nonexistent entity");
                m_targetCoords = Vector3D.Zero;
                m_lastKnownTargetName.Clear().Append("???");
            }
			ResourceSink.Update();
            Receiver.UpdateBroadcastersInRange();
            UpdateVisuals();
            UpdateText();
            UpdateEmissivity();
            UpdateMyStateText();
        }

        internal bool DoSetIsPerm(bool isPerm)
        {
            if (m_IsPermanent != isPerm)
            {
                if (State != StateEnum.connected)
                    return false;
                //set other to same value too:
                if (m_targetId == null)
                    return false;
                var other = GetLaserById((long)m_targetId);
                if (other == null)
                    return false;

                other.m_IsPermanent = isPerm;
                other.UpdateMyStateText();
                m_IsPermanent = isPerm;
                UpdateMyStateText();
                return true;
            }
            return false;
        }

        protected static MyLaserAntenna GetLaserById(long id)
        {
            
            MyEntity entity = null;
            MyEntities.TryGetEntityById(id, out entity);
            MyLaserAntenna laser = entity as MyLaserAntenna;
            //System.Diagnostics.Debug.Assert(laser != null, "Laser is null");
            return laser;
        }

        //----------------------------------------------
        private static List<MyPhysics.HitInfo> m_hits=new List<MyPhysics.HitInfo>();
        bool m_wasVisible=false;
        protected bool LosTests(MyLaserAntenna la)
        {
            m_wasVisible = true;
            if (!LosTest(la.HeadPos))
                la.m_wasVisible = false;
            if (m_wasVisible)
                m_wasVisible=la.LosTest(HeadPos);
            return m_wasVisible;
        }
        protected bool LosTest(Vector3D target)
        {//LOS test from me to half of distance to target, with maximum
            if (!m_needLineOfSight)
                return true;

            if (Vector3D.DistanceSquared(HeadPos, target) > m_Max_LosDist * m_Max_LosDist * 4)
                target = HeadPos + Vector3D.Normalize(target - HeadPos) * m_Max_LosDist;
            else
                target = (HeadPos + target) * 0.5f;
            LineD l = new LineD(target, HeadPos);
            m_hits.Clear();
            MyPhysics.CastRay(l.From, l.To, m_hits);
            foreach (var hit in m_hits)
            {
                var ent = hit.HkHitInfo.GetHitEntity();
                if (ent != CubeGrid)
                {
                    m_wasVisible = false;
                    return false;
                }
                else
                {
                    var grid = ent as MyCubeGrid;
                    var pos = grid.RayCastBlocks(l.From, l.To);
                    if (pos.HasValue && grid.GetCubeBlock(pos.Value) != SlimBlock)
                    {
                        m_wasVisible = false;
                        return false;
                    }
                }
            }
            return true;
        }

        //------------------- rotation
        float m_rotation=0;
        float m_elevation=0;

        protected MyEntity m_base1;
        protected MyEntity m_base2;

        private Vector3 LookAt(Vector3D target)
        {
            MatrixD m = MatrixD.CreateLookAt(GetWorldMatrix().Translation, target, GetWorldMatrix().Up);
            //MatrixD m = MatrixD.CreateLookAt(GetWorldMatrix().Translation, target, MatrixD.Identity.Up); 
            //MatrixD m = MatrixD.CreateLookAt(MatrixD.Identity.Translation, target, MatrixD.Identity.Up); 

            m = MatrixD.Invert(m);
            m = MatrixD.Normalize(m);
            m *= MatrixD.Invert(MatrixD.Normalize(InitializationMatrixWorld));

            Quaternion rot = Quaternion.CreateFromRotationMatrix(m);
            return MyMath.QuaternionToEuler(rot);
        }

        protected void ResetRotation()
        {
            m_rotation = 0;
            m_elevation = 0;
            ClampRotationAndElevation();
            m_rotationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }
        public override void OnModelChange()
        {
            base.OnModelChange();

            if (IsFunctional)
            {
                m_base1 = Subparts["LaserComTurret"];
                m_base2 = m_base1.Subparts["LaserCom"];
            }
            else
            {
                m_base1 = null;
                m_base2 = null;
            }
            UpdateEmissivity();
        }
        MatrixD InitializationMatrixWorld
        {
            get
            {
                return InitializationMatrix * Parent.WorldMatrix;
            }
        }
        protected void RotateModels()
        {
            ClampRotationAndElevation();

            if (m_base1 == null || m_base2 == null)
                return;

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyLargeShipGunBase::RotateModels");

            Matrix m = (Matrix)InitializationMatrixWorld;
            Vector3D trans = m_base1.WorldMatrix.Translation;
            Matrix.CreateRotationY(m_rotation, out m);
            m.Translation = m_base1.PositionComp.LocalMatrix.Translation;
            //m *= Matrix.CreateFromAxisAngle(InitializationMatrixWorld.Up, m_rotation);
            //m.Translation = trans;
            m_base1.PositionComp.LocalMatrix = m;

            Matrix.CreateRotationX(m_elevation, out m);
            m.Translation = m_base2.PositionComp.LocalMatrix.Translation;
            m_base2.PositionComp.LocalMatrix = m;

            //m_barrel.WorldPositionChanged();
          
            
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        protected int m_rotationInterval_ms;
        protected int m_elevationInterval_ms;
        protected float m_MinElevation=-20*(float)Math.PI/180;

        protected void GetRotationAndElevation(Vector3D target, ref float needRotation, ref float needElevation)
        {
            Vector3 lookAtPositionEuler = Vector3.Zero;
            lookAtPositionEuler = LookAt(target);
            // real rotation:
            needRotation = lookAtPositionEuler.Y;
            needElevation = lookAtPositionEuler.X;
        }

        public bool RotationAndElevation(float needRotation, float needElevation)
        {
            float step = BlockDefinition.RotationRate * (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_rotationInterval_ms);

            float diffRot = needRotation - m_rotation;

            if (diffRot > MathHelper.Pi)
                diffRot = diffRot - MathHelper.TwoPi;
            else
                if (diffRot < -MathHelper.Pi)
                    diffRot = diffRot + MathHelper.TwoPi;


            float diffRotAbs = Math.Abs(diffRot);

            //bool needUpdateMatrix = false;

            if (diffRotAbs > 0.001f)
            {
                float value = MathHelper.Clamp(step, float.Epsilon, diffRotAbs);
                m_rotation += diffRot > 0 ? value : -value;
                //needUpdateMatrix = true;
            }
            else
            {
                m_rotation = needRotation;
            }

            if (m_rotation > MathHelper.Pi)
                m_rotation = m_rotation - MathHelper.TwoPi;
            else
                if (m_rotation < -MathHelper.Pi)
                    m_rotation = m_rotation + MathHelper.TwoPi;


            // real elevation:
            step = BlockDefinition.RotationRate * (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_elevationInterval_ms);

            float diffElev = needElevation - m_elevation;
            float diffElevAbs = Math.Abs(diffElev);

            if (diffElevAbs > 0.001f)
            {
                float value = MathHelper.Clamp(step, float.Epsilon, diffElevAbs);
                m_elevation += diffElev > 0 ? value : -value;
                //needUpdateMatrix = true;
            }
            else
            {
                m_elevation = needElevation;
            }

            m_elevationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_rotationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            //  if (needUpdateMatrix)
            {
                // rotate models by rotation & elevation:
                RotateModels();
            }

            float stapR = Math.Abs(Math.Abs(needRotation) - Math.Abs(m_rotation));
            float stapE = Math.Abs(Math.Abs(needElevation) - Math.Abs(m_elevation));
            m_rotationFinished = (stapR <= float.Epsilon && stapE <= float.Epsilon);
            return m_rotationFinished;
        }

        #region Network

        #region paste coords
        private void PasteCoordinates(string coords)
        {
            if (!Sync.MultiplayerActive)
                DoPasteCoords(coords);
            else
                MyMultiplayer.RaiseEvent(this, x => x.PasteCoordinatesSuccess, coords);
        }

        [Event, Reliable, Server, Broadcast]
        void PasteCoordinatesSuccess(string coords)
        {
            DoPasteCoords(coords);
        }

        #endregion //  paste coords

        #region change permanent

        private void ChangePerm(bool isPerm)
        {
            if (!Sync.MultiplayerActive)
                DoSetIsPerm(isPerm);
            else
                MyMultiplayer.RaiseEvent(this, x => x.ChangePermRequest, isPerm);
        }

        [Event, Reliable, Server]
        void ChangePermRequest(bool isPerm)
        {
            if (DoSetIsPerm(isPerm))
                MyMultiplayer.RaiseEvent(this, x => x.ChangePermSuccess, isPerm);
        }

        [Event, Reliable, Broadcast]
        void ChangePermSuccess(bool isPerm)
        {
            DoSetIsPerm(isPerm);
        }

        #endregion // change permanent

        #region change mode
        private void ChangeModeSync(StateEnum Mode)
        {
            ChangeMode(Mode, true);
        }
        private void ShiftModeSync(StateEnum Mode)//same as ChengeMode but MP client side will not be propagated to server
        {
            ChangeMode(Mode, false);
        }

        private void ChangeMode(StateEnum mode, bool UploadFromClient)
        {
            if (!Sync.MultiplayerActive)
                ChangeMode(mode);
            else if (UploadFromClient || Sync.IsServer)
                MyMultiplayer.RaiseEvent(this, x => x.OnChangeModeRequest, mode);
        }

        [Event, Reliable, Server, Broadcast]
        void OnChangeModeRequest(StateEnum mode)
        {
            ChangeMode(mode);
        }

        #endregion // change mode

        #region change dest

        public void ConnectToRec(long TgtReceiver)
        {
            if (!Sync.MultiplayerActive)
                ConnectTo(TgtReceiver);
            else
                MyMultiplayer.RaiseEvent(this, x => x.OnConnectToRecRequest, TgtReceiver);
        }

        [Event, Reliable, Server]
        void OnConnectToRecRequest(long targetEntityId)
        {
            if(ConnectTo(targetEntityId))
                MyMultiplayer.RaiseEvent(this, x => x.OnConnectToRecSuccess, targetEntityId);
        }

        [Event, Reliable, Broadcast]
        void OnConnectToRecSuccess(long targetEntityId)
        {
            DoConnectTo(targetEntityId);
        }

        #endregion // change dest

        #endregion

    }
}
