#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Game.Entity.UseObject;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using IMyModdingControllableEntity = Sandbox.ModAPI.Interfaces.IMyControllableEntity;
using VRage.ObjectBuilders;
using VRage.ModAPI;
#endregion

namespace Sandbox.Game.Entities
{
    public enum ControllerPriority
    {
        AutoPilot = 1,
        Primary = 2,
        Secondary = 3
    };
    public partial class MyShipController : MyTerminalBlock, IMyControllableEntity, IMyRechargeSocketOwner, IMyShipController
    {
        #region Fields
        public MyGridGyroSystem GridGyroSystem;
        public MyGridThrustSystem GridThrustSystem;
        public MyGridSelectionSystem GridSelectionSystem;
        public MyPowerDistributor GridPowerDistributor
        {
            get { return (CubeGrid != null) ? CubeGrid.GridSystems.PowerDistributor : null; }
        }
        public MyGridReflectorLightSystem GridReflectorLights;
        public MyGridWheelSystem GridWheels
        {
            get { return (CubeGrid != null) ? CubeGrid.GridSystems.WheelSystem : null; }
        }

        private bool m_controlThrusters;
        private bool m_controlWheels;

        protected MyRechargeSocket m_rechargeSocket;

        MyHudNotification m_notificationReactorsOff;
        MyHudNotification m_notificationReactorsOn;
        MyHudNotification m_notificationLeave;
        MyHudNotification m_notificationTerminal;
        MyHudNotification m_inertiaDampenersNotification;

        MyHudNotification m_noWeaponNotification;
        MyHudNotification m_weaponSelectedNotification;
        MyHudNotification m_outOfAmmoNotification;
        MyHudNotification m_weaponNotWorkingNotification;

        MyHudNotification m_noControlNotification;

        protected virtual MyStringId LeaveNotificationHintText { get { return MySpaceTexts.NotificationHintLeaveCockpit; } }

        protected bool m_enableFirstPerson = false;
        protected bool m_enableShipControl = true;
        public bool EnableShipControl { get { return m_enableShipControl; } }

        // This value can be in some advanced settings
        static float RollControlMultiplier = 0.2f;

        bool m_forcedFPS;

        //        MyGunTypeEnum? m_selectedGunType;
        MyDefinitionId? m_selectedGunId;

        public MyToolbar Toolbar;

        protected MyEntity3DSoundEmitter m_soundEmitter;

        #endregion

        public VRage.Groups.MyGroups<MyCubeGrid, MyGridPhysicalGroupData> ControlGroup
        {
            get { return MyCubeGridGroups.Static.Physical; }
        }

        #region Init

        static MyShipController()
        {
            if (MyFakes.ENABLE_WHEEL_CONTROLS_IN_COCKPIT)
            {
                var controlThrusters = new MyTerminalControlCheckbox<MyShipController>("ControlThrusters", MySpaceTexts.TerminalControlPanel_Cockpit_ControlThrusters, MySpaceTexts.TerminalControlPanel_Cockpit_ControlThrusters);
                controlThrusters.Getter = (x) => x.ControlThrusters;
                controlThrusters.Setter = (x, v) => x.SyncObject.SetControlThrusters(v);
                controlThrusters.Visible = (x) => x.m_enableShipControl;
                controlThrusters.Enabled = (x) => x.IsMainCockpitFree();
                var action = controlThrusters.EnableAction();
                if (action != null)
                    action.Enabled = (x) => x.m_enableShipControl;
                MyTerminalControlFactory.AddControl(controlThrusters);

                var controlWheels = new MyTerminalControlCheckbox<MyShipController>("ControlWheels", MySpaceTexts.TerminalControlPanel_Cockpit_ControlWheels, MySpaceTexts.TerminalControlPanel_Cockpit_ControlWheels);
                controlWheels.Getter = (x) => x.ControlWheels;
                controlWheels.Setter = (x, v) => x.SyncObject.SetControlWheels(v);
                controlWheels.Visible = (x) => x.m_enableShipControl;
                controlWheels.Enabled = (x) => x.GridWheels.WheelCount > 0 && x.IsMainCockpitFree();
                action = controlWheels.EnableAction();
                if (action != null)
                    action.Enabled = (x) => x.m_enableShipControl;
                MyTerminalControlFactory.AddControl(controlWheels);

                var handBrake = new MyTerminalControlCheckbox<MyShipController>("HandBrake", MySpaceTexts.TerminalControlPanel_Cockpit_Handbrake, MySpaceTexts.TerminalControlPanel_Cockpit_Handbrake);
                handBrake.Getter = (x) => x.CubeGrid.GridSystems.WheelSystem.HandBrake;
                handBrake.Setter = (x, v) => x.CubeGrid.SyncObject.SetHandbrakeRequest(v);
                handBrake.Visible = (x) => x.m_enableShipControl;
                handBrake.Enabled = (x) => x.GridWheels.WheelCount > 0 && x.IsMainCockpitFree();
                action = handBrake.EnableAction();
                if (action != null)
                    action.Enabled = (x) => x.m_enableShipControl;
                MyTerminalControlFactory.AddControl(handBrake);
            }

            if (MyFakes.ENABLE_DAMPENERS_OVERRIDE)
            {
                var dampenersOverride = new MyTerminalControlCheckbox<MyShipController>("DampenersOverride", MySpaceTexts.ControlName_InertialDampeners, MySpaceTexts.ControlName_InertialDampeners);
                dampenersOverride.Getter = (x) =>
                {
                    if (x.GridThrustSystem == null)
                    {
                        Debug.Fail("Alex Florea: Grid thrust system should not be null!");
                        return false;
                    }
                    else
                    {
                        return x.GridThrustSystem.DampenersEnabled;
                    }
                };
                dampenersOverride.Setter = (x, v) => x.EnableDampingInternal(v, true);
                dampenersOverride.Visible = (x) => x.m_enableShipControl;

                var action = dampenersOverride.EnableAction();
                if (action != null)
                {
                    action.Enabled = (x) => x.m_enableShipControl;// x.EnableShipControl;
                }
                dampenersOverride.Enabled = (x) => x.IsMainCockpitFree();
                MyTerminalControlFactory.AddControl(dampenersOverride);
            }

            var mainCockpit = new MyTerminalControlCheckbox<MyShipController>("MainCockpit", MySpaceTexts.TerminalControlPanel_Cockpit_MainCockpit, MySpaceTexts.TerminalControlPanel_Cockpit_MainCockpit);
            mainCockpit.Getter = (x) => x.IsMainCockpit;
            mainCockpit.Setter = (x, v) => x.SetMainCockpit(v);
            mainCockpit.Enabled = (x) => x.IsMainCockpitFree();
            mainCockpit.Visible = (x) => x.CanBeMainCockpit();
            mainCockpit.EnableAction();

            MyTerminalControlFactory.AddControl(mainCockpit);
        }

        public virtual MyCharacter Pilot
        {
            get { return null; }
        }

        protected virtual ControllerPriority Priority
        {
            get
            {
                return ControllerPriority.Primary;
            }
        }

