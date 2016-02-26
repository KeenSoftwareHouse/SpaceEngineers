using Sandbox;
using Sandbox.Common;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using VRageRender;
using MyGuiConstants = Sandbox.Graphics.GUI.MyGuiConstants;

namespace SpaceEngineers.Game.Gui
{
    class MyGuiScreenOptionsVideoSpace : MyGuiScreenBase
    {
        private MyGuiControlCombobox m_videoAdapterCombobox;
        private MyGuiControlCombobox m_resolutionCombobox;
        private MyGuiControlCheckbox m_verticalSyncCheckbox;
        private MyGuiControlCombobox m_windowModeCombobox;
        private MyGuiControlCheckbox m_hardwareCursorCheckbox;
        private MyGuiControlCheckbox m_enableDamageEffectsCheckbox;
        private MyGuiControlCombobox m_renderQualityCombobox;
        private MyGuiControlLabel m_recommendAspectRatioLabel;
        private MyGuiControlLabel m_unsupportedAspectRatioLabel;
        private MyGuiControlSlider m_fieldOfViewSlider;
        private MyGuiControlLabel m_fieldOfViewDefaultLabel;
        private MyGuiControlCheckbox m_renderInterpolationCheckbox;

        private MyGraphicsSettings m_graphicsSettingsOld;
        private MyRenderDeviceSettings m_deviceSettingsOld;

        private MyGraphicsSettings m_graphicsSettingsNew;
        private MyRenderDeviceSettings m_deviceSettingsNew;

        private List<Vector2I> m_resolutionsForAdapter = new List<Vector2I>();

        bool m_waitingForConfirmation = false;

        bool m_doRevert = false;

        public MyGuiScreenOptionsVideoSpace()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, size: new Vector2(0.59f * 1.1f, 0.68544f * 1.11f))
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenOptionsVideo.ctor START");

            EnabledBackgroundFade = true;

            RecreateControls(true);

