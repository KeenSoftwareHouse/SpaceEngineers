#region Using

using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
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
using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Utils;
using VRageMath;
using VRageRender;
using ModelId = System.Int32;
using Sandbox.Game.Multiplayer;
using VRage.ObjectBuilders;

#endregion

namespace Sandbox.Game.Entities
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public partial class MyCubeBuilder : MyBlockBuilderBase
    {
        // These are names of ME controls. It's a hack to avoid defining them twice (in Sandbox.Game and Medieval).
        // It should be removed as soon as shit called MyCubeBuilder is rewritten.
        public const string ME_SWITCH_STAGES_CONTROL_NAME = "SWITCH_STAGES";
        public const string ME_PICK_BLOCK_CONTROL_NAME = "PICK_BLOCK";
        public const string ME_COMPOUND_BUILDING_CONTROL_NAME = "COMPOUND_BUILDING";
        public const string ME_SI_VIEW_NAME = "SI_VIEW";
        public const string ME_PRESS_TO_COMPOUND_NAME = "PRESS_TO_COMPOUND";
        private static readonly MyStringId ME_SWITCH_STAGES = MyStringId.GetOrCompute(ME_SWITCH_STAGES_CONTROL_NAME);
        private static readonly MyStringId ME_PICK_BLOCK = MyStringId.GetOrCompute(ME_PICK_BLOCK_CONTROL_NAME);
        private static readonly MyStringId ME_SI_VIEW = MyStringId.GetOrCompute(ME_SI_VIEW_NAME);
        private static readonly MyStringId ME_PRESS_TO_COMPOUND = MyStringId.GetOrCompute(ME_PRESS_TO_COMPOUND_NAME);
        #region Enums

        public enum BuildingModeEnum
        {
            SingleBlock,
            Line,
            Plane
        }

        #endregion

        #region Fields

        class BlockPair
        {
            public MyCubeBlockDefinition Small;
            public MyCubeBlockDefinition Large;
        }

        public static MyCubeBuilder Static;

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

        struct MyColoringArea
        {
            public Vector3I Start;
            public Vector3I End;
        }

        static MyColoringArea[] m_currColoringArea = new MyColoringArea[8];

        static List<Vector3I> m_cacheGridIntersections = new List<Vector3I>();
        List<Vector3> m_collisionTestPoints = new List<Vector3>(12);

        //static MyGridClipboard m_clipboard = new MyGridClipboard(MyPerGameSettings.PastingSettings, calculateVelocity: true);
        static MyGridClipboard m_clipboard = MyFakes.ENABLE_ALTERNATIVE_CLIPBOARD ? new MyGridClipboard2(MyPerGameSettings.PastingSettings, calculateVelocity: true)
            : new MyGridClipboard(MyPerGameSettings.PastingSettings, calculateVelocity: true);
        public MyGridClipboard Clipboard
        {
            get { return m_clipboard; }
        }

        static MyFloatingObjectClipboard m_floatingObjectClipboard = new MyFloatingObjectClipboard(true);
        internal MyFloatingObjectClipboard FloatingObjectClipboard
        {
            get { return m_floatingObjectClipboard; }
        }

        static MyGridClipboard m_shipCreationClipboard = new MyGridClipboard(MyPerGameSettings.CreationSettings, calculateVelocity: false);
        internal MyGridClipboard ShipCreationClipboard
        {
            get { return m_shipCreationClipboard; }
        }

        static MyVoxelClipboard m_voxelClipboard = new MyVoxelClipboard();
        internal MyVoxelClipboard VoxelClipboard
        {
            get { return m_voxelClipboard; }
        }

        private static MyMultiBlockClipboard m_multiBlockCreationClipboard = new MyMultiBlockClipboard(MyPerGameSettings.BuildingSettings, calculateVelocity: true);

        private bool m_rotationHintRotating = false;
        private int m_lastInputHandleTime;

        private MyBlockBuilderRotationHints m_rotationHints = new MyBlockBuilderRotationHints();
        private MyBlockBuilderRenderData m_renderData = new MyBlockBuilderRenderData();

        public bool CopyPasteVoxelIsActivated
        {
            get
            {
                return m_voxelClipboard.IsActive;
            }
        }

        public bool CopyPasteFloatingObjectIsActivated
        {
            get
            {
                return m_floatingObjectClipboard.IsActive;
            }
        }

        public bool CopyPasteIsActivated
        {
            get
            {
                return m_clipboard.IsActive;
            }
        }

        public bool ShipCreationIsActivated
        {
            get
            {
                return m_shipCreationClipboard.IsActive;
            }
        }

        public bool MultiBlockCreationIsActivated
        {
            get
            {
                return MyFakes.ENABLE_MULTIBLOCKS && m_multiBlockCreationClipboard.IsActive;
            }
        }

        public bool CompoundEnabled { get; private set; }


        private bool m_blockCreationActivated;
        public bool BlockCreationIsActivated
        {
            get { return m_blockCreationActivated; }
            private set { m_blockCreationActivated = value; }
        }

        public override bool IsActivated
        {
            get
            {
                return BlockCreationIsActivated || CopyPasteIsActivated || ShipCreationIsActivated || CopyPasteFloatingObjectIsActivated || CopyPasteVoxelIsActivated || MultiBlockCreationIsActivated;
            }
        }

        bool m_useSymmetry = true;
        public bool UseSymmetry
        {
            get { return m_useSymmetry && (MySession.Static != null && MySession.Static.CreativeMode); }
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

        /// <summary>
        /// Store last rotation for each block definition.
        /// </summary>
        private Dictionary<MyDefinitionId, Quaternion> m_rotationsByDefinitionHash = new Dictionary<MyDefinitionId, Quaternion>(MyDefinitionId.Comparer);

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
                        m_renderData.UpdateRenderEntitiesData(CurrentGrid.WorldMatrix, UseTransparency);
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

        MyCubeBuilderGizmo m_gizmo;

        MySymmetrySettingModeEnum m_symmetrySettingMode = MySymmetrySettingModeEnum.NoPlane;

        Vector3D m_initialIntersectionStart;
        Vector3D m_initialIntersectionDirection;

        protected internal override MyCubeGrid CurrentGrid
        {
            get { return m_currentGrid; }
            protected set
            {
                if (FreezeGizmo)
                    return;

                if (m_currentGrid != value)
                {
                    m_currentGrid = value;
                    UpdateNotificationBlockNotAvailable();

                    if (m_currentGrid == null)
                    {
                        RemoveSymmetryNotification();

                        m_gizmo.Clear();
                    }
                }
            }
        }
        private int m_variantIndex = -1;

        protected internal override MyVoxelMap CurrentVoxelMap
        {
            get
            {
                if (MyFakes.ENABLE_BLOCK_PLACEMENT_ON_VOXEL)
                    return m_currentVoxelMap;
                return null;
            }

            protected set
            {
                if (FreezeGizmo)
                    return;

                if (m_currentVoxelMap != value)
                {
                    m_currentVoxelMap = value;
                    UpdateNotificationBlockNotAvailable();

                    if (m_currentVoxelMap == null)
                    {
                        RemoveSymmetryNotification();

                        m_gizmo.Clear();
                    }
                }
            }
        }

        private List<MyCubeBlockDefinition> CurrentBlockDefinitionStages = new List<MyCubeBlockDefinition>();
        private Dictionary<MyDefinitionId, int> m_stageIndexByDefinitionHash = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer);

        private MyCubeBlockDefinitionWithVariants m_definitionWithVariants;
        protected override MyCubeBlockDefinition CurrentBlockDefinition
        {
            get
            {
                return m_definitionWithVariants;
            }
            set
            {
                if (value == null)
                {
                    m_definitionWithVariants = null;
                    CurrentBlockDefinitionStages.Clear();
                }
                else
                {
                    m_definitionWithVariants = new MyCubeBlockDefinitionWithVariants(value, m_variantIndex);

                    if (MyFakes.ENABLE_BLOCK_STAGES)
                    {
                        if (!CurrentBlockDefinitionStages.Contains(value))
                        {
                            CurrentBlockDefinitionStages.Clear();

                            if (value.BlockStages != null)
                            {
                                // First add this stage (main block definition from GUI)
                                CurrentBlockDefinitionStages.Add(value);

                                foreach (var stage in value.BlockStages)
                                {
                                    MyCubeBlockDefinition stageDef;
                                    MyDefinitionManager.Static.TryGetCubeBlockDefinition(stage, out stageDef);
                                    if (stageDef != null)
                                        CurrentBlockDefinitionStages.Add(stageDef);
                                }
                            }
                        }
                    }
                }
                UpdateNotificationBlockNotAvailable();
            }
        }

        public override MyCubeBlockDefinition HudBlockDefinition
        {
            get
            {
                if (m_shipCreationClipboard.IsActive)
                    return m_shipCreationClipboard.GetFirstBlockDefinition();
                else
                    return CurrentBlockDefinition;
            }
        }

        /// <summary>
        /// Current block definition for toolbar.
        /// </summary>
        public MyCubeBlockDefinition ToolbarBlockDefinition
        {
            get
            {
                if (MyFakes.ENABLE_BLOCK_STAGES)
                {
                    if (CurrentBlockDefinitionStages.Count > 0)
                        return CurrentBlockDefinitionStages[0];
                }
                return CurrentBlockDefinition;
            }
        }

        int m_modelIndex = 1;

        MyHudNotification m_blockNotAvailableNotification;
        MyHudNotification m_symmetryNotification;
        MyHudNotification m_pasteNotification;
        MyHudNotification m_stationRotationNotification;

        private bool m_dynamicMode;
        public bool DynamicMode
        {
            get
            {
                return MyFakes.ENABLE_CUBE_BUILDER_DYNAMIC_MODE && m_dynamicMode;
            }

            set
            {
                m_dynamicMode = MyFakes.ENABLE_CUBE_BUILDER_DYNAMIC_MODE && value;
            }
        }

        // Simple survival build delay time - for now disabled
        private static int SURVIVAL_BUILD_TIME_DELAY_MS = 0;
        private int m_lastBlockBuildTime = 0;

        private bool m_isBuildMode = false;
        public bool IsBuildMode
        {
            get { return m_isBuildMode; }
            private set 
            { 
                m_isBuildMode = value;
                MyHud.IsBuildMode = value;
                if (value)
                    ActivateBuildModeNotifications(MyInput.Static.IsJoystickConnected() && MyFakes.ENABLE_CONTROLLER_HINTS);
                else
                    DeactivateBuildModeNotifications();
            }
        }

        public static MyBuildComponentBase BuildComponent { get; set; }

        private static MyHudNotification BlockRotationHint;
        private static MyHudNotification ColorHint;
        private static MyHudNotification BuildingHint;
        private static MyHudNotification UnlimitedBuildingHint;
        private static MyHudNotification CompoundModeHint;
        private static MyHudNotification DynamicModeHint;

        private static MyHudNotification JoystickRotationHint;
        private static MyHudNotification JoystickBuildingHint;
        private static MyHudNotification JoystickUnlimitedBuildingHint;
        private static MyHudNotification JoystickCompoundModeHint;
        private static MyHudNotification JoystickDynamicModeHint;

        private MyHudNotification m_buildModeHint;

        #endregion

        #region Constructor

        public MyCubeBuilder()
        {
            m_gizmo = new MyCubeBuilderGizmo();
            InitializeNotifications();
        }

        #endregion

        #region Load data

        public override void LoadData()
        {
            Static = this;
            MyCubeGrid.ShowStructuralIntegrity = false;
        }

        #endregion

        #region Control

        private bool GridValid
        {
            get { return BlockCreationIsActivated && CurrentGrid != null; }
        }

        private bool GridAndBlockValid
        {
            get
            {
                return GridValid && CurrentBlockDefinition != null && (CurrentBlockDefinition.CubeSize == CurrentGrid.GridSizeEnum
                    || PlacingSmallGridOnLargeStatic);
            }
        }

        private bool VoxelMapAndBlockValid
        {
            get
            {
                return BlockCreationIsActivated && MyFakes.ENABLE_BLOCK_PLACEMENT_ON_VOXEL && CurrentVoxelMap != null
                    && CurrentBlockDefinition != null && (CurrentBlockDefinition.CubeSize == MyCubeSize.Large || CurrentBlockDefinition.CubeSize == MyCubeSize.Small);
            }
        }

        private bool PlacingSmallGridOnLargeStatic
        {
            get
            {
                return GridValid && CurrentBlockDefinition != null && MyFakes.ENABLE_STATIC_SMALL_GRID_ON_LARGE && CurrentBlockDefinition.CubeSize == MyCubeSize.Small
                    && CurrentGrid.GridSizeEnum == MyCubeSize.Large && CurrentGrid.IsStatic;
            }
        }

        private bool BuildInputValid
        {
            get { return GridAndBlockValid || VoxelMapAndBlockValid || (DynamicMode && (CurrentBlockDefinition != null || m_multiBlockCreationClipboard.IsActive)); }
        }

        private void RotateAxis(int index, int sign, bool newlyPressed, int frameDt)
        {
            if (CurrentBlockDefinition != null && CurrentBlockDefinition.Rotation == MyBlockRotation.None)
                return;

            float rotationSpeed = 0.002f;
            float angleDelta = frameDt * rotationSpeed;

            if (ShipCreationIsActivated)
                m_shipCreationClipboard.RotateAroundAxis(index, sign, newlyPressed, angleDelta);
            else if (CopyPasteIsActivated)
                m_clipboard.RotateAroundAxis(index, sign, newlyPressed, angleDelta);
            else if (CopyPasteFloatingObjectIsActivated)
                m_floatingObjectClipboard.RotateAroundAxis(index, sign, newlyPressed, angleDelta);
            else if (MultiBlockCreationIsActivated && m_multiBlockCreationClipboard.PreviewGrids.Count > 0)
            {
                if (!DynamicMode)
                {
                    MatrixD currentMatrix = m_multiBlockCreationClipboard.PreviewGrids[0].PositionComp.LocalMatrix;
                    MatrixD rotatedMatrix;
                    if (!CalculateBlockRotation(index, sign, ref currentMatrix, out rotatedMatrix, (float)Math.PI / 2, CurrentBlockDefinition != null ? CurrentBlockDefinition.Direction : MyBlockDirection.Both,
                        CurrentBlockDefinition != null ? CurrentBlockDefinition.Rotation : MyBlockRotation.Both))
                        return;
                }
                m_multiBlockCreationClipboard.RotateAroundAxis(index, sign, newlyPressed, angleDelta);

                // If multiblock creation is activated then block builder is deactivated so we return here or gizmo will be rotated
                return;
            }

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

        internal bool CalculateBlockRotation(int index, int sign, ref MatrixD currentMatrix, out MatrixD rotatedMatrix, float angle,
            MyBlockDirection blockDirection = MyBlockDirection.Both, MyBlockRotation blockRotation = MyBlockRotation.Both)
        {
            Matrix rotation = Matrix.Identity;
            rotatedMatrix = Matrix.Identity;

            //because Z axis is negative
            if (index == 2)
                sign *= -1;

            switch (index)
            {
                case 0:   //X
                    rotation = Matrix.CreateFromAxisAngle(currentMatrix.Right, sign * angle);
                    break;

                case 1: //Y
                    rotation = Matrix.CreateFromAxisAngle(currentMatrix.Up, sign * angle);
                    break;

                case 2: //Z
                    rotation = Matrix.CreateFromAxisAngle(currentMatrix.Forward, sign * angle);
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }

            rotatedMatrix = currentMatrix;
            rotatedMatrix *= rotation;

            return CheckValidBlockRotation(rotatedMatrix, blockDirection, blockRotation);
        }

        public void ActivateBlockCreation(MyDefinitionId? blockDefinitionId = null)
        {
            if (MySession.Static.CameraController == null || !MySession.Static.CameraController.AllowCubeBuilding)
            {
                return;
            }

            UpdateCubeBlockDefinition(blockDefinitionId);

            if (CopyPasteIsActivated)
                IntersectionDistance = DEFAULT_BLOCK_BUILDING_DISTANCE;
            m_clipboard.Deactivate();
            m_floatingObjectClipboard.Deactivate();
            m_voxelClipboard.Deactivate();
            m_shipCreationClipboard.Deactivate();

            if (!MyFakes.ENABLE_MULTIBLOCKS || !MultiBlockCreationIsActivated)
                m_multiBlockCreationClipboard.Deactivate();

            if (MySession.Static.CreativeMode)
            {
                AllowFreeSpacePlacement = false;
                MaxGridDistanceFrom = null;
                ShowRemoveGizmo = MyFakes.SHOW_REMOVE_GIZMO;
            }
            else if (MySession.Static.SimpleSurvival)
            {
                AllowFreeSpacePlacement = false;
                ShowRemoveGizmo = MyFakes.SHOW_REMOVE_GIZMO;
            }
            else
            {
                AllowFreeSpacePlacement = false;
                ShowRemoveGizmo = true;
            }

            ActivateNotifications();

            MyHud.Crosshair.Show(null);

            MyCubeBuilder.Static.UpdateNotificationBlockNotAvailable();

            BlockCreationIsActivated = !MultiBlockCreationIsActivated;

            UpdatePasteNotification(MySpaceTexts.CubeBuilderPasteNotification);
        }

        public void DeactivateBlockCreation()
        {
            BlockCreationIsActivated = false;
            DeactivateNotifications();
            MyCubeBuilder.Static.UpdateNotificationBlockNotAvailable();
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

        private void ActivateBuildModeNotifications(bool joystick)
        {
            if (joystick)
            {
                MyHud.Notifications.Remove(m_buildModeHint);
                MyHud.Notifications.Add(JoystickRotationHint);
                if (MyFakes.ENABLE_COMPOUND_BLOCKS)
                    MyHud.Notifications.Add(JoystickCompoundModeHint);
                if (MyFakes.ENABLE_CUBE_BUILDER_DYNAMIC_MODE)
                    MyHud.Notifications.Add(JoystickDynamicModeHint);
            }
            else
            {
                MyHud.Notifications.Add(BlockRotationHint);
                if (MyFakes.ENABLE_COMPOUND_BLOCKS)
                    MyHud.Notifications.Add(CompoundModeHint);
                if (MyFakes.ENABLE_CUBE_BUILDER_DYNAMIC_MODE)
                    MyHud.Notifications.Add(DynamicModeHint);
            }
        }

        private void DeactivateBuildModeNotifications()
        {
            if (MyInput.Static.IsJoystickConnected() && IsActivated)
                MyHud.Notifications.Add(m_buildModeHint);

            MyHud.Notifications.Remove(BlockRotationHint);
            MyHud.Notifications.Remove(JoystickRotationHint);

            if (MyFakes.ENABLE_COMPOUND_BLOCKS)
            {
                MyHud.Notifications.Remove(CompoundModeHint);
                MyHud.Notifications.Remove(JoystickCompoundModeHint);
            }

            if (MyFakes.ENABLE_CUBE_BUILDER_DYNAMIC_MODE)
            {
                MyHud.Notifications.Remove(DynamicModeHint);
                MyHud.Notifications.Remove(JoystickDynamicModeHint);
            }
        }

        private void InitializeNotifications()
        {
            // keyboard mouse notifications
            {
                var next = MyInput.Static.GetGameControl(MyControlsSpace.SWITCH_LEFT);
                var prev = MyInput.Static.GetGameControl(MyControlsSpace.SWITCH_RIGHT);
                var compoundToggle = MyInput.Static.GetGameControl(MyControlsSpace.SWITCH_COMPOUND);
                var buildingModeToggle = MyInput.Static.GetGameControl(MyControlsSpace.SWITCH_BUILDING_MODE);
                var build = MyInput.Static.GetGameControl(MyControlsSpace.PRIMARY_TOOL_ACTION);
                var rotxp = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE);
                var rotxn = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE);
                var rotyp = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE);
                var rotyn = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE);
                var rotzp = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE);
                var rotzn = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE);

                // This will combine controls which has name
                var controlHelper = new MyHudNotifications.ControlsHelper(rotxp, rotxn, rotzp, rotzn, rotyp, rotyn);

                BlockRotationHint = MyHudNotifications.CreateControlNotification(MySpaceTexts.NotificationRotationFormatCombined, controlHelper);
                ColorHint = MyHudNotifications.CreateControlNotification(MySpaceTexts.NotificationColorFormat, next, prev, "MMB", "CTRL", "SHIFT");
                BuildingHint = MyHudNotifications.CreateControlNotification(MySpaceTexts.NotificationBuildingFormat, build);
                UnlimitedBuildingHint = MyHudNotifications.CreateControlNotification(MySpaceTexts.NotificationUnlimitedBuildingFormat, "LMB", "RMB", "CTRL");
                if (MyFakes.ENABLE_COMPOUND_BLOCKS)
                    CompoundModeHint = MyHudNotifications.CreateControlNotification(MySpaceTexts.NotificationCompoundBuildingFormat, compoundToggle, "ALT");
                if (MyFakes.ENABLE_CUBE_BUILDER_DYNAMIC_MODE)
                    DynamicModeHint = MyHudNotifications.CreateControlNotification(MySpaceTexts.NotificationSwitchBuildingModeFormat, buildingModeToggle);
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

                StringBuilder sb = new StringBuilder();
                var rotation = new HashSet<char>() { rotateBlockCode1, rotateBlockCode2, rotateBlockCode3, rotateBlockCode4, rotateBlockRollCode, rotateBlockRollCode2 };
                foreach (var c in rotation)
                    sb.Append(c);

                JoystickRotationHint = MyHudNotifications.CreateControlNotification(MySpaceTexts.NotificationRotationFormatCombined, sb.ToString().Trim());
                JoystickBuildingHint = MyHudNotifications.CreateControlNotification(MySpaceTexts.NotificationBuildingFormat, primaryActionCode);
                JoystickUnlimitedBuildingHint = MyHudNotifications.CreateControlNotification(MySpaceTexts.NotificationJoystickUnlimitedBuildingFormat, primaryActionCode, secondaryActionCode);
                if (MyFakes.ENABLE_COMPOUND_BLOCKS)
                    JoystickCompoundModeHint = MyHudNotifications.CreateControlNotification(MySpaceTexts.NotificationJoystickCompoundBuildingFormat, compoundCode);
                if (MyFakes.ENABLE_CUBE_BUILDER_DYNAMIC_MODE)
                    JoystickDynamicModeHint = MyHudNotifications.CreateControlNotification(MySpaceTexts.NotificationSwitchBuildingModeFormat, dynamicModeCode);
                m_buildModeHint = MyHudNotifications.CreateControlNotification(MySpaceTexts.NotificationHintPressToOpenBuildMode, buildModeCode);
            }
        }

        public override void Deactivate()
        {
            HideStationRotationNotification();
            DeactivateShipCreationClipboard();
            DeactivateCopyPaste();
            DeactivateCopyPasteFloatingObject();
            DeactivateMultiBlockClipboard();
            DeactivateBlockCreation();
            CurrentBlockDefinition = null;
            CurrentGrid = null;
            CurrentVoxelMap = null;
            IsBuildMode = false;

            DynamicMode = false;
            IntersectionDistance = DEFAULT_BLOCK_BUILDING_DISTANCE;
        }

        public override void Activate()
        {
            if (MySession.Static.CameraController != null && MySession.Static.CameraController.AllowCubeBuilding)
            {
                Debug.Assert(!CopyPasteIsActivated || !ShipCreationIsActivated, "Both copy-paste and ship creation cannot be activated at the same time");
                if (!CopyPasteIsActivated && !ShipCreationIsActivated)
                {
                    ActivateBlockCreation(CurrentBlockDefinition != null ? CurrentBlockDefinition.Id : (MyDefinitionId?)null);
                }
            }
        }

        private void UpdateCubeBlockStageDefinition(MyCubeBlockDefinition stageCubeBlockDefinition)
        {
            Debug.Assert(stageCubeBlockDefinition != null);

            CurrentBlockDefinition = stageCubeBlockDefinition;

            if (MyFakes.ENABLE_MULTIBLOCKS)
                UpdateMultiBlock(false);
        }

        private void UpdateCubeBlockDefinition(MyDefinitionId? id)
        {
            if (id.HasValue)
            {
                if (CurrentBlockDefinition != null)
                {
                    var group = MyDefinitionManager.Static.GetDefinitionGroup(CurrentBlockDefinition.BlockPairName);

                    if (CurrentBlockDefinitionStages.Count > 1)
                    {
                        group = MyDefinitionManager.Static.GetDefinitionGroup(CurrentBlockDefinitionStages[0].BlockPairName);
                        if (group.Small != null)
                            m_stageIndexByDefinitionHash[group.Small.Id] = CurrentBlockDefinitionStages.IndexOf(CurrentBlockDefinition);

                        if (group.Large != null)
                            m_stageIndexByDefinitionHash[group.Large.Id] = CurrentBlockDefinitionStages.IndexOf(CurrentBlockDefinition);
                    }

                    var rotation = Quaternion.CreateFromRotationMatrix(m_gizmo.SpaceDefault.m_localMatrixAdd);
                    if (group.Small != null) m_rotationsByDefinitionHash[group.Small.Id] = rotation;
                    if (group.Large != null) m_rotationsByDefinitionHash[group.Large.Id] = rotation;

                }
                var tmpDef = MyDefinitionManager.Static.GetCubeBlockDefinition(id.Value);
                if (tmpDef.Public || MyFakes.ENABLE_NON_PUBLIC_BLOCKS)
                    CurrentBlockDefinition = tmpDef;
                else
                {
                    CurrentBlockDefinition = tmpDef.CubeSize == MyCubeSize.Large ?
                        MyDefinitionManager.Static.GetDefinitionGroup(tmpDef.BlockPairName).Small :
                        MyDefinitionManager.Static.GetDefinitionGroup(tmpDef.BlockPairName).Large;
                }

                if (CurrentBlockDefinition != null)
                {
                    m_gizmo.RotationOptions = MyCubeGridDefinitions.GetCubeRotationOptions(CurrentBlockDefinition);
                    Quaternion lastRot;

                    MyDefinitionId defBlockId = id.Value;
                    if (CurrentBlockDefinitionStages.Count > 1)
                        defBlockId = CurrentBlockDefinitionStages[0].Id;

                    if (m_rotationsByDefinitionHash.TryGetValue(defBlockId, out lastRot))
                        m_gizmo.SpaceDefault.m_localMatrixAdd = Matrix.CreateFromQuaternion(lastRot);
                    else
                        m_gizmo.SpaceDefault.m_localMatrixAdd = Matrix.Identity;

                    if (CurrentBlockDefinitionStages.Count > 1)
                    {
                        int lastStage;
                        if (m_stageIndexByDefinitionHash.TryGetValue(defBlockId, out lastStage))
                        {
                            if (lastStage >= 0 && lastStage < CurrentBlockDefinitionStages.Count)
                                CurrentBlockDefinition = CurrentBlockDefinitionStages[lastStage];
                        }
                    }

                    if (MyFakes.ENABLE_MULTIBLOCKS)
                        UpdateMultiBlock();
                }
            }
        }

        private void UpdateMultiBlock(bool resetOrientation = true)
        {
            Matrix orientationMatrix = m_gizmo.SpaceDefault.m_localMatrixAdd;

            if (m_multiBlockCreationClipboard.IsActive)
            {
                if (!resetOrientation)
                    orientationMatrix = m_multiBlockCreationClipboard.GetFirstGridOrientationMatrix();

                m_multiBlockCreationClipboard.Deactivate();
            }

            if (CurrentBlockDefinition.MultiBlock != null)
            {
                MyDefinitionId defId = new MyDefinitionId(typeof(MyObjectBuilder_MultiBlockDefinition), CurrentBlockDefinition.MultiBlock);
                MyMultiBlockDefinition multiBlockDef = MyDefinitionManager.Static.GetMultiBlockDefinition(defId);
                if (multiBlockDef != null)
                {
                    StartNewGridPlacement(multiBlockDef, orientationMatrix, false);
                }
                else
                {
                    Debug.Assert(false);
                    CurrentBlockDefinition = null;
                }
            }
            else
            {
                BlockCreationIsActivated = true;
            }
        }

        #endregion

        #region Render gizmo

        void AddFastBuildModels(MyCubeBuilderGizmo.MyGizmoSpaceProperties gizmoSpace, MatrixD baseMatrix, List<MatrixD> matrices, List<string> models, MyCubeBlockDefinition definition)
        {
            AddFastBuildModelWithSubparts(ref baseMatrix, matrices, models, definition);

            if (CurrentBlockDefinition != null && gizmoSpace.m_startBuild != null && gizmoSpace.m_continueBuild != null)
            {
                Vector3I rotatedSize;
                Vector3I.TransformNormal(ref CurrentBlockDefinition.Size, ref gizmoSpace.m_localMatrixAdd, out rotatedSize);
                rotatedSize = Vector3I.Abs(rotatedSize);

                Vector3I stepDelta;
                Vector3I counter;
                int stepCount;

                ComputeSteps(gizmoSpace.m_startBuild.Value, gizmoSpace.m_continueBuild.Value, rotatedSize, out stepDelta, out counter, out stepCount);

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
                            AddFastBuildModelWithSubparts(ref matrix, matrices, models, definition);
                        }
                    }
                }
            }
        }

        private void ShowStationRotationNotification()
        {
            if (m_stationRotationNotification == null && m_shipCreationClipboard.EnableStationRotation)
                m_stationRotationNotification = new MyHudNotification(MySpaceTexts.NotificationStationRotation, 0, priority: 1);

            if (m_shipCreationClipboard.EnableStationRotation)
            {
                MyHud.Notifications.Add(m_stationRotationNotification);
            }
            else
            {
                MyHud.Notifications.Remove(m_stationRotationNotification);
            }
        }

        private void HideStationRotationNotification()
        {
            if (m_stationRotationNotification != null)
            {
                MyHud.Notifications.Remove(m_stationRotationNotification);
            }
        }

        public void EnableStationRotation()
        {
            m_shipCreationClipboard.EnableStationRotation = !m_shipCreationClipboard.EnableStationRotation;
            m_clipboard.EnableStationRotation = !m_clipboard.EnableStationRotation;
            m_floatingObjectClipboard.EnableStationRotation = !m_floatingObjectClipboard.EnableStationRotation;
            ShowStationRotationNotification();     
        }

        public bool HandleGameInput()
        {
            m_rotationHintRotating = false;
            int frameDt = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastInputHandleTime;
            m_lastInputHandleTime += frameDt;

            bool disallowCockpitBuilding = MySession.ControlledEntity is MyCockpit && !SpectatorIsBuilding;

            // Don't allow cube builder when paused or when in cockpit and not in developer spectator mode
            if (MySandboxGame.IsPaused || disallowCockpitBuilding)
                return false;

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

            if (MyInput.Static.IsNewGameControlPressed(ME_SI_VIEW))
            {
                MyCubeGrid.ShowStructuralIntegrity = !MyCubeGrid.ShowStructuralIntegrity;
            }

            var context = (IsActivated && MySession.ControlledEntity is MyCharacter) ? MySession.ControlledEntity.ControlContext : MyStringId.NullOrEmpty;

            if (IsActivated && MyControllerHelper.IsControl(context, MyControlsSpace.BUILD_MODE))
            {
                IsBuildMode = !IsBuildMode;
            }

            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.STATION_ROTATION) && (ShipCreationIsActivated || CopyPasteIsActivated))
            {
                EnableStationRotation();
            }

            // When spectator active, building is instant
            if (MySession.Static.CreativeMode || SpectatorIsBuilding)
            {
                if (MySession.Static.EnableCopyPaste)
                {
                    if (MyInput.Static.IsNewKeyPressed(MyKeys.C) && MyInput.Static.IsAnyCtrlKeyPressed() && !MyInput.Static.IsAnyMousePressed())
                    {
                        if (MySession.Static.CameraController is MyCharacter || MySession.GetCameraControllerEnum() == MyCameraControllerEnum.Spectator)
                        {
                            MyGuiAudio.PlaySound(MyGuiSounds.HudClick);

                            if (m_clipboard.IsActive == false)
                            {
                                MySessionComponentVoxelHand.Static.Enabled = false;
                                DeactivateMultiBlockClipboard();

                                if (!MyInput.Static.IsAnyShiftKeyPressed())
                                {
                                    m_clipboard.CopyGroup(MyCubeGrid.GetTargetGrid());
                                }
                                else
                                    m_clipboard.CopyGrid(MyCubeGrid.GetTargetGrid());
                                UpdatePasteNotification(MySpaceTexts.CubeBuilderPasteNotification);
                            }
                            return true;
                        }
                    }

                    if (MyInput.Static.IsNewKeyPressed(MyKeys.X) && MyInput.Static.IsAnyCtrlKeyPressed())
                    {
                        MyGuiAudio.PlaySound(MyGuiSounds.HudClick);

                        MyEntity entity = MyCubeGrid.GetTargetEntity();

                        if (entity == null)
                        {
                            return true;
                        }
                        else if (entity is MyCubeGrid && m_clipboard.IsActive == false)
                        {
                            bool cutGroup = !MyInput.Static.IsAnyShiftKeyPressed();

                            if (MyFakes.CLIPBOARD_CUT_CONFIRMATION)
                            {
                                MyEntities.EnableEntityBoundingBoxDraw(entity, true);

                                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                buttonType: MyMessageBoxButtonsType.YES_NO,
                                messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextAreYouSureToMoveGridToClipboard),
                                messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionPleaseConfirm),
                                callback: (v) =>
                                {
                                    if (v == MyGuiScreenMessageBox.ResultEnum.YES)
                                        OnCutConfirm(entity as MyCubeGrid, cutGroup);

                                    MyEntities.EnableEntityBoundingBoxDraw(entity, false);
                                }));
                            }
                            else
                                OnCutConfirm(entity as MyCubeGrid, cutGroup);

                        }
                        else if (entity is MyVoxelMap && m_voxelClipboard.IsActive == false &&
                            MyPerGameSettings.GUI.VoxelMapEditingScreen == typeof(MyGuiScreenDebugSpawnMenu) // hack to disable this in ME
                            )
                        {
                            if (MyFakes.CLIPBOARD_CUT_CONFIRMATION)
                            {
                                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                    buttonType: MyMessageBoxButtonsType.YES_NO,
                                    messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextAreYouSureToRemoveAsteroid),
                                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionPleaseConfirm),
                                    callback: (v) =>
                                    {
                                        if (v == MyGuiScreenMessageBox.ResultEnum.YES)
                                            OnCutAsteroidConfirm(entity as MyVoxelMap);
                                        MyEntities.EnableEntityBoundingBoxDraw(entity, false);
                                    }));
                            }
                            else
                                OnCutAsteroidConfirm(entity as MyVoxelMap);
                        }
                        return true;
                    }

                    if (MyInput.Static.IsNewKeyPressed(MyKeys.V) && MyInput.Static.IsAnyCtrlKeyPressed() && !MyInput.Static.IsAnyShiftKeyPressed())
                    {
                        DeactivateBlockCreation();
                        ShowStationRotationNotification();
                        if (m_clipboard.PasteGrid())
                        {
                            UpdatePasteNotification(MySpaceTexts.CubeBuilderPasteNotification);
                            return true;
                        }
                    }

                    if (HandleBlockCreationMovement(context))
                        return true;
                }

                if (MyInput.Static.IsAnyCtrlKeyPressed() && m_shipCreationClipboard.IsActive)
                {
                    if (MyInput.Static.PreviousMouseScrollWheelValue() < MyInput.Static.MouseScrollWheelValue())
                    {
                        m_shipCreationClipboard.MoveEntityFurther();
                        return true;
                    }
                    else if (MyInput.Static.PreviousMouseScrollWheelValue() > MyInput.Static.MouseScrollWheelValue())
                    {
                        m_shipCreationClipboard.MoveEntityCloser();
                        return true;
                    }
                }

                if (MyInput.Static.IsNewKeyPressed(MyKeys.Escape))
                {
                    if (m_clipboard.IsActive)
                    {
                        HideStationRotationNotification();
                        m_clipboard.Deactivate();
                        UpdatePasteNotification(MySpaceTexts.CubeBuilderPasteNotification);
                        return true;
                    }

                    if (m_floatingObjectClipboard.IsActive)
                    {
                        m_floatingObjectClipboard.Deactivate();
                        UpdatePasteNotification(MySpaceTexts.CubeBuilderPasteNotification);
                        return true;
                    }

                    if (m_voxelClipboard.IsActive)
                    {
                        m_voxelClipboard.Deactivate();
                        UpdatePasteNotification(MySpaceTexts.CubeBuilderPasteNotification);
                        return true;
                    }

                    if (m_shipCreationClipboard.IsActive)
                    {
                        HideStationRotationNotification();
                        m_shipCreationClipboard.Deactivate();
                        UpdatePasteNotification(MySpaceTexts.CubeBuilderPasteNotification);
                        return true;
                    }

                    //if (m_multiBlockCreationClipboard.IsActive)
                    //{
                    //    m_multiBlockCreationClipboard.Deactivate();
                    //    UpdatePasteNotification(MySpaceTexts.CubeBuilderPasteNotification);
                    //    return true;
                    //}
                }

                if (MyInput.Static.IsNewGameControlPressed(ME_PICK_BLOCK) && !MyInput.Static.IsAnyCtrlKeyPressed())
                {
                    LineD line = new LineD(MySector.MainCamera.Position, MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 100);
                    MyCubeGrid grid = m_currentGrid;
                    Vector3D hitPos;
                    if (grid != null || MyCubeGrid.TryRayCastGrid(ref line, out grid, out hitPos))
                    {
                        var result = grid.RayCastBlocks(MySector.MainCamera.Position, MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 100);
                        if (result.HasValue)
                        {
                            var slot = MyToolbarComponent.CurrentToolbar.SelectedSlot;
                            SelectBlockToToolbar(grid.GetCubeBlock(result.Value), slot.HasValue ? false : true);
                            if (slot.HasValue)
                                MyToolbarComponent.CurrentToolbar.ActivateItemAtSlot(slot.Value);
                        }
                    }
                }

                if (MyInput.Static.IsNewLeftMousePressed() || MyControllerHelper.IsControl(context, MyControlsSpace.COPY_PASTE_ACTION))
                {
                    if (m_clipboard.IsActive)
                    {
                        if (m_clipboard.PasteGrid())
                        {
                            HideStationRotationNotification();
                            UpdatePasteNotification(MySpaceTexts.CubeBuilderPasteNotification);
                            return true;
                        }
                    }

                    if (m_floatingObjectClipboard.IsActive)
                    {
                        if (m_floatingObjectClipboard.PasteFloatingObject())
                        {
                            UpdatePasteNotification(MySpaceTexts.CubeBuilderPasteNotification);
                            return true;
                        }
                    }

                    if (m_voxelClipboard.IsActive)
                    {
                        if (m_voxelClipboard.PasteVoxelMap())
                        {
                            UpdatePasteNotification(MySpaceTexts.CubeBuilderPasteNotification);
                            return true;
                        }
                    }

                    if (m_shipCreationClipboard.IsActive)
                    {
                        if (m_shipCreationClipboard.PasteGrid())
                        {
                            HideStationRotationNotification();
                            MyGuiAudio.PlaySound(MyGuiSounds.HudPlaceBlock);
                            return true;
                        }
                    }

                    if (m_multiBlockCreationClipboard.IsActive)
                    {
                        if (m_multiBlockCreationClipboard.PasteGrid(deactivate: false))
                            MyGuiAudio.PlaySound(MyGuiSounds.HudPlaceBlock);

                        return true;
                    }

                }

                if (CurrentGrid != null)
                {

                    if (MyControllerHelper.IsControl(context, MyControlsSpace.SYMMETRY_SWITCH, MyControlStateType.NEW_PRESSED))
                    {
                        if (BlockCreationIsActivated)
                            MyGuiAudio.PlaySound(MyGuiSounds.HudClick);

                        switch (m_symmetrySettingMode)
                        {
                            case MySymmetrySettingModeEnum.NoPlane:
                                m_symmetrySettingMode = MySymmetrySettingModeEnum.XPlane;
                                UpdateSymmetryNotification(MySpaceTexts.SettingSymmetryX);
                                break;
                            case MySymmetrySettingModeEnum.XPlane:
                                m_symmetrySettingMode = MySymmetrySettingModeEnum.XPlaneOdd;
                                UpdateSymmetryNotification(MySpaceTexts.SettingSymmetryXOffset);
                                break;
                            case MySymmetrySettingModeEnum.XPlaneOdd:
                                m_symmetrySettingMode = MySymmetrySettingModeEnum.YPlane;
                                UpdateSymmetryNotification(MySpaceTexts.SettingSymmetryY);
                                break;
                            case MySymmetrySettingModeEnum.YPlane:
                                m_symmetrySettingMode = MySymmetrySettingModeEnum.YPlaneOdd;
                                UpdateSymmetryNotification(MySpaceTexts.SettingSymmetryYOffset);
                                break;
                            case MySymmetrySettingModeEnum.YPlaneOdd:
                                m_symmetrySettingMode = MySymmetrySettingModeEnum.ZPlane;
                                UpdateSymmetryNotification(MySpaceTexts.SettingSymmetryZ);
                                break;
                            case MySymmetrySettingModeEnum.ZPlane:
                                m_symmetrySettingMode = MySymmetrySettingModeEnum.ZPlaneOdd;
                                UpdateSymmetryNotification(MySpaceTexts.SettingSymmetryZOffset);
                                break;
                            case MySymmetrySettingModeEnum.ZPlaneOdd:
                                m_symmetrySettingMode = MySymmetrySettingModeEnum.NoPlane;
                                RemoveSymmetryNotification();
                                break;
                        }
                    }

                    if (MyControllerHelper.IsControl(context, MyControlsSpace.USE_SYMMETRY, MyControlStateType.NEW_PRESSED))
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

                    if (IsInSymmetrySettingMode)
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
                        } //if (input.IsNewLeftMousePressed())

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
                    } //if (IsInSymmetrySettingMode)


                    if (MyInput.Static.IsNewKeyPressed(MyKeys.Escape))
                    {
                        if (m_symmetrySettingMode != MySymmetrySettingModeEnum.NoPlane)
                        {
                            m_symmetrySettingMode = MySymmetrySettingModeEnum.NoPlane;
                            RemoveSymmetryNotification();
                            return true;
                        }

                        if (m_gizmo.SpaceDefault.m_continueBuild != null)
                        {
                            m_gizmo.SpaceDefault.m_startBuild = null;
                            m_gizmo.SpaceDefault.m_startRemove = null;
                            m_gizmo.SpaceDefault.m_continueBuild = null;
                            return true;
                        }
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

                            Remove();
                        }
                    }

                    if (MyInput.Static.IsLeftMousePressed() ||
                        MyInput.Static.IsRightMousePressed())
                    {
                        ContinueBuilding(MyInput.Static.IsAnyShiftKeyPressed() || BuildingMode == BuildingModeEnum.Plane);
                    }

                    if (MyInput.Static.IsNewLeftMouseReleased() ||
                        MyInput.Static.IsNewRightMouseReleased())
                    {
                        StopBuilding();
                    }               
                } //if (CurrentGrid != null)
                else if (CurrentVoxelMap != null)
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
                        //if (MyInput.Static.IsAnyCtrlKeyPressed() || BuildingMode != BuildingModeEnum.SingleBlock)
                        //{
                        //    StartBuilding();
                        //}
                        //else
                        {
                            Add();
                        }
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
                } //if (CurrentVoxelMap != null)
                else if (DynamicMode)
                {
                    if (MyControllerHelper.IsControl(context, MyControlsSpace.PRIMARY_TOOL_ACTION))
                    {
                        Add();
                    }
                } // if (DynamicMode)
            }
            else if (MySession.Static.SimpleSurvival)
            {
                if (DynamicMode && HandleBlockCreationMovement(context))
                    return true;

                if (MyControllerHelper.IsControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.SECONDARY_TOOL_ACTION))
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

                    Remove();
                }
            }
            else if (MyFakes.ENABLE_BATTLE_SYSTEM && MySession.Static.Battle) 
            {
                if (MyControllerHelper.IsControl(context, MyControlsSpace.PRIMARY_TOOL_ACTION))
                {
                    if (m_clipboard.IsActive)
                    {
                        if (m_clipboard.PasteGrid())
                        {
                            UpdatePasteNotification(MySpaceTexts.CubeBuilderPasteNotification);
                            return true;
                        }
                    }
                }
            }

            if (MyControllerHelper.IsControl(context, MyControlsSpace.SWITCH_COMPOUND, MyControlStateType.NEW_PRESSED))
                CompoundEnabled = !CompoundEnabled;

            if (MyInput.Static.IsNewGameControlPressed(ME_PRESS_TO_COMPOUND))
                CompoundEnabled = true;

            if (MyInput.Static.IsNewGameControlReleased(ME_PRESS_TO_COMPOUND))
                CompoundEnabled = false;

            if (MyInput.Static.IsNewKeyPressed(MyKeys.B) && MyInput.Static.IsAnyCtrlKeyPressed() && !MyInput.Static.IsAnyMousePressed())
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudClick);

                if (!m_clipboard.IsActive && !MySession.Static.Battle)
                {
                    MySessionComponentVoxelHand.Static.Enabled = false;
                    var copiedGrid = MyCubeGrid.GetTargetGrid();
                    if (!MyInput.Static.IsAnyShiftKeyPressed())
                        m_clipboard.CopyGroup(copiedGrid);
                    else
                        m_clipboard.CopyGrid(copiedGrid);

                    UpdatePasteNotification(MySpaceTexts.CubeBuilderPasteNotification);

                    var blueprintScreen = new MyGuiBlueprintScreen(m_clipboard);
                    if (copiedGrid != null)
                    {
                        blueprintScreen.CreateFromClipboard(true);
                    }
                    m_clipboard.Deactivate();
                    MyGuiSandbox.AddScreen(blueprintScreen);
                }
                return true;
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.Escape))
            {
                if (m_shipCreationClipboard.IsActive)
                {
                    /*m_shipCreationClipboard.Deactivate();*/
                    Deactivate();
                    return true;
                }
            }

            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.LANDING_GEAR) && MySession.LocalHumanPlayer != null && MySession.LocalHumanPlayer.Identity.Character == MySession.ControlledEntity)
            {
                if (!MyInput.Static.IsAnyShiftKeyPressed() && MyGuiScreenGamePlay.ActiveGameplayScreen == null)
                {
                    MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                    MyGuiSandbox.AddScreen(MyGuiScreenGamePlay.ActiveGameplayScreen = new MyGuiScreenColorPicker());
                }
            }

            if (CurrentGrid != null && MyInput.Static.IsNewGameControlPressed(MyControlsSpace.LANDING_GEAR))
            {
                if (MyInput.Static.IsAnyShiftKeyPressed())
                {
                    foreach (var gizmoSpace in m_gizmo.Spaces)
                    {
                        if (gizmoSpace.m_removeBlock != null)
                            MyToolbar.AddOrSwitchToColor(gizmoSpace.m_removeBlock.ColorMaskHSV);
                    }
                }
            }

            if (CurrentGrid != null && MyInput.Static.IsGameControlPressed(MyControlsSpace.CUBE_COLOR_CHANGE))
            {
                int expand = MyInput.Static.IsAnyCtrlKeyPressed() ? 1 : 0;
                expand = MyInput.Static.IsAnyShiftKeyPressed() ? 3 : expand;
                Change(expand);
            }

            if (IsActivated)
            {
                for (int i = 0; i < 6; ++i)
                {
                    bool standardRotation = MyControllerHelper.IsControl(context, m_rotationControls[i], MyControlStateType.PRESSED);
                    if (standardRotation)
                    {
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
                            m_rotationHintRotating |= !newPress;
                            RotateAxis(axis, direction, newPress, frameDt);
                        }
                    }
                }
            }

            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.SWITCH_LEFT))
            {
                if (IsActivated && (CurrentBlockDefinition == null || MyFakes.ENABLE_BLOCK_COLORING))
                {
                    MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                    MyToolbar.PrevColorSlot();
                }
            }
            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.SWITCH_RIGHT))
            {
                if (IsActivated && (CurrentBlockDefinition == null || MyFakes.ENABLE_BLOCK_COLORING))
                {
                    MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                    MyToolbar.NextColorSlot();
                }
            }

            if (MyFakes.ENABLE_BLOCK_STAGES && CurrentBlockDefinition != null && CurrentBlockDefinitionStages.Count > 0)
            {
                bool? switchForward = null;

                if (!MyInput.Static.IsGameControlPressed(MyControlsSpace.LOOKAROUND) && !MyInput.Static.IsAnyCtrlKeyPressed() && !MyInput.Static.IsAnyShiftKeyPressed())
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
                    int currDefinitionIndex = CurrentBlockDefinitionStages.IndexOf(CurrentBlockDefinition);
                    int nextIndex;

                    if (switchForward.Value)
                    {
                        nextIndex = 0;
                        if (currDefinitionIndex != -1 && currDefinitionIndex < CurrentBlockDefinitionStages.Count - 1)
                            nextIndex = currDefinitionIndex + 1;
                    }
                    else
                    {
                        nextIndex = CurrentBlockDefinitionStages.Count - 1;
                        if (currDefinitionIndex != -1 && currDefinitionIndex > 0)
                            nextIndex = currDefinitionIndex - 1;
                    }

                    UpdateCubeBlockStageDefinition(CurrentBlockDefinitionStages[nextIndex]);
                }
            }

            if (MyFakes.ENABLE_CUBE_BUILDER_DYNAMIC_MODE
                && (CurrentBlockDefinition != null || (MyFakes.ENABLE_ALTERNATIVE_CLIPBOARD && m_clipboard.IsActive))
                &&  MyControllerHelper.IsControl(context, MyControlsSpace.SWITCH_BUILDING_MODE, MyControlStateType.NEW_PRESSED))
            {
                DynamicMode = !DynamicMode;

                if (DynamicMode)
                {
                    if (MySession.Static.SurvivalMode && !SpectatorIsBuilding && CurrentBlockDefinition != null)
                    {
                        float gridSize = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize);
                        float maxSize = MathHelper.Max(CurrentBlockDefinition.Size.X, CurrentBlockDefinition.Size.Y, CurrentBlockDefinition.Size.Z) * gridSize;
                        IntersectionDistance = 1 + maxSize;
                        m_multiBlockCreationClipboard.SetDragDistance(IntersectionDistance);
                    }
                }
                else
                {
                    IntersectionDistance = DEFAULT_BLOCK_BUILDING_DISTANCE;
                    m_multiBlockCreationClipboard.SetDragDistance(IntersectionDistance);
                }

                if (MyFakes.ENABLE_ALTERNATIVE_CLIPBOARD)
                {
                    ((MyGridClipboard2)m_clipboard).DynamicModeChanged();
                }
            }

            return false;
        }

        private bool HandleBlockCreationMovement(MyStringId context)
        {
            bool ctrl = MyInput.Static.IsAnyCtrlKeyPressed();
            if ((ctrl && MyInput.Static.PreviousMouseScrollWheelValue() < MyInput.Static.MouseScrollWheelValue())
                || MyControllerHelper.IsControl(context, MyControlsSpace.MOVE_FURTHER, MyControlStateType.PRESSED))
            {
                bool handled = false;
                if (m_clipboard.IsActive)
                {
                    m_clipboard.MoveEntityFurther();
                    handled = true;
                }

                if (m_floatingObjectClipboard.IsActive)
                {
                    m_floatingObjectClipboard.MoveEntityFurther();
                    handled = true;
                }

                if (m_voxelClipboard.IsActive)
                {
                    m_voxelClipboard.MoveEntityFurther();
                    handled = true;
                }

                if (m_multiBlockCreationClipboard.IsActive)
                {
                    m_multiBlockCreationClipboard.MoveEntityFurther();
                    handled = true;
                }

                if (DynamicMode)
                {
                    float previousIntersectionDistance = IntersectionDistance;
                    IntersectionDistance *= 1.1f;
                    if (IntersectionDistance > MAX_BLOCK_BUILDING_DISTANCE)
                        IntersectionDistance = MAX_BLOCK_BUILDING_DISTANCE;

                    if (MySession.Static.SurvivalMode && !SpectatorIsBuilding)
                    {
                        float gridSize = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize);
                        BoundingBoxD localAABB = new BoundingBoxD(-CurrentBlockDefinition.Size * gridSize * 0.5f, CurrentBlockDefinition.Size * gridSize * 0.5f);
                        MatrixD gizmoMatrix = m_gizmo.SpaceDefault.m_worldMatrixAdd;
                        gizmoMatrix.Translation = FreePlacementTarget;
                        MatrixD inverseDrawMatrix = MatrixD.Invert(gizmoMatrix);
                        if (!MyCubeBuilderGizmo.DefaultGizmoCloseEnough(ref inverseDrawMatrix, localAABB, gridSize, IntersectionDistance))
                            IntersectionDistance = previousIntersectionDistance;

                        m_multiBlockCreationClipboard.SetDragDistance(IntersectionDistance);
                    }

                    handled = true;
                }

                return handled;
            }
            else if ((ctrl && MyInput.Static.PreviousMouseScrollWheelValue() > MyInput.Static.MouseScrollWheelValue())
                || MyControllerHelper.IsControl(context, MyControlsSpace.MOVE_CLOSER, MyControlStateType.PRESSED))
            {
                bool handled = false;

                if (m_clipboard.IsActive)
                {
                    m_clipboard.MoveEntityCloser();
                    handled = true;
                }

                if (m_floatingObjectClipboard.IsActive)
                {
                    m_floatingObjectClipboard.MoveEntityCloser();
                    handled = true;
                }

                if (m_voxelClipboard.IsActive)
                {
                    m_voxelClipboard.MoveEntityCloser();
                    handled = true;
                }

                if (m_multiBlockCreationClipboard.IsActive)
                {
                    m_multiBlockCreationClipboard.MoveEntityCloser();
                    handled = true;
                }

                if (DynamicMode)
                {
                    IntersectionDistance /= 1.1f;
                    if (IntersectionDistance < MIN_BLOCK_BUILDING_DISTANCE)
                        IntersectionDistance = MIN_BLOCK_BUILDING_DISTANCE;

                    handled = true;
                }

                return handled;
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

            if (index < 2)
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

        internal static void PrepareCharacterCollisionPoints(List<Vector3> outList)
        {
            MyCharacter character = (MySession.ControlledEntity as MyCharacter);

            float height = MyCharacter.CharacterHeight * 0.7f;
            float width = MyCharacter.CharacterWidth * 0.2f;

            if (character != null)
            {
                if (character.IsCrouching)
                    height = MyCharacter.CrouchHeight;

                Vector3 upVec = character.PositionComp.LocalMatrix.Up * height;
                Vector3 fwVec = character.PositionComp.LocalMatrix.Forward * width;
                Vector3 rtVec = character.PositionComp.LocalMatrix.Right * width;
                Vector3 pos = character.Entity.PositionComp.GetPosition() + character.PositionComp.LocalMatrix.Up * 0.2f;

                float angle = 0.0f;
                for (int i = 0; i < 6; ++i)
                {
                    float sin = (float)Math.Sin(angle);
                    float cos = (float)Math.Cos(angle);
                    Vector3 bottomPoint = pos + sin * rtVec + cos * fwVec;
                    outList.Add(bottomPoint);
                    outList.Add(bottomPoint + upVec);
                    angle += (float)Math.PI / 3.0f;
                }
            }
        }

        #endregion

        private void UpdateGizmo(MyCubeBuilderGizmo.MyGizmoSpaceProperties gizmoSpace, bool add, bool remove, bool draw)
        {
            if (!gizmoSpace.Enabled)
                return;

            if (MyFakes.ENABLE_BATTLE_SYSTEM && MySession.Static.Battle)
            {
                gizmoSpace.m_showGizmoCube = false;
                gizmoSpace.m_buildAllowed = false;
                return;
            }

            if (CurrentGrid != null)
            {
                UpdateGizmo_Grid(gizmoSpace, add, remove, draw);
            }
            else if (CurrentVoxelMap != null)
            {
                UpdateGizmo_VoxelMap(gizmoSpace, add, remove, draw);
            }
            else if (DynamicMode)
            {
                UpdateGizmo_DynamicMode(gizmoSpace);
            }
        }

        private void UpdateGizmo_DynamicMode(MyCubeBuilderGizmo.MyGizmoSpaceProperties gizmoSpace)
        {
            Debug.Assert(DynamicMode);

            float gridSize = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize);
            BoundingBoxD localAABB = new BoundingBoxD(-CurrentBlockDefinition.Size * gridSize * 0.5f, CurrentBlockDefinition.Size * gridSize * 0.5f);

            var settings = CurrentBlockDefinition.CubeSize == MyCubeSize.Large ? MyPerGameSettings.PastingSettings.LargeGrid : MyPerGameSettings.PastingSettings.SmallGrid;
            MatrixD drawMatrix = gizmoSpace.m_worldMatrixAdd;

            BuildComponent.GetGridSpawnMaterials(CurrentBlockDefinition, drawMatrix, false);
            gizmoSpace.m_buildAllowed &= BuildComponent.HasBuildingMaterials(MySession.LocalCharacter);

            if (MySession.Static.SurvivalMode && !SpectatorIsBuilding)
            {
                //MatrixD inverseDrawMatrix = MatrixD.Invert(drawMatrix);
                //if (!MyCubeBuilderGizmo.DefaultGizmoCloseEnough(ref inverseDrawMatrix, localAABB, gridSize, IntersectionDistance) 
                //    || MySession.GetCameraControllerEnum() == MyCameraControllerEnum.Spectator)
                //{
                //    gizmoSpace.m_buildAllowed = false;
                //    gizmoSpace.m_removeBlock = null;
                //}

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
                bool placementTest = MyCubeGrid.TestBlockPlacementArea(CurrentBlockDefinition, null, drawMatrix, ref settings, localAABB, DynamicMode);
                gizmoSpace.m_buildAllowed &= placementTest;
            }
            gizmoSpace.m_showGizmoCube = true;

            gizmoSpace.m_cubeMatricesTemp.Clear();
            gizmoSpace.m_cubeModelsTemp.Clear();

            if (gizmoSpace.m_showGizmoCube)
                UpdateShowGizmoCube(gizmoSpace, gridSize);

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
                        m_renderData.AddInstance(MyModel.GetId(model), gizmoSpace.m_cubeMatricesTemp[i], ref MatrixD.Identity);
                    }
                }
            }
        }

        private void UpdateGizmo_VoxelMap(MyCubeBuilderGizmo.MyGizmoSpaceProperties gizmoSpace, bool add, bool remove, bool draw)
        {
            Color green = new Color(Color.Green * 0.6f, 1f);
            Color red = new Color(Color.Red * 0.8f, 1);
            Color yellow = Color.Yellow;
            Color blue = Color.Blue;
            //Vector4 black = Color.Black.ToVector4();
            Color gray = Color.Gray;

            float gridSize = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize);

            Vector3 temp;
            Vector3D worldCenter = Vector3D.Zero;
            Vector3D worldPos = gizmoSpace.m_worldMatrixAdd.Translation;
            MatrixD drawMatrix = gizmoSpace.m_worldMatrixAdd;

            Color color = green;

            UpdateShowGizmoCube(gizmoSpace, gridSize);

            int posIndex = 0;
            for (temp.X = 0; temp.X < CurrentBlockDefinition.Size.X; temp.X++)
                for (temp.Y = 0; temp.Y < CurrentBlockDefinition.Size.Y; temp.Y++)
                    for (temp.Z = 0; temp.Z < CurrentBlockDefinition.Size.Z; temp.Z++)
                    {
                        color = gizmoSpace.m_buildAllowed ? green : gray;

                        Vector3I gridPosition = gizmoSpace.m_positions[posIndex++];
                        Vector3D tempWorldPos = gridPosition * gridSize;
                        if (!MyPerGameSettings.BuildingSettings.StaticGridAlignToCenter)
                            tempWorldPos -= 0.5 * gridSize;

                        worldCenter += tempWorldPos;

                        drawMatrix.Translation = tempWorldPos;

                        MyCubeGrid.GetCubeParts(CurrentBlockDefinition, gridPosition, gizmoSpace.m_localMatrixAdd.GetOrientation(), gridSize, gizmoSpace.m_cubeModelsTemp, gizmoSpace.m_cubeMatricesTemp, gizmoSpace.m_cubeNormals, gizmoSpace.m_patternOffsets);

                        if (gizmoSpace.m_showGizmoCube)
                        {
                            for (int i = 0; i < gizmoSpace.m_cubeMatricesTemp.Count; i++)
                            {
                                MatrixD modelMatrix = gizmoSpace.m_cubeMatricesTemp[i];
                                modelMatrix.Translation = tempWorldPos;
                                gizmoSpace.m_cubeMatricesTemp[i] = modelMatrix;
                            }

                            m_gizmo.AddFastBuildParts(gizmoSpace, CurrentBlockDefinition, null);
                            m_gizmo.UpdateGizmoCubeParts(gizmoSpace, m_renderData, ref MatrixD.Identity);
                        }
                    }

            //calculate world center for block model
            worldCenter /= CurrentBlockDefinition.Size.Size;
            drawMatrix.Translation = worldCenter;

            BoundingBoxD localAABB = new BoundingBoxD(-CurrentBlockDefinition.Size * gridSize * 0.5f, CurrentBlockDefinition.Size * gridSize * 0.5f);

            var settings = CurrentBlockDefinition.CubeSize == MyCubeSize.Large ? MyPerGameSettings.BuildingSettings.LargeStaticGrid : MyPerGameSettings.BuildingSettings.SmallStaticGrid;
            MyBlockOrientation blockOrientation = new MyBlockOrientation(ref Quaternion.Identity);
            bool placementTest = CheckValidBlockRotation(gizmoSpace.m_worldMatrixAdd, CurrentBlockDefinition.Direction, CurrentBlockDefinition.Rotation)
                && MyCubeGrid.TestBlockPlacementArea(CurrentBlockDefinition, blockOrientation, drawMatrix, ref settings, localAABB, false);
            gizmoSpace.m_buildAllowed &= placementTest;
            gizmoSpace.m_buildAllowed &= gizmoSpace.m_showGizmoCube;
            gizmoSpace.m_worldMatrixAdd = drawMatrix;

            BuildComponent.GetGridSpawnMaterials(CurrentBlockDefinition, drawMatrix, true);
            gizmoSpace.m_buildAllowed &= BuildComponent.HasBuildingMaterials(MySession.LocalCharacter);

            if (MySession.Static.SurvivalMode && !SpectatorIsBuilding)
            {
                BoundingBoxD gizmoBox = localAABB.Transform(ref drawMatrix);

                if (!MyCubeBuilderGizmo.DefaultGizmoCloseEnough(ref MatrixD.Identity, gizmoBox, gridSize, IntersectionDistance) || CameraControllerSpectator)
                {
                    gizmoSpace.m_buildAllowed = false;
                    gizmoSpace.m_showGizmoCube = false;
                    gizmoSpace.m_removeBlock = null;
                    return;
                }
            }

            //color = gizmoSpace.m_buildAllowed ? green : gray;
            color = Color.White;
            string lineMaterial = gizmoSpace.m_buildAllowed ? "GizmoDrawLine" : "GizmoDrawLineRed";

            if (gizmoSpace.SymmetryPlane == MySymmetrySettingModeEnum.Disabled)
            {
                MySimpleObjectDraw.DrawTransparentBox(ref drawMatrix,
                    ref localAABB, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.04f, lineMaterial: lineMaterial);

                m_rotationHints.CalculateRotationHints(drawMatrix, localAABB, !MyHud.MinimalHud && MySandboxGame.Config.RotationHints && draw && MyFakes.ENABLE_ROTATION_HINTS);
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

        }

        private void UpdateGizmo_Grid(MyCubeBuilderGizmo.MyGizmoSpaceProperties gizmoSpace, bool add, bool remove, bool draw)
        {
            Color green = new Color(Color.Green * 0.6f, 1f);
            Color red = new Color(Color.Red * 0.8f, 1);
            Color yellow = Color.Yellow;
            Color black = Color.Black;
            Color gray = Color.Gray;
            Color white = Color.White;

            if (add)
            {
                if (gizmoSpace.m_startBuild != null && gizmoSpace.m_continueBuild != null)
                {
                    gizmoSpace.m_buildAllowed = true;
                }

                if (PlacingSmallGridOnLargeStatic && gizmoSpace.m_positionsSmallOnLarge.Count == 0)
                    return;

                if (CurrentBlockDefinition != null)
                {
                    Matrix addOrientationMat = gizmoSpace.m_localMatrixAdd.GetOrientation();
                    MyBlockOrientation gizmoAddOrientation = new MyBlockOrientation(ref addOrientationMat);

                    // Test free space in the cube grid (& valid rotation of the block)
                    if (!PlacingSmallGridOnLargeStatic)
                    {
                        gizmoSpace.m_buildAllowed &= CheckValidBlockRotation(gizmoSpace.m_localMatrixAdd, CurrentBlockDefinition.Direction, CurrentBlockDefinition.Rotation) 
                            && CurrentGrid.CanPlaceBlock(gizmoSpace.m_min, gizmoSpace.m_max, gizmoAddOrientation, gizmoSpace.m_blockDefinition);
                    }

                    MyCubeBuilder.BuildComponent.GetBlockPlacementMaterials(gizmoSpace.m_blockDefinition, gizmoSpace.m_addPos, gizmoAddOrientation, CurrentGrid);
                    gizmoSpace.m_buildAllowed &= MyCubeBuilder.BuildComponent.HasBuildingMaterials(MySession.LocalCharacter);

                    // In survival, check whether you're close enough, and have enough materials or haven't built for long enough
                    if (!PlacingSmallGridOnLargeStatic && MySession.Static.SurvivalMode && !SpectatorIsBuilding)
                    {
                        Vector3 localMin = (m_gizmo.SpaceDefault.m_min - new Vector3(0.5f)) * CurrentGrid.GridSize;
                        Vector3 localMax = (m_gizmo.SpaceDefault.m_max + new Vector3(0.5f)) * CurrentGrid.GridSize;
                        BoundingBoxD gizmoBox = new BoundingBoxD(localMin, localMax);

                        if (!MyCubeBuilderGizmo.DefaultGizmoCloseEnough(ref m_invGridWorldMatrix, gizmoBox, CurrentGrid.GridSize, IntersectionDistance) || CameraControllerSpectator)
                        {
                            gizmoSpace.m_buildAllowed = false;
                            gizmoSpace.m_removeBlock = null;
                            return;
                        }
                    }

                    // Check whether mount points match any of its neighbors (only if we can build here though).
                    if (gizmoSpace.m_buildAllowed)
                    {
                        Quaternion.CreateFromRotationMatrix(ref gizmoSpace.m_localMatrixAdd, out gizmoSpace.m_rotation);

                        if (gizmoSpace.SymmetryPlane == MySymmetrySettingModeEnum.Disabled && !PlacingSmallGridOnLargeStatic)
                            gizmoSpace.m_buildAllowed = MyCubeGrid.CheckConnectivity(CurrentGrid, CurrentBlockDefinition, ref gizmoSpace.m_rotation, ref gizmoSpace.m_centerPos);
                    }

                    Color color = green;

                    UpdateShowGizmoCube(gizmoSpace, CurrentGrid.GridSize);

                    // Disable building from cockpit
                    if (MySession.ControlledEntity != null && MySession.ControlledEntity is MyCockpit && !SpectatorIsBuilding)
                    {
                        gizmoSpace.m_showGizmoCube = false;
                        return;
                    }

                    gizmoSpace.m_buildAllowed &= gizmoSpace.m_showGizmoCube;

                    Vector3 temp;
                    Vector3D worldCenter = Vector3D.Zero;
                    Vector3D worldPos = gizmoSpace.m_worldMatrixAdd.Translation;
                    MatrixD drawMatrix = gizmoSpace.m_worldMatrixAdd;

                    int posIndex = 0;
                    for (temp.X = 0; temp.X < CurrentBlockDefinition.Size.X; temp.X++)
                        for (temp.Y = 0; temp.Y < CurrentBlockDefinition.Size.Y; temp.Y++)
                            for (temp.Z = 0; temp.Z < CurrentBlockDefinition.Size.Z; temp.Z++)
                            {
                                color = gizmoSpace.m_buildAllowed ? green : gray;

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
                                else
                                {
                                    Vector3I gridPosition = gizmoSpace.m_positions[posIndex++];
                                    Vector3D tempWorldPos = Vector3D.Transform(gridPosition * CurrentGrid.GridSize, CurrentGrid.WorldMatrix);

                                    worldCenter += tempWorldPos;
                                    drawMatrix.Translation = tempWorldPos;

                                    //if (temp == center)
                                    //{
                                    //    //if bigger block, show its center with black color
                                    //    if (CurrentBlockDefinition.Size.Size > 1)
                                    //        color = black;

                                    //    if (gizmoSpace.SymmetryPlane == MySymmetrySettingModeEnum.Disabled)
                                    //    {
                                    //        //VRageRender.MyRenderProxy.DebugDrawLine3D(tempWorldPos, tempWorldPos - worldDir * CurrentGrid.GridSize, Color.Purple, Color.Gray, false);
                                    //        //VRageRender.MyRenderProxy.DebugDrawAxis(drawMatrix, 2, false);
                                    //    }
                                    //}

                                    MyCubeGrid.GetCubeParts(CurrentBlockDefinition, gridPosition, gizmoSpace.m_localMatrixAdd.GetOrientation(), CurrentGrid.GridSize, gizmoSpace.m_cubeModelsTemp, gizmoSpace.m_cubeMatricesTemp, gizmoSpace.m_cubeNormals, gizmoSpace.m_patternOffsets);

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

                                    //for (int i = 0; i < gizmoSpace.m_gizmoCubeRenderIds.Count; i++)
                                    //{
                                    //    Matrix modelMatrix = gizmoSpace.m_gizmoCubeMatricesTemp[i];
                                    //    VRageRender.MyRenderProxy.UpdateRenderObject(gizmoSpace.m_gizmoCubeRenderIds[i], ref modelMatrix, false);
                                    //}

                                    //MySimpleObjectDraw.DrawTransparentBox(ref drawMatrix,
                                    //  ref box, ref color, MySimpleObjectRasterizer.Solid, 1, 0.04f);
                                }
                            }


                    //calculate world center for block model
                    worldCenter /= CurrentBlockDefinition.Size.Size;
                    drawMatrix.Translation = worldCenter;

                    float gridSize = PlacingSmallGridOnLargeStatic ? MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize) : CurrentGrid.GridSize;
                    BoundingBoxD localAABB = new BoundingBoxD(-CurrentBlockDefinition.Size * gridSize * 0.5f, CurrentBlockDefinition.Size * gridSize * 0.5f);

                    if (PlacingSmallGridOnLargeStatic)
                    {
                        if (MySession.Static.SurvivalMode && !SpectatorIsBuilding)
                        {
                            MatrixD invDrawMatrix = Matrix.Invert(drawMatrix);

                            MyCubeBuilder.BuildComponent.GetBlockPlacementMaterials(CurrentBlockDefinition, gizmoSpace.m_addPos, gizmoAddOrientation, CurrentGrid);
                            gizmoSpace.m_buildAllowed &= MyCubeBuilder.BuildComponent.HasBuildingMaterials(MySession.LocalCharacter);

                            if (!MyCubeBuilderGizmo.DefaultGizmoCloseEnough(ref invDrawMatrix, localAABB, gridSize, IntersectionDistance) || CameraControllerSpectator)
                            {
                                gizmoSpace.m_buildAllowed = false;
                                gizmoSpace.m_removeBlock = null;
                                return;
                            }
                        }

                        var settings = MyPerGameSettings.BuildingSettings.GetGridPlacementSettings(CurrentGrid);
                        // Orientation is identity (local), because it is represented in world matrix also.
                        MyBlockOrientation orientation = new MyBlockOrientation(ref Quaternion.Identity);
                        bool placementTest = CheckValidBlockRotation(gizmoSpace.m_localMatrixAdd, CurrentBlockDefinition.Direction, CurrentBlockDefinition.Rotation)
                            && MyCubeGrid.TestBlockPlacementArea(CurrentBlockDefinition, orientation, drawMatrix, ref settings, localAABB, false);
                        gizmoSpace.m_buildAllowed &= placementTest;

                        if (gizmoSpace.m_buildAllowed && gizmoSpace.SymmetryPlane == MySymmetrySettingModeEnum.Disabled)
                            gizmoSpace.m_buildAllowed
                                &= MyCubeGrid.CheckConnectivitySmallBlockToLargeGrid(CurrentGrid, CurrentBlockDefinition, ref gizmoSpace.m_rotation, ref gizmoSpace.m_addDir);

                        gizmoSpace.m_worldMatrixAdd = drawMatrix;
                    }

                    //color = gizmoSpace.m_buildAllowed ? green : gray;
                    color = Color.White;
                    string lineMaterial = gizmoSpace.m_buildAllowed ? "GizmoDrawLine" : "GizmoDrawLineRed";

                    if (gizmoSpace.SymmetryPlane == MySymmetrySettingModeEnum.Disabled)
                    {
                        MySimpleObjectDraw.DrawTransparentBox(ref drawMatrix,
                            ref localAABB, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.04f, lineMaterial: lineMaterial);

                        m_rotationHints.CalculateRotationHints(drawMatrix, localAABB, !MyHud.MinimalHud && MySandboxGame.Config.RotationHints && draw && MyFakes.ENABLE_ROTATION_HINTS);
                    }

                    gizmoSpace.m_cubeMatricesTemp.Clear();
                    gizmoSpace.m_cubeModelsTemp.Clear();

                    if (gizmoSpace.m_showGizmoCube)
                    {
                        // Draw mount points of added cube block as yellow squares in neighboring cells.
                        if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS)
                        {
                            float cubeSize = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize);
                            if (!PlacingSmallGridOnLargeStatic)
                                cubeSize = CurrentGrid.GridSize;

                            DrawMountPoints(cubeSize, CurrentBlockDefinition, ref drawMatrix);
                        }

                        Vector3D rotatedModelOffset;
                        Vector3D.TransformNormal(ref CurrentBlockDefinition.ModelOffset, ref gizmoSpace.m_worldMatrixAdd, out rotatedModelOffset);

                        drawMatrix.Translation = worldCenter + rotatedModelOffset;

                        AddFastBuildModels(gizmoSpace, drawMatrix, gizmoSpace.m_cubeMatricesTemp, gizmoSpace.m_cubeModelsTemp, gizmoSpace.m_blockDefinition);

                        Debug.Assert(gizmoSpace.m_cubeMatricesTemp.Count == gizmoSpace.m_cubeModelsTemp.Count);
                        for (int i = 0; i < gizmoSpace.m_cubeMatricesTemp.Count; ++i)
                        {
                            string model = gizmoSpace.m_cubeModelsTemp[i];
                            if (!string.IsNullOrEmpty(model))
                                m_renderData.AddInstance(MyModel.GetId(model), gizmoSpace.m_cubeMatricesTemp[i], ref m_invGridWorldMatrix);
                        }
                    }
                }
            }

            if (gizmoSpace.m_startRemove != null && gizmoSpace.m_continueBuild != null)
            {
                gizmoSpace.m_buildAllowed = true;

                Vector3I stepDelta;
                Vector3I counter;
                int stepCount;
                ComputeSteps(gizmoSpace.m_startRemove.Value, gizmoSpace.m_continueBuild.Value, Vector3I.One, out stepDelta, out counter, out stepCount);

                var matrix = CurrentGrid.WorldMatrix;
                BoundingBoxD aabb = BoundingBoxD.CreateInvalid();
                aabb.Include((gizmoSpace.m_startRemove.Value * CurrentGrid.GridSize));
                aabb.Include((gizmoSpace.m_continueBuild.Value * CurrentGrid.GridSize));
                aabb.Min -= new Vector3(CurrentGrid.GridSize / 2.0f + 0.02f);
                aabb.Max += new Vector3(CurrentGrid.GridSize / 2.0f + 0.02f);

                MySimpleObjectDraw.DrawTransparentBox(ref matrix, ref aabb, ref white, MySimpleObjectRasterizer.Wireframe, counter, 0.04f, null, "GizmoDrawLineRed", true);
                Color faceColor = new Color(Color.Red * 0.2f, 0.3f);
                MySimpleObjectDraw.DrawTransparentBox(ref matrix, ref aabb, ref faceColor, MySimpleObjectRasterizer.Solid, 0, 0.04f, "Square", null, true);
            }
            else if (remove && gizmoSpace.m_showGizmoCube && ShowRemoveGizmo)
            {
                if (gizmoSpace.m_removeBlock != null)
                    DrawSemiTransparentBox(CurrentGrid, gizmoSpace.m_removeBlock, red, lineMaterial: "GizmoDrawLineRed");

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
            }
            else
            {
                if (MySession.Static.SurvivalMode && (!CameraControllerSpectator || MyFinalBuildConstants.IS_OFFICIAL))
                {
                    Vector3 localMin = (m_gizmo.SpaceDefault.m_min - new Vector3(0.5f)) * CurrentGrid.GridSize;
                    Vector3 localMax = (m_gizmo.SpaceDefault.m_max + new Vector3(0.5f)) * CurrentGrid.GridSize;
                    BoundingBoxD gizmoBox = new BoundingBoxD(localMin, localMax);

                    if (!MyCubeBuilderGizmo.DefaultGizmoCloseEnough(ref m_invGridWorldMatrix, gizmoBox, CurrentGrid.GridSize, IntersectionDistance))
                    {
                        gizmoSpace.m_removeBlock = null;
                    }
                }
            }
        }

        private bool CanStartConstruction()
        {
            /*if (!MySession.Static.SimpleSurvival && MySession.ControlledEntity is MyCharacter)
            {
                return (MySession.ControlledEntity as MyCharacter).CanStartConstruction(CurrentBlockDefinition);
            }

            if (MySession.Static.SimpleSurvival)
            {
                return CanBuildBlockSurvivalTime();
            }*/

            return false;
        }

        private void UpdateShowGizmoCube(MyCubeBuilderGizmo.MyGizmoSpaceProperties gizmoSpace, float gridSize)
        {
            if (CurrentBlockDefinition == null)
                return;

            // Disable gizmo cubes when the camera is inside the currently displayed cube or where the character is inside the cube
            gizmoSpace.m_showGizmoCube = true;

            if (MySector.MainCamera != null)
            {
                gizmoSpace.m_showGizmoCube = gizmoSpace.m_showGizmoCube 
                    && !m_gizmo.PointInsideGizmo(MySector.MainCamera.Position, gizmoSpace.SourceSpace, ref m_invGridWorldMatrix, gridSize, inflate: 0.05f, onVoxel: CurrentVoxelMap != null);
            }
            if (MySession.ControlledEntity != null && MySession.ControlledEntity is MyCharacter)
            {
                m_collisionTestPoints.Clear();
                PrepareCharacterCollisionPoints(m_collisionTestPoints);
                gizmoSpace.m_showGizmoCube = gizmoSpace.m_showGizmoCube
                    && !m_gizmo.PointsInsideGizmo(m_collisionTestPoints, gizmoSpace.SourceSpace, ref m_invGridWorldMatrix, gridSize, inflate: 0.05f, onVoxel: CurrentVoxelMap != null);
            }
        }

        private static bool CheckValidBlockRotation(Matrix localMatrix, MyBlockDirection blockDirection, MyBlockRotation blockRotation)
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

        #endregion

        #region Build

        HashSet<MyCubeGrid.MyBlockLocation> m_blocksBuildQueue = new HashSet<MyCubeGrid.MyBlockLocation>();
        List<Vector3I> m_tmpBlockPositionList = new List<Vector3I>();
        List<Tuple<Vector3I, ushort>> m_tmpCompoundBlockPositionIdList = new List<Tuple<Vector3I, ushort>>();

        void Add()
        {
            m_blocksBuildQueue.Clear();

            UpdateGizmos(true, true, false);

            var playUnableSound = true;

            foreach (var gizmoSpace in m_gizmo.Spaces)
            {
                if (BuildInputValid && !MyEntities.MemoryLimitReachedReport)
                {
                    if (!gizmoSpace.Enabled)
                        continue;

                    if (gizmoSpace.m_buildAllowed)
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
                CurrentGrid.BuildBlocks(MyToolbar.ColorMaskHSV, m_blocksBuildQueue, MySession.LocalCharacterEntityId);
            }
        }

        private bool AddBlocksToBuildQueueOrSpawn(MyCubeBuilderGizmo.MyGizmoSpaceProperties gizmoSpace)
        {
            bool added = true;

            if (GridAndBlockValid)
            {
                if (PlacingSmallGridOnLargeStatic)
                {
                    Vector3 offset = Vector3.Abs(Vector3.TransformNormal(MyCubeBlock.GetBlockGridOffset(gizmoSpace.m_blockDefinition), gizmoSpace.m_worldMatrixAdd));
                    MatrixD gridWorldMatrix = gizmoSpace.m_worldMatrixAdd;
                    gridWorldMatrix.Translation -= offset;

                    MySyncCreate.RequestStaticGridSpawn(gizmoSpace.m_blockDefinition, gridWorldMatrix, MySession.LocalCharacterEntityId);
                }
                else
                {
                    m_blocksBuildQueue.Add(new MyCubeGrid.MyBlockLocation(gizmoSpace.m_blockDefinition.Id, gizmoSpace.m_min, gizmoSpace.m_max, gizmoSpace.m_centerPos, gizmoSpace.LocalOrientation, MyEntityIdentifier.AllocateId(), MySession.LocalPlayerId));
                }
            }
            else if (VoxelMapAndBlockValid && !DynamicMode)
            {
                Vector3 offset = Vector3.Abs(Vector3.TransformNormal(MyCubeBlock.GetBlockGridOffset(gizmoSpace.m_blockDefinition), gizmoSpace.m_worldMatrixAdd));
                MatrixD gridWorldMatrix = gizmoSpace.m_worldMatrixAdd;
                gridWorldMatrix.Translation -= offset;

                MySyncCreate.RequestStaticGridSpawn(gizmoSpace.m_blockDefinition, gridWorldMatrix, MySession.LocalCharacterEntityId);
            }
            else if (DynamicMode)
            {
                MatrixD gridWorldMatrix = gizmoSpace.m_worldMatrixAdd;
                MySyncCreate.RequestDynamicGridSpawn(gizmoSpace.m_blockDefinition, gridWorldMatrix, MySession.LocalCharacterEntityId);
            }
            else
            {
                added = false;
            }

            return added;
        }

        private void UpdateGizmos(bool addPos, bool removePos, bool draw)
        {
            if (CurrentBlockDefinition == null)
                return;
            if (CurrentGrid == null && CurrentVoxelMap == null && !DynamicMode)
                return;

            m_gizmo.SpaceDefault.m_blockDefinition = CurrentBlockDefinition;

            m_gizmo.EnableGizmoSpaces(CurrentBlockDefinition, CurrentGrid, UseSymmetry);

            m_renderData.ClearInstanceData();
            m_rotationHints.Clear();

            foreach (var gizmoSpace in m_gizmo.Spaces)
            {
                bool spaceAddPos = addPos && BuildInputValid;

                if (gizmoSpace.SymmetryPlane == MySymmetrySettingModeEnum.Disabled)
                {
                    Quaternion quatOrientation = gizmoSpace.LocalOrientation;
                    if (!PlacingSmallGridOnLargeStatic && CurrentGrid != null)
                        spaceAddPos &= CurrentGrid.CanAddCube(gizmoSpace.m_addPos, new MyBlockOrientation(ref quatOrientation), CurrentBlockDefinition);
                }
                else
                {
                    spaceAddPos &= UseSymmetry;
                    removePos &= UseSymmetry;
                }

                UpdateGizmo(gizmoSpace, spaceAddPos || FreezeGizmo, removePos || FreezeGizmo, draw);
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

        public bool CanStartConstruction(MyEntity buildingEntity)
        {
            if (m_shipCreationClipboard.IsActive)
            {
                return m_shipCreationClipboard.EntityCanPaste(buildingEntity);
            }
            else if (m_multiBlockCreationClipboard.IsActive)
            {
                return m_shipCreationClipboard.EntityCanPaste(buildingEntity);
            }
            else
            {
                if (MySession.Static.SimpleSurvival)
                {
                    return true;
                }
                else
                {
                    var addMatrix = m_gizmo.SpaceDefault.m_worldMatrixAdd;
                    BuildComponent.GetGridSpawnMaterials(CurrentBlockDefinition, addMatrix, false);
                    return BuildComponent.HasBuildingMaterials(buildingEntity);
                }
            }
        }

        public bool AddConstruction(MyEntity builder)
        {
            var controllingPlayer = Sync.Players.GetControllingPlayer(builder);

            Debug.Assert(controllingPlayer != null && controllingPlayer.IsLocalPlayer(), "Only local players can call AddConstruction!");
            if (controllingPlayer == null || controllingPlayer.IsRemotePlayer()) return false;

            // The new ship creation clipboard is handled separately
            if (m_shipCreationClipboard.IsActive)
            {
                return m_shipCreationClipboard.PasteGrid(MyCubeBuilder.BuildComponent.GetBuilderInventory(builder));
            }

            if (MyFakes.ENABLE_MULTIBLOCKS && MultiBlockCreationIsActivated)
            {
                if (m_multiBlockCreationClipboard.PasteGrid(MyCubeBuilder.BuildComponent.GetBuilderInventory(builder), false))
                {
                    if (MySession.Static.SimpleSurvival)
                        m_lastBlockBuildTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                    return true;
                }
                return false;
            }

            var gizmoSpace = m_gizmo.SpaceDefault;

            if (gizmoSpace.Enabled && BuildInputValid && gizmoSpace.m_buildAllowed && !MyEntities.MemoryLimitReachedReport)
            {
                m_blocksBuildQueue.Clear();
                bool added = AddBlocksToBuildQueueOrSpawn(gizmoSpace);
                if (added)
                {
                    if (CurrentGrid != null && m_blocksBuildQueue.Count > 0)
                        CurrentGrid.BuildBlocks(MyToolbar.ColorMaskHSV, m_blocksBuildQueue, builder.EntityId);

                    if (MySession.Static.SimpleSurvival)
                        m_lastBlockBuildTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
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

        internal bool GetAddAndRemovePositions(float gridSize, bool placingSmallGridOnLargeStatic, out Vector3I addPos, out Vector3? addPosSmallOnLarge, out Vector3I addDir, out Vector3I removePos, out MySlimBlock removeBlock,
            out ushort? compoundBlockId)
        {
            bool result = false;

            addPos = new Vector3I();
            addPosSmallOnLarge = null;
            addDir = new Vector3I();
            removePos = new Vector3I();
            removeBlock = null;

            MySlimBlock intersectedBlock;
            Vector3D intersectedBlockPos;
            Vector3D intersectionBlockExact;
            result = GetBlockAddPosition(gridSize, placingSmallGridOnLargeStatic, out intersectedBlock, out intersectedBlockPos, out intersectionBlockExact, out addPos, out addDir, out compoundBlockId);

            float currentGridSize = placingSmallGridOnLargeStatic ? CurrentGrid.GridSize : gridSize;

            if (result && (MaxGridDistanceFrom == null
                || Vector3D.DistanceSquared(intersectionBlockExact * currentGridSize, Vector3.Transform(MaxGridDistanceFrom.Value, m_invGridWorldMatrix)) < (MAX_BLOCK_BUILDING_DISTANCE * MAX_BLOCK_BUILDING_DISTANCE)))
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

            // Placing small on large grid
            if (result && placingSmallGridOnLargeStatic)
            {
                if (m_hitInfo.HasValue)
                {
                    Vector3 hitInfoNormal = m_hitInfo.Value.HkHitInfo.Normal;
                    Base6Directions.Direction closestDir = Base6Directions.GetClosestDirection(hitInfoNormal);
                    Vector3I hitNormal = Base6Directions.GetIntVector(closestDir);
                    addDir = hitNormal;
                }

                // Because intersection can be out of cube (on edges) then we must clamp intersection inside (we need that created block will touch side not edge with target block)
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
                Vector3I addPosSmallOnLargeInt = Vector3I.Round((localIntersectionExact + 0.1f * smallToLarge * addDir - smallToLarge * Vector3.Half) / smallToLarge);
                addPosSmallOnLarge = smallToLarge * addPosSmallOnLargeInt + smallToLarge * Vector3.Half;
            }

            // Compound block - use its position.
            if (result && !placingSmallGridOnLargeStatic && intersectedBlock != null && intersectedBlock.FatBlock is MyCompoundCubeBlock)
            {
                MyCompoundCubeBlock cmpCubeBlock = intersectedBlock.FatBlock as MyCompoundCubeBlock;
                Quaternion quatOrientation = m_gizmo.SpaceDefault.LocalOrientation;
                MyCubeBlockDefinition blockDefinition = CurrentBlockDefinition;
                if (m_multiBlockCreationClipboard.IsActive)
                {
                    if (m_multiBlockCreationClipboard.PreviewGrids != null && m_multiBlockCreationClipboard.PreviewGrids.Count > 0)
                    {
                        MySlimBlock mainBlock = m_multiBlockCreationClipboard.PreviewGrids[0].GetCubeBlock(Vector3I.Zero);
                        if (mainBlock != null)
                        {
                            if (mainBlock.FatBlock is MyCompoundCubeBlock)
                            {
                                MyCompoundCubeBlock compoundBlock = mainBlock.FatBlock as MyCompoundCubeBlock;
                                if (compoundBlock.GetBlocksCount() > 0)
                                    blockDefinition = compoundBlock.GetBlocks()[0].BlockDefinition;
                            }
                            else
                            {
                                blockDefinition = mainBlock.BlockDefinition;
                            }
                        }
                    }
                }

                if (cmpCubeBlock.CanAddBlock(blockDefinition, new MyBlockOrientation(ref quatOrientation)) && CompoundEnabled)
                {
                    addPos = removePos;
                }
            }

            Debug.Assert(!result || addDir != Vector3I.Zero, "Direction vector cannot be zero");
            return result;
        }

        void Remove()
        {
            if (PlacingSmallGridOnLargeStatic)
                return;

            m_tmpBlockPositionList.Clear();
            m_tmpCompoundBlockPositionIdList.Clear();

            foreach (var gizmoSpace in m_gizmo.Spaces)
            {
                if (!gizmoSpace.Enabled)
                    continue;

                if (GridValid && gizmoSpace.m_removeBlock != null && (gizmoSpace.m_removeBlock.FatBlock == null || !gizmoSpace.m_removeBlock.FatBlock.IsSubBlock))
                {
                    RemoveBlock(gizmoSpace.m_removeBlock, gizmoSpace.m_blockIdInCompound);
                    gizmoSpace.m_removeBlock = null;
                }
            }

            if (MultiBlockCreationIsActivated)
            {
                RemoveBlock(m_multiBlockCreationClipboard.RemoveBlock, CompoundEnabled ? m_multiBlockCreationClipboard.BlockIdInCompound : null);
                m_multiBlockCreationClipboard.RemoveBlock = null;
            }

            if (m_tmpBlockPositionList.Count > 0 || m_tmpCompoundBlockPositionIdList.Count > 0)
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudDeleteBlock);

                if (m_tmpBlockPositionList.Count > 0)
                    CurrentGrid.RazeBlocks(m_tmpBlockPositionList);

                if (m_tmpCompoundBlockPositionIdList.Count > 0)
                    CurrentGrid.RazeBlockInCompoundBlock(m_tmpCompoundBlockPositionIdList);
            }
        }

        private void RemoveBlock(MySlimBlock block, ushort? blockIdInCompound)
        {
            if (block != null && (block.FatBlock == null || !block.FatBlock.IsSubBlock))
            {
                if (block.FatBlock is MyCompoundCubeBlock)
                {
                    if (blockIdInCompound.HasValue)
                    {
                        m_tmpCompoundBlockPositionIdList.Add(new Tuple<Vector3I, ushort>(block.Min, blockIdInCompound.Value));
                    }
                    else
                    {
                        // Remove whole compound
                        m_tmpBlockPositionList.Add(block.Min);
                    }
                }
                else
                {
                    m_tmpBlockPositionList.Add(block.Min);
                }
            }
        }

        void Change(int expand = 0)
        {
            ProfilerShort.Begin("MyCubeBuilder.Change");
            m_tmpBlockPositionList.Clear();

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

                    CurrentGrid.ColorBlocks(start, end, MyToolbar.ColorMaskHSV, playSound);
                }
            }
            ProfilerShort.End();
        }

        bool IsInSymmetrySettingMode
        {
            get { return m_symmetrySettingMode != MySymmetrySettingModeEnum.NoPlane; }
        }

        #endregion

        #region Update

        Vector3I? GetSingleMountPointNormal()
        {
            if (CurrentBlockDefinition == null || CurrentBlockDefinition.MountPoints.Length == 0)
            {
                return null;
            }

            var normal = CurrentBlockDefinition.MountPoints[0].Normal;
            var oppositeNormal = -normal;
            switch (CurrentBlockDefinition.AutorotateMode)
            {
                case MyAutorotateMode.OneDirection:
                    for (int i = 1; i < CurrentBlockDefinition.MountPoints.Length; i++)
                    {
                        var currentNormal = CurrentBlockDefinition.MountPoints[i].Normal;
                        if (currentNormal != normal)
                            return null;
                    }
                    break;

                case MyAutorotateMode.OppositeDirections:
                    for (int i = 1; i < CurrentBlockDefinition.MountPoints.Length; i++)
                    {
                        var currentNormal = CurrentBlockDefinition.MountPoints[i].Normal;
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

        public override void UpdateBeforeSimulation()
        {
            ProfilerShort.Begin("Setup");
            var normal = GetSingleMountPointNormal();
            // Gizmo add dir can be zero in some cases
            if (normal.HasValue && CurrentGrid != null && m_gizmo.SpaceDefault.m_addDir != Vector3I.Zero)
            {
                m_gizmo.SetupLocalAddMatrix(m_gizmo.SpaceDefault, normal.Value);
            }
            UpdateNotificationBlockNotAvailable(changeText: false);

            if (m_clipboard.IsActive && m_shipCreationClipboard.IsActive)
            {
                m_clipboard.Deactivate();
                UpdatePasteNotification(MySpaceTexts.CubeBuilderPasteNotification);
            }
            ProfilerShort.End();

            ProfilerShort.Begin("m_clipboard.Update");
            m_clipboard.Update();
            ProfilerShort.End();

            ProfilerShort.Begin("m_floatingObjectClipboard.Update");
            m_floatingObjectClipboard.Update();
            ProfilerShort.End();

            ProfilerShort.Begin("m_voxelClipboard.Update");
            m_voxelClipboard.Update();
            ProfilerShort.End();

            ProfilerShort.Begin("m_shipCreationClipboard.Update");
            m_shipCreationClipboard.Update();
            ProfilerShort.End();

            ProfilerShort.Begin("m_multiBlockCreationClipboard.Update");
            m_multiBlockCreationClipboard.Update();
            ProfilerShort.End();

            ProfilerShort.Begin("ClipboardActive");
            if (m_clipboard.IsActive || m_floatingObjectClipboard.IsActive || m_voxelClipboard.IsActive || m_shipCreationClipboard.IsActive)
            {
                m_collisionTestPoints.Clear();
                PrepareCharacterCollisionPoints(m_collisionTestPoints);

                bool hideClipboards = MySession.ControlledEntity is MyCockpit;
                if (MySession.GetCameraControllerEnum() == MyCameraControllerEnum.Spectator)
                {
                    if (!MyInput.Static.ENABLE_DEVELOPER_KEYS && MySession.Static.Settings.EnableSpectator)
                        hideClipboards &= true;
                    else
                        hideClipboards &= false;
                }

                if (!MyFakes.ENABLE_ALTERNATIVE_CLIPBOARD)
                {
                    if (m_clipboard.IsActive)
                    {
                        if (hideClipboards)
                            m_clipboard.Hide();
                        else
                        {
                            m_clipboard.Show();
                            m_clipboard.HideWhenColliding(m_collisionTestPoints);
                        }
                    }
                }

                if (m_floatingObjectClipboard.IsActive)
                {
                    if (hideClipboards)
                        m_floatingObjectClipboard.Hide();
                    else
                    {
                        m_floatingObjectClipboard.Show();
                        m_floatingObjectClipboard.HideWhenColliding(m_collisionTestPoints);
                    }
                }

                if (m_voxelClipboard.IsActive)
                {
                    if (hideClipboards)
                        m_voxelClipboard.Hide();
                    else
                        m_voxelClipboard.Show();
                }

                if (m_shipCreationClipboard.IsActive)
                {
                    if (hideClipboards)
                        m_shipCreationClipboard.Hide();
                    else
                    {
                        m_shipCreationClipboard.Show();
                        m_shipCreationClipboard.HideWhenColliding(m_collisionTestPoints);
                    }
                }
            }
            ProfilerShort.End();
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            RemoveSymmetryNotification();

            m_gizmo.Clear();

            m_clipboard.Deactivate();
            m_floatingObjectClipboard.Deactivate();
            m_voxelClipboard.Deactivate();
            m_shipCreationClipboard.Deactivate();
            m_multiBlockCreationClipboard.Deactivate();

            CurrentGrid = null;

            UnloadRenderObjects();
        }

        void UnloadRenderObjects()
        {
            m_gizmo.RemoveGizmoCubeParts();

            m_renderData.UnloadRenderObjects();
        }

        public void UpdateNotificationBlockNotAvailable(bool changeText = true)
        {
            if (!MyFakes.ENABLE_NOTIFICATION_BLOCK_NOT_AVAILABLE)
                return;

            bool developerSpectatorBuild = MySession.GetCameraControllerEnum() == MyCameraControllerEnum.Spectator && !MyFinalBuildConstants.IS_OFFICIAL;
            bool hideNotificationInCockpit = MySession.ControlledEntity != null && MySession.ControlledEntity is MyCockpit && !developerSpectatorBuild;

            if (BlockCreationIsActivated && CurrentGrid != null && CurrentBlockDefinition != null && CurrentBlockDefinition.CubeSize != CurrentGrid.GridSizeEnum && !hideNotificationInCockpit
                && !PlacingSmallGridOnLargeStatic)
            {
                if (changeText || m_blockNotAvailableNotification == null)
                {
                    ShowNotificationBlockNotAvailable(CurrentBlockDefinition.DisplayNameText,
                                                        (CurrentGrid.GridSizeEnum == MyCubeSize.Small) ? MySpaceTexts.NotificationArgSmallShip
                                                                                                    : (CurrentGrid.IsStatic) ? MySpaceTexts.NotificationArgStation
                                                                                                                                : MySpaceTexts.NotificationArgLargeShip);
                }
                else
                {
                    MyHud.Notifications.Add(m_blockNotAvailableNotification);
                }
            }
            else
            {
                HideNotificationBlockNotAvailable();
            }
        }

        /// <summary>
        /// Notification visible when looking at grid whose size is nto supported current block.
        /// </summary>
        private void ShowNotificationBlockNotAvailable(String blockDisplayName, MyStringId gridTypeText)
        {
            if (!MyFakes.ENABLE_NOTIFICATION_BLOCK_NOT_AVAILABLE)
                return;

            if (m_blockNotAvailableNotification == null)
                m_blockNotAvailableNotification = new MyHudNotification(MySpaceTexts.NotificationBlockNotAvailableFor, 0, font: MyFontEnum.Red, priority: 1);

            m_blockNotAvailableNotification.SetTextFormatArguments(blockDisplayName, MyTexts.Get(gridTypeText));
            MyHud.Notifications.Add(m_blockNotAvailableNotification);
        }

        private void HideNotificationBlockNotAvailable()
        {
            MyHud.Notifications.Remove(m_blockNotAvailableNotification);
        }

        #endregion

        #region Continuous building

        void StartBuilding()
        {
            if ((!GridAndBlockValid && !VoxelMapAndBlockValid) || PlacingSmallGridOnLargeStatic || MyEntities.MemoryLimitReachedReport || MySession.Static.SimpleSurvival)
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
            if (m_gizmo.SpaceDefault.m_startRemove == null && GetAddAndRemovePositions(gridSize, PlacingSmallGridOnLargeStatic, out addPos, out addPosSmallOnLarge, out dir, out removePos, out removeBlock, out compoundBlockId))
            {
                m_gizmo.SpaceDefault.m_startBuild = addPos;
            }
            else
                m_gizmo.SpaceDefault.m_startBuild = null;
        }

        void StartRemoving()
        {
            if (PlacingSmallGridOnLargeStatic || MySession.Static.SimpleSurvival)
                return;

            m_initialIntersectionStart = IntersectionStart;
            m_initialIntersectionDirection = IntersectionDirection;
            if (CurrentGrid != null && m_gizmo.SpaceDefault.m_startBuild == null)
            {
                double dst;
                m_gizmo.SpaceDefault.m_startRemove = IntersectCubes(CurrentGrid, out dst);
            }
        }

        void ContinueBuilding(bool planeBuild)
        {
            var defaulGizmoSpace = m_gizmo.SpaceDefault;

            if (!defaulGizmoSpace.m_startBuild.HasValue && !defaulGizmoSpace.m_startRemove.HasValue) return;

            if (!GridAndBlockValid && !VoxelMapAndBlockValid)
                return;

            defaulGizmoSpace.m_continueBuild = null;

            // Avoid sudden appearing right after player clicked (wait until he moved mouse at least a little).
            if (CheckSmallViewChange())
                return;

            IntersectInflated(m_cacheGridIntersections, CurrentGrid);

            //Vector3I startValue = m_gizmo.SpaceDefault.m_startBuild.HasValue ? m_gizmo.SpaceDefault.m_startBuild.Value : m_gizmo.SpaceDefault.m_startRemove.Value;

            Vector3I minGizmo = defaulGizmoSpace.m_startBuild.HasValue ? defaulGizmoSpace.m_min : defaulGizmoSpace.m_startRemove.Value;
            Vector3I maxGizmo = defaulGizmoSpace.m_startBuild.HasValue ? defaulGizmoSpace.m_max : defaulGizmoSpace.m_startRemove.Value;
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
                                        defaulGizmoSpace.m_continueBuild = intersection;
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
                                        defaulGizmoSpace.m_continueBuild = intersection;
                                        break;
                                    }
                                }
                            }
                        }
                    }
        }

        void StopBuilding()
        {
            if ((!GridAndBlockValid && !VoxelMapAndBlockValid && !MultiBlockCreationIsActivated) || MyEntities.MemoryLimitReachedReport)
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

                if (gizmoSpace.m_startBuild != null && (gizmoSpace.m_continueBuild != null || smallViewChange))
                {
                    Vector3I gridPos = gizmoSpace.m_startBuild.Value;
                    Vector3I min = gizmoSpace.m_min - gizmoSpace.m_centerPos;
                    Vector3I max = gizmoSpace.m_max - gizmoSpace.m_centerPos;

                    Vector3I rotatedSize;
                    Vector3I.TransformNormal(ref CurrentBlockDefinition.Size, ref gizmoSpace.m_localMatrixAdd, out rotatedSize);
                    rotatedSize = Vector3I.Abs(rotatedSize);

                    Vector3I stepDelta;
                    Vector3I counter;
                    int stepCount;

                    if (smallViewChange)
                        gizmoSpace.m_continueBuild = gizmoSpace.m_startBuild;

                    ComputeSteps(gizmoSpace.m_startBuild.Value, gizmoSpace.m_continueBuild.Value, rotatedSize, out stepDelta, out counter, out stepCount);

                    Vector3I centerPos = gizmoSpace.m_centerPos;
                    Quaternion orientation = gizmoSpace.LocalOrientation;
                    MyDefinitionId definitionId = gizmoSpace.m_blockDefinition.Id;

                    // Blocks can be randomly rotated if line/plane building is used.
                    bool allowRandomRotation = gizmoSpace.m_blockDefinition.RandomRotation 
                        && gizmoSpace.m_blockDefinition.Size.X == gizmoSpace.m_blockDefinition.Size.Y && gizmoSpace.m_blockDefinition.Size.X == gizmoSpace.m_blockDefinition.Size.Z
                        && (gizmoSpace.m_blockDefinition.Rotation == MyBlockRotation.Both || gizmoSpace.m_blockDefinition.Rotation == MyBlockRotation.Vertical);

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
                                    Vector3I tempCenter = gizmoSpace.m_centerPos + temp * stepDelta;
                                    Vector3I tempMin = gizmoSpace.m_min + temp * stepDelta;
                                    Vector3I tempMax = gizmoSpace.m_max + temp * stepDelta;

                                    Quaternion tempOrientation;

                                    if (gizmoSpace.m_blockDefinition.Rotation == MyBlockRotation.Both)
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

                                    m_blocksBuildQueue.Add(new MyCubeGrid.MyBlockLocation(gizmoSpace.m_blockDefinition.Id, tempMin, tempMax, tempCenter, tempOrientation,
                                        MyEntityIdentifier.AllocateId(), MySession.LocalPlayerId));
                                }
                            }
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
                        area.ColorMaskHSV = MyToolbar.ColorMaskHSV.PackHSVToUint();

                        CurrentGrid.BuildBlocks(MySession.LocalPlayerId, ref area);

                        // TODO: There will be message send instead of this, this will called and iterated after message success
                        //BuildByGizmo(ref min, ref max, ref stepDelta, ref counter, ref centerPos, ref orientation, ref definitionId);
                    }
                }
                else if (gizmoSpace.m_startRemove != null && (gizmoSpace.m_continueBuild != null || smallViewChange))
                {
                    MyGuiAudio.PlaySound(MyGuiSounds.HudDeleteBlock);

                    Vector3I min = gizmoSpace.m_startRemove.Value;
                    Vector3I max = gizmoSpace.m_startRemove.Value;

                    Vector3I stepDelta;
                    Vector3I counter;
                    int stepCount;
                    if (smallViewChange)
                        gizmoSpace.m_continueBuild = gizmoSpace.m_startRemove;

                    ComputeSteps(gizmoSpace.m_startRemove.Value, gizmoSpace.m_continueBuild.Value, Vector3I.One, out stepDelta, out counter, out stepCount);

                    min = Vector3I.Min(gizmoSpace.m_startRemove.Value, gizmoSpace.m_continueBuild.Value);
                    max = Vector3I.Max(gizmoSpace.m_startRemove.Value, gizmoSpace.m_continueBuild.Value);
                    var size = new Vector3UByte(max - min);
                    CurrentGrid.RazeBlocks(ref min, ref size);

                    //Vector3I offset = Vector3I.Zero;
                    //for (int i = 0; i < counter.X; i += 1, offset.X += stepDelta.X)
                    //{
                    //    offset.Y = 0;
                    //    for (int j = 0; j < counter.Y; j += 1, offset.Y += stepDelta.Y)
                    //    {
                    //        offset.Z = 0;
                    //        for (int k = 0; k < counter.Z; k += 1, offset.Z += stepDelta.Z)
                    //        {
                    //            Vector3I pos = gizmoSpace.m_startRemove.Value + offset;
                    //            if (CurrentGrid.CubeExists(pos))
                    //            {
                    //                m_tmpBlockPositionList.Add(pos);
                    //            }
                    //        }
                    //    }
                    //}
                }

                gizmoSpace.m_startBuild = null;
                gizmoSpace.m_continueBuild = null;
                gizmoSpace.m_startRemove = null;
            }

            if (m_blocksBuildQueue.Count > 0)
            {
                CurrentGrid.BuildBlocks(MyToolbar.ColorMaskHSV, m_blocksBuildQueue, MySession.LocalCharacterEntityId);
                m_blocksBuildQueue.Clear();
            }

            if (m_tmpBlockPositionList.Count > 0)
            {
                CurrentGrid.RazeBlocks(m_tmpBlockPositionList);
                m_tmpBlockPositionList.Clear();
            }

            HideStationRotationNotification();
        }

        // CH: At the time of writing this comment, this is not called anywhere (only one commented out occurence). If you want to use it, it's up to you to make it work :-)
        /*
        private void BuildByGizmo(ref Vector3I blockMin, ref Vector3I blockMax, ref Vector3I stepDelta, ref Vector3I buildAreaSize, ref Vector3I posInGrid, ref Quaternion orientation, ref MyDefinitionId definitionId)
        {
            Vector3I offset = Vector3I.Zero;
            for (int i = 0; i < buildAreaSize.X; i += 1, offset.X += stepDelta.X)
            {
                offset.Y = 0;
                for (int j = 0; j < buildAreaSize.Y; j += 1, offset.Y += stepDelta.Y)
                {
                    offset.Z = 0;
                    for (int k = 0; k < buildAreaSize.Z; k += 1, offset.Z += stepDelta.Z)
                    {
                        Vector3I gridPosIt = posInGrid + offset;// +gizmoSpace.m_mirroringOffset;
                        Vector3I minIt = gridPosIt + blockMin;
                        Vector3I maxIt = gridPosIt + blockMax;

                        // Early out
                        if (CurrentGrid.CanPlaceBlock(minIt, maxIt))
                        {
                            m_blocksBuildQueue.Add(new MyCubeGrid.MyBlockLocation(definitionId, minIt, maxIt, gridPosIt, orientation, MyEntityIdentifier.AllocateId(), MySession.Player.PlayerId));
                        }
                    }
                }
            }
        }
        */

        private bool CheckSmallViewChange()
        {
            float viewChangeCos = Vector3.Dot(m_initialIntersectionDirection, IntersectionDirection);
            double viewChangeDist = (m_initialIntersectionStart - IntersectionStart).Length();
            return viewChangeCos > CONTINUE_BUILDING_VIEW_ANGLE_CHANGE_THRESHOLD && viewChangeDist < CONTINUE_BUILDING_VIEW_POINT_CHANGE_THRESHOLD;
        }

        #endregion

        #region Draw

        internal override void ChoosePlacementObject()
        {
            if (m_gizmo.SpaceDefault.m_startBuild != null || m_gizmo.SpaceDefault.m_startRemove != null)
                return;

            base.ChoosePlacementObject();

            if (CurrentGrid != null && CurrentBlockDefinition != null && CurrentBlockDefinition.CubeSize != CurrentGrid.GridSizeEnum && !m_shipCreationClipboard.IsActive)
            {
                ChooseComplementBlock();
            }
        }

        /// <summary>
        /// Chooses same cube but for different grid size
        /// </summary>
        void ChooseComplementBlock()
        {
            var oldBlock = m_definitionWithVariants;

            if (oldBlock != null)
            {
                var group = MyDefinitionManager.Static.GetDefinitionGroup(oldBlock.Base.BlockPairName);
                if (oldBlock.Base.CubeSize == MyCubeSize.Small)
                {
                    if (group.Large != null && (group.Large.Public || MyFakes.ENABLE_NON_PUBLIC_BLOCKS))
                    {
                        CurrentBlockDefinition = group.Large;
                    }
                }
                else if (oldBlock.Base.CubeSize == MyCubeSize.Large)
                {
                    if (group.Small != null && (group.Small.Public || MyFakes.ENABLE_NON_PUBLIC_BLOCKS))
                    {
                        CurrentBlockDefinition = group.Small;
                    }
                }
            }
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
                float? dist = MyPhysics.CastShape(rayEnd, shape, ref matrix, MyPhysics.CollisionLayerWithoutCharacter);
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
                    distance = startToIntersection.Length();

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
                distance = lowLimit;
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
                Vector3 halfExt = CurrentBlockDefinition.Size * gridSize * 0.5f;
                float radius = halfExt.Length();
                return 3 * radius;
            }

            return 5;
        }

        private static void UpdateBlockInfoHud()
        {
            MyHud.BlockInfo.Visible = false;

			/*if (MyFakes.ENABLE_SIMPLE_SURVIVAL)
				return;*/

            var block = MyCubeBuilder.Static.HudBlockDefinition;
            if (block == null || !MyCubeBuilder.Static.IsActivated)
            {
                return;
            }

            MyHud.BlockInfo.LoadDefinition(block, MyCubeBuilder.BuildComponent.TotalMaterials);
            MyHud.BlockInfo.Visible = true;
            return;
        }


        #endregion

        #region Copy/Cut/Paste

        private void UpdatePasteNotification(MyStringId myTextsWrapperEnum)
        {
            RemovePasteNotification();

            if (m_clipboard.IsActive)
            {
                m_pasteNotification = new MyHudNotification(myTextsWrapperEnum, 0, level: MyNotificationLevel.Control);
                MyHud.Notifications.Add(m_pasteNotification);
            }
        }

        private void RemovePasteNotification()
        {
            if (m_pasteNotification != null)
            {
                MyHud.Notifications.Remove(m_pasteNotification);
                m_pasteNotification = null;
            }
        }

        private void OnCutConfirm(MyCubeGrid targetGrid, bool cutGroup)
        {
            Debug.Assert(targetGrid != null);

            //Check if entity wasn't deleted by someone else during waiting
            if (MyEntities.EntityExists(targetGrid.EntityId))
                if (cutGroup)
                    m_clipboard.CutGroup(targetGrid);
                else
                    m_clipboard.CutGrid(targetGrid);
        }

        private void OnCutAsteroidConfirm(MyVoxelMap targetVoxelMap)
        {
            Debug.Assert(targetVoxelMap != null);

            //Check if entity wasn't deleted by someone else during waiting
            if (MyEntities.EntityExists(targetVoxelMap.EntityId))
                targetVoxelMap.SyncObject.SendCloseRequest();
        }

        public void DeactivateCopyPasteVoxel()
        {
            if (!m_voxelClipboard.IsActive)
                return;

            m_voxelClipboard.Deactivate();

            RemovePasteNotification();
        }

        public void DeactivateCopyPasteFloatingObject()
        {
            if (!m_floatingObjectClipboard.IsActive)
                return;

            m_floatingObjectClipboard.Deactivate();

            RemovePasteNotification();
        }

        public void DeactivateCopyPaste()
        {
            if (!m_clipboard.IsActive)
                return;

            m_clipboard.Deactivate();

            RemovePasteNotification();
        }

        public void ActivateVoxelClipboard(MyObjectBuilder_VoxelMap voxelMap, IMyStorage storage, Vector3 centerDeltaDirection, float dragVectorLength)
        {
            if (m_shipCreationClipboard.IsActive)
                return;

            MySessionComponentVoxelHand.Static.Enabled = false;
            DeactivateMultiBlockClipboard();
            m_voxelClipboard.SetVoxelMapFromBuilder(voxelMap, storage, centerDeltaDirection, dragVectorLength);
        }

        public void ActivateFloatingObjectClipboard(MyObjectBuilder_FloatingObject floatingObject, Vector3 centerDeltaDirection, float dragVectorLength)
        {
            if (m_shipCreationClipboard.IsActive)
                return;

            MySessionComponentVoxelHand.Static.Enabled = false;
            DeactivateMultiBlockClipboard();
            m_floatingObjectClipboard.SetFloatingObjectFromBuilder(floatingObject, centerDeltaDirection, dragVectorLength);
        }

        public void ActivateShipCreationClipboard(MyObjectBuilder_CubeGrid grid, Vector3 centerDeltaDirection, float dragVectorLength)
        {
            if (m_shipCreationClipboard.IsActive)
                return;

            MySessionComponentVoxelHand.Static.Enabled = false;
            DeactivateMultiBlockClipboard();
            m_shipCreationClipboard.SetGridFromBuilder(grid, centerDeltaDirection, dragVectorLength);
        }

        private void ActivateMultiBlockCreationClipboard(MyObjectBuilder_CubeGrid grid, Vector3 centerDeltaDirection, float dragVectorLength)
        {
            if (m_multiBlockCreationClipboard.IsActive)
                return;

            MySessionComponentVoxelHand.Static.Enabled = false;
            m_multiBlockCreationClipboard.SetGridFromBuilder(grid, centerDeltaDirection, dragVectorLength);
        }

        public void ActivateShipCreationClipboard(MyObjectBuilder_CubeGrid[] grids, Vector3 centerDeltaDirection, float dragVectorLength)
        {
            if (m_shipCreationClipboard.IsActive)
                return;

            MySessionComponentVoxelHand.Static.Enabled = false;
            DeactivateMultiBlockClipboard();
            m_shipCreationClipboard.SetGridFromBuilders(grids, centerDeltaDirection, dragVectorLength);
        }

        public void DeactivateShipCreationClipboard()
        {
            if (!m_shipCreationClipboard.IsActive)
                return;

            m_shipCreationClipboard.Deactivate();
        }

        private void DeactivateMultiBlockClipboard()
        {
            if (!m_multiBlockCreationClipboard.IsActive)
                return;

            m_multiBlockCreationClipboard.Deactivate();
        }

        #endregion

        #region Grid creation

        public void StartNewGridPlacement(MyCubeSize cubeSize, bool isStatic)
        {
            var character = MySession.LocalCharacter;
            if (character != null)
            {
                character.SwitchToWeapon(null);
            }

            string prefabName;
            MyDefinitionManager.Static.GetBaseBlockPrefabName(cubeSize, isStatic, MySession.Static.CreativeMode, out prefabName);
            if (prefabName == null)
                return;
            var gridBuilders = MyPrefabManager.Static.GetGridPrefab(prefabName);
            Debug.Assert(gridBuilders != null && gridBuilders.Count() > 0);
            if (gridBuilders == null || gridBuilders.Count() == 0)
                return;

            var blockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(gridBuilders[0].CubeBlocks.First().GetId());
            var sizeInMeters = MyDefinitionManager.Static.GetCubeSize(cubeSize);
            Vector3 blockDiagonal = new Vector3(blockDefinition.Size) * sizeInMeters;

            Vector3 centerDisplacement;
            if (isStatic)
                centerDisplacement = Vector3.Zero;
            else
                centerDisplacement = new Vector3(0.0f, 0.55f, -1.0f) * sizeInMeters;

            foreach (var gridBuilder in gridBuilders)
            {
                if (gridBuilder.IsStatic && gridBuilder.PositionAndOrientation.HasValue)
                {
                    gridBuilder.PositionAndOrientation = MyPositionAndOrientation.Default;
                }
                foreach (var blockBuilder in gridBuilder.CubeBlocks)
                {
                    blockBuilder.ColorMaskHSV = MyToolbar.ColorMaskHSV;
                }
            }

            MyCubeBuilder.Static.ActivateShipCreationClipboard(gridBuilders, centerDisplacement, 5.0f + blockDiagonal.Length() * 0.5f);

            if (isStatic)
            {
                ShowStationRotationNotification();
            }
            else
            {
                HideStationRotationNotification();
            }
        }

        public void StartNewGridPlacement(MyCubeBlockDefinition blockDefinition, bool isStatic)
        {
            //MyCubeBuilder.Static.DeactivateShipCreationClipboard();
            var character = MySession.LocalCharacter;
            if (character != null)
            {
                character.SwitchToWeapon(null);
            }

            var gridBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(Vector3.Zero, Vector3.Forward, Vector3.Up);
            gridBuilder.GridSizeEnum = MyCubeSize.Small;
            gridBuilder.IsStatic = isStatic;
            gridBuilder.PersistentFlags |= MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;

            var blockBuilder = MyObjectBuilderSerializer.CreateNewObject(blockDefinition.Id) as MyObjectBuilder_CubeBlock;
            blockBuilder.Orientation = Quaternion.CreateFromForwardUp(Vector3I.Forward, Vector3I.Up);
            blockBuilder.ColorMaskHSV = new Vector3(0, -1, 0);

            gridBuilder.CubeBlocks.Add(blockBuilder);

            var sizeInMeters = MyDefinitionManager.Static.GetCubeSize(blockDefinition.CubeSize);
            Vector3 blockDiagonal = new Vector3(blockDefinition.Size) * sizeInMeters;

            Vector3 centerDisplacement;
            if (isStatic)
                centerDisplacement = Vector3.Zero;
            else
                centerDisplacement = Vector3.Zero; //new Vector3(0.0f, 0.55f, -1.0f) * sizeInMeters;

            foreach (var bb in gridBuilder.CubeBlocks)
            {
                bb.ColorMaskHSV = MyToolbar.ColorMaskHSV;
            }

            MyCubeBuilder.Static.ActivateShipCreationClipboard(gridBuilder, centerDisplacement, 5.0f + blockDiagonal.Length() * 0.5f);
        }

        public void StartNewGridPlacement(MyMultiBlockDefinition multiCubeBlockDefinition, Matrix rotationMatrix, bool isStatic)
        {
            Debug.Assert(MyFakes.ENABLE_MULTIBLOCKS);

            //var character = MySession.LocalCharacter;
            //if (character != null)
            //{
            //    character.SwitchToWeapon(null);
            //}

            var gridBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(Vector3.Zero, rotationMatrix.Forward, rotationMatrix.Up);
            gridBuilder.IsStatic = isStatic;
            gridBuilder.PersistentFlags |= MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;

            if (multiCubeBlockDefinition.BlockDefinitions == null)
            {
                Debug.Assert(false);
                return;
            }

            MyCubeSize? cubeSize = null;
            Vector3I min = Vector3I.MaxValue;
            Vector3I max = Vector3I.MinValue;

            foreach (var multiBlockPartDefinition in multiCubeBlockDefinition.BlockDefinitions)
            {
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
                blockBuilder.Min = multiBlockPartDefinition.Position;
                blockBuilder.ColorMaskHSV = MyToolbar.ColorMaskHSV;

                // Compound block
                if (MyFakes.ENABLE_COMPOUND_BLOCKS && blockDefinition.CompoundTemplates != null && blockDefinition.CompoundTemplates.Length > 0)
                {
                    MyObjectBuilder_CompoundCubeBlock compoundCBBuilder = MyCompoundCubeBlock.CreateBuilder(blockBuilder);
                    gridBuilder.CubeBlocks.Add(compoundCBBuilder);
                }
                else
                {
                    gridBuilder.CubeBlocks.Add(blockBuilder);
                }

                min = Vector3I.Min(min, multiBlockPartDefinition.Position);
                max = Vector3I.Max(max, multiBlockPartDefinition.Position);
            }

            if (gridBuilder.CubeBlocks.Count == 0)
            {
                Debug.Assert(false);
                return;
            }

            gridBuilder.GridSizeEnum = cubeSize.Value;

            var blockSizeInMeters = MyDefinitionManager.Static.GetCubeSize(cubeSize.Value);
            Vector3 gridSizeInMeters = (max - min + Vector3I.One) * blockSizeInMeters;

            ActivateMultiBlockCreationClipboard(gridBuilder, Vector3.Zero, IntersectionDistance);
        }

        public static void SpawnGrid(MyCubeBlockDefinition definition, MatrixD worldMatrix, MyEntity builder, bool isStatic)
        {
            Debug.Assert(Sync.IsServer);
            MyCubeGrid grid = null;
            if (isStatic)
                grid = SpawnStaticGrid(definition, builder, worldMatrix);
            else
                grid = SpawnDynamicGrid(definition, builder, worldMatrix);

            if (grid != null)
            {
                MySyncCreate.SendAfterGridBuilt(builder == null ? 0 : builder.EntityId, grid.EntityId);
                AfterGridBuild(builder, grid);
            }
        }

        public static void AfterGridBuild(MyEntity builder, MyCubeGrid grid)
        {
            if (grid != null)
            {
                MySlimBlock block = grid.GetCubeBlock(Vector3I.Zero);
                if (block != null)
                {
                    if (grid.IsStatic)
                    {
                        MyCubeGrid mainGrid = grid.DetectMerge(block);
                        if (mainGrid == null)
                            mainGrid = grid;
                        mainGrid.AdditionalModelGenerators.ForEach(g => g.UpdateAfterGridSpawn(block));

                        if (MyFakes.ENABLE_SMALL_BLOCK_TO_LARGE_STATIC_CONNECTIONS)
                        {
                            MyCubeGridSmallToLargeConnection.Static.AddBlockSmallToLargeConnection(block);
                        }
                    }

                    if (Sync.IsServer)
                    {
                        MyCubeBuilder.BuildComponent.AfterGridCreated(grid, builder);
                    }
                }
                else
                    Debug.Fail("Block not created");
            }
        }

        /// <summary>
        /// Spawn static grid - must have identity rotation matrix!
        /// </summary>
        public static MyCubeGrid SpawnStaticGrid(MyCubeBlockDefinition blockDefinition, MyEntity builder, MatrixD worldMatrix)
        {
            Debug.Assert(Sync.IsServer, "Only server can spawn grids! Clients have to send requests!");

            var gridBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(worldMatrix.Translation, Vector3.Forward, Vector3.Up);
            gridBuilder.GridSizeEnum = blockDefinition.CubeSize;
            gridBuilder.IsStatic = true;
            gridBuilder.PersistentFlags |= MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;
            gridBuilder.EntityId = MyEntityIdentifier.AllocateId();

            // Block must be placed on (0,0,0) coordinate
            var blockBuilder = MyObjectBuilderSerializer.CreateNewObject(blockDefinition.Id) as MyObjectBuilder_CubeBlock;
            blockBuilder.Orientation = Quaternion.CreateFromForwardUp(Vector3I.Round(worldMatrix.Forward), Vector3I.Round(worldMatrix.Up));
            Vector3I sizeRotated = Vector3I.Abs(Vector3I.Round(Vector3D.TransformNormal((Vector3)blockDefinition.Size, worldMatrix)));
            blockBuilder.Min = sizeRotated / 2 - sizeRotated + Vector3I.One;
            blockBuilder.EntityId = MyEntityIdentifier.AllocateId();
            MyCubeBuilder.BuildComponent.BeforeCreateBlock(blockDefinition, builder, blockBuilder);

            gridBuilder.CubeBlocks.Add(blockBuilder);

            MyCubeGrid grid = MyEntities.CreateFromObjectBuilderAndAdd(gridBuilder) as MyCubeGrid;
            if (grid != null)
            {
                MySyncCreate.SendEntityCreated(gridBuilder);
            }

            return grid;
        }

        public static MyCubeGrid SpawnDynamicGrid(MyCubeBlockDefinition blockDefinition, MyEntity builder, MatrixD worldMatrix, long entityId = 0)
        {
            var gridBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(ref worldMatrix);
            gridBuilder.GridSizeEnum = blockDefinition.CubeSize;
            gridBuilder.IsStatic = false;
            gridBuilder.PersistentFlags |= MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;

            // Block must be placed on (0,0,0) coordinate
            var blockBuilder = MyObjectBuilderSerializer.CreateNewObject(blockDefinition.Id) as MyObjectBuilder_CubeBlock;
            blockBuilder.Orientation = Quaternion.CreateFromForwardUp(Vector3I.Forward, Vector3I.Up);
            blockBuilder.Min = blockDefinition.Size / 2 - blockDefinition.Size + Vector3I.One;
            MyCubeBuilder.BuildComponent.BeforeCreateBlock(blockDefinition, builder, blockBuilder);

            gridBuilder.CubeBlocks.Add(blockBuilder);

            MyCubeGrid grid = null;

            //TODO: Try to find better way how to sync entity ID of subblocks..
            if (entityId != 0)
            {
                gridBuilder.EntityId = entityId;
                blockBuilder.EntityId = entityId + 1;
                grid = MyEntities.CreateFromObjectBuilderAndAdd(gridBuilder) as MyCubeGrid;
            }
            else
            {
                Debug.Assert(Sync.IsServer, "Only server can generate grid entity IDs!");
                if (Sync.IsServer)
                {
                    gridBuilder.EntityId = MyEntityIdentifier.AllocateId();
                    blockBuilder.EntityId = gridBuilder.EntityId + 1;
                    grid = MyEntities.CreateFromObjectBuilderAndAdd(gridBuilder) as MyCubeGrid;
                    if (grid != null)
                    {
                        MySyncCreate.SendEntityCreated(gridBuilder);
                    }
                }
            }

            return grid;
        }

        #endregion

        private static int m_cycle = 0;
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

        public bool CanBuildBlockSurvivalTime()
        {
            int currentGameTimeMs = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            return (currentGameTimeMs - m_lastBlockBuildTime) > SURVIVAL_BUILD_TIME_DELAY_MS;
        }
    }
}