        public MyShipController()
        {
            ControllerInfo.ControlAcquired += OnControlAcquired;
            ControllerInfo.ControlReleased += OnControlReleased;
            GridSelectionSystem = new MyGridSelectionSystem(this);
            m_soundEmitter = new MyEntity3DSoundEmitter(this);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;
            base.Init(objectBuilder, cubeGrid);

            //MyDebug.AssertDebug(objectBuilder.TypeId == typeof(MyObjectBuilder_ShipController));
            var def = MyDefinitionManager.Static.GetCubeBlockDefinition(objectBuilder.GetId());
            m_enableFirstPerson = BlockDefinition.EnableFirstPerson || MySession.Static.Settings.Enable3rdPersonView == false;
            m_enableShipControl = BlockDefinition.EnableShipControl;


            m_rechargeSocket = new MyRechargeSocket();

            MyObjectBuilder_ShipController shipControllerOb = (MyObjectBuilder_ShipController)objectBuilder;

            // No need for backward compatibility of selected weapon, we just leave it alone
            //            m_selectedGunType = shipControllerOb.SelectedGunType;
            m_selectedGunId = shipControllerOb.SelectedGunId;

            m_controlThrusters = shipControllerOb.ControlThrusters;
            m_controlWheels = shipControllerOb.ControlWheels;

            if (shipControllerOb.IsMainCockpit)
            {
                IsMainCockpit = true;
            }

            Toolbar = new MyToolbar(ToolbarType);

            Toolbar.Init(shipControllerOb.Toolbar, this);

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            // TODO: Seems like overkill
            if (Sync.IsServer && false)
            {
                //Because of simulating thrusts
                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            }

            CubeGrid.OnGridSplit += CubeGrid_OnGridSplit;
        }

        protected virtual void ComponentStack_IsFunctionalChanged()
        {
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_ShipController objectBuilder = (MyObjectBuilder_ShipController)base.GetObjectBuilderCubeBlock(copy);

            objectBuilder.SelectedGunId = m_selectedGunId;
            objectBuilder.UseSingleWeaponMode = m_singleWeaponMode;
            objectBuilder.ControlThrusters = m_controlThrusters;
            objectBuilder.ControlWheels = m_controlWheels;
            objectBuilder.Toolbar = Toolbar.GetObjectBuilder();
            objectBuilder.IsMainCockpit = m_isMainCockpit;

            return objectBuilder;
        }

        #endregion

        #region View

        public virtual MatrixD GetHeadMatrix(bool includeY, bool includeX = true, bool forceBoneMatrix = false, bool forceHeadBone = false)
        {
            var world = PositionComp.WorldMatrix;
            return world;
        }

        public override MatrixD GetViewMatrix()
        {
            var head = GetHeadMatrix(!ForceFirstPersonCamera, !ForceFirstPersonCamera);

            MatrixD result;
            MatrixD.Invert(ref head, out result);
            return result;
        }

        public bool PrimaryLookaround
        {
            get { return !m_enableShipControl; }
        }

