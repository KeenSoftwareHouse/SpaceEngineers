using Sandbox.Engine.Utils;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenOptionsControls : MyGuiScreenBase
    {
        private class ControlButtonData
        {
            public readonly MyControl Control;
            public readonly MyGuiInputDeviceEnum Device;

            public ControlButtonData(MyControl control, MyGuiInputDeviceEnum device)
            {
                this.Control = control;
                this.Device = device;
            }
        }

        MyGuiControlTypeEnum m_currentControlType;
        MyGuiControlCombobox m_controlTypeList;

        //  All controls in this screen
        Dictionary<MyGuiControlTypeEnum, List<MyGuiControlBase>> m_allControls = new Dictionary<MyGuiControlTypeEnum, List<MyGuiControlBase>>();

        //  List for getting button by input type
        List<MyGuiControlButton> m_key1Buttons;
        List<MyGuiControlButton> m_key2Buttons;
        List<MyGuiControlButton> m_mouseButtons;
        List<MyGuiControlButton> m_joystickButtons;
        List<MyGuiControlButton> m_joystickAxes;

        //  I need these checkboxes here so I can check their value if the user clicks 'OK'
        MyGuiControlCheckbox m_invertMouseXCheckbox;
        MyGuiControlCheckbox m_invertMouseYCheckbox;
        MyGuiControlSlider m_mouseSensitivitySlider;
        MyGuiControlSlider m_joystickSensitivitySlider;
        MyGuiControlSlider m_joystickDeadzoneSlider;
        MyGuiControlSlider m_joystickExponentSlider;
        MyGuiControlCombobox m_joystickCombobox;

        Vector2 m_controlsOriginLeft;
        Vector2 m_controlsOriginRight;

        public MyGuiScreenOptionsControls()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(1200f / 1600f, 1127f / 1200f))
        {
            EnabledBackgroundFade = true;
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption(MyCommonTexts.ScreenCaptionControls);

            MyInput.Static.TakeSnapshot();

            var topCenter = m_size.Value * new Vector2(0f, -0.5f);
            var bottomCenter = m_size.Value * new Vector2(0f, 0.5f);
            var topLeft = m_size.Value * -0.5f;
            m_controlsOriginLeft = topLeft + new Vector2(96f, 122f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            m_controlsOriginRight = topCenter + new Vector2(-60f, 122f) / MyGuiConstants.GUI_OPTIMAL_SIZE;

            #region Add Revert, OK, Cancel and selection combobox
            var buttonSize = MyGuiControlButton.GetVisualStyle(MyGuiControlButtonStyleEnum.Default).NormalTexture.MinSizeGui;
            float buttonsY = bottomCenter.Y - 0.055f;
            var okButton = new MyGuiControlButton(
                position: new Vector2(-buttonSize.X - 20f / MyGuiConstants.GUI_OPTIMAL_SIZE.X, buttonsY),
                size: MyGuiConstants.MESSAGE_BOX_BUTTON_SIZE_SMALL,
                text: MyTexts.Get(MyCommonTexts.Ok),
                onButtonClick: OnOkClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            var cancelButton = new MyGuiControlButton(
                position: new Vector2(0f, buttonsY),
                size: MyGuiConstants.MESSAGE_BOX_BUTTON_SIZE_SMALL,
                text: MyTexts.Get(MyCommonTexts.Cancel),
                onButtonClick: OnCancelClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            var resetButton = new MyGuiControlButton(
                position: new Vector2(buttonSize.X + 20f / MyGuiConstants.GUI_OPTIMAL_SIZE.X, buttonsY),
                size: MyGuiConstants.MESSAGE_BOX_BUTTON_SIZE_SMALL,
                text: MyTexts.Get(MyCommonTexts.Revert),
                onButtonClick: OnResetDefaultsClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
            Controls.Add(resetButton);

            //  Page selection combobox
            m_currentControlType = MyGuiControlTypeEnum.General;
            var cBoxPosition = m_controlsOriginRight + 0.5f * MyGuiConstants.CONTROLS_DELTA +
                               new Vector2(MyGuiConstants.COMBOBOX_MEDIUM_SIZE.X / 2.0f, 0) - new Vector2(0.065f, 0);
            m_controlTypeList = new MyGuiControlCombobox(cBoxPosition);
            m_controlTypeList.AddItem((int)MyGuiControlTypeEnum.General, MyCommonTexts.ControlTypeGeneral);
            m_controlTypeList.AddItem((int)MyGuiControlTypeEnum.Navigation, MyCommonTexts.ControlTypeNavigation);

            //by Gregory this is temporary for Medieval Engineers. Do not show Navigation2 screen if Navigation2 tag is not found
            var DoNotAddNavigation2 = true;
            foreach (var control in MyInput.Static.GetGameControlsList())
            {
                if (control.GetControlTypeEnum() == MyGuiControlTypeEnum.Navigation2)
                {
                    DoNotAddNavigation2 = false;
                    break;
                }
            }

            if (!DoNotAddNavigation2)
            {
                m_controlTypeList.AddItem((int)MyGuiControlTypeEnum.Navigation2, MyCommonTexts.ControlTypeNavigation2);
            }
            
            m_controlTypeList.AddItem((int)MyGuiControlTypeEnum.ToolsOrWeapons, MyCommonTexts.ControlTypeToolsOrWeapons);
            m_controlTypeList.AddItem((int)MyGuiControlTypeEnum.ToolsOrWeapons2, MyCommonTexts.ControlTypeToolsOrWeapons2);
            m_controlTypeList.AddItem((int)MyGuiControlTypeEnum.Systems1, MyCommonTexts.ControlTypeSystems1);
            m_controlTypeList.AddItem((int)MyGuiControlTypeEnum.Systems2, MyCommonTexts.ControlTypeSystems2);
            m_controlTypeList.AddItem((int)MyGuiControlTypeEnum.Systems3, MyCommonTexts.ControlTypeSystems3);
            m_controlTypeList.AddItem((int)MyGuiControlTypeEnum.Spectator, MyCommonTexts.Spectator);
            m_controlTypeList.SelectItemByKey((int)m_currentControlType);
            Controls.Add(m_controlTypeList);

            #endregion

            AddControls();

            ActivateControls(m_currentControlType);

            CloseButtonEnabled = true;
        }

        private void AddControls()
        {
            m_key1Buttons = new List<MyGuiControlButton>();
            m_key2Buttons = new List<MyGuiControlButton>();
            m_mouseButtons = new List<MyGuiControlButton>();
            if (MyFakes.ENABLE_JOYSTICK_SETTINGS)
            {
                m_joystickButtons = new List<MyGuiControlButton>();
                m_joystickAxes = new List<MyGuiControlButton>();
            }

            AddControlsByType(MyGuiControlTypeEnum.General);
            AddControlsByType(MyGuiControlTypeEnum.Navigation);
            AddControlsByType(MyGuiControlTypeEnum.Navigation2);
            AddControlsByType(MyGuiControlTypeEnum.Systems1);
            AddControlsByType(MyGuiControlTypeEnum.Systems2);
            AddControlsByType(MyGuiControlTypeEnum.Systems3);
            AddControlsByType(MyGuiControlTypeEnum.ToolsOrWeapons);
            AddControlsByType(MyGuiControlTypeEnum.ToolsOrWeapons2);
            AddControlsByType(MyGuiControlTypeEnum.Spectator);

            foreach (var entry in m_allControls)
            {
                foreach (var control in entry.Value)
                    Controls.Add(control);
                DeactivateControls(entry.Key);
            }

            //There are no controls for this category now, so hide it completely and uncomment, when we have new comms controls
            if (MyFakes.ENABLE_JOYSTICK_SETTINGS)
            {
                RefreshJoystickControlEnabling();
            }
        }

        private MyGuiControlLabel MakeLabel(float deltaMultip, MyStringId textEnum)
        {
            return new MyGuiControlLabel(
                position: m_controlsOriginLeft + deltaMultip * MyGuiConstants.CONTROLS_DELTA,
                text: MyTexts.GetString(textEnum));
        }

        private MyGuiControlLabel MakeLabel(MyStringId textEnum, Vector2 position)
        {
            var label = new MyGuiControlLabel(
                position: position,
                text: MyTexts.GetString(textEnum),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);

            return label;
        }

        private MyGuiControlButton MakeControlButton(MyControl control, Vector2 position, MyGuiInputDeviceEnum device)
        {
            StringBuilder boundText = null;
            control.AppendBoundButtonNames(ref boundText, device);
            MyControl.AppendUnknownTextIfNeeded(ref boundText, MyTexts.GetString(MyCommonTexts.UnknownControl_None));
            var button = new MyGuiControlButton(
                position: position,
                text: boundText,
                onButtonClick: OnControlClick,
                visualStyle: MyGuiControlButtonStyleEnum.ControlSetting,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            button.UserData = new ControlButtonData(control, device);
            return button;
        }

        private void AddControlsByType(MyGuiControlTypeEnum type)
        {
            //  "General" page is little bit too complex, so I need to create it separately.
            if (type == MyGuiControlTypeEnum.General)
            {
                AddGeneralControls();
                return;
            }

            var buttonStyle = MyGuiControlButton.GetVisualStyle(MyGuiControlButtonStyleEnum.ControlSetting);

            Vector2 controlsOriginRight = m_controlsOriginRight;
            controlsOriginRight.X -= 0.02f;
            m_allControls[type] = new List<MyGuiControlBase>();
            float i = 2;
            float buttonScale = 0.85f;

            var controls = MyInput.Static.GetGameControlsList();

            var keyboardLabel   = MakeLabel(MyCommonTexts.ScreenOptionsControls_Keyboard, Vector2.Zero);
            var keyboard2Label  = MakeLabel(MyCommonTexts.ScreenOptionsControls_Keyboard2, Vector2.Zero);
            var mouseLabel      = MakeLabel(MyCommonTexts.ScreenOptionsControls_Mouse, Vector2.Zero);
            var gamepadLabel    = (MyFakes.ENABLE_JOYSTICK_SETTINGS) ? MakeLabel(MyCommonTexts.ScreenOptionsControls_Gamepad, Vector2.Zero) : null;
            var analogAxesLabel = (MyFakes.ENABLE_JOYSTICK_SETTINGS) ? MakeLabel(MyCommonTexts.ScreenOptionsControls_AnalogAxes, Vector2.Zero) : null;

            float columnWidth = 1.1f * Math.Max(Math.Max(keyboardLabel.Size.X, keyboard2Label.Size.X),
                                                Math.Max(mouseLabel.Size.X, buttonStyle.SizeOverride.Value.X));

            var position = (i - 1) * MyGuiConstants.CONTROLS_DELTA + controlsOriginRight;
            position.X += columnWidth * 0.5f; // make labels centered
            keyboardLabel.Position = position; position.X += columnWidth;
            keyboard2Label.Position = position; position.X += columnWidth;
            mouseLabel.Position = position;

            m_allControls[type].Add(keyboardLabel);
            m_allControls[type].Add(keyboard2Label);
            m_allControls[type].Add(mouseLabel);

            if (MyFakes.ENABLE_JOYSTICK_SETTINGS)
            {
                //position.X += columnWidth; gamepadLabel.Position = position;
                //position.X += columnWidth; analogAxesLabel.Position = position;

                //m_allControls[type].Add(gamepadLabel);
                //m_allControls[type].Add(analogAxesLabel);
            }

            foreach (MyControl control in controls)
            {
                if (control.GetControlTypeEnum() == type)
                {
                    m_allControls[type].Add(new MyGuiControlLabel(
                        position: m_controlsOriginLeft + i * MyGuiConstants.CONTROLS_DELTA,
                        text: MyTexts.GetString(control.GetControlName())));

                    position = controlsOriginRight + i * MyGuiConstants.CONTROLS_DELTA;
                    position.X += columnWidth * 0.5f;

                    // This is column for keyboard 1
                    var key1Button = MakeControlButton(control, position, MyGuiInputDeviceEnum.Keyboard);
                    m_allControls[type].Add(key1Button);
                    m_key1Buttons.Add(key1Button);
                    position.X += columnWidth;

                    // This is column for keyboard 2
                    var key2Button = MakeControlButton(control, position, MyGuiInputDeviceEnum.KeyboardSecond);
                    m_allControls[type].Add(key2Button);
                    m_key2Buttons.Add(key2Button);
                    position.X += columnWidth;

                    // This is column for mouse
                    var mouseButton = MakeControlButton(control, position, MyGuiInputDeviceEnum.Mouse);
                    m_allControls[type].Add(mouseButton);
                    m_mouseButtons.Add(mouseButton);
                    position.X += columnWidth;

                    if (MyFakes.ENABLE_JOYSTICK_SETTINGS)
                    {
                        //// This is column for joystick
                        //var joyButton = MakeControlButton(control, position, MyGuiInputDeviceEnum.Joystick);
                        //m_allControls[type].Add(joyButton);
                        //m_joystickButtons.Add(joyButton);
                        //position.X += columnWidth;

                        //// This is column for joystick axes
                        //var joyAxis = MakeControlButton(control, position, MyGuiInputDeviceEnum.JoystickAxis);
                        //m_allControls[type].Add(joyAxis);
                        //m_joystickAxes.Add(joyAxis);
                        //position.X += columnWidth;
                    }

                    i += buttonScale;
                }
            }
        }

        private void AddGeneralControls()
        {
            m_allControls[MyGuiControlTypeEnum.General] = new List<MyGuiControlBase>();


            MyGuiControlLabel tmp = MakeLabel(2f, MyCommonTexts.InvertMouseX);
            m_allControls[MyGuiControlTypeEnum.General].Add(MakeLabel(2f, MyCommonTexts.InvertMouseX));
            m_allControls[MyGuiControlTypeEnum.General].Add(MakeLabel(3f, MyCommonTexts.InvertMouseY));
            m_allControls[MyGuiControlTypeEnum.General].Add(MakeLabel(4f, MyCommonTexts.MouseSensitivity));

            m_invertMouseXCheckbox = new MyGuiControlCheckbox(
                     position: m_controlsOriginRight + 2 * MyGuiConstants.CONTROLS_DELTA,
                     isChecked: MyInput.Static.GetMouseXInversion(),
                     originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);

            m_allControls[MyGuiControlTypeEnum.General].Add(m_invertMouseXCheckbox);


            m_invertMouseYCheckbox = new MyGuiControlCheckbox(
                 position: m_controlsOriginRight + 3 * MyGuiConstants.CONTROLS_DELTA,
                 isChecked: MyInput.Static.GetMouseYInversion(),
                 originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);

            m_allControls[MyGuiControlTypeEnum.General].Add(m_invertMouseYCheckbox);

            m_mouseSensitivitySlider = new MyGuiControlSlider(
                position: m_controlsOriginRight + 4 * MyGuiConstants.CONTROLS_DELTA,
                minValue: 0.0f,
                maxValue: 3.0f,
                defaultValue: MyInput.Static.GetMouseSensitivity(),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_allControls[MyGuiControlTypeEnum.General].Add(m_mouseSensitivitySlider);

            if (MyFakes.ENABLE_JOYSTICK_SETTINGS)
            {
                const float multiplierJoystick = 6.5f;
                const float multiplierSensitivity = 8;
                const float multiplierExponent = 9;
                const float multiplierDeadzone = 10;

                m_allControls[MyGuiControlTypeEnum.General].Add(MakeLabel(multiplierJoystick, MyCommonTexts.Joystick));
                m_allControls[MyGuiControlTypeEnum.General].Add(MakeLabel(multiplierSensitivity, MyCommonTexts.JoystickSensitivity));
                m_allControls[MyGuiControlTypeEnum.General].Add(MakeLabel(multiplierExponent, MyCommonTexts.JoystickExponent));
                m_allControls[MyGuiControlTypeEnum.General].Add(MakeLabel(multiplierDeadzone, MyCommonTexts.JoystickDeadzone));

                m_joystickCombobox = new MyGuiControlCombobox(m_controlsOriginRight + multiplierJoystick * MyGuiConstants.CONTROLS_DELTA + new Vector2(MyGuiConstants.COMBOBOX_MEDIUM_SIZE.X / 2.0f, 0));
                m_joystickCombobox.ItemSelected += OnSelectJoystick;
                AddJoysticksToComboBox();
                m_joystickCombobox.Enabled = !MyFakes.ENFORCE_CONTROLLER || !MyInput.Static.IsJoystickConnected();
                m_allControls[MyGuiControlTypeEnum.General].Add(m_joystickCombobox);

                m_joystickSensitivitySlider = new MyGuiControlSlider(
                    position: m_controlsOriginRight + multiplierSensitivity * MyGuiConstants.CONTROLS_DELTA + new Vector2(MyGuiConstants.COMBOBOX_MEDIUM_SIZE.X / 2.0f, 0),
                    minValue: 0.1f,
                    maxValue: 6.0f,
                    defaultValue: MyInput.Static.GetJoystickSensitivity());
                m_joystickSensitivitySlider.Value = MyInput.Static.GetJoystickSensitivity();
                m_allControls[MyGuiControlTypeEnum.General].Add(m_joystickSensitivitySlider);

                m_joystickExponentSlider = new MyGuiControlSlider(
                    position: m_controlsOriginRight + multiplierExponent * MyGuiConstants.CONTROLS_DELTA + new Vector2(MyGuiConstants.COMBOBOX_MEDIUM_SIZE.X / 2.0f, 0),
                    minValue: 1.0f,
                    maxValue: 8.0f,
                    defaultValue: MyInput.Static.GetJoystickExponent());
                m_joystickExponentSlider.Value = MyInput.Static.GetJoystickExponent();
                m_allControls[MyGuiControlTypeEnum.General].Add(m_joystickExponentSlider);

                m_joystickDeadzoneSlider = new MyGuiControlSlider(
                    position: m_controlsOriginRight + multiplierDeadzone * MyGuiConstants.CONTROLS_DELTA + new Vector2(MyGuiConstants.COMBOBOX_MEDIUM_SIZE.X / 2.0f, 0),
                    minValue: 0.0f,
                    maxValue: 0.5f,
                    defaultValue: MyInput.Static.GetJoystickDeadzone());
                m_joystickDeadzoneSlider.Value = MyInput.Static.GetJoystickDeadzone();
                m_allControls[MyGuiControlTypeEnum.General].Add(m_joystickDeadzoneSlider);
            }

        }

        private void DeactivateControls(MyGuiControlTypeEnum type)
        {
            foreach (var item in m_allControls[type])
            {
                item.Visible = false;
            }
        }

        private void ActivateControls(MyGuiControlTypeEnum type)
        {
            foreach (var item in m_allControls[type])
            {
                item.Visible = true;
            }
        }

        private void AddJoysticksToComboBox()
        {
            int counter = 0;
            bool selectedJoystick = false;
            m_joystickCombobox.AddItem(counter++, MyTexts.Get(MyCommonTexts.Disabled));

            var joysticks = MyInput.Static.EnumerateJoystickNames();
            foreach (string joystickName in joysticks)
            {
                m_joystickCombobox.AddItem(counter, new StringBuilder(joystickName));
                if (MyInput.Static.JoystickInstanceName == joystickName)
                {
                    selectedJoystick = true;
                    m_joystickCombobox.SelectItemByIndex(counter);
                }
                counter++;
            }

            if (!selectedJoystick)
                m_joystickCombobox.SelectItemByIndex(0);
        }

        private void OnSelectJoystick()
        {
            MyInput.Static.JoystickInstanceName = m_joystickCombobox.GetSelectedIndex() == 0 ? null : m_joystickCombobox.GetSelectedValue().ToString();
            RefreshJoystickControlEnabling();
        }

        private void RefreshJoystickControlEnabling()
        {
            bool enable = m_joystickCombobox.GetSelectedIndex() != 0;
            foreach (var button in m_joystickButtons)
                button.Enabled = enable;
            foreach (var axis in m_joystickAxes)
                axis.Enabled = enable;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenOptionsControls";
        }

        public override bool Update(bool hasFocus)
        {
            if (m_controlTypeList.GetSelectedKey() != (int)m_currentControlType)
            {
                DeactivateControls(m_currentControlType);
                m_currentControlType = (MyGuiControlTypeEnum)m_controlTypeList.GetSelectedKey();
                ActivateControls(m_currentControlType);
            }

            if (base.Update(hasFocus) == false) return false;
            return true;
        }

        private void OnControlClick(MyGuiControlButton button)
        {
            var data = (ControlButtonData)button.UserData;

            MyStringId messageText = MyCommonTexts.AssignControlKeyboard;
            if (data.Device == MyGuiInputDeviceEnum.Mouse)
            {
                messageText = MyCommonTexts.AssignControlMouse;
            }

            var mbox = new MyGuiControlAssignKeyMessageBox(data.Device, data.Control, messageText);
            mbox.Closed += (s) => RefreshButtonTexts();
            MyGuiSandbox.AddScreen(mbox);
        }

        private void OnResetDefaultsClick(MyGuiControlButton sender)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.YES_NO,
                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionResetControlsToDefault),
                messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextResetControlsToDefault),
                callback: (res) =>
                    {
                        if (res == MyGuiScreenMessageBox.ResultEnum.YES)
                        {
                            //  revert to controls when the screen was first opened and then close.
                            MyInput.Static.RevertToDefaultControls();
                            //  I need refresh text on buttons. Create them again is the easiest way.
                            DeactivateControls(m_currentControlType);
                            AddControls();
                            ActivateControls(m_currentControlType);
                        }
                    }));
        }

        protected override void Canceling()
        {
            MyInput.Static.RevertChanges();
            base.Canceling();
        }

        private void OnCancelClick(MyGuiControlButton sender)
        {
            //  revert to controls when the screen was first opened and then close.
            MyInput.Static.RevertChanges();
            CloseScreen();
        }

        private void OnOkClick(MyGuiControlButton sender)
        {
            CloseScreenAndSave();
        }

        private void CloseScreenAndSave()
        {
            MyInput.Static.SetMouseXInversion(m_invertMouseXCheckbox.IsChecked);
            MyInput.Static.SetMouseYInversion(m_invertMouseYCheckbox.IsChecked);
            MyInput.Static.SetMouseSensitivity(m_mouseSensitivitySlider.Value);

            if (MyFakes.ENABLE_JOYSTICK_SETTINGS)
            {
                MyInput.Static.JoystickInstanceName = m_joystickCombobox.GetSelectedIndex() == 0 ? null : m_joystickCombobox.GetSelectedValue().ToString();
                MyInput.Static.SetJoystickSensitivity(m_joystickSensitivitySlider.Value);
                MyInput.Static.SetJoystickExponent(m_joystickExponentSlider.Value);
                MyInput.Static.SetJoystickDeadzone(m_joystickDeadzoneSlider.Value);
            }

            MyInput.Static.SaveControls(MySandboxGame.Config.ControlsGeneral, MySandboxGame.Config.ControlsButtons);
            MySandboxGame.Config.Save();

            //MyGuiScreenGamePlay.Static.SetControlsChange(true);
            MyScreenManager.RecreateControls();
            CloseScreen();
        }


        private void RefreshButtonTexts()
        {
            RefreshButtonTexts(m_key1Buttons);
            RefreshButtonTexts(m_key2Buttons);
            RefreshButtonTexts(m_mouseButtons);
            if (MyFakes.ENABLE_JOYSTICK_SETTINGS)
            {
                RefreshButtonTexts(m_joystickButtons);
                RefreshButtonTexts(m_joystickAxes);
            }
        }

        private void RefreshButtonTexts(List<MyGuiControlButton> buttons)
        {
            StringBuilder tmp = null;
            foreach (var button in buttons)
            {
                var data = (ControlButtonData)button.UserData;
                data.Control.AppendBoundButtonNames(ref tmp, data.Device);
                MyControl.AppendUnknownTextIfNeeded(ref tmp, MyTexts.GetString(MyCommonTexts.UnknownControl_None));
                button.Text = tmp.ToString();
                tmp.Clear();
            }
        }

        private class MyGuiControlAssignKeyMessageBox : MyGuiScreenMessageBox
        {
            MyControl m_controlBeingSet;
            MyGuiInputDeviceEnum m_deviceType;

            List<MyKeys> m_newPressedKeys                             = new List<MyKeys>();
            List<MyMouseButtonsEnum> m_newPressedMouseButtons       = new List<MyMouseButtonsEnum>();
            List<MyJoystickButtonsEnum> m_newPressedJoystickButtons = new List<MyJoystickButtonsEnum>();
            List<MyJoystickAxesEnum> m_newPressedJoystickAxes       = new List<MyJoystickAxesEnum>();

            List<MyKeys> m_oldPressedKeys                             = new List<MyKeys>();
            List<MyMouseButtonsEnum> m_oldPressedMouseButtons       = new List<MyMouseButtonsEnum>();
            List<MyJoystickButtonsEnum> m_oldPressedJoystickButtons = new List<MyJoystickButtonsEnum>();
            List<MyJoystickAxesEnum> m_oldPressedJoystickAxes       = new List<MyJoystickAxesEnum>();

            public MyGuiControlAssignKeyMessageBox(MyGuiInputDeviceEnum deviceType, MyControl controlBeingSet, MyStringId messageText) : base(
                styleEnum: MyMessageBoxStyleEnum.Error,
                buttonType: MyMessageBoxButtonsType.NONE,
                messageText: MyTexts.Get(messageText),
                messageCaption: MyTexts.Get(MyCommonTexts.SelectControl),
                okButtonText: default(MyStringId),
                cancelButtonText: default(MyStringId),
                yesButtonText: default(MyStringId),
                noButtonText: default(MyStringId),
                callback: null,
                timeoutInMiliseconds: 0,
                focusedResult: ResultEnum.YES,
                canHideOthers: true,
                size: null)
            {
                DrawMouseCursor     = false;
                m_isTopMostScreen   = false;
                m_controlBeingSet   = controlBeingSet;
                m_deviceType        = deviceType;

                MyInput.Static.GetListOfPressedKeys(m_oldPressedKeys);
                MyInput.Static.GetListOfPressedMouseButtons(m_oldPressedMouseButtons);
                m_closeOnEsc = false;
                CanBeHidden = true;
            }

            public override void HandleInput(bool receivedFocusInThisUpdate)
            {
                base.HandleInput(receivedFocusInThisUpdate);
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Escape))
                {
                    Canceling();
                }

                //  Do nothing if base.HandleInput closing this screen right now
                if (State == MyGuiScreenState.CLOSING || State == MyGuiScreenState.HIDING)
                    return;

                switch (m_deviceType)
                {
                    case MyGuiInputDeviceEnum.Keyboard:
                    case MyGuiInputDeviceEnum.KeyboardSecond:
                        HandleKey();
                        break;

                    case MyGuiInputDeviceEnum.Mouse:
                        HandleMouseButton();
                        break;

                }
            }

            private void HandleKey()
            {
                ReadPressedKeys();

                foreach (var key in m_newPressedKeys)
                {
                    if (m_oldPressedKeys.Contains(key))
                        continue;

                    if (!MyInput.Static.IsKeyValid((MyKeys)key))
                    {
                        ShowControlIsNotValidMessageBox();
                        break;
                    }

                    MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                    MyControl ctrl = MyInput.Static.GetControl((MyKeys)key);
                    if (ctrl != null)
                    {
                        if (ctrl.Equals(m_controlBeingSet))
                        {
                            OverwriteAssignment(ctrl, key);
                            CloseScreen();
                        }
                        else
                        {
                            StringBuilder controlText = null;
                            MyControl.AppendName(ref controlText, (MyKeys)key);
                            ShowControlIsAlreadyAssigned(ctrl, controlText, () => OverwriteAssignment(ctrl, key));
                        }
                    }
                    else
                    {
                        m_controlBeingSet.SetControl(m_deviceType, (MyKeys)key);
                        CloseScreen();
                    }
                    break;
                }

                m_oldPressedKeys.Clear();
                MyUtils.Swap(ref m_oldPressedKeys, ref m_newPressedKeys);
            }

            private void HandleMouseButton()
            {
                MyInput.Static.GetListOfPressedMouseButtons(m_newPressedMouseButtons);

                //  don't assign buttons that were pressed when we arrived in the menu
                foreach (var button in m_newPressedMouseButtons)
                {
                    if (!m_oldPressedMouseButtons.Contains(button))
                    {
                        MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                        if (!MyInput.Static.IsMouseButtonValid(button))
                        {
                            ShowControlIsNotValidMessageBox();
                            break;
                        }

                        MyControl ctrl = MyInput.Static.GetControl(button);
                        if (ctrl != null)
                        {
                            if (ctrl.Equals(m_controlBeingSet))
                            {
                                OverwriteAssignment(ctrl, button);
                                CloseScreen();
                            }
                            else
                            {
                                StringBuilder controlText = null;
                                MyControl.AppendName(ref controlText, button);
                                ShowControlIsAlreadyAssigned(ctrl, controlText, () => OverwriteAssignment(ctrl, button));
                            }
                        }
                        else
                        {
                            m_controlBeingSet.SetControl(button);
                            CloseScreen();
                        }
                        break;
                    }
                }

                m_oldPressedMouseButtons.Clear();
                MyUtils.Swap(ref m_oldPressedMouseButtons, ref m_newPressedMouseButtons);
            }

            private void ReadPressedKeys()
            {
                MyInput.Static.GetListOfPressedKeys(m_newPressedKeys);
                m_newPressedKeys.Remove(MyKeys.Control);
                m_newPressedKeys.Remove(MyKeys.Shift);
                m_newPressedKeys.Remove(MyKeys.Alt);
                if (m_newPressedKeys.Contains(MyKeys.LeftControl) &&
                    m_newPressedKeys.Contains(MyKeys.RightAlt))
                {
                    m_newPressedKeys.Remove(MyKeys.LeftControl);
                }
            }

            private void ShowControlIsNotValidMessageBox()
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(MyCommonTexts.ControlIsNotValid),
                    messageCaption: MyTexts.Get(MyCommonTexts.CanNotAssignControl)));
            }

            private void ShowControlIsAlreadyAssigned(MyControl controlAlreadySet, StringBuilder controlButtonName, Action overwriteAssignmentCallback)
            {
                Debug.Assert(controlAlreadySet != null);
                Debug.Assert(overwriteAssignmentCallback != null);

                var messageBox = MakeControlIsAlreadyAssignedDialog(controlAlreadySet, controlButtonName);
                messageBox.ResultCallback = delegate(ResultEnum r)
                    {
                        if (r == ResultEnum.YES)
                        {
                            overwriteAssignmentCallback();
                            CloseScreen();
                        }
                        else
                        {
                            MyInput.Static.GetListOfPressedKeys(m_oldPressedKeys);
                            MyInput.Static.GetListOfPressedMouseButtons(m_oldPressedMouseButtons);
                        }
                    };
                MyGuiSandbox.AddScreen(messageBox);
            }

            private MyGuiScreenMessageBox MakeControlIsAlreadyAssignedDialog(MyControl controlAlreadySet, StringBuilder controlButtonName)
            {
                return MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageText: new StringBuilder(string.Format(MyTexts.GetString(MyCommonTexts.ControlAlreadyAssigned),
                                                                 controlButtonName,
                                                                 MyTexts.Get(controlAlreadySet.GetControlName()))),
                    messageCaption: MyTexts.Get(MyCommonTexts.CanNotAssignControl));
            }

            private void OverwriteAssignment(MyControl controlAlreadySet, MyKeys key)
            {
                Debug.Assert(controlAlreadySet != null);
                Debug.Assert(m_deviceType == MyGuiInputDeviceEnum.Keyboard ||
                             m_deviceType == MyGuiInputDeviceEnum.KeyboardSecond);
                Debug.Assert(controlAlreadySet.GetKeyboardControl() == key ||
                             controlAlreadySet.GetSecondKeyboardControl() == key);

                if (controlAlreadySet.GetKeyboardControl() == key)
                    controlAlreadySet.SetControl(MyGuiInputDeviceEnum.Keyboard, MyKeys.None);
                else
                    controlAlreadySet.SetControl(MyGuiInputDeviceEnum.KeyboardSecond, MyKeys.None);

                m_controlBeingSet.SetControl(m_deviceType, key);
            }

            private void OverwriteAssignment(MyControl controlAlreadySet, MyMouseButtonsEnum button)
            {
                Debug.Assert(controlAlreadySet != null);
                Debug.Assert(m_deviceType == MyGuiInputDeviceEnum.Mouse);
                Debug.Assert(controlAlreadySet.GetMouseControl() == button);

                controlAlreadySet.SetControl(MyMouseButtonsEnum.None);
                m_controlBeingSet.SetControl(button);
            }

            public override bool CloseScreen()
            {
                DrawMouseCursor = true;
                return base.CloseScreen();
            }
        }
    }
}