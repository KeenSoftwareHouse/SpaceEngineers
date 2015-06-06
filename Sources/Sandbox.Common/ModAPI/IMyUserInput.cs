using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VRage.Input;
using VRageMath;
using VRage.Utils;

namespace Sandbox.ModAPI
{
    public enum MyPressEnum
    {
        /// <summary>
        /// Currently pressed.
        /// </summary>
        PRESSED,

        /// <summary>
        /// Was pressed last update.
        /// </summary>
        WAS_PRESSED,

        /// <summary>
        /// Pressed now but was not pressed last update.
        /// </summary>
        JUST_PRESSED,

        /// <summary>
        /// Was pressed last update but not anymore.
        /// </summary>
        JUST_RELEASED
    }

    public enum MyJoystickContextEnum
    {
        BASE,
        GUI,
        CHARACTER,
        SPACESHIP,
        BUILD_MODE,
        VOXEL
    }

    public interface IMyUserInput
    {
        #region Lists

        /// <summary>
        /// Adds the currently pressed keys to the supplied list.
        /// NOTE: This includes mouse buttons since they're part of MyKeys enum.
        /// </summary>
        /// <param name="keys">NOTE: List is cleared internally.</param>
        void GetPressedKeys(List<MyKeys> keys);

        /// <summary>
        /// Adds the currently pressed mouse buttons to the supplied list.
        /// </summary>
        /// <param name="buttons">NOTE: List is cleared internally.</param>
        void GetPressedMouseButtons(List<MyMouseButtonsEnum> buttons);

        /// <summary>
        /// Adds the currently pressed joystick buttons to the supplied list.
        /// </summary>
        /// <param name="buttons">NOTE: List is cleared internally.</param>
        void GetPressedJoystickButtons(List<MyJoystickButtonsEnum> buttons);

        /// <summary>
        /// Adds the currently pressed joystick axes to the supplied list.
        /// </summary>
        /// <param name="axes">NOTE: List is cleared internally.</param>
        void GetPressedJoystickAxes(List<MyJoystickAxesEnum> axes);

        #endregion

        #region Keyboard

        bool IsNumLock { get; }
        bool IsCapsLock { get; }
        bool IsScrollLock { get; }

        /// <summary>
        /// Returns true if the specified key can be used for binding in the game.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool IsKeyValid(MyKeys key);

        /// <summary>
        /// Checks if the specified key is assigned to a game control.
        /// NOTE: Might not work for mouse buttons that are in the MyKeys enum.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool IsKeyAssigned(MyKeys key);

        /// <summary>
        /// Returns if the key is a digit or numpad digit key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool IsKeyDigit(MyKeys key);

        /// <summary>
        /// Returns if the key represents a mouse button instead.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool IsKeyMouseButton(MyKeys key);

        /// <summary>
        /// Get the key's status, by default checks for currently pressed.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        bool IsKeyStatus(MyKeys key, MyPressEnum check = MyPressEnum.PRESSED);

        #endregion

        #region Mouse
        /// <summary>
        /// Returns true if the specified mouse button can be use for binding in the game.
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        bool IsMouseButtonValid(MyMouseButtonsEnum button);

        /// <summary>
        /// Checks if the mouse button is assigned to a game control.
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        bool IsMouseButtonAssigned(MyMouseButtonsEnum button);

        /// <summary>
        /// Get the mouse button's status, by default checks for currently pressed.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        bool IsMouseButtonStatus(MyMouseButtonsEnum button, MyPressEnum check = MyPressEnum.PRESSED);

        /// <summary>
        /// The current mouse scroll wheel value in the current update.
        /// </summary>
        int MouseWheelValue { get; }

        /// <summary>
        /// The mouse scroll wheel value in the previous update.
        /// </summary>
        int MouseWheelPreviousValue { get; }

        /// <summary>
        /// The mouse cursor's X position on the screen.
        /// </summary>
        int MouseCursorX { get; }

        /// <summary>
        /// The mouse cursor's Y position on the screen.
        /// </summary>
        int MouseCursorY { get; }

        /// <summary>
        /// The mouse cursor's position on the screen.
        /// </summary>
        Vector2 MouseCursorPosition { get; }

        /// <summary>
        /// The screen area size that the mouse can go to, usually the user's video resolution.
        /// </summary>
        Vector2 MouseAreaSize { get; }

        /// <summary>
        /// Current Mouse X axis movement, adjusted with sensitivity and inversion.
        /// </summary>
        int MouseX { get; }

        /// <summary>
        /// Current Mouse Y axis movement, adjusted with sensitivity and inversion.
        /// </summary>
        int MouseY { get; }

        /// <summary>
        /// The mouse sensitivity as configured in the game settings.
        /// </summary>
        float MouseSensitivity { get; }

        /// <summary>
        /// Returns true if the mouse X axis is inverted in the user's settings.
        /// </summary>
        bool IsMouseInvertedX { get; }

        /// <summary>
        /// Returns true if the mouse Y axis is inverted in the user's settings.
        /// </summary>
        bool IsMouseInvertedY { get; }

        #endregion

        #region Controller

        bool JoystickConnected { get; }

        string JoystickName { get; }

        bool JoystickAsMouse { get; }

        float JoystickSensitivity { get; }

        float JoystickDeadzone { get; }

        float JoystickExponent { get; }

        bool IsJoystickAxisSupported(MyJoystickAxesEnum axis);

        bool IsJoystickButtonValid(MyJoystickButtonsEnum button);

        bool IsJoystickAxisValid(MyJoystickAxesEnum axis);