        public void MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator)
        {
            if (MyFakes.ENABLE_WHEEL_CONTROLS_IN_COCKPIT)
            {
                if ((ControllerInfo.IsLocallyControlled() || Sync.IsServer) && GridWheels != null && ControlWheels && m_enableShipControl)
                {
                    if (MyInput.Static.IsGameControlPressed(MyControlsSpace.JUMP))
                        CubeGrid.GridSystems.WheelSystem.Brake = true;
                    else
                        CubeGrid.GridSystems.WheelSystem.Brake = false;
                    if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.LANDING_GEAR))
                        CubeGrid.GridSystems.WheelSystem.HandBrake = !CubeGrid.GridSystems.WheelSystem.HandBrake;
                }
            }
            // No movement, no change, early return
            if (m_enableShipControl && moveIndicator == Vector3.Zero && rotationIndicator == Vector2.Zero && rollIndicator == 0.0f)
            {
                //if (ControllerInfo.Controller.IsLocalPlayer() || Sync.IsServer)
                if ((ControllerInfo.IsLocallyControlled() && CubeGrid.GridSystems.ControlSystem.IsLocallyControlled) || (Sync.IsServer && false))
                {
                    ClearMovementControl();
                }
                return;
            }

            if (IsMainCockpit == false && CubeGrid.HasMainCockpit())
            {
                return;
            }

            if (GridThrustSystem == null)
                return;

            if (GridGyroSystem == null)
                return;

            //System.Diagnostics.Debug.Assert(GridPowerDistributor != null);
            //System.Diagnostics.Debug.Assert(GridGyroSystem != null);
            //System.Diagnostics.Debug.Assert(GridThrustSystem != null);

            if (GridPowerDistributor == null)
                return;

            if (!Sync.Players.HasExtendedControl(this, this.CubeGrid))
                return;

            if (!m_enableShipControl)
                return;

            try
            {
                // Engine off, no control forces, early return
                if (CubeGrid.GridSystems.PowerDistributor.PowerState != MyPowerStateEnum.NoPower)
                {
                    // mouse pixels will do maximal rotation
                    const float pixelsForMaxRotation = 20;
                    rotationIndicator /= pixelsForMaxRotation;
                    Vector2.ClampToSphere(ref rotationIndicator, 1.0f);
                    rollIndicator *= RollControlMultiplier;

                    Matrix orientMatrix;
                    Orientation.GetMatrix(out orientMatrix);

                    var controlThrust = Vector3.Transform(moveIndicator, orientMatrix);
                    var controlTorque = Vector3.Transform(new Vector3(-rotationIndicator.X, -rotationIndicator.Y, -rollIndicator), orientMatrix);
                    Vector3.ClampToSphere(controlTorque, 1.0f);

                    GridThrustSystem.ControlThrust = controlThrust;
                    GridGyroSystem.ControlTorque = controlTorque;

                    if (MyFakes.ENABLE_WHEEL_CONTROLS_IN_COCKPIT)
                    {
                        if (GridWheels != null && ControlWheels)
                        {
                            GridWheels.CockpitMatrix = orientMatrix;
                            GridWheels.AngularVelocity = moveIndicator;
                        }
                    }
                }
            }
            finally
            {
                // Need it every frame because of MP interpolation
                CubeGrid.SyncObject.SendControlThrustAndTorque(GridThrustSystem.ControlThrust, GridGyroSystem.ControlTorque);
            }
        }

        public void MoveAndRotateStopped()
        {
            ClearMovementControl();
        }

        private void ClearMovementControl()
        {
            if (!m_enableShipControl)
                return;

            if (GridThrustSystem != null)
            {
                if (GridThrustSystem.ControlThrust != Vector3.Zero ||
                    GridGyroSystem.ControlTorque != Vector3.Zero)
                {
                    GridThrustSystem.ControlThrust = Vector3.Zero;
                    GridGyroSystem.ControlTorque = Vector3.Zero;
                }

                // Need it every frame because of MP interpolation
                CubeGrid.SyncObject.SendControlThrustAndTorque(Vector3.Zero, Vector3.Zero);
            }

            if (MyFakes.ENABLE_WHEEL_CONTROLS_IN_COCKPIT)
            {
                if (GridWheels != null)
                    GridWheels.AngularVelocity = Vector3.Zero;
            }
        }

        public bool ForceFirstPersonCamera
        {
            get
            {
                return (m_forcedFPS && m_enableFirstPerson);
            }
            set
            {
                if (m_forcedFPS != value)
                {
                    m_forcedFPS = value;

                    UpdateCameraAfterChange(false);
                }
            }
        }

        public bool EnableFirstPerson
        {
            get
            {
                return m_enableFirstPerson;
            }
        }

        #endregion

        #region Update

        public override void UpdatingStopped()
        {
            base.UpdatingStopped();

            ClearMovementControl();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            UpdateShipInfo();

            //Debug.Assert(GridGyroSystem != null && GridThrustSystem != null && Parent.Physics != null && m_cameraSpring != null && m_cameraShake != null, "CALL PROGRAMMER, this cant happen");

            // Vector3.One is max power, larger values will be clamped
            //if (GridThrustSystem != null && GridGyroSystem != null && ControllerInfo.Controller.IsLocalPlayer())
            //{
            //    if (
            //        (GridThrustSystem.ControlThrust != Vector3.Zero) ||
            //        (GridGyroSystem.ControlTorque != Vector3.Zero)
            //        )
            //    {
            //        CubeGrid.SyncObject.RequestControlThrustAndTorque(Vector3.Zero, Vector3.Zero);
            //        GridThrustSystem.ControlThrust = Vector3.Zero;
            //        GridGyroSystem.ControlTorque = Vector3.Zero;
            //    }
            //}

            if (ControllerInfo.Controller != null && MySession.LocalHumanPlayer != null && ControllerInfo.Controller == MySession.LocalHumanPlayer.Controller)
            {
                var shipController = CubeGrid.GridSystems.ControlSystem.GetController();
                if (shipController == ControllerInfo.Controller)
                {
                    if (m_noControlNotification != null)
                    {
                        MyHud.Notifications.Remove(m_noControlNotification);
                        m_noControlNotification = null;
                    }
                }
                else
                {
                    if (m_noControlNotification == null && EnableShipControl)
                    {
                        if (shipController == null)
                        {
                            m_noControlNotification = new MyHudNotification(MySpaceTexts.Notification_NoControlAutoPilot, 0);
                        }
                        else
                        {
                            if (CubeGrid.IsStatic)
                            {
                                m_noControlNotification = new MyHudNotification(MySpaceTexts.Notification_NoControlStation, 0);
                            }
                            else
                            {
                                m_noControlNotification = new MyHudNotification(MySpaceTexts.Notification_NoControl, 0);
                            }
                        }
                        MyHud.Notifications.Add(m_noControlNotification);
                    }
                }
            }

            foreach (MyShootActionEnum action in MyEnum<MyShootActionEnum>.Values)
            {
                if (SyncObject.IsShooting(action))
                {
                    Shoot(action);
                }
            }

            if (CanBeMainCockpit())
            {
                if (CubeGrid.HasMainCockpit() && CubeGrid.IsMainCockpit(this) == false)
                {
                    DetailedInfo.Clear();
                    DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MainCockpit));
                    DetailedInfo.Append(": " + CubeGrid.MainCockpit.CustomName);
                }
                else
                {
                    DetailedInfo.Clear();
                }
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
        }

        public override void UpdateBeforeSimulation10()
        {
            //System.Diagnostics.Debug.Assert(GridPowerDistributor != null);
            //System.Diagnostics.Debug.Assert(GridGyroSystem != null);
            //System.Diagnostics.Debug.Assert(GridThrustSystem != null);

            //if (this.Controller.Player != null && this.CubeGrid.Controller == null)
            //    this.CubeGrid.Controller = this;

            if (GridPowerDistributor == null)
                return;
            if (GridGyroSystem == null)
                return;
            if (GridThrustSystem == null)
                return;

            UpdateShipInfo10();

            base.UpdateBeforeSimulation10();
        }

        private void UpdateShipInfo()
        {
            if (!MySandboxGame.IsDedicated && MySession.LocalHumanPlayer != null)
            {
                if (ControllerInfo.Controller != MySession.LocalHumanPlayer.Controller)
                {
                    return;
                }
            }
            //These values are cached
            if (GridPowerDistributor != null)
            {
                MyHud.ShipInfo.FuelRemainingTime = GridPowerDistributor.RemainingFuelTime;
                MyHud.ShipInfo.Reactors = GridPowerDistributor.MaxAvailablePower;
                MyHud.ShipInfo.PowerState = GridPowerDistributor.PowerState;
            }
            if (GridGyroSystem != null)
                MyHud.ShipInfo.GyroCount = GridGyroSystem.GyroCount;
            if (GridThrustSystem != null)
            {
                MyHud.ShipInfo.ThrustCount = GridThrustSystem.ThrustCount;
                MyHud.ShipInfo.DampenersEnabled = GridThrustSystem.DampenersEnabled;
            }
        }

        protected virtual void UpdateShipInfo10(bool controlAcquired = false)
        {
            if (GridPowerDistributor == null)
                return;
            if (GridGyroSystem == null)
                return;
            if (GridThrustSystem == null)
                return;
            if (Parent.Physics == null)
                return;

            if (ControllerInfo.IsLocallyHumanControlled())
            {
                MyHud.ShipInfo.PowerUsage = (GridPowerDistributor.MaxAvailablePower != 0.0f)
                    ? (GridPowerDistributor.TotalRequiredInput / GridPowerDistributor.MaxAvailablePower)
                    : 0.0f;
                MyHud.ShipInfo.Speed = Parent.Physics.LinearVelocity.Length();
                MyHud.ShipInfo.ReflectorLights = GridReflectorLights.ReflectorsEnabled;

                MyHud.ShipInfo.NumberOfBatteries = GridPowerDistributor.GetProducerCount(MyProducerGroupEnum.Battery);

                GridPowerDistributor.UpdateHud(MyHud.ConsumerGroupInfo);
                MyHud.ShipInfo.LandingGearsTotal = CubeGrid.GridSystems.LandingSystem.TotalGearCount;
                MyHud.ShipInfo.LandingGearsLocked = CubeGrid.GridSystems.LandingSystem[Interfaces.LandingGearMode.Locked];
                MyHud.ShipInfo.LandingGearsInProximity = CubeGrid.GridSystems.LandingSystem[Interfaces.LandingGearMode.ReadyToLock];

                if (GridPowerDistributor.ProducersEnabled != MyMultipleEnabledEnum.NoObjects)
                {
                    if (GridPowerDistributor.ProducersEnabled == MyMultipleEnabledEnum.AllEnabled)
                    {
                        MyHud.Notifications.Remove(m_notificationReactorsOn);
                        MyHud.Notifications.Add(m_notificationReactorsOff);
                    }
                    else
                    {
                        MyHud.Notifications.Remove(m_notificationReactorsOff);
                        MyHud.Notifications.Add(m_notificationReactorsOn);
                    }
                }

                if (controlAcquired)
                    UpdateShipInfo100();
            }
        }

        private void UpdateShipInfo100()
        {
            if (ControllerInfo.IsLocallyHumanControlled())
            {
                MyHud.ShipInfo.Mass = (int)Parent.Physics.Mass;
            }
        }

        public override void UpdateBeforeSimulation100()
        {
            //System.Diagnostics.Debug.Assert(GridPowerDistributor != null);
            //System.Diagnostics.Debug.Assert(GridGyroSystem != null);
            //System.Diagnostics.Debug.Assert(GridThrustSystem != null);

            if (GridPowerDistributor == null)
                return;
            if (GridGyroSystem == null)
                return;
            if (GridThrustSystem == null)
                return;

            // This is here probably to give control to the second player when the first one leaves his cockpit. TODO: Do it properly
            //TryExtendControlToGroup();

            UpdateShipInfo100();

            UpdateSoundState();

            base.UpdateBeforeSimulation100();
        }

        // Returns true when the first controller has priority
        public static bool HasPriorityOver(MyShipController first, MyShipController second)
        {
            Debug.Assert(first.CubeGrid != null, "Ship controller cube grid was null");
            Debug.Assert(second.CubeGrid != null, "Ship controller cube grid was null");

            if (first.Priority < second.Priority) return true;
            if (first.Priority > second.Priority) return false;

            if (first.CubeGrid.Physics == null && second.CubeGrid.Physics == null)
            {
                return first.CubeGrid.BlocksCount > second.CubeGrid.BlocksCount;
            }
            else if (first.CubeGrid.Physics != null && second.CubeGrid.Physics != null)
            {
                return first.CubeGrid.Physics.Mass > second.CubeGrid.Physics.Mass;
            }
            else
            {
                return first.CubeGrid.Physics == null;
            }
        }

