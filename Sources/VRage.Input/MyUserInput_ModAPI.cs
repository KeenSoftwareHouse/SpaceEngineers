using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.ModAPI;
using VRageMath;
using VRage.Library.Utils;

namespace VRage.Input
{
    public partial class MyDirectXInput : IMyUserInput
    {
        #region Lists

        void IMyUserInput.GetPressedKeys(List<MyKeys> keys)
        {
            GetListOfPressedKeys(keys);
        }

        void IMyUserInput.GetPressedMouseButtons(List<MyMouseButtonsEnum> buttons)
        {
            GetListOfPressedMouseButtons(buttons);
        }

        void IMyUserInput.GetPressedJoystickButtons(List<MyJoystickButtonsEnum> buttons)
        {
            buttons.Clear();

            foreach (MyJoystickButtonsEnum button in Enum.GetValues(typeof(MyJoystickButtonsEnum)))
            {
                if (button != MyJoystickButtonsEnum.None && IsJoystickButtonPressed(button))
                    buttons.Add(button);
            }
        }

        void IMyUserInput.GetPressedJoystickAxes(List<MyJoystickAxesEnum> axes)
        {
            axes.Clear();

            foreach (MyJoystickAxesEnum axis in Enum.GetValues(typeof(MyJoystickAxesEnum)))
            {
                if (axis != MyJoystickAxesEnum.None && IsJoystickAxisPressed(axis))
                    axes.Add(axis);
            }
        }

        #endregion

        #region Keyboard

        bool IMyUserInput.IsCapsLock
        {
            get { return IsCapsLock; }
        }

        bool IMyUserInput.IsNumLock
        {
            get { return IsNumLock; }
        }

        bool IMyUserInput.IsScrollLock
        {
            get { return IsScrollLock; }
        }

        bool IMyUserInput.IsKeyValid(MyKeys key)
        {
            return IsKeyValid(key);
        }

        bool IMyUserInput.IsKeyAssigned(MyKeys key)
        {
            return GetControl(key) != null;
        }

        bool IMyUserInput.IsKeyDigit(MyKeys key)
        {
            return IsKeyDigit(key);
        }

        bool IMyUserInput.IsKeyMouseButton(MyKeys key)
        {
            switch (key)
            {
                case MyKeys.LeftButton:
                case MyKeys.RightButton:
                case MyKeys.MiddleButton:
                case MyKeys.ExtraButton1:
                case MyKeys.ExtraButton2:
                    return true;
            }
            return false;
        }

        bool IMyUserInput.IsKeyStatus(MyKeys key, MyPressEnum check)
        {
            switch (check)
            {
                case MyPressEnum.PRESSED: return IsKeyPress(key);
                case MyPressEnum.WAS_PRESSED: return WasKeyPressed(key);
                case MyPressEnum.JUST_PRESSED: return IsNewKeyPressed(key);
                case MyPressEnum.JUST_RELEASED: return IsNewKeyReleased(key);
            }

            return false;
        }

        #endregion

        #region Mouse

        bool IMyUserInput.IsMouseButtonValid(MyMouseButtonsEnum button)
        {
            return IsMouseButtonValid(button);
        }

        bool IMyUserInput.IsMouseButtonAssigned(MyMouseButtonsEnum button)
        {
            return GetControl(button) != null;
        }

        bool IMyUserInput.IsMouseButtonStatus(MyMouseButtonsEnum button, MyPressEnum check)
        {
            switch (check)
            {
                case MyPressEnum.PRESSED: return IsMousePressed(button);
                case MyPressEnum.WAS_PRESSED: return WasMousePressed(button);
                case MyPressEnum.JUST_PRESSED: return IsNewMousePressed(button);
                case MyPressEnum.JUST_RELEASED: return IsNewMouseReleased(button);
            }

            return false;
        }

        int IMyUserInput.MouseWheelValue
        {
            get { return MouseScrollWheelValue(); }
        }

        int IMyUserInput.MouseWheelPreviousValue
        {
            get { return PreviousMouseScrollWheelValue(); }
        }

        int IMyUserInput.MouseCursorX
        {
            get { return m_actualMouseState.X; }
        }

        int IMyUserInput.MouseCursorY
        {
            get { return m_actualMouseState.Y; }
        }

        int IMyUserInput.MouseX
        {
            get { return GetMouseXForGamePlay(); }
        }

        int IMyUserInput.MouseY
        {
            get { return GetMouseYForGamePlay(); }
        }

        Vector2 IMyUserInput.MouseCursorPosition
        {
            get { return m_absoluteMousePosition; }
        }

