using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Game.Entities;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using System.Text;
using VRage;
using VRage;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenOptionsGame : MyGuiScreenBase
    {
        struct OptionsGameSettings
        {
            public MyLanguagesEnum Language;
            public MyCubeBuilder.BuildingModeEnum BuildingMode;
            public bool ControlHints;
            public bool RotationHints;
            public bool ShowCrosshair;
            public bool DisableHeadbob;
            public bool CompressSaveGames;
            public bool ShowPlayerNamesOnHud;
            public float UITransparency;
            public float UIBkTransparency;
        }

        MyGuiControlCombobox m_languageCombobox;
        MyGuiControlCombobox m_buildingModeCombobox;
        MyGuiControlCheckbox m_controlHintsCheckbox;
        MyGuiControlCheckbox m_rotationHintsCheckbox;
        MyGuiControlCheckbox m_crosshairCheckbox;
        MyGuiControlCheckbox m_disableHeadbobCheckbox;
        MyGuiControlCheckbox m_compressSavesCheckbox;
        MyGuiControlCheckbox m_showPlayerNamesCheckbox;
        MyGuiControlSlider m_UITransparencySlider;
        MyGuiControlSlider m_UIBkTransparencySlider;
        private MyGuiControlButton m_localizationWebButton;
        private MyGuiControlLabel m_localizationWarningLabel;
        private OptionsGameSettings m_settings = new OptionsGameSettings();

        public MyGuiScreenOptionsGame()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, size: new Vector2(0.51f, 0.9f), backgroundTransition: MySandboxGame.Config.UIBkTransparency, guiTransition: MySandboxGame.Config.UITransparency)
        {
            EnabledBackgroundFade = true;

            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption(MySpaceTexts.ScreenCaptionGameOptions);

            var leftAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
            var rightAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
            Vector2 controlsOriginLeft = new Vector2(-m_size.Value.X / 2.0f + 0.025f, -m_size.Value.Y / 2.0f + 0.125f);
            Vector2 controlsOriginRight = new Vector2(m_size.Value.X / 2.0f - 0.025f, -m_size.Value.Y / 2.0f + 0.125f);
            Vector2 controlsDelta = new Vector2(0, 0.0525f);
            float rowIndex = 0;

            //  Language
            var languageLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.Language))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign,
            };
            m_languageCombobox = new MyGuiControlCombobox()
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };

            foreach (var languageId in MyLanguage.SupportedLanguages)
            {
                var description = MyTexts.Languages[languageId];
                var name = description.Name;
                if (description.IsCommunityLocalized)
                    name += " *";
                m_languageCombobox.AddItem(languageId, name);
            }
            m_languageCombobox.CustomSortItems((a, b) => a.Key.CompareTo(b.Key));
            m_languageCombobox.ItemSelected += m_languageCombobox_ItemSelected;

            rowIndex += 0.65f;
            m_localizationWebButton = new MyGuiControlButton(
               position: controlsOriginRight + rowIndex * controlsDelta,
               text: MyTexts.Get(MySpaceTexts.ScreenOptionsGame_MoreInfo),
               textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.85f * 0.85f,
               onButtonClick: LocalizationWebButtonClicked,
               implementedFeature: true,
               originAlign: rightAlign);
            m_localizationWebButton.VisualStyle = MyGuiControlButtonStyleEnum.ClickableText;
            var tmp = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.ScreenOptionsGame_MoreInfo), textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.85f * 0.85f);
            m_localizationWarningLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.ScreenOptionsGame_LocalizationWarning), textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.85f * 0.85f)
            {
                Position = controlsOriginRight + rowIndex * controlsDelta - new Vector2(tmp.Size.X + 0.005f, 0),
                OriginAlign = rightAlign,
            };
            rowIndex += 0.8f;

            var buildingModeLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.ScreenOptionsGame_BuildingMode))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign,
            };
            m_buildingModeCombobox = new MyGuiControlCombobox()
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_buildingModeCombobox.AddItem((int)MyCubeBuilder.BuildingModeEnum.SingleBlock, MySpaceTexts.ScreenOptionsGame_SingleBlock);
            m_buildingModeCombobox.AddItem((int)MyCubeBuilder.BuildingModeEnum.Line, MySpaceTexts.ScreenOptionsGame_Line);
            m_buildingModeCombobox.AddItem((int)MyCubeBuilder.BuildingModeEnum.Plane, MySpaceTexts.ScreenOptionsGame_Plane);
            m_buildingModeCombobox.ItemSelected += m_buildingModeCombobox_ItemSelected;

            //  Notifications
            rowIndex++;
            var controlHintsLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.ShowControlsHints))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            m_controlHintsCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipGameOptionsShowControlsHints))
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_controlHintsCheckbox.IsCheckedChanged += checkboxChanged;

            //  Rotation gizmo
            rowIndex++;
            var rotationHintsLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.ShowRotationHints))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            m_rotationHintsCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipGameOptionsShowRotationHints))
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_rotationHintsCheckbox.IsCheckedChanged += checkboxChanged;

            //  Show crosshair?
            rowIndex++;
            var crosshairLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.ShowCrosshair))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            m_crosshairCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipGameOptionsShowCrosshair))
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_crosshairCheckbox.IsCheckedChanged += checkboxChanged;

            //  Headbob
            rowIndex++;
            var headbobLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.Headbob))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            m_disableHeadbobCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MySpaceTexts.Headbob))
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_disableHeadbobCheckbox.IsCheckedChanged += checkboxChanged;

            //  Compress save games checkbox
            rowIndex++;
            var compressSavesLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.CompressSaveGames))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            m_compressSavesCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipGameOptionsCompressSaveGames))
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_compressSavesCheckbox.IsCheckedChanged += checkboxChanged;

            rowIndex++;
            var showPlayerNamesOnHudLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.ScreenOptionsGame_ShowPlayerNames))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            m_showPlayerNamesCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipGameOptionsShowPlayerNames))
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_showPlayerNamesCheckbox.IsCheckedChanged += checkboxChanged;

            rowIndex++;
            var UITransparencyLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.ScreenOptionsGame_UITransparency))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            rowIndex++;
            m_UITransparencySlider = new MyGuiControlSlider(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipGameOptionsUITransparency), minValue: 0.1f, maxValue: 1.0f, defaultValue: 1.0f)
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_UITransparencySlider.ValueChanged += sliderChanged;

            rowIndex++;
            var UIBkTransparencyLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.ScreenOptionsGame_UIBkTransparency))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            rowIndex++;
            m_UIBkTransparencySlider = new MyGuiControlSlider(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipGameOptionsUIBkTransparency), minValue: 0, maxValue: 1.0f, defaultValue: 1.0f)
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_UIBkTransparencySlider.ValueChanged += sliderChanged;

            //  Buttons OK and CANCEL
            var buttonOk = new MyGuiControlButton(text: MyTexts.Get(MySpaceTexts.Ok), onButtonClick: OnOkClick);
            var buttonCancel = new MyGuiControlButton(text: MyTexts.Get(MySpaceTexts.Cancel), onButtonClick: OnCancelClick);
            float buttonX = 0.01f;
            float buttonY = m_size.Value.Y / 2.0f - (buttonOk.Size.Y + 0.03f);
            buttonOk.Position = new Vector2(-buttonX, buttonY);
            buttonOk.OriginAlign = rightAlign;
            buttonCancel.Position = new Vector2(buttonX, buttonY);
            buttonCancel.OriginAlign = leftAlign;

            Controls.Add(languageLabel);
            Controls.Add(m_languageCombobox);
            Controls.Add(m_localizationWebButton);
            Controls.Add(m_localizationWarningLabel);
            Controls.Add(buildingModeLabel);
            Controls.Add(m_buildingModeCombobox);
            Controls.Add(controlHintsLabel);
            Controls.Add(rotationHintsLabel);
            Controls.Add(m_controlHintsCheckbox);
            Controls.Add(m_rotationHintsCheckbox);
            Controls.Add(crosshairLabel);
            Controls.Add(m_crosshairCheckbox);
            Controls.Add(headbobLabel);
            Controls.Add(m_disableHeadbobCheckbox);
            Controls.Add(compressSavesLabel);
            Controls.Add(m_compressSavesCheckbox);
            Controls.Add(showPlayerNamesOnHudLabel);
            Controls.Add(m_showPlayerNamesCheckbox);
            Controls.Add(UITransparencyLabel);
            Controls.Add(m_UITransparencySlider);
            Controls.Add(UIBkTransparencyLabel);
            Controls.Add(m_UIBkTransparencySlider);
            Controls.Add(buttonOk);
            Controls.Add(buttonCancel);

            //  Update controls with values from config file
            UpdateControls(constructor);

            CloseButtonEnabled = true;
        }


        private void checkboxChanged(MyGuiControlCheckbox obj)
        {
            if (obj == m_controlHintsCheckbox)
                m_settings.ControlHints = obj.IsChecked;
            else if (obj == m_rotationHintsCheckbox)
                m_settings.RotationHints = obj.IsChecked;
            else if (obj == m_crosshairCheckbox)
                m_settings.ShowCrosshair = obj.IsChecked;
            else if (obj == m_disableHeadbobCheckbox)
                m_settings.DisableHeadbob = obj.IsChecked;
            else if (obj == m_compressSavesCheckbox)
                m_settings.CompressSaveGames = obj.IsChecked;
            else if (obj == m_showPlayerNamesCheckbox)
                m_settings.ShowPlayerNamesOnHud = obj.IsChecked;
        }

        private void sliderChanged(MyGuiControlSlider obj)
        {
            if (obj == m_UITransparencySlider)
            {
                m_settings.UITransparency = obj.Value;
                m_guiTransition = obj.Value;
            }
            else if (obj == m_UIBkTransparencySlider)
            {
                m_settings.UIBkTransparency = obj.Value;
                m_backgroundTransition = obj.Value;
            }
        }

        void m_buildingModeCombobox_ItemSelected()
        {
            m_settings.BuildingMode = (MyCubeBuilder.BuildingModeEnum)m_buildingModeCombobox.GetSelectedKey();
        }

        void LocalizationWebButtonClicked(MyGuiControlButton obj)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.YES_NO,
                messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionPleaseConfirm),
                messageText: new StringBuilder().AppendFormat(MyTexts.GetString(MySpaceTexts.MessageBoxTextOpenBrowser), MyPerGameSettings.GameWebUrl),
                callback: delegate(MyGuiScreenMessageBox.ResultEnum retval)
                {
                    if (retval == MyGuiScreenMessageBox.ResultEnum.YES)
                        if (!MyBrowserHelper.OpenInternetBrowser(MyPerGameSettings.LocalizationWebUrl))
                        {
                            StringBuilder sbMessage = new StringBuilder();
                            sbMessage.AppendFormat(MyTexts.GetString(MySpaceTexts.TitleFailedToStartInternetBrowser), MyPerGameSettings.LocalizationWebUrl);
                            StringBuilder sbTitle = MyTexts.Get(MySpaceTexts.TitleFailedToStartInternetBrowser);
                            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                messageText: sbMessage,
                                messageCaption: sbTitle));
                        }
                }));
        }

        void m_languageCombobox_ItemSelected()
        {
            m_settings.Language = (MyLanguagesEnum)m_languageCombobox.GetSelectedKey();
            if (m_languageCombobox.GetSelectedIndex() > 0)
            {
                m_localizationWarningLabel.ColorMask = Color.Red.ToVector4();
            }
            else
            {
                m_localizationWarningLabel.ColorMask = Color.White.ToVector4();
            }
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenOptionsGame";
        }

        void UpdateControls(bool constructor)
        {
            if (constructor)
            {
                m_languageCombobox.SelectItemByKey((int)MySandboxGame.Config.Language);
                m_buildingModeCombobox.SelectItemByKey((int)MyCubeBuilder.BuildingMode);
                m_controlHintsCheckbox.IsChecked = MySandboxGame.Config.ControlsHints;
                m_rotationHintsCheckbox.IsChecked = MySandboxGame.Config.RotationHints;
                m_crosshairCheckbox.IsChecked = MySandboxGame.Config.ShowCrosshair;
                m_disableHeadbobCheckbox.IsChecked = MySandboxGame.Config.DisableHeadbob;
                m_compressSavesCheckbox.IsChecked = MySandboxGame.Config.CompressSaveGames;
                m_showPlayerNamesCheckbox.IsChecked = MySandboxGame.Config.ShowPlayerNamesOnHud;
                m_UITransparencySlider.Value = MySandboxGame.Config.UITransparency;
                m_UIBkTransparencySlider.Value = MySandboxGame.Config.UIBkTransparency;
            }
            else
            {
                m_languageCombobox.SelectItemByKey((int)m_settings.Language);
                m_buildingModeCombobox.SelectItemByKey((int)m_settings.BuildingMode);
                m_controlHintsCheckbox.IsChecked = m_settings.ControlHints;
                m_rotationHintsCheckbox.IsChecked = m_settings.RotationHints;
                m_crosshairCheckbox.IsChecked = m_settings.ShowCrosshair;
                m_disableHeadbobCheckbox.IsChecked = m_settings.DisableHeadbob;
                m_compressSavesCheckbox.IsChecked = m_settings.CompressSaveGames;
                m_showPlayerNamesCheckbox.IsChecked = m_settings.ShowPlayerNamesOnHud;
                m_UITransparencySlider.Value = m_settings.UITransparency;
                m_UIBkTransparencySlider.Value = m_settings.UIBkTransparency;
            }
        }

        void DoChanges()
        {
            MyLanguage.CurrentLanguage = (MyLanguagesEnum)m_languageCombobox.GetSelectedKey();
            MyScreenManager.RecreateControls();
            MyCubeBuilder.BuildingMode = (MyCubeBuilder.BuildingModeEnum)m_buildingModeCombobox.GetSelectedKey();
            MySandboxGame.Config.ControlsHints = m_controlHintsCheckbox.IsChecked;
            MySandboxGame.Config.RotationHints = m_rotationHintsCheckbox.IsChecked;
            MySandboxGame.Config.ShowCrosshair = m_crosshairCheckbox.IsChecked;
            MySandboxGame.Config.DisableHeadbob = m_disableHeadbobCheckbox.IsChecked;
            MySandboxGame.Config.CompressSaveGames = m_compressSavesCheckbox.IsChecked;
            MySandboxGame.Config.ShowPlayerNamesOnHud = m_showPlayerNamesCheckbox.IsChecked;
            MySandboxGame.Config.UITransparency = m_UITransparencySlider.Value;
            MySandboxGame.Config.UIBkTransparency = m_UIBkTransparencySlider.Value;
            MySandboxGame.Config.Save();
        }

        public void OnCancelClick(MyGuiControlButton sender)
        {
            //  Just close the screen, ignore any change
            CloseScreen();
        }

        public void OnOkClick(MyGuiControlButton sender)
        {
            //  Save/update and then close screen
            DoChanges();
            CloseScreen();
        }
    }
}
