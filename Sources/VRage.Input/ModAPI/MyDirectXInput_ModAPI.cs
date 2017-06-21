using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Utils;
using VRageMath;

namespace VRage.Input
{
#if !XB1
    public partial class MyDirectXInput : VRage.ModAPI.IMyInput
#else
    public partial class MyXInputInput : VRage.ModAPI.IMyInput
#endif
    {
        bool ModAPI.IMyInput.IsCapsLock { get { return IsCapsLock; } }

        string ModAPI.IMyInput.JoystickInstanceName { get { return JoystickInstanceName; } }

        ListReader<char> ModAPI.IMyInput.TextInput { get { return TextInput; } }

        List<string> ModAPI.IMyInput.EnumerateJoystickNames() { return EnumerateJoystickNames(); }

        //  Return true if ANY key IS pressed, that means that the key was pressed now. During previous Update it wasn't pressed at all.
        bool ModAPI.IMyInput.IsAnyKeyPress() { return IsAnyKeyPress();  }

        //  Return true if ANY mouse key IS pressed.
        bool ModAPI.IMyInput.IsAnyMousePressed() { return IsAnyMousePressed(); }

        bool ModAPI.IMyInput.IsAnyNewMousePressed() { return IsAnyNewMousePressed(); }

        //True if any SHIFT key is pressed
        bool ModAPI.IMyInput.IsAnyShiftKeyPressed() { return IsAnyShiftKeyPressed(); }

        //True if any ALT key is pressed
        bool ModAPI.IMyInput.IsAnyAltKeyPressed() { return IsAnyAltKeyPressed(); }

        //True if any CTRL key is pressed
        bool ModAPI.IMyInput.IsAnyCtrlKeyPressed() { return IsAnyCtrlKeyPressed(); }

        //  Gets an array of values that correspond to the keyboard keys that are currently
        //  being pressed. Reference page contains links to related code samples.
        void ModAPI.IMyInput.GetPressedKeys(List<MyKeys> keys) { GetPressedKeys(keys); }

        #region Key Button States

        //  Return true if new key pressed right now. Don't care if it was pressed in previous update too.
        bool ModAPI.IMyInput.IsKeyPress(MyKeys key) { return IsKeyPress(key); }

        //  Return true if new key was pressed, that means this key was pressed now. During previous Update it wasn't pressed at all.
        bool ModAPI.IMyInput.IsNewKeyPressed(MyKeys key) { return IsNewKeyPressed(key); }

        //  Return true if key was pressed in previous update and now it is not.
        bool ModAPI.IMyInput.IsNewKeyReleased(MyKeys key) { return IsNewKeyReleased(key); }

        #endregion

        #region Mouse Button States

        bool ModAPI.IMyInput.IsMousePressed(MyMouseButtonsEnum button) { return IsMousePressed(button); }

        bool ModAPI.IMyInput.IsMouseReleased(MyMouseButtonsEnum button) { return IsMouseReleased(button); }

        bool ModAPI.IMyInput.IsNewMousePressed(MyMouseButtonsEnum button) { return IsNewMousePressed(button); }

        #endregion

        #region Left Mouse Button States

        //  True if LEFT mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool ModAPI.IMyInput.IsNewLeftMousePressed() { return IsNewLeftMousePressed(); }

        //  True if LEFT mouse is released right now, but previous update wasn't pressed. So this is one-time release.
        bool ModAPI.IMyInput.IsNewLeftMouseReleased() { return IsNewLeftMouseReleased(); }

        //  True if LEFT mouse is pressed right now. Don't care if it was pressed in previous update too.
        bool ModAPI.IMyInput.IsLeftMousePressed() { return IsLeftMousePressed(); }

        //  True if LEFT mouse is released (not pressed) right now. Don't care if it was pressed/released in previous update too.
        bool ModAPI.IMyInput.IsLeftMouseReleased() { return IsLeftMouseReleased(); }

        #endregion

        #region Right Mouse Button states

        //  True if RIGHT mouse is pressed right now. Don't care if it was pressed in previous update too.
        bool ModAPI.IMyInput.IsRightMousePressed() { return IsRightMousePressed(); }

        //  True if RIGHT mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool ModAPI.IMyInput.IsNewRightMousePressed() { return IsNewRightMousePressed(); }

        //  True if RIGHT mouse is released right now, but previous update wasn't pressed. So this is one-time release.
        bool ModAPI.IMyInput.IsNewRightMouseReleased() { return IsNewRightMouseReleased(); }

        bool ModAPI.IMyInput.WasRightMousePressed() { return WasRightMousePressed(); }

        bool ModAPI.IMyInput.WasRightMouseReleased() { return WasRightMouseReleased(); }

        #endregion

        #region Middle Mouse Button States

        //  True if MIDDLE mouse is pressed right now. Don't care if it was pressed in previous update too.
        bool ModAPI.IMyInput.IsMiddleMousePressed() { return IsMiddleMousePressed(); }

        //  True if MIDDLE mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool ModAPI.IMyInput.IsNewMiddleMousePressed() { return IsNewMiddleMousePressed(); }

        //  True if MIDDLE mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool ModAPI.IMyInput.IsNewMiddleMouseReleased() { return IsNewMiddleMouseReleased(); }

        bool ModAPI.IMyInput.WasMiddleMousePressed() { return WasMiddleMousePressed(); }

        bool ModAPI.IMyInput.WasMiddleMouseReleased() { return WasMiddleMouseReleased(); }

        #endregion

        #region XButton1 Mouse Button States

        //  True if XButton1 mouse is pressed right now. Don't care if it was pressed in previous update too.
        bool ModAPI.IMyInput.IsXButton1MousePressed() { return IsXButton1MousePressed(); }

        //  True if XButton1 mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool ModAPI.IMyInput.IsNewXButton1MousePressed() { return IsNewXButton1MousePressed(); }

        bool ModAPI.IMyInput.IsNewXButton1MouseReleased() { return IsNewXButton1MouseReleased(); }

        bool ModAPI.IMyInput.WasXButton1MousePressed() { return WasXButton1MousePressed(); }

        bool ModAPI.IMyInput.WasXButton1MouseReleased() { return WasXButton1MouseReleased(); }

        #endregion

        #region XButton2 Mouse Button States

        //  True if XButton2 mouse is pressed right now. Don't care if it was pressed in previous update too.
        bool ModAPI.IMyInput.IsXButton2MousePressed() { return IsXButton2MousePressed(); }

        //  True if XButton2 mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool ModAPI.IMyInput.IsNewXButton2MousePressed() { return IsNewXButton2MousePressed(); }

        bool ModAPI.IMyInput.IsNewXButton2MouseReleased() { return IsNewXButton2MouseReleased(); }

        bool ModAPI.IMyInput.WasXButton2MousePressed() { return WasXButton2MousePressed(); }

        bool ModAPI.IMyInput.WasXButton2MouseReleased() { return WasXButton2MouseReleased(); }

        #endregion

        #region Joystick button States

        //  Check to see if a specific button on the joystick is pressed.
        bool ModAPI.IMyInput.IsJoystickButtonPressed(MyJoystickButtonsEnum button) { return IsJoystickButtonPressed(button); }

        //  Check to see if a specific button on the joystick is currently pressed and was not pressed during the last update. 
        bool ModAPI.IMyInput.IsJoystickButtonNewPressed(MyJoystickButtonsEnum button) { return IsJoystickButtonNewPressed(button); }

        bool ModAPI.IMyInput.IsNewJoystickButtonReleased(MyJoystickButtonsEnum button) { return IsNewJoystickButtonReleased(button); }

        #endregion

        #region Joystick axis States

        //  Find out how much a specific joystick half-axis is pressed.
        //  Return a number between 0 and 1 (taking deadzone, sensitivity and non-linearity into account).
        float ModAPI.IMyInput.GetJoystickAxisStateForGameplay(MyJoystickAxesEnum axis) { return GetJoystickAxisStateForGameplay(axis); }

        #region Joystick analog axes used for digital controls

        bool ModAPI.IMyInput.IsJoystickAxisPressed(MyJoystickAxesEnum axis) { return IsJoystickAxisPressed(axis); }

        //  Check to see if a specific button on the joystick is currently pressed and was not pressed during the last update. 
        bool ModAPI.IMyInput.IsJoystickAxisNewPressed(MyJoystickAxesEnum axis) { return IsJoystickAxisNewPressed(axis); }

        bool ModAPI.IMyInput.IsNewJoystickAxisReleased(MyJoystickAxesEnum axis) { return IsNewJoystickAxisReleased(axis); }

        #endregion

        #endregion

        #region Mouse and joystick shared states

        bool ModAPI.IMyInput.IsAnyMouseOrJoystickPressed() { return IsAnyMouseOrJoystickPressed(); }
        bool ModAPI.IMyInput.IsAnyNewMouseOrJoystickPressed() { return IsAnyNewMouseOrJoystickPressed(); }
        bool ModAPI.IMyInput.IsNewPrimaryButtonPressed() { return IsNewPrimaryButtonPressed(); }
        bool ModAPI.IMyInput.IsNewSecondaryButtonPressed() { return IsNewSecondaryButtonPressed(); }
        bool ModAPI.IMyInput.IsNewPrimaryButtonReleased() { return IsNewPrimaryButtonReleased(); }
        bool ModAPI.IMyInput.IsNewSecondaryButtonReleased() { return IsNewSecondaryButtonReleased(); }
        bool ModAPI.IMyInput.IsPrimaryButtonReleased() { return IsPrimaryButtonReleased(); }
        bool ModAPI.IMyInput.IsSecondaryButtonReleased() { return IsSecondaryButtonReleased(); }
        bool ModAPI.IMyInput.IsPrimaryButtonPressed() { return IsPrimaryButtonPressed(); }
        bool ModAPI.IMyInput.IsSecondaryButtonPressed() { return IsSecondaryButtonPressed(); }

        bool ModAPI.IMyInput.IsNewButtonPressed(MySharedButtonsEnum button) { return IsNewButtonPressed(button); }
        bool ModAPI.IMyInput.IsButtonPressed(MySharedButtonsEnum button) { return IsButtonPressed(button); }
        bool ModAPI.IMyInput.IsNewButtonReleased(MySharedButtonsEnum button) { return IsNewButtonReleased(button); }
        bool ModAPI.IMyInput.IsButtonReleased(MySharedButtonsEnum button) { return IsButtonReleased(button); }
        #endregion

        //  Current mouse scrollwheel value.
        int ModAPI.IMyInput.MouseScrollWheelValue() { return MouseScrollWheelValue(); }

        //  Previous mouse scrollwheel value.
        int ModAPI.IMyInput.PreviousMouseScrollWheelValue() { return PreviousMouseScrollWheelValue(); }

        //  Delta mouse scrollwheel value.
        int ModAPI.IMyInput.DeltaMouseScrollWheelValue() { return DeltaMouseScrollWheelValue(); }

        int ModAPI.IMyInput.GetMouseXForGamePlay() { return GetMouseXForGamePlay(); }
        int ModAPI.IMyInput.GetMouseYForGamePlay() { return GetMouseYForGamePlay(); }
        int ModAPI.IMyInput.GetMouseX() { return GetMouseX(); }
        int ModAPI.IMyInput.GetMouseY() { return GetMouseY(); }

        bool ModAPI.IMyInput.GetMouseXInversion() { return GetMouseXInversion(); }

        bool ModAPI.IMyInput.GetMouseYInversion() { return GetMouseYInversion(); }

        float ModAPI.IMyInput.GetMouseSensitivity() { return GetMouseSensitivity(); }

        Vector2 ModAPI.IMyInput.GetMousePosition() { return GetMousePosition(); }

        Vector2 ModAPI.IMyInput.GetMouseAreaSize() { return GetMouseAreaSize(); }

        //  Check if an assigned control for game is new pressed.
        bool ModAPI.IMyInput.IsNewGameControlPressed(MyStringId controlEnum) { return IsNewGameControlPressed(controlEnum); }

        //  Check if an assigned control for game is currently pressed.
        bool ModAPI.IMyInput.IsGameControlPressed(MyStringId controlEnum) { return IsGameControlPressed(controlEnum); }

        //  Check if an assigned control for game is new pressed.
        bool ModAPI.IMyInput.IsNewGameControlReleased(MyStringId controlEnum) { return IsNewGameControlReleased(controlEnum); }

        //  Check if an assigned control for game is currently pressed.
        float ModAPI.IMyInput.GetGameControlAnalogState(MyStringId controlEnum) { return GetGameControlAnalogState(controlEnum); }

        //  Check is an assigned game control is released
        bool ModAPI.IMyInput.IsGameControlReleased(MyStringId controlEnum) { return IsGameControlReleased(controlEnum); }

        //  Return true if key is valid for user controls
        bool ModAPI.IMyInput.IsKeyValid(MyKeys key) { return IsKeyValid(key); }

        bool ModAPI.IMyInput.IsKeyDigit(MyKeys key) { return IsKeyDigit(key); }

        //  Return true if mouse button is valid for user controls
        bool ModAPI.IMyInput.IsMouseButtonValid(MyMouseButtonsEnum button) { return IsMouseButtonValid(button); }

        //  Return true if joystick button is valid for user controls
        bool ModAPI.IMyInput.IsJoystickButtonValid(MyJoystickButtonsEnum button) { return IsJoystickButtonValid(button); }

        //  Return true if joystick axis is valid for user controls
        bool ModAPI.IMyInput.IsJoystickAxisValid(MyJoystickAxesEnum axis) { return IsJoystickAxisValid(axis); }

        //  Return true if a joystick was selected in the controller options and it is connected
        bool ModAPI.IMyInput.IsJoystickConnected() { return IsJoystickConnected(); }

        //  Used for enabling cursor movement with joystick analog
        bool ModAPI.IMyInput.JoystickAsMouse { get { return JoystickAsMouse; } }

        // Used for finding out whether joystick has been used last time. Otherwise, it returns false. 
        bool ModAPI.IMyInput.IsJoystickLastUsed { get { return IsJoystickLastUsed; } }

        // event which fires when you select joystick
        event Action<bool> ModAPI.IMyInput.JoystickConnected
        {
            add { JoystickConnected += value; }
            remove { JoystickConnected -= value; }
        }

        //  Return true if key is used by some user control
        ModAPI.IMyControl ModAPI.IMyInput.GetControl(MyKeys key) { return GetControl(key); }

        //  Return true if mouse button is used by some user control
        ModAPI.IMyControl ModAPI.IMyInput.GetControl(MyMouseButtonsEnum button) { return GetControl(button); }

        void ModAPI.IMyInput.GetListOfPressedKeys(List<MyKeys> keys) { GetListOfPressedKeys(keys); }

        void ModAPI.IMyInput.GetListOfPressedMouseButtons(List<MyMouseButtonsEnum> result) { GetListOfPressedMouseButtons(result); }

        //  Returns an array ModAPI.IMyControl that contains every assigned control for game.
        //DictionaryValuesReader<MyStringId, ModAPI.IMyControl> ModAPI.IMyInput.GetGameControlsList() { return GetGameControlsList(); }

        ModAPI.IMyControl ModAPI.IMyInput.GetGameControl(MyStringId controlEnum) { return GetGameControl(controlEnum); }

        string ModAPI.IMyInput.GetKeyName(MyKeys key) { return GetKeyName(key); }
        string ModAPI.IMyInput.GetName(MyMouseButtonsEnum mouseButton) { return GetName(mouseButton); }
        string ModAPI.IMyInput.GetName(MyJoystickButtonsEnum joystickButton) { return GetName(joystickButton); }
        string ModAPI.IMyInput.GetName(MyJoystickAxesEnum joystickAxis) { return GetName(joystickAxis); }
        string ModAPI.IMyInput.GetUnassignedName() { return GetUnassignedName(); }
    }
}
