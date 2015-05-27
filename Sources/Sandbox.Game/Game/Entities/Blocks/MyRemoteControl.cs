using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Text;

using VRageMath;
using VRage;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Localization;
using VRage.Utils;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using Sandbox.Graphics.GUI;
using VRage.Trace;
using System;
using Sandbox.Engine.Multiplayer;
using SteamSDK;
using ProtoBuf;
using Sandbox.Game.Screens.Helpers;
using System.Diagnostics;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_RemoteControl))]
    class MyRemoteControl : MyShipController, IMyPowerConsumer, IMyUsableEntity, IMyRemoteControl
    {
        public enum FlightMode : int
        {
            Patrol = 0,
            Circle = 1,
            OneWay = 2,
        }

        public class MyWaypoint
        {
            public Vector3D Coords;
            public string Name;

            public MyWaypoint(Vector3D coords, string name)
            {
                Coords = coords;
                Name = name;
            }

            public MyWaypoint(IMyGps gps)
            {
                Coords = gps.Coords;
                Name = gps.Name;
            }
        }

        [ProtoContract]
        public struct ToolbarItem : IEqualityComparer<ToolbarItem>
        {
            [ProtoMember]
            public long EntityID;
            [ProtoMember]
            public string GroupName;
            [ProtoMember]
            public string Action;

            public bool Equals(ToolbarItem x, ToolbarItem y)
            {
                if (x.EntityID != y.EntityID || x.GroupName != y.GroupName || x.Action != y.Action)
                    return false;
                return true;
            }

            public int GetHashCode(ToolbarItem obj)
            {
                unchecked
                {
                    int result = obj.EntityID.GetHashCode();
                    result = (result * 397) ^ obj.GroupName.GetHashCode();
                    result = (result * 397) ^ obj.Action.GetHashCode();
                    return result;
                }
            }
        }

        private const float MAX_TERMINAL_DISTANCE_SQUARED = 10.0f;

        private float m_powerNeeded = 0.01f;
        private long? m_savedPreviousControlledEntityId;
        private IMyControllableEntity m_previousControlledEntity;

        public IMyControllableEntity PreviousControlledEntity
        {
            get
            {
                if (m_savedPreviousControlledEntityId != null)
                {
                    if (TryFindSavedEntity())
                    {
                        m_savedPreviousControlledEntityId = null;
                    }
                }
                return m_previousControlledEntity;
            }
            private set
            {
                if (value != m_previousControlledEntity)
                {
                    if (m_previousControlledEntity != null)
                    {
                        m_previousControlledEntity.Entity.OnMarkForClose -= Entity_OnPreviousMarkForClose;

                        var cockpit = m_previousControlledEntity.Entity as MyCockpit;
                        if (cockpit != null && cockpit.Pilot != null)
                        {
                            cockpit.Pilot.OnMarkForClose -= Entity_OnPreviousMarkForClose;
                        }
                    }
                    m_previousControlledEntity = value;
                    if (m_previousControlledEntity != null)
                    {
                        AddPreviousControllerEvents();
                    }
                    UpdateEmissivity();
                }
            }
        }

        private MyCharacter cockpitPilot = null;
        public override MyCharacter Pilot
        {
            get
            {
                var character = PreviousControlledEntity as MyCharacter;
                if (character != null)
                {
                    return character;
                }
                return cockpitPilot;
            }
        }

        private new MyRemoteControlDefinition BlockDefinition
        {
            get { return (MyRemoteControlDefinition)base.BlockDefinition; }
        }

        public MyPowerReceiver PowerReceiver
        {
            get;
            protected set;
        }

        private List<MyWaypoint> m_waypoints;
        private MyWaypoint m_currentWaypoint;
        private bool m_autoPilotEnabled;
        private FlightMode m_currentFlightMode;
        private bool m_patrolDirectionForward = true;
        private Vector3D m_startPosition;

        private static List<MyToolbar> m_openedToolbars;
        private static bool m_shouldSetOtherToolbars;

        private List<ToolbarItem> m_items;
        public MyToolbar AutoPilotToolbar { get; set; }

        static MyRemoteControl()
        {
            var controlBtn = new MyTerminalControlButton<MyRemoteControl>("Control", MySpaceTexts.ControlRemote, MySpaceTexts.Blank, (b) => b.RequestControl());
            controlBtn.Enabled = r => r.CanControl();
            controlBtn.SupportsMultipleBlocks = false;
            var action = controlBtn.EnableAction(MyTerminalActionIcons.TOGGLE);
            if (action != null)
            {
                action.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.ButtonPanel };
                action.ValidForGroups = false;
            }
            MyTerminalControlFactory.AddControl(controlBtn);

            
            var autoPilotSeparator = new MyTerminalControlSeparator<MyRemoteControl>();
            MyTerminalControlFactory.AddControl(autoPilotSeparator);

            //TODO(AF) sync
            var autoPilot = new MyTerminalControlCheckbox<MyRemoteControl>("AutoPilot", MySpaceTexts.BlockPropertyTitle_AutoPilot, MySpaceTexts.Blank);
            autoPilot.Getter = (x) => x.m_autoPilotEnabled;
            autoPilot.Setter = (x, v) => x.SetAutoPilotEnabled(v);
            autoPilot.Enabled = r => r.CanEnableAutoPilot();
            autoPilot.EnableAction();
            MyTerminalControlFactory.AddControl(autoPilot);

            var flightMode = new MyTerminalControlCombobox<MyRemoteControl>("FlightMode", MySpaceTexts.BlockPropertyTitle_FlightMode, MySpaceTexts.Blank);
            flightMode.ComboBoxContent = (x) => FillFlightModeCombo(x);
            flightMode.Getter = (x) => (long)x.m_currentFlightMode;
            flightMode.Setter = (x, v) => x.ChangeFlightMode((FlightMode)v);
            MyTerminalControlFactory.AddControl(flightMode);

            var waypointList = new MyTerminalControlListbox<MyRemoteControl>("WaypointList", MySpaceTexts.BlockPropertyTitle_Waypoints, MySpaceTexts.Blank);
            waypointList.ListContent = (x, list1, list2) => x.FillWaypointList(list1, list2);
            waypointList.ItemSelected = (x, y) => x.SelectWaypoint(y, true);
            MyTerminalControlFactory.AddControl(waypointList);



            var removeBtn = new MyTerminalControlButton<MyRemoteControl>("RemoveWaypoint", MySpaceTexts.BlockActionTitle_RemoveWaypoint, MySpaceTexts.Blank, (b) => b.RemoveWaypoint());
            removeBtn.Enabled = r => r.CanRemove();
            removeBtn.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(removeBtn);

            var moveUp = new MyTerminalControlButton<MyRemoteControl>("MoveUp", MySpaceTexts.BlockActionTitle_MoveWaypointUp, MySpaceTexts.Blank, (b) => b.MoveUp());
            moveUp.Enabled = r => r.CanMoveUp();
            moveUp.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(moveUp);

            var moveDown = new MyTerminalControlButton<MyRemoteControl>("MoveDown", MySpaceTexts.BlockActionTitle_MoveWaypointDown, MySpaceTexts.Blank, (b) => b.MoveDown());
            moveDown.Enabled = r => r.CanMoveDown();
            moveDown.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(moveDown);

            var addButton = new MyTerminalControlButton<MyRemoteControl>("AddWaypoint", MySpaceTexts.BlockActionTitle_AddWaypoint, MySpaceTexts.Blank, (b) => b.AddWaypoint());
            addButton.Enabled = r => r.CanAdd();
            addButton.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(addButton);



            var gpsList = new MyTerminalControlListbox<MyRemoteControl>("GpsList", MySpaceTexts.BlockPropertyTitle_GpsLocations, MySpaceTexts.Blank);
            gpsList.ListContent = (x, list1, list2) => x.FillGpsList(list1, list2);
            gpsList.ItemSelected = (x, y) => x.SelectGps(y, true);
            MyTerminalControlFactory.AddControl(gpsList);


            m_openedToolbars = new List<MyToolbar>();

            var toolbarButton = new MyTerminalControlButton<MyRemoteControl>("Open Toolbar", MySpaceTexts.BlockPropertyTitle_AutoPilotToolbarOpen, MySpaceTexts.BlockPropertyPopup_AutoPilotToolbarOpen,
                delegate(MyRemoteControl self)
                {
                    m_openedToolbars.Add(self.AutoPilotToolbar);
                    if (MyGuiScreenCubeBuilder.Static == null)
                    {
                        m_shouldSetOtherToolbars = true;
                        MyToolbarComponent.CurrentToolbar = self.AutoPilotToolbar;
                        MyGuiScreenBase screen = MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.ToolbarConfigScreen, 0, self);
                        MyToolbarComponent.AutoUpdate = false;
                        screen.Closed += (source) =>
                        {
                            MyToolbarComponent.AutoUpdate = true;
                            m_openedToolbars.Clear();
                        };
                        MyGuiSandbox.AddScreen(screen);
                    }
                });
            toolbarButton.Enabled = r => r.m_currentFlightMode == FlightMode.OneWay;
            toolbarButton.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(toolbarButton);
        }

        private bool CanEnableAutoPilot()
        {
            return IsWorking && m_previousControlledEntity == null;
        }

        private static void FillFlightModeCombo(List<TerminalComboBoxItem> list)
        {
            list.Add(new TerminalComboBoxItem() { Key = 0, Value = MySpaceTexts.BlockPropertyTitle_FlightMode_Patrol });
            list.Add(new TerminalComboBoxItem() { Key = 1, Value = MySpaceTexts.BlockPropertyTitle_FlightMode_Circle });
            list.Add(new TerminalComboBoxItem() { Key = 2, Value = MySpaceTexts.BlockPropertyTitle_FlightMode_OneWay });
        }

        private void SetAutoPilotEnabled(bool enabled)
        {
            if (CanEnableAutoPilot())
            {
                SyncObject.SetAutoPilot(enabled);
            }
        }

        private void OnSetAutoPilotEnabled(bool enabled)
        {
            if (!enabled)
            {
                m_currentWaypoint = null;
                CubeGrid.GridSystems.ThrustSystem.AutoPilotThrust = Vector3.Zero;
                CubeGrid.GridSystems.GyroSystem.ControlTorque = Vector3.Zero;

                m_autoPilotEnabled = enabled;

                var group = ControlGroup.GetGroup(CubeGrid);
                if (group != null)
                {
                    group.GroupData.ControlSystem.RemoveControllerBlock(this);
                }
            }
            else
            {
                if (m_previousControlledEntity == null)
                {
                    m_autoPilotEnabled = enabled;
                }

                var group = ControlGroup.GetGroup(CubeGrid);
                if (group != null)
                {
                    group.GroupData.ControlSystem.AddControllerBlock(this);
                }

                ResetShipControls();
            }

            UpdateText();
        }

        private IMyGps m_selectedGps;
        private void SelectGps(List<MyGuiControlListbox.Item> y, bool p)
        {
            if (y.Count > 0)
            {
                m_selectedGps = (IMyGps)y[0].UserData;
            }
            else
            {
                m_selectedGps = null;
            }
            RaisePropertiesChanged();
        }

        private MyWaypoint m_selectedWaypoint;
        private void SelectWaypoint(List<MyGuiControlListbox.Item> y, bool p)
        {
            if (y.Count > 0)
            {
                m_selectedWaypoint = (MyWaypoint)y[0].UserData;
            }
            else
            {
                m_selectedWaypoint = null;
            }
            RaisePropertiesChanged();
        }

        private void AddWaypoint()
        {
            if (m_selectedGps != null)
            {
                SyncObject.AddWaypoint(m_selectedGps.Coords, m_selectedGps.Name);
                m_selectedGps = null;
            }
        }

        private void OnAddWaypoint(Vector3D coords, string name)
        {
            m_waypoints.Add(new MyWaypoint(coords, name));
            RaisePropertiesChanged();
        }

        private void MoveUp()
        {
            if (m_selectedWaypoint != null)
            {
                int index = m_waypoints.IndexOf(m_selectedWaypoint);
                SyncObject.MoveWaypointUp(index);
            }
        }

        private void OnMoveUp(int index)
        {
            if (index > 0)
            {
                SwapWaypoints(index - 1, index);
                RaisePropertiesChanged();
            }
        }

        private void MoveDown()
        {
            if (m_selectedWaypoint != null)
            {
                int index = m_waypoints.IndexOf(m_selectedWaypoint);
                SyncObject.MoveWaypointDown(index);
            }
        }

        private void OnMoveDown(int index)
        {
            if (index < m_waypoints.Count - 1)
            {
                SwapWaypoints(index, index + 1);
                RaisePropertiesChanged();
            }
        }

        private void SwapWaypoints(int index1, int index2)
        {
            var w1 = m_waypoints[index1];
            var w2 = m_waypoints[index2];

            m_waypoints[index1] = w2;
            m_waypoints[index2] = w1;
        }

        private void RemoveWaypoint()
        {
            if (m_selectedWaypoint != null)
            {
                SyncObject.RemoveWaypoint(m_waypoints.IndexOf(m_selectedWaypoint));
                m_selectedWaypoint = null;
            }
        }

        private void OnRemoveWaypoint(int index)
        {
            var waypoint = m_waypoints[index];
            if (m_currentWaypoint == waypoint)
            {
                AdvanceWaypoint();
            }

            m_waypoints.Remove(waypoint);
            RaisePropertiesChanged();
        }

        private void ChangeFlightMode(FlightMode flightMode)
        {
            if (flightMode != m_currentFlightMode)
            {
                SyncObject.ChangeFlightMode(flightMode);
            }
        }

        private void OnChangeFlightMode(FlightMode flightMode)
        {
            m_currentFlightMode = flightMode;
            RaisePropertiesChanged();
        }

        private bool CanAdd()
        {
            if (m_selectedGps == null)
            {
                return false;
            }

            if (m_waypoints.Count == 0)
            {
                return true;
            }

            var lastWaypoint = m_waypoints[m_waypoints.Count - 1];
            if (lastWaypoint.Coords == m_selectedGps.Coords && lastWaypoint.Name == m_selectedGps.Name)
            {
                return false;
            }

            return true;
        }

        private bool CanMoveUp()
        {
            if (m_selectedWaypoint == null)
            {
                return false;
            }

            if (m_waypoints.Count == 0)
            {
                return false;
            }

            if (m_waypoints.IndexOf(m_selectedWaypoint) == 0)
            {
                return false;
            }

            return true;
        }

        private bool CanMoveDown()
        {
            if (m_selectedWaypoint == null)
            {
                return false;
            }

            if (m_waypoints.Count == 0)
            {
                return false;
            }

            if (m_waypoints.IndexOf(m_selectedWaypoint) == m_waypoints.Count - 1)
            {
                return false;
            }

            return true;
        }

        private bool CanRemove()
        {
            return m_selectedWaypoint != null;
        }

        private void FillGpsList(ICollection<MyGuiControlListbox.Item> gpsItemList, ICollection<MyGuiControlListbox.Item> selectedGpsItemList)
        {
            List<IMyGps> gpsList = new List<IMyGps>();
            MySession.Static.Gpss.GetGpsList(MySession.LocalPlayerId, gpsList);
            foreach (var gps in gpsList)
            {
                var item = new MyGuiControlListbox.Item(text: new StringBuilder(gps.Name), userData: gps);
                gpsItemList.Add(item);

                if (gps == m_selectedGps)
                {
                    selectedGpsItemList.Add(item);
                }
            }
        }

        private void FillWaypointList(ICollection<MyGuiControlListbox.Item> waypoints, ICollection<MyGuiControlListbox.Item> selectedWaypoints)
        {
            foreach (var waypoint in m_waypoints)
            {
                var item = new MyGuiControlListbox.Item(text: new StringBuilder(waypoint.Name), userData: waypoint);
                waypoints.Add(item);

                if (waypoint == m_selectedWaypoint)
                {
                    selectedWaypoints.Add(item);
                }
            }
        }

        private void ResetShipControls()
        {
            CubeGrid.GridSystems.ThrustSystem.DampenersEnabled = true;
            foreach (var dir in Base6Directions.IntDirections)
            {
                var thrusters = CubeGrid.GridSystems.ThrustSystem.GetThrustersForDirection(dir);
                foreach (var thruster in thrusters)
                {
                    if (thruster.ThrustOverride != 0f)
                    {
                        thruster.SetThrustOverride(0f);
                    }
                }
            }

            foreach (var gyro in CubeGrid.GridSystems.GyroSystem.Gyros)
            {
                if (gyro.GyroOverride)
                {
                    gyro.SetGyroOverride(false);
                }
            }
        }


        public new MySyncRemoteControl SyncObject
        {
            get { return (MySyncRemoteControl)base.SyncObject; }
        }

        protected override MySyncEntity OnCreateSync()
        {
            var sync = new MySyncRemoteControl(this);
            OnInitSync(sync);
            return sync;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;
            base.Init(objectBuilder, cubeGrid);
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;

            var remoteOb = (MyObjectBuilder_RemoteControl)objectBuilder;
            m_savedPreviousControlledEntityId = remoteOb.PreviousControlledEntityId;


            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Utility,
                false,
                m_powerNeeded,
                this.CalculateRequiredPowerInput);

            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            PowerReceiver.RequiredInputChanged += Receiver_RequiredInputChanged;
            PowerReceiver.Update();

            m_autoPilotEnabled = remoteOb.AutoPilotEnabled;
            m_currentFlightMode = (FlightMode)remoteOb.FlightMode;

            if (m_autoPilotEnabled)
            {
                m_startPosition = WorldMatrix.Translation;
            }

            if (remoteOb.Coords == null)
            {
                m_waypoints = new List<MyWaypoint>();
                m_currentWaypoint = null;
            }
            else
            {
                m_waypoints = new List<MyWaypoint>(remoteOb.Coords.Count);
                for (int i = 0; i < remoteOb.Coords.Count; i++)
                {
                    m_waypoints.Add(new MyWaypoint(remoteOb.Coords[i], remoteOb.Names[i]));
                }
                if (remoteOb.CurrentWaypointIndex == -1)
                {
                    m_currentWaypoint = null;
                }
                else
                {
                    m_currentWaypoint = m_waypoints[remoteOb.CurrentWaypointIndex];
                }
            }

            m_items = new List<ToolbarItem>(2);
            for (int i = 0; i < 2; i++)
            {
                m_items.Add(new ToolbarItem() { EntityID = 0 });
            }
            AutoPilotToolbar = new MyToolbar(MyToolbarType.ButtonPanel, 1, 1);
            AutoPilotToolbar.DrawNumbers = false;
            AutoPilotToolbar.Init(remoteOb.AutoPilotToolbar, this);

            for (int i = 0; i < 2; i++)
            {
                var item = AutoPilotToolbar.GetItemAtIndex(i);
                if (item == null)
                    continue;
                m_items.RemoveAt(i);
                m_items.Insert(i, GetToolbarItem(item));
            }
            AutoPilotToolbar.ItemChanged += Toolbar_ItemChanged;

            UpdateText();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            UpdateEmissivity();
            PowerReceiver.Update();
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (m_autoPilotEnabled)
            {
                var group = ControlGroup.GetGroup(CubeGrid);
                if (group != null)
                {
                    group.GroupData.ControlSystem.AddControllerBlock(this);
                }
            }
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if (m_savedPreviousControlledEntityId != null)
            {
                TryFindSavedEntity();

                m_savedPreviousControlledEntityId = null;
            }

            if (IsWorking && m_autoPilotEnabled && CubeGrid.GridSystems.ControlSystem.GetShipController() == this)
            {
                if (m_currentWaypoint == null && m_waypoints.Count > 0)
                {
                    m_currentWaypoint = m_waypoints[0];
                    m_startPosition = WorldMatrix.Translation;
                    UpdateText();
                }

                if (m_currentWaypoint != null)
                {
                    if (IsInStoppingDistance())
                    {
                        AdvanceWaypoint();
                    }

                    if (Sync.IsServer && m_currentWaypoint != null && !IsInStoppingDistance())
                    {
                        if (!UpdateGyro())
                        {
                            UpdateThrust();
                        }
                        else
                        {
                            CubeGrid.GridSystems.ThrustSystem.AutoPilotThrust = Vector3.Zero;
                        }
                    }
                }
            }
            else if (!IsWorking && m_autoPilotEnabled)
            {
                SetAutoPilotEnabled(false);
            }
        }

        private bool IsInStoppingDistance()
        {
            double cubesErrorAllowed = 2;
            int currentIndex = m_waypoints.IndexOf(m_currentWaypoint);

            if (m_currentFlightMode == FlightMode.OneWay && currentIndex == m_waypoints.Count - 1)
            {
                cubesErrorAllowed = 0.5;
            }

            return (WorldMatrix.Translation - m_currentWaypoint.Coords).LengthSquared() < CubeGrid.GridSize * CubeGrid.GridSize * cubesErrorAllowed * cubesErrorAllowed;
        }

        private void AdvanceWaypoint()
        {
            int currentIndex = m_waypoints.IndexOf(m_currentWaypoint);
            var m_oldWaypoint = m_currentWaypoint;

            if (m_waypoints.Count > 0)
            {
                if (m_currentFlightMode == FlightMode.Circle)
                {
                    currentIndex = (currentIndex + 1) % m_waypoints.Count;
                }
                else if (m_currentFlightMode == FlightMode.Patrol)
                {
                    if (m_patrolDirectionForward)
                    {
                        currentIndex++;
                        if (currentIndex >= m_waypoints.Count)
                        {
                            currentIndex = m_waypoints.Count - 2;
                            m_patrolDirectionForward = false;
                        }
                    }
                    else
                    {
                        currentIndex--;
                        if (currentIndex < 0)
                        {
                            currentIndex = 1;
                            m_patrolDirectionForward = true;
                        }
                    }
                }
                else if (m_currentFlightMode == FlightMode.OneWay)
                {
                    currentIndex++;
                    if (currentIndex >= m_waypoints.Count)
                    {
                        currentIndex = 0;

                        CubeGrid.GridSystems.GyroSystem.ControlTorque = Vector3.Zero;
                        CubeGrid.GridSystems.ThrustSystem.AutoPilotThrust = Vector3.Zero;

                        SetAutoPilotEnabled(false);

                        AutoPilotToolbar.UpdateItem(0);
                        if (Sync.IsServer)
                        {
                            AutoPilotToolbar.ActivateItemAtSlot(0);
                        }
                    }
                }
            }

            if (currentIndex < 0 || currentIndex >= m_waypoints.Count)
            {
                m_currentWaypoint = null;
                SetAutoPilotEnabled(false);
            }
            else
            {
                m_currentWaypoint = m_waypoints[currentIndex];
                m_startPosition = WorldMatrix.Translation;
            }

            if (m_currentWaypoint != m_oldWaypoint)
            {
                UpdateText();
            }
        }

        private Vector3D GetAngleVelocity(QuaternionD q1, QuaternionD q2)
        {
            q1.Conjugate();
            QuaternionD r = q2 * q1;

            double angle = 2 * System.Math.Acos(r.W);
            if (angle > Math.PI)
            {
                angle -= 2.0 * Math.PI;
            }

            Vector3D velocity = angle * new Vector3D(r.X, r.Y, r.Z) /
                System.Math.Sqrt(r.X * r.X + r.Y * r.Y + r.Z * r.Z);

            return velocity;
        }

        private bool UpdateGyro()
        {
            var gyros = CubeGrid.GridSystems.GyroSystem;
            gyros.ControlTorque = Vector3.Zero;
            Vector3D angularVelocity = CubeGrid.Physics.AngularVelocity;
            var orientation = WorldMatrix.GetOrientation();
            Matrix invWorldRot = CubeGrid.PositionComp.WorldMatrixNormalizedInv.GetOrientation();

            Vector3D targetPos = m_currentWaypoint.Coords;
            Vector3D currentPos = m_startPosition;
            Vector3D deltaPos = targetPos - currentPos;

            Vector3D targetDirection = Vector3D.Normalize(deltaPos);

            QuaternionD current = QuaternionD.CreateFromRotationMatrix(orientation);
            QuaternionD target = QuaternionD.CreateFromForwardUp(targetDirection, orientation.Up);

            Vector3D velocity = GetAngleVelocity(current, target);
            Vector3D velocityToTarget = velocity * angularVelocity.Dot(ref velocity);

            velocity = Vector3D.Transform(velocity, invWorldRot);

            double angle = System.Math.Acos(Vector3D.Dot(targetDirection, WorldMatrix.Forward));
            if (angle < 0.01)
            {
                return false;
            }

            if (velocity.LengthSquared() > 1.0)
            {
                Vector3D.Normalize(velocity);
            }

            Vector3D deceleration = angularVelocity - gyros.GetAngularVelocity(-velocity);
            double timeToStop = (angularVelocity / deceleration).Max();
            double timeToReachTarget = (angle / velocityToTarget.Length()) * angle;

            if (double.IsNaN(timeToStop) || double.IsInfinity(timeToReachTarget) || timeToReachTarget > timeToStop)
            {
                gyros.ControlTorque = velocity;
            }

            if (angle > 0.25)
            {
                return true;
            }

            return false;
        }

        private void UpdateThrust()
        {
            var thrustSystem = CubeGrid.GridSystems.ThrustSystem;
            thrustSystem.AutoPilotThrust = Vector3.Zero;
            Matrix invWorldRot = CubeGrid.PositionComp.WorldMatrixNormalizedInv.GetOrientation();

            Vector3D target = m_currentWaypoint.Coords;
            Vector3D current = WorldMatrix.Translation;
            Vector3D delta = target - current;

            Vector3D targetDirection = delta;
            targetDirection.Normalize();

            Vector3D velocity = CubeGrid.Physics.LinearVelocity;

            Vector3 localSpaceTargetDirection = Vector3.Transform(targetDirection, invWorldRot);
            Vector3 localSpaceVelocity = Vector3.Transform(velocity, invWorldRot);

            thrustSystem.AutoPilotThrust = Vector3.Zero;

            Vector3 brakeThrust = thrustSystem.GetAutoPilotThrustForDirection(Vector3.Zero);

            if (velocity.Length() > 3.0f && velocity.Dot(ref targetDirection) < 0)
            {
                //Going the wrong way
                return;
            }

            Vector3D perpendicularToTarget1 = Vector3D.CalculatePerpendicularVector(targetDirection);
            Vector3D perpendicularToTarget2 = Vector3D.Cross(targetDirection, perpendicularToTarget1);

            Vector3D velocityToTarget = targetDirection * velocity.Dot(ref targetDirection);
            Vector3D velocity1 = perpendicularToTarget1 * velocity.Dot(ref perpendicularToTarget1);
            Vector3D velocity2 = perpendicularToTarget2 * velocity.Dot(ref perpendicularToTarget2);

            Vector3D velocityToCancel = velocity1 + velocity2;

            double timeToReachTarget = (delta.Length() / velocityToTarget.Length());
            double timeToStop = velocity.Length() * CubeGrid.Physics.Mass / brakeThrust.Length();

            if (double.IsInfinity(timeToReachTarget) || double.IsNaN(timeToStop) || timeToReachTarget > timeToStop * 1.5)
            {
                thrustSystem.AutoPilotThrust = Vector3D.Transform(delta, invWorldRot) - Vector3D.Transform(velocityToCancel, invWorldRot);
                thrustSystem.AutoPilotThrust.Normalize();
            }
        }

        private bool TryFindSavedEntity()
        {
            MyEntity oldControllerEntity;
            if (MyEntities.TryGetEntityById(m_savedPreviousControlledEntityId.Value, out oldControllerEntity))
            {
                m_previousControlledEntity = (IMyControllableEntity)oldControllerEntity;
                if (m_previousControlledEntity != null)
                {
                    AddPreviousControllerEvents();

                    if (m_previousControlledEntity is MyCockpit)
                    {
                        cockpitPilot = (m_previousControlledEntity as MyCockpit).Pilot;
                    }
                    return true;
                }
            }

            return false;
        }

        public bool WasControllingCockpitWhenSaved()
        {
            if (m_savedPreviousControlledEntityId != null)
            {
                MyEntity oldControllerEntity;
                if (MyEntities.TryGetEntityById(m_savedPreviousControlledEntityId.Value, out oldControllerEntity))
                {
                    return oldControllerEntity is MyCockpit;
                }
            }

            return false;
        }

        private void AddPreviousControllerEvents()
        {
            m_previousControlledEntity.Entity.OnMarkForClose += Entity_OnPreviousMarkForClose;
            var functionalBlock = m_previousControlledEntity.Entity as MyTerminalBlock;
            if (functionalBlock != null)
            {
                functionalBlock.IsWorkingChanged += PreviousCubeBlock_IsWorkingChanged;

                var cockpit = m_previousControlledEntity.Entity as MyCockpit;
                if (cockpit != null && cockpit.Pilot != null)
                {
                    cockpit.Pilot.OnMarkForClose += Entity_OnPreviousMarkForClose;
                }
            }
        }

        private void PreviousCubeBlock_IsWorkingChanged(MyCubeBlock obj)
        {
            if (!obj.IsWorking && !(obj.Closed || obj.MarkedForClose))
            {
                RequestRelease(false);
            }
        }

        //When previous controller is closed, release control of remote
        private void Entity_OnPreviousMarkForClose(MyEntity obj)
        {
            RequestRelease(true);
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var objectBuilder = (MyObjectBuilder_RemoteControl)base.GetObjectBuilderCubeBlock(copy);

            if (m_previousControlledEntity != null)
            {
                objectBuilder.PreviousControlledEntityId = m_previousControlledEntity.Entity.EntityId;
            }

            objectBuilder.AutoPilotEnabled = m_autoPilotEnabled;
            objectBuilder.FlightMode = (int)m_currentFlightMode;

            objectBuilder.Coords = new List<Vector3D>(m_waypoints.Count);
            objectBuilder.Names = new List<string>(m_waypoints.Count);

            foreach (var waypoint in m_waypoints)
            {
                objectBuilder.Coords.Add(waypoint.Coords);
                objectBuilder.Names.Add(waypoint.Name);
            }

            if (m_currentWaypoint != null)
            {
                objectBuilder.CurrentWaypointIndex = m_waypoints.IndexOf(m_currentWaypoint);
            }
            else
            {
                objectBuilder.CurrentWaypointIndex = -1;
            }

            objectBuilder.AutoPilotToolbar = AutoPilotToolbar.GetObjectBuilder();
            return objectBuilder;
        }

        public bool CanControl()
        {
            if (!CheckPreviousEntity(MySession.ControlledEntity)) return false;
            if (m_autoPilotEnabled) return false;
            return IsWorking && PreviousControlledEntity == null && CheckRangeAndAccess(MySession.ControlledEntity, MySession.LocalHumanPlayer);
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(m_powerNeeded, DetailedInfo);

            if (m_autoPilotEnabled && m_currentWaypoint != null)
            {
                DetailedInfo.Append("\n");
                DetailedInfo.Append("Current waypoint: ");
                DetailedInfo.Append(m_currentWaypoint.Name);

                DetailedInfo.Append("\n");
                DetailedInfo.Append("Coords: ");
                DetailedInfo.Append(m_currentWaypoint.Coords);
            }
            RaisePropertiesChanged();
        }

        void Toolbar_ItemChanged(MyToolbar self, MyToolbar.IndexArgs index)
        {
            Debug.Assert(self == AutoPilotToolbar);

            var tItem = GetToolbarItem(self.GetItemAtIndex(index.ItemIndex));
            var oldItem = m_items[index.ItemIndex];
            if ((tItem.EntityID == 0 && oldItem.EntityID == 0 || (tItem.EntityID != 0 && oldItem.EntityID != 0 && tItem.Equals(oldItem))))
                return;
            m_items.RemoveAt(index.ItemIndex);
            m_items.Insert(index.ItemIndex, tItem);
            SyncObject.SendToolbarItemChanged(tItem, index.ItemIndex);

            if (m_shouldSetOtherToolbars)
            {
                m_shouldSetOtherToolbars = false;
                if (!SyncObject.IsSyncing)
                {
                    foreach (var toolbar in m_openedToolbars)
                    {
                        if (toolbar != self)
                        {
                            toolbar.SetItemAtIndex(index.ItemIndex, self.GetItemAtIndex(index.ItemIndex));
                        }
                    }
                }
                m_shouldSetOtherToolbars = true;
            }
        }

        private ToolbarItem GetToolbarItem(MyToolbarItem item)
        {
            var tItem = new ToolbarItem();
            tItem.EntityID = 0;
            if (item is MyToolbarItemTerminalBlock)
            {
                var block = item.GetObjectBuilder() as MyObjectBuilder_ToolbarItemTerminalBlock;
                tItem.EntityID = block.BlockEntityId;
                tItem.Action = block.Action;
            }
            else if (item is MyToolbarItemTerminalGroup)
            {
                var block = item.GetObjectBuilder() as MyObjectBuilder_ToolbarItemTerminalGroup;
                tItem.EntityID = block.BlockEntityId;
                tItem.Action = block.Action;
                tItem.GroupName = block.GroupName;
            }
            return tItem;
        }

        protected override void ComponentStack_IsFunctionalChanged()
        {
            base.ComponentStack_IsFunctionalChanged();

            if (!IsWorking)
            {
                RequestRelease(false);

                if (m_autoPilotEnabled)
                {
                    SetAutoPilotEnabled(false);
                }
            }

            PowerReceiver.Update();
            UpdateEmissivity();
            UpdateText();
        }

        private void Receiver_RequiredInputChanged(MyPowerReceiver receiver, float oldRequirement, float newRequirement)
        {
            UpdateText();
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            UpdateEmissivity();
            UpdateText();

            if (!IsWorking)
            {
                RequestRelease(false);

                if (m_autoPilotEnabled)
                {
                    SetAutoPilotEnabled(false);
                }
            }
        }

        private float CalculateRequiredPowerInput()
        {
            return m_powerNeeded;
        }

        public override void ShowTerminal()
        {
            MyGuiScreenTerminal.Show(MyTerminalPageEnum.ControlPanel, MySession.LocalHumanPlayer.Character, this);
        }

        private void RequestControl()
        {
            if (!MyFakes.ENABLE_REMOTE_CONTROL)
            {
                return;
            }

            //Do not take control if you are already the controller
            if (MySession.ControlledEntity == this)
            {
                return;
            }

            //Double check because it can be called from toolbar
            if (!CanControl())
            {
                return;
            }

            if (MyGuiScreenTerminal.IsOpen)
            {
                MyGuiScreenTerminal.Hide();
            }

            //Temporary fix to prevent crashes on DS
            //This happens when remote control is triggered by a sensor or a timer block
            //We need to prevent this from happening at all
            if (MySession.ControlledEntity != null)
            {
                SyncObject.RequestUse(UseActionEnum.Manipulate, MySession.ControlledEntity);
            }
        }

        private void AcquireControl()
        {
            AcquireControl(MySession.ControlledEntity);
        }

        private void AcquireControl(IMyControllableEntity previousControlledEntity)
        {
            if (!CheckPreviousEntity(previousControlledEntity))
            {
                return;
            }

            if (m_autoPilotEnabled)
            {
                SetAutoPilotEnabled(false);
            }

            PreviousControlledEntity = previousControlledEntity;
            var shipController = (PreviousControlledEntity as MyShipController);
            if (shipController != null)
            {
                m_enableFirstPerson = shipController.EnableFirstPerson;
                cockpitPilot = shipController.Pilot;
                if (cockpitPilot != null)
                {
                    cockpitPilot.CurrentRemoteControl = this;
                }
            }
            else
            {
                m_enableFirstPerson = true;

                var character = PreviousControlledEntity as MyCharacter;
                if (character != null)
                {
                    character.CurrentRemoteControl = this;
                }
            }

            if (MyCubeBuilder.Static.IsActivated)
            {
                MyCubeBuilder.Static.Deactivate();
            }

            UpdateEmissivity();
        }

        private bool CheckPreviousEntity(IMyControllableEntity entity)
        {
            if (entity is MyCharacter)
            {
                return true;
            }

            if (entity is MyCryoChamber)
            {
                return false;
            }
            
            if (entity is MyCockpit)
            {
                return true;
            }

            return false;
        }

        public void RequestControlFromLoad()
        {
            AcquireControl();
        }

        public override void OnUnregisteredFromGridSystems()
        {
            base.OnUnregisteredFromGridSystems();
            RequestRelease(false);

            if (m_autoPilotEnabled)
            {
                //Do not go through sync layer when destroying
                OnSetAutoPilotEnabled(false);
            }
        }

        public override void ForceReleaseControl()
        {
            base.ForceReleaseControl();
            RequestRelease(false);
        }

        private void RequestRelease(bool previousClosed)
        {
            if (!MyFakes.ENABLE_REMOTE_CONTROL)
            {
                return;
            }

            if (m_previousControlledEntity != null)
            {
                //Corner case when cockpit was destroyed
                if (m_previousControlledEntity is MyCockpit)
                {
                    if (cockpitPilot != null)
                    {
                        cockpitPilot.CurrentRemoteControl = null;
                    }

                    var cockpit = m_previousControlledEntity as MyCockpit;
                    if (previousClosed || cockpit.Pilot == null)
                    {
                        //This is null when loading from file
                        ReturnControl(cockpitPilot);
                        return;
                    }
                }

                var character = m_previousControlledEntity as MyCharacter;
                if (character != null)
                {
                    character.CurrentRemoteControl = null;
                }

                ReturnControl(m_previousControlledEntity);

                var receiver = GetFirstRadioReceiver();
                if (receiver != null)
                {
                    receiver.Clear();
                }
            }

            UpdateEmissivity();
        }

        private void ReturnControl(IMyControllableEntity nextControllableEntity)
        {
            //Check if it was already switched by server
            if (ControllerInfo.Controller != null)
            {
                this.SwitchControl(nextControllableEntity);
            }

            PreviousControlledEntity = null;
        }

        protected override void sync_UseSuccess(UseActionEnum actionEnum, IMyControllableEntity user)
        {
            base.sync_UseSuccess(actionEnum, user);

            AcquireControl(user);

            if (user.ControllerInfo != null && user.ControllerInfo.Controller != null)
            {
                user.SwitchControl(this);
            }
        }

        protected override ControllerPriority Priority
        {
            get
            {
                if (m_autoPilotEnabled)
                {
                    return ControllerPriority.AutoPilot;
                }
                else
                {
                    return ControllerPriority.Secondary;
                }
            }
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();
            if (!MyFakes.ENABLE_REMOTE_CONTROL)
            {
                return;
            }
            if (m_previousControlledEntity != null)
            {
                if (!RemoteIsInRangeAndPlayerHasAccess())
                {
                    RequestRelease(false);
                    if (MyGuiScreenTerminal.IsOpen && MyGuiScreenTerminal.InteractedEntity == this)
                    {
                        MyGuiScreenTerminal.Hide();
                    }
                }

                var receiver = GetFirstRadioReceiver();
                if (receiver != null)
                {
                    receiver.UpdateHud(true);
                }
            }

            if (m_autoPilotEnabled)
            {
                ResetShipControls();
            }
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        private MyDataReceiver GetFirstRadioReceiver()
        {
            var receivers = MyDataReceiver.GetGridRadioReceivers(CubeGrid);
            if (receivers.Count > 0)
            {
                return receivers.FirstElement();
            }
            return null;
        }

        private bool RemoteIsInRangeAndPlayerHasAccess()
        {
            if (ControllerInfo.Controller == null)
            {
                System.Diagnostics.Debug.Fail("Controller is null, but remote control was not properly released!");
                return false;
            }

            return CheckRangeAndAccess(PreviousControlledEntity, ControllerInfo.Controller.Player);
        }

        private bool CheckRangeAndAccess(IMyControllableEntity controlledEntity, MyPlayer player)
        {
            var terminal = controlledEntity as MyTerminalBlock;
            if (terminal == null)
            {
                var character = controlledEntity as MyCharacter;
                if (character != null)
                {
                    return MyAntennaSystem.CheckConnection(character, CubeGrid, player);
                }
                else
                {
                    return true;
                }
            }

            MyCubeGrid playerGrid = terminal.SlimBlock.CubeGrid;

            return MyAntennaSystem.CheckConnection(playerGrid, CubeGrid, player);
        }

        protected override void OnOwnershipChanged()
        {
            base.OnOwnershipChanged();

            if (PreviousControlledEntity != null && Sync.IsServer)
            {
                if (ControllerInfo.Controller != null)
                {
                    var relation = GetUserRelationToOwner(ControllerInfo.ControllingIdentityId);
                    if (relation == MyRelationsBetweenPlayerAndBlock.Enemies || relation == MyRelationsBetweenPlayerAndBlock.Neutral)
                        SyncObject.ControlledEntity_Use();
                }
            }
        }

        protected override void OnControlledEntity_Used()
        {
            base.OnControlledEntity_Used();
            RequestRelease(false);
        }

        public override MatrixD GetHeadMatrix(bool includeY, bool includeX = true, bool forceHeadAnim = false, bool forceHeadBone = false)
        {
            if (m_previousControlledEntity != null)
            {
                return m_previousControlledEntity.GetHeadMatrix(includeY, includeX, forceHeadAnim);
            }
            else
            {
                return MatrixD.Identity;
            }
        }

        public UseActionResult CanUse(UseActionEnum actionEnum, IMyControllableEntity user)
        {
            return UseActionResult.OK;
        }

        protected override bool CheckIsWorking()
        {
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        private void UpdateEmissivity()
        {
            UpdateIsWorking();

            if (IsWorking)
            {
                if (m_previousControlledEntity != null)
                {
                    MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Teal, Color.White);
                }
                else
                {
                    MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Green, Color.White);
                }
            }
            else
            {
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Red, Color.White);
            }
        }

        public override void ShowInventory()
        {
            base.ShowInventory();
            if (m_enableShipControl)
            {
                var user = GetUser();
                if (user != null)
                {
                    MyGuiScreenTerminal.Show(MyTerminalPageEnum.Inventory, user, this);
                }
            }
        }

        private MyCharacter GetUser()
        {
            if (PreviousControlledEntity != null)
            {
                if (cockpitPilot != null)
                {
                    return cockpitPilot;
                }

                var character = PreviousControlledEntity as MyCharacter;
                MyDebug.AssertDebug(character != null, "Cannot get the user of this remote control block, even though it is used!");
                if (character != null)
                {
                    return character;
                }

                return null;
            }

            return null;
        }

        [PreloadRequired]
        public class MySyncRemoteControl : MySyncShipController
        {
            [MessageIdAttribute(2500, P2PMessageEnum.Reliable)]
            protected struct SetAutoPilotMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public BoolBlit Enabled;
            }

            [MessageIdAttribute(2501, P2PMessageEnum.Reliable)]
            protected struct ChangeFlightModeMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public FlightMode NewFlightMode;
            }

            [MessageIdAttribute(2502, P2PMessageEnum.Reliable)]
            protected struct RemoveWaypointMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public int WaypointIndex;
            }

            [MessageIdAttribute(2503, P2PMessageEnum.Reliable)]
            protected struct MoveWaypointUpMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public int WaypointIndex;
            }

            [MessageIdAttribute(2504, P2PMessageEnum.Reliable)]
            protected struct MoveWaypointDownMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public int WaypointIndex;
            }

            [ProtoContract]
            [MessageIdAttribute(2505, P2PMessageEnum.Reliable)]
            protected struct AddWaypointMsg : IEntityMessage
            {
                [ProtoMember(1)]
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                [ProtoMember(2)]
                public Vector3D Coords;
                [ProtoMember(3)]
                public string Name;
            }

            [ProtoContract]
            [MessageIdAttribute(2506, P2PMessageEnum.Reliable)]
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

            private bool m_syncing;
            public bool IsSyncing
            {
                get { return m_syncing; }
            }

            static MySyncRemoteControl()
            {
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, SetAutoPilotMsg>(OnSetAutoPilot, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, ChangeFlightModeMsg>(OnChangeFlightMode, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, RemoveWaypointMsg>(OnRemoveWaypoint, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, MoveWaypointUpMsg>(OnMoveWaypointUp, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, MoveWaypointDownMsg>(OnMoveWaypointDown, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, AddWaypointMsg>(OnAddWaypoint, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, ChangeToolbarItemMsg>(OnToolbarItemChanged, MyMessagePermissions.Any);
            }

            private MyRemoteControl m_remoteControl;
         
            public MySyncRemoteControl(MyRemoteControl remoteControl) :
                base(remoteControl)
            {
                m_remoteControl = remoteControl;
            }

            public void SetAutoPilot(bool enabled)
            {
                var msg = new SetAutoPilotMsg();
                msg.EntityId = m_remoteControl.EntityId;

                msg.Enabled = enabled;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            public void ChangeFlightMode(FlightMode flightMode)
            {
                var msg = new ChangeFlightModeMsg();
                msg.EntityId = m_remoteControl.EntityId;

                msg.NewFlightMode = flightMode;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            public void RemoveWaypoint(int waypointIndex)
            {
                var msg = new RemoveWaypointMsg();
                msg.EntityId = m_remoteControl.EntityId;

                msg.WaypointIndex = waypointIndex;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            public void MoveWaypointUp(int waypointIndex)
            {
                var msg = new MoveWaypointUpMsg();
                msg.EntityId = m_remoteControl.EntityId;

                msg.WaypointIndex = waypointIndex;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            public void MoveWaypointDown(int waypointIndex)
            {
                var msg = new MoveWaypointDownMsg();
                msg.EntityId = m_remoteControl.EntityId;

                msg.WaypointIndex = waypointIndex;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            public void AddWaypoint(Vector3D coords, string name)
            {
                var msg = new AddWaypointMsg();
                msg.EntityId = m_remoteControl.EntityId;

                msg.Coords = coords;
                msg.Name = name;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            public void SendToolbarItemChanged(ToolbarItem item, int index)
            {
                if (m_syncing)
                    return;
                var msg = new ChangeToolbarItemMsg();
                msg.EntityId = m_remoteControl.EntityId;

                msg.Item = item;
                msg.Index = index;

                Sync.Layer.SendMessageToAll(ref msg);
            }

            private static void OnSetAutoPilot(MySyncRemoteControl sync, ref SetAutoPilotMsg msg, MyNetworkClient sender)
            {
                sync.m_remoteControl.OnSetAutoPilotEnabled(msg.Enabled);
            }

            private static void OnChangeFlightMode(MySyncRemoteControl sync, ref ChangeFlightModeMsg msg, MyNetworkClient sender)
            {
                sync.m_remoteControl.OnChangeFlightMode(msg.NewFlightMode);
            }

            private static void OnRemoveWaypoint(MySyncRemoteControl sync, ref RemoveWaypointMsg msg, MyNetworkClient sender)
            {
                sync.m_remoteControl.OnRemoveWaypoint(msg.WaypointIndex);
            }

            private static void OnMoveWaypointUp(MySyncRemoteControl sync, ref MoveWaypointUpMsg msg, MyNetworkClient sender)
            {
                sync.m_remoteControl.OnMoveUp(msg.WaypointIndex);
            }

            private static void OnMoveWaypointDown(MySyncRemoteControl sync, ref MoveWaypointDownMsg msg, MyNetworkClient sender)
            {
                sync.m_remoteControl.OnMoveDown(msg.WaypointIndex);
            }

            private static void OnAddWaypoint(MySyncRemoteControl sync, ref AddWaypointMsg msg, MyNetworkClient sender)
            {
                sync.m_remoteControl.OnAddWaypoint(msg.Coords, msg.Name);
            }

            private static void OnToolbarItemChanged(MySyncRemoteControl sync, ref ChangeToolbarItemMsg msg, MyNetworkClient sender)
            {
                sync.m_syncing = true;
                MyToolbarItem item = null;
                if (msg.Item.EntityID != 0)
                    if (string.IsNullOrEmpty(msg.Item.GroupName))
                    {
                        MyTerminalBlock block;
                        if (MyEntities.TryGetEntityById<MyTerminalBlock>(msg.Item.EntityID, out block))
                        {
                            var builder = MyToolbarItemFactory.TerminalBlockObjectBuilderFromBlock(block);
                            builder.Action = msg.Item.Action;
                            item = MyToolbarItemFactory.CreateToolbarItem(builder);
                        }
                    }
                    else
                    {
                        MyRemoteControl parent;
                        if (MyEntities.TryGetEntityById<MyRemoteControl>(msg.Item.EntityID, out parent))
                        {
                            var grid = parent.CubeGrid;
                            var groupName = msg.Item.GroupName;
                            var group = grid.GridSystems.TerminalSystem.BlockGroups.Find((x) => x.Name.ToString() == groupName);
                            if (group != null)
                            {
                                var builder = MyToolbarItemFactory.TerminalGroupObjectBuilderFromGroup(group);
                                builder.Action = msg.Item.Action;
                                builder.BlockEntityId = msg.Item.EntityID;
                                item = MyToolbarItemFactory.CreateToolbarItem(builder);
                            }
                        }

                    }
                sync.m_remoteControl.AutoPilotToolbar.SetItemAtIndex(msg.Index, item);
                sync.m_syncing = false;
            }
        }
    }
}
