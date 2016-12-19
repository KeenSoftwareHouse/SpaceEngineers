using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.ContextHandling;
using Sandbox.Game.GameSystems.CoordinateSystem;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Components.Session;
using VRage.Game.Definitions.SessionComponents;
using VRage.Game.Entity;
using VRage.Input;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.Multiplayer;
using VRage.Audio;
using VRage.Profiler;

namespace Sandbox.Game.SessionComponents.Clipboard
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MyClipboardComponent : MySessionComponentBase, IMyFocusHolder
    {

        #region static members

        public static MyClipboardComponent Static;

        protected static readonly MyStringId[] m_rotationControls = new MyStringId[]
        {
            MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE,
            MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE,
            MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE,
            MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE,
            MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE,
            MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE,
        };

        protected static readonly int[] m_rotationDirections = new int[6] { -1, 1, 1, -1, 1, -1 };

        #endregion

        #region Data members

        private static MyClipboardDefinition m_definition;
        public static MyClipboardDefinition ClipboardDefinition { get { return m_definition; } }

        //Static on purpose, because we want to copy between sessions
        static private MyGridClipboard m_clipboard;

        public MyGridClipboard Clipboard
        {
            get { return m_clipboard; }
        }

        private MyFloatingObjectClipboard m_floatingObjectClipboard = new MyFloatingObjectClipboard(true);
        internal MyFloatingObjectClipboard FloatingObjectClipboard
        {
            get { return m_floatingObjectClipboard; }
        }

        private MyVoxelClipboard m_voxelClipboard = new MyVoxelClipboard();
        internal MyVoxelClipboard VoxelClipboard
        {
            get { return m_voxelClipboard; }
        }

        MyHudNotification m_symmetryNotification;
        MyHudNotification m_pasteNotification;

        private float IntersectionDistance = 20f;//MyBlockBuilderBase.DefaultBlockBuildingDistance;

        public Vector3D FreePlacementTarget
        {
            get
            {
                return MyBlockBuilderBase.IntersectionStart + MyBlockBuilderBase.IntersectionDirection * IntersectionDistance;
            }
        }

        private float BLOCK_ROTATION_SPEED = 0.002f;

        private MyCubeBlockDefinitionWithVariants m_definitionWithVariants;

        protected MyBlockBuilderRotationHints m_rotationHints = new MyBlockBuilderRotationHints();
        private List<Vector3D> m_collisionTestPoints = new List<Vector3D>(12);

        private int m_lastInputHandleTime;
        protected bool m_rotationHintRotating = false;
        private bool m_activated = false;

        MyHudNotification m_stationRotationNotification;
        MyHudNotification m_stationRotationNotificationOff;

        #endregion

        #region Properties

        private static bool DeveloperSpectatorIsBuilding
        {
            get
            {
                return MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.Spectator &&
                    (!MyFinalBuildConstants.IS_OFFICIAL || !MySession.Static.SurvivalMode || MyInput.Static.ENABLE_DEVELOPER_KEYS);
            }
        }

        public static bool SpectatorIsBuilding
        {
            get
            {
                return DeveloperSpectatorIsBuilding || AdminSpectatorIsBuilding;
            }
        }

        private static bool AdminSpectatorIsBuilding
        {
            get
            {
                return MyFakes.ENABLE_ADMIN_SPECTATOR_BUILDING && MySession.Static != null && MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.Spectator
                    && MyMultiplayer.Static != null && MySession.Static.LocalHumanPlayer != null && MySession.Static.LocalHumanPlayer.IsAdmin;
            }
        }

        #endregion

        #region Init

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
        }

        public override void InitFromDefinition(MySessionComponentDefinition definition)
        {
            base.InitFromDefinition(definition);

            MyClipboardDefinition clipboardDefinition = definition as MyClipboardDefinition;
            if (clipboardDefinition == null)
                Debug.Fail("Wrong definition! Please check.");

            if (m_clipboard == null)
            {
                m_definition = clipboardDefinition;
                m_clipboard = new MyGridClipboard(m_definition.PastingSettings, calculateVelocity: true);
            }
        }

        public override void LoadData()
        {
            base.LoadData();
            Static = this;
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            if (m_clipboard != null)
            {
            m_clipboard.Deactivate();
            }
            if (m_floatingObjectClipboard != null)
            {
            m_floatingObjectClipboard.Deactivate();
            }
            if (m_voxelClipboard != null)
            {
            m_voxelClipboard.Deactivate();
            }

            Static = null;
        }

        #endregion

        #region Operations

        private void RotateAxis(int index, int sign, bool newlyPressed, int frameDt)
        {
            float angleDelta = frameDt * BLOCK_ROTATION_SPEED;

            if (MyInput.Static.IsAnyCtrlKeyPressed())
            {
                if (!newlyPressed)
                    return;
                angleDelta = MathHelper.PiOver2;
            }
            if (MyInput.Static.IsAnyAltKeyPressed())
            {
                if (!newlyPressed)
                    return;
                angleDelta = MathHelper.ToRadians(1);
            }

            if (m_clipboard.IsActive)
                m_clipboard.RotateAroundAxis(index, sign, newlyPressed, angleDelta);
            if (m_floatingObjectClipboard.IsActive)
                m_floatingObjectClipboard.RotateAroundAxis(index, sign, newlyPressed, angleDelta);
        }

        public bool HandleGameInput()
        {
            m_rotationHintRotating = false;

            var context = (m_activated && MySession.Static.ControlledEntity is MyCharacter) ? MySession.Static.ControlledEntity.ControlContext : MyStringId.NullOrEmpty;

            // When spectator active, building is instant
            if (MySession.Static.IsCopyPastingEnabled || MySession.Static.CreativeMode || (SpectatorIsBuilding && MyFinalBuildConstants.IS_OFFICIAL == false))
            {
                if (MySession.Static.IsCopyPastingEnabled && !(MySession.Static.ControlledEntity is MyShipController))
                {
                    if (this.HandleCopyInput())
                        return true;

                    if (this.HandleCutInput())
                        return true;

                    if (this.HandlePasteInput())
                        return true;

                    if (HandleMouseScrollInput(context))
                        return true;
                }

                if (this.HandleEscape())
                    return true;

                if (this.HandleLeftMouseButton(context))
                    return true;
            }          

            if (this.HandleBlueprintInput())
                return true;

            if (m_clipboard != null && m_clipboard.IsActive && (MyControllerHelper.IsControl(context, MyControlsSpace.FREE_ROTATION) ||
                                        MyControllerHelper.IsControl(context, MyControlsSpace.SWITCH_BUILDING_MODE)))
            {

                m_clipboard.EnableStationRotation = !m_clipboard.EnableStationRotation;
                m_floatingObjectClipboard.EnableStationRotation = !m_floatingObjectClipboard.EnableStationRotation;

                //if (m_clipboard.EnableStationRotation)
                //    ShowStationRotationNotification();
                //else
                //    HideStationRotationNotification();

            }

            if (HandleRotationInput(context))
                return true;

            return false;
        }

        private bool HandleLeftMouseButton(MyStringId context)
        {
            if (MyInput.Static.IsNewLeftMousePressed() || MyControllerHelper.IsControl(context, MyControlsSpace.COPY_PASTE_ACTION))
            {

                bool handled = false;
                if (m_clipboard.IsActive)
                {
                    if (m_clipboard.PasteGrid())
                    {
                        UpdatePasteNotification(MyCommonTexts.CubeBuilderPasteNotification);
                        handled = true;
                    }
                }

                if (m_floatingObjectClipboard.IsActive)
                {
                    if (m_floatingObjectClipboard.PasteFloatingObject())
                    {
                        UpdatePasteNotification(MyCommonTexts.CubeBuilderPasteNotification);
                        handled = true;
                    }
                }

                if (m_voxelClipboard.IsActive)
                {
                    if (m_voxelClipboard.PasteVoxelMap())
                    {
                        UpdatePasteNotification(MyCommonTexts.CubeBuilderPasteNotification);
                        handled = true;
                    }
                }

                if (handled)
                {
                    this.Deactivate();
                    return true;
                }

            }

            return false;
        }

        private bool HandleEscape()
        {
            if (MyInput.Static.IsNewKeyPressed(MyKeys.Escape))
            {
                bool handled = false;
                if (m_clipboard.IsActive)
                {
                    m_clipboard.Deactivate();
                    UpdatePasteNotification(MyCommonTexts.CubeBuilderPasteNotification);
                    handled = true;
                }

                if (m_floatingObjectClipboard.IsActive)
                {
                    m_floatingObjectClipboard.Deactivate();
                    UpdatePasteNotification(MyCommonTexts.CubeBuilderPasteNotification);
                    handled = true;
                }

                if (m_voxelClipboard.IsActive)
                {
                    m_voxelClipboard.Deactivate();
                    UpdatePasteNotification(MyCommonTexts.CubeBuilderPasteNotification);
                    handled = true;
                }

                if (handled)
                {
                    this.Deactivate();
                    return true;
                }
            }

            return false;
        }

        private bool HandlePasteInput()
        {
            if (MyInput.Static.IsNewKeyPressed(MyKeys.V) && MyInput.Static.IsAnyCtrlKeyPressed() && !MyInput.Static.IsAnyShiftKeyPressed())
            {
                bool handled = false;
                MySession.Static.GameFocusManager.Clear();
                if (m_clipboard.PasteGrid())
                {
                    MySessionComponentVoxelHand.Static.Enabled = false;
                    UpdatePasteNotification(MyCommonTexts.CubeBuilderPasteNotification);
                    handled = true;
                }
                else if (m_floatingObjectClipboard.PasteFloatingObject())
                {
                    MySessionComponentVoxelHand.Static.Enabled = false;
                    UpdatePasteNotification(MyCommonTexts.CubeBuilderPasteNotification);
                    handled = true;
                }

                if (handled)
                {
                    if (m_activated)
                        this.Deactivate();
                    else
                        this.Activate();
                    return true;
                }

            }

            return false;
        }

        private bool HandleCutInput()
        {
            if (MyInput.Static.IsNewKeyPressed(MyKeys.X) && MyInput.Static.IsAnyCtrlKeyPressed())
            {
                MyEntity entity = MyCubeGrid.GetTargetEntity();
                if (entity == null)
                    return false;

                bool handled = false;

                if (entity is MyCubeGrid && m_clipboard.IsActive == false)
                {

                    MyGuiAudio.PlaySound(MyGuiSounds.HudClick);

                    bool cutGroup = !MyInput.Static.IsAnyShiftKeyPressed();
                    bool cutOverLg = MyInput.Static.IsAnyAltKeyPressed();

                    MyEntities.EnableEntityBoundingBoxDraw(entity, true);

                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        buttonType: MyMessageBoxButtonsType.YES_NO,
                        messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextAreYouSureToMoveGridToClipboard),
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                        callback: (v) =>
                        {
                            if (v == MyGuiScreenMessageBox.ResultEnum.YES)
                            {
                                OnCutConfirm(entity as MyCubeGrid, cutGroup, cutOverLg);
                            }

                            MyEntities.EnableEntityBoundingBoxDraw(entity, false);
                        }));

                    handled = true;

                }
                else if (entity is MyVoxelMap && m_voxelClipboard.IsActive == false &&
                    MyPerGameSettings.GUI.VoxelMapEditingScreen == typeof(MyGuiScreenDebugSpawnMenu) // hack to disable this in ME
                    )
                {

                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        buttonType: MyMessageBoxButtonsType.YES_NO,
                        messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextAreYouSureToRemoveAsteroid),
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                        callback: (v) =>
                        {
                            if (v == MyGuiScreenMessageBox.ResultEnum.YES)
                            {
                                OnCutAsteroidConfirm(entity as MyVoxelMap);
                            }
                            MyEntities.EnableEntityBoundingBoxDraw(entity, false);
                        }));

                    handled = true;

                }
                else if (entity is MyFloatingObject && !m_floatingObjectClipboard.IsActive)
                {

                    MyEntities.EnableEntityBoundingBoxDraw(entity, true);

                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        buttonType: MyMessageBoxButtonsType.YES_NO,
                        messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextAreYouSureToMoveGridToClipboard),
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                        callback: (v) =>
                        {
                            if (v == MyGuiScreenMessageBox.ResultEnum.YES)
                            {
                                OnCutFloatingObjectConfirm(entity as MyFloatingObject);
                                handled = true;
                            }

                            MyEntities.EnableEntityBoundingBoxDraw(entity, false);
                        }));

                    handled = true;
                }

                if (handled)
                {
                    return true;
                }

            }

            return false;
        }

        private bool HandleCopyInput()
        {
            if (MyInput.Static.IsNewKeyPressed(MyKeys.C) && MyInput.Static.IsAnyCtrlKeyPressed() && !MyInput.Static.IsAnyMousePressed())
            {
                if (MySession.Static.CameraController is MyCharacter || MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.Spectator)
                {
                    bool handled = false;
                    MyGuiAudio.PlaySound(MyGuiSounds.HudClick);

                    var entity = MyCubeGrid.GetTargetEntity();
                    if (m_clipboard.IsActive == false && entity is MyCubeGrid)
                    {
                        MyCubeGrid grid = entity as MyCubeGrid;
                        MySessionComponentVoxelHand.Static.Enabled = false;
                        DeactivateCopyPasteFloatingObject(true);

                        if (!MyInput.Static.IsAnyShiftKeyPressed())
                        {
                            m_clipboard.CopyGroup(grid, MyInput.Static.IsAnyAltKeyPressed() ? GridLinkTypeEnum.Physical : GridLinkTypeEnum.Logical);
                            m_clipboard.Activate();
                        }
                        else
                        {
                            m_clipboard.CopyGrid(grid);
                            m_clipboard.Activate();
                        }
                        UpdatePasteNotification(MyCommonTexts.CubeBuilderPasteNotification);
                        handled = true;
                    }
                    else if (!m_floatingObjectClipboard.IsActive && entity is MyFloatingObject)
                    {
                        MySessionComponentVoxelHand.Static.Enabled = false;
                        DeactivateCopyPaste(true);

                        m_floatingObjectClipboard.CopyfloatingObject(entity as MyFloatingObject);
                        UpdatePasteNotification(MyCommonTexts.CubeBuilderPasteNotification);
                        handled = true;
                    }

                    if (handled)
                    {
                        this.Activate();
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HandleRotationInput(MyStringId context)
        {
            int frameDt = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastInputHandleTime;
            m_lastInputHandleTime += frameDt;

            if (m_activated)
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

                        //if (MyFakes.ENABLE_STANDARD_AXES_ROTATION)
                        //{
                        //    axis = GetStandardRotationAxisAndDirection(i, ref direction);
                        //}

                        if (MyFakes.ENABLE_STANDARD_AXES_ROTATION)
                        {
                            int[] axes = new int[] { 1, 1, 0, 0, 2, 2 };
                            if (m_rotationHints.RotationUpAxis != axes[i])
                            {
                                return true;
                                //axis = m_rotationHints.RotationUpAxis;
                                //direction *= m_rotationHints.RotationUpDirection;
                            }
                        }
                        //else
                        //{
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
                       // }

                        if (axis != -1)
                        {
                            m_rotationHintRotating |= !newPress;
                            RotateAxis(axis, direction, newPress, frameDt);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool HandleBlueprintInput()
        {
            if (MyInput.Static.IsNewKeyPressed(MyKeys.B) && MyInput.Static.IsAnyCtrlKeyPressed() && !MyInput.Static.IsAnyMousePressed())
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudClick);

                if (!m_clipboard.IsActive)
                {
                    MySessionComponentVoxelHand.Static.Enabled = false;
                    var copiedGrid = MyCubeGrid.GetTargetGrid();

                    if (!MyInput.Static.IsAnyShiftKeyPressed())
                        m_clipboard.CopyGroup(copiedGrid, MyInput.Static.IsAnyAltKeyPressed() ? GridLinkTypeEnum.Physical : GridLinkTypeEnum.Logical);
                    else
                        m_clipboard.CopyGrid(copiedGrid);

                    UpdatePasteNotification(MyCommonTexts.CubeBuilderPasteNotification);

                    var blueprintScreen = new MyGuiBlueprintScreen(m_clipboard, MySession.Static.CreativeMode || MySession.Static.CreativeToolsEnabled(Sync.MyId));
                    if (copiedGrid != null)
                    {
                        blueprintScreen.CreateFromClipboard(true);
                    }
                    m_clipboard.Deactivate();
                    MyGuiSandbox.AddScreen(blueprintScreen);
                }
                return true;
            }

            return false;
        }

        //private void ShowStationRotationNotification()
        //{
        //    if (MyPerGameSettings.Game == GameEnum.ME_GAME) //TODO: refactor to remove it.
        //        return;

        //    if (m_stationRotationNotificationOff == null)
        //    {
        //        m_stationRotationNotificationOff = new MyHudNotification(MySpaceTexts.NotificationStationRotationOff, 0, priority: 1);
        //        m_stationRotationNotificationOff.SetTextFormatArguments(MyInput.Static.GetGameControl(MyControlsSpace.FREE_ROTATION));
        //    }

        //    MyHud.Notifications.Remove(m_stationRotationNotification);
        //    MyHud.Notifications.Add(m_stationRotationNotificationOff);
        //}

        //private void HideStationRotationNotification()
        //{
        //    if (m_stationRotationNotification != null)
        //    {
        //        MyHud.Notifications.Remove(m_stationRotationNotification);
        //    }
        //    if (m_stationRotationNotificationOff != null)
        //    {
        //        MyHud.Notifications.Remove(m_stationRotationNotificationOff);
        //    }
        //}

        private void OnCutConfirm(MyCubeGrid targetGrid, bool cutGroup, bool cutOverLgs)
        {
            Debug.Assert(targetGrid != null);

            //Check if entity wasn't deleted by someone else during waiting
            if (MyEntities.EntityExists(targetGrid.EntityId))
            {
                DeactivateCopyPasteVoxel(true);
                DeactivateCopyPasteFloatingObject(true);

                if (cutGroup)
                {
                    m_clipboard.CutGroup(targetGrid, cutOverLgs ? GridLinkTypeEnum.Physical : GridLinkTypeEnum.Logical);
                }
                else
                    m_clipboard.CutGrid(targetGrid);
            }
        }

        private void OnCutAsteroidConfirm(MyVoxelMap targetVoxelMap)
        {
            Debug.Assert(targetVoxelMap != null);

            //Check if entity wasn't deleted by someone else during waiting
            if (MyEntities.EntityExists(targetVoxelMap.EntityId))
            {
                DeactivateCopyPaste(true);
                DeactivateCopyPasteFloatingObject(true);
                targetVoxelMap.SyncObject.SendCloseRequest();
            }
        }

        private void OnCutFloatingObjectConfirm(MyFloatingObject floatingObj)
        {
            Debug.Assert(floatingObj != null);

            if (MyEntities.Exist(floatingObj))
            {
                DeactivateCopyPasteVoxel(true);
                DeactivateCopyPaste(true);
                m_floatingObjectClipboard.CutFloatingObject(floatingObj);
            }
        }

        public void OnLostFocus()
        {
            //this.DeactivateCopyPasteVoxel();
            //this.DeactivateCopyPasteFloatingObject();
            //this.DeactivateCopyPaste();
            this.Deactivate();
        }

        public void DeactivateCopyPasteVoxel(bool clear = false)
        {
            if (m_voxelClipboard.IsActive)
                m_voxelClipboard.Deactivate();

            RemovePasteNotification();

            if (clear)
                m_voxelClipboard.ClearClipboard();
        }

        public void DeactivateCopyPasteFloatingObject(bool clear = false)
        {
            if (m_floatingObjectClipboard.IsActive)
                m_floatingObjectClipboard.Deactivate();

            RemovePasteNotification();

            if (clear)
                m_floatingObjectClipboard.ClearClipboard();
        }

        public void DeactivateCopyPaste(bool clear = false)
        {
            if (m_clipboard.IsActive)
                m_clipboard.Deactivate();

            RemovePasteNotification();

            if (clear)
                m_clipboard.ClearClipboard();
        }

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

        private bool HandleMouseScrollInput(MyStringId context)
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

                return handled;
            }

            return false;
        }

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

        private void Activate()
        {
            MySession.Static.GameFocusManager.Register(this);
            m_activated = true;
        }

        private void Deactivate()
        {
            MySession.Static.GameFocusManager.Unregister(this);
            m_activated = false;
            m_rotationHints.ReleaseRenderData();
            this.DeactivateCopyPasteVoxel();
            this.DeactivateCopyPasteFloatingObject();
            this.DeactivateCopyPaste();
        }

        public void ActivateVoxelClipboard(MyObjectBuilder_EntityBase voxelMap, IMyStorage storage, Vector3 centerDeltaDirection, float dragVectorLength)
        {
            MySessionComponentVoxelHand.Static.Enabled = false;
            m_voxelClipboard.SetVoxelMapFromBuilder(voxelMap, storage, centerDeltaDirection, dragVectorLength);
            this.Activate();
        }

        public void ActivateFloatingObjectClipboard(MyObjectBuilder_FloatingObject floatingObject, Vector3 centerDeltaDirection, float dragVectorLength)
        {
            MySessionComponentVoxelHand.Static.Enabled = false;
            m_floatingObjectClipboard.SetFloatingObjectFromBuilder(floatingObject, centerDeltaDirection, dragVectorLength);
            this.Activate();
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if (!m_activated)
                return;

            //var normal = GetSingleMountPointNormal();
            //// Gizmo add dir can be zero in some cases
            //if (normal.HasValue && (GridAndBlockValid || VoxelMapAndBlockValid) && m_gizmo.SpaceDefault.m_addDir != Vector3I.Zero)
            //{
            //    m_gizmo.SetupLocalAddMatrix(m_gizmo.SpaceDefault, normal.Value);
            //}
            //m_gizmo.SetupLocalAddMatrix(m_gizmo.SpaceDefault, normal.Value);
            ProfilerShort.Begin("m_clipboard.Update");
            m_clipboard.Update();
            ProfilerShort.End();

            ProfilerShort.Begin("m_floatingObjectClipboard.Update");
            m_floatingObjectClipboard.Update();
            ProfilerShort.End();

            ProfilerShort.Begin("m_voxelClipboard.Update");
            m_voxelClipboard.Update();
            ProfilerShort.End();

            if (m_clipboard.IsActive || m_floatingObjectClipboard.IsActive || m_voxelClipboard.IsActive)
            {

                m_collisionTestPoints.Clear();
                PrepareCharacterCollisionPoints(m_collisionTestPoints);


                if (m_clipboard.IsActive)
                {
                    m_clipboard.Show();
                    //GR: For now disable this functionallity. Issue with render not all blocks are hidden (Cubeblocks are not hidden)
                    //m_clipboard.HideGridWhenColliding(m_collisionTestPoints);
                }
                else
                    m_clipboard.Hide();


                if (m_floatingObjectClipboard.IsActive)
                {
                    m_floatingObjectClipboard.Show();
                    m_floatingObjectClipboard.HideWhenColliding(m_collisionTestPoints);
                }
                else
                    m_floatingObjectClipboard.Hide();

                if (m_voxelClipboard.IsActive)
                    m_voxelClipboard.Show();
                else
                    m_voxelClipboard.Hide();

            }


            UpdateClipboards();

        }

        private void UpdateClipboards()
        {
            if (m_clipboard.IsActive)
            {
                m_clipboard.CalculateRotationHints(m_rotationHints, m_rotationHintRotating);
            }
            else if (m_floatingObjectClipboard.IsActive)
            {
                m_floatingObjectClipboard.CalculateRotationHints(m_rotationHints, m_rotationHintRotating);
            }
        }

        #endregion

    }
}