        bool IsJoystickButtonStatus(MyJoystickButtonsEnum button, MyPressEnum check = MyPressEnum.PRESSED);

        bool IsJoystickAxisStatus(MyJoystickAxesEnum axis, MyPressEnum check = MyPressEnum.PRESSED);

        /// <summary>
        /// How much a specific joystick axis is pressed.
        /// </summary>
        /// <param name="axis"></param>
        /// <returns>number between 0 and 65535. 32768 is the middle value.</returns>
        float GetJoystickAxisRaw(MyJoystickAxesEnum axis);

        /// <summary>
        /// How much a specific joystick axis was pressed in the previous update.
        /// </summary>
        /// <param name="axis"></param>
        /// <returns>number between 0 and 65535. 32768 is the middle value.</returns>
        float GetJoystickAxisRawPrevious(MyJoystickAxesEnum axis);

        /// <summary>
        /// Shortcut for GetJoystickAxisRaw(MyJoystickAxesEnum.Xpos).
        /// </summary>
        /// <returns></returns>
        float GetJoystickRawX();

        /// <summary>
        /// Shortcut for GetJoystickAxisRaw(MyJoystickAxesEnum.Ypos).
        /// </summary>
        /// <returns></returns>
        float GetJoystickRawY();

        /// <summary>
        /// How much a specific joystick axis is pressed while taking deadzone, sensitivity and non-linearity into account.
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        float GetJoystickAxisGameplay(MyJoystickAxesEnum axis);

        /// <summary>
        /// How much a specific joystick axis was pressed in the previous update.
        /// Taking deadzone, sensitivity and non-linearity into account.
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        float GetJoystickAxisGameplayPrevious(MyJoystickAxesEnum axis);

        /// <summary>
        /// Get the joystick status in a text format, useful for debugging.
        /// </summary>
        /// <param name="output">info will be appended to this, it is not cleared internally.</param>
        void DebugJoystickState(StringBuilder output);

        /// <summary>
        /// Gets the DPad controls status.
        /// </summary>
        /// <returns>can be null if no device is connected or it has no DPad controls.</returns>
        MyJoystickDPadEnum[] GetJoystickDPad();

        /// <summary>
        /// Gets the DPad controls status from the previous update.
        /// </summary>
        /// <returns>can be null if no device is connected or it has no DPad controls.</returns>
        MyJoystickDPadEnum[] GetJoystickDPadPrevious();

        bool IsJoystickDPadUpPressed();
        bool IsJoystickDPadDownPressed();
        bool IsJoystickDPadLeftPressed();
        bool IsJoystickDPadRightPressed();
        bool WasJoystickDPadUpPressed();
        bool WasJoystickDPadDownPressed();
        bool WasJoystickDPadLeftPressed();
        bool WasJoystickDPadRightPressed();

        #endregion

        #region GameControls

        /// <summary>
        /// Get the currently pressed controls as a list.
        /// </summary>
        /// <param name="controls">NOTE: The list is cleared internally.</param>
        void GetPressedControls(List<IMyControl> controls);

        /// <summary>
        /// Get the currently pressed controls as a dictionary for faster searching.
        /// </summary>
        /// <param name="controls">NOTE: The dictionary is cleared internally.</param>
        void GetPressedControls(Dictionary<MyStringId, IMyControl> controls);

        /// <summary>
        /// Get the control's status, by default it checks for currently pressed.
        /// </summary>
        /// <param name="controlId">the control ID, use MyStringId's static methods.</param>
        /// <param name="check">check status type</param>
        /// <param name="context">joystick control context</param>
        /// <returns></returns>
        bool IsControlStatus(MyStringId controlId, MyPressEnum check = MyPressEnum.PRESSED, MyJoystickContextEnum context = MyJoystickContextEnum.CHARACTER);

        /// <summary>
        /// Get the analog status of the specified control.
        /// </summary>
        /// <param name="controlId">the control ID, use MyStringId's static methods</param>
        /// <param name="context">joystick control context</param>
        /// <returns></returns>
        float GetControlAnalog(MyStringId controlId, MyJoystickContextEnum context = MyJoystickContextEnum.CHARACTER);

        /// <summary>
        /// Gets the control settings for a game action.
        /// </summary>
        /// <param name="controlId"></param>
        /// <returns>returns NULL if control is not found.</returns>
        IMyControl GetControl(MyStringId controlId);

        /// <summary>
        /// Gets the control that is triggered by the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>returns NULL if the key is unassigned.</returns>
        IMyControl GetControlFor(MyKeys key);

        /// <summary>
        /// Gets the control that is triggered by the specified mouse button.
        /// </summary>
        /// <param name="button"></param>
        /// <returns>returns NULL if the button is unassigned.</returns>
        IMyControl GetControlFor(MyMouseButtonsEnum button);

        /// <summary>
        /// Get all controls in the game.
        /// </summary>
        /// <param name="controls">NOTE: The dictionary is not cleared internally.</param>
        void GetControls(Dictionary<MyStringId, IMyControl> controls);

        /// <summary>
        /// Gets the default settings for a specific control.
        /// </summary>
        /// <param name="controlId"></param>
        /// <returns>Returns NULL if control is not found.</returns>
        IMyControl GetDefaultControl(MyStringId controlId);

        /// <summary>
        /// Gets the default settings for all controls.
        /// </summary>
        /// <param name="defaultControls">NOTE: The dictionary is not cleared internally.</param>
        void GetDefaultControls(Dictionary<MyStringId, IMyControl> defaultControls);

        #endregion
    }
}
