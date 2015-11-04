﻿using Sandbox.Common;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenOptionsDisplay : MyGuiScreenBase
    {
        private MyGuiControlLabel m_labelRecommendAspectRatio;
        private MyGuiControlLabel m_labelUnsupportedAspectRatio;
        private MyGuiControlCombobox m_comboVideoAdapter;
        private MyGuiControlCombobox m_comboResolution;
        private MyGuiControlCombobox m_comboWindowMode;
        private MyGuiControlCheckbox m_checkboxVSync;

        private MyRenderDeviceSettings m_settingsOld;
        private MyRenderDeviceSettings m_settingsNew;

        private List<Vector2I> m_resolutions = new List<Vector2I>();

        bool m_waitingForConfirmation = false;

        bool m_doRevert = false;

        public MyGuiScreenOptionsDisplay()
            : base(position: new Vector2(0.5f, 0.5f), backgroundColor: Vector4.One)
        {
            EnabledBackgroundFade = true;
            Size = new Vector2(1000f, 650f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            LoadResolutions();
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            if (!constructor)
                return;

            base.RecreateControls(constructor);

            AddCaption("Display settings");

            var tmp = new Vector2(0.268f, 0.145f) * MyGuiConstants.GUI_OPTIMAL_SIZE;

            var topLeft = m_size.Value * -0.5f;
            var topRight = m_size.Value * new Vector2(0.5f, -0.5f);

            Vector2 comboboxSize = new Vector2(600f, 0f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            Vector2 controlsOriginLeft = topLeft + new Vector2(75f, 125f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            Vector2 controlsOriginRight = topRight + new Vector2(-650f, 125f) / MyGuiConstants.GUI_OPTIMAL_SIZE;

            const float TEXT_SCALE = Sandbox.Graphics.GUI.MyGuiConstants.DEFAULT_TEXT_SCALE * 0.85f;

            var labelVideoAdapter = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.VideoAdapter));
            var labelResolution   = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.VideoMode));
            var labelWindowMode   = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.ScreenOptionsVideo_WindowMode));
            var labelVSync        = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.VerticalSync));

            m_comboVideoAdapter = new MyGuiControlCombobox(size: comboboxSize, toolTip: MyTexts.GetString(MySpaceTexts.ToolTipVideoOptionsVideoAdapter));
            m_comboResolution   = new MyGuiControlCombobox(size: comboboxSize, toolTip: MyTexts.GetString(MySpaceTexts.ToolTipVideoOptionsVideoMode));
            m_comboWindowMode   = new MyGuiControlCombobox(size: comboboxSize);
            m_checkboxVSync     = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipVideoOptionsVerticalSync));

            m_labelUnsupportedAspectRatio = new MyGuiControlLabel(colorMask: MyGuiConstants.LABEL_TEXT_COLOR * 0.9f, textScale: TEXT_SCALE * 0.85f);
            m_labelRecommendAspectRatio   = new MyGuiControlLabel(colorMask: MyGuiConstants.LABEL_TEXT_COLOR * 0.9f, textScale: TEXT_SCALE * 0.85f);

            var hintLineOffset = new Vector2(0f, m_labelUnsupportedAspectRatio.Size.Y);
            var hintOffset     = new Vector2(0.01f, -0.35f * MyGuiConstants.CONTROLS_DELTA.Y);

            labelVideoAdapter.Position   = controlsOriginLeft; controlsOriginLeft += MyGuiConstants.CONTROLS_DELTA;
            m_comboVideoAdapter.Position = controlsOriginRight; controlsOriginRight += MyGuiConstants.CONTROLS_DELTA;

            labelResolution.Position               = controlsOriginLeft; controlsOriginLeft += MyGuiConstants.CONTROLS_DELTA;
            m_comboResolution.Position             = controlsOriginRight; controlsOriginRight += MyGuiConstants.CONTROLS_DELTA;
            m_labelUnsupportedAspectRatio.Position = controlsOriginRight + hintOffset;
            m_labelRecommendAspectRatio.Position   = controlsOriginRight + hintOffset + hintLineOffset;
            controlsOriginLeft                    += MyGuiConstants.CONTROLS_DELTA;
            controlsOriginRight                   += MyGuiConstants.CONTROLS_DELTA;

            labelWindowMode.Position   = controlsOriginLeft; controlsOriginLeft += MyGuiConstants.CONTROLS_DELTA;
            m_comboWindowMode.Position = controlsOriginRight; controlsOriginRight += MyGuiConstants.CONTROLS_DELTA;
            labelVSync.Position        = controlsOriginLeft; controlsOriginLeft += MyGuiConstants.CONTROLS_DELTA;
            m_checkboxVSync.Position   = controlsOriginRight; controlsOriginRight += MyGuiConstants.CONTROLS_DELTA;

            Controls.Add(labelVideoAdapter); Controls.Add(m_comboVideoAdapter);
            Controls.Add(labelResolution); Controls.Add(m_comboResolution);
            Controls.Add(m_labelUnsupportedAspectRatio);
            Controls.Add(m_labelRecommendAspectRatio);
            Controls.Add(labelWindowMode); Controls.Add(m_comboWindowMode);
            Controls.Add(labelVSync); Controls.Add(m_checkboxVSync);

            foreach (var control in Controls)
                control.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;

            m_labelUnsupportedAspectRatio.Text = string.Format("* {0}", MyTexts.Get(MySpaceTexts.UnsupportedAspectRatio));

            { // AddAdaptersToComboBox
                int counter = 0;
                foreach (var adapter in MyVideoSettingsManager.Adapters)
                {
                    m_comboVideoAdapter.AddItem(counter++, new StringBuilder(adapter.Name));
                }
            }

            // These options show up if there are no supported display modes
            m_comboWindowMode.AddItem((int)MyWindowModeEnum.Window, MySpaceTexts.ScreenOptionsVideo_WindowMode_Window);
            m_comboWindowMode.AddItem((int)MyWindowModeEnum.FullscreenWindow, MySpaceTexts.ScreenOptionsVideo_WindowMode_FullscreenWindow);

            m_comboVideoAdapter.ItemSelected += ComboVideoAdapter_ItemSelected;
            m_comboResolution.ItemSelected += ComboResolution_ItemSelected;

            //  Buttons Ok and Cancel
            Controls.Add(new MyGuiControlButton(
                position: Size.Value * new Vector2(-0.5f, 0.5f) + new Vector2(100f, -75f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                size: MyGuiConstants.OK_BUTTON_SIZE,
                text: MyTexts.Get(MySpaceTexts.Ok),
                onButtonClick: OnOkClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM));

            Controls.Add(new MyGuiControlButton(
                position: Size.Value * new Vector2(0.5f, 0.5f) + new Vector2(-100f, -75f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                size: MyGuiConstants.OK_BUTTON_SIZE,
                text: MyTexts.Get(MySpaceTexts.Cancel),
                onButtonClick: OnCancelClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM));

            //  Update controls with values from config file
            m_settingsOld = MyVideoSettingsManager.CurrentDeviceSettings;
            m_settingsNew = m_settingsOld;
            WriteSettingsToControls(m_settingsOld);

            //  Update OLD settings
            ReadSettingsFromControls(ref m_settingsOld);
            ReadSettingsFromControls(ref m_settingsNew);

            CloseButtonEnabled = true;
            CloseButtonOffset = new Vector2(-50f, 50f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
        }

        private void LoadResolutions()
        {
            var duplicateFilter = new HashSet<Vector2I>(Vector2I.Comparer);
            foreach (var adapter in MyVideoSettingsManager.Adapters)
            {
                foreach (var displayMode in adapter.SupportedDisplayModes)
                {
                    duplicateFilter.Add(new Vector2I(displayMode.Width, displayMode.Height));
                }
            }

            m_resolutions.Clear();
            m_resolutions.AddHashset(duplicateFilter);
            m_resolutions.Sort((a, b) =>
            { // Sort by width, then height.
                if (a.X != b.X)
                    return a.X.CompareTo(b.X);
                return a.Y.CompareTo(b.Y);
            });

            // Also show any extra display modes we have (there shouldn't be any on official builds).
            foreach (var mode in MyVideoSettingsManager.DebugDisplayModes)
            {
                m_resolutions.Add(new Vector2I(mode.Width, mode.Height));
            }
        }

        private void ComboVideoAdapter_ItemSelected()
        {
            int adapterIndex = (int)m_comboVideoAdapter.GetSelectedKey();
            { // AddDisplayModesToComboBox
                m_comboResolution.ClearItems();

                var displayAspectRatio = MyVideoSettingsManager.GetRecommendedAspectRatio(adapterIndex);
                int resolutionToSelect = 0;
                int counter = 0;
                for (int i = 0; i < m_resolutions.Count; ++i)
                {
                    var resolution = m_resolutions[i];
                    float aspectRatio = (float)resolution.X / (float)resolution.Y;
                    var aspectId = MyVideoSettingsManager.GetClosestAspectRatio(aspectRatio);
                    var aspectDetails = MyVideoSettingsManager.GetAspectRatio(aspectId);
                    var aspectRatioText = aspectDetails.TextShort;
                    var starsMark = aspectDetails.IsSupported ? (aspectId == displayAspectRatio.AspectRatioEnum) ? " ***" // recommended
                                                                                                                 : "" // normal
                                                              : " *"; // unsupported by UI
                    if (resolution.X == m_settingsOld.BackBufferWidth &&
                        resolution.Y == m_settingsOld.BackBufferHeight)
                        resolutionToSelect = counter;

                    m_comboResolution.AddItem(counter++, new StringBuilder(
                        string.Format("{0} x {1} - {2}{3}", resolution.X, resolution.Y, aspectRatioText, starsMark)));
                }

                m_comboResolution.SelectItemByKey(resolutionToSelect);
            }

            { // UpdateRecommendecAspectRatioLabel
                MyAspectRatio recommendedAspectRatio = MyVideoSettingsManager.GetRecommendedAspectRatio(adapterIndex);
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(MyTexts.GetString(MySpaceTexts.RecommendedAspectRatio), recommendedAspectRatio.TextShort);
                m_labelRecommendAspectRatio.Text = string.Format("*** {0}", sb);
            }
        }

        private void ComboResolution_ItemSelected()
        {
            int adapterIndex = (int)m_comboVideoAdapter.GetSelectedKey();
            var selectedResolution = m_resolutions[(int)m_comboResolution.GetSelectedKey()];
            bool fullscreenSupported = false;
            foreach (var displayMode in MyVideoSettingsManager.Adapters[adapterIndex].SupportedDisplayModes)
            {
                if (displayMode.Width == selectedResolution.X &&
                    displayMode.Height == selectedResolution.Y)
                {
                    fullscreenSupported = true;
                    break;
                }
            }

            var selectedWindowMode = (MyWindowModeEnum)m_comboWindowMode.GetSelectedKey();
            m_comboWindowMode.ClearItems();
            m_comboWindowMode.AddItem((int)MyWindowModeEnum.Window, MySpaceTexts.ScreenOptionsVideo_WindowMode_Window);
            m_comboWindowMode.AddItem((int)MyWindowModeEnum.FullscreenWindow, MySpaceTexts.ScreenOptionsVideo_WindowMode_FullscreenWindow);
            if (fullscreenSupported)
                m_comboWindowMode.AddItem((int)MyWindowModeEnum.Fullscreen, MySpaceTexts.ScreenOptionsVideo_WindowMode_Fullscreen);

            if (!fullscreenSupported && selectedWindowMode == MyWindowModeEnum.Fullscreen)
            {
                m_comboWindowMode.SelectItemByKey((long)MyWindowModeEnum.FullscreenWindow);
            }
            else
            {
                m_comboWindowMode.SelectItemByKey((long)selectedWindowMode);
            }
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenOptionsVideo";
        }

        private bool ReadSettingsFromControls(ref MyRenderDeviceSettings deviceSettings)
        {
            bool changed;
            MyRenderDeviceSettings read = new MyRenderDeviceSettings();

            var selectedResolution = (int)m_comboResolution.GetSelectedKey();
            if ((uint)selectedResolution < (uint)m_resolutions.Count)
            {
                var resolution = m_resolutions[selectedResolution];
                read.BackBufferWidth = resolution.X;
                read.BackBufferHeight = resolution.Y;
                read.WindowMode = (MyWindowModeEnum)m_comboWindowMode.GetSelectedKey();
                read.AdapterOrdinal = (int)m_comboVideoAdapter.GetSelectedKey();
                read.VSync = m_checkboxVSync.IsChecked;
                read.RefreshRate = 0;
                foreach (var displayMode in MyVideoSettingsManager.Adapters[deviceSettings.AdapterOrdinal].SupportedDisplayModes)
                { // Pick the highest refresh rate available (although it might be better to add combobox for refresh rates as well)
                    if (displayMode.Width == read.BackBufferWidth &&
                        displayMode.Height == read.BackBufferHeight &&
                        read.RefreshRate < displayMode.RefreshRate)
                    {
                        read.RefreshRate = displayMode.RefreshRate;
                    }
                }
                changed = !read.Equals(ref deviceSettings);
                deviceSettings = read;
            }
            else
            { // unsupported display mode selected, so pretend nothing was changed
                changed = false;
            }

            return changed;
        }

        private void WriteSettingsToControls(MyRenderDeviceSettings deviceSettings)
        {
            m_comboVideoAdapter.SelectItemByKey(deviceSettings.AdapterOrdinal);
            m_comboResolution.SelectItemByKey(m_resolutions.FindIndex(
                (res) => res.X == deviceSettings.BackBufferWidth && res.Y == deviceSettings.BackBufferHeight));
            m_comboWindowMode.SelectItemByKey((int)deviceSettings.WindowMode);
            m_checkboxVSync.IsChecked = deviceSettings.VSync;
        }

        public void OnCancelClick(MyGuiControlButton sender)
        {
            //  Just close the screen, ignore any change
            CloseScreen();
        }

        public void OnOkClick(MyGuiControlButton sender)
        {
            //  Update NEW settings
            bool somethingChanged = ReadSettingsFromControls(ref m_settingsNew);

            //  Change video mode to new one
            if (somethingChanged)
            {
                OnVideoModeChangedAndConfirm(MyVideoSettingsManager.Apply(m_settingsNew));
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
                        messageText: MyTexts.Get(MySpaceTexts.DoYouWantToKeepTheseSettingsXSecondsRemaining),
                        messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionPleaseConfirm),
                        callback: OnMessageBoxCallback,
                        timeoutInMiliseconds: MyGuiConstants.VIDEO_OPTIONS_CONFIRMATION_TIMEOUT_IN_MILISECONDS));
                    break;

                case MyVideoSettingsManager.ChangeResult.NothingChanged:
                    break;

                case MyVideoSettingsManager.ChangeResult.Failed:
                    m_doRevert = true;
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        messageText: MyTexts.Get(MySpaceTexts.SorryButSelectedSettingsAreNotSupportedByYourHardware),
                        messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError)));
                    break;
            }
        }

        private void OnVideoModeChanged(MyVideoSettingsManager.ChangeResult result)
        {
            WriteSettingsToControls(m_settingsOld);
            ReadSettingsFromControls(ref m_settingsNew);
        }

        public void OnMessageBoxCallback(MyGuiScreenMessageBox.ResultEnum callbackReturn)
        {
            if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
            {
                //  Save current video mode settings
                MyVideoSettingsManager.SaveCurrentSettings();

                //  These are now OLD settings
                ReadSettingsFromControls(ref m_settingsOld);

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

        public override bool Draw()
        {
            if (!base.Draw())
                return false;

            if (m_doRevert)
            {
                //  Revert changes - setting new video resolution must be done from Draw call, because when called
                //  from Update while game isn't active (alt-tabed or minimized) it will fail on weird XNA exceptions
                OnVideoModeChanged(MyVideoSettingsManager.Apply(m_settingsOld));
                m_doRevert = false;
            }

            return true;
        }

    }
}
