#region Using

using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;
using PlayerId = Sandbox.Game.World.MyPlayer.PlayerId;
using Sandbox.Game.EntityComponents;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Replication;
using VRage.Network;
using Sandbox.Game.GameSystems.CoordinateSystem;
using VRage.Game.Components;
using VRage.Game.Models;
using VRage.Game.Entity;
using VRage.Game;
using VRage.OpenVRWrapper;
using Sandbox.Game.Audio;
using Sandbox.Game.Entities.Cube.CubeBuilder;
using Sandbox.Game.GameSystems.ContextHandling;
using VRage.Audio;
using VRage.Game.Components.Session;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;
using VRage.Profiler;

#endregion

namespace Sandbox.Game.Entities
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    [StaticEventOwner]
    public partial class MyCubeBuilder : MyBlockBuilderBase, IMyFocusHolder
    {
        public override Type[] Dependencies
        {
            get
            {
                Type[] dependencies = new Type[base.Dependencies.Length + 1];
                for (int i = 0; i < base.Dependencies.Length; i++)
                    dependencies[i] = base.Dependencies[i];
                dependencies[dependencies.Length - 1] = typeof(MyToolbarComponent);
                return dependencies;
            }
        }

        #region Structs

        struct BuildData
        { 
            public Vector3D Position;
            public Vector3 Forward;
            public Vector3 Up;
            public bool AbsolutePosition;
        }

        [Flags]
        public enum SpawnFlags : ushort
        {
            None                          = 0,
            AddToScene                    = 1 << 0,
            CreatePhysics                 = 1 << 1,
            EnableSmallTolargeConnections = 1 << 2,
            SpawnAsMaster                 = 1 << 3,

            Default                       = AddToScene | CreatePhysics | EnableSmallTolargeConnections
        }

        struct Author
        {
            public long EntityId;
            public long IdentityId;
            public Author(long entityId, long identityId)
            {
                EntityId = entityId;
                IdentityId = identityId;
            }
        }

        #endregion

        #region Enums

        public enum BuildingModeEnum
        {
            SingleBlock,
            Line,
            Plane
        }

        #endregion

        #region Fields

        #region Static

        public static MyCubeBuilder Static;

        protected static float BLOCK_ROTATION_SPEED = 0.002f;

        static MyColoringArea[] m_currColoringArea = new MyColoringArea[8];

        static List<Vector3I> m_cacheGridIntersections = new List<Vector3I>();

        public static MyBuildComponentBase BuildComponent { get; set; }

        private static MyHudNotification BlockRotationHint;
        private static MyHudNotification ColorHint;
        private static MyHudNotification BuildingHint;
        private static MyHudNotification UnlimitedBuildingHint;
        private static MyHudNotification CompoundModeHint;
        private static MyHudNotification DynamicModeHint;

        private static MyHudNotification JoystickRotationHint;
        private static MyHudNotification JoystickBuildingHint;
        private static MyHudNotification JoystickColorHint;
        private static MyHudNotification JoystickUnlimitedBuildingHint;
        private static MyHudNotification JoystickCompoundModeHint;
        private static MyHudNotification JoystickDynamicModeHint;

        private static int m_cycle = 0;

        public static Dictionary<PlayerId, List<Vector3>> AllPlayersColors = null;

        #endregion

        //public override bool IsRequiredByGame
        //{
        //    get
        //    {
        //        return base.IsRequiredByGame && MyPerGameSettings.Game == GameEnum.SE_GAME;
        //    }
        //}

        protected bool canBuild = true;

        struct MyColoringArea
        {
            public Vector3I Start;
            public Vector3I End;
        }

        List<Vector3D> m_collisionTestPoints = new List<Vector3D>(12);

        private int m_lastInputHandleTime;

        private bool m_alignToDefault = true;
        private bool m_customRotation = false;
        //private int m_lastDefault = 0;

        private float m_animationSpeed = 0.1f;
        private bool m_animationLock = false;

        private bool m_stationPlacement = false;

        protected MyBlockBuilderRotationHints m_rotationHints = new MyBlockBuilderRotationHints();
        protected MyBlockBuilderRenderData m_renderData = new MyBlockBuilderRenderData();


        public bool CompoundEnabled { get; protected set; }


        private bool m_blockCreationActivated;
        public bool BlockCreationIsActivated
        {
            get { return m_blockCreationActivated; }
            private set { m_blockCreationActivated = value; }
        }

        public override bool IsActivated
        {
            get { return BlockCreationIsActivated; }
            }

        bool m_useSymmetry = false;
        public bool UseSymmetry
        {
            get { return m_useSymmetry && (MySession.Static != null && (MySession.Static.CreativeMode || MySession.Static.CreativeToolsEnabled(Sync.MyId))) && !(MySession.Static.ControlledEntity is MyShipController); }
            set
            {
                if (m_useSymmetry != value)
                {
                    m_useSymmetry = value;
                    MySandboxGame.Config.CubeBuilderUseSymmetry = value;
                    MySandboxGame.Config.Save();
                }
            }
        }

        private bool m_useTransparency = true;
        public bool UseTransparency
        {
            get { return m_useTransparency; }
            set
            {
                if (m_useTransparency != value)
                {
                    m_useTransparency = value;
                    m_renderData.ClearInstanceData();
                    m_rotationHints.Clear();

                    m_renderData.UpdateRenderInstanceData();
                    if (CurrentGrid != null)
                    {
                        m_renderData.UpdateRenderEntitiesData(CurrentGrid.WorldMatrix, UseTransparency, CurrentGrid.GridScale);
                    }
                }
            }
        }

        public bool FreezeGizmo { get; set; }

        public bool ShowRemoveGizmo { get; set; }
        public Vector3? MaxGridDistanceFrom = null;
        private bool AllowFreeSpacePlacement = true;
        private float FreeSpacePlacementDistance = 20;
        private StringBuilder m_cubeCountStringBuilder = new StringBuilder(10);

        const int MAX_CUBES_BUILT_AT_ONCE = 2048;
        const int MAX_CUBES_BUILT_IN_ONE_AXIS = 255;
        const float CONTINUE_BUILDING_VIEW_ANGLE_CHANGE_THRESHOLD = 0.998f;
        const float CONTINUE_BUILDING_VIEW_POINT_CHANGE_THRESHOLD = 0.25f;

        protected MyCubeBuilderGizmo m_gizmo;

        MySymmetrySettingModeEnum m_symmetrySettingMode = MySymmetrySettingModeEnum.NoPlane;

        Vector3D m_initialIntersectionStart;
        Vector3D m_initialIntersectionDirection;

        protected MyCubeBuilderState m_cubeBuildlerState;

        protected MyCoordinateSystem.CoordSystemData m_lastLocalCoordSysData;

        public MyCubeBuilderState CubeBuilderState 
        {
            get
            {
                return m_cubeBuildlerState; 
            }
        }

        protected internal override MyCubeGrid CurrentGrid
        {
            get { return m_currentGrid; }
            protected set
            {
                if (FreezeGizmo)
                    return;

                if (m_currentGrid != value)
                {
                    BeforeCurrentGridChange(value);
                    m_currentGrid = value;
                    m_customRotation = false;

                    //Change block size if diferent size grid selected
                    if (IsCubeSizeModesAvailable && CurrentBlockDefinition != null && m_currentGrid != null)
                    {
                        var blockDefGroup = MyDefinitionManager.Static.GetDefinitionGroup(CurrentBlockDefinition.BlockPairName);
                        int currDefinitionIndex = m_cubeBuildlerState.CurrentBlockDefinitionStages.IndexOf(CurrentBlockDefinition);
                        MyCubeSize newCubeSize = m_currentGrid.GridSizeEnum;
                        if (newCubeSize != CurrentBlockDefinition.CubeSize)
                        {
                            if ((newCubeSize == MyCubeSize.Small && blockDefGroup.Small != null) || (newCubeSize == MyCubeSize.Large && blockDefGroup.Large != null))
                            {
                                m_cubeBuildlerState.SetCubeSize(newCubeSize);
                                SetSurvivalIntersectionDist();
                                if (currDefinitionIndex != -1 && m_cubeBuildlerState.CurrentBlockDefinitionStages.Count > 0)
                                {
                                    UpdateCubeBlockStageDefinition(m_cubeBuildlerState.CurrentBlockDefinitionStages[currDefinitionIndex]);
                                }
                            }
                        }
                    }

                    if (m_currentGrid == null)
                    {
                        RemoveSymmetryNotification();

                        m_gizmo.Clear();
                    }
                }
            }
        }

        protected internal override MyVoxelBase CurrentVoxelBase
        {
            get
            {
                //if (MyFakes.ENABLE_BLOCK_PLACEMENT_ON_VOXEL) // TODO: check this fake?
                    return m_currentVoxelBase;
            }

            protected set
            {
                if (FreezeGizmo)
                    return;

                if (m_currentVoxelBase != value)
                {
                    m_currentVoxelBase = value;
                    //UpdateNotificationBlockNotAvailable();

                    if (m_currentVoxelBase == null)
                    {
                        RemoveSymmetryNotification();

                        m_gizmo.Clear();
                    }
                }
            }
        }

        protected override MyCubeBlockDefinition CurrentBlockDefinition
        {
            get { return m_cubeBuildlerState.CurrentBlockDefinition; }
            set
            {
                m_cubeBuildlerState.CurrentBlockDefinition = value;
                //UpdateNotificationBlockNotAvailable();
            }
        }

        /// <summary>
        /// Current block definition for toolbar.
        /// </summary>
        public MyCubeBlockDefinition ToolbarBlockDefinition
        {
            get
            {
                if (m_cubeBuildlerState == null)
                    return null;

                if (MyFakes.ENABLE_BLOCK_STAGES)
                {
                    if (m_cubeBuildlerState.CurrentBlockDefinitionStages.Count > 0)
                        return m_cubeBuildlerState.CurrentBlockDefinitionStages[0];
                }
                return CurrentBlockDefinition;
            }
        }

        MyHudNotification m_blockNotAvailableNotification;
        MyHudNotification m_symmetryNotification;

        private bool m_dynamicMode;

        private bool m_isBuildMode = false;

        private MyHudNotification m_buildModeHint;

        #endregion

        #region Properties

        public static BuildingModeEnum BuildingMode
        {
            get
            {
                int val = MySandboxGame.Config.CubeBuilderBuildingMode;
                if (!Enum.IsDefined(typeof(BuildingModeEnum), val))
                    val = 0;
                return (BuildingModeEnum)val;
            }
            set
            {
                MySandboxGame.Config.CubeBuilderBuildingMode = (int)value;
            }
        }

        ///// <summary>
        ///// Defines default block building distance set on start building.
        ///// </summary>
        //protected virtual float DefaultBlockBuildingDistance
        //{
        //    get { return DEFAULT_BLOCK_BUILDING_DISTANCE; }
        //}

        ///// <summary>
        ///// Indicates if cube builder cube size modes feature is avaliable.
        ///// </summary>
        public virtual bool IsCubeSizeModesAvailable
        {
            get { return true; }
        }

        public bool IsBuildMode
        {
            get { return m_isBuildMode; }
            set 
            { 
                m_isBuildMode = value;
                MyHud.IsBuildMode = value;
                if (value)
                    ActivateBuildModeNotifications(MyInput.Static.IsJoystickConnected() && MyFakes.ENABLE_CONTROLLER_HINTS);
                else
                    DeactivateBuildModeNotifications();
            }
        }

        public bool DynamicMode
        {
            get
            {
                return m_dynamicMode;
        }

            set
        {
                m_dynamicMode = value;
        }
        }

        #endregion

        #region Constructor

		static MyCubeBuilder()
		{
			if(Sync.IsServer)
				AllPlayersColors = new Dictionary<PlayerId, List<Vector3>>();
		}

        public MyCubeBuilder()
        {
            m_gizmo = new MyCubeBuilderGizmo();
            InitializeNotifications();
        }

        #endregion

        #region Load data

        public override void InitFromDefinition(MySessionComponentDefinition definition)
        {
            base.InitFromDefinition(definition);
        }

        public override void LoadData()
        {
            base.LoadData();
            m_cubeBuildlerState = new MyCubeBuilderState();
            Static = this;
            MyCubeGrid.ShowStructuralIntegrity = false;
        }

        #endregion

        #region Control

        protected bool GridValid
        {
            get { return BlockCreationIsActivated && CurrentGrid != null; }
        }

        protected bool GridAndBlockValid
        {
            get
            {
                return GridValid && CurrentBlockDefinition != null && (CurrentBlockDefinition.CubeSize == CurrentGrid.GridSizeEnum
                    || PlacingSmallGridOnLargeStatic);
            }
        }

        protected bool VoxelMapAndBlockValid
        {
            get
            {
                return BlockCreationIsActivated && CurrentVoxelBase != null && CurrentBlockDefinition != null;
            }
        }

        public bool PlacingSmallGridOnLargeStatic
        {
            get
            {
                return MyFakes.ENABLE_STATIC_SMALL_GRID_ON_LARGE && GridValid && CurrentBlockDefinition != null && CurrentBlockDefinition.CubeSize == MyCubeSize.Small
                    && CurrentGrid.GridSizeEnum == MyCubeSize.Large && CurrentGrid.IsStatic;
            }
        }

        protected bool BuildInputValid
        {
            get { return GridAndBlockValid || VoxelMapAndBlockValid || DynamicMode; }
        }

        private float CurrentBlockScale
        {
            get
            {
                if (CurrentBlockDefinition == null)
                    return 1f;

                return MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize) / MyDefinitionManager.Static.GetCubeSizeOriginal(CurrentBlockDefinition.CubeSize);
            }
        }

        protected virtual void RotateAxis(int index, int sign, float angleDelta, bool newlyPressed)
        {
            if (DynamicMode)
            {
                MatrixD currentMatrix = m_gizmo.SpaceDefault.m_worldMatrixAdd;
                MatrixD rotatedMatrix;

                if (!CalculateBlockRotation(index, sign, ref currentMatrix, out rotatedMatrix, angleDelta, MyBlockDirection.Both, MyBlockRotation.Both))
                    return;

                m_gizmo.SpaceDefault.m_worldMatrixAdd = rotatedMatrix;
            }
            else
            {
                // Rotate gizmos only on a key press
                if (!newlyPressed)
                    return;

                angleDelta = (float)Math.PI / 2;
                MatrixD currentMatrix = m_gizmo.SpaceDefault.m_localMatrixAdd;
                MatrixD rotatedMatrix;

                if (!CalculateBlockRotation(index, sign, ref currentMatrix, out rotatedMatrix, angleDelta, CurrentBlockDefinition != null ? CurrentBlockDefinition.Direction : MyBlockDirection.Both,
                    CurrentBlockDefinition != null ? CurrentBlockDefinition.Rotation : MyBlockRotation.Both))
                    return;

                if (m_gizmo.RotationOptions != MyRotationOptionsEnum.None)
                    MyGuiAudio.PlaySound(MyGuiSounds.HudRotateBlock);

                m_gizmo.RotateAxis(ref rotatedMatrix);
            }
        }

        public static bool CalculateBlockRotation(int index, int sign, ref MatrixD currentMatrix, out MatrixD rotatedMatrix, float angle,
            MyBlockDirection blockDirection = MyBlockDirection.Both, MyBlockRotation blockRotation = MyBlockRotation.Both)
        {
            Matrix rotation = Matrix.Identity;

            //because Z axis is negative
            if (index == 2)
                sign *= -1;

            Vector3D tmpRotationAnimation = Vector3D.Zero;

            switch (index)
            {
                case 0:   //X
                    tmpRotationAnimation.X += sign * angle;
                    rotation = Matrix.CreateFromAxisAngle(currentMatrix.Right, sign * angle);
                    break;

                case 1: //Y
                    tmpRotationAnimation.Y += sign * angle;
                    rotation = Matrix.CreateFromAxisAngle(currentMatrix.Up, sign * angle);
                    break;

                case 2: //Z
                    tmpRotationAnimation.Z += sign * angle;
                    rotation = Matrix.CreateFromAxisAngle(currentMatrix.Forward, sign * angle);
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }

            rotatedMatrix = currentMatrix;
            rotatedMatrix *= rotation;

            bool isValid = CheckValidBlockRotation(rotatedMatrix, blockDirection, blockRotation);
            if (isValid && MySandboxGame.Config.AnimatedRotation)
                if (!Static.DynamicMode)
                    if (!Static.m_animationLock)
                        Static.m_animationLock = true;
                    else
                        isValid = !isValid;

            return isValid;
        }

        private void ActivateBlockCreation(MyDefinitionId? blockDefinitionId = null)
        {
            if (MySession.Static.CameraController == null || !MySession.Static.CameraController.AllowCubeBuilding)
            {
            //    return;
            }

            if (MySession.Static.ControlledEntity is MyShipController && (MySession.Static.ControlledEntity as MyShipController).BuildingMode == false)
                return;

            //Change block size if same block selected
            if (IsCubeSizeModesAvailable && blockDefinitionId.HasValue && CurrentBlockDefinition != null)
            {
                var tmpDef = MyDefinitionManager.Static.GetCubeBlockDefinition(blockDefinitionId.Value);
                var blockDefGroup = MyDefinitionManager.Static.GetDefinitionGroup(tmpDef.BlockPairName);
                if ((CurrentBlockDefinition.CubeSize == MyCubeSize.Large && blockDefGroup.Small != null) || (CurrentBlockDefinition.CubeSize == MyCubeSize.Small && blockDefGroup.Large != null))
                {
                    int currDefinitionIndex = m_cubeBuildlerState.CurrentBlockDefinitionStages.IndexOf(CurrentBlockDefinition);
                    MyCubeSize newCubeSize = m_cubeBuildlerState.CubeSizeMode == MyCubeSize.Large ? MyCubeSize.Small : MyCubeSize.Large;
                    m_cubeBuildlerState.SetCubeSize(newCubeSize);
                    SetSurvivalIntersectionDist();
                    if (currDefinitionIndex != -1 && m_cubeBuildlerState.CurrentBlockDefinitionStages.Count > 0)
                    {
                        UpdateCubeBlockStageDefinition(m_cubeBuildlerState.CurrentBlockDefinitionStages[currDefinitionIndex]);
                    }
                }
                else
                {
                    UpdateNotificationBlockNotAvailable();
                }
            }
            else if (CurrentBlockDefinition == null && blockDefinitionId.HasValue)
            {
                var tmpDef = MyDefinitionManager.Static.GetCubeBlockDefinition(blockDefinitionId.Value);
                var newCubeSize = m_cubeBuildlerState.CubeSizeMode;
                if (tmpDef.CubeSize != newCubeSize)
                {
                    var otherblock = tmpDef.CubeSize == MyCubeSize.Large ? MyDefinitionManager.Static.GetDefinitionGroup(tmpDef.BlockPairName).Small : MyDefinitionManager.Static.GetDefinitionGroup(tmpDef.BlockPairName).Large;
                    if (otherblock == null)
                    {
                        newCubeSize = tmpDef.CubeSize;
                    }
                }
                m_cubeBuildlerState.SetCubeSize(newCubeSize);
            }


            UpdateCubeBlockDefinition(blockDefinitionId);

            SetSurvivalIntersectionDist();

            if (MySession.Static.CreativeMode)
            {
                AllowFreeSpacePlacement = false;
                MaxGridDistanceFrom = null;
                ShowRemoveGizmo = MyFakes.SHOW_REMOVE_GIZMO;
            }
            else
            {
                AllowFreeSpacePlacement = false;
                ShowRemoveGizmo = true;
            }

            ActivateNotifications();

            if (!(MySession.Static.ControlledEntity is MyShipController) || (MySession.Static.ControlledEntity as MyShipController).BuildingMode == false) MyHud.Crosshair.ResetToDefault();

           // MyCubeBuilder.Static.UpdateNotificationBlockNotAvailable();

            BlockCreationIsActivated = true;//!MultiBlockCreationIsActivated;

            this.AlignToGravity();

        }

        public void DeactivateBlockCreation()
        {
            if (m_cubeBuildlerState.CurrentBlockDefinition != null)
            {
                m_cubeBuildlerState.UpdateCubeBlockDefinition(m_cubeBuildlerState.CurrentBlockDefinition.Id, m_gizmo.SpaceDefault.m_localMatrixAdd);
            }

            BlockCreationIsActivated = false;
            DeactivateNotifications();
            //MyCubeBuilder.Static.UpdateNotificationBlockNotAvailable();
        }

        private void ActivateNotifications()
        {
            if (MyInput.Static.IsJoystickConnected() && MyFakes.ENABLE_CONTROLLER_HINTS)
            {
                if (!IsBuildMode)
                    MyHud.Notifications.Add(m_buildModeHint);
                if (MySession.Static.CreativeMode)
                    MyHud.Notifications.Add(JoystickUnlimitedBuildingHint);
                else
                    MyHud.Notifications.Add(JoystickBuildingHint);
            }
            else
            {
                if (MySession.Static.CreativeMode)
                    MyHud.Notifications.Add(UnlimitedBuildingHint);
                else
                    MyHud.Notifications.Add(BuildingHint);

                ActivateBuildModeNotifications(false);
                if (MyFakes.ENABLE_BLOCK_COLORING)
                    MyHud.Notifications.Add(ColorHint);
            }
        }

        private void DeactivateNotifications()
        {
            MyHud.Notifications.Remove(m_buildModeHint);
            if (MySession.Static.CreativeMode)
            {
                MyHud.Notifications.Remove(UnlimitedBuildingHint);
                MyHud.Notifications.Remove(JoystickUnlimitedBuildingHint);
            }
            else
            {
                MyHud.Notifications.Remove(BuildingHint);
                MyHud.Notifications.Remove(JoystickBuildingHint);
            }

            if (MyFakes.ENABLE_BLOCK_COLORING)
                MyHud.Notifications.Remove(ColorHint);

            DeactivateBuildModeNotifications();
        }

        /// <summary>
        /// Allows to override normal behaviour of Cube builder.
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsDynamicOverride()
        {
            if (m_cubeBuildlerState.CurrentBlockDefinition == null || CurrentGrid == null)
                return false;

            return m_cubeBuildlerState.CurrentBlockDefinition.CubeSize == MyCubeSize.Small && CurrentGrid.GridSizeEnum == MyCubeSize.Large;
        }

        private void ActivateBuildModeNotifications(bool joystick)
        {
            if (joystick)
            {
                MyHud.Notifications.Remove(m_buildModeHint);
                MyHud.Notifications.Add(JoystickRotationHint);
                MyHud.Notifications.Add(JoystickColorHint);
                if (MyFakes.ENABLE_COMPOUND_BLOCKS)
                    MyHud.Notifications.Add(JoystickCompoundModeHint);
                
                    MyHud.Notifications.Add(JoystickDynamicModeHint);
            }
            else
            {
                MyHud.Notifications.Add(BlockRotationHint);
                if (MyFakes.ENABLE_COMPOUND_BLOCKS)
                    MyHud.Notifications.Add(CompoundModeHint);
                
                //    MyHud.Notifications.Add(DynamicModeHint);
            }
        }

        private void DeactivateBuildModeNotifications()
        {
            if (MyInput.Static.IsJoystickConnected() && IsActivated)
                MyHud.Notifications.Add(m_buildModeHint);

            MyHud.Notifications.Remove(BlockRotationHint);
            MyHud.Notifications.Remove(JoystickRotationHint);
            MyHud.Notifications.Remove(JoystickColorHint);

            if (MyFakes.ENABLE_COMPOUND_BLOCKS)
            {
                MyHud.Notifications.Remove(CompoundModeHint);
                MyHud.Notifications.Remove(JoystickCompoundModeHint);
            }

                //MyHud.Notifications.Remove(DynamicModeHint);
                MyHud.Notifications.Remove(JoystickDynamicModeHint);
            
            }

        private void InitializeNotifications()
        {
            // keyboard mouse notifications
            {
                var next = MyInput.Static.GetGameControl(MyControlsSpace.SWITCH_LEFT);
                var prev = MyInput.Static.GetGameControl(MyControlsSpace.SWITCH_RIGHT);
                var compoundToggle = MyInput.Static.GetGameControl(MyControlsSpace.SWITCH_COMPOUND);
              //  var buildingModeToggle = MyInput.Static.GetGameControl(MyControlsSpace.SWITCH_BUILDING_MODE);
                var build = MyInput.Static.GetGameControl(MyControlsSpace.PRIMARY_TOOL_ACTION);
                var rotxp = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE);
                var rotxn = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE);
                var rotyp = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE);
                var rotyn = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE);
                var rotzp = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE);
                var rotzn = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE);

                // This will combine controls which has name
                var controlHelper = new MyHudNotifications.ControlsHelper(rotxp, rotxn, rotzp, rotzn, rotyp, rotyn);

                BlockRotationHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationRotationFormatCombined, controlHelper);
                ColorHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationColorFormat, next, prev, "MMB", "CTRL", "SHIFT");
                BuildingHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationBuildingFormat, build);
                UnlimitedBuildingHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationUnlimitedBuildingFormat, "LMB", "RMB", "CTRL");
                if (MyFakes.ENABLE_COMPOUND_BLOCKS)
                    CompoundModeHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationCompoundBuildingFormat, compoundToggle, "ALT");
                
                //    DynamicModeHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationSwitchBuildingModeFormat, buildingModeToggle);
                m_buildModeHint = null;
            }

            // joystick notifications
            {
                var cx_char = MySpaceBindingCreator.CX_CHARACTER;
                var cx_build = MySpaceBindingCreator.CX_BUILD_MODE;

                var primaryActionCode = MyControllerHelper.GetCodeForControl(cx_char, MyControlsSpace.PRIMARY_TOOL_ACTION);
                var secondaryActionCode = MyControllerHelper.GetCodeForControl(cx_char, MyControlsSpace.SECONDARY_TOOL_ACTION);
                var rotateBlockCode1 = MyControllerHelper.GetCodeForControl(cx_build, MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE);
                var rotateBlockCode2 = MyControllerHelper.GetCodeForControl(cx_build, MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE);
                var rotateBlockCode3 = MyControllerHelper.GetCodeForControl(cx_build, MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE);
                var rotateBlockCode4 = MyControllerHelper.GetCodeForControl(cx_build, MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE);
                var rotateBlockRollCode = MyControllerHelper.GetCodeForControl(cx_build, MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE);
                var rotateBlockRollCode2 = MyControllerHelper.GetCodeForControl(cx_build, MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE);
                var dynamicModeCode = MyControllerHelper.GetCodeForControl(cx_build, MyControlsSpace.SWITCH_BUILDING_MODE);
                var compoundCode = MyControllerHelper.GetCodeForControl(cx_build, MyControlsSpace.SWITCH_COMPOUND);
                var buildModeCode = MyControllerHelper.GetCodeForControl(cx_char, MyControlsSpace.BUILD_MODE);
                var colorCode = MyControllerHelper.GetCodeForControl(cx_build, MyControlsSpace.CUBE_COLOR_CHANGE);

                StringBuilder sb = new StringBuilder();
                var rotation = new HashSet<char>() { rotateBlockCode1, rotateBlockCode2, rotateBlockCode3, rotateBlockCode4, rotateBlockRollCode, rotateBlockRollCode2 };
                foreach (var c in rotation)
                    sb.Append(c);

                JoystickRotationHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationRotationFormatCombined, sb.ToString().Trim());
                JoystickColorHint = MyHudNotifications.CreateControlNotification(MySpaceTexts.NotificationJoystickColorFormat, colorCode);
                JoystickBuildingHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationBuildingFormat, primaryActionCode);
                JoystickUnlimitedBuildingHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationJoystickUnlimitedBuildingFormat, primaryActionCode, secondaryActionCode);
                if (MyFakes.ENABLE_COMPOUND_BLOCKS)
                    JoystickCompoundModeHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationJoystickCompoundBuildingFormat, compoundCode);
                
                    JoystickDynamicModeHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationSwitchBuildingModeFormat, dynamicModeCode);
                m_buildModeHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationHintPressToOpenBuildMode, buildModeCode);
            }
        }

        public override void Deactivate()
        {
            DeactivateBlockCreation();

            CurrentBlockDefinition = null;

            m_stationPlacement = false;
            CurrentGrid = null;
            CurrentVoxelBase = null;
            IsBuildMode = false;

            DynamicMode = false;
            
            //VR:TODO:check if this didnt break something
            PlacementProvider = null;
            m_rotationHints.ReleaseRenderData();

            MyCoordinateSystem.Static.Visible = false;

        }

        public void OnLostFocus()
        {
            this.Deactivate();
        }

        public override void Activate(MyDefinitionId? blockDefinitionId = null)
        {
            if (MySession.Static.CameraController != null)
            {
                MySession.Static.GameFocusManager.Register(this);
            }

            this.ActivateBlockCreation(blockDefinitionId);

        }

        protected virtual void UpdateCubeBlockStageDefinition(MyCubeBlockDefinition stageCubeBlockDefinition)
        {
            Debug.Assert(stageCubeBlockDefinition != null);

            if (CurrentBlockDefinition != null && stageCubeBlockDefinition != null)
            {
                Quaternion rotation = Quaternion.CreateFromRotationMatrix(m_gizmo.SpaceDefault.m_localMatrixAdd);
                m_cubeBuildlerState.RotationsByDefinitionHash[CurrentBlockDefinition.Id] = rotation;
            }

            CurrentBlockDefinition = stageCubeBlockDefinition;

            m_gizmo.RotationOptions = MyCubeGridDefinitions.GetCubeRotationOptions(CurrentBlockDefinition);

            Quaternion lastRot;
            if (m_cubeBuildlerState.RotationsByDefinitionHash.TryGetValue(stageCubeBlockDefinition.Id, out lastRot))
                m_gizmo.SpaceDefault.m_localMatrixAdd = Matrix.CreateFromQuaternion(lastRot);
            else
                m_gizmo.SpaceDefault.m_localMatrixAdd = Matrix.Identity;
        }

        protected virtual void UpdateCubeBlockDefinition(MyDefinitionId? id)
        {

            m_cubeBuildlerState.UpdateCubeBlockDefinition(id, m_gizmo.SpaceDefault.m_localMatrixAdd);

            if (CurrentBlockDefinition != null && IsCubeSizeModesAvailable)
                m_cubeBuildlerState.UpdateComplementBlock();

            m_cubeBuildlerState.UpdateBlockDefinitionStages(id);

            if (m_cubeBuildlerState.CurrentBlockDefinition == null)
                return;

            m_gizmo.RotationOptions = MyCubeGridDefinitions.GetCubeRotationOptions(CurrentBlockDefinition);
            Quaternion lastRot;

            MyDefinitionId defBlockId = id.HasValue ? id.Value : new MyDefinitionId();

            if (m_cubeBuildlerState.RotationsByDefinitionHash.TryGetValue(defBlockId, out lastRot))
                m_gizmo.SpaceDefault.m_localMatrixAdd = Matrix.CreateFromQuaternion(lastRot);
            else
                m_gizmo.SpaceDefault.m_localMatrixAdd = Matrix.Identity;

        }

        #endregion

        #region Render gizmo

        public void AddFastBuildModels(MatrixD baseMatrix, ref Matrix localMatrixAdd, List<MatrixD> matrices, List<string> models, MyCubeBlockDefinition definition,
            Vector3I? startBuild, Vector3I? continueBuild)
        {
            AddFastBuildModelWithSubparts(ref baseMatrix, matrices, models, definition, CurrentBlockScale);

            // Can be CurrentBlockDefinition != definition?
            if (CurrentBlockDefinition != null && startBuild != null && continueBuild != null)
            {
                Vector3I rotatedSize;
                Vector3I.TransformNormal(ref CurrentBlockDefinition.Size, ref localMatrixAdd, out rotatedSize);
                rotatedSize = Vector3I.Abs(rotatedSize);

                Vector3I stepDelta;
                Vector3I counter;
                int stepCount;

                ComputeSteps(startBuild.Value, continueBuild.Value, rotatedSize, out stepDelta, out counter, out stepCount);

                Vector3I offset = Vector3I.Zero;
                for (int i = 0; i < counter.X; i += 1, offset.X += stepDelta.X)
                {
                    offset.Y = 0;
                    for (int j = 0; j < counter.Y; j += 1, offset.Y += stepDelta.Y)
                    {
                        offset.Z = 0;
                        for (int k = 0; k < counter.Z; k += 1, offset.Z += stepDelta.Z)
                        {
                            Vector3I pos = offset;
                            Vector3 offsetFloat;
                            if (CurrentGrid != null)
                            {
                                offsetFloat = Vector3.Transform(pos * CurrentGrid.GridSize, CurrentGrid.WorldMatrix.GetOrientation());
                            }
                            else
                            {
                                float gridSize = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize);
                                offsetFloat = pos * gridSize;
                            }

                            var matrix = baseMatrix;
                            matrix.Translation += offsetFloat;

                            AddFastBuildModelWithSubparts(ref matrix, matrices, models, definition, CurrentBlockScale);
                        }
                    }
                }
            }
        }

        void AddFastBuildModels(MyCubeBuilderGizmo.MyGizmoSpaceProperties gizmoSpace, MatrixD baseMatrix, List<MatrixD> matrices, List<string> models, MyCubeBlockDefinition definition)
        {
            AddFastBuildModels(baseMatrix, ref gizmoSpace.m_localMatrixAdd, matrices, models, definition, gizmoSpace.m_startBuild, gizmoSpace.m_continueBuild);
        }

        public void AlignToGravity(bool alignToCamera = true)
        {
         
            Vector3 gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(IntersectionStart);
            if (gravity.LengthSquared() < Double.Epsilon && MyPerGameSettings.Game == GameEnum.ME_GAME)
            {
                gravity = Vector3.Down;
            }

            if (gravity.LengthSquared() > 0)
            {
                Matrix oldTransform = m_gizmo.SpaceDefault.m_worldMatrixAdd;
                gravity.Normalize();

                Vector3D newForward;

                if (MySector.MainCamera != null && alignToCamera)
                {
                    newForward = Vector3D.Reject(MySector.MainCamera.ForwardVector, gravity);
                }
                else
                {
                    newForward = Vector3D.Reject(m_gizmo.SpaceDefault.m_worldMatrixAdd.Forward, gravity);
                }

                if (!newForward.IsValid() || newForward.LengthSquared() <= double.Epsilon)
                {
                    newForward = Vector3D.CalculatePerpendicularVector(gravity);
                }

                newForward.Normalize();
                m_gizmo.SpaceDefault.m_worldMatrixAdd = Matrix.CreateWorld(oldTransform.Translation, newForward, -gravity);

            }

        }

        public virtual bool HandleGameInput()
        {
            if (this.HandleExportInput())
                return true;

            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.LANDING_GEAR) && MySession.Static.ControlledEntity == MySession.Static.LocalCharacter && MySession.Static.LocalHumanPlayer != null && MySession.Static.LocalHumanPlayer.Identity.Character == MySession.Static.ControlledEntity)
            {
                if (!MyInput.Static.IsAnyShiftKeyPressed() && MyGuiScreenGamePlay.ActiveGameplayScreen == null)
                {
                    MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                    MyGuiSandbox.AddScreen(MyGuiScreenGamePlay.ActiveGameplayScreen = new MyGuiScreenColorPicker());
                }
            }

            if (!IsActivated)  // do not consume input when not active
                return false;

            int frameDt = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastInputHandleTime;
            m_lastInputHandleTime += frameDt;

            bool disallowCockpitBuilding = MySession.Static.ControlledEntity is MyCockpit && !SpectatorIsBuilding;
            if (disallowCockpitBuilding && MySession.Static.ControlledEntity is MyCockpit && (MySession.Static.ControlledEntity as MyCockpit).BuildingMode) disallowCockpitBuilding = false;

            // Don't allow cube builder when paused or when in cockpit and not in developer spectator mode
            if (MySandboxGame.IsPaused || disallowCockpitBuilding)
                return false;

            if (MyInput.Static.IsNewLeftMousePressed() && MySession.Static.ControlledEntity is MyCockpit && (MySession.Static.ControlledEntity as MyCockpit).BuildingMode && MySession.Static.SurvivalMode)
                MySession.Static.LocalCharacter.BeginShoot(MyShootActionEnum.PrimaryAction);

            if (this.HandleDevInput())
                return true;

            

            var context = (IsActivated && MySession.Static.ControlledEntity is MyCharacter) ? MySession.Static.ControlledEntity.ControlContext : MyStringId.NullOrEmpty;

            if (IsActivated && MyControllerHelper.IsControl(context, MyControlsSpace.BUILD_MODE))
            {
                IsBuildMode = !IsBuildMode;
            }

            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.FREE_ROTATION)) //TODO: do something with it
            {
                this.AlignToGravity();
            }

            if (this.HandleAdminAndCreativeInput(context))
                return true;

            // Color Picker
            if (CurrentGrid != null && MyInput.Static.IsNewGameControlPressed(MyControlsSpace.LANDING_GEAR))
            {
                if (MyInput.Static.IsAnyShiftKeyPressed())
                {
                    foreach (var gizmoSpace in m_gizmo.Spaces)
                    {
                        if (gizmoSpace.m_removeBlock != null && MySession.Static.LocalHumanPlayer != null)
                            MySession.Static.LocalHumanPlayer.ChangeOrSwitchToColor(gizmoSpace.m_removeBlock.ColorMaskHSV);
                    }
                }
            }

            if (CurrentGrid != null && MyControllerHelper.IsControl(context, MyControlsSpace.CUBE_COLOR_CHANGE, MyControlStateType.PRESSED))
            {
                int expand = 0;

                //If Ctrl + Shift + Middle mouse button are pressed, recolor a huge area of blocks
                //THANK YOU PETR!
                
                if (MyInput.Static.IsAnyCtrlKeyPressed() && MyInput.Static.IsAnyShiftKeyPressed())
                {
                    expand = -1;
                }
                else
                {
                    // If only Ctrl + Middle mouse button are pressed, recolor a tiny block area

                    if (MyInput.Static.IsAnyCtrlKeyPressed())
                        expand =  1;
                    else
                        // If only Shift + Middle mouse button are pressed, recolor a medium block area

                        if (MyInput.Static.IsAnyShiftKeyPressed())
                            expand = 3;
                }

                Change(expand);
            }

            if (this.HandleRotationInput(context, frameDt))
                return true;

			var humanPlayer = MySession.Static.LocalHumanPlayer;
            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.SWITCH_LEFT))
            {
				if (IsActivated && (CurrentBlockDefinition == null || MyFakes.ENABLE_BLOCK_COLORING) && humanPlayer != null)
                {
                    MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
					if (humanPlayer.SelectedBuildColorSlot - 1 < 0)
						humanPlayer.SelectedBuildColorSlot = humanPlayer.BuildColorSlots.Count-1;
					else
						humanPlayer.SelectedBuildColorSlot -= 1;
                }
            }
            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.SWITCH_RIGHT))
            {
				if (IsActivated && (CurrentBlockDefinition == null || MyFakes.ENABLE_BLOCK_COLORING) && humanPlayer != null)
                {
                    MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
					if (humanPlayer.SelectedBuildColorSlot + 1 >= humanPlayer.BuildColorSlots.Count)
						humanPlayer.SelectedBuildColorSlot = 0;
					else
						humanPlayer.SelectedBuildColorSlot += 1;
                }
            }

            if (this.HandleBlockVariantsInput(context))
                return true;

            return false;
        }

        private bool HandleBlockVariantsInput(MyStringId context)
        {
            if (MyFakes.ENABLE_BLOCK_STAGES && CurrentBlockDefinition != null && m_cubeBuildlerState.CurrentBlockDefinitionStages.Count > 0)
            {
                bool? switchForward = null;

                int currentScrollWheelVal = MyInput.Static.MouseScrollWheelValue();

                if (!MyInput.Static.IsGameControlPressed(MyControlsSpace.LOOKAROUND) && !MyInput.Static.IsAnyCtrlKeyPressed() && !MyInput.Static.IsAnyShiftKeyPressed()
                    && currentScrollWheelVal != 0)
                {

                    if (MyInput.Static.PreviousMouseScrollWheelValue() < MyInput.Static.MouseScrollWheelValue()
                        || MyControllerHelper.IsControl(context, MyControlsSpace.NEXT_BLOCK_STAGE, MyControlStateType.NEW_PRESSED))
                    {
                        switchForward = true;
                    }
                    else if (MyInput.Static.PreviousMouseScrollWheelValue() > MyInput.Static.MouseScrollWheelValue()
                        || MyControllerHelper.IsControl(context, MyControlsSpace.PREV_BLOCK_STAGE, MyControlStateType.NEW_PRESSED))
                    {
                        switchForward = false;
                    }
                }

                //if (switchForward == null && MyInput.Static.IsNewGameControlPressed(ME_SWITCH_STAGES))
                //{
                //    switchForward = true;
                //    if (MyInput.Static.IsAnyShiftKeyPressed())
                //        switchForward = false;
                //}

                if (switchForward.HasValue)
                {
                    int currDefinitionIndex = m_cubeBuildlerState.CurrentBlockDefinitionStages.IndexOf(CurrentBlockDefinition);
                    int nextIndex;

                    int increment = switchForward.Value ? 1 : -1;
                    nextIndex = currDefinitionIndex;
                    while ((nextIndex = nextIndex + increment) != currDefinitionIndex)
                    {
                        if (nextIndex >= m_cubeBuildlerState.CurrentBlockDefinitionStages.Count)
                            nextIndex = 0;
                        else if (nextIndex < 0)
                            nextIndex = m_cubeBuildlerState.CurrentBlockDefinitionStages.Count - 1;

                        if (!MySession.Static.SurvivalMode || (m_cubeBuildlerState.CurrentBlockDefinitionStages[nextIndex].AvailableInSurvival && (MyFakes.ENABLE_MULTIBLOCK_CONSTRUCTION || m_cubeBuildlerState.CurrentBlockDefinitionStages[nextIndex].MultiBlock == null)))
                            break;
                    }

                    UpdateCubeBlockStageDefinition(m_cubeBuildlerState.CurrentBlockDefinitionStages[nextIndex]);
                }
            }

            //if (IsCubeSizeModesAvailable && MyInput.Static.IsGameControlReleased(MyControlsSpace.CUBE_BUILDER_CUBESIZE_MODE))
            //{
            //    int currDefinitionIndex = m_cubeBuildlerState.CurrentBlockDefinitionStages.IndexOf(CurrentBlockDefinition);
            //    MyCubeSize newCubeSize = m_cubeBuildlerState.CubeSizeMode == MyCubeSize.Large ? MyCubeSize.Small : MyCubeSize.Large;
            //    m_cubeBuildlerState.SetCubeSize(newCubeSize);
            //    SetSurvivalIntersectionDist();
            //    if (currDefinitionIndex != -1 && m_cubeBuildlerState.CurrentBlockDefinitionStages.Count > 0)
            //    {
            //        UpdateCubeBlockStageDefinition(m_cubeBuildlerState.CurrentBlockDefinitionStages[currDefinitionIndex]);
            //    }
            //    return true;
            //}

            return false;
        }

        /// <summary>
        /// Refresh intersection distance for survival. Usable when switching grid size.
        /// </summary>
        private void SetSurvivalIntersectionDist()
        {
            if (CurrentBlockDefinition != null)
            {
                if (MySession.Static.SurvivalMode && !SpectatorIsBuilding && MySession.Static.CreativeToolsEnabled(Sync.MyId) == false)
                    if (CurrentBlockDefinition.CubeSize == MyCubeSize.Large)
                        IntersectionDistance = (float)CubeBuilderDefinition.BuildingDistLargeSurvivalCharacter;
                    else
                        IntersectionDistance = (float)CubeBuilderDefinition.BuildingDistSmallSurvivalCharacter;
            }
        }

        private bool HandleRotationInput(MyStringId context, int frameDt)
        {
            if (IsActivated)
            {
                if (MyControllerHelper.IsControl(context, MyControlsSpace.CUBE_DEFAULT_MOUNTPOINT))
                    m_alignToDefault = !m_alignToDefault;

                for (int i = 0; i < 6; ++i)
                {
                    bool standardRotation = MyControllerHelper.IsControl(context, m_rotationControls[i], MyControlStateType.PRESSED);
                    if (standardRotation)
                    {
                        if (m_alignToDefault)
                            m_customRotation = true;

                        bool newStandardPress = MyControllerHelper.IsControl(context, m_rotationControls[i], MyControlStateType.NEW_PRESSED);
                        bool newPress = newStandardPress;

                        int axis = -1;
                        int direction = m_rotationDirections[i];

                        if (MyFakes.ENABLE_STANDARD_AXES_ROTATION)
                        {
                            axis = GetStandardRotationAxisAndDirection(i, ref direction);
                        }
                        else
                        {
                            if (i < 2)
                            {
                                axis = m_rotationHints.RotationUpAxis;
                                direction *= m_rotationHints.RotationUpDirection;
                            }
                            if (i >= 2 && i < 4)
                            {
                                axis = m_rotationHints.RotationRightAxis;
                                direction *= m_rotationHints.RotationRightDirection;
                            }
                            if (i >= 4)
                            {
                                axis = m_rotationHints.RotationForwardAxis;
                                direction *= m_rotationHints.RotationForwardDirection;
                            }
                        }

                        if (axis != -1)
                        {

                            if (CurrentBlockDefinition != null && CurrentBlockDefinition.Rotation == MyBlockRotation.None)
                                return false;

                            float angleDelta = frameDt * BLOCK_ROTATION_SPEED;

                            if (MyInput.Static.IsAnyCtrlKeyPressed())
                            {
                                if (!newPress)
                                    return false;
                                angleDelta = MathHelper.PiOver2;
                            }
                            if (MyInput.Static.IsAnyAltKeyPressed())
                            {
                                if (!newPress)
                                    return false;
                                angleDelta = MathHelper.ToRadians(1);
                            }

                            RotateAxis(axis, direction, angleDelta, newPress);
                        }
            }
                }
            }

            return false;
        }

        private bool HandleDevInput()
        {
            //TODO: this code is for debug purposes. Need to remove before merge
            //if(MyInput.Static.IsNewKeyPressed(MyKeys.OemBackslash))
            //{
            //    m_gizmo.SpaceDefault.m_worldMatrixAdd = MatrixD.CreateWorld(m_gizmo.SpaceDefault.m_worldMatrixAdd.Translation, Vector3.Forward, Vector3.Up);
            //}

            //batch convert should be active in any game mode
            if (MyInput.Static.ENABLE_DEVELOPER_KEYS && MyInput.Static.IsNewKeyPressed(MyKeys.R) && MyInput.Static.IsAnyCtrlKeyPressed() && !MyInput.Static.IsAnyShiftKeyPressed() && !MyInput.Static.IsAnyMousePressed())
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                MyCubeGrid.ConvertPrefabsToObjs();
                return true;
            }

            if (MyInput.Static.ENABLE_DEVELOPER_KEYS && MyInput.Static.IsNewKeyPressed(MyKeys.T) && MyInput.Static.IsAnyCtrlKeyPressed() && !MyInput.Static.IsAnyShiftKeyPressed() && !MyInput.Static.IsAnyMousePressed() && MyPerGameSettings.EnableObjectExport)
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                MyCubeGrid.PlacePrefabsToWorld();
                return true;
            }

            return false;
        }

        private bool HandleExportInput()
        {

            if (MyInput.Static.IsNewKeyPressed(MyKeys.E) && MyInput.Static.IsAnyAltKeyPressed() && MyInput.Static.IsAnyCtrlKeyPressed() && !MyInput.Static.IsAnyShiftKeyPressed() && !MyInput.Static.IsAnyMousePressed() && MyPerGameSettings.EnableObjectExport)
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                var targetGrid = MyCubeGrid.GetTargetGrid();
                if (targetGrid != null)
                {
                    MyCubeGrid.ExportObject(targetGrid, false, true);
                }
                return true;
            }

            return false;
            }

        private bool HandleAdminAndCreativeInput(MyStringId context)
            {
            bool isAdminOrCreative = (MySession.Static.CreativeToolsEnabled(Sync.MyId) && MySession.Static.HasCreativeRights) || MySession.Static.CreativeMode;

            // When spectator active, building is instant
            if (isAdminOrCreative || (SpectatorIsBuilding && MyFinalBuildConstants.IS_OFFICIAL == false))
            {
                if (!(MySession.Static.ControlledEntity is MyShipController))
                {

                    if (HandleBlockCreationMovement(context))
                        return true;
                }

                if (DynamicMode)
                {
                        //GR: Go here when not in creative. When in survival and Admin menu is enabled will have duplicates of the same cubeblock (on the server)!
                        if (!MySession.Static.SurvivalMode && MyControllerHelper.IsControl(context, MyControlsSpace.PRIMARY_TOOL_ACTION))
                    {
                        Add();
                    }
                    
                }
                else if (CurrentGrid != null)
                {
                    this.HandleCurrentGridInput(context);
                }
                //if (CurrentGrid != null)
                //{
                //    this.HandleCurrentGridInput(context);
                //}
                //else if (DynamicMode)
                //{
                //    if (MyControllerHelper.IsControl(context, MyControlsSpace.PRIMARY_TOOL_ACTION))
                //    {
                //        Add();
                //    }
                //}
                else
                {
                    //RKTODO - creation of blocks in line or plane will be done when server function will be prepared 
                    // (need to create grid with one block - the first target and then build all other blocks in the grid)
                    //if (MyInput.Static.IsNewKeyPressed(Keys.Escape))
                    //{
                    //    if (m_gizmo.SpaceDefault.m_continueBuild != null)
                    //    {
                    //        m_gizmo.SpaceDefault.m_startBuild = null;
                    //        m_gizmo.SpaceDefault.m_startRemove = null;
                    //        m_gizmo.SpaceDefault.m_continueBuild = null;
                    //        return true;
                    //    }
                    //}

                    if (MyControllerHelper.IsControl(context, MyControlsSpace.PRIMARY_TOOL_ACTION))
                    {
                        //if (MyInput.Static.IsAnyCtrlKeyPressed() || BuildingMode != BuildingModeEnum.SingleBlock) //TODO: do something with it
                        //{
                        //    StartBuilding();
                        //}
                        //else
                        //{
                        Add();
                        //}
                    }

                    //if (MyInput.Static.IsLeftMousePressed() ||
                    //    MyInput.Static.IsRightMousePressed())
                    //{
                    //    ContinueBuilding(MyInput.Static.IsAnyShiftKeyPressed() || BuildingMode == BuildingModeEnum.Plane);
                    //}

                    //if (MyInput.Static.IsNewLeftMouseReleased() ||
                    //    MyInput.Static.IsNewRightMouseReleased())
                    //{
                    //    StopBuilding();
                    //}
                }
            }

            return false;
        }

        /// <summary>
        /// Handles input related when current grid being targeted.
        /// </summary>
        /// <returns></returns>
        private bool HandleCurrentGridInput(MyStringId context)
        {
                    if (MyControllerHelper.IsControl(context, MyControlsSpace.SYMMETRY_SWITCH, MyControlStateType.NEW_PRESSED) && !(MySession.Static.ControlledEntity is MyShipController))
                    {
                        if (BlockCreationIsActivated)
                            MyGuiAudio.PlaySound(MyGuiSounds.HudClick);

                        switch (m_symmetrySettingMode)
                        {
                            case MySymmetrySettingModeEnum.NoPlane:
                                m_symmetrySettingMode = MySymmetrySettingModeEnum.XPlane;
                                UpdateSymmetryNotification(MyCommonTexts.SettingSymmetryX);
                                break;
                            case MySymmetrySettingModeEnum.XPlane:
                                m_symmetrySettingMode = MySymmetrySettingModeEnum.XPlaneOdd;
                                UpdateSymmetryNotification(MyCommonTexts.SettingSymmetryXOffset);
                                break;
                            case MySymmetrySettingModeEnum.XPlaneOdd:
                                m_symmetrySettingMode = MySymmetrySettingModeEnum.YPlane;
                                UpdateSymmetryNotification(MyCommonTexts.SettingSymmetryY);
                                break;
                            case MySymmetrySettingModeEnum.YPlane:
                                m_symmetrySettingMode = MySymmetrySettingModeEnum.YPlaneOdd;
                                UpdateSymmetryNotification(MyCommonTexts.SettingSymmetryYOffset);
                                break;
                            case MySymmetrySettingModeEnum.YPlaneOdd:
                                m_symmetrySettingMode = MySymmetrySettingModeEnum.ZPlane;
                                UpdateSymmetryNotification(MyCommonTexts.SettingSymmetryZ);
                                break;
                            case MySymmetrySettingModeEnum.ZPlane:
                                m_symmetrySettingMode = MySymmetrySettingModeEnum.ZPlaneOdd;
                                UpdateSymmetryNotification(MyCommonTexts.SettingSymmetryZOffset);
                                break;
                            case MySymmetrySettingModeEnum.ZPlaneOdd:
                                m_symmetrySettingMode = MySymmetrySettingModeEnum.NoPlane;
                                RemoveSymmetryNotification();
                                break;
                        }
                    }

                    if (MyControllerHelper.IsControl(context, MyControlsSpace.USE_SYMMETRY, MyControlStateType.NEW_PRESSED) && !(MySession.Static.ControlledEntity is MyShipController))
                    {
                        MyGuiAudio.PlaySound(MyGuiSounds.HudClick);

                        if (m_symmetrySettingMode != MySymmetrySettingModeEnum.NoPlane)
                        {
                            UseSymmetry = false;
                            m_symmetrySettingMode = MySymmetrySettingModeEnum.NoPlane;
                            RemoveSymmetryNotification();
                            return true;
                        }

                        UseSymmetry = !UseSymmetry;
                    }

                    if (CurrentBlockDefinition == null || !BlockCreationIsActivated)
                    {
                        m_symmetrySettingMode = MySymmetrySettingModeEnum.NoPlane;
                        RemoveSymmetryNotification();
                    }

                    if (IsInSymmetrySettingMode && !(MySession.Static.ControlledEntity is MyShipController))
                    {
                        if (MyControllerHelper.IsControl(context, MyControlsSpace.PRIMARY_TOOL_ACTION, MyControlStateType.NEW_PRESSED))
                        {
                            if (m_gizmo.SpaceDefault.m_removeBlock != null)
                            {
                                Vector3I center = (m_gizmo.SpaceDefault.m_removeBlock.Min + m_gizmo.SpaceDefault.m_removeBlock.Max) / 2;

                                MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                                switch (m_symmetrySettingMode)
                                {
                                    case MySymmetrySettingModeEnum.NoPlane:
                                        System.Diagnostics.Debug.Assert(false, "Cannot get here");
                                        break;
                                    case MySymmetrySettingModeEnum.XPlane:
                                        CurrentGrid.XSymmetryPlane = center;
                                        CurrentGrid.XSymmetryOdd = false;
                                        break;
                                    case MySymmetrySettingModeEnum.XPlaneOdd:
                                        CurrentGrid.XSymmetryPlane = center;
                                        CurrentGrid.XSymmetryOdd = true;
                                        break;
                                    case MySymmetrySettingModeEnum.YPlane:
                                        CurrentGrid.YSymmetryPlane = center;
                                        CurrentGrid.YSymmetryOdd = false;
                                        break;
                                    case MySymmetrySettingModeEnum.YPlaneOdd:
                                        CurrentGrid.YSymmetryPlane = center;
                                        CurrentGrid.YSymmetryOdd = true;
                                        break;
                                    case MySymmetrySettingModeEnum.ZPlane:
                                        CurrentGrid.ZSymmetryPlane = center;
                                        CurrentGrid.ZSymmetryOdd = false;
                                        break;
                                    case MySymmetrySettingModeEnum.ZPlaneOdd:
                                        CurrentGrid.ZSymmetryPlane = center;
                                        CurrentGrid.ZSymmetryOdd = true;
                                        break;
                                }
                            }

                            return true;
                }

                        if (MyControllerHelper.IsControl(context, MyControlsSpace.SECONDARY_TOOL_ACTION, MyControlStateType.NEW_PRESSED))
                        {
                            MyGuiAudio.PlaySound(MyGuiSounds.HudDeleteBlock);

                            switch (m_symmetrySettingMode)
                            {
                                case MySymmetrySettingModeEnum.NoPlane:
                                    System.Diagnostics.Debug.Assert(false, "Cannot get here");
                                    break;
                                case MySymmetrySettingModeEnum.XPlane:
                                case MySymmetrySettingModeEnum.XPlaneOdd:
                                    CurrentGrid.XSymmetryPlane = null;
                                    CurrentGrid.XSymmetryOdd = false;
                                    break;
                                case MySymmetrySettingModeEnum.YPlane:
                                case MySymmetrySettingModeEnum.YPlaneOdd:
                                    CurrentGrid.YSymmetryPlane = null;
                                    CurrentGrid.YSymmetryOdd = false;
                                    break;
                                case MySymmetrySettingModeEnum.ZPlane:
                                case MySymmetrySettingModeEnum.ZPlaneOdd:
                                    CurrentGrid.ZSymmetryPlane = null;
                                    CurrentGrid.ZSymmetryOdd = false;
                                    break;
                            }

                            return false;
                        }
            }


                    if (MyInput.Static.IsNewKeyPressed(MyKeys.Escape))
                    {
                        if (m_symmetrySettingMode != MySymmetrySettingModeEnum.NoPlane)
                        {
                            m_symmetrySettingMode = MySymmetrySettingModeEnum.NoPlane;
                            RemoveSymmetryNotification();
                            return true;
                        }

                if (CancelBuilding())
                            return true;
                    }

                    if (MyInput.Static.IsNewLeftMousePressed() || MyControllerHelper.IsControl(context, MyControlsSpace.PRIMARY_BUILD_ACTION))
                    {
                        if (!PlacingSmallGridOnLargeStatic && (MyInput.Static.IsAnyCtrlKeyPressed() || BuildingMode != BuildingModeEnum.SingleBlock))
                        {
                            StartBuilding();
                        }
                        else
                        {
                            Add();
                        }
                    }

                    if (MyInput.Static.IsNewRightMousePressed() || MyControllerHelper.IsControl(context, MyControlsSpace.SECONDARY_BUILD_ACTION))
                    {
                        if (MyInput.Static.IsAnyCtrlKeyPressed() || BuildingMode != BuildingModeEnum.SingleBlock)
                        {
                            StartRemoving();
                        }
                        else
                        {
                            if (MyFakes.ENABLE_COMPOUND_BLOCKS && !CompoundEnabled)
                            {
                                foreach (var gizmoSpace in m_gizmo.Spaces)
                                {
                                    if (!gizmoSpace.Enabled)
                                        continue;

                                    gizmoSpace.m_blockIdInCompound = null;
                                }
                            }

                    PrepareBlocksToRemove();
                            Remove();
                        }
                    }

                    if (MyInput.Static.IsLeftMousePressed() ||
                        MyInput.Static.IsRightMousePressed() ||
                        MyControllerHelper.IsControl(context, MyControlsSpace.PRIMARY_BUILD_ACTION, MyControlStateType.PRESSED))
                    {
                        ContinueBuilding(MyInput.Static.IsAnyShiftKeyPressed() || BuildingMode == BuildingModeEnum.Plane);
                    }

                    if (MyInput.Static.IsNewLeftMouseReleased() ||
                        MyInput.Static.IsNewRightMouseReleased() ||
                        MyControllerHelper.IsControl(context, MyControlsSpace.PRIMARY_BUILD_ACTION, MyControlStateType.NEW_RELEASED))
                    {
                        StopBuilding();
                }

            return false;
        }

        private bool HandleBlockCreationMovement(MyStringId context)
        {
            bool ctrl = MyInput.Static.IsAnyCtrlKeyPressed();
            if ((ctrl && MyInput.Static.PreviousMouseScrollWheelValue() < MyInput.Static.MouseScrollWheelValue())
                || MyControllerHelper.IsControl(context, MyControlsSpace.MOVE_FURTHER, MyControlStateType.PRESSED))
            {
                    float previousIntersectionDistance = IntersectionDistance;
                    IntersectionDistance *= 1.1f;
                    if (IntersectionDistance > CubeBuilderDefinition.MaxBlockBuildingDistance)
                        IntersectionDistance = CubeBuilderDefinition.MaxBlockBuildingDistance;

                    if (MySession.Static.SurvivalMode && !SpectatorIsBuilding)
                    {
                        if (CurrentBlockDefinition != null)
                        {
                            float gridSize = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize);
                            BoundingBoxD localAABB = new BoundingBoxD(-CurrentBlockDefinition.Size * gridSize * 0.5f, CurrentBlockDefinition.Size * gridSize * 0.5f);
                            MatrixD gizmoMatrix = m_gizmo.SpaceDefault.m_worldMatrixAdd;
                            gizmoMatrix.Translation = FreePlacementTarget;
                            MatrixD inverseDrawMatrix = MatrixD.Invert(gizmoMatrix);
                            if (!MyCubeBuilderGizmo.DefaultGizmoCloseEnough(ref inverseDrawMatrix, localAABB, gridSize, IntersectionDistance))
                                IntersectionDistance = previousIntersectionDistance;
                        }
                    }

                return true;
                }
            else if ((ctrl && MyInput.Static.PreviousMouseScrollWheelValue() > MyInput.Static.MouseScrollWheelValue())
                || MyControllerHelper.IsControl(context, MyControlsSpace.MOVE_CLOSER, MyControlStateType.PRESSED))
            {
                    IntersectionDistance /= 1.1f;
                    if (IntersectionDistance < CubeBuilderDefinition.MinBlockBuildingDistance)
                        IntersectionDistance = CubeBuilderDefinition.MinBlockBuildingDistance;            

                return true;
                }

            return false;
        }

        /// <summary>
        /// Standard rotation, vertical around grid's Y, Roll around block's Z, and perpendicular vector to both (for parallel case used block's right). Returns axis index and sets direction.
        /// </summary>
        private int GetStandardRotationAxisAndDirection(int index, ref int direction)
        {
            int axis = -1;

            MatrixD transpose = MatrixD.Transpose(m_gizmo.SpaceDefault.m_localMatrixAdd);
            Vector3I upAxis = Vector3I.Round(Vector3D.TransformNormal(Vector3D.Up, transpose));

            if (MyInput.Static.IsAnyShiftKeyPressed())
                direction *= -1;

            if (DynamicMode)
            {
                int[] axes = new int[] { 1, 1, 0, 0, 2, 2 };
                Debug.Assert(axes.Length > index);
                return axes[index];
            }

            var singleMountPointNormal = GetSingleMountPointNormal();
            if (singleMountPointNormal != null)
            {
                var normal = singleMountPointNormal.Value;
                int dotUp = Vector3I.Dot(ref normal, ref Vector3I.Up);
                int dotRight = Vector3I.Dot(ref normal, ref Vector3I.Right);
                int dotForward = Vector3I.Dot(ref normal, ref Vector3I.Forward);

                if (dotUp == 1 || dotUp == -1)
                {
                    axis = 1;
                    direction *= dotUp;
                }
                else if (dotRight == 1 || dotRight == -1)
                {
                    axis = 0;
                    direction *= dotRight;
                }
                else if (dotForward == 1 || dotForward == -1)
                {
                    axis = 2;
                    direction *= dotForward;
                }
            }
            else if (index < 2)
            {
                // Vertical axis
                int dotUp = Vector3I.Dot(ref upAxis, ref Vector3I.Up);
                int dotRight = Vector3I.Dot(ref upAxis, ref Vector3I.Right);
                int dotForward = Vector3I.Dot(ref upAxis, ref Vector3I.Forward);

                if (dotUp == 1 || dotUp == -1)
                {
                    axis = 1;
                    direction *= dotUp;
                }
                else if (dotRight == 1 || dotRight == -1)
                {
                    axis = 0;
                    direction *= dotRight;
                }
                else if (dotForward == 1 || dotForward == -1)
                {
                    axis = 2;
                    direction *= dotForward;
                }
            }
            else if (index >= 2 && index < 4)
            {
                // Right axis - perpendicular to global Up and block.Forward (roll dir)
                Vector3I rightAxis;
                Vector3I blockForward = Vector3I.Round(m_gizmo.SpaceDefault.m_localMatrixAdd.Forward);
                int dotForwardUp = Vector3I.Dot(ref blockForward, ref Vector3I.Up);
                if (dotForwardUp == 0)
                {
                    Vector3I.Cross(ref blockForward, ref Vector3I.Up, out rightAxis);
                    rightAxis = Vector3I.Round(Vector3D.TransformNormal((Vector3)rightAxis, transpose));

                    int dotUp = Vector3I.Dot(ref rightAxis, ref Vector3I.Up);
                    int dotRight = Vector3I.Dot(ref rightAxis, ref Vector3I.Right);
                    int dotForward = Vector3I.Dot(ref rightAxis, ref Vector3I.Forward);

                    if (dotUp == 1 || dotUp == -1)
                    {
                        axis = 1;
                        direction *= dotUp;
                    }
                    else if (dotRight == 1 || dotRight == -1)
                    {
                        axis = 0;
                        // Do not set direction according to dot (or model will not be rotated in all positions but only in 2)!
                        //direction *= dotRight;
                    }
                    else if (dotForward == 1 || dotForward == -1)
                    {
                        axis = 2;
                        direction *= dotForward;
                    }
                }
                else
                {
                    // Use block's right
                    axis = 0;
                }
            }
            else if (index >= 4)
            {
                // Roll uses local model Z axis
                axis = 2;
            }

            return axis;
        }

        public void InputLost()
        {
            m_gizmo.Clear();
        }

        private void UpdateSymmetryNotification(MyStringId myTextsWrapperEnum)
        {
            RemoveSymmetryNotification();

            m_symmetryNotification = new MyHudNotification(myTextsWrapperEnum, 0, level: MyNotificationLevel.Control);
            if (!MyInput.Static.IsJoystickConnected())
            {
                m_symmetryNotification.SetTextFormatArguments(
                    MyInput.Static.GetGameControl(MyControlsSpace.PRIMARY_TOOL_ACTION),
                    MyInput.Static.GetGameControl(MyControlsSpace.SECONDARY_TOOL_ACTION));
            }
            else
            {
                m_symmetryNotification.SetTextFormatArguments(
                    MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_BUILD_MODE, MyControlsSpace.PRIMARY_TOOL_ACTION),
                    MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_BUILD_MODE, MyControlsSpace.SECONDARY_BUILD_ACTION));
            }

            MyHud.Notifications.Add(m_symmetryNotification);
        }

        private void RemoveSymmetryNotification()
        {
            if (m_symmetryNotification != null)
            {
                MyHud.Notifications.Remove(m_symmetryNotification);
                m_symmetryNotification = null;
            }
        }

        #region GizmoTests

        public static void PrepareCharacterCollisionPoints(List<Vector3D> outList)
        {
            MyCharacter character = (MySession.Static.ControlledEntity as MyCharacter);
            if (character == null) return;

            float height = character.Definition.CharacterCollisionHeight * 0.7f;
            float width = character.Definition.CharacterCollisionWidth * 0.2f;

            if (character != null)
            {
                if (character.IsCrouching)
                    height = character.Definition.CharacterCollisionCrouchHeight;

                Vector3 upVec = character.PositionComp.LocalMatrix.Up * height;
                Vector3 fwVec = character.PositionComp.LocalMatrix.Forward * width;
                Vector3 rtVec = character.PositionComp.LocalMatrix.Right * width;
                Vector3D pos = character.Entity.PositionComp.GetPosition() + character.PositionComp.LocalMatrix.Up * 0.2f;

                float angle = 0.0f;
                for (int i = 0; i < 6; ++i)
                {
                    float sin = (float)Math.Sin(angle);
                    float cos = (float)Math.Cos(angle);
                    Vector3D bottomPoint = pos + sin * rtVec + cos * fwVec;
                    outList.Add(bottomPoint);
                    outList.Add(bottomPoint + upVec);
                    angle += (float)Math.PI / 3.0f;
                }
            }
        }

        #endregion

        protected virtual void UpdateGizmo(MyCubeBuilderGizmo.MyGizmoSpaceProperties gizmoSpace, bool add, bool remove, bool draw)
        {
            if (!gizmoSpace.Enabled)
                return;

            ProfilerShort.Begin("CanBuild");
            if (MyCubeBuilder.Static.canBuild == false)
            {
                gizmoSpace.m_showGizmoCube = false;
                gizmoSpace.m_buildAllowed = false;
            }                       
            if (DynamicMode)
            {
                ProfilerShort.BeginNextBlock("UpdateGizmo_DynamicMode");
                UpdateGizmo_DynamicMode(gizmoSpace);
            }
            else if (CurrentGrid != null)
            {
                ProfilerShort.BeginNextBlock("UpdateGizmo_Grid");
                UpdateGizmo_Grid(gizmoSpace, add, remove, draw);
            }
            //if (CurrentGrid != null)
            //{
            //    ProfilerShort.BeginNextBlock("UpdateGizmo_Grid");
            //    UpdateGizmo_Grid(gizmoSpace, add, remove, draw);
            //}
            //else if (DynamicMode)
            //{
            //    ProfilerShort.BeginNextBlock("UpdateGizmo_DynamicMode");
            //    UpdateGizmo_DynamicMode(gizmoSpace);
            //}
            else// if (CurrentVoxelBase != null)
            {
                ProfilerShort.BeginNextBlock("UpdateGizmo_VoxelMap");
                UpdateGizmo_VoxelMap(gizmoSpace, add, remove, draw);
            }
            
            ProfilerShort.End();
        }

        private void UpdateGizmo_DynamicMode(MyCubeBuilderGizmo.MyGizmoSpaceProperties gizmoSpace)
        {
            Debug.Assert(DynamicMode);
            gizmoSpace.m_animationProgress = 1;

            float gridSize = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize);
            BoundingBoxD localAABB = new BoundingBoxD(-CurrentBlockDefinition.Size * gridSize * 0.5f, CurrentBlockDefinition.Size * gridSize * 0.5f);

            var settings = CurrentBlockDefinition.CubeSize == MyCubeSize.Large ? CubeBuilderDefinition.BuildingSettings.LargeGrid : CubeBuilderDefinition.BuildingSettings.SmallGrid;
            MatrixD drawMatrix = gizmoSpace.m_worldMatrixAdd;

            MyCubeGrid.GetCubeParts(CurrentBlockDefinition, Vector3I.Zero, Matrix.Identity, gridSize, gizmoSpace.m_cubeModelsTemp, gizmoSpace.m_cubeMatricesTemp, gizmoSpace.m_cubeNormals, gizmoSpace.m_patternOffsets);

            if (gizmoSpace.m_showGizmoCube)
            {
                //MatrixD invDrawMatrix = MatrixD.Invert(gizmoSpace.m_worldMatrixAdd);

                m_gizmo.AddFastBuildParts(gizmoSpace, CurrentBlockDefinition, null);
                m_gizmo.UpdateGizmoCubeParts(gizmoSpace, m_renderData, ref MatrixD.Identity, CurrentBlockDefinition);
            }

            BuildComponent.GetGridSpawnMaterials(CurrentBlockDefinition, drawMatrix, false);
            if (MySession.Static.CreativeToolsEnabled(Sync.MyId) == false)
            {
                gizmoSpace.m_buildAllowed &= BuildComponent.HasBuildingMaterials(MySession.Static.LocalCharacter);
            }

            
            MatrixD inverseDrawMatrix = MatrixD.Invert(drawMatrix);
            if (MySession.Static.SurvivalMode && !SpectatorIsBuilding && !MySession.Static.CreativeToolsEnabled(Sync.MyId))
            {
                if (!MyCubeBuilderGizmo.DefaultGizmoCloseEnough(ref inverseDrawMatrix, localAABB, gridSize, IntersectionDistance) 
                    || MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.Spectator)
                {
                    gizmoSpace.m_buildAllowed = false;
                    gizmoSpace.m_removeBlock = null;
                }

                if (CameraControllerSpectator)
                {
                    gizmoSpace.m_showGizmoCube = false;
                    gizmoSpace.m_buildAllowed = false;
                    return;
                }
            }

            // m_buildAllowed is set in shape cast
            if (!gizmoSpace.m_dynamicBuildAllowed)
            {
                bool placementTest = MyCubeGrid.TestBlockPlacementArea(CurrentBlockDefinition, null, drawMatrix, ref settings, localAABB, DynamicMode);//DynamicModeVoxelTest);
                gizmoSpace.m_buildAllowed &= placementTest;
            }
            gizmoSpace.m_showGizmoCube = true;

            gizmoSpace.m_cubeMatricesTemp.Clear();
            gizmoSpace.m_cubeModelsTemp.Clear();

            m_rotationHints.CalculateRotationHints(drawMatrix, localAABB, !MyHud.MinimalHud && !MyHud.CutsceneHud && MySandboxGame.Config.RotationHints && MyFakes.ENABLE_ROTATION_HINTS);

            // In dynamic mode gizmo cube is shown even if it intersects character
            gizmoSpace.m_buildAllowed &= !IntersectsCharacterOrCamera(gizmoSpace, gridSize, ref inverseDrawMatrix);

            if (gizmoSpace.m_showGizmoCube)
            {
                Color color = Color.White;
                string lineMaterial = gizmoSpace.m_buildAllowed ? "GizmoDrawLine" : "GizmoDrawLineRed";

                if (gizmoSpace.SymmetryPlane == MySymmetrySettingModeEnum.Disabled)
                {
                    MySimpleObjectDraw.DrawTransparentBox(ref drawMatrix,
                        ref localAABB, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.04f, lineMaterial: lineMaterial);
                }

                AddFastBuildModels(gizmoSpace, MatrixD.Identity, gizmoSpace.m_cubeMatricesTemp, gizmoSpace.m_cubeModelsTemp, gizmoSpace.m_blockDefinition);

                Debug.Assert(gizmoSpace.m_cubeMatricesTemp.Count == gizmoSpace.m_cubeModelsTemp.Count);
                for (int i = 0; i < gizmoSpace.m_cubeMatricesTemp.Count; ++i)
                {
                    string model = gizmoSpace.m_cubeModelsTemp[i];
                    if (!string.IsNullOrEmpty(model))
                    {
                        var mId = MyModel.GetId(model);
                        m_renderData.AddInstance(mId, gizmoSpace.m_cubeMatricesTemp[i], ref MatrixD.Identity);
                    }
                }
            }
        }

        private void UpdateGizmo_VoxelMap(MyCubeBuilderGizmo.MyGizmoSpaceProperties gizmoSpace, bool add, bool remove, bool draw)
        {

            if (!m_animationLock)
            {
                gizmoSpace.m_animationLastMatrix = gizmoSpace.m_localMatrixAdd;
            }
            MatrixD animationMatrix = gizmoSpace.m_localMatrixAdd;
            if (MySandboxGame.Config.AnimatedRotation && gizmoSpace.m_animationProgress < 1)
            {
                animationMatrix = MatrixD.Slerp(gizmoSpace.m_animationLastMatrix, gizmoSpace.m_localMatrixAdd, gizmoSpace.m_animationProgress);
            }
            else if (MySandboxGame.Config.AnimatedRotation && gizmoSpace.m_animationProgress >= 1)
            {
                m_animationLock = false;
                gizmoSpace.m_animationLastMatrix = gizmoSpace.m_localMatrixAdd;
            }
            
            Color green = new Color(Color.Green * 0.6f, 1f);

            float gridSize = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize);

            Vector3 temp;
            Vector3D worldCenter = Vector3D.Zero;
            MatrixD drawMatrix = gizmoSpace.m_worldMatrixAdd;
            MatrixD localOrientation = animationMatrix.GetOrientation();

            Color color = green;

            gizmoSpace.m_showGizmoCube = !IntersectsCharacterOrCamera(gizmoSpace, gridSize, ref MatrixD.Identity);

            int posIndex = 0;
            for (temp.X = 0; temp.X < CurrentBlockDefinition.Size.X; temp.X++)
                for (temp.Y = 0; temp.Y < CurrentBlockDefinition.Size.Y; temp.Y++)
                    for (temp.Z = 0; temp.Z < CurrentBlockDefinition.Size.Z; temp.Z++)
                    {
                        Vector3I gridPosition = gizmoSpace.m_positions[posIndex++];
                        Vector3D gridRelative = gridPosition * gridSize;
                        if (!CubeBuilderDefinition.BuildingSettings.StaticGridAlignToCenter)
                            gridRelative += new Vector3D(0.5 * gridSize, 0.5 * gridSize, -0.5 * gridSize);
                        Vector3D tempWorldPos = Vector3D.Transform(gridRelative, gizmoSpace.m_worldMatrixAdd);

                        worldCenter += gridRelative;

                        MyCubeGrid.GetCubePartsWithoutTopologyCheck(
                            CurrentBlockDefinition, 
                            gridPosition,
                            localOrientation, 
                            gridSize, 
                            gizmoSpace.m_cubeModelsTemp,
                            gizmoSpace.m_cubeMatricesTemp, 
                            gizmoSpace.m_cubeNormals, 
                            gizmoSpace.m_patternOffsets
                            );

                        if (gizmoSpace.m_showGizmoCube)
                        {
                            for (int i = 0; i < gizmoSpace.m_cubeMatricesTemp.Count; i++)
                            {
                                MatrixD modelMatrix = gizmoSpace.m_cubeMatricesTemp[i] * gizmoSpace.m_worldMatrixAdd;
                                modelMatrix.Translation = tempWorldPos;
                                gizmoSpace.m_cubeMatricesTemp[i] = modelMatrix;
                            }
                            drawMatrix.Translation = tempWorldPos;
                            MatrixD invDrawMatrix = MatrixD.Invert(localOrientation * drawMatrix);
                            
                            m_gizmo.AddFastBuildParts(gizmoSpace, CurrentBlockDefinition, null);
                            m_gizmo.UpdateGizmoCubeParts(gizmoSpace, m_renderData, ref invDrawMatrix, CurrentBlockDefinition);
                        }
                    }

            //calculate world center for block model
            worldCenter /= CurrentBlockDefinition.Size.Size;
            if (!m_animationLock)
            {
                gizmoSpace.m_animationProgress = 0;
                gizmoSpace.m_animationLastPosition = worldCenter;
            }
            else if (MySandboxGame.Config.AnimatedRotation && gizmoSpace.m_animationProgress < 1)
            {
                worldCenter = Vector3D.Lerp(gizmoSpace.m_animationLastPosition, worldCenter, gizmoSpace.m_animationProgress);
            }
            worldCenter = Vector3D.Transform(worldCenter, gizmoSpace.m_worldMatrixAdd);
            drawMatrix.Translation = worldCenter;
            drawMatrix = localOrientation * drawMatrix;

            BoundingBoxD localAABB = new BoundingBoxD(-CurrentBlockDefinition.Size * gridSize * 0.5f, CurrentBlockDefinition.Size * gridSize * 0.5f);

            var settings = CurrentBlockDefinition.CubeSize == MyCubeSize.Large ? CubeBuilderDefinition.BuildingSettings.LargeStaticGrid : CubeBuilderDefinition.BuildingSettings.SmallStaticGrid;
            MyBlockOrientation blockOrientation = new MyBlockOrientation(ref Quaternion.Identity);
            bool placementTest = CheckValidBlockRotation(gizmoSpace.m_localMatrixAdd, CurrentBlockDefinition.Direction, CurrentBlockDefinition.Rotation)
                && MyCubeGrid.TestBlockPlacementArea(CurrentBlockDefinition, blockOrientation, drawMatrix, ref settings, localAABB, false);
            gizmoSpace.m_buildAllowed &= placementTest;
            gizmoSpace.m_buildAllowed &= gizmoSpace.m_showGizmoCube;
            gizmoSpace.m_worldMatrixAdd = drawMatrix;

            BuildComponent.GetGridSpawnMaterials(CurrentBlockDefinition, drawMatrix, true);
            if (MySession.Static.CreativeToolsEnabled(Sync.MyId) == false)
            {
                gizmoSpace.m_buildAllowed &= BuildComponent.HasBuildingMaterials(MySession.Static.LocalCharacter);
            }

            if (MySession.Static.SurvivalMode && !SpectatorIsBuilding && MySession.Static.CreativeToolsEnabled(Sync.MyId) == false)
            {
                BoundingBoxD gizmoBox = localAABB.TransformFast(ref drawMatrix);

                if (!MyCubeBuilderGizmo.DefaultGizmoCloseEnough(ref MatrixD.Identity, gizmoBox, gridSize, IntersectionDistance) || CameraControllerSpectator)
                {
                    gizmoSpace.m_buildAllowed = false;
                    gizmoSpace.m_showGizmoCube = false;
                    gizmoSpace.m_removeBlock = null;
                    return;
                }
            }


            color = Color.White;
            string lineMaterial = gizmoSpace.m_buildAllowed ? "GizmoDrawLine" : "GizmoDrawLineRed";

            if (gizmoSpace.SymmetryPlane == MySymmetrySettingModeEnum.Disabled)
            {
                MySimpleObjectDraw.DrawTransparentBox(ref drawMatrix,
                    ref localAABB, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.04f, lineMaterial: lineMaterial);

                m_rotationHints.CalculateRotationHints(drawMatrix, localAABB, !MyHud.MinimalHud && !MyHud.CutsceneHud && MySandboxGame.Config.RotationHints && draw && MyFakes.ENABLE_ROTATION_HINTS);
            }

            gizmoSpace.m_cubeMatricesTemp.Clear();
            gizmoSpace.m_cubeModelsTemp.Clear();

            if (gizmoSpace.m_showGizmoCube)
            {
                // Draw mount points of added cube block as yellow squares in neighboring cells.
                if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS)
                {
                    DrawMountPoints(gridSize, CurrentBlockDefinition, ref drawMatrix);
                }

                AddFastBuildModels(gizmoSpace, MatrixD.Identity, gizmoSpace.m_cubeMatricesTemp, gizmoSpace.m_cubeModelsTemp, gizmoSpace.m_blockDefinition);

                Debug.Assert(gizmoSpace.m_cubeMatricesTemp.Count == gizmoSpace.m_cubeModelsTemp.Count);
                for (int i = 0; i < gizmoSpace.m_cubeMatricesTemp.Count; ++i)
                {
                    string model = gizmoSpace.m_cubeModelsTemp[i];
                    if (!string.IsNullOrEmpty(model))
                        m_renderData.AddInstance(MyModel.GetId(model), gizmoSpace.m_cubeMatricesTemp[i], ref MatrixD.Identity);
                }
            }
            gizmoSpace.m_animationProgress += m_animationSpeed;
        }

        private void UpdateGizmo_Grid(MyCubeBuilderGizmo.MyGizmoSpaceProperties gizmoSpace, bool add, bool remove, bool draw)
        {
            ProfilerShort.Begin("Colors");
            Color green = new Color(Color.Green * 0.6f, 1f);
            Color red = new Color(Color.Red * 0.8f, 1);
            Color yellow = Color.Yellow;
            Color black = Color.Black;
            Color gray = Color.Gray;
            Color white = Color.White;

            ProfilerShort.BeginNextBlock("Add");
            if (add)
            {
                if (!m_animationLock)
                {
                    gizmoSpace.m_animationLastMatrix = gizmoSpace.m_localMatrixAdd;
                }
                MatrixD animationMatrix = gizmoSpace.m_localMatrixAdd;
                if (MySandboxGame.Config.AnimatedRotation && gizmoSpace.m_animationProgress < 1)
                {
                    animationMatrix = MatrixD.Slerp(gizmoSpace.m_animationLastMatrix, gizmoSpace.m_localMatrixAdd, gizmoSpace.m_animationProgress);
                }
                else if (MySandboxGame.Config.AnimatedRotation && gizmoSpace.m_animationProgress >= 1)
                {
                    m_animationLock = false;
                    gizmoSpace.m_animationLastMatrix = gizmoSpace.m_localMatrixAdd;
                }
                MatrixD modelTransform = animationMatrix * CurrentGrid.WorldMatrix;


                if (gizmoSpace.m_startBuild != null && gizmoSpace.m_continueBuild != null)
                {
                    gizmoSpace.m_buildAllowed = true;
                }

                if (PlacingSmallGridOnLargeStatic && gizmoSpace.m_positionsSmallOnLarge.Count == 0)
                {
                    ProfilerShort.End();
                    return;
                }

                ProfilerShort.Begin("CurrentBlockDefinition");
                if (CurrentBlockDefinition != null)
                {
                    ProfilerShort.Begin("GetOrientation");
                    Matrix addOrientationMat = gizmoSpace.m_localMatrixAdd.GetOrientation();
                    MyBlockOrientation gizmoAddOrientation = new MyBlockOrientation(ref addOrientationMat);

                    ProfilerShort.BeginNextBlock("PlacingSmallGridOnLargeStatic");
                    // Test free space in the cube grid (& valid rotation of the block)
                    if (!PlacingSmallGridOnLargeStatic)
                    {
                        ProfilerShort.Begin("CheckValidBlockRotation");
                        bool isValidBlockRotation = CheckValidBlockRotation(gizmoSpace.m_localMatrixAdd, CurrentBlockDefinition.Direction, CurrentBlockDefinition.Rotation);
                        ProfilerShort.BeginNextBlock("CanPlaceBlock");
                        bool canPlaceBlock = CurrentGrid.CanPlaceBlock(gizmoSpace.m_min, gizmoSpace.m_max, gizmoAddOrientation, gizmoSpace.m_blockDefinition);
                        ProfilerShort.BeginNextBlock("m_buildAllowed");
                        gizmoSpace.m_buildAllowed &= isValidBlockRotation && canPlaceBlock;
                        ProfilerShort.End();
                    }

                    ProfilerShort.BeginNextBlock("IsAdminModeEnabled");
                    MyCubeBuilder.BuildComponent.GetBlockPlacementMaterials(gizmoSpace.m_blockDefinition, gizmoSpace.m_addPos, gizmoAddOrientation, CurrentGrid);
                    if (MySession.Static.CreativeToolsEnabled(Sync.MyId) == false)
                    {
                        gizmoSpace.m_buildAllowed &= MyCubeBuilder.BuildComponent.HasBuildingMaterials(MySession.Static.LocalCharacter);
                    }

                    ProfilerShort.BeginNextBlock("SurvivalMode");
                    // In survival, check whether you're close enough, and have enough materials or haven't built for long enough
                    if (!PlacingSmallGridOnLargeStatic && MySession.Static.SurvivalMode && MySession.Static.CreativeToolsEnabled(Sync.MyId) == false && !SpectatorIsBuilding)
                    {
                        Vector3 localMin = (m_gizmo.SpaceDefault.m_min - new Vector3(0.5f)) * CurrentGrid.GridSize;
                        Vector3 localMax = (m_gizmo.SpaceDefault.m_max + new Vector3(0.5f)) * CurrentGrid.GridSize;
                        BoundingBoxD gizmoBox = new BoundingBoxD(localMin, localMax);

                        if (!MyCubeBuilderGizmo.DefaultGizmoCloseEnough(ref m_invGridWorldMatrix, gizmoBox, CurrentGrid.GridSize, IntersectionDistance) || CameraControllerSpectator)
                        {
                            gizmoSpace.m_buildAllowed = false;
                            gizmoSpace.m_removeBlock = null;

                            ProfilerShort.End();
                            ProfilerShort.End();
                            ProfilerShort.End();
                            return;
                        }
                    }

                    ProfilerShort.BeginNextBlock("m_buildAllowed");
                    // Check whether mount points match any of its neighbors (only if we can build here though).
                    if (gizmoSpace.m_buildAllowed)
                    {
                        ProfilerShort.Begin("CreateFromRotationMatrix");
                        Quaternion.CreateFromRotationMatrix(ref gizmoSpace.m_localMatrixAdd, out gizmoSpace.m_rotation);

                        ProfilerShort.BeginNextBlock("SymmetryPlane");
						if (gizmoSpace.SymmetryPlane == MySymmetrySettingModeEnum.Disabled && !PlacingSmallGridOnLargeStatic)
						{
                            ProfilerShort.Begin("GetBuildProgressModelMountPoints");
							var mountPoints = CurrentBlockDefinition.GetBuildProgressModelMountPoints(MyComponentStack.NewBlockIntegrity);
                            
                            ProfilerShort.BeginNextBlock("CheckConnectivity");
							gizmoSpace.m_buildAllowed = MyCubeGrid.CheckConnectivity(CurrentGrid, CurrentBlockDefinition, mountPoints, ref gizmoSpace.m_rotation, ref gizmoSpace.m_centerPos);
                            
                            ProfilerShort.End();
						}
                        ProfilerShort.End();
                    }

                    Color color = green;

                    ProfilerShort.BeginNextBlock("DisableInside");
                    // Disable gizmo cubes when the camera is inside the currently displayed cube or where the character is inside the cube
                    if (PlacingSmallGridOnLargeStatic)
                    {
                        var invMatrix = MatrixD.Invert(gizmoSpace.m_worldMatrixAdd);
                        gizmoSpace.m_showGizmoCube = !IntersectsCharacterOrCamera(gizmoSpace, CurrentGrid.GridSize, ref invMatrix);
                    }
                    else
                    {
                        gizmoSpace.m_showGizmoCube = !IntersectsCharacterOrCamera(gizmoSpace, CurrentGrid.GridSize, ref m_invGridWorldMatrix);
                    }

                    gizmoSpace.m_buildAllowed &= gizmoSpace.m_showGizmoCube;

                    Vector3 temp;
                    Vector3D worldCenter = Vector3D.Zero;
                    Vector3D worldPos = gizmoSpace.m_worldMatrixAdd.Translation;
                    MatrixD drawMatrix = gizmoSpace.m_worldMatrixAdd;

                    ProfilerShort.BeginNextBlock("DrawGizmo");
                    int posIndex = 0;
                    for (temp.X = 0; temp.X < CurrentBlockDefinition.Size.X; temp.X++)
                        for (temp.Y = 0; temp.Y < CurrentBlockDefinition.Size.Y; temp.Y++)
                            for (temp.Z = 0; temp.Z < CurrentBlockDefinition.Size.Z; temp.Z++)
                                #region if(PlacingSmallGridOnLargeStatic)
                                if (PlacingSmallGridOnLargeStatic)
                                {
                                    float smallToLarge = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize) / CurrentGrid.GridSize;

                                    Vector3D gridPosition = gizmoSpace.m_positionsSmallOnLarge[posIndex++];
                                    Vector3I gridPositionInt = Vector3I.Round(gridPosition / smallToLarge);
                                    Vector3D tempWorldPos = Vector3D.Transform(gridPosition * CurrentGrid.GridSize, CurrentGrid.WorldMatrix);

                                    worldCenter += tempWorldPos;
                                    drawMatrix.Translation = tempWorldPos;

                                    MyCubeGrid.GetCubeParts(CurrentBlockDefinition, gridPositionInt, gizmoSpace.m_localMatrixAdd.GetOrientation(), CurrentGrid.GridSize, gizmoSpace.m_cubeModelsTemp, gizmoSpace.m_cubeMatricesTemp, gizmoSpace.m_cubeNormals, gizmoSpace.m_patternOffsets);

                                    if (gizmoSpace.m_showGizmoCube)
                                    {
                                        for (int i = 0; i < gizmoSpace.m_cubeMatricesTemp.Count; i++)
                                        {
                                            MatrixD modelMatrix = gizmoSpace.m_cubeMatricesTemp[i];
                                            modelMatrix.Translation *= smallToLarge;
                                            modelMatrix = modelMatrix * CurrentGrid.WorldMatrix;
                                            modelMatrix.Translation = tempWorldPos;
                                            gizmoSpace.m_cubeMatricesTemp[i] = modelMatrix;
                                        }

                                        m_gizmo.AddFastBuildParts(gizmoSpace, CurrentBlockDefinition, CurrentGrid);
                                        m_gizmo.UpdateGizmoCubeParts(gizmoSpace, m_renderData, ref m_invGridWorldMatrix);
                                    }
                                }
                                #endregion
                                else
                                {
                                    Vector3I gridPosition = gizmoSpace.m_positions[posIndex++];
                                    Vector3D tempWorldPos = Vector3D.Transform(gridPosition * CurrentGrid.GridSize, CurrentGrid.WorldMatrix);

                                    worldCenter += gridPosition * CurrentGrid.GridSize;

                                    MyCubeGrid.GetCubePartsWithoutTopologyCheck(
                                        CurrentBlockDefinition,
                                        gridPosition,
                                        animationMatrix.GetOrientation(),
                                        CurrentGrid.GridSize,
                                        gizmoSpace.m_cubeModelsTemp,
                                        gizmoSpace.m_cubeMatricesTemp,
                                        gizmoSpace.m_cubeNormals,
                                        gizmoSpace.m_patternOffsets
                                        );

                                    //armor render
                                    if (gizmoSpace.m_showGizmoCube)
                                    {
                                        for (int i = 0; i < gizmoSpace.m_cubeMatricesTemp.Count; i++)
                                        {
                                            MatrixD modelMatrix = gizmoSpace.m_cubeMatricesTemp[i] * CurrentGrid.WorldMatrix;
                                            modelMatrix.Translation = tempWorldPos;
                                            gizmoSpace.m_cubeMatricesTemp[i] = modelMatrix;
                                        }
                                        
                                        m_gizmo.AddFastBuildParts(gizmoSpace, CurrentBlockDefinition, CurrentGrid);
                                        m_gizmo.UpdateGizmoCubeParts(gizmoSpace, m_renderData, ref m_invGridWorldMatrix, CurrentBlockDefinition);
                                    }
                                }


                    //calculate world center for block model
                    worldCenter /= CurrentBlockDefinition.Size.Size;
                    
                    if (!m_animationLock)
                    {
                        gizmoSpace.m_animationProgress = 0;
                        gizmoSpace.m_animationLastPosition = worldCenter;
                    }
                    else if (MySandboxGame.Config.AnimatedRotation && gizmoSpace.m_animationProgress < 1)
                    {
                        worldCenter = Vector3D.Lerp(gizmoSpace.m_animationLastPosition, worldCenter, gizmoSpace.m_animationProgress);
                    }
                    worldCenter = Vector3D.Transform(worldCenter, CurrentGrid.WorldMatrix);
                    drawMatrix.Translation = worldCenter;
                    
                    float gridSize = PlacingSmallGridOnLargeStatic ? MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize) : CurrentGrid.GridSize;
                    BoundingBoxD localAABB = new BoundingBoxD(-CurrentBlockDefinition.Size * gridSize * 0.5f, CurrentBlockDefinition.Size * gridSize * 0.5f);

                    // Test voxel only here. Cube placement was tested earlier.
                    var settingsVoxelTest = CubeBuilderDefinition.BuildingSettings.GetGridPlacementSettings(CurrentBlockDefinition.CubeSize, CurrentGrid.IsStatic);
                    MyBlockOrientation orientation = new MyBlockOrientation(ref Quaternion.Identity);
                    bool voxelTest = MyCubeGrid.TestVoxelPlacement(CurrentBlockDefinition, settingsVoxelTest, false, drawMatrix, localAABB);
                    gizmoSpace.m_buildAllowed &= voxelTest;

                    ProfilerShort.BeginNextBlock("CheckConnectivity");
                    #region if(PlacingSmallGridOnLargeStatic)
                    if (PlacingSmallGridOnLargeStatic)
                    {
                        if (MySession.Static.SurvivalMode && !SpectatorIsBuilding && MySession.Static.CreativeToolsEnabled(Sync.MyId) == false)
                        {
                            MatrixD invDrawMatrix = Matrix.Invert(drawMatrix);

                            MyCubeBuilder.BuildComponent.GetBlockPlacementMaterials(CurrentBlockDefinition, gizmoSpace.m_addPos, gizmoAddOrientation, CurrentGrid);

                            gizmoSpace.m_buildAllowed &= MyCubeBuilder.BuildComponent.HasBuildingMaterials(MySession.Static.LocalCharacter);

                            if (!MyCubeBuilderGizmo.DefaultGizmoCloseEnough(ref invDrawMatrix, localAABB, gridSize, IntersectionDistance) || CameraControllerSpectator)
                            {
                                gizmoSpace.m_buildAllowed = false;
                                gizmoSpace.m_removeBlock = null;

                                ProfilerShort.End();
                                ProfilerShort.End();
                                ProfilerShort.End();
                                return;
                            }
                        }

                        var settings = CubeBuilderDefinition.BuildingSettings.GetGridPlacementSettings(CurrentGrid.GridSizeEnum, CurrentGrid.IsStatic);
                        // Orientation is identity (local), because it is represented in world matrix also.
                        bool placementTest = CheckValidBlockRotation(gizmoSpace.m_localMatrixAdd, CurrentBlockDefinition.Direction, CurrentBlockDefinition.Rotation)
                            && MyCubeGrid.TestBlockPlacementArea(CurrentBlockDefinition, orientation, drawMatrix, ref settings, localAABB, !CurrentGrid.IsStatic, testVoxel: false);
                        gizmoSpace.m_buildAllowed &= placementTest;

                        if (gizmoSpace.m_buildAllowed && gizmoSpace.SymmetryPlane == MySymmetrySettingModeEnum.Disabled)
                            gizmoSpace.m_buildAllowed
                                &= MyCubeGrid.CheckConnectivitySmallBlockToLargeGrid(CurrentGrid, CurrentBlockDefinition, ref gizmoSpace.m_rotation, ref gizmoSpace.m_addDir);

                        gizmoSpace.m_worldMatrixAdd = drawMatrix;
                    }
                    #endregion

                    color = Color.White;
                    string lineMaterial = gizmoSpace.m_buildAllowed ? "GizmoDrawLine" : "GizmoDrawLineRed";

                    ProfilerShort.BeginNextBlock("SymmetryPlane 2");
                    if (gizmoSpace.SymmetryPlane == MySymmetrySettingModeEnum.Disabled)
                    {
                        ProfilerShort.Begin("DrawTransparentBox");
                        #region if(MyFakes.ENABLE_VR_BUILDING)
                        if (MyFakes.ENABLE_VR_BUILDING)
                        {
                            Vector3 centerOffset = -0.5f * gizmoSpace.m_addDir;
                            if (gizmoSpace.m_addPosSmallOnLarge != null)
                            {
                                float smallToLarge = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize) / CurrentGrid.GridSize;
                                centerOffset = -0.5f * smallToLarge * gizmoSpace.m_addDir;
                            }
                            centerOffset *= CurrentGrid.GridSize;

                            Vector3I rotatedSize = Vector3I.Round(Vector3.Abs(Vector3.TransformNormal((Vector3)CurrentBlockDefinition.Size, gizmoSpace.m_localMatrixAdd)));
                            Vector3I invAddDir = Vector3I.One - Vector3I.Abs(gizmoSpace.m_addDir);
                            Vector3 halfExtends = gridSize * 0.5f * (rotatedSize * invAddDir) + 0.02f * Vector3I.Abs(gizmoSpace.m_addDir);

                            BoundingBoxD vrAabb = new BoundingBoxD(-halfExtends + centerOffset, halfExtends + centerOffset);

                            MySimpleObjectDraw.DrawTransparentBox(ref drawMatrix,
                                ref vrAabb, ref color, MySimpleObjectRasterizer.Wireframe, 1, gizmoSpace.m_addPosSmallOnLarge != null ? 0.04f : 0.06f, null, 
                                lineMaterial, false, -1);
                        }
                        #endregion
                        else
                        {
                            //Wireframe box
                            modelTransform.Translation = drawMatrix.Translation;
                            MySimpleObjectDraw.DrawTransparentBox(ref modelTransform,
                                ref localAABB, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.04f, null, lineMaterial, false, -1);

                        }

                        ProfilerShort.End();
                    }

                    ProfilerShort.BeginNextBlock("Clear");
                    gizmoSpace.m_cubeMatricesTemp.Clear();
                    gizmoSpace.m_cubeModelsTemp.Clear();

                    ProfilerShort.BeginNextBlock("m_showGizmoCube");
                    if (gizmoSpace.m_showGizmoCube)
                    {
                        #region Draw_mount_points
                        // Draw mount points of added cube block as yellow squares in neighboring cells.
                        if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS)
                        {
                            float cubeSize = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize);
                            if (!PlacingSmallGridOnLargeStatic)
                                cubeSize = CurrentGrid.GridSize;

                            DrawMountPoints(cubeSize, CurrentBlockDefinition, ref drawMatrix);
                        }
                        #endregion

                        Vector3D rotatedModelOffset;
                        Vector3D.TransformNormal(ref CurrentBlockDefinition.ModelOffset, ref gizmoSpace.m_worldMatrixAdd, out rotatedModelOffset);

                        modelTransform.Translation = worldCenter + CurrentGrid.GridScale * rotatedModelOffset;

                        //Render add gizmo for model
                        AddFastBuildModels(gizmoSpace, modelTransform, gizmoSpace.m_cubeMatricesTemp, gizmoSpace.m_cubeModelsTemp, gizmoSpace.m_blockDefinition);

                        Debug.Assert(gizmoSpace.m_cubeMatricesTemp.Count == gizmoSpace.m_cubeModelsTemp.Count);
                        for (int i = 0; i < gizmoSpace.m_cubeMatricesTemp.Count; ++i)
                        {
                            string model = gizmoSpace.m_cubeModelsTemp[i];
                            if (!string.IsNullOrEmpty(model))
                                m_renderData.AddInstance(MyModel.GetId(model), gizmoSpace.m_cubeMatricesTemp[i], ref m_invGridWorldMatrix);
                        }
                    }

                    if (gizmoSpace.SymmetryPlane == MySymmetrySettingModeEnum.Disabled)
                    {
                        ProfilerShort.BeginNextBlock("CalculateRotationHints");
                        m_rotationHints.CalculateRotationHints(modelTransform, localAABB, !MyHud.MinimalHud && !MyHud.CutsceneHud && MySandboxGame.Config.RotationHints && draw && MyFakes.ENABLE_ROTATION_HINTS);
                    }

                    ProfilerShort.End();
                }
                ProfilerShort.End();
            }

            #region AfterAdd
            ProfilerShort.BeginNextBlock("After Add");
            if (gizmoSpace.m_startRemove != null && gizmoSpace.m_continueBuild != null)
            {
                gizmoSpace.m_buildAllowed = true;

                ProfilerShort.Begin("DrawRemovingCubes");
                DrawRemovingCubes(gizmoSpace.m_startRemove, gizmoSpace.m_continueBuild, gizmoSpace.m_removeBlock);
                ProfilerShort.End();
            }
            else if (remove && gizmoSpace.m_showGizmoCube && ShowRemoveGizmo)
            {
                ProfilerShort.Begin("ShowRemoveGizmo");
                if (gizmoSpace.m_removeBlocksInMultiBlock.Count > 0)
                {
                    m_tmpBlockPositionsSet.Clear();

                    GetAllBlocksPositions(gizmoSpace.m_removeBlocksInMultiBlock, m_tmpBlockPositionsSet);

                    foreach (var position in m_tmpBlockPositionsSet)
                        DrawSemiTransparentBox(position, position, CurrentGrid, red, lineMaterial: "GizmoDrawLineRed");

                    m_tmpBlockPositionsSet.Clear();
                }
                else if (gizmoSpace.m_removeBlock != null)
                {
                    if (!MyFakes.ENABLE_VR_BUILDING)
                        DrawSemiTransparentBox(CurrentGrid, gizmoSpace.m_removeBlock, red, lineMaterial: "GizmoDrawLineRed");
                }

                if (gizmoSpace.m_removeBlock != null && MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_REMOVE_CUBE_COORDS)
                {
                    var block = gizmoSpace.m_removeBlock;
                    var grid = block.CubeGrid;
                    Matrix gridMatrix = grid.WorldMatrix;
                    Vector3 blockWorldPos = Vector3.Transform(block.Position * grid.GridSize, gridMatrix);

                    // Show forward-up
                    //if (block.FatBlock != null)
                    //{
                    //    MyRenderProxy.DebugDrawLine3D(block.FatBlock.WorldMatrix.Translation, block.FatBlock.WorldMatrix.Translation + block.FatBlock.WorldMatrix.Forward, Color.Red, Color.Red, false);
                    //    MyRenderProxy.DebugDrawLine3D(block.FatBlock.WorldMatrix.Translation, block.FatBlock.WorldMatrix.Translation + block.FatBlock.WorldMatrix.Up, Color.Green, Color.Green, false);
                    //}

                    MyRenderProxy.DebugDrawText3D(blockWorldPos, block.Position.ToString(), Color.White, 1.0f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                }
                ProfilerShort.End();
            }
            else
            {
                ProfilerShort.Begin("SurvivalMode distance check");
                if (MySession.Static.SurvivalMode && !MySession.Static.CreativeToolsEnabled(Sync.MyId) && 
                    (!CameraControllerSpectator || MyFinalBuildConstants.IS_OFFICIAL))
                {
                    Vector3 localMin = (m_gizmo.SpaceDefault.m_min - new Vector3(0.5f)) * CurrentGrid.GridSize;
                    Vector3 localMax = (m_gizmo.SpaceDefault.m_max + new Vector3(0.5f)) * CurrentGrid.GridSize;
                    BoundingBoxD gizmoBox = new BoundingBoxD(localMin, localMax);

                    if (!MyCubeBuilderGizmo.DefaultGizmoCloseEnough(ref m_invGridWorldMatrix, gizmoBox, CurrentGrid.GridSize, IntersectionDistance))
                    {
                        gizmoSpace.m_removeBlock = null;
                    }
                }
                ProfilerShort.End();
            }
            ProfilerShort.End();
            //*/
            #endregion
            gizmoSpace.m_animationProgress += m_animationSpeed;
        }

        private bool IntersectsCharacterOrCamera(MyCubeBuilderGizmo.MyGizmoSpaceProperties gizmoSpace, float gridSize, ref MatrixD inverseBlockInGridWorldMatrix)
        {
            if (CurrentBlockDefinition == null)
                return false;

            bool intersects = false;

            if (MySector.MainCamera != null)
            {
                intersects = m_gizmo.PointInsideGizmo(MySector.MainCamera.Position, gizmoSpace.SourceSpace, ref inverseBlockInGridWorldMatrix, gridSize, 
                        inflate: 0.05f, onVoxel: CurrentVoxelBase != null, dynamicMode: DynamicMode);
            }

            if (intersects)
                return true;

            if (MySession.Static.ControlledEntity != null && MySession.Static.ControlledEntity is MyCharacter)
            {
                m_collisionTestPoints.Clear();
                PrepareCharacterCollisionPoints(m_collisionTestPoints);
                intersects = m_gizmo.PointsAABBIntersectsGizmo(m_collisionTestPoints, gizmoSpace.SourceSpace, ref inverseBlockInGridWorldMatrix, gridSize, 
                        inflate: 0.05f, onVoxel: CurrentVoxelBase != null, dynamicMode: DynamicMode);
            }

            return intersects;
        }

        public static bool CheckValidBlockRotation(Matrix localMatrix, MyBlockDirection blockDirection, MyBlockRotation blockRotation)
        {
            Vector3I forward = Vector3I.Round(localMatrix.Forward);
            Vector3I up = Vector3I.Round(localMatrix.Up);

            int forwardDot = Vector3I.Dot(ref forward, ref forward);
            int upDot = Vector3I.Dot(ref up, ref up);

            // Matrix is not aligned to snap directions.
            if (forwardDot > 1 || upDot > 1)
            {
                if (blockDirection == MyBlockDirection.Both)
                    return true;
                return false;
            }

            if (blockDirection == MyBlockDirection.Horizontal)
            {
                if (forward == Vector3I.Up || forward == -Vector3I.Up)
                    return false;

                if (blockRotation == MyBlockRotation.Vertical && up != Vector3I.Up)
                    return false;
            }

            return true;
        }

        public static bool CheckValidBlocksRotation(Matrix gridLocalMatrix, MyCubeGrid grid)
        {
            Matrix blockLocalMatrix;
            bool retval = true;

            foreach (var block in grid.GetBlocks())
            {
                MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                if (compoundBlock != null) 
                {
                    foreach (var blockInCompound in compoundBlock.GetBlocks())
                    {
                        blockInCompound.Orientation.GetMatrix(out blockLocalMatrix);
                        blockLocalMatrix = blockLocalMatrix * gridLocalMatrix;

                        retval = retval && CheckValidBlockRotation(blockLocalMatrix, blockInCompound.BlockDefinition.Direction, blockInCompound.BlockDefinition.Rotation);
                        if (!retval)
                            break;
                    }
                }
                else 
                {
                    block.Orientation.GetMatrix(out blockLocalMatrix);
                    blockLocalMatrix = blockLocalMatrix * gridLocalMatrix;

                    retval = retval && CheckValidBlockRotation(blockLocalMatrix, block.BlockDefinition.Direction, block.BlockDefinition.Rotation);
                }

                if (!retval)
                    break;
            }

            return retval;
        }

        #endregion

        #region Build

        protected HashSet<MyCubeGrid.MyBlockLocation> m_blocksBuildQueue = new HashSet<MyCubeGrid.MyBlockLocation>();
        protected List<Vector3I> m_tmpBlockPositionList = new List<Vector3I>();
        protected List<Tuple<Vector3I, ushort>> m_tmpCompoundBlockPositionIdList = new List<Tuple<Vector3I, ushort>>();
        protected HashSet<Vector3I> m_tmpBlockPositionsSet = new HashSet<Vector3I>();

        public virtual void Add()
        {
            // Cannot build if no block is selected
            if (CurrentBlockDefinition == null)
                return;

            m_blocksBuildQueue.Clear();

            var playUnableSound = true;

            foreach (var gizmoSpace in m_gizmo.Spaces)
            {
                if (BuildInputValid && !MyEntities.MemoryLimitReachedReport)
                {
                    if (!gizmoSpace.Enabled)
                        continue;

                    if ((gizmoSpace.m_buildAllowed && MyCubeBuilder.Static.canBuild))
                    {
                        playUnableSound = false;

                        AddBlocksToBuildQueueOrSpawn(gizmoSpace);
                    }
                }
            }

            if (playUnableSound)
                MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);

            if (m_blocksBuildQueue.Count > 0)
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudPlaceBlock);
                if (MyMusicController.Static != null)
                    MyMusicController.Static.Building(2000);
                CurrentGrid.BuildBlocks(MyPlayer.SelectedColor, m_blocksBuildQueue, MySession.Static.LocalCharacterEntityId, MySession.Static.LocalPlayerId);
            }
        }

        private bool IsWithinWorldLimits(long ownerID, string name)
        {
            if (!MySession.Static.EnableBlockLimits) return true;

            var identity = MySession.Static.Players.TryGetIdentity(ownerID);
            bool withinLimits = true;
            if (MySession.Static.MaxBlocksPerPlayer != 0 && identity != null)
            {
                withinLimits &= identity.BlocksBuilt < MySession.Static.MaxBlocksPerPlayer + identity.BlockLimitModifier;
            }
            short typeLimit = MySession.Static.GetBlockTypeLimit(name);
            int typeBuilt;
            if (identity != null && typeLimit > 0)
            {
                withinLimits &= (identity.BlockTypeBuilt.TryGetValue(name, out typeBuilt) ? typeBuilt : 0) < typeLimit;
            }
            return withinLimits;
        }

        protected bool AddBlocksToBuildQueueOrSpawn(MyCubeBlockDefinition blockDefinition, ref MatrixD worldMatrixAdd, Vector3I min, Vector3I max, Vector3I center, Quaternion localOrientation)
        {
            bool added = false;
            BuildData position = new BuildData();

            if (!IsWithinWorldLimits(MySession.Static.LocalPlayerId, blockDefinition.BlockPairName))
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
                MyHud.Notifications.Add(MyNotificationSingletons.ShipOverLimits);
                return false;
            }

            if (GridAndBlockValid)
            {
                if (PlacingSmallGridOnLargeStatic)
                {
                    MatrixD gridWorldMatrix = worldMatrixAdd;
                    position.Position = gridWorldMatrix.Translation;
                    if (MySession.Static.ControlledEntity != null)
                        position.Position -= MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition();
                    else
                        position.AbsolutePosition = true;
                    position.Forward = (Vector3) gridWorldMatrix.Forward;
                    position.Up = (Vector3) gridWorldMatrix.Up;

                    MyMultiplayer.RaiseStaticEvent(s => RequestGridSpawn, new Author(MySession.Static.LocalCharacterEntityId, MySession.Static.LocalPlayerId), (DefinitionIdBlit)blockDefinition.Id, position, MySession.Static.CreativeToolsEnabled(Sync.MyId), true, MyPlayer.SelectedColor.PackHSVToUint());
                }
                else
                {
                    m_blocksBuildQueue.Add(new MyCubeGrid.MyBlockLocation(blockDefinition.Id, min, max, center,
                        localOrientation, MyEntityIdentifier.AllocateId(), MySession.Static.LocalPlayerId));
                }

                added = true;
            }
            else
            {

                position.Position = worldMatrixAdd.Translation;
                if (MySession.Static.ControlledEntity != null)
                    position.Position -= MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition();
                else
                    position.AbsolutePosition = true;
                position.Forward = worldMatrixAdd.Forward;
                position.Up = worldMatrixAdd.Up;

                MyMultiplayer.RaiseStaticEvent(s => RequestGridSpawn, new Author(MySession.Static.LocalCharacterEntityId, MySession.Static.LocalPlayerId), (DefinitionIdBlit)blockDefinition.Id, position, MySession.Static.CreativeToolsEnabled(Sync.MyId), false, MyPlayer.SelectedColor.PackHSVToUint());
                MyGuiAudio.PlaySound(MyGuiSounds.HudPlaceBlock);
                added = true;
            }

            return added;

        }

        private bool AddBlocksToBuildQueueOrSpawn(MyCubeBuilderGizmo.MyGizmoSpaceProperties gizmoSpace)
        {
            return AddBlocksToBuildQueueOrSpawn(gizmoSpace.m_blockDefinition, ref gizmoSpace.m_worldMatrixAdd, gizmoSpace.m_min, gizmoSpace.m_max, gizmoSpace.m_centerPos, gizmoSpace.LocalOrientation);
        }

        private void UpdateGizmos(bool addPos, bool removePos, bool draw)
        {
            if (CurrentBlockDefinition == null)
                return;

            if (CurrentGrid != null && CurrentGrid.Physics != null && CurrentGrid.Physics.RigidBody.HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT))
                return;

            m_gizmo.SpaceDefault.m_blockDefinition = CurrentBlockDefinition;

            m_gizmo.EnableGizmoSpaces(CurrentBlockDefinition, CurrentGrid, UseSymmetry);

            m_renderData.ClearInstanceData();
            m_rotationHints.Clear();
            int gizmoCt = m_gizmo.Spaces.Length;
            if(CurrentGrid!=null)
                m_invGridWorldMatrix = MatrixD.Invert(CurrentGrid.WorldMatrix);
            for (int i = 0; i < gizmoCt; i++)
            {
                var gizmoSpace = m_gizmo.Spaces[i];

                ProfilerShort.Begin("Prepare update");
                bool spaceAddPos = addPos && BuildInputValid;

                if (gizmoSpace.SymmetryPlane == MySymmetrySettingModeEnum.Disabled)
                {
                    Quaternion quatOrientation = gizmoSpace.LocalOrientation;
                    if (!PlacingSmallGridOnLargeStatic && CurrentGrid != null && !MySession.Static.CreativeToolsEnabled(Sync.MyId))
                        spaceAddPos &= CurrentGrid.CanAddCube(gizmoSpace.m_addPos, new MyBlockOrientation(ref quatOrientation), CurrentBlockDefinition); //GK: TODOK Check if this is needed in the first place.
                }
                else
                {
                    spaceAddPos &= UseSymmetry;
                    removePos &= UseSymmetry;
                }

                ProfilerShort.BeginNextBlock("UpdateGizmo");
                UpdateGizmo(gizmoSpace, spaceAddPos || FreezeGizmo, removePos || FreezeGizmo, draw);
                ProfilerShort.End();
            }
        }

        public MyOrientedBoundingBoxD GetBuildBoundingBox(float inflate = 0.0f)
        {
            if (m_gizmo.SpaceDefault.m_blockDefinition == null)
                return new MyOrientedBoundingBoxD();
            float scale = MyDefinitionManager.Static.GetCubeSize(m_gizmo.SpaceDefault.m_blockDefinition.CubeSize);
            Vector3 halfExtents = m_gizmo.SpaceDefault.m_blockDefinition.Size * scale * 0.5f + inflate;

            MatrixD m = m_gizmo.SpaceDefault.m_worldMatrixAdd;

            // CH: Don't ask me why this correction has to be applied, but it's apparently correct :-)
            if (m_gizmo.SpaceDefault.m_removeBlock != null && !m_gizmo.SpaceDefault.m_addPosSmallOnLarge.HasValue)
            {
                var block = m_gizmo.SpaceDefault.m_removeBlock;
                var pos = Vector3D.Transform(m_gizmo.SpaceDefault.m_addPos * scale, block.CubeGrid.PositionComp.WorldMatrix);
                m.Translation = pos;
            }
            Vector3D minPos = Vector3D.Zero - halfExtents;
            Vector3D maxPos = Vector3D.Zero + halfExtents;
            return new MyOrientedBoundingBoxD(new BoundingBoxD((Vector3D)minPos, (Vector3D)maxPos), m);
        }

        public virtual bool CanStartConstruction(MyEntity buildingEntity)
        {
                var addMatrix = m_gizmo.SpaceDefault.m_worldMatrixAdd;
                BuildComponent.GetGridSpawnMaterials(CurrentBlockDefinition, addMatrix, false);
                return BuildComponent.HasBuildingMaterials(buildingEntity);
            }

        public virtual bool AddConstruction(MyEntity builder)
        {
            var controllingPlayer = Sync.Players.GetControllingPlayer(builder);

            if (canBuild == false ||(controllingPlayer  != null && controllingPlayer.IsLocalPlayer == false)) return false;
     
            if (controllingPlayer == null || controllingPlayer.IsRemotePlayer)
            {
                var cockpit = (builder as MyCharacter).IsUsing;
                if (cockpit != null)
                {
                    controllingPlayer = Sync.Players.GetControllingPlayer(cockpit);
                    if (controllingPlayer == null || controllingPlayer.IsRemotePlayer)
                    {
                        Debug.Assert(controllingPlayer != null && controllingPlayer.IsLocalPlayer, "Only local players can call AddConstruction!");
                        return false;
                    }
                }
                else
                {
                    Debug.Assert(controllingPlayer != null && controllingPlayer.IsLocalPlayer, "Only local players can call AddConstruction! (cockpit is null)");
                    return false;
                }
            }

            var gizmoSpace = m_gizmo.SpaceDefault;

            if (gizmoSpace.Enabled && BuildInputValid && gizmoSpace.m_buildAllowed && canBuild && !MyEntities.MemoryLimitReachedReport)
            {
                m_blocksBuildQueue.Clear();
                bool added = AddBlocksToBuildQueueOrSpawn(gizmoSpace);
                if (added)
                {
                    if (CurrentGrid != null && m_blocksBuildQueue.Count > 0)
                    {
                        if (MySession.Static != null && builder == MySession.Static.LocalCharacter && MyMusicController.Static != null)
                            MyMusicController.Static.Building(2000);
                        MyGuiAudio.PlaySound(MyGuiSounds.HudPlaceBlock);
                        if (builder == MySession.Static.LocalCharacter)
                            MySession.Static.TotalBlocksCreated++;
                        CurrentGrid.BuildBlocks(MyPlayer.SelectedColor, m_blocksBuildQueue, builder.EntityId, controllingPlayer.Identity.IdentityId);
                    }
                }
                return added;
            }

            return false;
        }

        private Vector3I MakeCubePosition(Vector3 position)
        {
            position = position - CurrentGrid.WorldMatrix.Translation;
            Vector3 size = new Vector3(CurrentGrid.GridSize);
            Vector3 div = position / size;
            Vector3I posInt;
            posInt.X = (int)Math.Round(div.X);
            posInt.Y = (int)Math.Round(div.Y);
            posInt.Z = (int)Math.Round(div.Z);
            return posInt;
        }

        public void GetAddPosition(out Vector3D position)
        {
            position = m_gizmo.SpaceDefault.m_worldMatrixAdd.Translation;
        }

        public virtual bool GetAddAndRemovePositions(float gridSize, bool placingSmallGridOnLargeStatic, out Vector3I addPos, out Vector3? addPosSmallOnLarge, out Vector3I addDir, out Vector3I removePos, out MySlimBlock removeBlock,
            out ushort? compoundBlockId, HashSet<Tuple<MySlimBlock, ushort?>> removeBlocksInMultiBlock)
        {
            bool result = false;

            addPosSmallOnLarge = null;
            removePos = new Vector3I();
            removeBlock = null;

            MySlimBlock intersectedBlock;
            Vector3D intersectedBlockPos;
            Vector3D intersectionBlockExact;
            result = GetBlockAddPosition(gridSize, placingSmallGridOnLargeStatic, out intersectedBlock, out intersectedBlockPos, out intersectionBlockExact, out addPos, out addDir, out compoundBlockId);
            Debug.Assert(intersectedBlock == null || intersectedBlock.CubeGrid == CurrentGrid);
            if (MySession.Static.ControlledEntity is MyShipController && m_currentGrid.Equals((MySession.Static.ControlledEntity as MyShipController).CubeGrid)) return false;

            float currentGridSize = placingSmallGridOnLargeStatic ? CurrentGrid.GridSize : gridSize;

            if (result && (MaxGridDistanceFrom == null
                || Vector3D.DistanceSquared(intersectionBlockExact * currentGridSize, Vector3.Transform(MaxGridDistanceFrom.Value, m_invGridWorldMatrix)) < (CubeBuilderDefinition.MaxBlockBuildingDistance * CubeBuilderDefinition.MaxBlockBuildingDistance)))
            {
                removePos = Vector3I.Round(intersectedBlockPos);
                removeBlock = intersectedBlock;
            }
            else if (AllowFreeSpacePlacement && CurrentGrid != null)
            {
                Vector3D intersectionPos = IntersectionStart + IntersectionDirection * Math.Min(FreeSpacePlacementDistance, IntersectionDistance);
                addPos = MakeCubePosition(intersectionPos);
                addDir = new Vector3I(0, 0, 1);
                removePos = addPos - addDir;
                removeBlock = CurrentGrid.GetCubeBlock(removePos);

                result = true;
            }
            else
            {
                result = false;
            }
            if (MyCubeBuilder.Static.canBuild == false) return false;

            // Placing small on large grid
            if (result && placingSmallGridOnLargeStatic)
            {
                MatrixD placingRotationMatrix = Matrix.Identity;
                if (intersectedBlock != null) 
                {
                    placingRotationMatrix = intersectedBlock.CubeGrid.WorldMatrix.GetOrientation();

                    if (intersectedBlock.FatBlock != null) 
                    {
                        if (compoundBlockId != null) 
                        {
                            MyCompoundCubeBlock cmpCubeBlock = intersectedBlock.FatBlock as MyCompoundCubeBlock;
                            if (cmpCubeBlock != null)
                            {
                                var blockInCompound = cmpCubeBlock.GetBlock(compoundBlockId.Value);
                                if (blockInCompound != null && blockInCompound.FatBlock.Components.Has<MyFractureComponentBase>())
                                    return false;
                            }
                        }
                        else 
                        {
                            if (intersectedBlock.FatBlock.Components.Has<MyFractureComponentBase>())
                                return false;
                        }
                    }
                }
                MatrixD placingRotationMatrixInv = MatrixD.Invert(placingRotationMatrix);

                if (m_hitInfo.HasValue)
                {
                    Vector3 hitInfoNormal = Vector3.TransformNormal(m_hitInfo.Value.HkHitInfo.Normal,m_invGridWorldMatrix);
                    addDir = Vector3I.Sign(Vector3.DominantAxisProjection(hitInfoNormal));
                }

                // Because intersection can be out of cube (on edges) then we must clamp intersection inside (we need that created block will touch side not edge with target block)
                // Calculated in local intersected object coordinates
                Vector3 sideCenter = removePos + 0.5f * addDir;
                Vector3 sideCenterToIntersectionExact = intersectionBlockExact - sideCenter;
                const float clampSideValue = 0.495f;
                // Direction axis filter (use the same value)
                Vector3I dirFilter = Vector3I.Abs(addDir);
                // Inverse direction filter (use clamped side values)
                Vector3I invDirFilter = Vector3I.One - dirFilter;
                sideCenterToIntersectionExact = invDirFilter * Vector3.Clamp(sideCenterToIntersectionExact, new Vector3(-clampSideValue), new Vector3(clampSideValue))
                    + dirFilter * sideCenterToIntersectionExact;

                Vector3D localIntersectionExact = sideCenter + sideCenterToIntersectionExact;
                float smallToLarge = gridSize / CurrentGrid.GridSize;
                // Note that there is 0.1 coef instead of 0.5 - because we need that small block is into large one when intersection is not on sides.
                float moveAtNormal = MyFakes.ENABLE_VR_BUILDING ? 0.25f : 0.1f;
                Vector3I addPosSmallOnLargeInt = Vector3I.Round((localIntersectionExact + moveAtNormal * smallToLarge * addDir - smallToLarge * Vector3.Half) / smallToLarge);
                addPosSmallOnLarge = smallToLarge * addPosSmallOnLargeInt + smallToLarge * Vector3.Half;
            }



            Debug.Assert(!result || addDir != Vector3I.Zero, "Direction vector cannot be zero");
            return result;
        }

        protected virtual void PrepareBlocksToRemove()
        {

            m_tmpBlockPositionList.Clear();
            m_tmpCompoundBlockPositionIdList.Clear();

                foreach (var gizmoSpace in m_gizmo.Spaces)
                {
                    if (!gizmoSpace.Enabled)
                        continue;

                    if (GridAndBlockValid && gizmoSpace.m_removeBlock != null && (gizmoSpace.m_removeBlock.FatBlock == null || !gizmoSpace.m_removeBlock.FatBlock.IsSubBlock))
                    {
                        Debug.Assert(CurrentGrid == gizmoSpace.m_removeBlock.CubeGrid);
                        if (CurrentGrid == gizmoSpace.m_removeBlock.CubeGrid)
                        {
                            if (gizmoSpace.m_removeBlocksInMultiBlock.Count > 0)
                            {
                                foreach (var tuple in gizmoSpace.m_removeBlocksInMultiBlock)
                                    RemoveBlock(tuple.Item1, tuple.Item2);
                            }
                            else
                            {
                                RemoveBlock(gizmoSpace.m_removeBlock, gizmoSpace.m_blockIdInCompound, checkExisting: true);
                            }

                            gizmoSpace.m_removeBlock = null;
                            gizmoSpace.m_removeBlocksInMultiBlock.Clear();
                        }
                    }
                }
            }

        protected void Remove()
        {
            if (m_tmpBlockPositionList.Count > 0 || m_tmpCompoundBlockPositionIdList.Count > 0)
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudDeleteBlock);

                if (m_tmpBlockPositionList.Count > 0)
                {
                    CurrentGrid.RazeBlocks(m_tmpBlockPositionList);
                    m_tmpBlockPositionList.Clear();
                }

                if (m_tmpCompoundBlockPositionIdList.Count > 0)
                    CurrentGrid.RazeBlockInCompoundBlock(m_tmpCompoundBlockPositionIdList);
            }
        }

        protected void RemoveBlock(MySlimBlock block, ushort? blockIdInCompound, bool checkExisting = false)
        {
            if (block != null && (block.FatBlock == null || !block.FatBlock.IsSubBlock))
            {
                if (block.FatBlock is MyCompoundCubeBlock)
                {
                    if (blockIdInCompound.HasValue)
                    {
                        if (!checkExisting || !m_tmpCompoundBlockPositionIdList.Exists(t => t.Item1 == block.Min && t.Item2 == blockIdInCompound.Value))
                            m_tmpCompoundBlockPositionIdList.Add(new Tuple<Vector3I, ushort>(block.Min, blockIdInCompound.Value));
                    }
                    else
                    {
                        // Remove whole compound
                        if (!checkExisting || !m_tmpBlockPositionList.Contains(block.Min))
                            m_tmpBlockPositionList.Add(block.Min);
                    }
                }
                else
                {
                    if (!checkExisting || !m_tmpBlockPositionList.Contains(block.Min))
                        m_tmpBlockPositionList.Add(block.Min);
                }
            }
        }

        void Change(int expand = 0)
        {
            ProfilerShort.Begin("MyCubeBuilder.Change");
            m_tmpBlockPositionList.Clear();

            //Repaint ALL
            if (expand == -1)
            {
                CurrentGrid.ColorGrid(MyPlayer.SelectedColor, true);
            }

            int count = -1;
            bool playSound = false;
            foreach (var gizmoSpace in m_gizmo.Spaces)
            {
                ++count;
                if (!gizmoSpace.Enabled)
                    continue;

                if (gizmoSpace.m_removeBlock != null)
                {
                    playSound = false;

                    Vector3I start = gizmoSpace.m_removeBlock.Position - Vector3I.One * expand;
                    Vector3I end = gizmoSpace.m_removeBlock.Position + Vector3I.One * expand;

                    if ((m_currColoringArea[count].Start != start) || (m_currColoringArea[count].End != end))
                    {
                        m_currColoringArea[count].Start = start;
                        m_currColoringArea[count].End = end;

                        playSound = true;
                    }

					CurrentGrid.ColorBlocks(start, end, MyPlayer.SelectedColor, playSound);
                }
            }
            ProfilerShort.End();
        }

        bool IsInSymmetrySettingMode
        {
            get { return m_symmetrySettingMode != MySymmetrySettingModeEnum.NoPlane; }
        }

        /// <summary>
        /// Indicates if cube block size is avaliable for current cube builder state.
        /// </summary>
        /// <param name="blockDef">Block definition to check for.</param>
        /// <returns>True if it is avaliable.</returns>
        public bool IsCubeSizeAvailable(MyCubeBlockDefinition blockDef)
        {

            if (blockDef == null)
                return false;

            if (!IsCubeSizeModesAvailable)
                return true;

            var group = MyDefinitionManager.Static.GetDefinitionGroup(blockDef.BlockPairName);

            if (this.CubeBuilderState == null)
                return true;

            bool available = (this.CubeBuilderState.CubeSizeMode == MyCubeSize.Large && @group.Large != null && (@group.Large.Public || MyFakes.ENABLE_NON_PUBLIC_BLOCKS)) ||
                            this.CubeBuilderState.CubeSizeMode == MyCubeSize.Small && @group.Small != null && (@group.Small.Public || MyFakes.ENABLE_NON_PUBLIC_BLOCKS);

            return available;
        }

        #endregion

        #region Update

        Vector3I? GetSingleMountPointNormal()
        {
			if (CurrentBlockDefinition == null)
                return null;

			var currentBlockMountPoints = CurrentBlockDefinition.GetBuildProgressModelMountPoints(1.0f);
			if (currentBlockMountPoints == null || currentBlockMountPoints.Length == 0)
				return null;

			var normal = currentBlockMountPoints[0].Normal;
            //if (m_alignToDefault)
            //{
            //    for (int i = m_lastDefault; i < currentBlockMountPoints.Length + m_lastDefault; i++)
            //    {
            //        int index = i % currentBlockMountPoints.Length;
            //        if (currentBlockMountPoints[index].Default)
            //        {
            //            m_lastDefault = index;
            //            return currentBlockMountPoints[index].Normal;
            //        }
            //    }
            //    for (int i = m_lastDefault; i < currentBlockMountPoints.Length + m_lastDefault; i++)
            //    {
            //        int index = i % currentBlockMountPoints.Length;
            //        if (MyCubeBlockDefinition.NormalToBlockSide(currentBlockMountPoints[index].Normal) == BlockSideEnum.Bottom)
            //        {
            //            m_lastDefault = index;
            //            return currentBlockMountPoints[index].Normal;
            //        }
            //    }
            //}

            if (m_alignToDefault && !m_customRotation)
            {
                for (int i = 0; i < currentBlockMountPoints.Length; i++)
                {
                    if (currentBlockMountPoints[i].Default)
                    {
                        return currentBlockMountPoints[i].Normal;
                    }
                }
                for (int i = 0; i < currentBlockMountPoints.Length; i++)
                {
                    if (MyCubeBlockDefinition.NormalToBlockSide(currentBlockMountPoints[i].Normal) == BlockSideEnum.Bottom)
                    {
                        return currentBlockMountPoints[i].Normal;
                    }
                }
            }


            var oppositeNormal = -normal;
            switch (CurrentBlockDefinition.AutorotateMode)
            {
                case MyAutorotateMode.OneDirection:
					for (int i = 1; i < currentBlockMountPoints.Length; i++)
                    {
						var currentNormal = currentBlockMountPoints[i].Normal;
                        if (currentNormal != normal)
                            return null;
                    }
                    break;

                case MyAutorotateMode.OppositeDirections:
					for (int i = 1; i < currentBlockMountPoints.Length; i++)
                    {
						var currentNormal = currentBlockMountPoints[i].Normal;
                        if (currentNormal != normal && currentNormal != oppositeNormal)
                            return null;
                    }
                    break;

                case MyAutorotateMode.FirstDirection:
                    break;

                default:
                    Debug.Fail("Invalid branch.");
                    return null;
            }

            return normal;
        }

        protected void CalculateLocalCoordAndMode()
        {
            Vector3D freePlacementIntersectionPoint = IntersectionStart + IntersectionDistance * IntersectionDirection;

            if (!IsActivated || CurrentBlockDefinition == null)
            {
                return;
            }

            ChooseHitObject();

            if (m_hitInfo != null)
                freePlacementIntersectionPoint = m_hitInfo.Value.Position;

            float gridSize = 0;
            if (CurrentBlockDefinition != null)
                gridSize = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize);

            m_lastLocalCoordSysData = MyCoordinateSystem.Static.SnapWorldPosToClosestGrid(ref freePlacementIntersectionPoint, gridSize, CubeBuilderDefinition.BuildingSettings.StaticGridAlignToCenter);
            // Blocks should snap to local coordinates if Voxel is hit.
            bool snapToLocalCoords = CurrentVoxelBase != null && MyCoordinateSystem.Static.LocalCoordExist;

            bool dynamicOverride = IsDynamicOverride();

            // Dynamic mode if no possible way to snap to local coords and no grid is hit.
            DynamicMode = !snapToLocalCoords && CurrentGrid == null || dynamicOverride;
        }

        public override void UpdateBeforeSimulation()
        {
            ProfilerShort.Begin("Setup");
            //var normal = GetSingleMountPointNormal();
            //// Gizmo add dir can be zero in some cases
            //if (normal.HasValue && (GridAndBlockValid || VoxelMapAndBlockValid) && m_gizmo.SpaceDefault.m_addDir != Vector3I.Zero) //TODO: do something with it
            //{
            //    m_gizmo.SetupLocalAddMatrix(m_gizmo.SpaceDefault, normal.Value);
            //}
            //UpdateNotificationBlockNotAvailable(changeText: false);
            UpdateNotificationBlockLimit();

            ProfilerShort.End();

            if (MyCubeBuilder.Static.IsActivated && MySession.Static.ControlledEntity is MyShipController)
            {
                if ((MySession.Static.ControlledEntity as MyShipController).hasPower && (MySession.Static.ControlledEntity as MyShipController).BuildingMode)
                {
                    MyCubeBuilder.Static.canBuild = true;
                }
                else
                {
                    MyCubeBuilder.Static.canBuild = false;
                }
            }
            else
            {
                MyCubeBuilder.Static.canBuild = true;
            }

            this.CalculateLocalCoordAndMode();

        }

        protected override void UnloadData()
        {
            base.UnloadData();

            RemoveSymmetryNotification();

            m_gizmo.Clear();

            CurrentGrid = null;

            UnloadRenderObjects();

            m_cubeBuildlerState = null;
        }

        void UnloadRenderObjects()
        {
            m_gizmo.RemoveGizmoCubeParts();

            m_renderData.UnloadRenderObjects();
        }

        /// <summary>
        /// Update notification telling player how many blocks they have left if per player limits are present
        /// </summary>
        private void UpdateNotificationBlockLimit()
        {
            if (MySession.Static.EnableBlockLimits && MySession.Static.MaxBlocksPerPlayer > 0)
            {
                if (IsActivated)
                {
                    var identity = MySession.Static.Players.TryGetIdentity(MySession.Static.LocalPlayerId);
                    if (identity != null)
                    {
                        int maxLimit = MySession.Static.MaxBlocksPerPlayer + identity.BlockLimitModifier;
                        int blocksBuilt = MySession.Static.Players.TryGetIdentity(MySession.Static.LocalPlayerId).BlocksBuilt;
                        if (((float)blocksBuilt / (float)maxLimit) >= 0.9f)
                        {
                        MyHud.BlocksLeft.Start(MyFontEnum.White, Vector2.Zero, Color.White, 1, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
                            MyHud.BlocksLeft.GetStringBuilder().AppendFormat(MyCommonTexts.NotificationBlocksLeft, maxLimit - blocksBuilt);
                        MyHud.BlocksLeft.Visible = true;
                    }
                }
                }
                else
                {
                    MyHud.BlocksLeft.Visible = false;
                }
            }
            else
            {
                MyHud.BlocksLeft.Visible = false;
        }
        }

        public void UpdateNotificationBlockNotAvailable(bool changeText = true)
        {
            if (!MyFakes.ENABLE_NOTIFICATION_BLOCK_NOT_AVAILABLE)
                return;

            bool developerSpectatorBuild = MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.Spectator && !MyFinalBuildConstants.IS_OFFICIAL;
            bool hideNotificationInCockpit = MySession.Static.ControlledEntity != null && MySession.Static.ControlledEntity is MyCockpit && !developerSpectatorBuild;

            if (BlockCreationIsActivated && CurrentGrid != null && CurrentBlockDefinition != null && CurrentBlockDefinition.CubeSize != CurrentGrid.GridSizeEnum && !hideNotificationInCockpit
                && !PlacingSmallGridOnLargeStatic)
            {
                if (changeText || m_blockNotAvailableNotification == null)
                {
                    MyStringId myGrid = (CurrentGrid.GridSizeEnum == MyCubeSize.Small) ? MySpaceTexts.NotificationArgLargeShip : MySpaceTexts.NotificationArgSmallShip;
                    MyStringId targetGrid = (CurrentGrid.GridSizeEnum == MyCubeSize.Small) ? MySpaceTexts.NotificationArgSmallShip
                                                                                                    : (CurrentGrid.IsStatic) ? MySpaceTexts.NotificationArgStation
                                                                                                                                : MySpaceTexts.NotificationArgLargeShip;
                    ShowNotificationBlockNotAvailable(myGrid, CurrentBlockDefinition.DisplayNameText, targetGrid);
                }
                else
                {
                    MyHud.Notifications.Add(m_blockNotAvailableNotification);
                }
            }
            else if(CurrentBlockDefinition != null)
            {
                HideNotificationBlockNotAvailable();
            }

        }

        /// <summary>
        /// Notification visible when looking at grid whose size is nto supported current block.
        /// </summary>
        private void ShowNotificationBlockNotAvailable(MyStringId grid1Text, String blockDisplayName, MyStringId grid2Text)
        {
            if (!MyFakes.ENABLE_NOTIFICATION_BLOCK_NOT_AVAILABLE)
                return;

            if (m_blockNotAvailableNotification == null)
                m_blockNotAvailableNotification = new MyHudNotification(MySpaceTexts.NotificationBlockNotAvailableFor, 2500, font: MyFontEnum.Red, priority: 1);

            m_blockNotAvailableNotification.SetTextFormatArguments(MyTexts.Get(grid1Text).ToLower().FirstLetterUpperCase(), blockDisplayName.ToLower(), MyTexts.Get(grid2Text).ToLower()); 
            MyHud.Notifications.Add(m_blockNotAvailableNotification);
        }

        private void HideNotificationBlockNotAvailable()
        {
            MyHud.Notifications.Remove(m_blockNotAvailableNotification);
        }

        #endregion

        #region Continuous building

        public virtual void StartBuilding()
        {
            StartBuilding(ref m_gizmo.SpaceDefault.m_startBuild, m_gizmo.SpaceDefault.m_startRemove);
        }

        /// <summary>
        /// Starts continuous building. Do not put any gizmo related stuff here.
        /// </summary>
        protected void StartBuilding(ref Vector3I? startBuild, Vector3I? startRemove)
        {
            if ((!GridAndBlockValid && !VoxelMapAndBlockValid) || PlacingSmallGridOnLargeStatic || MyEntities.MemoryLimitReachedReport)
                return;

            Vector3I addPos;
            Vector3? addPosSmallOnLarge;
            Vector3I removePos;
            Vector3I dir;
            MySlimBlock removeBlock;
            ushort? compoundBlockId;

            m_initialIntersectionStart = IntersectionStart;
            m_initialIntersectionDirection = IntersectionDirection;
            float gridSize = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize);
            if (startRemove == null && GetAddAndRemovePositions(gridSize, PlacingSmallGridOnLargeStatic, out addPos, out addPosSmallOnLarge, out dir, out removePos, out removeBlock, out compoundBlockId, null))
                startBuild = addPos;
            else
                startBuild = null;
        }

        protected virtual void StartRemoving()
        {
            StartRemoving(m_gizmo.SpaceDefault.m_startBuild, ref m_gizmo.SpaceDefault.m_startRemove);
        }

        /// <summary>
        /// Starts continuous removing. Do not put any gizmo related stuff here.
        /// </summary>
        protected void StartRemoving(Vector3I? startBuild, ref Vector3I? startRemove)
        {
            if (PlacingSmallGridOnLargeStatic)
                return;

            m_initialIntersectionStart = IntersectionStart;
            m_initialIntersectionDirection = IntersectionDirection;
            if (CurrentGrid != null && startBuild == null)
            {
                double dst;
                startRemove = IntersectCubes(CurrentGrid, out dst);
            }
        }

        public virtual void ContinueBuilding(bool planeBuild)
        {
            var defaulGizmoSpace = m_gizmo.SpaceDefault;
            ContinueBuilding(planeBuild, defaulGizmoSpace.m_startBuild, defaulGizmoSpace.m_startRemove, ref defaulGizmoSpace.m_continueBuild, defaulGizmoSpace.m_min, defaulGizmoSpace.m_max);
        }

        /// <summary>
        /// Continues building/removing. Do not put any gizmo related stuff here.
        /// </summary>
        protected void ContinueBuilding(bool planeBuild, Vector3I? startBuild, Vector3I? startRemove, ref Vector3I? continueBuild, Vector3I blockMinPosision, Vector3I blockMaxPosition)
        {
            if (!startBuild.HasValue && !startRemove.HasValue) 
                return;

            if (!GridAndBlockValid && !VoxelMapAndBlockValid)
                return;

            continueBuild = null;

            // Avoid sudden appearing right after player clicked (wait until he moved mouse at least a little).
            if (CheckSmallViewChange())
                return;

            IntersectInflated(m_cacheGridIntersections, CurrentGrid);

            Vector3I minGizmo = startBuild.HasValue ? blockMinPosision : startRemove.Value;
            Vector3I maxGizmo = startBuild.HasValue ? blockMaxPosition : startRemove.Value;
            Vector3I startValue;
            for (startValue.X = minGizmo.X; startValue.X <= maxGizmo.X; startValue.X++)
                for (startValue.Y = minGizmo.Y; startValue.Y <= maxGizmo.Y; startValue.Y++)
                    for (startValue.Z = minGizmo.Z; startValue.Z <= maxGizmo.Z; startValue.Z++)
                    {
                        if (planeBuild)
                        {   //We have to find intersection with most user friendly normal to camera
                            foreach (Vector3I intersection in m_cacheGridIntersections)
                            {
                                if ((intersection.X == startValue.X)
                                    ||
                                    (intersection.Y == startValue.Y)
                                    ||
                                    (intersection.Z == startValue.Z))
                                {
                                    Vector3 axis1 = Vector3.Zero;
                                    Vector3 axis2 = Vector3.Zero;

                                    if (intersection.X == startValue.X)
                                    {
                                        if (CurrentGrid != null)
                                        {
                                            axis1 = CurrentGrid.WorldMatrix.Up;
                                            axis2 = CurrentGrid.WorldMatrix.Forward;
                                        }
                                        else
                                        {
                                            axis1 = Vector3.Up;
                                            axis2 = Vector3.Forward;
                                        }
                                    }
                                    else
                                        if (intersection.Y == startValue.Y)
                                        {
                                            if (CurrentGrid != null)
                                            {
                                                axis1 = CurrentGrid.WorldMatrix.Right;
                                                axis2 = CurrentGrid.WorldMatrix.Forward;
                                            }
                                            else
                                            {
                                                axis1 = Vector3.Right;
                                                axis2 = Vector3.Forward;
                                            }
                                        }
                                        else
                                            if (intersection.Z == startValue.Z)
                                            {
                                                if (CurrentGrid != null)
                                                {
                                                    axis1 = CurrentGrid.WorldMatrix.Up;
                                                    axis2 = CurrentGrid.WorldMatrix.Right;
                                                }
                                                else
                                                {
                                                    axis1 = Vector3.Up;
                                                    axis2 = Vector3.Right;
                                                }
                                            }

                                    Vector3I counter = Vector3I.Abs(intersection - startValue) + Vector3I.One;

                                    if (counter.Size < MAX_CUBES_BUILT_AT_ONCE && counter.AbsMax() <= MAX_CUBES_BUILT_IN_ONE_AXIS)
                                    {
                                        continueBuild = intersection;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (Vector3I intersection in m_cacheGridIntersections)
                            {
                                if (((intersection.X == startValue.X) &&
                                        (intersection.Y == startValue.Y))
                                    ||
                                    ((intersection.Y == startValue.Y) &&
                                        (intersection.Z == startValue.Z))
                                    ||
                                    ((intersection.X == startValue.X) &&
                                        (intersection.Z == startValue.Z)))
                                {
                                    if ((intersection - startValue + Vector3I.One).AbsMax() <= MAX_CUBES_BUILT_IN_ONE_AXIS)
                                    {
                                        continueBuild = intersection;
                                        break;
                                    }
                                }
                            }
                        }
                    }
        }

        public virtual void StopBuilding()
        {
            if ((!GridAndBlockValid && !VoxelMapAndBlockValid) || MyEntities.MemoryLimitReachedReport)
            {
                foreach (var gizmoSpace in m_gizmo.Spaces)
                {
                    gizmoSpace.m_startBuild = null;
                    gizmoSpace.m_continueBuild = null;
                    gizmoSpace.m_startRemove = null;
                }
                return;
            }

            bool smallViewChange = CheckSmallViewChange();

            m_blocksBuildQueue.Clear();
            m_tmpBlockPositionList.Clear();

            UpdateGizmos(true, true, false);

            int enabledCount = 0;
            foreach (var gizmoSpace in m_gizmo.Spaces)
            {
                if (gizmoSpace.Enabled)
                    ++enabledCount;
            }

            foreach (var gizmoSpace in m_gizmo.Spaces)
            {
                if (!gizmoSpace.Enabled)
                    continue;

                StopBuilding(smallViewChange, ref gizmoSpace.m_startBuild, ref gizmoSpace.m_startRemove, ref gizmoSpace.m_continueBuild, gizmoSpace.m_min, gizmoSpace.m_max,
                    gizmoSpace.m_centerPos, ref gizmoSpace.m_localMatrixAdd, gizmoSpace.m_blockDefinition);
            }

            if (m_blocksBuildQueue.Count > 0)
            {
                CurrentGrid.BuildBlocks(MyPlayer.SelectedColor, m_blocksBuildQueue, MySession.Static.LocalCharacterEntityId, MySession.Static.LocalPlayerId);
                m_blocksBuildQueue.Clear();
            }

            if (m_tmpBlockPositionList.Count > 0)
            {
                CurrentGrid.RazeBlocks(m_tmpBlockPositionList);
                m_tmpBlockPositionList.Clear();
            }
        }

        /// <summary>
        /// Stops continuous building/removing. Do not put any gizmo related stuff here.
        /// </summary>
        protected void StopBuilding(bool smallViewChange, ref Vector3I? startBuild, ref Vector3I? startRemove, ref Vector3I? continueBuild, Vector3I blockMinPosition, Vector3I blockMaxPosition, 
            Vector3I blockCenterPosition, ref Matrix localMatrixAdd, MyCubeBlockDefinition blockDefinition)
        {
            if (startBuild != null && (continueBuild != null || smallViewChange))
            {
                Vector3I min = blockMinPosition - blockCenterPosition;
                Vector3I max = blockMaxPosition - blockCenterPosition;

                Vector3I rotatedSize;
                Vector3I.TransformNormal(ref CurrentBlockDefinition.Size, ref localMatrixAdd, out rotatedSize);
                rotatedSize = Vector3I.Abs(rotatedSize);

                Vector3I stepDelta;
                Vector3I counter;
                int stepCount;

                if (smallViewChange)
                    continueBuild = startBuild;

                ComputeSteps(startBuild.Value, continueBuild.Value, rotatedSize, out stepDelta, out counter, out stepCount);

                Vector3I centerPos = blockCenterPosition;
                Quaternion orientation = Quaternion.CreateFromRotationMatrix(localMatrixAdd);
                MyDefinitionId definitionId = blockDefinition.Id;

                // Blocks can be randomly rotated if line/plane building is used.
                bool allowRandomRotation = blockDefinition.RandomRotation
                    && blockDefinition.Size.X == blockDefinition.Size.Y && blockDefinition.Size.X == blockDefinition.Size.Z
                    && (blockDefinition.Rotation == MyBlockRotation.Both || blockDefinition.Rotation == MyBlockRotation.Vertical);

                if (allowRandomRotation)
                {
                    m_blocksBuildQueue.Clear();

                    Vector3I temp;
                    for (temp.X = 0; temp.X < counter.X; ++temp.X)
                    {
                        for (temp.Y = 0; temp.Y < counter.Y; ++temp.Y)
                        {
                            for (temp.Z = 0; temp.Z < counter.Z; ++temp.Z)
                            {
                                Vector3I tempCenter = blockCenterPosition + temp * stepDelta;
                                Vector3I tempMin = blockMinPosition + temp * stepDelta;
                                Vector3I tempMax = blockMaxPosition + temp * stepDelta;

                                Quaternion tempOrientation;

                                if (blockDefinition.Rotation == MyBlockRotation.Both)
                                {
                                    Base6Directions.Direction forward = (Base6Directions.Direction)(Math.Abs(MyRandom.Instance.Next()) % 6);
                                    Base6Directions.Direction up = forward;

                                    while (Vector3I.Dot(Base6Directions.GetIntVector(forward), Base6Directions.GetIntVector(up)) != 0)
                                        up = (Base6Directions.Direction)(Math.Abs(MyRandom.Instance.Next()) % 6);

                                    tempOrientation = Quaternion.CreateFromForwardUp(Base6Directions.GetIntVector(forward), Base6Directions.GetIntVector(up));
                                }
                                else
                                {
                                    Base6Directions.Direction up = Base6Directions.Direction.Up;
                                    Base6Directions.Direction forward = up;

                                    while (Vector3I.Dot(Base6Directions.GetIntVector(forward), Base6Directions.GetIntVector(up)) != 0)
                                        forward = (Base6Directions.Direction)(Math.Abs(MyRandom.Instance.Next()) % 6);

                                    tempOrientation = Quaternion.CreateFromForwardUp(Base6Directions.GetIntVector(forward), Base6Directions.GetIntVector(up));
                                }

                                m_blocksBuildQueue.Add(new MyCubeGrid.MyBlockLocation(blockDefinition.Id, tempMin, tempMax, tempCenter, tempOrientation,
                                    MyEntityIdentifier.AllocateId(), MySession.Static.LocalPlayerId));
                            }
                        }
                    }
                    if (m_blocksBuildQueue.Count > 0)
                    {
                        MyGuiAudio.PlaySound(MyGuiSounds.HudPlaceBlock);
                    }
                }
                else
                {
                    // New version of build code
                    MyCubeGrid.MyBlockBuildArea area = new MyCubeGrid.MyBlockBuildArea();
                    area.PosInGrid = centerPos;
                    area.BlockMin = new Vector3B(min);
                    area.BlockMax = new Vector3B(max);
                    area.BuildAreaSize = new Vector3UByte(counter);
                    area.StepDelta = new Vector3B(stepDelta);
                    area.OrientationForward = Base6Directions.GetForward(ref orientation);
                    area.OrientationUp = Base6Directions.GetUp(ref orientation);
                    area.DefinitionId = definitionId;
                    area.ColorMaskHSV = MyPlayer.SelectedColor.PackHSVToUint();

                    CurrentGrid.BuildBlocks(ref area, MySession.Static.LocalCharacterEntityId, MySession.Static.LocalPlayerId);
                }
            }
            else if (startRemove != null && (continueBuild != null || smallViewChange))
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudDeleteBlock);

                Vector3I min = startRemove.Value;
                Vector3I max = startRemove.Value;

                Vector3I stepDelta;
                Vector3I counter;
                int stepCount;
                if (smallViewChange)
                    continueBuild = startRemove;

                ComputeSteps(startRemove.Value, continueBuild.Value, Vector3I.One, out stepDelta, out counter, out stepCount);

                min = Vector3I.Min(startRemove.Value, continueBuild.Value);
                max = Vector3I.Max(startRemove.Value, continueBuild.Value);
                var size = new Vector3UByte(max - min);
                CurrentGrid.RazeBlocks(ref min, ref size);
            }

            startBuild = null;
            continueBuild = null;
            startRemove = null;
        }

        protected virtual bool CancelBuilding() 
        {
            if (m_gizmo.SpaceDefault.m_continueBuild != null)
            {
                m_gizmo.SpaceDefault.m_startBuild = null;
                m_gizmo.SpaceDefault.m_startRemove = null;
                m_gizmo.SpaceDefault.m_continueBuild = null;
                return true;
            }

            return false;
        }

        protected virtual bool IsBuilding()
        {
            return (m_gizmo.SpaceDefault.m_startBuild != null || m_gizmo.SpaceDefault.m_startRemove != null);
        }

        protected bool CheckSmallViewChange()
        {
            float viewChangeCos = Vector3.Dot(m_initialIntersectionDirection, IntersectionDirection);
            double viewChangeDist = (m_initialIntersectionStart - IntersectionStart).Length();
            return viewChangeCos > CONTINUE_BUILDING_VIEW_ANGLE_CHANGE_THRESHOLD && viewChangeDist < CONTINUE_BUILDING_VIEW_POINT_CHANGE_THRESHOLD;
        }

        #endregion

        #region Draw

        protected internal override void ChooseHitObject()
        {
            if (IsBuilding())
                return;


            
            base.ChooseHitObject();

            m_gizmo.Clear();
            }

        private Vector3D GetFreeSpacePlacementPosition(out bool valid)
        {
            valid = false;

            float gridSize = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize);

            Vector3 halfExt = CurrentBlockDefinition.Size * gridSize * 0.5f;
            MatrixD matrix = m_gizmo.SpaceDefault.m_worldMatrixAdd;
            Vector3D rayStart = IntersectionStart;
            Vector3D rayEnd = FreePlacementTarget;
            matrix.Translation = rayStart;

            HkShape shape = new HkBoxShape(halfExt);

            double distance = double.MaxValue;

            try
            {
                float? dist = MyPhysics.CastShape(rayEnd, shape, ref matrix, MyPhysics.CollisionLayers.CollisionLayerWithoutCharacter);
                if (dist.HasValue && dist.Value != 0f)
                {
                    Vector3D intersectionPoint = rayStart + dist.Value * (rayEnd - rayStart);
                    const bool debugDraw = false;
                    if (debugDraw)
                    {
                        Color green = Color.Green;
                        BoundingBoxD localAABB = new BoundingBoxD(-halfExt, halfExt);
                        MatrixD drawMatrix = matrix;
                        drawMatrix.Translation = intersectionPoint;
                        MySimpleObjectDraw.DrawTransparentBox(ref drawMatrix, ref localAABB, ref green, MySimpleObjectRasterizer.Wireframe, 1, 0.04f);
                    }

                    Vector3D startToIntersection = intersectionPoint - IntersectionStart;
                    distance = startToIntersection.Length() * 0.98;

                    valid = true;
                }
            }
            finally
            {
                shape.RemoveReference();
            }

            float lowLimit = LowLimitDistanceForDynamicMode();
            if (distance < lowLimit)
            {
                distance = IntersectionDistance;
                valid = false;
            }

            if (distance > IntersectionDistance)
            {
                distance = IntersectionDistance;
                valid = false;
            }

            return IntersectionStart + distance * IntersectionDirection;
        }

        private float LowLimitDistanceForDynamicMode()
        {
            if (CurrentBlockDefinition != null)
            {
                float gridSize = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize);
                return gridSize + 0.1f;
                //Vector3 halfExt = CurrentBlockDefinition.Size * gridSize * 0.5f;
                //float radius = halfExt.Length();
                //return 3 * radius;
            }

            return 2.6f;
        }

        protected static void UpdateBlockInfoHud()
        {
            MyHud.BlockInfo.Visible = false;

			/*if (MyFakes.ENABLE_SIMPLE_SURVIVAL)
				return;*/

            var block = MyCubeBuilder.Static.CurrentBlockDefinition;
            if (block == null || !MyCubeBuilder.Static.IsActivated)
            {
                return;
            }

            if (!MyFakes.ENABLE_SMALL_GRID_BLOCK_INFO && block != null && block.CubeSize == MyCubeSize.Small)
                return;

            MySlimBlock.SetBlockComponents(MyHud.BlockInfo, block, MyCubeBuilder.BuildComponent.GetBuilderInventory(MySession.Static.LocalCharacter));
            MyHud.BlockInfo.Visible = true;
            return;
        }


        #endregion

        #region Grid creation

        public void StartStaticGridPlacement(MyCubeSize cubeSize, bool isStatic)
        {
            var character = MySession.Static.LocalCharacter;
            if (character != null)
            {
                character.SwitchToWeapon(null);
            }

            MyDefinitionId id = new MyDefinitionId(typeof(MyObjectBuilder_CubeBlock), "LargeBlockArmorBlock");
            MyCubeBlockDefinition def;
            if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(id, out def))
            {
                Activate(def.Id);
                m_stationPlacement = true;
            }
        }

        protected static MyObjectBuilder_CubeGrid CreateMultiBlockGridBuilder(MyMultiBlockDefinition multiCubeBlockDefinition, Matrix rotationMatrix, Vector3D position = default(Vector3D))
        {
            Debug.Assert(MyFakes.ENABLE_MULTIBLOCKS);

            var gridBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(position, rotationMatrix.Forward, rotationMatrix.Up);
            gridBuilder.IsStatic = false;
            gridBuilder.PersistentFlags |= MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;

            if (multiCubeBlockDefinition.BlockDefinitions == null)
            {
                Debug.Assert(false);
                return null;
            }

            MyCubeSize? cubeSize = null;
            Vector3I min = Vector3I.MaxValue;
            Vector3I max = Vector3I.MinValue;

            int multiblockId = MyRandom.Instance.Next();
            while (multiblockId == 0)
                multiblockId = MyRandom.Instance.Next();

            for (int i = 0; i < multiCubeBlockDefinition.BlockDefinitions.Length; ++i)
            {
                var multiBlockPartDefinition = multiCubeBlockDefinition.BlockDefinitions[i];

                MyCubeBlockDefinition blockDefinition;
                MyDefinitionManager.Static.TryGetCubeBlockDefinition(multiBlockPartDefinition.Id, out blockDefinition);
                if (blockDefinition == null)
                {
                    Debug.Assert(false);
                    continue;
                }

                if (cubeSize == null)
                {
                    cubeSize = blockDefinition.CubeSize;
                }
                else if (cubeSize.Value != blockDefinition.CubeSize)
                {
                    Debug.Assert(false, "Blocks with different sizes cannot be in multi block");
                    continue;
                }

                MyObjectBuilder_CubeBlock blockBuilder = MyObjectBuilderSerializer.CreateNewObject(blockDefinition.Id) as MyObjectBuilder_CubeBlock;
                blockBuilder.Orientation = Base6Directions.GetOrientation(multiBlockPartDefinition.Forward, multiBlockPartDefinition.Up);
                blockBuilder.Min = multiBlockPartDefinition.Min;
                blockBuilder.ColorMaskHSV = MyPlayer.SelectedColor;
                blockBuilder.MultiBlockId = multiblockId;
                blockBuilder.MultiBlockIndex = i;
                blockBuilder.MultiBlockDefinition = multiCubeBlockDefinition.Id;
                blockBuilder.EntityId = MyEntityIdentifier.AllocateId();

                // Check block on existing position
                bool added = false;
                {
                    bool canBeAdded = true;

                    bool isValidCompound = MyCompoundCubeBlock.IsCompoundEnabled(blockDefinition);
                    // Find out existing multiblock
                    foreach (var existingBlock in gridBuilder.CubeBlocks)
                    {
                        if (existingBlock.Min == blockBuilder.Min)
                        {
                            if (MyFakes.ENABLE_COMPOUND_BLOCKS && (existingBlock is MyObjectBuilder_CompoundCubeBlock))
                            {
                                if (isValidCompound)
                                {
                                    var existingCB = existingBlock as MyObjectBuilder_CompoundCubeBlock;
                                    MyObjectBuilder_CubeBlock[] blocks = new MyObjectBuilder_CubeBlock[existingCB.Blocks.Length + 1];
                                    Array.Copy(existingCB.Blocks, blocks, existingCB.Blocks.Length);
                                    blocks[blocks.Length - 1] = blockBuilder;
                                    existingCB.Blocks = blocks;

                                    added = true;
                                }
                                else
                                {
                                    Debug.Assert(false, "Block cannot be added to compound (no compound templated defined) in multiblock");
                                    canBeAdded = false;
                                }
                            }
                            else
                            {
                                Debug.Assert(false, "Block position already used in multiblock");
                                canBeAdded = false;
                            }

                            break;
                        }
                    }

                    if (!canBeAdded)
                        continue;
                }

                if (!added)
                {
                    // Compound block
                    if (MyFakes.ENABLE_COMPOUND_BLOCKS && MyCompoundCubeBlock.IsCompoundEnabled(blockDefinition))
                    {
                        MyObjectBuilder_CompoundCubeBlock compoundCBBuilder = MyCompoundCubeBlock.CreateBuilder(blockBuilder);
                        gridBuilder.CubeBlocks.Add(compoundCBBuilder);
                    }
                    else
                    {
                        gridBuilder.CubeBlocks.Add(blockBuilder);
                    }
                }

                min = Vector3I.Min(min, multiBlockPartDefinition.Min);
                max = Vector3I.Max(max, multiBlockPartDefinition.Min);
            }

            if (gridBuilder.CubeBlocks.Count == 0)
            {
                Debug.Assert(false);
                return null;
            }

            gridBuilder.GridSizeEnum = cubeSize.Value;

            return gridBuilder;
        }

        protected static void AfterGridBuild(MyEntity builder, MyCubeGrid grid, bool instantBuild)
        {
            if (grid != null)
            {
                MySlimBlock block = grid.GetCubeBlock(Vector3I.Zero);
                if (block != null)
                {
                    if (grid.IsStatic)
                    {
                        MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                        MySlimBlock blockInCompound = compoundBlock != null && compoundBlock.GetBlocksCount() > 0 ? compoundBlock.GetBlocks()[0] : null;

                        MyCubeGrid mainGrid = grid.DetectMerge(block);
                        if (mainGrid == null)
                            mainGrid = grid;

                        MySlimBlock mainBlock = block;
                        if (blockInCompound != null)
                        {
                            Debug.Assert(blockInCompound.CubeGrid == mainGrid);
                            mainBlock = mainGrid.GetCubeBlock(blockInCompound.Position);
                        }

                        mainGrid.AdditionalModelGenerators.ForEach(g => g.UpdateAfterGridSpawn(mainBlock));

                        if (MyCubeGridSmallToLargeConnection.Static != null)
                        {
                            if (Sync.IsServer && !MyCubeGridSmallToLargeConnection.Static.AddBlockSmallToLargeConnection(block) && grid.GridSizeEnum == MyCubeSize.Small)
                                block.CubeGrid.TestDynamic = MyCubeGrid.MyTestDynamicReason.GridCopied;
                        }
                    }

                    if (Sync.IsServer)
                    {
                        MyCubeBuilder.BuildComponent.AfterSuccessfulBuild(builder, instantBuild);
                    }

                    if (block.FatBlock != null)
                        block.FatBlock.OnBuildSuccess(builder.EntityId);

                    MyCubeGrids.NotifyBlockBuilt(grid, block);
                }
                else
                    Debug.Fail("Block not created");

                
            }
        }

        /// <summary>
        /// Spawn static grid - must have identity rotation matrix! If dontAdd is true, grid won't be added to enitites. Also it won't have entityId set.
        /// </summary>
        public static MyCubeGrid SpawnStaticGrid(MyCubeBlockDefinition blockDefinition, MyEntity builder, MatrixD worldMatrix, Vector3 color, SpawnFlags spawnFlags = SpawnFlags.Default, long builtBy = 0, Action completionCallback = null)
        {
            Debug.Assert(Sync.IsServer, "Only server can spawn grids! Clients have to send requests!");

            var gridBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();

            Vector3 offset = Vector3.TransformNormal(MyCubeBlock.GetBlockGridOffset(blockDefinition), worldMatrix);
            gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(worldMatrix.Translation - offset, worldMatrix.Forward, worldMatrix.Up);
            gridBuilder.GridSizeEnum = blockDefinition.CubeSize;
            gridBuilder.IsStatic = true;
            gridBuilder.CreatePhysics = (spawnFlags & SpawnFlags.CreatePhysics) != SpawnFlags.None;
            gridBuilder.EnableSmallToLargeConnections = (spawnFlags & SpawnFlags.EnableSmallTolargeConnections) != SpawnFlags.None;
            gridBuilder.PersistentFlags |= MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;
            if ((spawnFlags & SpawnFlags.AddToScene) != SpawnFlags.None)
                gridBuilder.EntityId = MyEntityIdentifier.AllocateId();

            // Block must be placed on (0,0,0) coordinate
            var blockBuilder = MyObjectBuilderSerializer.CreateNewObject(blockDefinition.Id) as MyObjectBuilder_CubeBlock;
            blockBuilder.Orientation = Quaternion.CreateFromForwardUp(Vector3I.Forward, Vector3I.Up);
            blockBuilder.Min = blockDefinition.Size / 2 - blockDefinition.Size + Vector3I.One;
            if ((spawnFlags & SpawnFlags.AddToScene) != SpawnFlags.None)
                blockBuilder.EntityId = MyEntityIdentifier.AllocateId();
            blockBuilder.ColorMaskHSV = color;
            blockBuilder.BuiltBy = builtBy;
            
            MyCubeBuilder.BuildComponent.BeforeCreateBlock(blockDefinition, builder, blockBuilder, buildAsAdmin: (spawnFlags & SpawnFlags.SpawnAsMaster) != SpawnFlags.None);

            gridBuilder.CubeBlocks.Add(blockBuilder);

            MyCubeGrid grid;

            if ((spawnFlags & SpawnFlags.AddToScene) != SpawnFlags.None)
            {
                grid = MyEntities.CreateFromObjectBuilderParallel(gridBuilder, true, completionCallback, callbackNeedsReplicable: true) as MyCubeGrid;
            }
            else
            {
                grid = MyEntities.CreateFromObjectBuilderParallel(gridBuilder, completionCallback: completionCallback, callbackNeedsReplicable: true) as MyCubeGrid;
            }

            return grid;
        }

        public static MyCubeGrid SpawnDynamicGrid(MyCubeBlockDefinition blockDefinition, MyEntity builder, MatrixD worldMatrix, Vector3 color, long entityId = 0, SpawnFlags spawnFlags = SpawnFlags.Default, long builtBy = 0, Action completionCallback = null)
        {
            var gridBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            Vector3 offset = Vector3.TransformNormal(MyCubeBlock.GetBlockGridOffset(blockDefinition), worldMatrix);

            gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(worldMatrix.Translation - offset, worldMatrix.Forward, worldMatrix.Up);
            gridBuilder.GridSizeEnum = blockDefinition.CubeSize;
            gridBuilder.IsStatic = false;
            gridBuilder.PersistentFlags |= MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;

            // Block must be placed on (0,0,0) coordinate
            var blockBuilder = MyObjectBuilderSerializer.CreateNewObject(blockDefinition.Id) as MyObjectBuilder_CubeBlock;
            blockBuilder.Orientation = Quaternion.CreateFromForwardUp(Vector3I.Forward, Vector3I.Up);
            blockBuilder.Min = blockDefinition.Size / 2 - blockDefinition.Size + Vector3I.One;
            blockBuilder.ColorMaskHSV = color;
            blockBuilder.BuiltBy = builtBy;
            MyCubeBuilder.BuildComponent.BeforeCreateBlock(blockDefinition, builder, blockBuilder, buildAsAdmin: (spawnFlags & SpawnFlags.SpawnAsMaster) != SpawnFlags.None);

            gridBuilder.CubeBlocks.Add(blockBuilder);

            MyCubeGrid grid = null;

            //TODO: Try to find better way how to sync entity ID of subblocks..
            if (entityId != 0)
            {
                gridBuilder.EntityId = entityId;
                blockBuilder.EntityId = entityId + 1;
                grid = MyEntities.CreateFromObjectBuilderParallel(gridBuilder, true, completionCallback) as MyCubeGrid;
            }
            else
            {
                Debug.Assert(Sync.IsServer, "Only server can generate grid entity IDs!");
                if (Sync.IsServer)
                {
                    gridBuilder.EntityId = MyEntityIdentifier.AllocateId();
                    blockBuilder.EntityId = gridBuilder.EntityId + 1;
                    grid = MyEntities.CreateFromObjectBuilderParallel(gridBuilder, true, completionCallback) as MyCubeGrid;
                }
            }

            return grid;
        }

        #endregion

        public static void SelectBlockToToolbar(MySlimBlock block, bool selectToNextSlot = true)
        {
            Debug.Assert(block != null && MyToolbarComponent.CurrentToolbar != null);
            MyDefinitionId blockId = block.BlockDefinition.Id;
            if (block.FatBlock is MyCompoundCubeBlock)
            {
                var compound = block.FatBlock as MyCompoundCubeBlock;
                m_cycle %= compound.GetBlocksCount();
                blockId = compound.GetBlocks()[m_cycle].BlockDefinition.Id;
                m_cycle++;
            }
            if (block.FatBlock is MyFracturedBlock)
            {
                var fracture = block.FatBlock as MyFracturedBlock;
                m_cycle %= fracture.OriginalBlocks.Count;
                blockId = fracture.OriginalBlocks[m_cycle];
                m_cycle++;
            }
            if (MyToolbarComponent.CurrentToolbar.SelectedSlot.HasValue)
            {
                int slot = MyToolbarComponent.CurrentToolbar.SelectedSlot.Value;
                if (selectToNextSlot)
                    slot++;
                if (!MyToolbarComponent.CurrentToolbar.IsValidSlot(slot))
                    slot = 0;
                var builder = new MyObjectBuilder_ToolbarItemCubeBlock();
                builder.DefinitionId = blockId;
                var item = MyToolbarItemFactory.CreateToolbarItem(builder);
                MyToolbarComponent.CurrentToolbar.SetItemAtSlot(slot, item);
            }
            else
            {
                int slot = 0;
                while (MyToolbarComponent.CurrentToolbar.GetSlotItem(slot) != null)
                    slot++;
                if (!MyToolbarComponent.CurrentToolbar.IsValidSlot(slot))
                    slot = 0;

                var builder = new MyObjectBuilder_ToolbarItemCubeBlock();
                builder.DefinitionId = blockId;
                var item = MyToolbarItemFactory.CreateToolbarItem(builder);
                MyToolbarComponent.CurrentToolbar.SetItemAtSlot(slot, item);
            }
        }

        /// <summary>
        /// Triggered when Current Grid is changed to new one.
        /// </summary>
        /// <param name="newCurrentGrid">New grid that will replace the old one.</param>
        private void BeforeCurrentGridChange(MyCubeGrid newCurrentGrid)
        {
            this.TriggerRespawnShipNotification(newCurrentGrid);
        }

        /// <summary>
        /// Checks if any player is an owner of particular respawn ship/cart,
        /// and if yes than shows warning about desapearing respawn ship/cart.
        /// </summary>
        private void TriggerRespawnShipNotification(MyCubeGrid newCurrentGrid)
        {
            bool warningShown = false;

            if (newCurrentGrid != null && newCurrentGrid.IsRespawnGrid)
            {
                Sandbox.Game.Gui.MyHud.Notifications.Add(MyNotificationSingletons.RespawnShipWarning);
                warningShown = true;
            }

            if (!warningShown)
                Sandbox.Game.Gui.MyHud.Notifications.Remove(MyNotificationSingletons.RespawnShipWarning);

        }

        public static double? GetCurrentRayIntersection()
        {
            var hitInfo = MyPhysics.CastRay(MyCubeBuilder.IntersectionStart, MyCubeBuilder.IntersectionStart + 2000 * MyCubeBuilder.IntersectionDirection, MyPhysics.CollisionLayers.CollisionLayerWithoutCharacter);
            if (hitInfo.HasValue)
            {
                Vector3D p = hitInfo.Value.Position - MyCubeBuilder.IntersectionStart;
                double dist = p.Length();
                return dist;
            }

            return null;
        }

        /// <summary>
        /// Converts large grid hit coordinates for small cubes. Allows placement of small grids to large grids.
        /// Returns coordinates of small grid (in large grid coordinates) which touches large grid in the hit position.
        /// </summary>
        public static Vector3 TransformLargeGridHitCoordToSmallGrid(Vector3D coords, MatrixD worldMatrixNormalizedInv, float gridSize)
        {
            Vector3D localCoords = Vector3D.Transform(coords, worldMatrixNormalizedInv);
            localCoords /= gridSize;
            // We have 10 small cubes in large one.
            localCoords *= 10f;
            Vector3I sign = Vector3I.Sign(localCoords);
            // Center of small cube has offset 0.05
            localCoords -= 0.5 * sign;
            localCoords = sign * Vector3I.Round(Vector3D.Abs(localCoords));
            localCoords += 0.5 * sign;
            localCoords /= 10;
            return localCoords;
        }

        public static MyObjectBuilder_CubeGrid ConvertGridBuilderToStatic(MyObjectBuilder_CubeGrid originalGrid, MatrixD worldMatrix)
        {
            var gridBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            gridBuilder.EntityId = originalGrid.EntityId;
            gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(worldMatrix.Translation, worldMatrix.Forward, worldMatrix.Up);
            gridBuilder.GridSizeEnum = originalGrid.GridSizeEnum;
            gridBuilder.IsStatic = true;
            gridBuilder.PersistentFlags |= MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;
            
            // Blocks in static grid - must be recreated for static grid with different orientation and position
            foreach (var origBlock in originalGrid.CubeBlocks)
            {
                if (origBlock is MyObjectBuilder_CompoundCubeBlock)
                {
                    var origBlockCompound = origBlock as MyObjectBuilder_CompoundCubeBlock;
                    var blockBuilderCompound = ConvertDynamicGridBlockToStatic(ref worldMatrix, origBlock) as MyObjectBuilder_CompoundCubeBlock;
                    Debug.Assert(blockBuilderCompound != null);
                    if (blockBuilderCompound == null)
                        continue;

                    blockBuilderCompound.Blocks = new MyObjectBuilder_CubeBlock[origBlockCompound.Blocks.Length];

                    for (int i = 0; i < origBlockCompound.Blocks.Length; ++i)
                    {
                        var origBlockInCompound = origBlockCompound.Blocks[i];
                        var blockBuilder = ConvertDynamicGridBlockToStatic(ref worldMatrix, origBlockInCompound);
                        if (blockBuilder == null)
                            continue;

                        blockBuilderCompound.Blocks[i] = blockBuilder;
                    }
                    gridBuilder.CubeBlocks.Add(blockBuilderCompound);
                }
                else
                {
                    var blockBuilder = ConvertDynamicGridBlockToStatic(ref worldMatrix, origBlock);
                    if (blockBuilder == null)
                        continue;
                    gridBuilder.CubeBlocks.Add(blockBuilder);
                }
            }

            return gridBuilder;
        }

        public static MyObjectBuilder_CubeBlock ConvertDynamicGridBlockToStatic(ref MatrixD worldMatrix, MyObjectBuilder_CubeBlock origBlock)
        {
            MyDefinitionId defId = new MyDefinitionId(origBlock.TypeId, origBlock.SubtypeName);
            MyCubeBlockDefinition blockDefinition;
            MyDefinitionManager.Static.TryGetCubeBlockDefinition(defId, out blockDefinition);
            if (blockDefinition == null)
                return null;

            var blockBuilder = MyObjectBuilderSerializer.CreateNewObject(defId) as MyObjectBuilder_CubeBlock;
            blockBuilder.EntityId = origBlock.EntityId;
            // Orientation quaternion is not setup in origblock
            MyBlockOrientation orientation = origBlock.BlockOrientation;
            Quaternion rotationQuat;
            orientation.GetQuaternion(out rotationQuat);
            Matrix origRotationMatrix = Matrix.CreateFromQuaternion(rotationQuat);
            Matrix rotationMatrix = origRotationMatrix * worldMatrix;
            //blockBuilder.Orientation = Quaternion.CreateFromRotationMatrix(rotationMatrix);
            blockBuilder.Orientation = Quaternion.CreateFromRotationMatrix(origRotationMatrix);

            Vector3I origSizeRotated = Vector3I.Abs(Vector3I.Round(Vector3.TransformNormal((Vector3)blockDefinition.Size, origRotationMatrix)));
            Vector3I origMin = origBlock.Min;
            Vector3I origMax = origBlock.Min + origSizeRotated - Vector3I.One;

            Vector3I minXForm = Vector3I.Round(Vector3.TransformNormal((Vector3)origMin, worldMatrix));
            Vector3I maxXForm = Vector3I.Round(Vector3.TransformNormal((Vector3)origMax, worldMatrix));

            //blockBuilder.Min = Vector3I.Min(minXForm, maxXForm);
            blockBuilder.Min = Vector3I.Min(origMin, origMax);

            blockBuilder.MultiBlockId = origBlock.MultiBlockId;
            blockBuilder.MultiBlockDefinition = origBlock.MultiBlockDefinition;
            blockBuilder.MultiBlockIndex = origBlock.MultiBlockIndex;

            blockBuilder.BuildPercent = origBlock.BuildPercent;
            blockBuilder.IntegrityPercent = origBlock.BuildPercent;

            return blockBuilder;
        }

        public static void GetAllBlocksPositions(HashSet<Tuple<MySlimBlock, ushort?>> blockInCompoundIDs, HashSet<Vector3I> outPositions)
        {
            foreach (var blockInCompoundID in blockInCompoundIDs)
            {
                Vector3I cube = blockInCompoundID.Item1.Min;
                for (Vector3I_RangeIterator it = new Vector3I_RangeIterator(ref blockInCompoundID.Item1.Min, ref blockInCompoundID.Item1.Max); it.IsValid(); it.GetNext(out cube))
                {
                    outPositions.Add(cube);
                }
            }
        }

        [Event,Reliable,Server]
        static void RequestGridSpawn(Author author, DefinitionIdBlit definition, BuildData position, bool instantBuild, bool forceStatic, uint colorMaskHsv)
        {
            Debug.Assert(BuildComponent != null, "The build component was not set in cube builder!");

            MyEntity builder = null;
            bool isAdmin = (MyEventContext.Current.IsLocallyInvoked || MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value) || MySession.Static.CreativeToolsEnabled(Sync.MyId));
            MyEntities.TryGetEntityById(author.EntityId, out builder);

            var blockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(definition);
            var absPosition = position.Position;
            if (!position.AbsolutePosition) 
                absPosition += builder.PositionComp.GetPosition();
            MatrixD worldMatrix = MatrixD.CreateWorld(absPosition, position.Forward, position.Up);

            float gridSize = MyDefinitionManager.Static.GetCubeSize(blockDefinition.CubeSize);
            BoundingBoxD localAABB = new BoundingBoxD(-blockDefinition.Size * gridSize * 0.5f, blockDefinition.Size * gridSize * 0.5f);

            MyGridPlacementSettings settings = CubeBuilderDefinition.BuildingSettings.GetGridPlacementSettings(blockDefinition.CubeSize);
            VoxelPlacementSettings voxelPlacementDef = new VoxelPlacementSettings() { PlacementMode = VoxelPlacementMode.OutsideVoxel };
            settings.VoxelPlacement = voxelPlacementDef;

            bool isStatic = forceStatic || MyCubeGrid.IsAabbInsideVoxel(worldMatrix, localAABB, settings) || Static.m_stationPlacement;

            BuildComponent.GetGridSpawnMaterials(blockDefinition, worldMatrix, isStatic);
            bool hasBuildMat = (isAdmin && instantBuild) || MyCubeBuilder.BuildComponent.HasBuildingMaterials(builder);

            bool canSpawn = true;
            // Try spawning "fake" grid in that place, if fail it means something already there.
            // TODO: broken for armor blocks. Rendering instance stays on the screen after creating temp grid
            //if (isStatic)
            //{
            //    canSpawn = GridPlacementTest(builder, blockDefinition, worldMatrix);
            //}
            canSpawn = hasBuildMat & canSpawn; // It is not possible to create something in already occupied place, even if admin.

            ulong senderId = MyEventContext.Current.Sender.Value;

            if(senderId == 0)
                SpawnGridReply(canSpawn);
            else
                MyMultiplayer.RaiseStaticEvent(s => SpawnGridReply, canSpawn, new EndpointId(senderId));

            if (!canSpawn) return;

            MyCubeGrid grid = null;
            SpawnFlags flags = SpawnFlags.Default;
            if (isAdmin && instantBuild)
            {
                flags |= SpawnFlags.SpawnAsMaster;
            }

            Vector3 color = ColorExtensions.UnpackHSVFromUint(colorMaskHsv);

            if (isStatic)
            {
                grid = SpawnStaticGrid(blockDefinition, builder, worldMatrix, color, flags, author.IdentityId, completionCallback: delegate() { AfterGridBuild(builder, grid, instantBuild); });
            }
            else
                grid = SpawnDynamicGrid(blockDefinition, builder, worldMatrix, color, spawnFlags: flags, builtBy: author.IdentityId, completionCallback: delegate() { AfterGridBuild(builder, grid, instantBuild); });

            

            if (grid != null)
            {
                if(grid.IsStatic && grid.GridSizeEnum != MyCubeSize.Small)
                {
                    bool result = MyCoordinateSystem.Static.IsLocalCoordSysExist(ref worldMatrix, grid.GridSize);
                    if (result)
                    {
                        MyCoordinateSystem.Static.RegisterCubeGrid(grid);
                    }
                    else
                    {
                        MyCoordinateSystem.Static.CreateCoordSys(grid, CubeBuilderDefinition.BuildingSettings.StaticGridAlignToCenter, true);
                    }
                }
            }
        }

        ///// <summary>
        ///// Checks block placement in area.
        ///// </summary>
        ///// <returns>true if placement is possible</returns>
        //private static bool GridPlacementTest(MyEntity builder, MyCubeBlockDefinition blockDefinition, MatrixD worldMatrix)
        //{
        //    bool canSpawn = true;
        //    // TODO: this temporary grid has to be removed from here - causes many problems.
        //    MyCubeGrid tempGrid = MyCubeBuilder.SpawnStaticGrid(blockDefinition, builder, worldMatrix, Vector3.Zero, SpawnFlags.None);
        //    // tempGrid can be null when entity init fails.
        //    if (tempGrid == null)
        //        return false;

        //    // Im not sure in this place, should client and server have the same creation settings?
        //    //MyGridPlacementSettings gridPlacementSettings = MyPerGameSettings.CreationSettings.GetGridPlacementSettings(tempGrid);

        //    var gridSize = tempGrid.GridSize;
        //    var blocks = tempGrid.GetBlocks();

        //    foreach (var block in blocks)
        //    {
        //        if (block.FatBlock is MyCompoundCubeBlock)
        //        {
        //            // TODO: block orientation should be removed 
        //            MyBlockOrientation blockOrientation = new MyBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up);

        //            foreach (var blockInCompound in (block.FatBlock as MyCompoundCubeBlock).GetBlocks())
        //            {
        //                BoundingBoxD localAABB = new BoundingBoxD(-blockInCompound.BlockDefinition.Size * gridSize * 0.5f, blockInCompound.BlockDefinition.Size * gridSize * 0.5f);
        //                canSpawn &= MyCubeGrid.TestBlockPlacementArea(blockInCompound.BlockDefinition, blockOrientation, blockInCompound.FatBlock.PositionComp.WorldMatrix, ref gridPlacementSettings, localAABB, false);
        //            }
        //        }
        //        else
        //        {
        //            BoundingBoxD localAABB = new BoundingBoxD(-block.BlockDefinition.Size * gridSize * 0.5f, block.BlockDefinition.Size * gridSize * 0.5f);
        //            Vector3D worldC;
        //            block.ComputeWorldCenter(out worldC);
        //            MatrixD blockWorldMatrix = MatrixD.CreateTranslation(worldC);
        //            if (block.FatBlock != null)
        //                blockWorldMatrix = block.FatBlock.PositionComp.WorldMatrix;
        //            canSpawn &= MyCubeGrid.TestBlockPlacementArea(block.BlockDefinition, null, blockWorldMatrix, ref gridPlacementSettings, localAABB, false);
        //        }
        //    }
            
        //    tempGrid.Close();

        //    return canSpawn;
        //}

        [Event, Reliable, Client]
        static void SpawnGridReply(bool success)
        {
            if (success)
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudPlaceBlock);
            }
            else
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
            }
        }
    }
}