        Vector2 IMyUserInput.MouseAreaSize
        {
            get { return m_bufferedInputSource.MouseAreaSize; }
        }

        float IMyUserInput.MouseSensitivity
        {
            get { return m_mouseSensitivity; }
        }

        bool IMyUserInput.IsMouseInvertedX
        {
            get { return m_mouseXIsInverted; }
        }

        bool IMyUserInput.IsMouseInvertedY
        {
            get { return m_mouseYIsInverted; }
        }

        #endregion

        #region Controller


        bool IMyUserInput.JoystickConnected
        {
            get { return m_joystickConnected; }
        }

        string IMyUserInput.JoystickName
        {
            get { return JoystickInstanceName; }
        }

        bool IMyUserInput.JoystickAsMouse
        {
            get { return JoystickAsMouse; }
        }

        float IMyUserInput.JoystickSensitivity
        {
            get { return m_joystickSensitivity; }
        }

        float IMyUserInput.JoystickDeadzone
        {
            get { return m_joystickDeadzone; }
        }

        float IMyUserInput.JoystickExponent
        {
            get { return m_joystickExponent; }
        }

        bool IMyUserInput.IsJoystickButtonValid(MyJoystickButtonsEnum button)
        {
            return IsJoystickButtonValid(button);
        }

        bool IMyUserInput.IsJoystickAxisValid(MyJoystickAxesEnum axis)
        {
            return IsJoystickAxisValid(axis);
        }

        bool IMyUserInput.IsJoystickButtonStatus(MyJoystickButtonsEnum button, MyPressEnum check)
        {
            switch (check)
            {
                case MyPressEnum.PRESSED: return IsJoystickButtonPressed(button);
                case MyPressEnum.WAS_PRESSED: return WasJoystickButtonPressed(button);
                case MyPressEnum.JUST_PRESSED: return IsJoystickButtonNewPressed(button);
                case MyPressEnum.JUST_RELEASED: return WasJoystickButtonPressed(button) && !IsJoystickButtonPressed(button);
            }

            return false;
        }

        bool IMyUserInput.IsJoystickAxisStatus(MyJoystickAxesEnum axis, MyPressEnum check)
        {
            switch (check)
            {
                case MyPressEnum.PRESSED: return IsJoystickAxisPressed(axis);
                case MyPressEnum.WAS_PRESSED: return WasJoystickAxisPressed(axis);
                case MyPressEnum.JUST_PRESSED: return IsJoystickAxisNewPressed(axis);
                case MyPressEnum.JUST_RELEASED: return WasJoystickAxisPressed(axis) && !IsJoystickAxisPressed(axis);
            }

            return false;
        }

        float IMyUserInput.GetJoystickAxisRaw(MyJoystickAxesEnum axis)
        {
            return GetJoystickAxisStateRaw(axis);
        }

        float IMyUserInput.GetJoystickAxisRawPrevious(MyJoystickAxesEnum axis)
        {
            return GetPreviousJoystickAxisStateRaw(axis);
        }

        float IMyUserInput.GetJoystickRawX()
        {
            return GetJoystickX();
        }

        float IMyUserInput.GetJoystickRawY()
        {
            return GetJoystickY();
        }

        float IMyUserInput.GetJoystickAxisGameplay(MyJoystickAxesEnum axis)
        {
            return GetJoystickAxisStateForGameplay(axis);
        }

        float IMyUserInput.GetJoystickAxisGameplayPrevious(MyJoystickAxesEnum axis)
        {
            return GetJoystickAxisStateForGameplay(axis);
        }

        void IMyUserInput.DebugJoystickState(StringBuilder output)
        {
            GetActualJoystickState(output);
        }

        MyJoystickDPadEnum[] IMyUserInput.GetJoystickDPad()
        {
            if (m_joystickConnected)
            {
                return (MyJoystickDPadEnum[])(object)m_actualJoystickState.PointOfViewControllers;
            }
            return null;
        }

        MyJoystickDPadEnum[] IMyUserInput.GetJoystickDPadPrevious()
        {
            if (m_joystickConnected)
            {
                return (MyJoystickDPadEnum[])(object)m_previousJoystickState.PointOfViewControllers;
            }
            return null;
        }

        bool IMyUserInput.IsJoystickDPadUpPressed()
        {
            return IsGamepadKeyUpPressed();
        }

        bool IMyUserInput.IsJoystickDPadDownPressed()
        {
            return IsGamepadKeyDownPressed();
        }

        bool IMyUserInput.IsJoystickDPadLeftPressed()
        {
            return IsGamepadKeyLeftPressed();
        }

