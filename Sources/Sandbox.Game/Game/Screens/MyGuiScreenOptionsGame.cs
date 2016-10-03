using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens;
using Sandbox.Graphics.GUI;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Definitions.GUI;
using VRage;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.GUI;
using VRage.Game;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenOptionsGame : MyGuiScreenBase
    {
        struct OptionsGameSettings
        {
            public MyLanguagesEnum Language;
            public MyCubeBuilder.BuildingModeEnum BuildingMode;
            public MyStringId SkinId;
            public bool ControlHints;
            public bool RotationHints;
            public bool AnimatedRotation;
            public bool ShowBuildingSizeHint;
            public bool ShowCrosshair;
            public bool DisableHeadbob;
            public bool CompressSaveGames;
            public bool ShowPlayerNamesOnHud;
            public bool ReleasingAltResetsCamera;
            public bool EnablePerformanceWarnings;
            public float UIOpacity;
            public float UIBkOpacity;
        }

        MyGuiControlCombobox m_languageCombobox;
        MyGuiControlCombobox m_skinCombobox;
        MyGuiControlCombobox m_buildingModeCombobox;
        MyGuiControlCheckbox m_controlHintsCheckbox;
        MyGuiControlCheckbox m_rotationHintsCheckbox;
        MyGuiControlCheckbox m_animatedRotationCheckbox;
        MyGuiControlCheckbox m_showBuildingSizeHintCheckbox;
        MyGuiControlCheckbox m_crosshairCheckbox;
        MyGuiControlCheckbox m_disableHeadbobCheckbox;
        MyGuiControlCheckbox m_compressSavesCheckbox;
        MyGuiControlCheckbox m_showPlayerNamesCheckbox;
        MyGuiControlCheckbox m_releasingAltResetsCameraCheckbox;
        MyGuiControlSlider m_UIOpacitySlider;
        MyGuiControlSlider m_UIBkOpacitySlider;
        private MyGuiControlButton m_localizationWebButton;
        private MyGuiControlLabel m_skinLabel;
        private MyGuiControlLabel m_skinWarningLabel;
        private MyGuiControlLabel m_localizationWarningLabel;
        private OptionsGameSettings m_settings = new OptionsGameSettings() { UIOpacity = 1.0f, UIBkOpacity = 1.0f };

        public MyGuiScreenOptionsGame()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, size: new Vector2(0.56f, 0.88f),
            backgroundTransition: MySandboxGame.Config.UIBkOpacity, guiTransition: MySandboxGame.Config.UIOpacity)
        {
            EnabledBackgroundFade = true;

            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption(MyCommonTexts.ScreenCaptionGameOptions);

            var leftAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
            var rightAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
            Vector2 controlsOriginLeft = new Vector2(-m_size.Value.X / 2.0f + 0.025f, -m_size.Value.Y / 2.0f + 0.1f);
            Vector2 controlsOriginRight = new Vector2(m_size.Value.X / 2.0f - 0.025f, -m_size.Value.Y / 2.0f + 0.1f);
            Vector2 controlsDelta = new Vector2(0, 0.0525f);
            float rowIndex = 0;

            //  Language
            var languageLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.Language))
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
               text: MyTexts.Get(MyCommonTexts.ScreenOptionsGame_MoreInfo),
               textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.85f * 0.85f,
               onButtonClick: LocalizationWebButtonClicked,
               originAlign: rightAlign);
            m_localizationWebButton.VisualStyle = MyGuiControlButtonStyleEnum.ClickableText;
            var tmp = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.ScreenOptionsGame_MoreInfo), textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.85f * 0.85f);
            m_localizationWarningLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.ScreenOptionsGame_LocalizationWarning), textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.85f * 0.85f)
            {
                Position = controlsOriginRight + rowIndex * controlsDelta - new Vector2(tmp.Size.X + 0.005f, 0),
                OriginAlign = rightAlign,
            };
            rowIndex += 0.8f;

            if (MyFakes.ENABLE_NON_PUBLIC_GUI_ELEMENTS && MyGuiSkinManager.Static.SkinCount > 0)
            {
                m_skinLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.ScreenOptionsGame_Skin))
                {
                    Position = controlsOriginLeft + rowIndex * controlsDelta,
                    OriginAlign = leftAlign,
                };
                m_skinCombobox = new MyGuiControlCombobox()
                {
                    Position = controlsOriginRight + rowIndex * controlsDelta,
                    OriginAlign = rightAlign,
                };
                foreach (var skin in MyGuiSkinManager.Static.AvailableSkins)
                {
                    m_skinCombobox.AddItem(skin.Key, skin.Value.DisplayNameText);
                }
                m_skinCombobox.SelectItemByKey(MyGuiSkinManager.Static.CurrentSkinId);
                rowIndex += 0.65f;
                m_skinWarningLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.ScreenOptionsGame_SkinWarning),
                    textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.85f * 0.85f)
                {
                    Position = controlsOriginRight + rowIndex * controlsDelta,
                    OriginAlign = rightAlign,
                };
                rowIndex += 0.8f;
            }

            var buildingModeLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.ScreenOptionsGame_BuildingMode))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign,
            };
            m_buildingModeCombobox = new MyGuiControlCombobox()
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_buildingModeCombobox.AddItem((int)MyCubeBuilder.BuildingModeEnum.SingleBlock, MyCommonTexts.ScreenOptionsGame_SingleBlock);
            m_buildingModeCombobox.AddItem((int)MyCubeBuilder.BuildingModeEnum.Line, MyCommonTexts.ScreenOptionsGame_Line);
            m_buildingModeCombobox.AddItem((int)MyCubeBuilder.BuildingModeEnum.Plane, MyCommonTexts.ScreenOptionsGame_Plane);
            m_buildingModeCombobox.ItemSelected += m_buildingModeCombobox_ItemSelected;

            //  Notifications
            rowIndex++;
            var controlHintsLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.ShowControlsHints))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            m_controlHintsCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipGameOptionsShowControlsHints))
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_controlHintsCheckbox.IsCheckedChanged += checkboxChanged;

            //  Rotation gizmo
            MyGuiControlLabel rotationHintsLabel = null;
            if (MyFakes.ENABLE_ROTATION_HINTS)
            {
            rowIndex++;
            rotationHintsLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.ShowRotationHints))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            m_rotationHintsCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipGameOptionsShowRotationHints))
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_rotationHintsCheckbox.IsCheckedChanged += checkboxChanged;
            }

            //  Animated Gizmo Rotation
            rowIndex++;
            var animatedRotationLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.AnimatedRotation))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            m_animatedRotationCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MyCommonTexts.AnimatedRotation))
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_animatedRotationCheckbox.IsCheckedChanged += checkboxChanged;

            //  Building Size Hints
            rowIndex++;
            var buildingSizeHintLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.BuildingSizeHint))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            m_showBuildingSizeHintCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MyCommonTexts.BuildingSizeHint))
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_showBuildingSizeHintCheckbox.IsCheckedChanged += checkboxChanged;

            //  Show crosshair?
            rowIndex++;
            var crosshairLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.ShowCrosshair))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            m_crosshairCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipGameOptionsShowCrosshair))
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_crosshairCheckbox.IsCheckedChanged += checkboxChanged;

            //  Headbob
            rowIndex++;
            var headbobLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.Headbob))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            m_disableHeadbobCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MyCommonTexts.Headbob))
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_disableHeadbobCheckbox.IsCheckedChanged += checkboxChanged;

            //  Compress save games checkbox
            rowIndex++;
            var compressSavesLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.CompressSaveGames))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            m_compressSavesCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipGameOptionsCompressSaveGames))
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_compressSavesCheckbox.IsCheckedChanged += checkboxChanged;

            rowIndex++;
            var showPlayerNamesOnHudLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.ScreenOptionsGame_ShowPlayerNames))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            m_showPlayerNamesCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipGameOptionsShowPlayerNames))
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_showPlayerNamesCheckbox.IsCheckedChanged += checkboxChanged;

            rowIndex++;
            var releasingAltResetsCameraLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.ScreenOptionsGame_ReleasingAltResetsCamera))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
            m_releasingAltResetsCameraCheckbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipGameOptionsReleasingAltResetsCamera))
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_releasingAltResetsCameraCheckbox.IsCheckedChanged += checkboxChanged;
            
            rowIndex++;
			var UIOpacityLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.ScreenOptionsGame_UIOpacity))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
			m_UIOpacitySlider = new MyGuiControlSlider(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipGameOptionsUIOpacity), minValue: 0.1f, maxValue: 1.0f, defaultValue: 1.0f)
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_UIOpacitySlider.ValueChanged += sliderChanged;

            rowIndex++;
			var UIBkOpacityLabel = new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.ScreenOptionsGame_UIBkOpacity))
            {
                Position = controlsOriginLeft + rowIndex * controlsDelta,
                OriginAlign = leftAlign
            };
			m_UIBkOpacitySlider = new MyGuiControlSlider(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipGameOptionsUIBkOpacity), minValue: 0, maxValue: 1.0f, defaultValue: 1.0f)
            {
                Position = controlsOriginRight + rowIndex * controlsDelta,
                OriginAlign = rightAlign,
            };
            m_UIBkOpacitySlider.ValueChanged += sliderChanged;

            rowIndex++;

            //  Buttons OK and CANCEL
            var buttonOk = new MyGuiControlButton(text: MyTexts.Get(MyCommonTexts.Ok), onButtonClick: OnOkClick);
            var buttonCancel = new MyGuiControlButton(text: MyTexts.Get(MyCommonTexts.Cancel), onButtonClick: OnCancelClick);
            float buttonX = 0.01f;
            float buttonY = m_size.Value.Y / 2.0f - (buttonOk.Size.Y + 0.03f)+0.025f;
            buttonOk.Position = new Vector2(-buttonX, buttonY);
            buttonOk.OriginAlign = rightAlign;
            buttonCancel.Position = new Vector2(buttonX, buttonY);
            buttonCancel.OriginAlign = leftAlign;

            Controls.Add(languageLabel);
            Controls.Add(m_languageCombobox);
            Controls.Add(m_localizationWebButton);
            Controls.Add(m_localizationWarningLabel);
            if (MyFakes.ENABLE_NON_PUBLIC_GUI_ELEMENTS && MyGuiSkinManager.Static.SkinCount > 0)
            {
                Controls.Add(m_skinLabel);
                Controls.Add(m_skinCombobox);
                Controls.Add(m_skinWarningLabel);
            }
            Controls.Add(buildingModeLabel);
            Controls.Add(m_buildingModeCombobox);
            Controls.Add(controlHintsLabel);
            if (rotationHintsLabel != null)
                Controls.Add(rotationHintsLabel);
            Controls.Add(m_controlHintsCheckbox);
            if (m_rotationHintsCheckbox != null)
                Controls.Add(m_rotationHintsCheckbox);
            Controls.Add(animatedRotationLabel);
            Controls.Add(m_animatedRotationCheckbox);
            Controls.Add(buildingSizeHintLabel);
            Controls.Add(m_showBuildingSizeHintCheckbox);
            Controls.Add(crosshairLabel);
            Controls.Add(m_crosshairCheckbox);
            Controls.Add(headbobLabel);
            Controls.Add(m_disableHeadbobCheckbox);
            Controls.Add(compressSavesLabel);
            Controls.Add(m_compressSavesCheckbox);
            Controls.Add(showPlayerNamesOnHudLabel);
            Controls.Add(m_showPlayerNamesCheckbox);
            Controls.Add(releasingAltResetsCameraLabel);
            Controls.Add(m_releasingAltResetsCameraCheckbox);
            Controls.Add(UIOpacityLabel);
            Controls.Add(m_UIOpacitySlider);
            Controls.Add(UIBkOpacityLabel);
            Controls.Add(m_UIBkOpacitySlider);
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
            else if (m_rotationHintsCheckbox != null && obj == m_rotationHintsCheckbox)
                m_settings.RotationHints = obj.IsChecked;
            else if (obj == m_crosshairCheckbox)
                m_settings.ShowCrosshair = obj.IsChecked;
            else if (obj == m_disableHeadbobCheckbox)
                m_settings.DisableHeadbob = obj.IsChecked;
            else if (obj == m_compressSavesCheckbox)
                m_settings.CompressSaveGames = obj.IsChecked;
            else if (obj == m_showPlayerNamesCheckbox)
                m_settings.ShowPlayerNamesOnHud = obj.IsChecked;
            else if (obj == m_releasingAltResetsCameraCheckbox)
                m_settings.ReleasingAltResetsCamera = obj.IsChecked;
            else if (obj == m_animatedRotationCheckbox)
                m_settings.AnimatedRotation = obj.IsChecked;
            else if (obj == m_showBuildingSizeHintCheckbox)
                m_settings.ShowBuildingSizeHint = obj.IsChecked;
        }

        private void sliderChanged(MyGuiControlSlider obj)
        {
            if (obj == m_UIOpacitySlider)
            {
                m_settings.UIOpacity = obj.Value;
                m_guiTransition = obj.Value;
            }
            else if (obj == m_UIBkOpacitySlider)
            {
                m_settings.UIBkOpacity = obj.Value;
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
                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                messageText: new StringBuilder().AppendFormat(MyTexts.GetString(MyCommonTexts.MessageBoxTextOpenBrowser), MyPerGameSettings.GameWebUrl),
                callback: delegate(MyGuiScreenMessageBox.ResultEnum retval)
                {
                    if (retval == MyGuiScreenMessageBox.ResultEnum.YES)
                        if (!MyBrowserHelper.OpenInternetBrowser(MyPerGameSettings.LocalizationWebUrl))
                        {
                            StringBuilder sbMessage = new StringBuilder();
                            sbMessage.AppendFormat(MyTexts.GetString(MyCommonTexts.TitleFailedToStartInternetBrowser), MyPerGameSettings.LocalizationWebUrl);
                            StringBuilder sbTitle = MyTexts.Get(MyCommonTexts.TitleFailedToStartInternetBrowser);
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
                if (m_rotationHintsCheckbox != null)
                    m_rotationHintsCheckbox.IsChecked = MySandboxGame.Config.RotationHints;
                m_animatedRotationCheckbox.IsChecked = MySandboxGame.Config.AnimatedRotation;
                m_showBuildingSizeHintCheckbox.IsChecked = MySandboxGame.Config.ShowBuildingSizeHint;
                m_crosshairCheckbox.IsChecked = MySandboxGame.Config.ShowCrosshair;
                m_disableHeadbobCheckbox.IsChecked = MySandboxGame.Config.DisableHeadbob;
                m_compressSavesCheckbox.IsChecked = MySandboxGame.Config.CompressSaveGames;
                m_showPlayerNamesCheckbox.IsChecked = MySandboxGame.Config.ShowPlayerNamesOnHud;
                m_releasingAltResetsCameraCheckbox.IsChecked = MySandboxGame.Config.ReleasingAltResetsCamera;
                m_UIOpacitySlider.Value = MySandboxGame.Config.UIOpacity;
                m_UIBkOpacitySlider.Value = MySandboxGame.Config.UIBkOpacity;
            }
            else
            {
                m_languageCombobox.SelectItemByKey((int)m_settings.Language);
                m_buildingModeCombobox.SelectItemByKey((int)m_settings.BuildingMode);
                m_controlHintsCheckbox.IsChecked = m_settings.ControlHints;
                if (m_rotationHintsCheckbox != null)
                    m_rotationHintsCheckbox.IsChecked = m_settings.RotationHints;
                m_animatedRotationCheckbox.IsChecked = m_settings.AnimatedRotation;
                m_showBuildingSizeHintCheckbox.IsChecked = m_settings.ShowBuildingSizeHint;
                m_crosshairCheckbox.IsChecked = m_settings.ShowCrosshair;
                m_disableHeadbobCheckbox.IsChecked = m_settings.DisableHeadbob;
                m_compressSavesCheckbox.IsChecked = m_settings.CompressSaveGames;
                m_showPlayerNamesCheckbox.IsChecked = m_settings.ShowPlayerNamesOnHud;
                m_releasingAltResetsCameraCheckbox.IsChecked = m_settings.ReleasingAltResetsCamera;
                m_UIOpacitySlider.Value = m_settings.UIOpacity;
                m_UIBkOpacitySlider.Value = m_settings.UIBkOpacity;
            }
        }

        void DoChanges()
        {
            MyLanguage.CurrentLanguage = (MyLanguagesEnum)m_languageCombobox.GetSelectedKey();
            if (m_skinCombobox != null)
                MyGuiSkinManager.Static.SelectSkin((int)m_skinCombobox.GetSelectedKey());
            MyScreenManager.RecreateControls();
            MyCubeBuilder.BuildingMode = (MyCubeBuilder.BuildingModeEnum)m_buildingModeCombobox.GetSelectedKey();
            MySandboxGame.Config.ControlsHints = m_controlHintsCheckbox.IsChecked;
            if (m_rotationHintsCheckbox != null)
                MySandboxGame.Config.RotationHints = m_rotationHintsCheckbox.IsChecked;
            MySandboxGame.Config.AnimatedRotation = m_animatedRotationCheckbox.IsChecked;
            MySandboxGame.Config.ShowBuildingSizeHint = m_showBuildingSizeHintCheckbox.IsChecked;
            MySandboxGame.Config.ShowCrosshair = m_crosshairCheckbox.IsChecked;
            MySandboxGame.Config.DisableHeadbob = m_disableHeadbobCheckbox.IsChecked;
            MySandboxGame.Config.CompressSaveGames = m_compressSavesCheckbox.IsChecked;
            MySandboxGame.Config.ShowPlayerNamesOnHud = m_showPlayerNamesCheckbox.IsChecked;
            MySandboxGame.Config.ReleasingAltResetsCamera = m_releasingAltResetsCameraCheckbox.IsChecked;
            MySandboxGame.Config.UIOpacity = m_UIOpacitySlider.Value;
            MySandboxGame.Config.UIBkOpacity = m_UIBkOpacitySlider.Value;
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
