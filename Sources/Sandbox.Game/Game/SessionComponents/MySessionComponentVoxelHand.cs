using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.GameSystems;
using System.Diagnostics;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Voxels;

namespace Sandbox.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MySessionComponentVoxelHand : MySessionComponentBase
    {

        #region Static members

        private IMyVoxelBrush[] m_brushes;

        #endregion

        public override Type[] Dependencies
        {
            get
            {
                return new[] { typeof(MyToolbarComponent) };
            }
        }

        public static MySessionComponentVoxelHand Static;

        internal const float VOXEL_SIZE = MyVoxelConstants.VOXEL_SIZE_IN_METRES;
        internal const float VOXEL_HALF = MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;

        internal static float GRID_SIZE = MyDefinitionManager.Static.GetCubeSize(MyCubeSize.Large);
        internal static float SCALE_MAX = MyDefinitionManager.Static.GetCubeSize(MyCubeSize.Large)*10f;

        internal static float MIN_BRUSH_ZOOM = GRID_SIZE;
        internal static float MAX_BRUSH_ZOOM = GRID_SIZE*20f;

        private static float DEG_IN_RADIANS = MathHelper.ToRadians(1f);

        private byte    m_selectedMaterial;
        private int     m_materialCount;
        private float   m_position;
        public MatrixD m_rotation;

        private MyVoxelBase m_currentVoxelMap;

        private MyGuiCompositeTexture m_texture;

        public Color ShapeColor;

        private bool m_buildMode;
        public bool BuildMode
        {
            private set 
            {
                m_buildMode = value;
                MyHud.IsBuildMode = value;

                if (value)
                    ActivateHudBuildModeNotifications();
                else
                    DeactivateHudBuildModeNotifications();
            }
            get { return m_buildMode; }
        }

        private bool m_enabled;
        public bool Enabled
        {
            get { return m_enabled; }
            set
            {
                if (m_enabled != value)
                {
                    if (value)
                    {
                        this.Activate();
                    }
                    else
                    {
                        this.Deactivate();
                    }

                    m_enabled = value;
                }

                
            }
        }
        public bool SnapToVoxel { get; set; }
        public bool ProjectToVoxel { get; set; }
        public bool ShowGizmos { get; set; }
        public bool ScreenVisible { get; set; }
        public bool FreezePhysics { get; set; }

        private bool m_editing;

        public IMyVoxelBrush CurrentShape { get; set; }
        public MyVoxelHandDefinition CurrentDefinition { get; set; }

        private MyHudNotification m_voxelMaterialHint;
        private MyHudNotification m_voxelSettingsHint;
        private MyHudNotification m_joystickVoxelMaterialHint;
        private MyHudNotification m_joystickVoxelSettingsHint;
        private MyHudNotification m_buildModeHint;

        public MySessionComponentVoxelHand()
        {
            Static = this;
            SnapToVoxel   = true;
            ScreenVisible = false;
            ShowGizmos = true;
            ShapeColor = new Vector4(0.6f, 0.6f, 0.6f, 0.25f);

            m_selectedMaterial = 0;
            m_materialCount = MyDefinitionManager.Static.VoxelMaterialCount;
            m_position = MIN_BRUSH_ZOOM*2f;
            m_rotation  = MatrixD.Identity;

            m_texture = new MyGuiCompositeTexture();
            m_texture.Center = new MyGuiSizedTexture
            {
                Texture = MyDefinitionManager.Static.GetVoxelMaterialDefinition(m_selectedMaterial).DiffuseY
            };
        }

        public override void LoadData()
        {
            base.LoadData();
            MyToolbarComponent.CurrentToolbar.SelectedSlotChanged += CurrentToolbar_SelectedSlotChanged;
            MyToolbarComponent.CurrentToolbar.SlotActivated       += CurrentToolbar_SlotActivated;
            MyToolbarComponent.CurrentToolbar.Unselected          += CurrentToolbar_Unselected;

            InitializeHints();
        }

        protected override void UnloadData()
        {
            MyToolbarComponent.CurrentToolbar.Unselected          -= CurrentToolbar_Unselected;
            MyToolbarComponent.CurrentToolbar.SlotActivated       -= CurrentToolbar_SlotActivated;
            MyToolbarComponent.CurrentToolbar.SelectedSlotChanged -= CurrentToolbar_SelectedSlotChanged;
            base.UnloadData();
        }

        private void InitializeHints()
        {
            { // keyboard mouse hints
                var next = MyInput.Static.GetGameControl(MyControlsSpace.SWITCH_LEFT);
                var prev = MyInput.Static.GetGameControl(MyControlsSpace.SWITCH_RIGHT);
                //var voxelHandSettings = MyInput.Static.GetGameControl(MyControlsSpace.VOXEL_HAND_SETTINGS);
                
                m_voxelMaterialHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationVoxelMaterialFormat, next, prev);
                m_voxelSettingsHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationVoxelHandHintFormat, "Ctrl + H");
            }

            { // joystick hints
                var cx_voxel = MySpaceBindingCreator.CX_VOXEL;
                var voxelNextMaterialCode = MyControllerHelper.GetCodeForControl(cx_voxel, MyControlsSpace.SWITCH_LEFT);
                //var voxelSettingsCode = MyControllerHelper.GetCodeForControl(cx_voxel, MyControlsSpace.VOXEL_HAND_SETTINGS);
                var buildModeCode = MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.BUILD_MODE);
                m_joystickVoxelMaterialHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationJoystickVoxelMaterialFormat, voxelNextMaterialCode);
                m_joystickVoxelSettingsHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationVoxelHandHintFormat, "Ctrl + H");
                m_buildModeHint = MyHudNotifications.CreateControlNotification(MyCommonTexts.NotificationHintPressToOpenBuildMode, buildModeCode);
            }
        }

        #region Toolbar events

        private void CurrentToolbar_SelectedSlotChanged(MyToolbar toolbar, MyToolbar.SlotArgs args)
        {
            if (!(toolbar.SelectedItem is MyToolbarItemVoxelHand))
            {
                if (Enabled)
                    Enabled = false;
            }
        }

        private void CurrentToolbar_SlotActivated(MyToolbar toolbar, MyToolbar.SlotArgs args)
        {
            if (!(toolbar.GetItemAtIndex(toolbar.SlotToIndex(args.SlotNumber.Value)) is MyToolbarItemVoxelHand))
            {
                if (Enabled)
                    Enabled = false;
            }
        }

        private void CurrentToolbar_Unselected(MyToolbar toolbar)
        {
            if (Enabled)
                Enabled = false;
        }

        #endregion

        public override void HandleInput()
        {
            if (!Enabled || !(MyScreenManager.GetScreenWithFocus() is MyGuiScreenGamePlay))
                return;

            if (MyControllerHelper.IsControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.BUILD_MODE, MyControlStateType.NEW_PRESSED))
            {
                BuildMode = !BuildMode;
            }

            base.HandleInput();

            var context = BuildMode ? MySpaceBindingCreator.CX_VOXEL : MySpaceBindingCreator.CX_CHARACTER;

            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.VOXEL_HAND_SETTINGS))    //MyControlsSpace.TERMINAL
                MyScreenManager.AddScreen(new MyGuiScreenVoxelHandSetting());
         
            // rotation
            if      (MyControllerHelper.IsControl(context, MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE, MyControlStateType.PRESSED))
                m_rotation *= MatrixD.CreateFromAxisAngle(m_rotation.Forward, DEG_IN_RADIANS);
            else if (MyControllerHelper.IsControl(context, MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE, MyControlStateType.PRESSED))
                m_rotation *= MatrixD.CreateFromAxisAngle(m_rotation.Forward, -DEG_IN_RADIANS);
            else if (MyControllerHelper.IsControl(context, MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE, MyControlStateType.PRESSED))
                m_rotation *= MatrixD.CreateFromAxisAngle(m_rotation.Up, -DEG_IN_RADIANS);
            else if (MyControllerHelper.IsControl(context, MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE, MyControlStateType.PRESSED))
                m_rotation *= MatrixD.CreateFromAxisAngle(m_rotation.Up, DEG_IN_RADIANS);
            else if (MyControllerHelper.IsControl(context, MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE, MyControlStateType.PRESSED))
                m_rotation *= MatrixD.CreateFromAxisAngle(m_rotation.Right, - DEG_IN_RADIANS);
            else if (MyControllerHelper.IsControl(context, MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE, MyControlStateType.PRESSED))
                m_rotation *= MatrixD.CreateFromAxisAngle(m_rotation.Right, DEG_IN_RADIANS);

            CurrentShape.SetRotation(ref m_rotation);

            // voxel editing
            if (MyControllerHelper.IsControl(context, MyControlsSpace.SWITCH_LEFT, MyControlStateType.NEW_PRESSED))
            {
                SetMaterial(m_selectedMaterial, false);
            }
            else if (MyControllerHelper.IsControl(context, MyControlsSpace.SWITCH_RIGHT, MyControlStateType.NEW_PRESSED))
            {
                SetMaterial(m_selectedMaterial, true);

            }

            if (m_currentVoxelMap == null)
                return;

            var shape = CurrentShape as MyBrushAutoLevel;
            if (shape != null)
            {
                if (MyControllerHelper.IsControl(context, MyControlsSpace.PRIMARY_TOOL_ACTION, MyControlStateType.NEW_PRESSED) ||
                    MyControllerHelper.IsControl(context, MyControlsSpace.SECONDARY_TOOL_ACTION, MyControlStateType.NEW_PRESSED))
                {
                    shape.FixAxis();
                }
                else if (MyControllerHelper.IsControl(context, MyControlsSpace.PRIMARY_TOOL_ACTION, MyControlStateType.NEW_RELEASED) ||
                    MyControllerHelper.IsControl(context, MyControlsSpace.SECONDARY_TOOL_ACTION, MyControlStateType.NEW_RELEASED))
                {
                    shape.UnFix();
                }
            }

            bool edited = false;

            var phys = (MyVoxelPhysicsBody)m_currentVoxelMap.Physics;
            if (MyControllerHelper.IsControl(context, MyControlsSpace.PRIMARY_TOOL_ACTION, MyControlStateType.PRESSED))
            {
                if (phys != null) phys.QueueInvalidate = edited = FreezePhysics;
                CurrentShape.Fill(m_currentVoxelMap, m_selectedMaterial);
            }
            else if (MyInput.Static.IsMiddleMousePressed() || MyControllerHelper.IsControl(context, MyControlsSpace.VOXEL_PAINT, MyControlStateType.PRESSED))
            {
                CurrentShape.Paint(m_currentVoxelMap, m_selectedMaterial);
            }
            else if (MyControllerHelper.IsControl(context, MyControlsSpace.SECONDARY_TOOL_ACTION, MyControlStateType.PRESSED))
            {
                if (phys != null) phys.QueueInvalidate = edited = FreezePhysics;
                CurrentShape.CutOut(m_currentVoxelMap);
            }

            var scrolldir = Math.Sign(MyInput.Static.DeltaMouseScrollWheelValue());
            if (scrolldir != 0 && MyInput.Static.IsAnyCtrlKeyPressed())
            {
                var delta = (float)CurrentShape.GetBoundaries().HalfExtents.Length() * 0.5f; //Take into account size of brush when zooming
                SetBrushZoom(m_position + scrolldir * delta);
            }

            if (phys != null && m_editing != edited)
            {
                phys.QueueInvalidate = edited;
                m_editing = edited;
            }
        }

        public float GetBrushZoom()
        {
            return m_position;
        }

        public void SetBrushZoom(float value)
        {
            m_position = MathHelper.Clamp(value, MIN_BRUSH_ZOOM, MAX_BRUSH_ZOOM);
        }

        private void SetMaterial(byte idx, bool next = true)
        {
            idx = next ? ++idx : --idx;
            if (idx == byte.MaxValue)
                idx = (byte)(m_materialCount - 1);

            m_selectedMaterial = (byte)(idx % m_materialCount);

            var definition = MyDefinitionManager.Static.GetVoxelMaterialDefinition(m_selectedMaterial);
            if (definition.Id.SubtypeName == "BrownMaterial" || definition.Id.SubtypeName == "DebugMaterial")
            {
                SetMaterial(idx, next);
                return;
            }

            m_texture.Center = new MyGuiSizedTexture
            {
                Texture = definition.DiffuseXZ
            };
        }

        public override void UpdateBeforeSimulation()
        {
            if (!Enabled)
                return;

            base.UpdateBeforeSimulation();

            var character = MySession.Static.LocalCharacter;
            if (character == null)
                return;

            if (character.ControllerInfo.Controller == null)
            {
                Enabled = false;
                return;
            }

            var camera = MySector.MainCamera;
            if (camera == null)
                return;

            var position = MySession.Static.IsCameraUserControlledSpectator() ? camera.Position : character.GetHeadMatrix(true).Translation;
            var targetPosition = position + (Vector3D)camera.ForwardVector * Math.Max(2 * CurrentShape.GetBoundaries().TransformFast(camera.ViewMatrix).HalfExtents.Z, m_position);

            var vmap = m_currentVoxelMap;

            var boundingBox = CurrentShape.PeekWorldBoundingBox(ref targetPosition);
            m_currentVoxelMap = MySession.Static.VoxelMaps.GetVoxelMapWhoseBoundingBoxIntersectsBox(ref boundingBox, null);

            if (ProjectToVoxel && m_currentVoxelMap != null)
            {
                var hitList = new List<MyPhysics.HitInfo>();
                MyPhysics.CastRay(position, position + camera.ForwardVector * m_currentVoxelMap.SizeInMetres, hitList, MyPhysics.CollisionLayers.VoxelLod1CollisionLayer);
                bool found = false;
                foreach (var hit in hitList)
                {
                    var entity = hit.HkHitInfo.GetHitEntity();
                    if (entity is MyVoxelBase && ((MyVoxelBase)entity).RootVoxel == m_currentVoxelMap.RootVoxel)
                    {
                        targetPosition = hit.Position;
                        m_currentVoxelMap = (MyVoxelBase)entity;
                        found = true;
                        break;
                    }
                }
                if (found == false)
                {
                    m_currentVoxelMap = null;
                }
            }

            if (vmap != m_currentVoxelMap && vmap != null && vmap.Physics != null)
            {
                ((MyVoxelPhysicsBody)vmap.Physics).QueueInvalidate = false;
            }

            if (m_currentVoxelMap == null) return;
            else m_currentVoxelMap = m_currentVoxelMap.RootVoxel;

            if (SnapToVoxel)
            {
                // snap to voxel
                // voxel positions are floored, but we want to aim at approx. center of the shape, so offset target by half to turn this into rounding.
                targetPosition += MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;
                Vector3I targetVoxel;
                MyVoxelCoordSystems.WorldPositionToVoxelCoord(m_currentVoxelMap.PositionLeftBottomCorner, ref targetPosition, out targetVoxel);
                MyVoxelCoordSystems.VoxelCoordToWorldPosition(m_currentVoxelMap.PositionLeftBottomCorner, ref targetVoxel, out targetPosition);
                CurrentShape.SetPosition(ref targetPosition);
            }
            else
            {
                CurrentShape.SetPosition(ref targetPosition);
            }

        }

        static List<MyEntity> m_foundElements = new List<MyEntity>();
        public override void Draw()
        {
            if (!Enabled || m_currentVoxelMap == null)
                return;

            base.Draw();

            m_foundElements.Clear();
            BoundingBoxD box = m_currentVoxelMap.PositionComp.WorldAABB;
            Color color = new Color(0.2f, 0.0f, 0, 0.1f);
            MatrixD worldMatrix;

            if (ShowGizmos)
            {
                if (MyFakes.SHOW_FORBIDDEN_ENITIES_VOXEL_HAND)
                {
                    MyEntities.GetElementsInBox(ref box, m_foundElements);

                    foreach (var entity in m_foundElements)
                    {
                        if (!(entity is MyCharacter) && MyVoxelBase.IsForbiddenEntity(entity))
                        {
                            worldMatrix = entity.PositionComp.WorldMatrix;
                            box = (BoundingBoxD)entity.PositionComp.LocalAABB;
                            MySimpleObjectDraw.DrawTransparentBox(ref worldMatrix, ref box, ref color, MySimpleObjectRasterizer.SolidAndWireframe, 0);
                        }
                    }
                }

                if (MyFakes.SHOW_CURRENT_VOXEL_MAP_AABB_IN_VOXEL_HAND)
                {
                    box = (BoundingBoxD)m_currentVoxelMap.PositionComp.LocalAABB;
                    color = new Vector4(0.0f, 0.2f, 0, 0.1f);
                    worldMatrix = m_currentVoxelMap.PositionComp.WorldMatrix;
                    MySimpleObjectDraw.DrawTransparentBox(ref worldMatrix, ref box, ref color, MySimpleObjectRasterizer.Solid, 0);
                }
            }
            CurrentShape.Draw(ref ShapeColor);
            if (!MyHud.MinimalHud && !MyHud.CutsceneHud)
            {
                DrawMaterial();
            }
        }

        public void DrawMaterial()
        {
            var pos = new Vector2(0.5525f,0.84f);
            var size = new Vector2(0.05f, 0.05f);

            m_texture.Draw(pos, size, Color.White);
            MyGuiManager.DrawBorders(pos, size, Color.White, 1);
            
            pos.X += 0.06f;

            var text    = MyTexts.GetString(MyCommonTexts.VoxelHandSettingScreen_HandMaterial);
            var matName = MyDefinitionManager.Static.GetVoxelMaterialDefinition(m_selectedMaterial).Id.SubtypeName;
            MyGuiManager.DrawString(MyFontEnum.White, new StringBuilder(string.Format("{0}: {1}", text, matName)), pos, 1f);
        }

        private void ActivateHudNotifications()
        {
            if (MySession.Static.CreativeMode)
            {
                if (!MyInput.Static.IsJoystickConnected())
                {
                    MyHud.Notifications.Add(m_voxelMaterialHint);
                    MyHud.Notifications.Add(m_voxelSettingsHint);
                }
                else
                {
                    MyHud.Notifications.Add(m_buildModeHint);
                }
            }
        }

        private void DeactivateHudNotifications()
        {
            if (MySession.Static.CreativeMode)
            {
                MyHud.Notifications.Remove(m_voxelMaterialHint);
                MyHud.Notifications.Remove(m_voxelSettingsHint);
                MyHud.Notifications.Remove(m_joystickVoxelMaterialHint);
                MyHud.Notifications.Remove(m_joystickVoxelSettingsHint);
                if (m_buildModeHint != null)
                    MyHud.Notifications.Remove(m_buildModeHint);
            }
        }

        private void ActivateHudBuildModeNotifications()
        {
            if (MySession.Static.CreativeMode && MyInput.Static.IsJoystickConnected())
            {
                MyHud.Notifications.Add(m_joystickVoxelMaterialHint);
                MyHud.Notifications.Add(m_joystickVoxelSettingsHint);
                MyHud.Notifications.Remove(m_buildModeHint);
            }
        }

        private void DeactivateHudBuildModeNotifications()
        {
            if (MySession.Static.CreativeMode)
            {
                MyHud.Notifications.Remove(m_joystickVoxelMaterialHint);
                MyHud.Notifications.Remove(m_joystickVoxelSettingsHint);
                if (Enabled)
                    MyHud.Notifications.Add(m_buildModeHint);
            }
        }

        private void Activate()
        {
            this.AlignToGravity();
            ActivateHudNotifications();
        }

        private void Deactivate()
        {
            DeactivateHudNotifications();
            CurrentShape = null;
            BuildMode = false;
        }

        private void AlignToGravity()
        {
            if (CurrentShape.AutoRotate)
            {
                Vector3D gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(MySector.MainCamera.Position);
                if (!gravity.Equals(Vector3.Zero))
                {
                    gravity.Normalize();
                    Vector3D perpGrav = gravity;
                    gravity.CalculatePerpendicularVector(out perpGrav);
                    MatrixD rotation = MatrixD.CreateFromDir(perpGrav, -gravity);
                    //rotation = MatrixD.CreateRotationZ(-MathHelper.PiOver2) * rotation;

                    CurrentShape.SetRotation(ref rotation);
                    m_rotation = rotation;
                }
            }
        }

        /// <summary>
        /// Tries to set the brush on voxel hand.
        /// </summary>
        /// <param name="brushSubtypeName">Brush subtype name.</param>
        /// <returns>False if brush with given name does not exist.</returns>
        public bool TrySetBrush(string brushSubtypeName)
        {
            // If brushes not yet initialized, do it now.
            if(m_brushes == null)
            {
                m_brushes = new IMyVoxelBrush[]
                {
                    MyBrushBox.Static,
                    MyBrushCapsule.Static,
                    MyBrushRamp.Static,
                    MyBrushSphere.Static,
                    MyBrushAutoLevel.Static,
                    MyBrushEllipsoid.Static,
                };
            }

            foreach (var brush in m_brushes)
            {
                if(brushSubtypeName == brush.SubtypeName)
                {
                    CurrentShape = brush;
                    return true;
                }
            }

            Debug.Fail("Brush '" + brushSubtypeName+ "' does not exist");

            return false;
        }
    }

    public enum MyVoxelBrushGUIPropertyOrder
    {
        First,
        Second,
        Third
    }

    public interface IMyVoxelBrushGUIProperty
    {
        void AddControlsToList(List<MyGuiControlBase> list);
    }

    public class MyBrushGUIPropertyNumberCombo : IMyVoxelBrushGUIProperty
    {
        MyGuiControlLabel    m_label;
        MyGuiControlCombobox m_combo;

        public Action ItemSelected;

        public long SelectedKey;

        public MyBrushGUIPropertyNumberCombo(MyVoxelBrushGUIPropertyOrder order, MyStringId labelText)
        {
            var labelPos = new Vector2(-0.1f, -0.15f);
            var comboPos = new Vector2(-0.1f, -0.12f);

            switch (order)
            {
                case MyVoxelBrushGUIPropertyOrder.Second:
                    labelPos.Y = -0.07f;
                    comboPos.Y = -0.04f;
                    break;

                case MyVoxelBrushGUIPropertyOrder.Third:
                    labelPos.Y = 0.01f;
                    comboPos.Y = 0.04f;
                    break;
            }

            m_label = new MyGuiControlLabel { Position = labelPos, TextEnum = labelText, OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP };
            m_combo = new MyGuiControlCombobox();
            m_combo.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_combo.Position = comboPos;
            m_combo.Size = new Vector2(0.212f, 0.1f);
            m_combo.ItemSelected += Combo_ItemSelected;
        }

        public void AddItem(long key, MyStringId text)
        {
            m_combo.AddItem(key, text);
        }

        public void SelectItem(long key)
        {
            m_combo.SelectItemByKey(key);
        }

        private void Combo_ItemSelected()
        {
            SelectedKey = m_combo.GetSelectedKey();

            if (ItemSelected != null)
                ItemSelected();
        }

        public void AddControlsToList(List<MyGuiControlBase> list)
        {
            list.Add(m_label);
            list.Add(m_combo);
        }
    }

    public class MyBrushGUIPropertyNumberSelect : IMyVoxelBrushGUIProperty
    {
        MyGuiControlButton m_lowerValue;
        MyGuiControlButton m_upperValue;

        MyGuiControlLabel m_label;
        MyGuiControlLabel m_labelValue;

        public Action ValueIncreased;
        public Action ValueDecreased;

        public float Value;
        public float ValueMin;
        public float ValueMax;
        public float ValueStep;

        public MyBrushGUIPropertyNumberSelect(
            float value, float valueMin, float valueMax, float valueStep,
            MyVoxelBrushGUIPropertyOrder order, MyStringId labelText)
        {
            // first is default
            var labelPos = new Vector2(-0.1f,   -0.15f);
            var valuePos = new Vector2( 0.035f, -0.15f);
            var lowerPos = new Vector2( 0f,     -0.1475f);
            var upperPos = new Vector2( 0.08f,  -0.1475f);

            switch (order)
            {
                case MyVoxelBrushGUIPropertyOrder.Second:
                    labelPos.Y = -0.07f;
                    valuePos.Y = -0.07f;
                    lowerPos.Y = -0.0675f;
                    upperPos.Y = -0.0675f;
                    break;

                case MyVoxelBrushGUIPropertyOrder.Third:
                    labelPos.Y = 0.01f;
                    valuePos.Y = 0.01f;
                    lowerPos.Y = 0.0125f;
                    upperPos.Y = 0.0125f;
                    break;
            }

            Value     = value;
            ValueMin  = valueMin;
            ValueMax  = valueMax;
            ValueStep = valueStep;

            m_label      = new MyGuiControlLabel { Position = labelPos, TextEnum = labelText, OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP };
            m_lowerValue = new MyGuiControlButton { Position = lowerPos, VisualStyle = MyGuiControlButtonStyleEnum.ArrowLeft, OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP };
            m_upperValue = new MyGuiControlButton { Position = upperPos, VisualStyle = MyGuiControlButtonStyleEnum.ArrowRight, OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP };
            m_labelValue = new MyGuiControlLabel { Position = valuePos, Text = Value.ToString(), OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP };

            m_lowerValue.ButtonClicked += LowerClicked;
            m_upperValue.ButtonClicked += UpperClicked;
        }

        private void LowerClicked(MyGuiControlButton sender)
        {
            Value = MathHelper.Clamp(Value - ValueStep, ValueMin, ValueMax);
            m_labelValue.Text = Value.ToString();

            if (ValueDecreased != null)
                ValueDecreased();
        }

        private void UpperClicked(MyGuiControlButton sender)
        {
            Value = MathHelper.Clamp(Value + ValueStep, ValueMin, ValueMax);
            m_labelValue.Text = Value.ToString();

            if (ValueIncreased != null)
                ValueIncreased();
        }

        public void AddControlsToList(List<MyGuiControlBase> list)
        {
            list.Add(m_lowerValue);
            list.Add(m_upperValue);
            list.Add(m_label);
            list.Add(m_labelValue);
        }
    }

    public class MyBrushGUIPropertyNumberSlider : IMyVoxelBrushGUIProperty
    {
        MyGuiControlLabel  m_label;
        MyGuiControlLabel  m_labelValue;
        MyGuiControlSlider m_sliderValue;

        public Action ValueChanged;

        public float Value;
        public float ValueMin;
        public float ValueMax;
        public float ValueStep;

        public MyBrushGUIPropertyNumberSlider(
            float value, float valueMin, float valueMax, float valueStep,
            MyVoxelBrushGUIPropertyOrder order, MyStringId labelText)
        {
            // first is default
            var labelPos  = new Vector2(-0.1f,   -0.15f);
            var valuePos  = new Vector2( 0.075f, -0.15f);
            var sliderPos = new Vector2(-0.1f,   -0.12f);

            switch (order)
            {
                case MyVoxelBrushGUIPropertyOrder.Second:
                    labelPos.Y  = -0.07f;
                    valuePos.Y  = -0.07f;
                    sliderPos.Y = -0.04f;
                    break;

                case MyVoxelBrushGUIPropertyOrder.Third:
                    labelPos.Y  = 0.01f;
                    valuePos.Y  = 0.01f;
                    sliderPos.Y = 0.04f;
                    break;
            }

            Value = value;
            ValueMin = valueMin;
            ValueMax = valueMax;
            ValueStep = valueStep;

            m_label       = new MyGuiControlLabel { Position = labelPos, TextEnum = labelText, OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP };
            m_labelValue  = new MyGuiControlLabel { Position = valuePos, Text = Value.ToString(), OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP };
            m_sliderValue = new MyGuiControlSlider { Position = sliderPos,  OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP };
            m_sliderValue.Size = new Vector2(0.212f, 0.1f);
            m_sliderValue.MaxValue = ValueMax;
            m_sliderValue.Value = Value;
            m_sliderValue.MinValue = ValueMin;
            m_sliderValue.ValueChanged += Slider_ValueChanged;
        }

        private void Slider_ValueChanged(MyGuiControlSlider sender)
        {
            var inv = 1f / ValueStep;
            var val = m_sliderValue.Value * inv;
            Value = MathHelper.Clamp((int)val / inv, ValueMin, ValueMax);
            m_labelValue.Text = Value.ToString();

            if (ValueChanged != null)
                ValueChanged();
        }

        public void AddControlsToList(List<MyGuiControlBase> list)
        {
            list.Add(m_label);
            list.Add(m_labelValue);
            list.Add(m_sliderValue);
        }
    }

    public interface IMyVoxelBrush
    {
        // settings
        float MinScale { get; }
        float MaxScale { get; }
        bool AutoRotate { get; }

        // voxel
        void Fill(MyVoxelBase map, byte matId);
        void Paint(MyVoxelBase map, byte matId);
        void CutOut(MyVoxelBase map);

        // transformation
        void SetRotation(ref MatrixD rotationMat);
        void SetPosition(ref Vector3D targetPosition);

        // GUI & render
        BoundingBoxD PeekWorldBoundingBox(ref Vector3D targetPosition);
        BoundingBoxD GetBoundaries();
        BoundingBoxD GetWorldBoundaries();
        void Draw(ref Color color);
        List<MyGuiControlBase> GetGuiControls();

        // Other properties
        string SubtypeName { get; }
    }
    
    public class MyBrushBox : IMyVoxelBrush
    {
        public static MyBrushBox Static = new MyBrushBox();

        private MyShapeBox m_shape;
        private MatrixD    m_transform;

        private MyBrushGUIPropertyNumberSlider m_width;
        private MyBrushGUIPropertyNumberSlider m_height;
        private MyBrushGUIPropertyNumberSlider m_depth;
        private List<MyGuiControlBase> m_list;

        private MyBrushBox()
        {
            m_shape = new MyShapeBox();
            m_transform = MatrixD.Identity;

            m_width = new MyBrushGUIPropertyNumberSlider(
                MinScale, MinScale, MaxScale,
                MySessionComponentVoxelHand.VOXEL_HALF,
                MyVoxelBrushGUIPropertyOrder.First, MyCommonTexts.VoxelHandProperty_Box_Width
            );
            m_width.ValueChanged += RecomputeShape;

            m_height = new MyBrushGUIPropertyNumberSlider(
                MinScale, MinScale, MaxScale,
                MySessionComponentVoxelHand.VOXEL_HALF,
                MyVoxelBrushGUIPropertyOrder.Second, MyCommonTexts.VoxelHandProperty_Box_Height
            );
            m_height.ValueChanged += RecomputeShape;

            m_depth = new MyBrushGUIPropertyNumberSlider(
                MinScale, MinScale, MaxScale,
                MySessionComponentVoxelHand.VOXEL_HALF,
                MyVoxelBrushGUIPropertyOrder.Third, MyCommonTexts.VoxelHandProperty_Box_Depth
            );
            m_depth.ValueChanged += RecomputeShape;

            m_list = new List<MyGuiControlBase>();
            m_width.AddControlsToList(m_list);
            m_height.AddControlsToList(m_list);
            m_depth.AddControlsToList(m_list);

            RecomputeShape();
        }

        private void RecomputeShape()
        {
            var sh = new Vector3D(m_width.Value, m_height.Value, m_depth.Value) * 0.5;
            m_shape.Boundaries.Min = -sh;
            m_shape.Boundaries.Max =  sh;
        }

        #region IMyVoxelBrush

        public float MinScale { get { return MySessionComponentVoxelHand.VOXEL_SIZE; } }
        public float MaxScale { get { return MySessionComponentVoxelHand.GRID_SIZE * 40f; } }
        public bool AutoRotate { get { return true; } }
        public string SubtypeName { get { return "Box"; } }

        public void Fill(MyVoxelBase map, byte matId)
        {
            MyVoxelGenerator.RequestFillInShape(map, m_shape, matId);
        }

        public void Paint(MyVoxelBase map, byte matId)
        {
            MyVoxelGenerator.RequestPaintInShape(map, m_shape, matId);
        }

        public void CutOut(MyVoxelBase map)
        {
            MyVoxelGenerator.RequestCutOutShape(map, m_shape);
        }

        public void SetPosition(ref Vector3D targetPosition)
        {
            m_transform.Translation = targetPosition;
            m_shape.Transformation = m_transform;
        }

        public void SetRotation(ref MatrixD rotationMat) {
            if (!rotationMat.IsRotation())
                return;

            m_transform.M11 = rotationMat.M11;
            m_transform.M12 = rotationMat.M12;
            m_transform.M13 = rotationMat.M13;

            m_transform.M21 = rotationMat.M21;
            m_transform.M22 = rotationMat.M22;
            m_transform.M23 = rotationMat.M23;

            m_transform.M31 = rotationMat.M31;
            m_transform.M32 = rotationMat.M32;
            m_transform.M33 = rotationMat.M33;

            m_shape.Transformation = m_transform;
        }

        public BoundingBoxD GetBoundaries()
        {
            return m_shape.Boundaries;
        }
        public BoundingBoxD PeekWorldBoundingBox(ref Vector3D targetPosition)
        {
            return m_shape.PeekWorldBoundaries(ref targetPosition);
        }
        public BoundingBoxD GetWorldBoundaries()
        {
            return m_shape.GetWorldBoundaries();
        }

        public void Draw(ref Color color)
        {
            MySimpleObjectDraw.DrawTransparentBox(ref m_transform, ref m_shape.Boundaries, ref color, MySimpleObjectRasterizer.Solid, 1, 0.04f);
        }

        public List<MyGuiControlBase> GetGuiControls()
        {
            return m_list;
        }

        #endregion
    }

    public class MyBrushCapsule : IMyVoxelBrush
    {
        public static MyBrushCapsule Static = new MyBrushCapsule();

        private MyShapeCapsule m_shape;
        private MatrixD        m_transform;

        private MyBrushGUIPropertyNumberSlider m_radius;
        private MyBrushGUIPropertyNumberSlider m_length;
        private List<MyGuiControlBase> m_list;

        private MyBrushCapsule()
        {
            m_shape = new MyShapeCapsule();
            m_transform = MatrixD.Identity;

            m_radius = new MyBrushGUIPropertyNumberSlider(
                MinScale, MinScale, MaxScale,
                MySessionComponentVoxelHand.VOXEL_HALF,
                MyVoxelBrushGUIPropertyOrder.First, MyCommonTexts.VoxelHandProperty_Capsule_Radius
            );
            m_radius.ValueChanged += RecomputeShape;

            m_length = new MyBrushGUIPropertyNumberSlider(
                MinScale, MinScale, MaxScale,
                MySessionComponentVoxelHand.VOXEL_HALF,
                MyVoxelBrushGUIPropertyOrder.Second, MyCommonTexts.VoxelHandProperty_Capsule_Length
            );
            m_length.ValueChanged += RecomputeShape;

            m_list = new List<MyGuiControlBase>();
            m_radius.AddControlsToList(m_list);
            m_length.AddControlsToList(m_list);

            RecomputeShape();
        }

        private void RecomputeShape()
        {
            m_shape.Radius = m_radius.Value;

            var hl = m_length.Value * 0.5;
            m_shape.A.X = m_shape.A.Z = 0.0;
            m_shape.B.X = m_shape.B.Z = 0.0;
            m_shape.A.Y = -hl;
            m_shape.B.Y =  hl;
        }

        #region IMyVoxelBrush

        public float MinScale { get { return MySessionComponentVoxelHand.VOXEL_SIZE + MySessionComponentVoxelHand.VOXEL_HALF; } }
        public float MaxScale { get { return MySessionComponentVoxelHand.GRID_SIZE * 40f; } }
        public bool AutoRotate { get { return true; } }
        public string SubtypeName { get { return "Capsule"; } }

        public void Fill(MyVoxelBase map, byte matId)
        {
            MyVoxelGenerator.RequestFillInShape(map, m_shape, matId);
        }

        public void Paint(MyVoxelBase map, byte matId)
        {
            MyVoxelGenerator.RequestPaintInShape(map, m_shape, matId);
        }

        public void CutOut(MyVoxelBase map)
        {
            MyVoxelGenerator.RequestCutOutShape(map, m_shape);
        }

        public void SetPosition(ref Vector3D targetPosition)
        {
            m_transform.Translation = targetPosition;
            m_shape.Transformation = m_transform;
        }

        public void SetRotation(ref MatrixD rotationMat)
        {
            if (!rotationMat.IsRotation())
                return;

            m_transform.M11 = rotationMat.M11;
            m_transform.M12 = rotationMat.M12;
            m_transform.M13 = rotationMat.M13;
            
            m_transform.M21 = rotationMat.M21;
            m_transform.M22 = rotationMat.M22;
            m_transform.M23 = rotationMat.M23;

            m_transform.M31 = rotationMat.M31;
            m_transform.M32 = rotationMat.M32;
            m_transform.M33 = rotationMat.M33;

            m_shape.Transformation = m_transform;
        }

        public BoundingBoxD GetBoundaries()
        {
            return m_shape.GetWorldBoundaries();
        }
        public BoundingBoxD PeekWorldBoundingBox(ref Vector3D targetPosition)
        {
            return m_shape.PeekWorldBoundaries(ref targetPosition);
        }
        public BoundingBoxD GetWorldBoundaries()
        {
            return m_shape.GetWorldBoundaries();
        }

        public void Draw(ref Color color)
        {
            MySimpleObjectDraw.DrawTransparentCapsule(ref m_transform, m_shape.Radius, m_length.Value, ref color, 20);
        }

        public List<MyGuiControlBase> GetGuiControls()
        {
            return m_list;
        }

        #endregion
    }

    public class MyBrushRamp : IMyVoxelBrush
    {
        public static MyBrushRamp Static = new MyBrushRamp();

        private MyShapeRamp m_shape;
        private MatrixD     m_transform;

        private MyBrushGUIPropertyNumberSlider m_width;
        private MyBrushGUIPropertyNumberSlider m_height;
        private MyBrushGUIPropertyNumberSlider m_depth;
        private List<MyGuiControlBase> m_list;

        private MyBrushRamp()
        {
            m_shape = new MyShapeRamp();
            m_transform = MatrixD.Identity;

            m_width = new MyBrushGUIPropertyNumberSlider(
                MinScale, MinScale, MaxScale,
                MySessionComponentVoxelHand.VOXEL_HALF,
                MyVoxelBrushGUIPropertyOrder.First, MyCommonTexts.VoxelHandProperty_Box_Width
            );
            m_width.ValueChanged += RecomputeShape;

            m_height = new MyBrushGUIPropertyNumberSlider(
                MinScale, MinScale, MaxScale,
                MySessionComponentVoxelHand.VOXEL_HALF,
                MyVoxelBrushGUIPropertyOrder.Second, MyCommonTexts.VoxelHandProperty_Box_Height
            );
            m_height.ValueChanged += RecomputeShape;

            m_depth = new MyBrushGUIPropertyNumberSlider(
                MinScale, MinScale, MaxScale,
                MySessionComponentVoxelHand.VOXEL_HALF,
                MyVoxelBrushGUIPropertyOrder.Third, MyCommonTexts.VoxelHandProperty_Box_Depth
            );
            m_depth.ValueChanged += RecomputeShape;

            m_list = new List<MyGuiControlBase>();
            m_width.AddControlsToList(m_list);
            m_height.AddControlsToList(m_list);
            m_depth.AddControlsToList(m_list);

            RecomputeShape();
        }

        private void RecomputeShape()
        {
            var sh = new Vector3D(m_width.Value, m_height.Value, m_depth.Value) * 0.5;
            m_shape.Boundaries.Min = -sh;
            m_shape.Boundaries.Max = sh;

            var p = m_shape.Boundaries.Min; p.X -= m_shape.Boundaries.Size.Z;
            var n = Vector3D.Normalize((m_shape.Boundaries.Min - p).Cross(m_shape.Boundaries.Max - p));
            var w = n.Dot(p);

            m_shape.RampNormal = n;
            m_shape.RampNormalW = -w;
        }

        #region IMyVoxelBrush

        public float MinScale { get { return MySessionComponentVoxelHand.VOXEL_SIZE * 4.5f; } }
        public float MaxScale { get { return MySessionComponentVoxelHand.GRID_SIZE * 40f; } }
        public bool AutoRotate { get { return true; } }
        public string SubtypeName { get { return "Ramp"; } }

        public void Fill(MyVoxelBase map, byte matId)
        {
            MyVoxelGenerator.RequestFillInShape(map, m_shape, matId);
        }

        public void Paint(MyVoxelBase map, byte matId)
        {
            MyVoxelGenerator.RequestPaintInShape(map, m_shape, matId);
        }

        public void CutOut(MyVoxelBase map)
        {
            MyVoxelGenerator.RequestCutOutShape(map, m_shape);
        }

        public void SetPosition(ref Vector3D targetPosition)
        {
            m_transform.Translation = targetPosition;
            m_shape.Transformation = m_transform;
        }

        public void SetRotation(ref MatrixD rotationMat)
        {
            if (!rotationMat.IsRotation())
                return;

            m_transform.M11 = rotationMat.M11;
            m_transform.M12 = rotationMat.M12;
            m_transform.M13 = rotationMat.M13;
            
            m_transform.M21 = rotationMat.M21;
            m_transform.M22 = rotationMat.M22;
            m_transform.M23 = rotationMat.M23;

            m_transform.M31 = rotationMat.M31;
            m_transform.M32 = rotationMat.M32;
            m_transform.M33 = rotationMat.M33;

            m_shape.Transformation = m_transform;
        }

        public BoundingBoxD GetBoundaries()
        {
            return m_shape.Boundaries;
        }
        public BoundingBoxD PeekWorldBoundingBox(ref Vector3D targetPosition)
        {
            return m_shape.PeekWorldBoundaries(ref targetPosition);
        }
        public BoundingBoxD GetWorldBoundaries()
        {
            return m_shape.GetWorldBoundaries();
        }
        public void Draw(ref Color color)
        {
            MySimpleObjectDraw.DrawTransparentRamp(ref m_transform, ref m_shape.Boundaries, ref color);
        }

        public List<MyGuiControlBase> GetGuiControls()
        {
            return m_list;
        }

        #endregion
    }

    public class MyBrushSphere : IMyVoxelBrush
    {
        public static MyBrushSphere Static = new MyBrushSphere();

        private MyShapeSphere m_shape;
        private MatrixD       m_transform;

        private MyBrushGUIPropertyNumberSlider m_radius;
        private List<MyGuiControlBase> m_list;

        private MyBrushSphere()
        {
            m_shape = new MyShapeSphere();
            m_shape.Radius = MinScale;
            m_transform = MatrixD.Identity;

            m_radius = new MyBrushGUIPropertyNumberSlider(
                m_shape.Radius, MinScale, MaxScale,
                MySessionComponentVoxelHand.VOXEL_HALF,
                MyVoxelBrushGUIPropertyOrder.First, MyCommonTexts.VoxelHandProperty_Sphere_Radius
            );
            m_radius.ValueChanged += RadiusChanged;

            m_list = new List<MyGuiControlBase>();
            m_radius.AddControlsToList(m_list);
        }

        private void RadiusChanged()
        {
            m_shape.Radius = m_radius.Value;
        }

        #region IMyVoxelBrush

        public float MinScale { get { return MySessionComponentVoxelHand.VOXEL_SIZE + MySessionComponentVoxelHand.VOXEL_HALF; } }
        public float MaxScale { get { return MySessionComponentVoxelHand.GRID_SIZE * 40f; } }
        public bool AutoRotate { get { return false; } }
        public string SubtypeName { get { return "Sphere"; } }

        public void Fill(MyVoxelBase map, byte matId)
        {
            MyVoxelGenerator.RequestFillInShape(map, m_shape, matId);
        }

        public void Paint(MyVoxelBase map, byte matId)
        {
            MyVoxelGenerator.RequestPaintInShape(map, m_shape, matId);
        }

        public void CutOut(MyVoxelBase map)
        {
            MyVoxelGenerator.RequestCutOutShape(map, m_shape);
        }

        public void SetPosition(ref Vector3D targetPosition)
        {
            m_shape.Center = targetPosition;
            m_transform.Translation = targetPosition;
        }

        public void SetRotation(ref MatrixD rotationMat) {}

        public BoundingBoxD GetBoundaries()
        {
            return m_shape.GetWorldBoundaries();
        }
        public BoundingBoxD PeekWorldBoundingBox(ref Vector3D targetPosition)
        {
            return m_shape.PeekWorldBoundaries(ref targetPosition);
        }
        public BoundingBoxD GetWorldBoundaries()
        {
            return m_shape.GetWorldBoundaries();
        }
        public void Draw(ref Color color)
        {
            MySimpleObjectDraw.DrawTransparentSphere(ref m_transform, m_shape.Radius, ref color, MySimpleObjectRasterizer.Solid, 20);
        }

        public List<MyGuiControlBase> GetGuiControls()
        {
            return m_list;
        }

        #endregion
    }

    public class MyBrushEllipsoid : IMyVoxelBrush
    {
        public static MyBrushEllipsoid Static = new MyBrushEllipsoid();

        private MyShapeEllipsoid m_shape;
        private MatrixD m_transform;

        private MyBrushGUIPropertyNumberSlider m_radiusX;
        private MyBrushGUIPropertyNumberSlider m_radiusY;
        private MyBrushGUIPropertyNumberSlider m_radiusZ;
        private List<MyGuiControlBase> m_list;

        private MyBrushEllipsoid()
        {
            m_shape = new MyShapeEllipsoid();
            m_transform = MatrixD.Identity;

            float step = MySessionComponentVoxelHand.VOXEL_SIZE / 4;

            m_radiusX = new MyBrushGUIPropertyNumberSlider(
                MinScale, MinScale, MaxScale, step,
                MyVoxelBrushGUIPropertyOrder.First, MyStringId.GetOrCompute("Radius X")
            );
            m_radiusX.ValueChanged += RadiusChanged;

            m_radiusY = new MyBrushGUIPropertyNumberSlider(
                MinScale, MinScale, MaxScale, step,
                MyVoxelBrushGUIPropertyOrder.Second, MyStringId.GetOrCompute("Radius Y")
            );
            m_radiusY.ValueChanged += RadiusChanged;

            m_radiusZ = new MyBrushGUIPropertyNumberSlider(
                MinScale, MinScale, MaxScale, step,
                MyVoxelBrushGUIPropertyOrder.Third, MyStringId.GetOrCompute("Radius Z")
            );
            m_radiusZ.ValueChanged += RadiusChanged;

            m_list = new List<MyGuiControlBase>();
            m_radiusX.AddControlsToList(m_list);
            m_radiusY.AddControlsToList(m_list);
            m_radiusZ.AddControlsToList(m_list);

            RecomputeShape();
        }

        private void RadiusChanged()
        {
            RecomputeShape();
        }

        private void RecomputeShape()
        {
            m_shape.Radius = new Vector3(m_radiusX.Value, m_radiusY.Value, m_radiusZ.Value);
        }

        #region IMyVoxelBrush

        public float MinScale { get { return MySessionComponentVoxelHand.VOXEL_SIZE/4; } }
        public float MaxScale { get { return MySessionComponentVoxelHand.GRID_SIZE * 40f; } }
        public bool AutoRotate { get { return false; } }
        public string SubtypeName { get { return "Ellipsoid"; } }

        public void Fill(MyVoxelBase map, byte matId)
        {
            MyVoxelGenerator.RequestFillInShape(map, m_shape, matId);
        }

        public void Paint(MyVoxelBase map, byte matId)
        {
            MyVoxelGenerator.RequestPaintInShape(map, m_shape, matId);
        }

        public void CutOut(MyVoxelBase map)
        {
            MyVoxelGenerator.RequestCutOutShape(map, m_shape);
        }

        public void SetPosition(ref Vector3D targetPosition)
        {
            m_transform.Translation = targetPosition;
            m_shape.Transformation = m_transform;
        }

        public void SetRotation(ref MatrixD rotationMat) 
        {
            if (!rotationMat.IsRotation())
                return;

            m_transform.M11 = rotationMat.M11;
            m_transform.M12 = rotationMat.M12;
            m_transform.M13 = rotationMat.M13;

            m_transform.M21 = rotationMat.M21;
            m_transform.M22 = rotationMat.M22;
            m_transform.M23 = rotationMat.M23;

            m_transform.M31 = rotationMat.M31;
            m_transform.M32 = rotationMat.M32;
            m_transform.M33 = rotationMat.M33;

            m_shape.Transformation = m_transform;
        }

        public BoundingBoxD GetBoundaries()
        {
            return m_shape.GetWorldBoundaries();
        }
        public BoundingBoxD PeekWorldBoundingBox(ref Vector3D targetPosition)
        {
            return m_shape.PeekWorldBoundaries(ref targetPosition);
        }
        public BoundingBoxD GetWorldBoundaries()
        {
            return m_shape.GetWorldBoundaries();
        }

        public void Draw(ref Color color)
        {
            BoundingBoxD boundaries = m_shape.Boundaries;
            MySimpleObjectDraw.DrawTransparentBox(ref m_transform, ref boundaries, ref color, MySimpleObjectRasterizer.Solid, 1, 0.04f);
        }

        public List<MyGuiControlBase> GetGuiControls()
        {
            return m_list;
        }

        #endregion
    }

    public class MyBrushAutoLevel : IMyVoxelBrush
    {
        public static MyBrushAutoLevel Static = new MyBrushAutoLevel();

        private MyShapeBox m_shape;
        private MatrixD    m_transform;

        private MyBrushGUIPropertyNumberCombo  m_axis;
        private MyBrushGUIPropertyNumberSlider m_area;
        private MyBrushGUIPropertyNumberSlider m_height;
        private List<MyGuiControlBase> m_list;

        private const long X_ASIS = 0;
        private const long Y_ASIS = 1;
        private const long Z_ASIS = 2;

        private bool   m_painting;
        private double m_Xpos;
        private double m_Ypos;
        private double m_Zpos;

        private MyBrushAutoLevel()
        {
            m_shape = new MyShapeBox();
            m_transform = MatrixD.Identity;

            m_axis = new MyBrushGUIPropertyNumberCombo(
                MyVoxelBrushGUIPropertyOrder.First, MyCommonTexts.VoxelHandProperty_AutoLevel_Axis
            );
            m_axis.AddItem(X_ASIS, MyCommonTexts.VoxelHandProperty_AutoLevel_AxisX);
            m_axis.AddItem(Y_ASIS, MyCommonTexts.VoxelHandProperty_AutoLevel_AxisY);
            m_axis.AddItem(Z_ASIS, MyCommonTexts.VoxelHandProperty_AutoLevel_AxisZ);
            m_axis.SelectItem(Y_ASIS);

            m_area = new MyBrushGUIPropertyNumberSlider(
                MinScale*2f, MinScale, MaxScale,
                MySessionComponentVoxelHand.VOXEL_HALF,
                MyVoxelBrushGUIPropertyOrder.Second, MyCommonTexts.VoxelHandProperty_AutoLevel_Area
            );
            m_area.ValueChanged += RecomputeShape;

            m_height = new MyBrushGUIPropertyNumberSlider(
                MinScale, MinScale, MaxScale,
                MySessionComponentVoxelHand.VOXEL_HALF,
                MyVoxelBrushGUIPropertyOrder.Third, MyCommonTexts.VoxelHandProperty_Box_Height
            );
            m_height.ValueChanged += RecomputeShape;

            m_list = new List<MyGuiControlBase>();
            m_axis.AddControlsToList(m_list);
            m_area.AddControlsToList(m_list);
            m_height.AddControlsToList(m_list);

            RecomputeShape();
        }

        private void RecomputeShape()
        {
            var ha = m_area.Value * 0.5;
            var hh = m_height.Value * 0.5;

            m_shape.Boundaries.Min.X = -ha;
            m_shape.Boundaries.Min.Y = -hh;
            m_shape.Boundaries.Min.Z = -ha;
            m_shape.Boundaries.Max.X =  ha;
            m_shape.Boundaries.Max.Y =  hh;
            m_shape.Boundaries.Max.Z =  ha;
        }

        public void FixAxis()
        {
            m_painting = true;

            var max =  m_shape.Boundaries.TransformFast(m_transform).Center;

            switch (m_axis.SelectedKey)
            {
                case X_ASIS: m_Xpos = max.X; break;
                case Y_ASIS: m_Ypos = max.Y; break;
                case Z_ASIS: m_Zpos = max.Z; break;
            }
        }

        public void UnFix()
        {
            m_painting = false;
        }

        #region IMyVoxelBrush

        public float MinScale { get { return MySessionComponentVoxelHand.GRID_SIZE; } }
        public float MaxScale { get { return MySessionComponentVoxelHand.GRID_SIZE * 40f; } }
        public bool AutoRotate { get { return true; } }
        public string SubtypeName { get { return "AutoLevel"; } }

        public void Fill(MyVoxelBase map, byte matId)
        {
            MyVoxelGenerator.RequestFillInShape(map, m_shape, matId);
        }

        public void Paint(MyVoxelBase map, byte matId) { }
        public void CutOut(MyVoxelBase map) 
        {
            MyVoxelGenerator.RequestCutOutShape(map, m_shape);
        }

        public void SetPosition(ref Vector3D targetPosition)
        {
            if (m_painting)
            {
                switch (m_axis.SelectedKey)
                {
                    case X_ASIS: targetPosition.X = m_Xpos; break;
                    case Y_ASIS: targetPosition.Y = m_Ypos; break;
                    case Z_ASIS: targetPosition.Z = m_Zpos; break;
                }
            }
            m_transform.Translation = targetPosition;
            m_shape.Transformation = m_transform;
        }

        public void SetRotation(ref MatrixD rotationMat) {
            if (!rotationMat.IsRotation())
                return;

            m_transform.M11 = rotationMat.M11;
            m_transform.M12 = rotationMat.M12;
            m_transform.M13 = rotationMat.M13;

            m_transform.M21 = rotationMat.M21;
            m_transform.M22 = rotationMat.M22;
            m_transform.M23 = rotationMat.M23;

            m_transform.M31 = rotationMat.M31;
            m_transform.M32 = rotationMat.M32;
            m_transform.M33 = rotationMat.M33;

            m_shape.Transformation = m_transform;
        }

        public BoundingBoxD GetBoundaries()
        {
            return m_shape.Boundaries;
        }

        public BoundingBoxD PeekWorldBoundingBox(ref Vector3D targetPosition)
        {
            return m_shape.PeekWorldBoundaries(ref targetPosition);
        }

        public BoundingBoxD GetWorldBoundaries()
        {
            return m_shape.GetWorldBoundaries();
        }

        public void Draw(ref Color color)
        {
            MySimpleObjectDraw.DrawTransparentBox(ref m_transform, ref m_shape.Boundaries, ref color, MySimpleObjectRasterizer.Solid, 1, 0.04f);
        }

        public List<MyGuiControlBase> GetGuiControls()
        {
            return m_list;
        }

        #endregion
    }
}