        bool IMyUserInput.IsJoystickDPadRightPressed()
        {
            return IsGamepadKeyRightPressed();
        }

        bool IMyUserInput.WasJoystickDPadUpPressed()
        {
            return WasGamepadKeyUpPressed();
        }

        bool IMyUserInput.WasJoystickDPadDownPressed()
        {
            return WasGamepadKeyDownPressed();
        }

        bool IMyUserInput.WasJoystickDPadLeftPressed()
        {
            return WasGamepadKeyLeftPressed();
        }

        bool IMyUserInput.WasJoystickDPadRightPressed()
        {
            return WasGamepadKeyRightPressed();
        }

        #endregion

        #region GameControls

        void IMyUserInput.GetPressedControls(List<IMyControl> controls)
        {
            foreach (var control in m_gameControlsList.Values)
            {
                if (control.IsPressed())
                {
                    controls.Add(MyControlWrapper.Get(control));
                }
            }
        }

        void IMyUserInput.GetPressedControls(Dictionary<MyStringId, IMyControl> controls)
        {
            foreach (MyControl control in m_gameControlsList.Values)
            {
                if (control.IsPressed())
                {
                    controls.Add(control.GetControlName(), MyControlWrapper.Get(control));
                }
            }
        }

        private MyStringId GetContextFromEnum(MyJoystickContextEnum context)
        {
            switch (context)
            {
                case MyJoystickContextEnum.BASE: return MyControllerHelper.CX_BASE;
                case MyJoystickContextEnum.GUI: return MyControllerHelper.CX_GUI;
                case MyJoystickContextEnum.CHARACTER: return MyControllerHelper.CX_CHARACTER;
                case MyJoystickContextEnum.SPACESHIP: return MyStringId.GetOrCompute("SPACESHIP");
                case MyJoystickContextEnum.BUILD_MODE: return MyStringId.GetOrCompute("BUILD_MODE");
                case MyJoystickContextEnum.VOXEL: return MyStringId.GetOrCompute("VOXEL");
            }

            return MyStringId.NullOrEmpty;
        }

        bool IMyUserInput.IsControlStatus(MyStringId controlId, MyPressEnum check, MyJoystickContextEnum context)
        {
            switch (check)
            {
                case MyPressEnum.PRESSED: return MyControllerHelper.IsControl(GetContextFromEnum(context), controlId, MyControlStateType.PRESSED);
                case MyPressEnum.WAS_PRESSED: return MyControllerHelper.IsControl(GetContextFromEnum(context), controlId, MyControlStateType.WAS_PRESSED);
                case MyPressEnum.JUST_PRESSED: return MyControllerHelper.IsControl(GetContextFromEnum(context), controlId, MyControlStateType.NEW_PRESSED);
                case MyPressEnum.JUST_RELEASED: return MyControllerHelper.IsControl(GetContextFromEnum(context), controlId, MyControlStateType.NEW_RELEASED);
            }

            return false;
        }

        float IMyUserInput.GetControlAnalog(MyStringId controlId, MyJoystickContextEnum context)
        {
            return MyControllerHelper.IsControlAnalog(GetContextFromEnum(context), controlId);
        }

        IMyControl IMyUserInput.GetControl(MyStringId controlId)
        {
            MyControl control;
            if (m_gameControlsList.TryGetValue(controlId, out control))
            {
                return MyControlWrapper.Get(control);
            }
            return null;
        }

        IMyControl IMyUserInput.GetControlFor(MyKeys key)
        {
            return MyControlWrapper.Get(GetControl(key));
        }

        IMyControl IMyUserInput.GetControlFor(MyMouseButtonsEnum button)
        {
            return MyControlWrapper.Get(GetControl(button));
        }

        void IMyUserInput.GetControls(Dictionary<MyStringId, IMyControl> controls)
        {
            foreach (var kv in m_gameControlsList)
            {
                controls.Add(kv.Key, MyControlWrapper.Get(kv.Value));
            }
        }

        IMyControl IMyUserInput.GetDefaultControl(MyStringId controlId)
        {
            MyControl control;
            if (m_defaultGameControlsList.TryGetValue(controlId, out control))
            {
                return MyControlWrapper.Get(control);
            }
            return null;
        }

        void IMyUserInput.GetDefaultControls(Dictionary<MyStringId, IMyControl> defaultControls)
        {
            foreach (var kv in m_defaultGameControlsList)
            {
                defaultControls.Add(kv.Key, kv.Value as IMyControl);
            }
        }

        #endregion
    }
}