#warning Get rid of this once everything is working without it
        private void TryExtendControlToGroup()
        {
            Debug.Assert(Sync.IsServer, "Extending grid control on client is forbidden!");
            if (!Sync.IsServer) return;

            //Try to get control of group, early return on fail (you control whole group or nothing)
            if (m_enableShipControl && ControllerInfo.Controller != null)
            {
                bool canTakeControl = false;
                bool forceReleaseControl = false;

                var group = ControlGroup.GetGroup(CubeGrid);

                var groupController = CubeGrid.GridSystems.ControlSystem.GetController();
                if (group != null)
                {
                    if (groupController == null)
                    {
                        canTakeControl = true;
                    }
                    else
                    {
                        var shipController = groupController.ControlledEntity as MyShipController;
                        if (shipController != null)
                        {
                            if (this.Priority < shipController.Priority)
                            {
                                canTakeControl = true;
                                forceReleaseControl = true;
                            }
                            else
                            {
                                if (this.CubeGrid.Physics.Mass > shipController.CubeGrid.Physics.Mass)
                                {
                                    canTakeControl = true;
                                }
                            }
                        }
                    }
                }

                if (canTakeControl)
                {
                    if (groupController != null)
                    {
                        var shipController = groupController.ControlledEntity as MyShipController;
                        foreach (var node in group.Nodes)
                            Sync.Players.TryReduceControl(shipController, node.NodeData);

                        if (forceReleaseControl)
                        {
                            shipController.ForceReleaseControl();
                        }
                    }

                    Sync.Players.SetControlledEntity(ControllerInfo.Controller.Player.Id, this);

                    foreach (var node in group.Nodes)
                        Sync.Players.TryExtendControl(this, node.NodeData);
                }

                if (Sync.Players.HasExtendedControl(this, CubeGrid))
                    GridThrustSystem.Enabled = m_controlThrusters;
            }
        }
        #endregion

        #region Notifications

        private void RefreshControlNotifications()
        {
            RemoveControlNotifications();

            if (m_notificationReactorsOn == null)
            {
                var controlName = MyInput.Static.GetGameControl(MyControlsSpace.TOGGLE_REACTORS).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard);
                m_notificationReactorsOn = new MyHudNotification(MySpaceTexts.NotificationHintTurnAllReactorsOn, 0);
                if (!MyInput.Static.IsJoystickConnected())
                    m_notificationReactorsOn.SetTextFormatArguments(controlName);
                else
                    m_notificationReactorsOn.SetTextFormatArguments(MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_SPACESHIP, MyControlsSpace.TOGGLE_REACTORS));
                m_notificationReactorsOn.Level = MyNotificationLevel.Control;
            }

            if (m_notificationReactorsOff == null)
            {
                var controlName = MyInput.Static.GetGameControl(MyControlsSpace.TOGGLE_REACTORS).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard);
                m_notificationReactorsOff = new MyHudNotification(MySpaceTexts.NotificationHintTurnAllReactorsOff, 0);
                if (!MyInput.Static.IsJoystickConnected())
                    m_notificationReactorsOff.SetTextFormatArguments(controlName);
                else
                    m_notificationReactorsOff.SetTextFormatArguments(MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_SPACESHIP, MyControlsSpace.TOGGLE_REACTORS));
                m_notificationReactorsOff.Level = MyNotificationLevel.Control;
            }

            if (m_notificationLeave == null)
            {
                var controlName = MyInput.Static.GetGameControl(MyControlsSpace.USE).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard);
                m_notificationLeave = new MyHudNotification(LeaveNotificationHintText, 0);
                if (!MyInput.Static.IsJoystickConnected())
                    m_notificationLeave.SetTextFormatArguments(controlName);
                else
                    m_notificationLeave.SetTextFormatArguments(MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_SPACESHIP, MyControlsSpace.USE));
                m_notificationLeave.Level = MyNotificationLevel.Control;
            }

            if (m_notificationTerminal == null)
            {
                var controlName = MyInput.Static.GetGameControl(MyControlsSpace.TERMINAL).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard);
                if (!MyInput.Static.IsJoystickConnected())
                {
                    m_notificationTerminal = new MyHudNotification(MySpaceTexts.NotificationHintOpenShipControlPanel, 0);
                    m_notificationTerminal.SetTextFormatArguments(controlName);
                    m_notificationTerminal.Level = MyNotificationLevel.Control;
                }
                else
                {
                    m_notificationTerminal = null;
                }
            }

            if (m_notificationWeaponMode == null)
            {
                var controlName = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_COLOR_CHANGE).GetControlButtonName(MyGuiInputDeviceEnum.Mouse);
                if (!MyInput.Static.IsJoystickConnected())
                {
                    m_notificationWeaponMode = new MyHudNotification(MySpaceTexts.NotificationHintSwitchWeaponMode, 0);
                    m_notificationWeaponMode.SetTextFormatArguments(controlName);
                    m_notificationWeaponMode.Level = MyNotificationLevel.Control;
                }
                else
                {
                    m_notificationWeaponMode = null;
                }
            }

            MyHud.Notifications.Add(m_notificationLeave);
            if (m_notificationTerminal != null)
                MyHud.Notifications.Add(m_notificationTerminal);
            if (m_notificationWeaponMode != null)
                MyHud.Notifications.Add(m_notificationWeaponMode);
        }

        private void RemoveControlNotifications()
        {
            if (m_notificationReactorsOff != null)
                MyHud.Notifications.Remove(m_notificationReactorsOff);

            if (m_notificationReactorsOn != null)
                MyHud.Notifications.Remove(m_notificationReactorsOn);

            if (m_notificationLeave != null)
                MyHud.Notifications.Remove(m_notificationLeave);

            if (m_notificationTerminal != null)
                MyHud.Notifications.Remove(m_notificationTerminal);

            if (m_notificationWeaponMode != null)
                MyHud.Notifications.Remove(m_notificationWeaponMode);
        }

        #endregion

        #region Object control

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);
        }

        public override void OnAddedToScene(object source)
        {
            bool nearFlag = Render.NearFlag;
            Render.NearFlag = false;

            base.OnAddedToScene(source);
        }

        protected virtual void OnControlAcquired_UpdateCamera()
        {
        }

        protected virtual bool IsCameraController()
        {
            return false;
        }

        protected void OnControlAcquired(MyEntityController controller)
        {
            // Try to take control of ship
            // This won't be here at all
            if (MySession.LocalHumanPlayer == controller.Player)
            {
                if (MyFakes.ENABLE_WHEEL_CONTROLS_IN_COCKPIT)
                    GridWheels.InitControl();

                if (MySession.Static.CameraController is MyEntity && IsCameraController())
                    MySession.SetCameraController(MyCameraControllerEnum.Entity, this);

                //if (MyGuiScreenGamePlay.Static != null)
                //    MySession.Static.CameraAttachedToChanged += Static_CameraAttachedToChanged;

                if (MySession.Static.Settings.RespawnShipDelete && controller.Player.RespawnShip.Contains(CubeGrid.EntityId))
                    MyHud.Notifications.Add(MyNotificationSingletons.RespawnShipWarning);

                Static_CameraAttachedToChanged(null, null);

                RefreshControlNotifications();

                if (IsCameraController())
                {
                    OnControlAcquired_UpdateCamera();
                }

                MyHud.HideAll();
                MyHud.ShipInfo.Show(null);
                MyHud.Crosshair.Show(null);
                MyHud.CharacterInfo.Show(null);
                MyHud.ConsumerGroupInfo.Visible = true;
                MyHud.GravityIndicator.Entity = this;
                MyHud.GravityIndicator.Show(null);
                MyHud.OreMarkers.Visible = true;
                MyHud.LargeTurretTargets.Visible = true;
            }
            else
            {
#warning TODO: Add player name change support
                //controller.Player.OnDisplayNameChanged += UpdateHudMarker;
                UpdateHudMarker();
            }

            if (m_enableShipControl && (IsMainCockpit == true || CubeGrid.HasMainCockpit() == false))
            {
                var group = ControlGroup.GetGroup(CubeGrid);
                if (group != null)
                {
                    group.GroupData.ControlSystem.AddControllerBlock(this);
                }
                /*if (Sync.IsServer)
                {
                    var group = ControlGroup.GetGroup(CubeGrid);
                    if (group != null)
                    {
                        if (!CubeGrid.GridSystems.ControlSystem.ControllerSteamId.HasValue)
                        {
                            foreach (var node in group.Nodes)
                                Sync.Controllers.TryExtendControl(this, node.NodeData);
                        }
                    }
                    else
                        Sync.Controllers.TryExtendControl(this, CubeGrid);

                }*/

                GridSelectionSystem.OnControlAcquired();
            }

            if (controller == Sync.Players.GetEntityController(CubeGrid) && GridThrustSystem != null)
                GridThrustSystem.Enabled = m_controlThrusters;

            UpdateShipInfo10(true);

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

            if (Sync.IsServer || controller.Player == MySession.LocalHumanPlayer)
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        protected virtual void OnControlReleased_UpdateCamera()
        {

        }

        protected virtual void OnControlReleased(MyEntityController controller)
        {
            // Release control of the ship
            if (Sync.Players.GetEntityController(this) == controller && GridThrustSystem != null)
                GridThrustSystem.Enabled = true;

            if (MySession.LocalHumanPlayer == controller.Player)
            {
                OnControlReleased_UpdateCamera();

                ForceFirstPersonCamera = false;

                if (MyGuiScreenGamePlay.Static != null)
                {
                    Static_CameraAttachedToChanged(null, null);
                    //MySession.Static.CameraAttachedToChanged -= Static_CameraAttachedToChanged;
                }

                MyHud.Notifications.Remove(MyNotificationSingletons.RespawnShipWarning);

                RemoveControlNotifications();

                if (GridThrustSystem != null)
                {
                    ClearMovementControl();
                }

                MyHud.ShipInfo.Hide();
                MyHud.GravityIndicator.Hide();
                MyHud.Crosshair.Hide();
                MyHud.LargeTurretTargets.Visible = false;
                MyHud.Notifications.Remove(m_noControlNotification);
            }
            else
            {
                if (!MyFakes.ENABLE_RADIO_HUD)
                {
                    MyHud.LocationMarkers.UnregisterMarker(this);
                }
#warning TODO: Add player name changing support
                //controller.Player.OnDisplayNameChanged -= UpdateHudMarker;
            }

            if (SyncObject.IsShooting())
            {
                EndShootAll();
            }

            if (m_enableShipControl)
            {
                if (GridSelectionSystem != null)
                {
                    GridSelectionSystem.OnControlReleased();
                }

                //if (Sync.IsServer)
                /*{
                    var group = ControlGroup.GetGroup(CubeGrid);
                    Debug.Assert(group != null, "Grid should be in group when player lefts cockpit?");
                    if (group != null)
                    {
                        foreach (var node in group.Nodes)
                            Sync.Controllers.RemoveControlledEntity(node.NodeData);
                    }
                }*/
                var group = ControlGroup.GetGroup(CubeGrid);
                if (group != null)
                {
                    group.GroupData.ControlSystem.RemoveControllerBlock(this);
                }
            }
        }

        //Will be called when someone kicks player out of controller
        public virtual void ForceReleaseControl()
        {

        }

        void UpdateHudMarker()
        {
            if (!MyFakes.ENABLE_RADIO_HUD)
            {
                MyHud.LocationMarkers.RegisterMarker(this, new MyHudEntityParams()
                {
                    FlagsEnum = MyHudIndicatorFlagsEnum.SHOW_TEXT,
                    Text = new StringBuilder(ControllerInfo.Controller.Player.DisplayName),
                    ShouldDraw = MyHud.CheckShowPlayerNamesOnHud,
                    MustBeDirectlyVisible = true,
                });
            }
        }

        protected virtual bool ShouldSit()
        {
            return !m_enableShipControl;
        }

        #endregion

        #region Interactions
        void Static_CameraAttachedToChanged(IMyCameraController oldController, IMyCameraController newController)
        {
            if (MySession.ControlledEntity == this && newController != MyThirdPersonSpectator.Static && newController != this)
            {
                EndShootAll();
            }

            UpdateCameraAfterChange();
        }

        protected virtual void UpdateCameraAfterChange(bool resetHeadLocalAngle = true)
        {
        }

        public void Shoot(MyShootActionEnum action)
        {
            if (m_enableShipControl && !SyncObject.IsWaitingForWeaponSwitch)
            {
                MyGunStatusEnum status;
                IMyGunObject<MyDeviceBase> gun;
                if (GridSelectionSystem.CanShoot(action, out status, out gun))
                {
                    GridSelectionSystem.Shoot(action);
                }
            }
        }

        public void Zoom(bool newKeyPress)
        {
        }

        public void Use()
        {
            MyGuiAudio.PlaySound(MyGuiSounds.HudUse);
            SyncObject.ControlledEntity_Use();
        }

        public void UseContinues()
        {
        }

        public void UseFinished()
        {
        }

        public void Crouch()
        {
        }

        public void Jump()
        {
        }

        public void SwitchWalk()
        {
        }

        public void Sprint()
        {
        }

        public void Up()
        {
        }

        public void Down()
        {
        }

        public virtual void ShowInventory()
        {
        }

        public virtual void ShowTerminal()
        {
        }

        public void SwitchBroadcasting()
        {

        }

        public void SwitchDamping()
        {
            if (m_enableShipControl)
                EnableDampingInternal(!GridThrustSystem.DampenersEnabled, true);
        }

        internal void EnableDampingInternal(bool enableDampeners, bool updateProxy)
        {
            GridThrustSystem.DampenersEnabled = enableDampeners;

            if (updateProxy)
            {
                SyncObject.SendDampenersUpdate(enableDampeners);
            }

            if (ControllerInfo.IsLocallyHumanControlled())
            {
                if (m_inertiaDampenersNotification == null)
                    m_inertiaDampenersNotification = new MyHudNotification();
                m_inertiaDampenersNotification.Text = (GridThrustSystem.DampenersEnabled ? MySpaceTexts.NotificationInertiaDampenersOn : MySpaceTexts.NotificationInertiaDampenersOff);
                MyHud.Notifications.Add(m_inertiaDampenersNotification);
            }
        }

        public virtual void SwitchThrusts()
        {
        }

        public void Die()
        {
        }

        public void SwitchLights()
        {
            if (m_enableShipControl)
            {
                if (GridReflectorLights.ReflectorsEnabled == MyMultipleEnabledEnum.AllDisabled)
                    GridReflectorLights.ReflectorsEnabled = MyMultipleEnabledEnum.AllEnabled;
                else
                    // When some lights are on, we consider lights to be on and want to disable them.
                    GridReflectorLights.ReflectorsEnabled = MyMultipleEnabledEnum.AllDisabled;
            }
        }

        public void SwitchLeadingGears()
        {
            if (m_enableShipControl)
            {
                CubeGrid.GridSystems.LandingSystem.Switch();
                CubeGrid.GridSystems.ConveyorSystem.ToggleConnectors();
            }
        }

        public void SwitchReactors()
        {
            if (m_enableShipControl)
            {
                if (GridPowerDistributor.ProducersEnabled != MyMultipleEnabledEnum.AllEnabled)
                {
                    CubeGrid.SyncObject.SendPowerDistributorState(MyMultipleEnabledEnum.AllEnabled, MySession.LocalPlayerId);
                }
                else
                {
                    CubeGrid.SyncObject.SendPowerDistributorState(MyMultipleEnabledEnum.AllDisabled, MySession.LocalPlayerId);
                }
            }
        }

        #endregion

        #region HUD

        public void DrawHud(IMyCameraController camera, long playerId)
        {
            if (GridSelectionSystem != null)
                GridSelectionSystem.DrawHud(camera, playerId);

            var to = PositionComp.GetPosition() + 1000 * PositionComp.WorldMatrix.Forward;

            Vector2 target = Vector2.Zero;
            if (MyHudCrosshair.GetProjectedVector(to, ref target))
            {
                MyHud.Crosshair.Position = target;
            }
        }

        #endregion

        #region Properties

        public MyEntity TopGrid
        {
            get
            {
                return Parent;
            }
        }

        public MyEntity IsUsing
        {
            get { return null; }
        }

        public virtual bool IsLargeShip()
        {
            return true;
        }

        protected override bool ShouldSync
        {
            get
            {
                // Don't sync update
                return false;
            }
        }

        public override Vector3D LocationForHudMarker
        {
            get
            {
                return base.LocationForHudMarker + (0.65 * CubeGrid.GridSize * BlockDefinition.Size.Y * PositionComp.WorldMatrix.Up);
            }
        }

        public new MyShipControllerDefinition BlockDefinition
        {
            get { return base.BlockDefinition as MyShipControllerDefinition; }
        }

        #endregion

        #region Player controlled parameters

        public bool ControlThrusters
        {
            get { return m_controlThrusters; }
            set
            {
                m_controlThrusters = value;
                if (Sync.Players.HasExtendedControl(this, CubeGrid))
                    GridThrustSystem.Enabled = m_controlThrusters;
            }
        }

        public bool ControlWheels
        {
            get { return m_controlWheels; }
            set { m_controlWheels = value; }
        }

        #endregion


        //public MyGunTypeEnum? GetWeaponType(MyObjectBuilderType weapon)
        //{
        //    if (weapon == typeof(MyObjectBuilder_Drill))
        //        return MyGunTypeEnum.Drill;

        //    else if (weapon == typeof(MyObjectBuilder_SmallMissileLauncher))
        //        return MyGunTypeEnum.Missile;

        //    else if (weapon == typeof(MyObjectBuilder_SmallGatlingGun))
        //        return MyGunTypeEnum.Projectile;

        //    else if (weapon == typeof(MyObjectBuilder_ShipGrinder))
        //        return MyGunTypeEnum.AngleGrinder;

        //    else if (weapon == typeof(MyObjectBuilder_ShipWelder))
        //        return MyGunTypeEnum.Welder;

        //    else
        //        return null;
        //}


        public bool CanSwitchToWeapon(MyDefinitionId? weapon)
        {
            if (weapon == null) return true;

            var type = weapon.Value.TypeId;
            if (type == typeof(MyObjectBuilder_Drill) ||
                type == typeof(MyObjectBuilder_SmallMissileLauncher) ||
                type == typeof(MyObjectBuilder_SmallGatlingGun) ||
                type == typeof(MyObjectBuilder_ShipGrinder) ||
                type == typeof(MyObjectBuilder_ShipWelder) ||
                type == typeof(MyObjectBuilder_SmallMissileLauncherReload))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void SwitchToWeapon(MyDefinitionId weapon)
        {
            if (m_enableShipControl)
            {
                SwitchToWeaponInternal(weapon, true);
            }
        }

		public void SwitchToWeapon(MyToolbarItemWeapon weapon)
		{
			if (m_enableShipControl)
			{
				SwitchToWeaponInternal((weapon != null ? weapon.Definition.Id : (MyDefinitionId?)null), true);
			}
		}

        public void RequestUse(UseActionEnum actionEnum, MyCharacter user)
        {
            SyncObject.RequestUse(actionEnum, user);
        }

        void SwitchToWeaponInternal(MyDefinitionId? weapon, bool updateSync)
        {
            if (updateSync)
            {
                SyncObject.RequestSwitchToWeapon(weapon, null, 0);
                return;
            }

            StopCurrentWeaponShooting();

            if (weapon.HasValue)
            {
                //    var gun = GetWeaponType(weapon.Value.TypeId);

                SwitchToWeaponInternal(weapon);
            }
            else
            {
                m_selectedGunId = null;
                GridSelectionSystem.SwitchTo(null);
            }
        }

        void SwitchToWeaponInternal(MyDefinitionId? gunId)
        {
            GridSelectionSystem.SwitchTo(gunId, m_singleWeaponMode);
            m_selectedGunId = gunId;

            Debug.Assert(gunId != null, "gunType Should not be null when switching weapon. (Cestmir)");
            if (ControllerInfo.IsLocallyHumanControlled())
            {
                if (m_weaponSelectedNotification == null)
                    m_weaponSelectedNotification = new MyHudNotification(MySpaceTexts.NotificationSwitchedToWeapon);
                m_weaponSelectedNotification.SetTextFormatArguments(MyDeviceBase.GetGunNotificationName(m_selectedGunId.Value));
                MyHud.Notifications.Add(m_weaponSelectedNotification);
            }
        }

        void SwitchAmmoMagazineInternal(bool sync)
        {
            if (sync)
            {
                SyncObject.RequestSwitchAmmoMagazine();
                return;
            }

            if (m_enableShipControl && !SyncObject.IsWaitingForWeaponSwitch)
            {
                GridSelectionSystem.SwitchAmmoMagazine();
            }
        }

        void SwitchAmmoMagazineSuccess()
        {
            if (GridSelectionSystem.CanSwitchAmmoMagazine())
            {
                SwitchAmmoMagazineInternal(false);
            }
        }

        private void ShowShootNotification(MyGunStatusEnum status, IMyGunObject<MyDeviceBase> weapon)
        {
            if (!ControllerInfo.IsLocallyHumanControlled())
                return;

            switch (status)
            {
                case MyGunStatusEnum.NotSelected:
                    if (m_noWeaponNotification == null)
                    {
                        m_noWeaponNotification = new MyHudNotification(MySpaceTexts.NotificationNoWeaponSelected, 2000, font: MyFontEnum.Red);
                        MyHud.Notifications.Add(m_noWeaponNotification);
                    }

                    MyHud.Notifications.Add(m_noWeaponNotification);
                    break;
                case MyGunStatusEnum.OutOfAmmo:
                    if (m_outOfAmmoNotification == null)
                    {
                        m_outOfAmmoNotification = new MyHudNotification(MySpaceTexts.OutOfAmmo, 2000, font: MyFontEnum.Red);
                    }

                    if (weapon is MyCubeBlock)
                        m_outOfAmmoNotification.SetTextFormatArguments((weapon as MyCubeBlock).DisplayNameText);

                    MyHud.Notifications.Add(m_outOfAmmoNotification);
                    break;
                case MyGunStatusEnum.NotFunctional:
                case MyGunStatusEnum.OutOfPower:
                    if (m_weaponNotWorkingNotification == null)
                    {
                        m_weaponNotWorkingNotification = new MyHudNotification(MySpaceTexts.NotificationWeaponNotWorking, 2000, font: MyFontEnum.Red);
                    }

                    if (weapon is MyCubeBlock)
                        m_weaponNotWorkingNotification.SetTextFormatArguments((weapon as MyCubeBlock).DisplayNameText);

                    MyHud.Notifications.Add(m_weaponNotWorkingNotification);
                    break;
                default:
                    break;
            }
        }

        public override void OnRegisteredToGridSystems()
        {
            GridThrustSystem = CubeGrid.GridSystems.ThrustSystem;
            GridGyroSystem = CubeGrid.GridSystems.GyroSystem;
            GridReflectorLights = CubeGrid.GridSystems.ReflectorLightSystem;

            if (MyFakes.ENABLE_WHEEL_CONTROLS_IN_COCKPIT)
            {
                ControlThrusters = m_controlThrusters;
                ControlWheels = m_controlWheels;
            }

            CubeGrid.AddedToLogicalGroup += CubeGrid_AddedToLogicalGroup;
            CubeGrid.RemovedFromLogicalGroup += CubeGrid_RemovedFromLogicalGroup;
            SetWeaponSystem(CubeGrid.GridSystems.WeaponSystem);

            base.OnRegisteredToGridSystems();
        }

        public override void OnUnregisteredFromGridSystems()
        {
            //System.Diagnostics.Debug.Assert(GridThrustSystem != null);
            //System.Diagnostics.Debug.Assert(GridGyroSystem != null);
            //System.Diagnostics.Debug.Assert(GridWeaponSystem != null);

            if (GridThrustSystem != null)
            {
                ClearMovementControl();
            }

            CubeGrid.AddedToLogicalGroup -= CubeGrid_AddedToLogicalGroup;
            CubeGrid.RemovedFromLogicalGroup -= CubeGrid_RemovedFromLogicalGroup;
            CubeGrid_RemovedFromLogicalGroup();

            GridThrustSystem = null;
            GridGyroSystem = null;
            GridReflectorLights = null;

            base.OnUnregisteredFromGridSystems();
        }

        private void CubeGrid_RemovedFromLogicalGroup()
        {
            GridSelectionSystem.WeaponSystem = null;
            GridSelectionSystem.SwitchTo(null);
        }

        private void CubeGrid_AddedToLogicalGroup(MyGridLogicalGroupData obj)
        {
            SetWeaponSystem(obj.WeaponSystem);
        }

        public void SetWeaponSystem(MyGridWeaponSystem weaponSystem)
        {
            GridSelectionSystem.WeaponSystem = weaponSystem;
            GridSelectionSystem.SwitchTo(m_selectedGunId, m_singleWeaponMode);
        }

        public override void UpdateVisual()
        {
            if (Render.NearFlag)
            {
                //TODO: Find out how to correctly change Near model
                return;
            }

            base.UpdateVisual();
        }


        #region Multiplayer

        protected override MySyncEntity OnCreateSync()
        {
            var sync = new MySyncShipController(this);
            OnInitSync(sync);
            return sync;
        }

        protected virtual void OnInitSync(MySyncShipController sync)
        {
            sync.UseSuccess += sync_UseSuccess;
            sync.UseFailed += sync_UseFailed;
            sync.ControlledEntity_Used += sync_ControlledEntity_Used;
            sync.SwitchToWeaponSuccessHandler += SwitchToWeaponSuccess;
            sync.SwitchAmmoMagazineSuccessHandler += SwitchAmmoMagazineSuccess;
            sync.PilotRelativeEntryUpdated += sync_PilotRelativeEntryUpdated;
            sync.DampenersUpdated += sync_DampenersUpdated;
        }

        protected virtual void sync_PilotRelativeEntryUpdated(MyPositionAndOrientation relativeEntry)
        {
        }

        void sync_DampenersUpdated(bool enableDampeners)
        {
            EnableDampingInternal(enableDampeners, false);
        }

        protected virtual void sync_UseSuccess(UseActionEnum actionEnum, IMyControllableEntity user)
        {
        }

        void sync_UseFailed(UseActionEnum actionEnum, UseActionResult actionResult, IMyControllableEntity user)
        {
            if (user != null && user.ControllerInfo.IsLocallyHumanControlled())
            {
                if (actionResult == UseActionResult.UsedBySomeoneElse)
                    MyHud.Notifications.Add(new MyHudNotification(MySpaceTexts.AlreadyUsedBySomebodyElse, 2500, MyFontEnum.Red));
                else if (actionResult == UseActionResult.AccessDenied)
                    MyHud.Notifications.Add(MyNotificationSingletons.AccessDenied);
                else if (actionResult == UseActionResult.Unpowered)
                    MyHud.Notifications.Add(new MyHudNotification(MySpaceTexts.BlockIsNotPowered, 2500, MyFontEnum.Red));
                else if (actionResult == UseActionResult.CockpitDamaged)
                    MyHud.Notifications.Add(new MyHudNotification(MySpaceTexts.Notification_CockpitIsDamaged, 2500, MyFontEnum.Red));
            }
        }

        void sync_ControlledEntity_Used()
        {
            OnControlledEntity_Used();
        }

        protected virtual void OnControlledEntity_Used() { }

        #endregion

        public MyEntity Entity
        {
            get { return this; }
        }

        private MyControllerInfo m_info = new MyControllerInfo();
        protected bool m_singleWeaponMode;
        protected Vector3 m_headLocalPosition;
        private MyHudNotification m_notificationWeaponMode;
        public MyControllerInfo ControllerInfo { get { return m_info; } }

        public new MySyncShipController SyncObject
        {
            get { return (MySyncShipController)base.SyncObject; }
        }

        void SwitchToWeaponSuccess(MyDefinitionId? weapon, MyObjectBuilder_Base weaponObjectBuilder, long weaponEntityId)
        {
            SwitchToWeaponInternal(weapon, false);
        }

        MyRechargeSocket IMyRechargeSocketOwner.RechargeSocket
        {
            get
            {
                return m_rechargeSocket;
            }
        }

        public void BeginShoot(MyShootActionEnum action)
        {
            if (!SyncObject.IsWaitingForWeaponSwitch)
            {
                MyGunStatusEnum status = MyGunStatusEnum.OK;
                IMyGunObject<MyDeviceBase> gun = null;
                bool canShoot = GridSelectionSystem.CanShoot(action, out status, out gun);

                if (status != MyGunStatusEnum.OK)
                {
                    ShowShootNotification(status, gun);
                }

                SyncObject.BeginShoot((Vector3)PositionComp.WorldMatrix.Forward, action);
            }
        }

        protected void EndShootAll()
        {
            foreach (MyShootActionEnum action in MyEnum<MyShootActionEnum>.Values)
            {
                if (SyncObject.IsShooting(action))
                    EndShoot(action);
            }
        }

        private void StopCurrentWeaponShooting()
        {
            foreach (MyShootActionEnum action in MyEnum<MyShootActionEnum>.Values)
            {
                if (SyncObject.IsShooting(action))
                {
                    GridSelectionSystem.EndShoot(action);
                }
            }
        }

        public void EndShoot(MyShootActionEnum action)
        {
            SyncObject.EndShoot(action);
        }

        public void OnBeginShoot(MyShootActionEnum action)
        {
            MyGunStatusEnum status = MyGunStatusEnum.OK;
            IMyGunObject<MyDeviceBase> gun = null;
            bool canShoot = GridSelectionSystem.CanShoot(action, out status, out gun);
            if (canShoot == false && status != MyGunStatusEnum.OK && status != MyGunStatusEnum.Cooldown)
            {
                ShootBeginFailed(action, status, gun);
            }
        }

        public void OnEndShoot(MyShootActionEnum action)
        {
            GridSelectionSystem.EndShoot(action);
        }

        private void ShootBeginFailed(MyShootActionEnum action, MyGunStatusEnum status, IMyGunObject<MyDeviceBase> failedGun)
        {
            failedGun.BeginFailReaction(action, status);
        }

        protected override void Closing()
        {
            if (MyFakes.ENABLE_NEW_SOUNDS)
                StopLoopSound();

            IsMainCockpit = false;
            CubeGrid.OnGridSplit -= CubeGrid_OnGridSplit;
            base.Closing();
        }

        protected virtual void UpdateSoundState()
        {

        }

        protected virtual void StartLoopSound()
        {

        }

        protected virtual void StopLoopSound()
        {

        }

        public void RemoveUsers(bool local)
        {
            if (local)
            {
                RemoveLocal();
            }
            else
                SyncObject.ControlledEntity_Use();
        }

        protected virtual void RemoveLocal()
        {

        }

        internal void SwitchWeaponMode()
        {
            SingleWeaponMode = !SingleWeaponMode;
        }

        public bool SingleWeaponMode
        {
            get { return m_singleWeaponMode; }
            private set
            {
                if (m_singleWeaponMode != value)
                {
                    m_singleWeaponMode = value;
					if (m_selectedGunId.HasValue)
						SwitchToWeapon(m_selectedGunId.Value);
					else
						SwitchToWeapon(null);
                }
            }
        }

        private bool m_isMainCockpit = false;
        public bool IsMainCockpit
        {
            get
            {
                return m_isMainCockpit;
            }
            set
            {
                if (value != m_isMainCockpit)
                {
                    m_isMainCockpit = value;
                    if (m_isMainCockpit)
                    {
                        CubeGrid.SetMainCockpit(this);
                    }
                    else
                    {
                        if (CubeGrid.IsMainCockpit(this))
                        {
                            CubeGrid.SetMainCockpit(null);
                        }
                    }
                    RaisePropertiesChanged();
                }
            }
        }

        private void SetMainCockpit(bool value)
        {
            if (value)
            {
                if (CubeGrid.HasMainCockpit() && !CubeGrid.IsMainCockpit(this))
                {
                    IsMainCockpit = false;
                    RaisePropertiesChanged();
                    return;
                }
            }
            IsMainCockpit = value;
            SyncObject.SendSetMainCockpit(IsMainCockpit);
        }

        protected virtual bool CanBeMainCockpit()
        {
            return false;
        }

        protected bool IsMainCockpitFree()
        {
            return CubeGrid.HasMainCockpit() == false || CubeGrid.IsMainCockpit(this);
        }


        public virtual MyToolbarType ToolbarType
        {
            get
            {
                return m_enableShipControl ? MyToolbarType.Ship : MyToolbarType.Seat;
            }
        }

        bool IMyModdingControllableEntity.ForceFirstPersonCamera
        {
            get
            {
                return ForceFirstPersonCamera;
            }
            set
            {
                ForceFirstPersonCamera = value;
            }
        }

        MatrixD Sandbox.ModAPI.Interfaces.IMyControllableEntity.GetHeadMatrix(bool includeY, bool includeX, bool forceHeadAnim, bool forceHeadBone = false)
        {
            return GetHeadMatrix(includeY, includeX, forceHeadAnim);
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator)
        {
            MoveAndRotate(moveIndicator, rotationIndicator, rollIndicator);
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.MoveAndRotateStopped()
        {
            MoveAndRotateStopped();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.Use()
        {
            Use();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.UseContinues()
        {
            UseContinues();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.Jump()
        {
            Jump();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.Up()
        {
            Up();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.Crouch()
        {
            Crouch();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.Down()
        {
            Down();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.ShowInventory()
        {
            ShowInventory();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.ShowTerminal()
        {
            ShowTerminal();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.SwitchThrusts()
        {
            SwitchThrusts();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.SwitchDamping()
        {
            SwitchDamping();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.SwitchLights()
        {
            SwitchLights();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.SwitchLeadingGears()
        {
            SwitchLeadingGears();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.SwitchReactors()
        {
            SwitchReactors();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.SwitchHelmet()
        {

        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.Die()
        {
            Die();
        }


        bool Sandbox.ModAPI.Interfaces.IMyControllableEntity.EnabledThrusts
        {
            get { return false; }
        }

        bool Sandbox.ModAPI.Interfaces.IMyControllableEntity.EnabledDamping
        {
            get { return GridThrustSystem.DampenersEnabled; }
        }

        bool Sandbox.ModAPI.Interfaces.IMyControllableEntity.EnabledLights
        {
            get { return GridReflectorLights.ReflectorsEnabled == MyMultipleEnabledEnum.AllEnabled; }
        }

        bool Sandbox.ModAPI.Interfaces.IMyControllableEntity.EnabledLeadingGears
        {
            get 
            {
                var state = CubeGrid.GridSystems.LandingSystem.Locked;
                return state == MyMultipleEnabledEnum.Mixed || state == MyMultipleEnabledEnum.AllEnabled;
            }
        }

        bool Sandbox.ModAPI.Interfaces.IMyControllableEntity.EnabledReactors
        {
            get { return GridPowerDistributor.ProducersEnabled == MyMultipleEnabledEnum.AllEnabled; }
        }

        bool IMyControllableEntity.EnabledBroadcasting
        {
            get { return false; }
        }

        bool Sandbox.ModAPI.Interfaces.IMyControllableEntity.EnabledHelmet
        {
            get { return false; }
        }

        void IMyControllableEntity.SwitchAmmoMagazine()
        {
            if (m_enableShipControl)
            {
                if (GridSelectionSystem.CanSwitchAmmoMagazine())
                {
                    SwitchAmmoMagazineInternal(true);
                }
            }
        }

        bool IMyControllableEntity.CanSwitchAmmoMagazine()
        {
            return m_selectedGunId.HasValue && GridSelectionSystem.CanSwitchAmmoMagazine();
        }

        public virtual float HeadLocalXAngle
        {
            get;
            set;
        }

        public virtual float HeadLocalYAngle
        {
            get;
            set;
        }

        bool IMyShipController.IsUnderControl { get { return ControllerInfo.Controller != null; } }

        bool IMyShipController.ControlWheels
        {
            get { return ControlWheels; }
        }
        bool IMyShipController.ControlThrusters
        {
            get { return ControlThrusters; }
        }

        bool IMyShipController.HandBrake
        {
            get
            {
                return CubeGrid.GridSystems.WheelSystem.HandBrake;
            }
        }
        bool IMyShipController.DampenersOverride
        {
            get
            {
                if (GridThrustSystem == null)
                {
                    Debug.Fail("Alex Florea: Grid thrust system should not be null!");
                    return false;
                }
                else
                {
                    return GridThrustSystem.DampenersEnabled;
                }
            }
        }

        void CubeGrid_OnGridSplit(MyCubeGrid grid1, MyCubeGrid grid2)
        {
            CheckGridCokpit(grid1);
            CheckGridCokpit(grid2);
        }

        bool HasCockpit(MyCubeGrid grid)
        {
            return grid.CubeBlocks.Contains(this.SlimBlock);
        }

        void CheckGridCokpit(MyCubeGrid grid)
        {
            if (HasCockpit(grid) == false)
            {
                if (grid.IsMainCockpit(this) && CubeGrid != grid)
                {
                    grid.SetMainCockpit(null);
                }
            }
        }

        public MyEntityCameraSettings GetCameraEntitySettings()
        {
            return null;
        }

        public MyStringId ControlContext
        {
            get { return MySpaceBindingCreator.CX_SPACESHIP; }
        }
    }
}

