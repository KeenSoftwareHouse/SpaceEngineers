using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Text;

using VRageMath;
using VRage;
using Sandbox.Game.Localization;
using VRage.Utils;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using Sandbox.Graphics.GUI;
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
using VRage.Library.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.EntityComponents;
using VRageRender;
using Sandbox.Game.AI.Navigation;
using VRage.Game;
using VRage.Network;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Serialization;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.Utils;
using VRage.Sync;
using VRage.Voxels;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;
using MyWaypointInfo = Sandbox.ModAPI.Ingame.MyWaypointInfo;
using Sandbox.Game.Weapons;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Voxels;
using VRage.Game.ObjectBuilders.AI;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_RemoteControl))]
    public class MyRemoteControl : MyShipController, IMyUsableEntity, IMyRemoteControl
    {
        private static readonly double PLANET_REPULSION_RADIUS = 2500;
        private static readonly double PLANET_AVOIDANCE_RADIUS = 5000;
        private static readonly double PLANET_AVOIDANCE_TOLERANCE = 100;

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

        private struct PlanetCoordInformation
        {
            public MyPlanet Planet;
            public double Elevation; // m above sea level
            public double Height; // m above the ground
            public Vector3D PlanetVector; // Planet-local direction vector from the center of the planet to the destination coord
            public Vector3D GravityWorld; // Direction of the gravity in the destination coord

            internal void Clear()
            {
                Planet = null;
                Elevation = 0.0f;
                Height = 0.0f;
                PlanetVector = Vector3D.Up;
                GravityWorld = Vector3D.Down;
            }

            internal bool IsValid()
            {
                return Planet != null;
            }

            internal void Calculate(Vector3D worldPoint)
            {
                Clear();
                var planet = MyGamePruningStructure.GetClosestPlanet(worldPoint);
                if (planet != null)
                {
                    var planetVector = worldPoint - planet.PositionComp.GetPosition();
                    var gravityLimit = ((MySphericalNaturalGravityComponent)planet.Components.Get<MyGravityProviderComponent>()).GravityLimit;
                    if (planetVector.Length() > gravityLimit)
                    {
                        return;
                    }

                    Planet = planet;
                    PlanetVector = planetVector;
                    if (!Vector3D.IsZero(PlanetVector))
                    {
                        GravityWorld = Vector3D.Normalize(Planet.Components.Get<MyGravityProviderComponent>().GetWorldGravity(worldPoint));

                        Vector3 localPoint = (Vector3)(worldPoint - Planet.WorldMatrix.Translation);
                        Vector3D closestPoint = Planet.GetClosestSurfacePointLocal(ref localPoint);
                        Height = Vector3D.Distance(localPoint, closestPoint);

                        Elevation = PlanetVector.Length();
                        PlanetVector *= 1.0 / Elevation;
                    }
                }
            }

            internal double EstimateDistanceToGround(Vector3D worldPoint)
            {
                Vector3D localPoint;
                MyVoxelCoordSystems.WorldPositionToLocalPosition(Planet.PositionLeftBottomCorner, ref worldPoint, out localPoint);
                return Math.Max(0.0f, Planet.Storage.DataProvider.GetDistanceToPoint(ref localPoint));
            }
        }

        private const float MAX_TERMINAL_DISTANCE_SQUARED = 10.0f;

        private float m_powerNeeded = 0.01f;
        private long? m_savedPreviousControlledEntityId = null;
        private IMyControllableEntity m_previousControlledEntity;
        private Sync<long> m_bindedCamera;
        private static MyTerminalControlCombobox<MyRemoteControl> m_cameraList = null;

        public IMyControllableEntity PreviousControlledEntity
        {
            get
            {
                if (m_savedPreviousControlledEntityId.HasValue)
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

        public MyAutopilotWaypoint CurrentWaypoint
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
        private PlanetCoordInformation m_destinationInfo;
        private PlanetCoordInformation m_currentInfo;

        private Vector3D m_currentWorldPosition;
        private Vector3D m_previousWorldPosition;

        private bool m_rotateBetweenWaypoints;
        public bool RotateBetweenWaypoints 
        {
            get { return MyFakes.ENABLE_VR_REMOTE_CONTROL_WAYPOINTS_FAST_MOVEMENT ? m_rotateBetweenWaypoints : false; }
            set { m_rotateBetweenWaypoints = value; }
        }

        private float m_currentAutopilotSpeedLimit = MyObjectBuilder_RemoteControl.DEFAULT_AUTOPILOT_SPEED_LIMIT;
        private Sync<float> m_autopilotSpeedLimit;

        private  readonly Sync<bool> m_useCollisionAvoidance;

        private int m_collisionCtr = 0;
        private Vector3D m_oldCollisionDelta = Vector3D.Zero;
        private float[] m_terrainHeightDetection = new float[TERRAIN_HEIGHT_DETECTION_SAMPLES];
        private int m_thdPtr = 0;
        private static readonly int TERRAIN_HEIGHT_DETECTION_SAMPLES = 8;

        private MyStuckDetection m_stuckDetection;

        private Vector3D m_dbgDelta;
        private Vector3D m_dbgDeltaH;

        private readonly Sync<bool> m_autoPilotEnabled;
        private readonly Sync<bool> m_dockingModeEnabled;
        private readonly Sync<FlightMode> m_currentFlightMode;
        private bool m_patrolDirectionForward = true;
        private Vector3D m_startPosition;
        private MyToolbar m_actionToolbar;
        private readonly Sync<Base6Directions.Direction> m_currentDirection;

        //new collision avoidance fields
        private Vector3D m_lastDelta = Vector3D.Zero;
        private float m_lastAutopilotSpeedLimit = 2;
        private int m_collisionAvoidanceFrameSkip = 0;
        private float m_rotateFor = 0f;
        private List<DetectedObject> m_detectedObstacles = new List<DetectedObject>();

        // Automatic behaviour of remote control (used for strafing, etc)
        public double TargettingAimDelta { get; private set; }
        public interface IRemoteControlAutomaticBehaviour
        {
            bool NeedUpdate { get; }
            bool IsActive { get; }

            bool RotateToTarget { get; set; }
            bool CollisionAvoidance { get; set; }
            int PlayerPriority { get; set; }
            float MaxPlayerDistance { get; }
            TargetPrioritization PrioritizationStyle { get; set; }
            MyEntity CurrentTarget { get; }
            List<DroneTarget> TargetList { get; }
            List<MyEntity> WaypointList { get; }
            bool WaypointActive { get; }
            bool CycleWaypoints { get; set; }
            Vector3D OriginPoint { get; set; }

            float PlayerYAxisOffset { get; }
            float WaypointThresholdDistance { get; }
            bool ResetStuckDetection { get; }

            void Update();
            void WaypointAdvanced();
            void TargetAdd(DroneTarget target);
            void TargetClear();
            void TargetRemove(MyEntity target);
            void TargetLoseCurrent();
            void WaypointAdd(MyEntity waypoint);
            void WaypointClear();

            void DebugDraw();
            void Load(MyObjectBuilder_AutomaticBehaviour objectBuilder, MyRemoteControl remoteControl);
            MyObjectBuilder_AutomaticBehaviour GetObjectBuilder();
        }

        private IRemoteControlAutomaticBehaviour m_automaticBehaviour;
        public IRemoteControlAutomaticBehaviour AutomaticBehaviour { get { return m_automaticBehaviour; } }
        private readonly Sync<float> m_waypointThresholdDistance;

        private static MyObjectBuilder_AutopilotClipboard m_clipboard;
        private static MyGuiControlListbox m_gpsGuiControl;
        private static MyGuiControlListbox m_waypointGuiControl;

        private static Dictionary<Base6Directions.Direction, MyStringId> m_directionNames = new Dictionary<Base6Directions.Direction, MyStringId>()
        {
            { Base6Directions.Direction.Forward, MyCommonTexts.Thrust_Forward },
            { Base6Directions.Direction.Backward, MyCommonTexts.Thrust_Back },
            { Base6Directions.Direction.Left, MyCommonTexts.Thrust_Left },
            { Base6Directions.Direction.Right, MyCommonTexts.Thrust_Right },
            { Base6Directions.Direction.Up, MyCommonTexts.Thrust_Up },
            { Base6Directions.Direction.Down, MyCommonTexts.Thrust_Down }
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

        bool m_syncing = false;

        public MyRemoteControl()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_autopilotSpeedLimit = SyncType.CreateAndAddProp<float>();
            m_useCollisionAvoidance = SyncType.CreateAndAddProp<bool>();
            m_autoPilotEnabled = SyncType.CreateAndAddProp<bool>();
            m_dockingModeEnabled = SyncType.CreateAndAddProp<bool>();
            m_currentFlightMode = SyncType.CreateAndAddProp<FlightMode>();
            m_currentDirection = SyncType.CreateAndAddProp<Base6Directions.Direction>();
            m_waypointThresholdDistance = SyncType.CreateAndAddProp<float>();
#endif // XB1
            CreateTerminalControls();
            TargettingAimDelta = 0;
            m_autoPilotEnabled.ValueChanged += (x) => OnSetAutoPilotEnabled();
            m_isMainRemoteControl.ValueChanged += (x) => MainRemoteControlChanged();
        }

        private void FillCameraComboBoxContent(ICollection<MyTerminalControlComboBoxItem> items)
        {
            items.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyCommonTexts.ScreenGraphicsOptions_AntiAliasing_None });
            bool bindedCameraExist = false;
            foreach (var block in CubeGrid.GetFatBlocks<MyCameraBlock>())
            {
                items.Add(new MyTerminalControlComboBoxItem() { Key = block.EntityId, Value = MyStringId.GetOrCompute(block.CustomName.ToString()) });
                if (block.EntityId == m_bindedCamera)
                    bindedCameraExist = true;
            }
            var group = MyCubeGridGroups.Static.Logical.GetGroup(CubeGrid);
            if (group != null)
            {
                foreach (var grid in group.Nodes)
                {
                    if (grid.NodeData != CubeGrid)
                    {
                        foreach (var block in grid.NodeData.GetFatBlocks<MyCameraBlock>())
                        {
                            items.Add(new MyTerminalControlComboBoxItem() { Key = block.EntityId, Value = MyStringId.GetOrCompute(block.CustomName.ToString()) });
                            if (block.EntityId == m_bindedCamera)
                                bindedCameraExist = true;
                        }
                    }
                }
            }
            if (!bindedCameraExist)
                m_bindedCamera.Value = 0;
        }

        protected override void CreateTerminalControls()
        {
            
            if (MyTerminalControlFactory.AreControlsCreated<MyRemoteControl>())
                return;
            base.CreateTerminalControls();

            var mainRemoteControl = new MyTerminalControlCheckbox<MyRemoteControl>("MainRemoteControl", MySpaceTexts.TerminalControlPanel_Cockpit_MainRemoteControl, MySpaceTexts.TerminalControlPanel_Cockpit_MainRemoteControl);
            mainRemoteControl.Getter = (x) => x.IsMainRemoteControl;
            mainRemoteControl.Setter = (x, v) => x.IsMainRemoteControl = v;
            mainRemoteControl.Enabled = (x) => x.IsMainRemoteControlFree();
            mainRemoteControl.EnableAction();
            MyTerminalControlFactory.AddControl(mainRemoteControl);

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

            var collisionAv = new MyTerminalControlOnOffSwitch<MyRemoteControl>("CollisionAvoidance", MySpaceTexts.BlockPropertyTitle_CollisionAvoidance, MySpaceTexts.Blank);
            collisionAv.Getter = (x) => x.m_useCollisionAvoidance;
            collisionAv.Setter = (x, v) => x.SetCollisionAvoidance(v);
            collisionAv.Enabled = r => true;
            collisionAv.EnableToggleAction();
            collisionAv.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(collisionAv);

            var dockignMode = new MyTerminalControlOnOffSwitch<MyRemoteControl>("DockingMode", MySpaceTexts.BlockPropertyTitle_EnableDockingMode, MySpaceTexts.Blank);
            dockignMode.Getter = (x) => x.m_dockingModeEnabled;
            dockignMode.Setter = (x, v) => x.SetDockingMode(v);
            dockignMode.Enabled = r => r.IsWorking;
            dockignMode.EnableToggleAction();
            dockignMode.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(dockignMode);

            var cameraList = new MyTerminalControlCombobox<MyRemoteControl>("CameraList", MySpaceTexts.BlockPropertyTitle_AssignedCamera, MySpaceTexts.Blank);
            cameraList.ComboBoxContentWithBlock = (x, list) => x.FillCameraComboBoxContent(list);
            cameraList.Getter = (x) => (long)x.m_bindedCamera;
            cameraList.Setter = (x, y) => x.m_bindedCamera.Value = y;
            MyTerminalControlFactory.AddControl(cameraList);
            m_cameraList = cameraList;

            var flightMode = new MyTerminalControlCombobox<MyRemoteControl>("FlightMode", MySpaceTexts.BlockPropertyTitle_FlightMode, MySpaceTexts.Blank);
            flightMode.ComboBoxContent = (x) => FillFlightModeCombo(x);
            flightMode.Getter = (x) => (long)x.m_currentFlightMode.Value;
            flightMode.Setter = (x, v) => x.ChangeFlightMode((FlightMode)v);
            flightMode.SetSerializerRange((int)MyEnum<FlightMode>.Range.Min, (int)MyEnum<FlightMode>.Range.Max);
            MyTerminalControlFactory.AddControl(flightMode);

            var directionCombo = new MyTerminalControlCombobox<MyRemoteControl>("Direction", MySpaceTexts.BlockPropertyTitle_ForwardDirection, MySpaceTexts.Blank);
            directionCombo.ComboBoxContent = (x) => FillDirectionCombo(x);
            directionCombo.Getter = (x) => (long)x.m_currentDirection.Value;
            directionCombo.Setter = (x, v) => x.ChangeDirection((Base6Directions.Direction)v);
            MyTerminalControlFactory.AddControl(directionCombo);

            if (MyFakes.ENABLE_VR_REMOTE_BLOCK_AUTOPILOT_SPEED_LIMIT)
            {
                var sliderSpeedLimit = new MyTerminalControlSlider<MyRemoteControl>("SpeedLimit", MySpaceTexts.BlockPropertyTitle_RemoteBlockSpeedLimit,
                    MySpaceTexts.BlockPropertyTitle_RemoteBlockSpeedLimit);
                sliderSpeedLimit.SetLimits(1, 200);
                sliderSpeedLimit.DefaultValue = MyObjectBuilder_RemoteControl.DEFAULT_AUTOPILOT_SPEED_LIMIT;
                sliderSpeedLimit.Getter = (x) => x.m_autopilotSpeedLimit;
                sliderSpeedLimit.Setter = (x, v) => x.m_autopilotSpeedLimit.Value = v;
                sliderSpeedLimit.Writer = (x, sb) => sb.Append(MyValueFormatter.GetFormatedFloat(x.m_autopilotSpeedLimit, 0));
                sliderSpeedLimit.EnableActions();
                MyTerminalControlFactory.AddControl(sliderSpeedLimit);
            }

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

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true; 
            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                BlockDefinition.ResourceSinkGroup,
                m_powerNeeded,
                this.CalculateRequiredPowerInput);

           
            ResourceSink = sinkComp;
            base.Init(objectBuilder, cubeGrid);
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;

            var remoteOb = (MyObjectBuilder_RemoteControl)objectBuilder;
            m_savedPreviousControlledEntityId = remoteOb.PreviousControlledEntityId;

            sinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;
            sinkComp.RequiredInputChanged += Receiver_RequiredInputChanged;
			
            sinkComp.Update();

            m_autoPilotEnabled.Value = remoteOb.AutoPilotEnabled;
            m_dockingModeEnabled.Value = remoteOb.DockingModeEnabled;
            m_currentFlightMode.Value = (FlightMode)remoteOb.FlightMode;
            m_currentDirection.Value = (Base6Directions.Direction)remoteOb.Direction;
            m_autopilotSpeedLimit.Value = remoteOb.AutopilotSpeedLimit;
            m_bindedCamera.Value = remoteOb.BindedCamera;
            m_waypointThresholdDistance.Value = remoteOb.WaypointThresholdDistance;
            m_currentAutopilotSpeedLimit = m_autopilotSpeedLimit;
            IsMainRemoteControl = remoteOb.IsMainRemoteControl;

            m_stuckDetection = new MyStuckDetection(0.03f, 0.01f, this.CubeGrid.PositionComp.WorldAABB);

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

            UpdatePlanetWaypointInfo();

            m_actionToolbar = new MyToolbar(MyToolbarType.ButtonPanel, pageCount: 1);
            m_actionToolbar.DrawNumbers = false;
            m_actionToolbar.Init(null, this);

            m_selectedGpsLocations = new List<IMyGps>();
            m_selectedWaypoints = new List<MyAutopilotWaypoint>();
            UpdateText();

            AddDebugRenderComponent(new MyDebugRenderComponentRemoteControl(this));

            m_useCollisionAvoidance.Value = remoteOb.CollisionAvoidance;
            if (remoteOb.AutomaticBehaviour != null)
            {
                if (remoteOb.AutomaticBehaviour is MyObjectBuilder_DroneStrafeBehaviour)
                {
                    MyDroneStrafeBehaviour behavior = new MyDroneStrafeBehaviour();
                    behavior.Load(remoteOb.AutomaticBehaviour, this);
                    SetAutomaticBehaviour(behavior);
                }
            }

            for (int i = 0; i < TERRAIN_HEIGHT_DETECTION_SAMPLES; i++)
            {
                m_terrainHeightDetection[i] = 0.0f;
            }
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            UpdateEmissivity();
			ResourceSink.Update();

            if (m_autoPilotEnabled)
            {
                var group = ControlGroup.GetGroup(CubeGrid);
                if (group != null)
                {
                    group.GroupData.ControlSystem.AddControllerBlock(this);
                }
            }
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

            m_previousWorldPosition = m_currentWorldPosition;
            m_currentWorldPosition = WorldMatrix.Translation;

            if (m_savedPreviousControlledEntityId.HasValue)
            {
                MySession.Static.Players.UpdatePlayerControllers(EntityId);
                if (TryFindSavedEntity())
                {
                    m_savedPreviousControlledEntityId = null;
                }
            }
            UpdateAutopilot();
        }

        #region Autopilot GUI
        private bool CanEnableAutoPilot()
        {
            //by Gregory: disable autopilot when  no waypoints or only one way point in circle or patrol
            if (m_automaticBehaviour == null && (m_waypoints.Count == 0 || (m_waypoints.Count == 1 && m_currentFlightMode != FlightMode.OneWay)))
                return false;
            
            return IsFunctional && m_previousControlledEntity == null;
        }

        private static void FillFlightModeCombo(List<MyTerminalControlComboBoxItem> list)
        {
            list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MySpaceTexts.BlockPropertyTitle_FlightMode_Patrol });
            list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MySpaceTexts.BlockPropertyTitle_FlightMode_Circle });
            list.Add(new MyTerminalControlComboBoxItem() { Key = 2, Value = MySpaceTexts.BlockPropertyTitle_FlightMode_OneWay });
        }

        private static void FillDirectionCombo(List<MyTerminalControlComboBoxItem> list)
        {
            foreach (var direction in m_directionNames)
            {
                list.Add(new MyTerminalControlComboBoxItem() { Key = (long)direction.Key, Value = direction.Value });
            }
        }

        public void SetCollisionAvoidance(bool enabled)
        {
            m_useCollisionAvoidance.Value = enabled;
        }

        public void SetAutoPilotEnabled(bool enabled)
        {
            if (CanEnableAutoPilot())
            {
                if (enabled == false)
                {
                    ClearMovementControl();
                }
            m_autoPilotEnabled.Value = enabled;
        }
            }

        bool ModAPI.Ingame.IMyRemoteControl.IsAutoPilotEnabled
        {
            get { return m_autoPilotEnabled.Value; }
        }

        public bool IsAutopilotEnabled()
        {
            return m_autoPilotEnabled.Value;
        }

        public bool HasWaypoints()
        {
            return m_waypoints.Count > 0;
        }

        public void SetWaypointThresholdDistance(float thresholdDistance)
        {
            m_waypointThresholdDistance.Value = thresholdDistance;
        }

        void RemoveAutoPilot()
        {
            var thrustComp = CubeGrid.Components.Get<MyEntityThrustComponent>();
            if (thrustComp != null)
                thrustComp.AutoPilotControlThrust = Vector3.Zero;
            CubeGrid.GridSystems.GyroSystem.ControlTorque = Vector3.Zero;

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

        private void OnSetAutoPilotEnabled()
        {
            if (!m_autoPilotEnabled)
            {
                RemoveAutoPilot();
            }
            else
            {
                var group = ControlGroup.GetGroup(CubeGrid);
                if (group != null)
                {
                    group.GroupData.ControlSystem.AddControllerBlock(this);
                }
                SetAutopilot(true);

                ResetShipControls();
            }
        }

        private void SetAutopilot(bool enabled)
        {
			var thrustComp = CubeGrid.Components.Get<MyEntityThrustComponent>();

            if (thrustComp != null)
            {
                thrustComp.AutopilotEnabled = enabled;
                thrustComp.MarkDirty();
            }
            if (CubeGrid.GridSystems.GyroSystem != null)
            {
                CubeGrid.GridSystems.GyroSystem.AutopilotEnabled = enabled;
                CubeGrid.GridSystems.GyroSystem.MarkDirty();
            }
        }

        public void SetDockingMode(bool enabled)
        {
            m_dockingModeEnabled.Value = enabled;
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

                MyMultiplayer.RaiseEvent(this,x=> x.OnAddWaypoints,coords, names);
                m_selectedGpsLocations.Clear();
            }
        }

        [Event, Reliable, Server, Broadcast]
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
                    MyMultiplayer.RaiseEvent(this, x => x.OnMoveWaypointsUp, indexes);
                }
            }
        }

        [Event, Reliable, Server, Broadcast]
        private void OnMoveWaypointsUp(List<int> indexes)
        {
            for (int i = 0; i < indexes.Count; i++)
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
                    MyMultiplayer.RaiseEvent(this, x => x.OnMoveWaypointsDown, indexes);
                }
            }
        }

        [Event, Reliable, Server, Broadcast]
        private void OnMoveWaypointsDown(List<int> indexes)
        {
            for (int i = indexes.Count - 1; i >= 0; i--)
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
                MyMultiplayer.RaiseEvent(this, x => x.OnRemoveWaypoints, indexes);
               
                m_selectedWaypoints.Clear();
                RaisePropertiesChangedRemote();
            }
        }

        [Event, Reliable, Server, Broadcast]
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

        public void ChangeFlightMode(FlightMode flightMode)
        {
            if (flightMode != m_currentFlightMode)
            {
                m_currentFlightMode.Value = flightMode;
            }

            SetAutoPilotEnabled(m_autoPilotEnabled);
        }

        public void SetAutoPilotSpeedLimit(float speedLimit)
        {
            m_currentAutopilotSpeedLimit = speedLimit;
        }

        public void ChangeDirection(Base6Directions.Direction direction)
        {
            m_currentDirection.Value = direction;
        }

        private void OnChangeDirection(Base6Directions.Direction direction)
        {
            m_currentDirection.Value = direction;
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
            MyMultiplayer.RaiseEvent(this, x => x.OnResetWaypoint);
            if(Sync.IsServer == false)
            {
                OnResetWaypoint();
            }
        }

        [Event, Reliable, Server, BroadcastExcept]
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
            m_clipboard.Direction = (byte)m_currentDirection.Value;
            m_clipboard.FlightMode = (int)m_currentFlightMode.Value;
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
                MyMultiplayer.RaiseEvent(this, x => x.OnPasteAutopilotSetup, m_clipboard);
            }
        }

        [Event, Reliable, Server, Broadcast]
        private void OnPasteAutopilotSetup(MyObjectBuilder_AutopilotClipboard clipboard)
        {
            m_currentDirection.Value = (Base6Directions.Direction)clipboard.Direction;
            m_currentFlightMode.Value = (FlightMode)clipboard.FlightMode;
            m_dockingModeEnabled.Value = clipboard.DockingModeEnabled;
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
    
        public void ClearWaypoints()
        {
            MyMultiplayer.RaiseEvent(this, x => x.ClearWaypoints_Implementation);
            if (Sync.IsServer == false)
            {
                ClearWaypoints_Implementation();
            }
        }

        void ModAPI.Ingame.IMyRemoteControl.GetWaypointInfo(List<MyWaypointInfo> waypoints)
        {
            if (waypoints == null)
                return;
            waypoints.Clear();
            for (int index = 0; index < m_waypoints.Count; index++)
            {
                var waypoint = m_waypoints[index];
                waypoints.Add(new MyWaypointInfo(waypoint.Name, waypoint.Coords));
            }
        }

        [Event, Reliable, Server, BroadcastExcept]
        void ClearWaypoints_Implementation()
        {
            m_waypoints.Clear();
            m_currentAutopilotSpeedLimit = m_autopilotSpeedLimit;
            AdvanceWaypoint();
            RaisePropertiesChangedRemote();
        }

        public void AddWaypoint(Vector3D point, string name)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnAddWaypoint, point, name);
        }

        [Event, Reliable, Server, Broadcast]
        private void OnAddWaypoint(Vector3D point, string name)
        {
            m_waypoints.Add(new MyAutopilotWaypoint(point, name, this));
            RaisePropertiesChangedRemote();
        }

        public void AddWaypoint(Vector3D point, string name, List<MyObjectBuilder_ToolbarItem> actionBuilders)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnAddWaypoint, point, name, actionBuilders);
        }

        [Event,Reliable,Server,Broadcast]
        private void OnAddWaypoint(Vector3D point, string name, [DynamicItem(typeof(MyObjectBuilderDynamicSerializer))] List<MyObjectBuilder_ToolbarItem> actionBuilders)
        {
            m_waypoints.Add(new MyAutopilotWaypoint(point, name, actionBuilders, null, this));
            RaisePropertiesChangedRemote();
        }

        private void FillGpsList(ICollection<MyGuiControlListbox.Item> gpsItemList, ICollection<MyGuiControlListbox.Item> selectedGpsItemList)
        {
            List<IMyGps> gpsList = new List<IMyGps>();
            MySession.Static.Gpss.GetGpsList(MySession.Static.LocalPlayerId, gpsList);
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
                SendToolbarItemChanged(ToolbarItem.FromItem(self.GetItemAtIndex(index.ItemIndex)), index.ItemIndex, m_waypoints.IndexOf(m_selectedWaypoints[0]));
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
            if (IsWorking && m_autoPilotEnabled)
            {
                var shipController = CubeGrid.GridSystems.ControlSystem.GetShipController();
                if (shipController == null)
                {
                    var group = ControlGroup.GetGroup(CubeGrid);
                    if (group != null)
                    {
                        group.GroupData.ControlSystem.AddControllerBlock(this);
                    }
                    shipController = CubeGrid.GridSystems.ControlSystem.GetShipController();
                }

                if (shipController == this)
                {
					var thrustComp = CubeGrid.Components.Get<MyEntityThrustComponent>();
                    if (thrustComp != null && thrustComp.AutopilotEnabled == false) thrustComp.AutopilotEnabled = true;
                    Debug.Assert(CubeGrid.GridSystems.GyroSystem.AutopilotEnabled == true);

                    if (CurrentWaypoint == null && m_waypoints.Count > 0)
                    {
                        CurrentWaypoint = m_waypoints[0];
                        UpdatePlanetWaypointInfo();
                        UpdateText();
                    }

                    if (CurrentWaypoint != null)
                    {
                        if (IsInStoppingDistance() || m_stuckDetection.IsStuck)
                        {
                            AdvanceWaypoint();
                        }

                        if (MyFakes.ENABLE_VR_REMOTE_CONTROL_WAYPOINTS_FAST_MOVEMENT)
                        {
                            // Skip all reached waypoints at once
                            MyAutopilotWaypoint oldWaypoint = null;
                            while (CurrentWaypoint != null && CurrentWaypoint != oldWaypoint 
                                && (m_automaticBehaviour == null || !m_automaticBehaviour.IsActive) 
                                && IsInStoppingDistance())
                            {
                                oldWaypoint = CurrentWaypoint;
                                AdvanceWaypoint();
                            }
                        }

                        if (Sync.IsServer && CurrentWaypoint != null && !IsInStoppingDistance() && m_autoPilotEnabled) // Autopilot can be disabled in AdvanceWaypoint
                        {
                            Vector3D deltaPos, perpDeltaPos, targetDelta;
                            bool rotating, isLabile;
                            float autopilotSpeedLimit;

                            CalculateDeltaPos(out deltaPos, out perpDeltaPos, out targetDelta, out autopilotSpeedLimit);
                            UpdateGyro(targetDelta, perpDeltaPos, out rotating, out isLabile);

                            m_stuckDetection.SetRotating(rotating);

                            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
                                MyRenderProxy.DebugDrawLine3D(WorldMatrix.Translation, WorldMatrix.Translation + deltaPos, Color.Green, Color.GreenYellow, false, false);
                            m_rotateFor -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                            if ((m_automaticBehaviour == null || m_automaticBehaviour.RotateToTarget == false || m_automaticBehaviour.CurrentTarget == null) 
                                && rotating && !isLabile
                                && (!MyFakes.ENABLE_NEW_COLLISION_AVOIDANCE || !m_useCollisionAvoidance.Value || Vector3D.DistanceSquared(CurrentWaypoint.Coords, CubeGrid.WorldMatrix.Translation) < 25))
                            {
                                if (thrustComp != null)
                                    thrustComp.AutoPilotControlThrust = Vector3.Zero;
                            }
                            else
                            {
                                UpdateThrust(deltaPos, perpDeltaPos, autopilotSpeedLimit);
                            }
                        }
                    }
                    else if (Sync.IsServer && m_automaticBehaviour != null && m_automaticBehaviour.IsActive && m_automaticBehaviour.RotateToTarget)
                    {
                        bool rotating, isLabile;
                        UpdateGyro(Vector3.Zero, Vector3.Zero, out rotating, out isLabile);

                        if (rotating && !isLabile)
                        {
                            if (thrustComp != null)
                                thrustComp.AutoPilotControlThrust = Vector3.Zero;
                        }
                    }

                    m_stuckDetection.Update(this.WorldMatrix.Translation, this.WorldMatrix.Forward, CurrentWaypoint == null ? Vector3D.Zero : CurrentWaypoint.Coords);
                }

                if (m_automaticBehaviour != null)
                    m_automaticBehaviour.Update();
            }
        }

        private bool IsInStoppingDistance()
        {
            int currentIndex = m_waypoints.IndexOf(CurrentWaypoint);
            double currentDstSqr = (WorldMatrix.Translation - CurrentWaypoint.Coords).LengthSquared();
            double dstError = CubeGrid.GridSize * 3;

            if (m_automaticBehaviour != null && m_automaticBehaviour.IsActive)
            {
                dstError = m_automaticBehaviour.WaypointThresholdDistance;
            }
            else if (m_waypointThresholdDistance > 0)
            {
                dstError = m_waypointThresholdDistance;
            }
            else if (m_dockingModeEnabled || (m_currentFlightMode == FlightMode.OneWay && currentIndex == m_waypoints.Count - 1))
            {
                if (CubeGrid.GridSize >= 0.5f)
                    dstError = CubeGrid.GridSize * 0.25;
                else
                    dstError = CubeGrid.GridSize;
            }

            if (MyFakes.ENABLE_VR_REMOTE_CONTROL_WAYPOINTS_FAST_MOVEMENT)
            {
                // Check if movement from previous position to current position intersects waypoint sphere.
                if (currentDstSqr < dstError * dstError)
                    return true;

                double prevDstSqr = (m_previousWorldPosition - CurrentWaypoint.Coords).LengthSquared();
                if (prevDstSqr < dstError * dstError)
                    return true;

                var dir = WorldMatrix.Translation - m_previousWorldPosition;
                double rayLength = dir.Normalize();
                if (rayLength > 0.01)
                {
                    RayD ray = new RayD(m_previousWorldPosition, dir);
                    BoundingSphereD sphere = new BoundingSphereD(CurrentWaypoint.Coords, dstError);
                    double? intersection = sphere.Intersects(ray);
                    return (intersection != null) ? intersection.Value <= rayLength : false;
                }
            }

            return currentDstSqr < dstError * dstError;
        }

        private void AdvanceWaypoint()
        {
            int currentIndex = m_waypoints.IndexOf(CurrentWaypoint);
            var oldWaypoint = CurrentWaypoint;

            bool enableAutopilot = m_autoPilotEnabled;

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
                            if (m_waypoints.Count != 1)
                            {
                                currentIndex = m_waypoints.Count - 2;
                            }
                            else
                            {
                                currentIndex = 0;
                            }
                            m_patrolDirectionForward = false;
                        }
                    }
                    else
                    {
                        currentIndex--;
                        if (currentIndex < 0)
                        {
                            currentIndex = 1;
                            if (m_waypoints.Count == 1)
                            {
                                currentIndex = 0;
                            }
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

						var thrustComp = CubeGrid.Components.Get<MyEntityThrustComponent>();

						if(thrustComp != null)
                        thrustComp.AutoPilotControlThrust = Vector3.Zero;

                        if (Sync.IsServer)
                        {
                            enableAutopilot = false;
                        }
                    }
                }
            }

            if (currentIndex < 0 || currentIndex >= m_waypoints.Count)
            {
                CurrentWaypoint = null;
                if (Sync.IsServer)
                {
                    enableAutopilot = false;
                }
                UpdatePlanetWaypointInfo();
                UpdateText();
            }
            else
            {
                CurrentWaypoint = m_waypoints[currentIndex];

                if (CurrentWaypoint != oldWaypoint || m_waypoints.Count == 1)
                {
                    if (Sync.IsServer && oldWaypoint.Actions != null)
                    {
                        for (int i = 0; i < oldWaypoint.Actions.Length; i++)
                        {
                            if (oldWaypoint.Actions[i] != null)
                            {
                                m_actionToolbar.SetItemAtIndex(0, oldWaypoint.Actions[i]);
                                m_actionToolbar.UpdateItem(0);
                                m_actionToolbar.ActivateItemAtSlot(0);

                                var action = m_actionToolbar.GetItemAtSlot(0);
                                
                                //The action activated maybe to activate autopilot. Then we must take this into account
                                if (Sync.IsServer && action != null && action.DisplayName.ToString().Contains("Autopilot"))
                                {
                                    enableAutopilot = m_autoPilotEnabled.Value;
                                }
                                
                                //by Gregory temporary fix in order not to get the action looping for one waypoint
                                if (Sync.IsServer && m_waypoints.Count == 1)
                                {
                                    enableAutopilot = false;
                                }
                                
                            }
                        }
                        m_actionToolbar.Clear();
                    }

                    UpdatePlanetWaypointInfo();
                    UpdateText();
                }
            }

            if (Sync.IsServer && enableAutopilot != m_autoPilotEnabled)
            {
                SetAutoPilotEnabled(enableAutopilot);
            }

            bool forceResetStuckDetection = m_automaticBehaviour != null && m_automaticBehaviour.IsActive && m_automaticBehaviour.ResetStuckDetection;
            m_stuckDetection.Reset(force: forceResetStuckDetection);

            if (m_automaticBehaviour != null)
                m_automaticBehaviour.WaypointAdvanced();
        }

        private void UpdatePlanetWaypointInfo()
        {
            if (CurrentWaypoint == null) m_destinationInfo.Clear();
            else m_destinationInfo.Calculate(CurrentWaypoint.Coords);
        }

        private Vector3D GetAngleVelocity(QuaternionD q1, QuaternionD q2)
        {
            q1.Conjugate();
            QuaternionD r = q2 * q1;

            double angle = 2 * System.Math.Acos(MathHelper.Clamp(r.W, -1, 1));
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

        private void UpdateGyro(Vector3D deltaPos, Vector3D perpDeltaPos, out bool rotating, out bool isLabile)
        {
            isLabile = false; // Whether the rotation is currently near a value, where there is discontinuity in the desired rotation
            rotating = true;  // Whether the gyros are currently performing a rotation

            var gyros = CubeGrid.GridSystems.GyroSystem;
            gyros.ControlTorque = Vector3.Zero;
            Vector3D angularVelocity = CubeGrid.Physics.AngularVelocity;
            var orientation = GetOrientation();
            Matrix invWorldRot = CubeGrid.PositionComp.WorldMatrixNormalizedInv.GetOrientation();

            Vector3D gravity = m_currentInfo.GravityWorld;
            QuaternionD current = QuaternionD.CreateFromRotationMatrix(orientation);

            Vector3D targetDirection;
            QuaternionD target;
            if (m_currentInfo.IsValid())
            {
                targetDirection = perpDeltaPos;
                targetDirection.Normalize();
                target = QuaternionD.CreateFromForwardUp(targetDirection, -gravity);

                // If directly above or below the target, the target orientation changes very quickly
                isLabile = Vector3D.Dot(targetDirection, orientation.Forward) > 0.95 || Math.Abs(Vector3D.Dot(Vector3D.Normalize(deltaPos), gravity)) > 0.95;
            }
            else
            {
                targetDirection = deltaPos;
                targetDirection.Normalize();
                target = QuaternionD.CreateFromForwardUp(targetDirection, orientation.Up);
            }

            // Rotate to enemy player when strafing
            if (m_automaticBehaviour != null && m_automaticBehaviour.IsActive && m_automaticBehaviour.RotateToTarget && m_automaticBehaviour.CurrentTarget != null)
            {
                isLabile = false;
                var forwardOrientation = MatrixD.CreateWorld(Vector3D.Zero, (Vector3D)Base6Directions.GetVector(Base6Directions.Direction.Forward), m_upVectors[Base6Directions.Direction.Forward]);
                orientation = forwardOrientation * WorldMatrix.GetOrientation();
                current = QuaternionD.CreateFromRotationMatrix(orientation);

                targetDirection = m_automaticBehaviour.CurrentTarget.WorldMatrix.Translation - WorldMatrix.Translation;
                if (m_automaticBehaviour.CurrentTarget is MyCharacter)
                    targetDirection += m_automaticBehaviour.CurrentTarget.WorldMatrix.Up * m_automaticBehaviour.PlayerYAxisOffset;
                targetDirection.Normalize();

                Vector3D up = m_automaticBehaviour.CurrentTarget.WorldMatrix.Up;
                up.Normalize();

                if (Math.Abs(Vector3D.Dot(targetDirection, up)) >= 0.98)
                {
                    up = Vector3D.CalculatePerpendicularVector(targetDirection);
                }
                else
                {
                    Vector3D right = Vector3D.Cross(targetDirection, up);
                    up = Vector3D.Cross(right, targetDirection);
                }

                target = QuaternionD.CreateFromForwardUp(targetDirection, up);

                //rotating = false;
            }

            Vector3D velocity = GetAngleVelocity(current, target);
            Vector3D velocityToTarget = velocity * angularVelocity.Dot(ref velocity);

            velocity = Vector3D.Transform(velocity, invWorldRot);
            double angle = System.Math.Acos(MathHelper.Clamp(Vector3D.Dot(targetDirection, orientation.Forward),-1,1));

            TargettingAimDelta = angle;
            if (angle < 0.01)
            {
                rotating = false;
                return;
            }

            rotating = rotating && !RotateBetweenWaypoints;

            Vector3D deceleration = angularVelocity - gyros.GetAngularVelocity(-velocity);
            double timeToStop = (angularVelocity / deceleration).Max();
            double timeToReachTarget = (angle / velocityToTarget.Length()) * angle;
            // TODO: this should be rewritten (no IsNan, IsInfinity)
            if (double.IsNaN(timeToStop) || double.IsInfinity(timeToReachTarget) || timeToReachTarget > timeToStop)
            {
                if (m_dockingModeEnabled)
                {
                    velocity /= 4.0;
                }
                gyros.ControlTorque = velocity;
                gyros.MarkDirty();
            }
            else if (angle < 0.1 && m_automaticBehaviour != null && m_automaticBehaviour.RotateToTarget && m_automaticBehaviour.CurrentTarget != null)
            {
                gyros.ControlTorque = velocity / 3.0;
                gyros.MarkDirty();
            }

            if (m_dockingModeEnabled)
            {
                if (angle > 0.05) return;
            }
            else
            {
                if (angle > 0.25) return;
            }
        }

        private void CalculateDeltaPos(out Vector3D deltaPos, out Vector3D perpDeltaPos, out Vector3D targetDelta, out float autopilotSpeedLimit)
        {
            autopilotSpeedLimit = m_currentAutopilotSpeedLimit;
            m_currentInfo.Calculate(WorldMatrix.Translation);
            Vector3D targetPos = CurrentWaypoint.Coords;
            Vector3D currentPos = WorldMatrix.Translation;
            targetDelta = targetPos - currentPos;

            if (m_useCollisionAvoidance)
            {
                if (MyFakes.ENABLE_NEW_COLLISION_AVOIDANCE)
                {
                    deltaPos = AvoidCollisionsVs2(targetDelta, ref autopilotSpeedLimit);
                    targetDelta = deltaPos;
                }
                else
                {
                    deltaPos = AvoidCollisions(targetDelta, ref autopilotSpeedLimit);
                }
            }
            else
            {
                deltaPos = targetDelta;
            }

            perpDeltaPos = Vector3D.Reject(targetDelta, m_currentInfo.GravityWorld);
        }

        public struct DetectedObject
        {
            public float Distance;
            public Vector3D Position;
            public bool IsVoxel;

            public DetectedObject(float dist, Vector3D pos, bool voxel)
            {
                Distance = dist;
                Position = pos;
                IsVoxel = voxel;
            }
        }

        private void FillListOfDetectedObjects(Vector3D pos, MyEntity parentEntity, ref int listLimit, ref Vector3D shipFront, ref float closestEntityDist, ref MyEntity closestEntity)
        {
            float dist = Vector3.DistanceSquared((Vector3)pos, (Vector3)shipFront);
            if (dist < closestEntityDist)
            {
                closestEntityDist = dist;
                closestEntity = parentEntity;
            }
            if (m_detectedObstacles.Count == 0)
            {
                m_detectedObstacles.Add(new DetectedObject(dist, pos, parentEntity is MyVoxelBase));
            }
            else
            {
                for (int i = 0; i < m_detectedObstacles.Count; i++)
                {
                    if (dist < m_detectedObstacles[i].Distance)
                    {
                        if (m_detectedObstacles.Count == listLimit)
                            m_detectedObstacles.RemoveAt(listLimit - 1);
                        m_detectedObstacles.AddOrInsert(new DetectedObject(dist, pos, parentEntity is MyVoxelBase), i);
                        break;
                    }
                }
            }
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
                MyRenderProxy.DebugDrawSphere(pos, 1.5f, Color.Red, 1f, false);
        }

        private Vector3D AvoidCollisionsVs2(Vector3D delta, ref float autopilotSpeedLimit)
        {
            if (m_collisionAvoidanceFrameSkip > 0)
            {
                m_collisionAvoidanceFrameSkip--;
                autopilotSpeedLimit = m_lastAutopilotSpeedLimit;
                return m_lastDelta;
            }
            m_collisionAvoidanceFrameSkip = 19;

            MyEntityThrustComponent thrustSystem = CubeGrid.Components.Get<MyEntityThrustComponent>();
            if (thrustSystem == null)
                return delta;

            bool debugDraw = MyDebugDrawSettings.ENABLE_DEBUG_DRAW;
            //debugDraw = true;
            float farRatio = 1;
            int listLimit = 5;
            bool avoidCharacters = true;
            Vector3D originalDelta = delta;

            //detection values
            float mass = CubeGrid.GetCurrentMass();
            float velocity = Math.Max(CubeGrid.Physics.LinearVelocity.Length(), 3);
            float radiusRatio = velocity <= 3 ? 1 : 1.25f;
            float thrusterForce = thrustSystem.GetMaxThrustInDirection(m_currentDirection.Value);
            double decceleration = thrusterForce / mass;
            double deccelerationTime = velocity / decceleration;
            double stoppingDistance = deccelerationTime * velocity / 2;

            //detection positions
            Vector3D direction = Vector3D.Normalize(CubeGrid.Physics.LinearVelocity);
            Vector3D speedDelta = direction * stoppingDistance;
            Vector3D shipFront = CubeGrid.PositionComp.LocalVolume.Radius * direction + CubeGrid.PositionComp.WorldAABB.Center;
            Vector3D shipBack = CubeGrid.PositionComp.WorldAABB.Center - CubeGrid.PositionComp.LocalVolume.Radius * direction;
            Vector3D farPoint = shipFront + direction * stoppingDistance;

            //bounding boxes
            Quaternion orientation = Quaternion.CreateFromForwardUp(direction, CubeGrid.WorldMatrix.Up);
            MatrixD orientationMatrix = MatrixD.CreateFromQuaternion(orientation);
            MyOrientedBoundingBoxD closeBoundingBox = new MyOrientedBoundingBoxD((shipBack + farPoint) / 2, new Vector3D(CubeGrid.PositionComp.LocalVolume.Radius * radiusRatio, CubeGrid.PositionComp.LocalVolume.Radius * radiusRatio, stoppingDistance / 2 + CubeGrid.PositionComp.LocalVolume.Radius * 2), orientation);
            MyOrientedBoundingBoxD farBoundingBox = new MyOrientedBoundingBoxD(farPoint + farRatio * speedDelta / 2, new Vector3D(CubeGrid.PositionComp.LocalVolume.Radius * radiusRatio, CubeGrid.PositionComp.LocalVolume.Radius * radiusRatio, farRatio * stoppingDistance / 2), orientation);
            BoundingBoxD closeBoundingBoxLocal = new BoundingBoxD(closeBoundingBox.Center - closeBoundingBox.HalfExtent, closeBoundingBox.Center + closeBoundingBox.HalfExtent);
            if (debugDraw && m_rotateFor <= 0)
            {
                MyRenderProxy.DebugDrawOBB(closeBoundingBox, Color.Red, 0.25f, false, false, false);
                //MyRenderProxy.DebugDrawLine3D(shipFront, farPoint, Color.Red, Color.Yellow, false, false);
            }

            //close check - find all possible obstacles in boundaries
            BoundingSphereD sphere = new BoundingSphereD(shipFront + speedDelta / 2, stoppingDistance / 2);
            List<MyEntity> resultEntities = new List<MyEntity>();
            List<MySlimBlock> resultBlocks = new List<MySlimBlock>();
            MyGamePruningStructure.GetAllTargetsInSphere(ref sphere, resultEntities, MyEntityQueryType.Both);
            MyEntity closestEntity = null;
            float closestEntityDist = float.MaxValue;
            bool voxels = false;
            foreach (var entity in resultEntities)
            {
                if (entity is MyCubeGrid)
                {
                    MyCubeGrid grid = (MyCubeGrid)entity;
                    if (MyCubeGridGroups.Static.Physical.GetGroup(CubeGrid) == MyCubeGridGroups.Static.Physical.GetGroup(grid))
                        continue;

                    foreach (var block in grid.GetBlocks())
                    {
                        Vector3D pos = block.WorldPosition;
                        if (closeBoundingBox.Contains(ref pos))
                            FillListOfDetectedObjects(pos, grid, ref listLimit, ref shipFront, ref closestEntityDist, ref closestEntity);
                    }
                }
                if (entity is MyCharacter && avoidCharacters)
                    FillListOfDetectedObjects(entity.WorldMatrix.Translation, entity, ref listLimit, ref shipFront, ref closestEntityDist, ref closestEntity);

                if (entity is MyVoxelBase)
                    voxels = true;
            }

            //voxels are inside bounding box - use raycasting to find obstacles
            bool centralRaycast = false;
            if (voxels)
            {
                Vector3D[] corners = new Vector3D[8];
                closeBoundingBox.GetCorners(corners, 0);

                for (int i = -1; i < 4; i++)
                {
                    Vector3D targetPosition = i >= 0 ? corners[i + 4] : farPoint;//raycast far corners of bounding box and center of far face of bounding box
                    MyPhysics.HitInfo? hitInfo = MyPhysics.CastRay(shipFront + direction * 0.1, targetPosition);
                    if (hitInfo != null)
                    {
                        MyEntity target = hitInfo.Value.HkHitInfo.Body.UserObject as MyEntity;
                        if (target == null && hitInfo.Value.HkHitInfo.Body.UserObject is MyVoxelPhysicsBody)
                            target = ((MyVoxelPhysicsBody)hitInfo.Value.HkHitInfo.Body.UserObject).Entity as MyEntity;
                        FillListOfDetectedObjects(hitInfo.Value.Position, target, ref listLimit, ref shipFront, ref closestEntityDist, ref closestEntity);

                        if (i == -1 && targetPosition != null)
                            centralRaycast = true;
                    }

                    if (debugDraw)
                        MyRenderProxy.DebugDrawLine3D(shipFront, targetPosition, Color.Pink, Color.White, false, false);
                }
            }

            //at least one obstacle detected
            if (closestEntityDist < float.MaxValue)
            {
                m_rotateFor = 3f;
                int detectedCount = 0;
                Vector3D avoidPositionAverage = Vector3D.Zero;
                bool voxelTunnel = false;
                if (!centralRaycast)
                {
                    for (int i = 0; i < m_detectedObstacles.Count; i++)
                    {
                        if (i == 4)
                        {
                            voxelTunnel = true;
                            break;
                        }
                        if (!m_detectedObstacles[i].IsVoxel)
                            break;
                    }
                }

                if (voxelTunnel)//special case when inside voxel tunnel
                {
                    avoidPositionAverage = farPoint;
                }
                else
                {
                    //go through 5 closest obstacles that are also close to closest obstacle and create correction course
                    for (int i = 0; i < m_detectedObstacles.Count; i++)
                    {
                        if (i == 0 || m_detectedObstacles[i].Distance - m_detectedObstacles[0].Distance < 15 * 15)
                        {
                            detectedCount++;
                            Vector3D toObstacle = m_detectedObstacles[i].Position - CubeGrid.WorldMatrix.Translation;
                            Vector3D crossUp = Vector3D.Cross(delta, toObstacle);
                            Vector3D crossObstacle = Vector3D.Cross(crossUp, delta);
                            //Vector3D avoidPosition = closestEntityPos - Vector3D.Normalize(crossObstacle) * CubeGrid.PositionComp.LocalVolume.Radius * 2;
                            Vector3D avoidPosition = m_detectedObstacles[i].Position - Vector3D.Normalize(crossObstacle) * CubeGrid.PositionComp.LocalVolume.Radius * radiusRatio * 2;
                            if (debugDraw)
                                MyRenderProxy.DebugDrawLine3D(CubeGrid.WorldMatrix.Translation, avoidPosition, Color.White, Color.Tomato, false, false);
                            avoidPositionAverage += avoidPosition;
                        }
                    }
                    avoidPositionAverage /= detectedCount;
                }
                /*
                //Vector3D toObstacle = closestEntityPos - CubeGrid.WorldMatrix.Translation;
                Vector3D toObstacle = average - CubeGrid.WorldMatrix.Translation;
                Vector3D crossUp = Vector3D.Cross(delta, toObstacle);
                Vector3D crossObstacle = Vector3D.Cross(crossUp, delta);
                //Vector3D avoidPosition = closestEntityPos - Vector3D.Normalize(crossObstacle) * CubeGrid.PositionComp.LocalVolume.Radius * 2;
                Vector3D avoidPosition = closestEntityPos - Vector3D.Normalize(crossObstacle) * CubeGrid.PositionComp.LocalVolume.Radius * radiusRatio * 2;
                MyRenderProxy.DebugDrawLine3D(closestEntityPos, avoidPosition, Color.Red, Color.GreenYellow, false, false);*/
                //delta = (avoidPosition - CubeGrid.WorldMatrix.Translation) * 15;
                //delta = (avoidPosition - closestEntityPos) * 50;

                autopilotSpeedLimit = 1 + autopilotSpeedLimit * ((float)Math.Sqrt(closestEntityDist)) / ((float)stoppingDistance) * 0.5f;
                if (detectedCount < 5 || voxelTunnel)//voxel tunnel or less than 5 close obstacles
                    delta = (avoidPositionAverage - CubeGrid.WorldMatrix.Translation);
                else if (closestEntity != null)//5+ close obstacles (wall for example)
                {
                    Vector3D toObstacle = closestEntity.WorldMatrix.Translation - CubeGrid.WorldMatrix.Translation;
                    Vector3D crossUp = Vector3D.Cross(delta, toObstacle);
                    Vector3D crossObstacle = Vector3D.Cross(crossUp, delta);
                    delta = (closestEntity.WorldMatrix.Translation - Vector3D.Normalize(crossObstacle) * CubeGrid.PositionComp.LocalVolume.Radius * radiusRatio * 2) - closestEntity.WorldMatrix.Translation;
                    delta *= 2f;
                    autopilotSpeedLimit *= 0.75f;
                }

                //check if resulting course is not to close to first obstacle
                Vector3D deltaNormalized = Vector3D.Normalize(delta);
                float dot = (float)Vector3D.Dot(deltaNormalized, Vector3D.Normalize(m_detectedObstacles[0].Position - CubeGrid.WorldMatrix.Translation));
                if (dot > 0.5f)
                    delta *= -1;
                else
                {//check if resulting course is not heading in completely opposite direction from target
                    dot = (float)Vector3D.Dot(deltaNormalized, Vector3D.Normalize(originalDelta));
                    if (dot < -0.5f)
                        delta = originalDelta;
                }

                if(debugDraw)
                    MyRenderProxy.DebugDrawLine3D(CubeGrid.WorldMatrix.Translation, CubeGrid.WorldMatrix.Translation + delta, Color.Red, Color.Aquamarine, false, false);
                //MatrixD toObstacle = MatrixD.CreateFromDir(shipFront - closestEntityPos);
                //delta = CubeGrid.PositionComp.GetPosition() - (closestEntityPos + toObstacle.Right * CubeGrid.PositionComp.LocalVolume.Radius / 2);
            }
            else
            {
                if (debugDraw && m_rotateFor <= 0)
                {
                    MyRenderProxy.DebugDrawLine3D(farPoint, farPoint + speedDelta, Color.Yellow, Color.Green, false, false);
                    //MyRenderProxy.DebugDrawOBB(farBoundingBox, Color.Green, 0.25f, false, false, false);
                }
            }
            m_detectedObstacles.Clear();

            //keep last course for some time after no obstacles are found
            if (closestEntityDist == float.MaxValue && m_rotateFor > 1.5f)
            {
                autopilotSpeedLimit = m_lastAutopilotSpeedLimit;
                return m_lastDelta;
            }
            m_lastAutopilotSpeedLimit = autopilotSpeedLimit;
            m_lastDelta = delta;
            return delta;
        }

        private Vector3D AvoidCollisions(Vector3D delta, ref float autopilotSpeedLimit)
        {
            if (m_collisionCtr <= 0)
            {
                m_collisionCtr = 0;
            }
            else
            {
                m_collisionCtr--;
                return m_oldCollisionDelta;
            }

            bool drawDebug = MyDebugDrawSettings.ENABLE_DEBUG_DRAW;// && MyDebugDrawSettings.DEBUG_DRAW_DRONES;

            Vector3D originalDelta = delta;

            Vector3D origin = this.CubeGrid.Physics.CenterOfMassWorld;
            double shipRadius = this.CubeGrid.PositionComp.WorldVolume.Radius * 1.3f;
            if (MyFakes.ENABLE_VR_DRONE_COLLISIONS) //TODO VR: this MyFake should be enabled in VR but disabled in SE
                shipRadius = this.CubeGrid.PositionComp.WorldVolume.Radius * 1f;

            Vector3D linVel = this.CubeGrid.Physics.LinearVelocity;

            double vel = linVel.Length();
            double detectionRadius = this.CubeGrid.PositionComp.WorldVolume.Radius * 10.0f + (vel * vel) * 0.05;
            if (MyFakes.ENABLE_VR_DRONE_COLLISIONS)
                detectionRadius = this.CubeGrid.PositionComp.WorldVolume.Radius + (vel * vel) * 0.05;
            BoundingSphereD sphere = new BoundingSphereD(origin, detectionRadius);

            Vector3D testPoint = sphere.Center + linVel * 2.0f;
            if (MyFakes.ENABLE_VR_DRONE_COLLISIONS)
                testPoint = sphere.Center + linVel;

            if (drawDebug)
            {
                MyRenderProxy.DebugDrawSphere(sphere.Center, (float)shipRadius, Color.HotPink, 1.0f, false);
                MyRenderProxy.DebugDrawSphere(sphere.Center + linVel, 1.0f, Color.HotPink, 1.0f, false);
                MyRenderProxy.DebugDrawSphere(sphere.Center, (float)detectionRadius, Color.White, 1.0f, false);
            }

            Vector3D steeringVector = Vector3D.Zero;
            Vector3D avoidanceVector = Vector3D.Zero;
            int n = 0;

            double maxAvCoeff = 0.0f;

            var entities = MyEntities.GetTopMostEntitiesInSphere(ref sphere);
            IMyGravityProvider well;
            if (MyGravityProviderSystem.GetStrongestNaturalGravityWell(origin, out well) > 0 && well is MyGravityProviderComponent)
            {
                MyEntity e = (MyEntity)((MyGravityProviderComponent)well).Entity;
                if (!entities.Contains(e)) entities.Add(e);
            }

            for (int i = 0; i < entities.Count; ++i)
            {
                var entity = entities[i];

                if (entity == this.Parent) continue;

                Vector3D steeringDelta = Vector3D.Zero;
                Vector3D avoidanceDelta = Vector3D.Zero;

                if ((entity is MyCubeGrid) || (entity is MyVoxelMap) || (entity is MySkinnedEntity))
                {
                    if (MyFakes.ENABLE_VR_DRONE_COLLISIONS && (entity is MyCubeGrid))
                    {
                        var grid = entity as MyCubeGrid;
                        if (grid.IsStatic)
                        {
                            continue;
                        }
                    }

                    if (entity is MyCubeGrid)
                    {
                        var grid = entity as MyCubeGrid;
                        if(MyCubeGridGroups.Static.Physical.GetGroup(CubeGrid) == MyCubeGridGroups.Static.Physical.GetGroup(grid))
                        {
                            continue;
                        }
                    }

                    var otherSphere = entity.PositionComp.WorldVolume;
                    otherSphere.Radius += shipRadius;
                    Vector3D offset = otherSphere.Center - sphere.Center;

                    if (this.CubeGrid.Physics.LinearVelocity.LengthSquared() > 5.0f && Vector3D.Dot(delta, this.CubeGrid.Physics.LinearVelocity) < 0) continue;

                    // Collision avoidance
                    double dist = offset.Length();
                    BoundingSphereD forbiddenSphere = new BoundingSphereD(otherSphere.Center + linVel, otherSphere.Radius + vel);
                    if (forbiddenSphere.Contains(testPoint) == ContainmentType.Contains)
                    {
                        autopilotSpeedLimit = 2.0f;
                        if (drawDebug)
                        {
                            MyRenderProxy.DebugDrawSphere(forbiddenSphere.Center, (float)forbiddenSphere.Radius, Color.Red, 1.0f, false);
                        }
                    }
                    else
                    {
                        if (Vector3D.Dot(offset, linVel) < 0)
                        {
                            if (drawDebug)
                            {
                                MyRenderProxy.DebugDrawSphere(forbiddenSphere.Center, (float)forbiddenSphere.Radius, Color.Red, 1.0f, false);
                            }
                        }
                        else if (drawDebug)
                        {
                            MyRenderProxy.DebugDrawSphere(forbiddenSphere.Center, (float)forbiddenSphere.Radius, Color.DarkOrange, 1.0f, false);
                        }
                    }

                    // 0.693 is log(2), because we want svLength(otherSphere.radius) -> 1 and svLength(0) -> 2
                    double exponent = -0.693 * dist / (otherSphere.Radius + this.CubeGrid.PositionComp.WorldVolume.Radius + vel);
                    double svLength = 2 * Math.Exp(exponent);
                    double avCoeff = Math.Min(1.0f, Math.Max(0.0f, -(forbiddenSphere.Center - sphere.Center).Length() / forbiddenSphere.Radius + 2));
                    maxAvCoeff = Math.Max(maxAvCoeff, avCoeff);

                    Vector3D normOffset = offset / dist;
                    steeringDelta = -normOffset * svLength;
                    avoidanceDelta = -normOffset * avCoeff;
                }
                else if (entity is MyPlanet)
                {
                    var planet = entity as MyPlanet;
                    float gravityLimit = ((MySphericalNaturalGravityComponent)planet.Components.Get<MyGravityProviderComponent>()).GravityLimit;

                    Vector3D planetPos = planet.WorldMatrix.Translation;
                    Vector3D offset = planetPos - origin;

                    double dist = offset.Length();

                    double distFromGravity = dist - gravityLimit;
                    if (distFromGravity > PLANET_AVOIDANCE_RADIUS || distFromGravity < -PLANET_AVOIDANCE_TOLERANCE) continue;

                    Vector3D repulsionPoleDir = planetPos - m_currentWaypoint.Coords;
                    if (Vector3D.IsZero(repulsionPoleDir)) repulsionPoleDir = Vector3.Up;
                    else repulsionPoleDir.Normalize();

                    Vector3D repulsionPole = planetPos + repulsionPoleDir * gravityLimit;

                    Vector3D toCenter = offset;
                    toCenter.Normalize();

                    double repulsionDistSq = (repulsionPole - origin).LengthSquared();
                    if (repulsionDistSq < PLANET_REPULSION_RADIUS * PLANET_REPULSION_RADIUS)
                    {
                        double centerDist = Math.Sqrt(repulsionDistSq);
                        double repCoeff = centerDist / PLANET_REPULSION_RADIUS;
                        Vector3D repulsionTangent = origin - repulsionPole;
                        if (Vector3D.IsZero(repulsionTangent))
                        {
                            repulsionTangent = Vector3D.CalculatePerpendicularVector(repulsionPoleDir);
                        }
                        else
                        {
                            repulsionTangent = Vector3D.Reject(repulsionTangent, repulsionPoleDir);
                            repulsionTangent.Normalize();
                        }
                        // Don't bother with quaternions...
                        steeringDelta = Vector3D.Lerp(repulsionPoleDir, repulsionTangent, repCoeff);
                    }
                    else
                    {
                        Vector3D toTarget = m_currentWaypoint.Coords - origin;
                        toTarget.Normalize();

                        if (Vector3D.Dot(toTarget, toCenter) > 0)
                        {
                            steeringDelta = Vector3D.Reject(toTarget, toCenter);
                            if (Vector3D.IsZero(steeringDelta))
                            {
                                steeringDelta = Vector3D.CalculatePerpendicularVector(toCenter);
                            }
                            else
                            {
                                steeringDelta.Normalize();
                            }
                        }
                    }

                    double testPointDist = (testPoint - planetPos).Length();
                    if (testPointDist < gravityLimit) m_autopilotSpeedLimit.Value = 2.0f;

                    double avCoeff = (gravityLimit + PLANET_AVOIDANCE_RADIUS - testPointDist) / PLANET_AVOIDANCE_RADIUS;

                    steeringDelta *= avCoeff; // avCoeff == svLength
                    avoidanceDelta = -toCenter * avCoeff;
                }
                else
                {
                    continue;
                }

                steeringVector += steeringDelta;
                avoidanceVector += avoidanceDelta;
                n++;
            }
            entities.Clear();

            /*if (minTmin < vel)
            {
                delta = origin + minTmin * vel;
            }*/

            if (n > 0)
            {
                double l = delta.Length();
                delta = delta / l;
                steeringVector *= (1.0f - maxAvCoeff) * 0.1f / n;

                Vector3D debugDraw = steeringVector + delta;

                delta += steeringVector + avoidanceVector;
                delta *= l;

                if (drawDebug)
                {
                    MyRenderProxy.DebugDrawArrow3D(origin, origin + delta / l * 100.0f, Color.Green, Color.Green, false);
                    MyRenderProxy.DebugDrawArrow3D(origin, origin + avoidanceVector * 100.0f, Color.Red, Color.Red, false);
                    MyRenderProxy.DebugDrawSphere(origin, 100.0f, Color.Gray, 0.5f, false);
                }
            }

            m_oldCollisionDelta = delta;
            return delta;
        }

        private void UpdateThrust(Vector3D delta, Vector3D perpDelta, double maxSpeed)
        {
            var thrustSystem = CubeGrid.Components.Get<MyEntityThrustComponent>();
	        if (thrustSystem == null)
		        return;

            thrustSystem.AutoPilotControlThrust = Vector3.Zero;

            // Planet-related stuff
            m_dbgDeltaH = Vector3.Zero;
            if (m_currentInfo.IsValid())
            {
                // Sample several points around the bottom of the ship to get a better estimation of the terrain underneath
                Vector3D shipCenter = CubeGrid.PositionComp.WorldVolume.Center + m_currentInfo.GravityWorld * CubeGrid.PositionComp.WorldVolume.Radius;

                // Limit max speed if too close to the ground.
                Vector3D samplePosition;
                m_thdPtr++;
                // A velocity-based sample
                if (m_thdPtr >= TERRAIN_HEIGHT_DETECTION_SAMPLES)
                {
                    m_thdPtr = 0;
                    samplePosition = shipCenter + new Vector3D(CubeGrid.Physics.LinearVelocity) * 5.0;
                }
                // Flight direction sample
                else if (m_thdPtr == 1)
                {
                    Vector3D direction = WorldMatrix.GetDirectionVector(m_currentDirection);
                    samplePosition = shipCenter + direction * CubeGrid.PositionComp.WorldVolume.Radius * 2.0;
                }
                // Random samples
                else
                {
                    Vector3D tangent = Vector3D.CalculatePerpendicularVector(m_currentInfo.GravityWorld);
                    Vector3D bitangent = Vector3D.Cross(tangent, m_currentInfo.GravityWorld);
                    samplePosition = MyUtils.GetRandomDiscPosition(ref shipCenter, CubeGrid.PositionComp.WorldVolume.Radius, ref tangent, ref bitangent);
                }

                if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_DRONES)
                {
                    MyRenderProxy.DebugDrawCapsule(samplePosition, samplePosition + m_currentInfo.GravityWorld * 50.0f, 1.0f, Color.Yellow, false);
                }

                if (m_currentInfo.IsValid())
                {
                    m_terrainHeightDetection[m_thdPtr] = (float)m_currentInfo.EstimateDistanceToGround(samplePosition);
                }
                else
                {
                    m_terrainHeightDetection[m_thdPtr] = 0.0f;
                }
                double distanceToGround = m_terrainHeightDetection[0];
                for (int i = 1; i < TERRAIN_HEIGHT_DETECTION_SAMPLES; ++i)
                {
                    distanceToGround = Math.Min(distanceToGround, m_terrainHeightDetection[i]);
                }

                if (distanceToGround < 0.0f) Debugger.Break();

                // Below 50m, the speed will be minimal, Above 150m, it will be maximal
                // coeff(50) = 0, coeff(150) = 1
                double coeff = (distanceToGround - 50.0) * 0.01;
                if (coeff < 0.05) coeff = 0.15;
                if (coeff > 1.0f) coeff = 1.0f;
                maxSpeed = maxSpeed * Math.Max(coeff, 0.05);

                double dot = m_currentInfo.PlanetVector.Dot(m_currentInfo.PlanetVector);
                double deltaH = m_currentInfo.Elevation - m_currentInfo.Elevation;
                double deltaPSq = perpDelta.LengthSquared();

                // Add height difference compensation on long distances (to avoid flying off the planet)
                if (dot < 0.99 && deltaPSq > 100.0)
                {
                    m_dbgDeltaH = -deltaH * m_currentInfo.GravityWorld;
                }

                delta += m_dbgDeltaH;

                //For now remove this (causes cubegrid to get stuck at a height)
                // If we are very close to the ground, just thrust upward to avoid crashing.
                // The coefficient is set-up that way that at 50m, this thrust will overcome the thrust to the target.
                //double groundAvoidanceCoeff = Math.Max(0.0, Math.Min(1.0, distanceToGround * 0.1));
                //delta = Vector3D.Lerp(-m_currentInfo.GravityWorld * delta.Length(), delta, groundAvoidanceCoeff);
            }

            m_dbgDelta = delta;

            Matrix invWorldRot = CubeGrid.PositionComp.WorldMatrixNormalizedInv.GetOrientation();

            Vector3D targetDirection = delta;
            targetDirection.Normalize();

            Vector3D velocity = CubeGrid.Physics.LinearVelocity;

            Vector3 localSpaceTargetDirection = Vector3.Transform(targetDirection, invWorldRot);
            Vector3 localSpaceVelocity = Vector3.Transform(velocity, invWorldRot);

            thrustSystem.AutoPilotControlThrust = Vector3.Zero;

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
            double verticalVelocity = -velocity.Dot(ref m_currentInfo.GravityWorld);

            Vector3D velocityToCancel = velocity1 + velocity2;

            double timeToReachTarget = (delta.Length() / velocityToTarget.Length());
            double timeToStop = velocity.Length() * CubeGrid.Physics.Mass / brakeThrust.Length();

            if (m_dockingModeEnabled)
            {
                timeToStop *= 2.5f;
            }

            if ((double.IsInfinity(timeToReachTarget) || double.IsNaN(timeToStop) || timeToReachTarget > timeToStop) && velocity.LengthSquared() < (maxSpeed * maxSpeed))
            {
                Vector3 thrust = Vector3D.Transform(delta, invWorldRot) - Vector3D.Transform(velocityToCancel, invWorldRot);       
                thrust.Normalize();
                // becaouse of c# properties and structs 
                thrustSystem.AutoPilotControlThrust = thrust;
            }
        }

        private void ResetShipControls()
        {
			var thrustComp = CubeGrid.Components.Get<MyEntityThrustComponent>();
			if(thrustComp != null)
				thrustComp.DampenersEnabled = true;
        }

        bool ModAPI.Ingame.IMyRemoteControl.GetNearestPlayer(out Vector3D playerPosition)
        {
            playerPosition = default(Vector3D);
            if (!MySession.Static.Players.IdentityIsNpc(OwnerId))
            {
                return false;
            }

            var player = GetNearestPlayer();
            if (player == null)
                return false;

            playerPosition = player.Controller.ControlledEntity.Entity.WorldMatrix.Translation;
            return true;
        }

        public bool GetNearestPlayer(out MatrixD playerWorldTransform, Vector3 offset)
        {
            playerWorldTransform = MatrixD.Identity;
            if (!MySession.Static.Players.IdentityIsNpc(OwnerId))
                return false;

            var player = GetNearestPlayer();
            if (player == null)
                return false;

            playerWorldTransform = player.Controller.ControlledEntity.Entity.WorldMatrix;
            Vector3 offsetTransformed = Vector3.TransformNormal(offset, playerWorldTransform);
            playerWorldTransform.Translation = playerWorldTransform.Translation + offsetTransformed;
            return true;
        }

        public Vector3D GetNaturalGravity()
        {
            return MyGravityProviderSystem.CalculateNaturalGravityInPoint(WorldMatrix.Translation);
        }

        public MyPlayer GetNearestPlayer()
        {
            Vector3D myPosition = this.WorldMatrix.Translation;
            double closestDistSq = double.MaxValue;
            MyPlayer result = null;

            foreach (var player in MySession.Static.Players.GetOnlinePlayers())
            {
                var controlled = player.Controller.ControlledEntity;
                if (controlled == null) continue;

                Vector3D position = controlled.Entity.WorldMatrix.Translation;
                double distSq = Vector3D.DistanceSquared(myPosition, position);
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    result = player;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a destination and tries to fix it so that it does not collide with anything
        /// </summary>
        /// <param name="originalDestination">The final destination that the remote wants to get to.</param>
        /// <param name="checkRadius">The maximum radius until which this method should search.</param>
        /// <param name="shipRadius">The radius of our ship. Make sure that this is large enough to avoid collision.</param>
        Vector3D ModAPI.IMyRemoteControl.GetFreeDestination(Vector3D originalDestination, float checkRadius, float shipRadius)
        {
            MyCestmirDebugInputComponent.ClearDebugSpheres();
            MyCestmirDebugInputComponent.ClearDebugPoints();

            MyCestmirDebugInputComponent.AddDebugPoint(this.WorldMatrix.Translation, Color.Green);

            Vector3D retval = originalDestination;
            BoundingSphereD sphere = new BoundingSphereD(this.WorldMatrix.Translation, shipRadius + checkRadius);

            Vector3D rayDirection = originalDestination - this.WorldMatrix.Translation;
            double originalDistance = rayDirection.Length();
            rayDirection = rayDirection / originalDistance;
            RayD ray = new RayD(this.WorldMatrix.Translation, rayDirection);

            double closestIntersection = double.MaxValue;
            BoundingSphereD closestSphere = default(BoundingSphereD);

            var entities = MyEntities.GetTopMostEntitiesInSphere(ref sphere);
            for (int i = 0; i < entities.Count; ++i)
            {
                var entity = entities[i];

                if (!(entity is MyCubeGrid) && !(entity is MyVoxelMap)) continue;
                if (entity.Parent != null) continue;
                if (entity == this.Parent) continue;

                BoundingSphereD entitySphere = entity.PositionComp.WorldVolume;
                entitySphere.Radius += shipRadius;

                MyCestmirDebugInputComponent.AddDebugSphere(entitySphere.Center, (float)entity.PositionComp.WorldVolume.Radius, Color.Plum);
                MyCestmirDebugInputComponent.AddDebugSphere(entitySphere.Center, (float)entity.PositionComp.WorldVolume.Radius + shipRadius, Color.Purple);

                double? intersection = ray.Intersects(entitySphere);
                if (intersection.HasValue && intersection.Value < closestIntersection)
                {
                    closestIntersection = intersection.Value;
                    closestSphere = entitySphere;
                }
            }

            if (closestIntersection != double.MaxValue)
            {
                Vector3D correctedDestination = ray.Position + closestIntersection * ray.Direction;
                MyCestmirDebugInputComponent.AddDebugSphere(correctedDestination, 1.0f, Color.Blue);
                Vector3D normal = correctedDestination - closestSphere.Center;
                if (Vector3D.IsZero(normal))
                {
                    normal = Vector3D.Up;
                }
                normal.Normalize();
                MyCestmirDebugInputComponent.AddDebugSphere(correctedDestination + normal, 1.0f, Color.Red);
                Vector3D newDirection = Vector3D.Reject(ray.Direction, normal);
                newDirection.Normalize();
                newDirection *= Math.Max(20.0, closestSphere.Radius * 0.5);
                MyCestmirDebugInputComponent.AddDebugSphere(correctedDestination + newDirection, 1.0f, Color.LightBlue);
                retval = correctedDestination + newDirection;
            }
            else
            {
                retval = ray.Position + ray.Direction * Math.Min(checkRadius, originalDistance);
            }

            entities.Clear();

            return retval;
        }
        #endregion

        private bool TryFindSavedEntity()
        {
            MyEntity oldControllerEntity;
            if (m_savedPreviousControlledEntityId.HasValue && MyEntities.TryGetEntityById(m_savedPreviousControlledEntityId.Value, out oldControllerEntity))
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
            if (m_savedPreviousControlledEntityId.HasValue)
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
            objectBuilder.FlightMode = (int)m_currentFlightMode.Value;
            objectBuilder.Direction = (byte)m_currentDirection.Value;
            objectBuilder.AutopilotSpeedLimit = m_autopilotSpeedLimit;
            objectBuilder.WaypointThresholdDistance = m_waypointThresholdDistance;
            objectBuilder.BindedCamera = m_bindedCamera.Value;
            objectBuilder.IsMainRemoteControl = m_isMainRemoteControl;

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

            objectBuilder.CollisionAvoidance = m_useCollisionAvoidance;
            objectBuilder.AutomaticBehaviour = m_automaticBehaviour != null ? m_automaticBehaviour.GetObjectBuilder() : null;

            return objectBuilder;
        }

        public bool CanControl()
        {
            if (!CheckPreviousEntity(MySession.Static.ControlledEntity)) return false;
            if (m_autoPilotEnabled) return false;
            return IsWorking && PreviousControlledEntity == null && CheckRangeAndAccess(MySession.Static.ControlledEntity, MySession.Static.LocalHumanPlayer);
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(m_powerNeeded, DetailedInfo);
            DetailedInfo.Append("\n");
            var pilot = m_previousControlledEntity as MyCharacter;
            if( pilot != null && pilot != MySession.Static.LocalCharacter)
            {
                DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.RemoteControlUsedBy));
                DetailedInfo.Append(pilot.DisplayNameText);
                DetailedInfo.Append("\n");
            }

            if (m_autoPilotEnabled && CurrentWaypoint != null)
            {
                DetailedInfo.Append("\n");
                DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.RemoteControlWaypoint));
                DetailedInfo.Append(CurrentWaypoint.Name);

                DetailedInfo.Append("\n");
                DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.RemoteControlCoord));
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

            ResourceSink.Update();
            UpdateEmissivity();
            UpdateText();
        }

        private void Receiver_RequiredInputChanged(MyDefinitionId resourceTypeId, MyResourceSinkComponent receiver, float oldRequirement, float newRequirement)
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
            }
        }

        private float CalculateRequiredPowerInput()
        {
            return m_powerNeeded;
        }

        public override void ShowTerminal()
        {
            MyGuiScreenTerminal.Show(MyTerminalPageEnum.ControlPanel, MySession.Static.LocalHumanPlayer.Character, this);
        }

        public void RequestControl()
        {
            if (!MyFakes.ENABLE_REMOTE_CONTROL)
            {
                return;
            }

            //Do not take control if you are already the controller
            if (MySession.Static.ControlledEntity == this)
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
            if (MySession.Static.ControlledEntity != null)
            {
                RequestUse(UseActionEnum.Manipulate, MySession.Static.ControlledEntity);
            }
        }

        private void AcquireControl()
        {
            AcquireControl(MySession.Static.ControlledEntity);
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

            //MySession.Static.SetCameraController(MyCameraControllerEnum.Entity, this);

            if (MyCubeBuilder.Static.IsActivated)
            {
                //MyCubeBuilder.Static.Deactivate();
                MySession.Static.GameFocusManager.Clear();
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

            //if (MySession.Static.CameraController == this && Pilot == MySession.Static.LocalCharacter)
            //{
            //    MySession.Static.SetCameraController(MyCameraControllerEnum.Entity, Pilot);
            //}  

            if (m_autoPilotEnabled)
            {
                //Do not go through sync layer when destroying
                RemoveAutoPilot();
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
            RefreshTerminal();
            UpdateEmissivity();
        }

        private void RefreshTerminal()
        {
            if (Pilot != MySession.Static.LocalCharacter)
            {
                RaisePropertiesChanged();
                UpdateText();
            }
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

                RefreshTerminal();
            }

            //switch to binded camera
            if (m_bindedCamera != 0)
            {
                MyEntity entity;
                if (MyEntities.TryGetEntityById(m_bindedCamera, out entity))
                {
                    MyCameraBlock camera = entity as MyCameraBlock;
                    if (camera != null)
                    {
                        camera.RequestSetView();
        }
                    else
                    {
                        m_bindedCamera.Value = 0;
                    }
                }
                else
                {
                    m_bindedCamera.Value = 0;
                }
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
                System.Diagnostics.Debug.Assert(m_savedPreviousControlledEntityId.HasValue,"Controller is null, but remote control was not properly released!");
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
                    return MyAntennaSystem.Static.CheckConnection(character, CubeGrid, player);
                }
                else
                {
                    return true;
                }
            }

            MyCubeGrid playerGrid = terminal.SlimBlock.CubeGrid;

            return MyAntennaSystem.Static.CheckConnection(playerGrid, CubeGrid, player);
        }

        protected override void OnOwnershipChanged()
        {
            base.OnOwnershipChanged();

            if (PreviousControlledEntity != null && Sync.IsServer)
            {
                if (ControllerInfo.Controller != null)
                {
                    var relation = GetUserRelationToOwner(ControllerInfo.ControllingIdentityId);
                    if (relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies || relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Neutral)
                    {
                        RaiseControlledEntityUsed();
                    }
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
            if (m_previousControlledEntity != null && user != m_previousControlledEntity)
            {
                return UseActionResult.UsedBySomeoneElse;
            }
            return UseActionResult.OK;
        }

        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
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

        void SendToolbarItemChanged(ToolbarItem item, int index, int waypointIndex)
        {
            if (m_syncing)
                return;

            MyMultiplayer.RaiseEvent(this, x => x.OnToolbarItemChanged, item, index, waypointIndex);
        }

        [Event, Reliable, Server, BroadcastExcept]
        private void OnToolbarItemChanged(ToolbarItem item, int index, int waypointIndex)
        {
            m_syncing = true;
            MyToolbarItem toolbarItem = null;
            if (item.EntityID != 0)
            {
                if (string.IsNullOrEmpty(item.GroupName))
                {
                    MyTerminalBlock block;
                    if (MyEntities.TryGetEntityById<MyTerminalBlock>(item.EntityID, out block))
                    {
                        var builder = MyToolbarItemFactory.TerminalBlockObjectBuilderFromBlock(block);
                        builder._Action = item.Action;
                        builder.Parameters = item.Parameters;
                        toolbarItem = MyToolbarItemFactory.CreateToolbarItem(builder);
                    }
                }
                else
                {
                    MyRemoteControl parent;
                    if (MyEntities.TryGetEntityById<MyRemoteControl>(item.EntityID, out parent))
                    {
                        var grid = parent.CubeGrid;
                        var groupName = item.GroupName;
                        var group = grid.GridSystems.TerminalSystem.BlockGroups.Find((x) => x.Name.ToString() == groupName);
                        if (group != null)
                        {
                            var builder = MyToolbarItemFactory.TerminalGroupObjectBuilderFromGroup(group);
                            builder._Action = item.Action;
                            builder.BlockEntityId = item.EntityID;
                            builder.Parameters = item.Parameters;
                            toolbarItem = MyToolbarItemFactory.CreateToolbarItem(builder);
                        }
                    }
                }
            }

            var waypoint = m_waypoints[waypointIndex];
            if (waypoint.Actions == null)
            {
                waypoint.InitActions();
            }
            waypoint.Actions[index] = toolbarItem;
            RaisePropertiesChangedRemote();
            m_syncing = false;
        }

        public void SetAutomaticBehaviour(IRemoteControlAutomaticBehaviour automaticBehaviour)
        {
            m_automaticBehaviour = automaticBehaviour;
        }

        public void RemoveAutomaticBehaviour()
        {
            m_automaticBehaviour = null;
        }

        private readonly Sync<bool> m_isMainRemoteControl;
        public bool IsMainRemoteControl
        {
            get
            {
                return m_isMainRemoteControl;
            }
            set
            {
                m_isMainRemoteControl.Value = value;
            }
        }

        private void SetMainRemoteControl(bool value)
        {
            if (value)
            {
                if (CubeGrid.HasMainRemoteControl() && !CubeGrid.IsMainRemoteControl(this))
                {
                    IsMainRemoteControl = false;
                    return;
                }
            }
            IsMainRemoteControl = value;
        }

        private void MainRemoteControlChanged()
        {
            if (m_isMainRemoteControl)
            {
                CubeGrid.SetMainRemoteControl(this);
            }
            else
            {
                if (CubeGrid.IsMainRemoteControl(this))
                {
                    CubeGrid.SetMainRemoteControl(null);
                }
            }
        }

        protected bool IsMainRemoteControlFree()
        {
            return CubeGrid.HasMainRemoteControl() == false || CubeGrid.IsMainRemoteControl(this);
        }

        class MyDebugRenderComponentRemoteControl : MyDebugRenderComponent
        {
            MyRemoteControl m_remote;
            public MyDebugRenderComponentRemoteControl(MyRemoteControl remote)
                : base(remote)
            {
                m_remote = remote;
            }

            MyAutopilotWaypoint m_prevWaypoint;
            public override void DebugDraw()
            {
                if (m_remote.CurrentWaypoint == null && m_prevWaypoint == null) return;
                if (m_remote.CurrentWaypoint != null)
                    m_prevWaypoint = m_remote.CurrentWaypoint;

                var waypoint = m_prevWaypoint;

                Vector3D pos1 = m_remote.WorldMatrix.Translation;

                MyRenderProxy.DebugDrawArrow3D(pos1, pos1 + m_remote.m_dbgDelta, Color.Yellow, Color.Yellow, false);
                MyRenderProxy.DebugDrawArrow3D(pos1, pos1 + m_remote.m_dbgDeltaH, Color.LightBlue, Color.LightBlue, false);
                MyRenderProxy.DebugDrawLine3D(pos1, waypoint.Coords, Color.Red, Color.Red, false);
                MyRenderProxy.DebugDrawText3D(waypoint.Coords, m_remote.m_destinationInfo.Elevation.ToString("N"), Color.White, 1.0f, false);
                MyRenderProxy.DebugDrawText3D(pos1, m_remote.m_currentInfo.Elevation.ToString("N"), Color.White, 1.0f, false);

                if (m_remote.m_automaticBehaviour != null)
                    m_remote.m_automaticBehaviour.DebugDraw();
            }
        }

        //void IMyCameraController.ControlCamera(MyCamera currentCamera)
        //{
        //    IMyCameraController pilotCameraController = Pilot;
        //    if (pilotCameraController != null)
        //        pilotCameraController.ControlCamera(currentCamera);
        //}

        //void IMyCameraController.Rotate(Vector2 rotationIndicator, float rollIndicator)
        //{
        //    IMyCameraController pilotCameraController = Pilot;
        //    if (pilotCameraController != null)
        //        pilotCameraController.Rotate(rotationIndicator, rollIndicator);
        //}

        //void IMyCameraController.RotateStopped()
        //{
        //    MyEntity pilotParent = Pilot;
        //    while (pilotParent != null && pilotParent.Parent is IMyCameraController)
        //        pilotParent = pilotParent.Parent;

        //    IMyCameraController pilotCameraController = (IMyCameraController)pilotParent;
        //    if (pilotCameraController != null)
        //        pilotCameraController.RotateStopped();
        //}

        //void IMyCameraController.OnAssumeControl(IMyCameraController previousCameraController)
        //{
        //}

        //void IMyCameraController.OnReleaseControl(IMyCameraController newCameraController)
        //{
        //}

        //bool IMyCameraController.HandleUse()
        //{
        //    IMyCameraController pilotCameraController = Pilot;
        //    if (pilotCameraController != null)
        //        return pilotCameraController.HandleUse();
        //    return false;
        //}

        //bool IMyCameraController.HandlePickUp()
        //{
        //    IMyCameraController pilotCameraController = Pilot;
        //    if (pilotCameraController != null)
        //        return pilotCameraController.HandlePickUp();
        //    return false;
        //}

        //bool IMyCameraController.IsInFirstPersonView
        //{
        //    get
        //    {
        //        IMyCameraController pilotCameraController = Pilot;
        //        if (pilotCameraController != null)
        //            return pilotCameraController.IsInFirstPersonView;
        //        return true;
        //    }
        //    set
        //    {
        //        IMyCameraController pilotCameraController = Pilot;
        //        if (pilotCameraController != null)
        //            pilotCameraController.IsInFirstPersonView = value;
        //    }
        //}

        //bool IMyCameraController.AllowCubeBuilding
        //{
        //    get { return false; }
        //}
        }
    }
