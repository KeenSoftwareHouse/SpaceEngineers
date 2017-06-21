using Sandbox.Common;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenVoxelHandSetting : MyGuiScreenBase
    {
        MyGuiControlLabel m_labelSettings;

        MyGuiControlLabel    m_labelSnapToVoxel;
        MyGuiControlCheckbox m_checkSnapToVoxel;

        MyGuiControlLabel m_labelProjectToVoxel;
        MyGuiControlCheckbox m_projectToVoxel;

        MyGuiControlLabel m_labelFreezePhysics;
        MyGuiControlCheckbox m_freezePhysicsCheck;

        MyGuiControlLabel m_labelShowGizmos;
        MyGuiControlCheckbox m_showGizmos;

        MyGuiControlLabel  m_labelTransparency;
        MyGuiControlSlider m_sliderTransparency;

        MyGuiControlLabel  m_labelZoom;
        MyGuiControlSlider m_sliderZoom;

        MyGuiControlVoxelHandSettings m_voxelControl;

        public MyGuiScreenVoxelHandSetting()
            : base(size: new Vector2(0.25f, 0.5f),
                   backgroundColor: MyGuiConstants.SCREEN_BACKGROUND_COLOR,
                   backgroundTexture: MyGuiConstants.TEXTURE_HUD_BG_LARGE_DEFAULT.Texture)
        {
            CanHideOthers = false;
            EnabledBackgroundFade = false;

            m_position = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            m_position.X -= m_size.Value.X * 0.44f;
            m_position.Y += m_size.Value.Y - 0.24f;

            RecreateControls(true);

            MySessionComponentVoxelHand.Static.ScreenVisible = true;
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            float basePosition = -0.22f;

            m_labelSettings = new MyGuiControlLabel() { Position = new Vector2(-0.1f, basePosition), TextEnum = MyCommonTexts.VoxelHandSettingScreen_HandSettings, OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, Font = MyFontEnum.ScreenCaption };

            basePosition += 0.045f;
            m_checkSnapToVoxel = new MyGuiControlCheckbox() { Position = new Vector2(0.115f, basePosition), OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP };
            m_checkSnapToVoxel.IsChecked = MySessionComponentVoxelHand.Static.SnapToVoxel;
            m_checkSnapToVoxel.IsCheckedChanged += SnapToVoxel_Changed;

            basePosition += 0.01f;
            m_labelSnapToVoxel = new MyGuiControlLabel() { Position = new Vector2(-0.1f, basePosition), TextEnum = MyCommonTexts.VoxelHandSettingScreen_HandSnapToVoxel, OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP };

            basePosition += 0.045f;
            m_labelTransparency = new MyGuiControlLabel() { Position = new Vector2(-0.1f, basePosition), TextEnum = MyCommonTexts.VoxelHandSettingScreen_HandTransparency, OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP };
          
            basePosition += 0.035f;
            m_sliderTransparency = new MyGuiControlSlider() { Position = new Vector2(-0.1f, basePosition), OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP };
            m_sliderTransparency.Size = new Vector2(0.212f, 0.1f);
            m_sliderTransparency.MinValue = 0.0f;
            m_sliderTransparency.MaxValue = 1f;
            m_sliderTransparency.Value = 1f - MySessionComponentVoxelHand.Static.ShapeColor.ToVector4().W;
            m_sliderTransparency.ValueChanged += BrushTransparency_ValueChanged;


            basePosition += 0.045f;
            m_labelZoom = new MyGuiControlLabel() { Position = new Vector2(-0.1f, basePosition), TextEnum = MyCommonTexts.VoxelHandSettingScreen_HandDistance, OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP };

            basePosition += 0.035f;
            m_sliderZoom = new MyGuiControlSlider() { Position = new Vector2(-0.1f, basePosition), OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP };
            m_sliderZoom.Size = new Vector2(0.212f, 0.1f);
            m_sliderZoom.MaxValue = MySessionComponentVoxelHand.MAX_BRUSH_ZOOM;
            m_sliderZoom.Value = MySessionComponentVoxelHand.Static.GetBrushZoom();
            m_sliderZoom.MinValue = MySessionComponentVoxelHand.MIN_BRUSH_ZOOM;
            m_sliderZoom.Enabled = !MySessionComponentVoxelHand.Static.ProjectToVoxel;
            m_sliderZoom.ValueChanged += BrushZoom_ValueChanged;

            /* Project to Voxel */

            basePosition += 0.06f;
            m_projectToVoxel = new MyGuiControlCheckbox() { Position = new Vector2(0.115f, basePosition), OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP };
            m_projectToVoxel.IsChecked = MySessionComponentVoxelHand.Static.ProjectToVoxel;
            m_projectToVoxel.IsCheckedChanged += ProjectToVoxel_Changed;

            basePosition += 0.01f;
            m_labelProjectToVoxel = new MyGuiControlLabel() { Position = new Vector2(-0.1f, basePosition), TextEnum = MyCommonTexts.VoxelHandSettingScreen_HandProjectToVoxel, OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP };

            /* Freeze Physics */

            basePosition += 0.045f;
            m_freezePhysicsCheck = new MyGuiControlCheckbox() { Position = new Vector2(0.115f, basePosition), OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP };
            m_freezePhysicsCheck.IsChecked = MySessionComponentVoxelHand.Static.FreezePhysics;
            m_freezePhysicsCheck.IsCheckedChanged += FreezePhysics_Changed;

            basePosition += 0.01f;
            m_labelFreezePhysics = new MyGuiControlLabel() { Position = new Vector2(-0.1f, basePosition), TextEnum = MyCommonTexts.VoxelHandSettingScreen_FreezePhysics, OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP };

            /* Show Gizmos */

            basePosition += 0.045f;
            m_showGizmos = new MyGuiControlCheckbox() { Position = new Vector2(0.115f, basePosition), OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP };
            m_showGizmos.IsChecked = MySessionComponentVoxelHand.Static.ShowGizmos;
            m_showGizmos.IsCheckedChanged += ShowGizmos_Changed;

            basePosition += 0.01f;
            m_labelShowGizmos = new MyGuiControlLabel() { Position = new Vector2(-0.1f, basePosition), TextEnum = MyCommonTexts.VoxelHandSettingScreen_HandShowGizmos, OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP };

            m_voxelControl = new MyGuiControlVoxelHandSettings();
            m_voxelControl.Position = new Vector2(-0.125f, 0.26f);
            m_voxelControl.OKButton.ButtonClicked += OKButtonClicked;
            m_voxelControl.Item = MyToolbarComponent.CurrentToolbar.SelectedItem as MyToolbarItemVoxelHand;
            m_voxelControl.UpdateFromBrush(MySessionComponentVoxelHand.Static.CurrentShape);

            Controls.Add(m_labelSettings);
            Controls.Add(m_labelSnapToVoxel);
            Controls.Add(m_checkSnapToVoxel);
            Controls.Add(m_labelShowGizmos);
            Controls.Add(m_showGizmos);
            Controls.Add(m_labelProjectToVoxel);
            Controls.Add(m_projectToVoxel);
            Controls.Add(m_labelFreezePhysics);
            Controls.Add(m_freezePhysicsCheck);
            Controls.Add(m_labelTransparency);
            Controls.Add(m_sliderTransparency);
            Controls.Add(m_labelZoom);
            Controls.Add(m_sliderZoom);
            Controls.Add(m_voxelControl);
        }

        private void SnapToVoxel_Changed(MyGuiControlCheckbox sender)
        {
            MySessionComponentVoxelHand.Static.SnapToVoxel = m_checkSnapToVoxel.IsChecked;
        }

        private void ShowGizmos_Changed(MyGuiControlCheckbox sender)
        {
            MySessionComponentVoxelHand.Static.ShowGizmos = m_showGizmos.IsChecked;
        }

        private void ProjectToVoxel_Changed(MyGuiControlCheckbox sender)
        {
            MySessionComponentVoxelHand.Static.ProjectToVoxel = m_projectToVoxel.IsChecked;
            m_sliderZoom.Enabled = !m_projectToVoxel.IsChecked;
        }

        private void FreezePhysics_Changed(MyGuiControlCheckbox sender)
        {
            MySessionComponentVoxelHand.Static.FreezePhysics = sender.IsChecked;
        }

        private void BrushTransparency_ValueChanged(MyGuiControlSlider sender)
        {
            MySessionComponentVoxelHand.Static.ShapeColor.A = (byte)((1f - m_sliderTransparency.Value) * 255f);
        }

        private void BrushZoom_ValueChanged(MyGuiControlSlider sender)
        {
            MySessionComponentVoxelHand.Static.SetBrushZoom(m_sliderZoom.Value);
        }

        private void OKButtonClicked(MyGuiControlButton sender)
        {
            CloseScreen();
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.VOXEL_HAND_SETTINGS))    // or MyControlsSpace.TERMINAL
            {
                CloseScreen();
            }
            base.HandleInput(receivedFocusInThisUpdate);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenVoxelHandSetting";
        }
    }
}
