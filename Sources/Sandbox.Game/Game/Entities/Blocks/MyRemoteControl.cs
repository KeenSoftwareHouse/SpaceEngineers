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
using Sandbox.Game.Entities.UseObject;
using VRage.Game.Entity.UseObject;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage.Collections;
using System.Linq;

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

        public class MyAutopilotWaypoint
        {
            public Vector3D Coords;
            public string Name;

            public MyToolbarItem[] Actions;

            public MyAutopilotWaypoint(Vector3D coords, string name, List<MyObjectBuilder_ToolbarItem> actionBuilders, List<int> indexes, MyRemoteControl owner)
            {
                Coords = coords;
                Name = name;

                if (actionBuilders != null)
                {
                    InitActions();
                    bool hasIndexes = indexes != null && indexes.Count > 0;

                    Debug.Assert(actionBuilders.Count <= MyToolbar.DEF_SLOT_COUNT);
                    if (hasIndexes)
                    {
                        Debug.Assert(indexes.Count == actionBuilders.Count);
                    }
                    for (int i = 0; i < actionBuilders.Count; i++)
                    {
                        if (actionBuilders[i] != null)
                        {
                            if (hasIndexes)
                            {
                                Actions[indexes[i]] = MyToolbarItemFactory.CreateToolbarItem(actionBuilders[i]);
                            }
                            else
                            {
                                Actions[i] = MyToolbarItemFactory.CreateToolbarItem(actionBuilders[i]);
                            }
                        }
                    }
                }
            }

            public MyAutopilotWaypoint(Vector3D coords, string name, MyRemoteControl owner)
                : this(coords, name, null, null, owner)
            {
            }

            public MyAutopilotWaypoint(IMyGps gps, MyRemoteControl owner)
                : this(gps.Coords, gps.Name, null, null, owner)
            {
            }

            public MyAutopilotWaypoint(MyObjectBuilder_AutopilotWaypoint builder, MyRemoteControl owner)
                : this(builder.Coords, builder.Name, builder.Actions, builder.Indexes, owner)
            {
            }

            public void InitActions()
            {
                Actions = new MyToolbarItem[MyToolbar.DEF_SLOT_COUNT];
            }

            public void SetActions(List<MyObjectBuilder_Toolbar.Slot> actionSlots)
            {
                Actions = new MyToolbarItem[MyToolbar.DEF_SLOT_COUNT];
                Debug.Assert(actionSlots.Count <= MyToolbar.DEF_SLOT_COUNT);

                for (int i = 0; i < actionSlots.Count; i++)
                {
                    if (actionSlots[i].Data != null)
                    {
                        Actions[i] = MyToolbarItemFactory.CreateToolbarItem(actionSlots[i].Data);
                    }
                }
            }

            public MyObjectBuilder_AutopilotWaypoint GetObjectBuilder()
            {
                MyObjectBuilder_AutopilotWaypoint builder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_AutopilotWaypoint>();
                builder.Coords = Coords;
                builder.Name = Name;

                if (Actions != null)
                {
                    bool actionExists = false;
                    foreach (var action in Actions)
                    {
                        if (action != null)
                        {
                            actionExists = true;
                        }
                    }

                    if (actionExists)
                    {
                        builder.Actions = new List<MyObjectBuilder_ToolbarItem>();
                        builder.Indexes = new List<int>();
                        for (int i = 0; i < Actions.Length; i++)
                        {
                            var action = Actions[i];
                            if (action != null)
                            {
                                builder.Actions.Add(action.GetObjectBuilder());
                                builder.Indexes.Add(i);
                            }
                        }
                    }
                }
                return builder;
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

        private MyAutopilotWaypoint CurrentWaypoint
        {
            get
            {
                return m_currentWaypoint;
            }
            set
            {
                m_currentWaypoint = value;
                if (m_currentWaypoint != null)
                {
                    m_startPosition = WorldMatrix.Translation;
                }
            }
        }

        private List<MyAutopilotWaypoint> m_waypoints;
        private MyAutopilotWaypoint m_currentWaypoint;
        private bool m_autoPilotEnabled;
        private bool m_dockingModeEnabled;
        private FlightMode m_currentFlightMode;
        private bool m_patrolDirectionForward = true;
        private Vector3D m_startPosition;
        private MyToolbar m_actionToolbar;
        private Base6Directions.Direction m_currentDirection = Base6Directions.Direction.Forward;

        private static MyObjectBuilder_AutopilotClipboard m_clipboard;
        private static MyGuiControlListbox m_gpsGuiControl;
        private static MyGuiControlListbox m_waypointGuiControl;

        private static Dictionary<Base6Directions.Direction, MyStringId> m_directionNames = new Dictionary<Base6Directions.Direction, MyStringId>()
        {
            { Base6Directions.Direction.Forward, MySpaceTexts.Thrust_Forward },
            { Base6Directions.Direction.Backward, MySpaceTexts.Thrust_Back },
            { Base6Directions.Direction.Left, MySpaceTexts.Thrust_Left },
            { Base6Directions.Direction.Right, MySpaceTexts.Thrust_Right },
            { Base6Directions.Direction.Up, MySpaceTexts.Thrust_Up },
            { Base6Directions.Direction.Down, MySpaceTexts.Thrust_Down }
        };

        private static Dictionary<Base6Directions.Direction, Vector3D> m_upVectors = new Dictionary<Base6Directions.Direction, Vector3D>()
        {
            { Base6Directions.Direction.Forward, Vector3D.Up },
            { Base6Directions.Direction.Backward, Vector3D.Up },
            { Base6Directions.Direction.Left, Vector3D.Up },
            { Base6Directions.Direction.Right, Vector3D.Up },
            { Base6Directions.Direction.Up, Vector3D.Right },
            { Base6Directions.Direction.Down, Vector3D.Right }
        };

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

            var autoPilot = new MyTerminalControlOnOffSwitch<MyRemoteControl>("AutoPilot", MySpaceTexts.BlockPropertyTitle_AutoPilot, MySpaceTexts.Blank);
            autoPilot.Getter = (x) => x.m_autoPilotEnabled;
            autoPilot.Setter = (x, v) => x.SetAutoPilotEnabled(v);
            autoPilot.Enabled = r => r.CanEnableAutoPilot();
            autoPilot.EnableToggleAction();
            autoPilot.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(autoPilot);

            var dockignMode = new MyTerminalControlOnOffSwitch<MyRemoteControl>("DockingMode", MySpaceTexts.BlockPropertyTitle_EnableDockingMode, MySpaceTexts.Blank);
            dockignMode.Getter = (x) => x.m_dockingModeEnabled;
            dockignMode.Setter = (x, v) => x.SetDockingMode(v);
            dockignMode.Enabled = r => r.IsWorking;
            dockignMode.EnableToggleAction();
            dockignMode.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(dockignMode);

            var flightMode = new MyTerminalControlCombobox<MyRemoteControl>("FlightMode", MySpaceTexts.BlockPropertyTitle_FlightMode, MySpaceTexts.Blank);
            flightMode.ComboBoxContent = (x) => FillFlightModeCombo(x);
            flightMode.Getter = (x) => (long)x.m_currentFlightMode;
            flightMode.Setter = (x, v) => x.ChangeFlightMode((FlightMode)v);
            MyTerminalControlFactory.AddControl(flightMode);

            var directionCombo = new MyTerminalControlCombobox<MyRemoteControl>("Direction", MySpaceTexts.BlockPropertyTitle_ForwardDirection, MySpaceTexts.Blank);
            directionCombo.ComboBoxContent = (x) => FillDirectionCombo(x);
            directionCombo.Getter = (x) => (long)x.m_currentDirection;
            directionCombo.Setter = (x, v) => x.ChangeDirection((Base6Directions.Direction)v);
            MyTerminalControlFactory.AddControl(directionCombo);

            var waypointList = new MyTerminalControlListbox<MyRemoteControl>("WaypointList", MySpaceTexts.BlockPropertyTitle_Waypoints, MySpaceTexts.Blank, true);
            waypointList.ListContent = (x, list1, list2) => x.FillWaypointList(list1, list2);
            waypointList.ItemSelected = (x, y) => x.SelectWaypoint(y);
            if (!MySandboxGame.IsDedicated)
            {
                m_waypointGuiControl = (MyGuiControlListbox)((MyGuiControlBlockProperty)waypointList.GetGuiControl()).PropertyControl;
            }
            MyTerminalControlFactory.AddControl(waypointList);


            var toolbarButton = new MyTerminalControlButton<MyRemoteControl>("Open Toolbar", MySpaceTexts.BlockPropertyTitle_AutoPilotToolbarOpen, MySpaceTexts.BlockPropertyPopup_AutoPilotToolbarOpen,
                delegate(MyRemoteControl self)
                {
                    var actions = self.m_selectedWaypoints[0].Actions;
                    if (actions != null)
                    {
                        for (int i = 0; i < actions.Length; i++)
                        {
                            if (actions[i] != null)
                            {
                                self.m_actionToolbar.SetItemAtIndex(i, actions[i]);
                            }
                        }
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
            toolbarButton.Enabled = r => r.m_selectedWaypoints.Count == 1;
            toolbarButton.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(toolbarButton);

            var removeBtn = new MyTerminalControlButton<MyRemoteControl>("RemoveWaypoint", MySpaceTexts.BlockActionTitle_RemoveWaypoint, MySpaceTexts.Blank, (b) => b.RemoveWaypoints());
            removeBtn.Enabled = r => r.CanRemoveWaypoints();
            removeBtn.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(removeBtn);

            var moveUp = new MyTerminalControlButton<MyRemoteControl>("MoveUp", MySpaceTexts.BlockActionTitle_MoveWaypointUp, MySpaceTexts.Blank, (b) => b.MoveWaypointsUp());
            moveUp.Enabled = r => r.CanMoveWaypointsUp();
            moveUp.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(moveUp);

            var moveDown = new MyTerminalControlButton<MyRemoteControl>("MoveDown", MySpaceTexts.BlockActionTitle_MoveWaypointDown, MySpaceTexts.Blank, (b) => b.MoveWaypointsDown());
            moveDown.Enabled = r => r.CanMoveWaypointsDown();
            moveDown.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(moveDown);

            var addButton = new MyTerminalControlButton<MyRemoteControl>("AddWaypoint", MySpaceTexts.BlockActionTitle_AddWaypoint, MySpaceTexts.Blank, (b) => b.AddWaypoints());
            addButton.Enabled = r => r.CanAddWaypoints();
            addButton.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(addButton);

            var gpsList = new MyTerminalControlListbox<MyRemoteControl>("GpsList", MySpaceTexts.BlockPropertyTitle_GpsLocations, MySpaceTexts.Blank, true);
            gpsList.ListContent = (x, list1, list2) => x.FillGpsList(list1, list2);
            gpsList.ItemSelected = (x, y) => x.SelectGps(y);
            if (!MySandboxGame.IsDedicated)
            {
                m_gpsGuiControl = (MyGuiControlListbox)((MyGuiControlBlockProperty)gpsList.GetGuiControl()).PropertyControl;
            }
            MyTerminalControlFactory.AddControl(gpsList);

            foreach (var direction in m_directionNames)
            {
                var setDirectionAction = new MyTerminalAction<MyRemoteControl>(MyTexts.Get(direction.Value).ToString(), MyTexts.Get(direction.Value), OnAction, null, MyTerminalActionIcons.TOGGLE);
                setDirectionAction.Enabled = (b) => b.IsWorking;
                setDirectionAction.ParameterDefinitions.Add(TerminalActionParameter.Get((byte)direction.Key));
                MyTerminalControlFactory.AddAction(setDirectionAction);
            }

            var resetButton = new MyTerminalControlButton<MyRemoteControl>("Reset", MySpaceTexts.BlockActionTitle_WaypointReset, MySpaceTexts.BlockActionTooltip_WaypointReset, (b) => b.ResetWaypoint());
            resetButton.Enabled = r => r.IsWorking;
            resetButton.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(resetButton);

            var copyButton = new MyTerminalControlButton<MyRemoteControl>("Copy", MySpaceTexts.BlockActionTitle_RemoteCopy, MySpaceTexts.Blank, (b) => b.CopyAutopilotSetup());
            copyButton.Enabled = r => r.IsWorking;
            copyButton.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(copyButton);

            var pasteButton = new MyTerminalControlButton<MyRemoteControl>("Paste", MySpaceTexts.BlockActionTitle_RemotePaste, MySpaceTexts.Blank, (b) => b.PasteAutopilotSetup());
            pasteButton.Enabled = r => r.IsWorking && MyRemoteControl.m_clipboard != null;
            pasteButton.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(pasteButton);
        }

        private static void OnAction(MyRemoteControl block, ListReader<TerminalActionParameter> paramteres)
        {
            var firstParameter = paramteres.FirstOrDefault();
            if (!firstParameter.IsEmpty)
            {
                block.ChangeDirection((Base6Directions.Direction)firstParameter.Value);
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
            m_dockingModeEnabled = remoteOb.DockingModeEnabled;
            m_currentFlightMode = (FlightMode)remoteOb.FlightMode;
            m_currentDirection = (Base6Directions.Direction)remoteOb.Direction;

            if (remoteOb.Coords == null || remoteOb.Coords.Count == 0)
            {
                if (remoteOb.Waypoints == null)
                {
                    m_waypoints = new List<MyAutopilotWaypoint>();
                    CurrentWaypoint = null;
                }
                else
                {
                    m_waypoints = new List<MyAutopilotWaypoint>(remoteOb.Waypoints.Count);
                    for (int i = 0; i < remoteOb.Waypoints.Count; i++)
                    {
                        m_waypoints.Add(new MyAutopilotWaypoint(remoteOb.Waypoints[i], this));
                    }
                }
            }
            else
            {
                m_waypoints = new List<MyAutopilotWaypoint>(remoteOb.Coords.Count);
                for (int i = 0; i < remoteOb.Coords.Count; i++)
                {
                    m_waypoints.Add(new MyAutopilotWaypoint(remoteOb.Coords[i], remoteOb.Names[i], this));
                }

                if (remoteOb.AutoPilotToolbar != null && m_currentFlightMode == FlightMode.OneWay)
                {
                    m_waypoints[m_waypoints.Count - 1].SetActions(remoteOb.AutoPilotToolbar.Slots);
                }
            }

            if (remoteOb.CurrentWaypointIndex == -1 || remoteOb.CurrentWaypointIndex >= m_waypoints.Count)
            {
                CurrentWaypoint = null;
            }
            else
            {
                CurrentWaypoint = m_waypoints[remoteOb.CurrentWaypointIndex];
            }

            m_actionToolbar = new MyToolbar(MyToolbarType.ButtonPanel, pageCount: 1);
            m_actionToolbar.DrawNumbers = false;
            m_actionToolbar.Init(null, this);

            m_selectedGpsLocations = new List<IMyGps>();
            m_selectedWaypoints = new List<MyAutopilotWaypoint>();
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

            UpdateAutopilot();
        }

        #region Autopilot GUI
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

        private static void FillDirectionCombo(List<TerminalComboBoxItem> list)
        {
            foreach (var direction in m_directionNames)
            {
                list.Add(new TerminalComboBoxItem() { Key = (long)direction.Key, Value = direction.Value });
            }
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
            if (m_autoPilotEnabled != enabled)
            {
                if (!enabled)
                {
                    CubeGrid.GridSystems.ThrustSystem.AutoPilotThrust = Vector3.Zero;
                    CubeGrid.GridSystems.GyroSystem.ControlTorque = Vector3.Zero;

                    m_autoPilotEnabled = enabled;

                    var group = ControlGroup.GetGroup(CubeGrid);
                    if (group != null)
                    {
                        group.GroupData.ControlSystem.RemoveControllerBlock(this);
                    }

                    if (CubeGrid.GridSystems.ControlSystem != null)
                    {
                        var shipController = CubeGrid.GridSystems.ControlSystem.GetShipController() as MyRemoteControl;
                        if (shipController == null || !shipController.m_autoPilotEnabled)
                        {
                            SetAutopilot(false);
                        }
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
                    SetAutopilot(true);

                    ResetShipControls();
                }
            }

            UpdateText();
        }

        private void SetAutopilot(bool enabled)
        {
            if (CubeGrid.GridSystems.ThrustSystem != null)
            {
                CubeGrid.GridSystems.ThrustSystem.AutopilotEnabled = enabled;
                CubeGrid.GridSystems.ThrustSystem.MarkDirty();
            }
            if (CubeGrid.GridSystems.GyroSystem != null)
            {
                CubeGrid.GridSystems.GyroSystem.AutopilotEnabled = enabled;
                CubeGrid.GridSystems.GyroSystem.MarkDirty();
            }
        }

        private void SetDockingMode(bool enabled)
        {
            if (enabled != m_dockingModeEnabled)
            {
                SyncObject.SetDockingMode(enabled);
            }
        }

        private void OnSetDockingMode(bool enabled)
        {
            m_dockingModeEnabled = enabled;
        }

        private List<IMyGps> m_selectedGpsLocations;
        private void SelectGps(List<MyGuiControlListbox.Item> selection)
        {
            m_selectedGpsLocations.Clear();
            if (selection.Count > 0)
            {
                foreach (var item in selection)
                {
                    m_selectedGpsLocations.Add((IMyGps)item.UserData);
                }
            }
            RaisePropertiesChangedRemote();
        }

        private List<MyAutopilotWaypoint> m_selectedWaypoints;
        private void SelectWaypoint(List<MyGuiControlListbox.Item> selection)
        {
            m_selectedWaypoints.Clear();
            if (selection.Count > 0)
            {
                foreach (var item in selection)
                {
                    m_selectedWaypoints.Add((MyAutopilotWaypoint)item.UserData);
                }
            }
            RaisePropertiesChangedRemote();
        }

        private void AddWaypoints()
        {
            if (m_selectedGpsLocations.Count > 0)
            {
                int gpsCount = m_selectedGpsLocations.Count;

                Vector3D[] coords = new Vector3D[gpsCount];
                string[] names = new string[gpsCount];

                for (int i = 0; i < gpsCount; i++)
                {
                    coords[i] = m_selectedGpsLocations[i].Coords;
                    names[i] = m_selectedGpsLocations[i].Name;
                }

                SyncObject.AddWaypoints(coords, names);
                m_selectedGpsLocations.Clear();
            }
        }

        private void OnAddWaypoints(Vector3D[] coords, string[] names)
        {
            Debug.Assert(coords.Length == names.Length);

            for (int i = 0; i < coords.Length; i++)
            {
                m_waypoints.Add(new MyAutopilotWaypoint(coords[i], names[i], this));
            }
            RaisePropertiesChangedRemote();
        }

        private bool CanMoveItemUp(int index)
        {
            if (index == -1)
            {
                return false;
            }

            for (int i = index - 1; i >= 0; i--)
            {
                if (!m_selectedWaypoints.Contains(m_waypoints[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void MoveWaypointsUp()
        {
            if (m_selectedWaypoints.Count > 0)
            {
                var indexes = new List<int>(m_selectedWaypoints.Count);
                foreach (var item in m_selectedWaypoints)
                {
                    int index = m_waypoints.IndexOf(item);
                    if (CanMoveItemUp(index))
                    {
                        indexes.Add(index);
                    }
                }

                if (indexes.Count > 0)
                {
                    var indexesToSend = indexes.ToArray();
                    Array.Sort(indexesToSend);

                    SyncObject.MoveWaypointsUp(indexesToSend);
                }
            }
        }

        private void OnMoveWaypointsUp(int[] indexes)
        {
            for (int i = 0; i < indexes.Length; i++)
            {
                Debug.Assert(indexes[i] > 0);
                SwapWaypoints(indexes[i] - 1, indexes[i]);
            }
            RaisePropertiesChangedRemote();
        }

        private bool CanMoveItemDown(int index)
        {
            if (index == -1)
            {
                return false;
            }

            for (int i = index + 1; i < m_waypoints.Count; i++)
            {
                if (!m_selectedWaypoints.Contains(m_waypoints[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private void MoveWaypointsDown()
        {
            if (m_selectedWaypoints.Count > 0)
            {
                var indexes = new List<int>(m_selectedWaypoints.Count);
                foreach (var item in m_selectedWaypoints)
                {
                    int index = m_waypoints.IndexOf(item);
                    if (CanMoveItemDown(index))
                    {
                        indexes.Add(index);
                    }
                }

                if (indexes.Count > 0)
                {
                    var indexesToSend = indexes.ToArray();
                    Array.Sort(indexesToSend);

                    SyncObject.MoveWaypointsDown(indexesToSend);
                }
            }
        }

        private void OnMoveWaypointsDown(int[] indexes)
        {
            for (int i = indexes.Length - 1; i >= 0; i--)
            {
                int index = indexes[i];
                Debug.Assert(index < m_waypoints.Count - 1);

                SwapWaypoints(index, index + 1);
            }
            RaisePropertiesChangedRemote();
        }

        private void SwapWaypoints(int index1, int index2)
        {
            var w1 = m_waypoints[index1];
            var w2 = m_waypoints[index2];

            m_waypoints[index1] = w2;
            m_waypoints[index2] = w1;
        }

        private void RemoveWaypoints()
        {
            if (m_selectedWaypoints.Count > 0)
            {
                int[] indexes = new int[m_selectedWaypoints.Count];
                for (int i = 0; i < m_selectedWaypoints.Count; i++)
                {
                    var item = m_selectedWaypoints[i];
                    indexes[i] = m_waypoints.IndexOf(item);
                }
                Array.Sort(indexes);

                SyncObject.RemoveWaypoints(indexes);

                m_selectedWaypoints.Clear();
            }
        }

        private void OnRemoveWaypoints(int[] indexes)
        {
            bool currentWaypointRemoved = false;
            for (int i = indexes.Length - 1; i >= 0; i--)
            {
                var waypoint = m_waypoints[indexes[i]];
                m_waypoints.Remove(waypoint);

                if (CurrentWaypoint == waypoint)
                {
                    currentWaypointRemoved = true;
                }
            }
            if (currentWaypointRemoved)
            {
                AdvanceWaypoint();
            }
            RaisePropertiesChangedRemote();
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
            RaisePropertiesChangedRemote();
        }

        private void ChangeDirection(Base6Directions.Direction direction)
        {
            if (direction != m_currentDirection)
            {
                SyncObject.ChangeDirection(direction);
            }
        }

        private void OnChangeDirection(Base6Directions.Direction direction)
        {
            m_currentDirection = direction;
            RaisePropertiesChangedRemote();
        }

        private bool CanAddWaypoints()
        {
            if (m_selectedGpsLocations.Count == 0)
            {
                return false;
            }

            if (m_waypoints.Count == 0)
            {
                return true;
            }

            return true;
        }

        private bool CanMoveWaypointsUp()
        {
            if (m_selectedWaypoints.Count == 0)
            {
                return false;
            }

            if (m_waypoints.Count == 0)
            {
                return false;
            }

            foreach (var item in m_selectedWaypoints)
            {
                int index = m_waypoints.IndexOf(item);
                {
                    if (CanMoveItemUp(index))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CanMoveWaypointsDown()
        {
            if (m_selectedWaypoints.Count == 0)
            {
                return false;
            }

            if (m_waypoints.Count == 0)
            {
                return false;
            }

            foreach (var item in m_selectedWaypoints)
            {
                int index = m_waypoints.IndexOf(item);
                {
                    if (CanMoveItemDown(index))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CanRemoveWaypoints()
        {
            return m_selectedWaypoints.Count > 0;
        }

        private void ResetWaypoint()
        {
            SyncObject.SendResetWaypoint();
        }

        private void OnResetWaypoint()
        {
            if (m_waypoints.Count > 0)
            {
                CurrentWaypoint = m_waypoints[0];
                m_patrolDirectionForward = true;
                RaisePropertiesChangedRemote();
            }
        }

        private void CopyAutopilotSetup()
        {
            m_clipboard = new MyObjectBuilder_AutopilotClipboard();
            m_clipboard.Direction = (byte)m_currentDirection;
            m_clipboard.FlightMode = (int)m_currentFlightMode;
            m_clipboard.RemoteEntityId = EntityId;
            m_clipboard.DockingModeEnabled = m_dockingModeEnabled;
            m_clipboard.Waypoints = new List<MyObjectBuilder_AutopilotWaypoint>(m_waypoints.Count);
            foreach (var waypoint in m_waypoints)
            {
                m_clipboard.Waypoints.Add(waypoint.GetObjectBuilder());
            }
            RaisePropertiesChangedRemote();
        }

        private void PasteAutopilotSetup()
        {
            if (m_clipboard != null)
            {
                SyncObject.SendPasteAutopilotSettings(m_clipboard);
            }
        }

        private void OnPasteAutopilotSetup(MyObjectBuilder_AutopilotClipboard clipboard)
        {
            m_currentDirection = (Base6Directions.Direction)clipboard.Direction;
            m_currentFlightMode = (FlightMode)clipboard.FlightMode;
            m_dockingModeEnabled = clipboard.DockingModeEnabled;
            if (clipboard.Waypoints != null)
            {
                m_waypoints = new List<MyAutopilotWaypoint>(clipboard.Waypoints.Count);
                foreach (var waypoint in clipboard.Waypoints)
                {
                    if (waypoint.Actions != null)
                    {
                        foreach (var action in waypoint.Actions)
                        {
                            var blockAction = action as MyObjectBuilder_ToolbarItemTerminalBlock;
                            //Swith from old entity to the new one
                            if (blockAction != null && blockAction.BlockEntityId == clipboard.RemoteEntityId)
                            {
                                blockAction.BlockEntityId = EntityId;
                            }
                        }
                    }
                    m_waypoints.Add(new MyAutopilotWaypoint(waypoint, this));
                }
            }

            m_selectedWaypoints.Clear();

            RaisePropertiesChangedRemote();
        }

        private void FillGpsList(ICollection<MyGuiControlListbox.Item> gpsItemList, ICollection<MyGuiControlListbox.Item> selectedGpsItemList)
        {
            List<IMyGps> gpsList = new List<IMyGps>();
            MySession.Static.Gpss.GetGpsList(MySession.LocalPlayerId, gpsList);
            foreach (var gps in gpsList)
            {
                var item = new MyGuiControlListbox.Item(text: new StringBuilder(gps.Name), userData: gps);
                gpsItemList.Add(item);

                if (m_selectedGpsLocations.Contains(gps))
                {
                    selectedGpsItemList.Add(item);
                }
            }
        }

        private StringBuilder m_tempName = new StringBuilder();
        private StringBuilder m_tempTooltip = new StringBuilder();
        private StringBuilder m_tempActions = new StringBuilder();
        private void FillWaypointList(ICollection<MyGuiControlListbox.Item> waypoints, ICollection<MyGuiControlListbox.Item> selectedWaypoints)
        {
            foreach (var waypoint in m_waypoints)
            {
                m_tempName.Append(waypoint.Name);

                int actionCount = 0;

                m_tempActions.Append("\nActions:");
                if (waypoint.Actions != null)
                {
                    foreach (var action in waypoint.Actions)
                    {
                        if (action != null)
                        {
                            m_tempActions.Append("\n");
                            action.Update(this);
                            m_tempActions.AppendStringBuilder(action.DisplayName);

                            actionCount++;
                        }
                    }
                }

                m_tempTooltip.AppendStringBuilder(m_tempName);
                m_tempTooltip.Append('\n');
                m_tempTooltip.Append(waypoint.Coords.ToString());

                if (actionCount > 0)
                {
                    m_tempName.Append(" [");
                    m_tempName.Append(actionCount.ToString());
                    if (actionCount > 1)
                    {
                        m_tempName.Append(" Actions]");
                    }
                    else
                    {
                        m_tempName.Append(" Action]");
                    }
                    m_tempTooltip.AppendStringBuilder(m_tempActions);
                }

                var item = new MyGuiControlListbox.Item(text: m_tempName, toolTip: m_tempTooltip.ToString(), userData: waypoint);
                waypoints.Add(item);

                if (m_selectedWaypoints.Contains(waypoint))
                {
                    selectedWaypoints.Add(item);
                }

                m_tempName.Clear();
                m_tempTooltip.Clear();
                m_tempActions.Clear();
            }
        }

        void Toolbar_ItemChanged(MyToolbar self, MyToolbar.IndexArgs index)
        {
            if (m_selectedWaypoints.Count == 1)
            {
                SyncObject.SendToolbarItemChanged(ToolbarItem.FromItem(self.GetItemAtIndex(index.ItemIndex)), index.ItemIndex, m_waypoints.IndexOf(m_selectedWaypoints[0]));
            }
        }

        private void RaisePropertiesChangedRemote()
        {
            int gpsFirstVisibleRow = m_gpsGuiControl != null ? m_gpsGuiControl.FirstVisibleRow : 0;
            int waypointFirstVisibleRow = m_waypointGuiControl != null ? m_waypointGuiControl.FirstVisibleRow : 0;
            RaisePropertiesChanged();
            if (m_gpsGuiControl != null && gpsFirstVisibleRow < m_gpsGuiControl.Items.Count)
            {
                m_gpsGuiControl.FirstVisibleRow = gpsFirstVisibleRow;
            }
            if (m_waypointGuiControl != null && waypointFirstVisibleRow < m_waypointGuiControl.Items.Count)
            {
                m_waypointGuiControl.FirstVisibleRow = waypointFirstVisibleRow;
            }
        }
        #endregion

        #region Autopilot Logic
        private void UpdateAutopilot()
        {
            if (IsWorking && m_autoPilotEnabled && CubeGrid.GridSystems.ControlSystem.GetShipController() == this)
            {
                Debug.Assert(CubeGrid.GridSystems.ThrustSystem.AutopilotEnabled == true);
                Debug.Assert(CubeGrid.GridSystems.GyroSystem.AutopilotEnabled == true);

                if (CurrentWaypoint == null && m_waypoints.Count > 0)
                {
                    CurrentWaypoint = m_waypoints[0];
                    UpdateText();
                }

                if (CurrentWaypoint != null)
                {
                    if (IsInStoppingDistance())
                    {
                        AdvanceWaypoint();
                    }

                    if (Sync.IsServer && CurrentWaypoint != null && !IsInStoppingDistance())
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
            else if (!IsWorking && m_autoPilotEnabled && Sync.IsServer)
            {
                SyncObject.SetAutoPilot(false);
            }
        }

        private bool IsInStoppingDistance()
        {
            double cubesErrorAllowed = 3;
            int currentIndex = m_waypoints.IndexOf(CurrentWaypoint);

            if (m_dockingModeEnabled || (m_currentFlightMode == FlightMode.OneWay && currentIndex == m_waypoints.Count - 1))
            {
                cubesErrorAllowed = 0.25;
            }

            return (WorldMatrix.Translation - CurrentWaypoint.Coords).LengthSquared() < CubeGrid.GridSize * CubeGrid.GridSize * cubesErrorAllowed * cubesErrorAllowed;
        }

        private void AdvanceWaypoint()
        {
            int currentIndex = m_waypoints.IndexOf(CurrentWaypoint);
            var m_oldWaypoint = CurrentWaypoint;

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
                    }
                }
            }

            if (currentIndex < 0 || currentIndex >= m_waypoints.Count)
            {
                CurrentWaypoint = null;
                SetAutoPilotEnabled(false);
                UpdateText();
            }
            else
            {
                CurrentWaypoint = m_waypoints[currentIndex];

                if (CurrentWaypoint != m_oldWaypoint)
                {
                    if (Sync.IsServer && m_oldWaypoint.Actions != null && m_autoPilotEnabled)
                    {
                        for (int i = 0; i < m_oldWaypoint.Actions.Length; i++)
                        {
                            if (m_oldWaypoint.Actions[i] != null)
                            {
                                m_actionToolbar.SetItemAtIndex(0, m_oldWaypoint.Actions[i]);
                                m_actionToolbar.UpdateItem(0);
                                m_actionToolbar.ActivateItemAtSlot(0);
                            }
                        }
                        m_actionToolbar.Clear();
                    }

                    UpdateText();
                }
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

        private MatrixD GetOrientation()
        {
            var orientation = MatrixD.CreateWorld(Vector3D.Zero, (Vector3D)Base6Directions.GetVector(m_currentDirection), m_upVectors[m_currentDirection]);
            return orientation * WorldMatrix.GetOrientation();
        }

        private bool UpdateGyro()
        {
            var gyros = CubeGrid.GridSystems.GyroSystem;
            gyros.ControlTorque = Vector3.Zero;
            Vector3D angularVelocity = CubeGrid.Physics.AngularVelocity;
            var orientation = GetOrientation();
            Matrix invWorldRot = CubeGrid.PositionComp.WorldMatrixNormalizedInv.GetOrientation();

            Vector3D targetPos = CurrentWaypoint.Coords;
            Vector3D currentPos = m_startPosition;
            Vector3D deltaPos = targetPos - currentPos;

            Vector3D targetDirection = Vector3D.Normalize(deltaPos);

            QuaternionD current = QuaternionD.CreateFromRotationMatrix(orientation);
            QuaternionD target = QuaternionD.CreateFromForwardUp(targetDirection, orientation.Up);

            Vector3D velocity = GetAngleVelocity(current, target);
            Vector3D velocityToTarget = velocity * angularVelocity.Dot(ref velocity);

            velocity = Vector3D.Transform(velocity, invWorldRot);

            double angle = System.Math.Acos(Vector3D.Dot(targetDirection, orientation.Forward));
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
                if (m_dockingModeEnabled)
                {
                    velocity /= 4.0;
                }
                gyros.ControlTorque = velocity;
            }

            if (m_dockingModeEnabled)
            {
                if (angle > 0.05)
                {
                    return true;
                }
            }
            else
            {
                if (angle > 0.25)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateThrust()
        {
            var thrustSystem = CubeGrid.GridSystems.ThrustSystem;
            thrustSystem.AutoPilotThrust = Vector3.Zero;
            Matrix invWorldRot = CubeGrid.PositionComp.WorldMatrixNormalizedInv.GetOrientation();

            Vector3D target = CurrentWaypoint.Coords;
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

            if (m_dockingModeEnabled)
            {
                timeToStop *= 2.5f;
            }

            if (double.IsInfinity(timeToReachTarget) || double.IsNaN(timeToStop) || timeToReachTarget > timeToStop)
            {
                thrustSystem.AutoPilotThrust = Vector3D.Transform(delta, invWorldRot) - Vector3D.Transform(velocityToCancel, invWorldRot);
                thrustSystem.AutoPilotThrust.Normalize();
            }
        }

        private void ResetShipControls()
        {
            CubeGrid.GridSystems.ThrustSystem.DampenersEnabled = true;
        }
        #endregion

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
            objectBuilder.DockingModeEnabled = m_dockingModeEnabled;
            objectBuilder.FlightMode = (int)m_currentFlightMode;
            objectBuilder.Direction = (byte)m_currentDirection;

            objectBuilder.Waypoints = new List<MyObjectBuilder_AutopilotWaypoint>(m_waypoints.Count);

            foreach (var waypoint in m_waypoints)
            {
                objectBuilder.Waypoints.Add(waypoint.GetObjectBuilder());
            }

            if (CurrentWaypoint != null)
            {
                objectBuilder.CurrentWaypointIndex = m_waypoints.IndexOf(CurrentWaypoint);
            }
            else
            {
                objectBuilder.CurrentWaypointIndex = -1;
            }

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

            if (m_autoPilotEnabled && CurrentWaypoint != null)
            {
                DetailedInfo.Append("\n");
                DetailedInfo.Append("Current waypoint: ");
                DetailedInfo.Append(CurrentWaypoint.Name);

                DetailedInfo.Append("\n");
                DetailedInfo.Append("Coords: ");
                DetailedInfo.Append(CurrentWaypoint.Coords);
            }
            RaisePropertiesChangedRemote();
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

        public override void OnRegisteredToGridSystems()
        {
            base.OnRegisteredToGridSystems();

            if (m_autoPilotEnabled)
            {
                SetAutopilot(true);
            }
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
            protected struct SetDockingModeMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public BoolBlit Enabled;
            }

            [MessageIdAttribute(2502, P2PMessageEnum.Reliable)]
            protected struct ChangeFlightModeMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public FlightMode NewFlightMode;
            }

            [MessageIdAttribute(2503, P2PMessageEnum.Reliable)]
            protected struct ChangeDirectionMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public Base6Directions.Direction NewDirection;
            }

            [ProtoContract]
            [MessageIdAttribute(2504, P2PMessageEnum.Reliable)]
            protected struct RemoveWaypointsMsg : IEntityMessage
            {
                [ProtoMember]
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                [ProtoMember]
                public int[] WaypointIndexes;
            }

            [ProtoContract]
            [MessageIdAttribute(2505, P2PMessageEnum.Reliable)]
            protected struct MoveWaypointsUpMsg : IEntityMessage
            {
                [ProtoMember]
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                [ProtoMember]
                public int[] WaypointIndexes;
            }

            [ProtoContract]
            [MessageIdAttribute(2506, P2PMessageEnum.Reliable)]
            protected struct MoveWaypointsDownMsg : IEntityMessage
            {
                [ProtoMember]
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                [ProtoMember]
                public int[] WaypointIndexes;
            }

            [ProtoContract]
            [MessageIdAttribute(2507, P2PMessageEnum.Reliable)]
            protected struct AddWaypointsMsg : IEntityMessage
            {
                [ProtoMember]
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                [ProtoMember]
                public Vector3D[] Coords;
                [ProtoMember]
                public string[] Names;
            }

            [ProtoContract]
            [MessageIdAttribute(2508, P2PMessageEnum.Reliable)]
            protected struct ChangeToolbarItemMsg : IEntityMessage
            {
                [ProtoMember]
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                [ProtoMember]
                public int WaypointIndex;

                [ProtoMember]
                public ToolbarItem Item;

                [ProtoMember]
                public int Index;

                public override string ToString()
                {
                    return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
                }
            }

            [MessageIdAttribute(2509, P2PMessageEnum.Reliable)]
            protected struct ResetWaypointMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }
            }

            [ProtoContract]
            [MessageIdAttribute(2510, P2PMessageEnum.Reliable)]
            protected struct PasteAutopilotSetupMsg : IEntityMessage
            {
                [ProtoMember]
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                [ProtoMember]
                public MyObjectBuilder_AutopilotClipboard Clipboard;
            }

            private bool m_syncing;
            public bool IsSyncing
            {
                get { return m_syncing; }
            }

            static MySyncRemoteControl()
            {
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, SetAutoPilotMsg>(OnSetAutoPilot, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, SetDockingModeMsg>(OnSetDockingMode, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, ChangeFlightModeMsg>(OnChangeFlightMode, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, ChangeDirectionMsg>(OnChangeDirection, MyMessagePermissions.Any);
                
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, RemoveWaypointsMsg>(OnRemoveWaypoints, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, MoveWaypointsUpMsg>(OnMoveWaypointsUp, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, MoveWaypointsDownMsg>(OnMoveWaypointsDown, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, AddWaypointsMsg>(OnAddWaypoints, MyMessagePermissions.Any);
                
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, ChangeToolbarItemMsg>(OnToolbarItemChanged, MyMessagePermissions.Any);

                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, ResetWaypointMsg>(OnResetWaypoint, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncRemoteControl, PasteAutopilotSetupMsg>(OnPasteAutopilotSetup, MyMessagePermissions.Any);
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

            public void SetDockingMode(bool enabled)
            {
                var msg = new SetDockingModeMsg();
                msg.EntityId = m_remoteControl.EntityId;

                msg.Enabled = enabled;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            public void ChangeFlightMode(FlightMode flightMode)
            {
                if (m_syncing)
                {
                    return;
                }

                var msg = new ChangeFlightModeMsg();
                msg.EntityId = m_remoteControl.EntityId;

                msg.NewFlightMode = flightMode;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            public void ChangeDirection(Base6Directions.Direction direction)
            {
                if (m_syncing)
                {
                    return;
                }

                var msg = new ChangeDirectionMsg();
                msg.EntityId = m_remoteControl.EntityId;
                msg.NewDirection = direction;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            public void RemoveWaypoints(int[] waypointIndexes)
            {
                if (m_syncing)
                {
                    return;
                }

                var msg = new RemoveWaypointsMsg();
                msg.EntityId = m_remoteControl.EntityId;

                msg.WaypointIndexes = waypointIndexes;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
                m_syncing = true;
            }

            public void MoveWaypointsUp(int[] waypointIndexes)
            {
                if (m_syncing)
                {
                    return;
                }

                var msg = new MoveWaypointsUpMsg();
                msg.EntityId = m_remoteControl.EntityId;

                msg.WaypointIndexes = waypointIndexes;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
                m_syncing = true;
            }

            public void MoveWaypointsDown(int[] waypointIndexes)
            {
                if (m_syncing)
                {
                    return;
                }

                var msg = new MoveWaypointsDownMsg();
                msg.EntityId = m_remoteControl.EntityId;

                msg.WaypointIndexes = waypointIndexes;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
                m_syncing = true;
            }

            public void AddWaypoints(Vector3D[] coords, string[] names)
            {
                if (m_syncing)
                {
                    return;
                }

                var msg = new AddWaypointsMsg();
                msg.EntityId = m_remoteControl.EntityId;

                msg.Coords = coords;
                msg.Names = names;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
                m_syncing = true;
            }

            public void SendToolbarItemChanged(ToolbarItem item, int index, int waypointIndex)
            {
                if (m_syncing)
                    return;
                var msg = new ChangeToolbarItemMsg();
                msg.EntityId = m_remoteControl.EntityId;

                msg.Item = item;
                msg.Index = index;
                msg.WaypointIndex = waypointIndex;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            public void SendResetWaypoint()
            {
                var msg = new ResetWaypointMsg();
                msg.EntityId = m_remoteControl.EntityId;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            public void SendPasteAutopilotSettings(MyObjectBuilder_AutopilotClipboard clipboard)
            {
                var msg = new PasteAutopilotSetupMsg();
                msg.EntityId = m_remoteControl.EntityId;

                msg.Clipboard = clipboard;

                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            private static void OnSetAutoPilot(MySyncRemoteControl sync, ref SetAutoPilotMsg msg, MyNetworkClient sender)
            {
                sync.m_remoteControl.OnSetAutoPilotEnabled(msg.Enabled);
            }

            private static void OnSetDockingMode(MySyncRemoteControl sync, ref SetDockingModeMsg msg, MyNetworkClient sender)
            {
                sync.m_remoteControl.OnSetDockingMode(msg.Enabled);
            }

            private static void OnChangeFlightMode(MySyncRemoteControl sync, ref ChangeFlightModeMsg msg, MyNetworkClient sender)
            {
                sync.m_remoteControl.OnChangeFlightMode(msg.NewFlightMode);
            }

            private static void OnChangeDirection(MySyncRemoteControl sync, ref ChangeDirectionMsg msg, MyNetworkClient sender)
            {
                sync.m_remoteControl.OnChangeDirection(msg.NewDirection);
            }

            private static void OnRemoveWaypoints(MySyncRemoteControl sync, ref RemoveWaypointsMsg msg, MyNetworkClient sender)
            {
                sync.m_remoteControl.OnRemoveWaypoints(msg.WaypointIndexes);
                sync.m_syncing = false;
            }

            private static void OnMoveWaypointsUp(MySyncRemoteControl sync, ref MoveWaypointsUpMsg msg, MyNetworkClient sender)
            {
                sync.m_remoteControl.OnMoveWaypointsUp(msg.WaypointIndexes);
                sync.m_syncing = false;
            }

            private static void OnMoveWaypointsDown(MySyncRemoteControl sync, ref MoveWaypointsDownMsg msg, MyNetworkClient sender)
            {
                sync.m_remoteControl.OnMoveWaypointsDown(msg.WaypointIndexes);
                sync.m_syncing = false;
            }

            private static void OnAddWaypoints(MySyncRemoteControl sync, ref AddWaypointsMsg msg, MyNetworkClient sender)
            {
                sync.m_remoteControl.OnAddWaypoints(msg.Coords, msg.Names);
                sync.m_syncing = false;
            }

            private static void OnToolbarItemChanged(MySyncRemoteControl sync, ref ChangeToolbarItemMsg msg, MyNetworkClient sender)
            {
                sync.m_syncing = true;
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
                                builder.Parameters = msg.Item.Parameters;
                                item = MyToolbarItemFactory.CreateToolbarItem(builder);
                            }
                        }
                    }
                }

                var waypoint = sync.m_remoteControl.m_waypoints[msg.WaypointIndex];
                if (waypoint.Actions == null)
                {
                    waypoint.InitActions();
                }
                waypoint.Actions[msg.Index] = item;
                sync.m_remoteControl.RaisePropertiesChangedRemote();
                sync.m_syncing = false;
            }

            private static void OnResetWaypoint(MySyncRemoteControl sync, ref ResetWaypointMsg msg, MyNetworkClient sender)
            {
                sync.m_remoteControl.OnResetWaypoint();
            }

            private static void OnPasteAutopilotSetup(MySyncRemoteControl sync, ref PasteAutopilotSetupMsg msg, MyNetworkClient sender)
            {
                sync.m_remoteControl.OnPasteAutopilotSetup(msg.Clipboard);
            }
        }
    }
}