            MySandboxGame.Log.WriteLine("MyGuiScreenOptionsVideo.ctor END");
        }

        public override void RecreateControls(bool constructor)
        {
            if (!constructor)
                return;

            base.RecreateControls(constructor);

            AddCaption(MySpaceTexts.ScreenCaptionVideoOptions);

            Vector2 controlsOriginLeft = new Vector2(-m_size.Value.X / 2.0f + 0.05f, -m_size.Value.Y / 2.0f + 0.145f) + new Vector2(0.02f, 0f);
            Vector2 controlsOriginRight = new Vector2(-m_size.Value.X / 2.0f + 0.225f, -m_size.Value.Y / 2.0f + 0.145f) + new Vector2(0.043f, 0f);

            const float TEXT_SCALE = MyGuiConstants.DEFAULT_TEXT_SCALE * 0.85f;

            var labelVideoAdapter        = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.VideoAdapter));
            var labelVideoMode           = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.VideoMode));
            var labelWindowMode          = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.ScreenOptionsVideo_WindowMode));
            var labelVSync               = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.VerticalSync));
            var labelHwCursor            = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.HardwareCursor));
            var labelRenderQuality       = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.RenderQuality));
            var labelFoV                 = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.FieldOfView));
            m_fieldOfViewDefaultLabel    = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.DefaultFOV));
            var labelRenderInterpolation = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.RenderIterpolation));
            var labelEnableDamageEffects = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.EnableDamageEffects));

            m_videoAdapterCombobox   = new MyGuiControlCombobox(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipVideoOptionsVideoAdapter));
            m_resolutionCombobox     = new MyGuiControlCombobox(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipVideoOptionsVideoMode));
            m_windowModeCombobox     = new MyGuiControlCombobox();
            m_verticalSyncCheckbox   = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipVideoOptionsVerticalSync));
            m_hardwareCursorCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipVideoOptionsHardwareCursor));
            m_enableDamageEffectsCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipVideoOptionsEnableDamageEffects));

            m_renderQualityCombobox  = new MyGuiControlCombobox(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipVideoOptionsRenderQuality));
            m_fieldOfViewSlider      = new MyGuiControlSlider(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipVideoOptionsFieldOfView),
                labelText: new StringBuilder("{0}").ToString(),
                labelSpaceWidth: 0.035f,
                labelScale: TEXT_SCALE,
                labelFont: MyFontEnum.Blue,
                minValue: MathHelper.ToDegrees(MyConstants.FIELD_OF_VIEW_CONFIG_MIN),
                maxValue: MathHelper.ToDegrees(MyConstants.FIELD_OF_VIEW_CONFIG_MAX),
                defaultValue: MathHelper.ToDegrees(MyConstants.FIELD_OF_VIEW_CONFIG_DEFAULT));

            m_renderInterpolationCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipVideoOptionRenderIterpolation));

            m_unsupportedAspectRatioLabel = new MyGuiControlLabel(colorMask: MyGuiConstants.LABEL_TEXT_COLOR * 0.9f, textScale: TEXT_SCALE * 0.85f);
            m_recommendAspectRatioLabel = new MyGuiControlLabel(colorMask: MyGuiConstants.LABEL_TEXT_COLOR * 0.9f, textScale: TEXT_SCALE * 0.85f);


            var hintLineOffset = new Vector2(0f, m_unsupportedAspectRatioLabel.Size.Y);
            var hintOffset = new Vector2(0.01f, -0.35f * MyGuiConstants.CONTROLS_DELTA.Y);

            labelVideoAdapter.Position      = controlsOriginLeft; controlsOriginLeft += MyGuiConstants.CONTROLS_DELTA;
            m_videoAdapterCombobox.Position = controlsOriginRight; controlsOriginRight += MyGuiConstants.CONTROLS_DELTA;

            labelVideoMode.Position                = controlsOriginLeft; controlsOriginLeft += MyGuiConstants.CONTROLS_DELTA;
            m_resolutionCombobox.Position          = controlsOriginRight; controlsOriginRight += MyGuiConstants.CONTROLS_DELTA;
            m_unsupportedAspectRatioLabel.Position = controlsOriginRight + hintOffset;
            m_recommendAspectRatioLabel.Position   = controlsOriginRight + hintOffset + hintLineOffset;
            controlsOriginLeft                    += MyGuiConstants.CONTROLS_DELTA;
            controlsOriginRight                   += MyGuiConstants.CONTROLS_DELTA;

            labelWindowMode.Position               = controlsOriginLeft; controlsOriginLeft += MyGuiConstants.CONTROLS_DELTA;
            m_windowModeCombobox.Position          = controlsOriginRight; controlsOriginRight += MyGuiConstants.CONTROLS_DELTA;
            labelVSync.Position                    = controlsOriginLeft; controlsOriginLeft += MyGuiConstants.CONTROLS_DELTA;
            m_verticalSyncCheckbox.Position        = controlsOriginRight; controlsOriginRight += MyGuiConstants.CONTROLS_DELTA;
            labelHwCursor.Position                 = controlsOriginLeft; controlsOriginLeft += MyGuiConstants.CONTROLS_DELTA;
            m_hardwareCursorCheckbox.Position      = controlsOriginRight; controlsOriginRight += MyGuiConstants.CONTROLS_DELTA;
            labelRenderQuality.Position            = controlsOriginLeft; controlsOriginLeft += MyGuiConstants.CONTROLS_DELTA;
            m_renderQualityCombobox.Position       = controlsOriginRight; controlsOriginRight += MyGuiConstants.CONTROLS_DELTA;
            labelRenderInterpolation.Position      = controlsOriginLeft; controlsOriginLeft += MyGuiConstants.CONTROLS_DELTA;
            m_renderInterpolationCheckbox.Position = controlsOriginRight; controlsOriginRight += MyGuiConstants.CONTROLS_DELTA;
            labelEnableDamageEffects.Position      = controlsOriginLeft; controlsOriginLeft += MyGuiConstants.CONTROLS_DELTA;
            m_enableDamageEffectsCheckbox.Position = controlsOriginRight; controlsOriginRight += MyGuiConstants.CONTROLS_DELTA;

            labelFoV.Position                  = controlsOriginLeft; controlsOriginLeft += MyGuiConstants.CONTROLS_DELTA;
            m_fieldOfViewSlider.Position       = controlsOriginRight; controlsOriginRight += MyGuiConstants.CONTROLS_DELTA;
            m_fieldOfViewDefaultLabel.Position = controlsOriginRight + hintOffset;

            Controls.Add(labelVideoAdapter); Controls.Add(m_videoAdapterCombobox);
            Controls.Add(labelVideoMode); Controls.Add(m_resolutionCombobox);
            Controls.Add(m_unsupportedAspectRatioLabel);
            Controls.Add(m_recommendAspectRatioLabel);
            Controls.Add(labelWindowMode); Controls.Add(m_windowModeCombobox);
            Controls.Add(labelVSync); Controls.Add(m_verticalSyncCheckbox);
            Controls.Add(labelHwCursor); Controls.Add(m_hardwareCursorCheckbox);
            Controls.Add(labelRenderQuality); Controls.Add(m_renderQualityCombobox);
            Controls.Add(labelRenderInterpolation); Controls.Add(m_renderInterpolationCheckbox);
            Controls.Add(labelEnableDamageEffects); Controls.Add(m_enableDamageEffectsCheckbox);

            Controls.Add(labelFoV); Controls.Add(m_fieldOfViewSlider);
            Controls.Add(m_fieldOfViewDefaultLabel);

            foreach (var control in Controls)
                control.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;

            m_unsupportedAspectRatioLabel.Text = string.Format("* {0}", MyTexts.Get(MyCommonTexts.UnsupportedAspectRatio));
            AddAdaptersToComboBox();
            AddRenderQualitiesToComboBox();
            AddWindowModesToComboBox();
            m_fieldOfViewDefaultLabel.UpdateFormatParams(MathHelper.ToDegrees(MyConstants.FIELD_OF_VIEW_CONFIG_DEFAULT));

            m_videoAdapterCombobox.ItemSelected += OnVideoAdapterSelected;
            m_resolutionCombobox.ItemSelected += OnResolutionSelected;
            m_windowModeCombobox.ItemSelected += OnWindowModeSelected;

            //  Buttons APPLY and BACK
            Controls.Add(new MyGuiControlButton(
                position: new Vector2(-0.05f, 0.31f),
                size: MyGuiConstants.OK_BUTTON_SIZE,
                text: MyTexts.Get(MyCommonTexts.Ok),
                onButtonClick: OnOkClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER));

            Controls.Add(new MyGuiControlButton(
                position: new Vector2(0.05f, 0.31f),
                size: MyGuiConstants.OK_BUTTON_SIZE,
                text: MyTexts.Get(MyCommonTexts.Cancel),
                onButtonClick: OnCancelClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));

            //  Update controls with values from config file
            m_deviceSettingsOld = MyVideoSettingsManager.CurrentDeviceSettings;
            m_graphicsSettingsOld = MyVideoSettingsManager.CurrentGraphicsSettings;
            m_deviceSettingsNew = m_deviceSettingsOld;
            m_graphicsSettingsNew = m_graphicsSettingsOld;
            WriteSettingsToControls(m_deviceSettingsOld, m_graphicsSettingsOld);

            //  Update OLD settings
            ReadSettingsFromControls(ref m_deviceSettingsOld, ref m_graphicsSettingsOld);
            ReadSettingsFromControls(ref m_deviceSettingsNew, ref m_graphicsSettingsNew);

            CloseButtonEnabled = true;
        }

        private void AddRenderQualitiesToComboBox()
        {
            m_renderQualityCombobox.AddItem((int)VRageRender.MyRenderQualityEnum.NORMAL, MySpaceTexts.RenderQualityNormal);
            m_renderQualityCombobox.AddItem((int)VRageRender.MyRenderQualityEnum.HIGH, MySpaceTexts.RenderQualityHigh);
            m_renderQualityCombobox.AddItem((int)VRageRender.MyRenderQualityEnum.EXTREME, MySpaceTexts.RenderQualityExtreme);
        }

        private void AddWindowModesToComboBox()
        {
            m_windowModeCombobox.AddItem((int)MyWindowModeEnum.Window,           MyCommonTexts.ScreenOptionsVideo_WindowMode_Window);
            m_windowModeCombobox.AddItem((int)MyWindowModeEnum.FullscreenWindow, MyCommonTexts.ScreenOptionsVideo_WindowMode_FullscreenWindow);
            m_windowModeCombobox.AddItem((int)MyWindowModeEnum.Fullscreen,       MyCommonTexts.ScreenOptionsVideo_WindowMode_Fullscreen);
        }

        void OnVideoAdapterSelected()
        {
            int adapterIndex = (int)m_videoAdapterCombobox.GetSelectedKey();
            { // AddDisplayModesToComboBox
                m_resolutionCombobox.ClearItems();

                var duplicateFilter = new HashSet<Vector2I>(Vector2I.Comparer);
                foreach (var displayMode in MyVideoSettingsManager.Adapters[adapterIndex].SupportedDisplayModes)
                {
                    duplicateFilter.Add(new Vector2I(displayMode.Width, displayMode.Height));
                }

                m_resolutionsForAdapter.Clear();
                m_resolutionsForAdapter.AddHashset(duplicateFilter);
                m_resolutionsForAdapter.Sort((a, b) =>
                { // Sort by width, then height.
                    if (a.X != b.X)
                        return a.X.CompareTo(b.X);
                    return a.Y.CompareTo(b.Y);
                });

                // Also show any extra display modes we have (there shouldn't be any on official builds).
                foreach (var mode in MyVideoSettingsManager.DebugDisplayModes)
                {
                    m_resolutionsForAdapter.Add(new Vector2I(mode.Width, mode.Height));
                }

                var displayAspectRatio = MyVideoSettingsManager.GetRecommendedAspectRatio(adapterIndex);
                int resolutionToSelect = 0;
                int counter = 0;
                for (int i = 0; i < m_resolutionsForAdapter.Count; ++i)
                {
                    var resolution = m_resolutionsForAdapter[i];
                    float aspectRatio = (float)resolution.X / (float)resolution.Y;
                    var aspectId = MyVideoSettingsManager.GetClosestAspectRatio(aspectRatio);
                    var aspectDetails = MyVideoSettingsManager.GetAspectRatio(aspectId);
                    var aspectRatioText = aspectDetails.TextShort;
                    var starsMark = aspectDetails.IsSupported ? (aspectId == displayAspectRatio.AspectRatioEnum) ? " ***" // recommended
                                                                                                                 : "" // normal
                                                              : " *"; // unsupported
                    if (resolution.X == m_deviceSettingsOld.BackBufferWidth &&
                        resolution.Y == m_deviceSettingsOld.BackBufferHeight)
                        resolutionToSelect = counter;

                    m_resolutionCombobox.AddItem(counter++, new StringBuilder(
                        string.Format("{0} x {1} - {2}{3}", resolution.X, resolution.Y, aspectRatioText, starsMark)));
                }

                m_resolutionCombobox.SelectItemByKey(resolutionToSelect);
            }

            { // UpdateRecommendecAspectRatioLabel
                MyAspectRatio recommendedAspectRatio = MyVideoSettingsManager.GetRecommendedAspectRatio(adapterIndex);
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(MyTexts.GetString(MyCommonTexts.RecommendedAspectRatio), recommendedAspectRatio.TextShort);
                m_recommendAspectRatioLabel.Text = string.Format("*** {0}", sb);
            }
        }

        void OnResolutionSelected()
        {
            var resolution = m_resolutionsForAdapter[(int)m_resolutionCombobox.GetSelectedKey()];
            float min, max;
            MyVideoSettingsManager.GetFovBounds((float)resolution.X / (float)resolution.Y, out min, out max);
            SetFoVSliderBounds(min, max);
        }

        private void OnWindowModeSelected()
        {
            m_resolutionCombobox.Enabled = (MyWindowModeEnum)m_windowModeCombobox.GetSelectedKey() != MyWindowModeEnum.FullscreenWindow;
        }

        private void SetFoVSliderBounds(float minRadians, float maxRadians)
        {
            m_fieldOfViewSlider.SetBounds(MathHelper.ToDegrees(minRadians), MathHelper.ToDegrees(maxRadians));
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenOptionsVideo";
        }

        private bool ReadSettingsFromControls(ref MyRenderDeviceSettings deviceSettings, ref MyGraphicsSettings graphicsSettings)
        {
            bool changed;

            changed = deviceSettings.AdapterOrdinal != (int)m_videoAdapterCombobox.GetSelectedKey();
            deviceSettings.AdapterOrdinal = (int)m_videoAdapterCombobox.GetSelectedKey();

            var resolution = m_resolutionsForAdapter[(int)m_resolutionCombobox.GetSelectedKey()];

            changed = changed || deviceSettings.BackBufferWidth != resolution.X;
            deviceSettings.BackBufferWidth = resolution.X;

            changed = changed || deviceSettings.BackBufferHeight != resolution.Y;
            deviceSettings.BackBufferHeight = resolution.Y;

            int refreshRate = 0;
            foreach (var displayMode in MyVideoSettingsManager.Adapters[deviceSettings.AdapterOrdinal].SupportedDisplayModes)
            { // Pick the highest refresh rate available (although it might be better to add combobox for refresh rates as well)
                if (displayMode.Width == resolution.X &&
                    displayMode.Height == resolution.Y &&
                    refreshRate < displayMode.RefreshRate)
                    refreshRate = displayMode.RefreshRate;
            }

            changed = changed || deviceSettings.RefreshRate != refreshRate;
            deviceSettings.RefreshRate = refreshRate;

            changed = changed || deviceSettings.VSync != m_verticalSyncCheckbox.IsChecked;
            deviceSettings.VSync = m_verticalSyncCheckbox.IsChecked;

            changed = changed || deviceSettings.WindowMode != (MyWindowModeEnum)m_windowModeCombobox.GetSelectedKey();
            deviceSettings.WindowMode = (MyWindowModeEnum)m_windowModeCombobox.GetSelectedKey();

            var fov = MathHelper.ToRadians(m_fieldOfViewSlider.Value);
            changed = changed || graphicsSettings.FieldOfView != fov;
            graphicsSettings.FieldOfView = fov;

            changed = changed || graphicsSettings.HardwareCursor != m_hardwareCursorCheckbox.IsChecked;
            graphicsSettings.HardwareCursor = m_hardwareCursorCheckbox.IsChecked;

            changed = changed || graphicsSettings.EnableDamageEffects != m_enableDamageEffectsCheckbox.IsChecked;
            graphicsSettings.EnableDamageEffects = m_enableDamageEffectsCheckbox.IsChecked;

            changed = changed || graphicsSettings.Render.InterpolationEnabled != m_renderInterpolationCheckbox.IsChecked;
            graphicsSettings.Render.InterpolationEnabled = m_renderInterpolationCheckbox.IsChecked;

            changed = changed || graphicsSettings.Render.Dx9Quality != (MyRenderQualityEnum)m_renderQualityCombobox.GetSelectedKey();
            graphicsSettings.Render.Dx9Quality = (MyRenderQualityEnum)m_renderQualityCombobox.GetSelectedKey();

            return changed;
        }

        void WriteSettingsToControls(MyRenderDeviceSettings deviceSettings, MyGraphicsSettings graphicsSettings)
        {
            m_videoAdapterCombobox.SelectItemByKey(deviceSettings.AdapterOrdinal);
            m_resolutionCombobox.SelectItemByKey(m_resolutionsForAdapter.FindIndex(
                (res) => res.X == deviceSettings.BackBufferWidth && res.Y == deviceSettings.BackBufferHeight));
            m_windowModeCombobox.SelectItemByKey((int)deviceSettings.WindowMode);
            m_verticalSyncCheckbox.IsChecked = deviceSettings.VSync;
            m_hardwareCursorCheckbox.IsChecked = graphicsSettings.HardwareCursor;
            m_enableDamageEffectsCheckbox.IsChecked = graphicsSettings.EnableDamageEffects;

            m_renderQualityCombobox.SelectItemByKey((int)graphicsSettings.Render.Dx9Quality);
            if (m_renderQualityCombobox.GetSelectedKey() == -1)
                m_renderQualityCombobox.SelectItemByIndex(0);

            m_fieldOfViewSlider.Value = MathHelper.ToDegrees(graphicsSettings.FieldOfView);

            m_renderInterpolationCheckbox.IsChecked = graphicsSettings.Render.InterpolationEnabled;
        }

        public void OnCancelClick(MyGuiControlButton sender)
        {
            //  Just close the screen, ignore any change
            CloseScreen();
        }

        public void OnOkClick(MyGuiControlButton sender)
        {
            //  Update NEW settings
            bool somethingChanged = ReadSettingsFromControls(ref m_deviceSettingsNew, ref m_graphicsSettingsNew);

            //  Change video mode to new one
            if (somethingChanged)
            {
                OnVideoModeChangedAndConfirm(MyVideoSettingsManager.ApplyVideoSettings(m_deviceSettingsNew, m_graphicsSettingsNew));
            }
            else
            {
                CloseScreen();
            }
        }

        private void OnVideoModeChangedAndConfirm(MyVideoSettingsManager.ChangeResult result)
        {
            switch (result)
            {
                case MyVideoSettingsManager.ChangeResult.Success:
                    m_waitingForConfirmation = true;
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        buttonType: MyMessageBoxButtonsType.YES_NO_TIMEOUT,
                        messageText: MyTexts.Get(MyCommonTexts.DoYouWantToKeepTheseSettingsXSecondsRemaining),
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                        callback: OnMessageBoxCallback,
                        timeoutInMiliseconds: MyGuiConstants.VIDEO_OPTIONS_CONFIRMATION_TIMEOUT_IN_MILISECONDS));
                    break;

                case MyVideoSettingsManager.ChangeResult.NothingChanged:
                    break;

                case MyVideoSettingsManager.ChangeResult.Failed:
                    m_doRevert = true;
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        messageText: MyTexts.Get(MyCommonTexts.SorryButSelectedSettingsAreNotSupportedByYourHardware),
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError)));
                    break;
            }
        }

        private void OnVideoModeChanged(MyVideoSettingsManager.ChangeResult result)
        {
            WriteSettingsToControls(m_deviceSettingsOld, m_graphicsSettingsOld);
            ReadSettingsFromControls(ref m_deviceSettingsNew, ref m_graphicsSettingsNew);
        }

        public void OnMessageBoxCallback(MyGuiScreenMessageBox.ResultEnum callbackReturn)
        {
            if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
            {
                //  Save current video mode settings
                MyVideoSettingsManager.SaveCurrentSettings();

                //  These are now OLD settings
                ReadSettingsFromControls(ref m_deviceSettingsOld, ref m_graphicsSettingsOld);

                this.CloseScreenNow();
            }
            else
            {
                m_doRevert = true;
            }

            m_waitingForConfirmation = false;
        }

        public override bool CloseScreen()
        {
            bool ret = base.CloseScreen();

            //  If the screen was closed for whatever reason during we waited for 15secs acknowledgement of changes, we need to revert them (because YES wasn't pressed)
            if ((ret == true) && (m_waitingForConfirmation == true))
            {
                //RevertChanges();
            }

            return ret;
        }

        void AddAdaptersToComboBox()
        {
            int counter = 0;
            foreach (var adapter in MyVideoSettingsManager.Adapters)
            {
                m_videoAdapterCombobox.AddItem(counter++, new StringBuilder(adapter.Name));
            }
        }

        public override bool Draw()
        {
            if (!base.Draw())
                return false;

            if (m_doRevert)
            {
                //  Revert changes - setting new video resolution must be done from Draw call, because when called
                //  from Update while game isn't active (alt-tabed or minimized) it will fail on weird XNA exceptions
                OnVideoModeChanged(MyVideoSettingsManager.ApplyVideoSettings(m_deviceSettingsOld, m_graphicsSettingsOld));
                m_doRevert = false;
            }

            return true;
        }
    }
}
