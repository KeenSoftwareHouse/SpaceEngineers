using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Utils;
using VRageMath;

namespace VRage.Input
{
    public partial class MyNullInput : VRage.ModAPI.IMyInput
    {
        bool ModAPI.IMyInput.IsCapsLock { get { return ((IMyInput)this).IsCapsLock; } }

        string ModAPI.IMyInput.JoystickInstanceName { get { return ((IMyInput)this).JoystickInstanceName; } }

        ListReader<char> ModAPI.IMyInput.TextInput { get { return ((IMyInput)this).TextInput; } }

        List<string> ModAPI.IMyInput.EnumerateJoystickNames() { return ((IMyInput)this).EnumerateJoystickNames(); }

        //  Return true if ANY key IS pressed, that means that the key was pressed now. During previous Update it wasn't pressed at all.
        bool ModAPI.IMyInput.IsAnyKeyPress() { return ((IMyInput)this).IsAnyKeyPress();  }

        //  Return true if ANY mouse key IS pressed.
        bool ModAPI.IMyInput.IsAnyMousePressed() { return ((IMyInput)this).IsAnyMousePressed(); }

        bool ModAPI.IMyInput.IsAnyNewMousePressed() { return ((IMyInput)this).IsAnyNewMousePressed(); }

        //True if any SHIFT key is pressed
        bool ModAPI.IMyInput.IsAnyShiftKeyPressed() { return ((IMyInput)this).IsAnyShiftKeyPressed(); }

        //True if any ALT key is pressed
        bool ModAPI.IMyInput.IsAnyAltKeyPressed() { return ((IMyInput)this).IsAnyAltKeyPressed(); }

        //True if any CTRL key is pressed
        bool ModAPI.IMyInput.IsAnyCtrlKeyPressed() { return ((IMyInput)this).IsAnyCtrlKeyPressed(); }

        //  Gets an array of values that correspond to the keyboard keys that are currently
        //  being pressed. Reference page contains links to related code samples.
        void ModAPI.IMyInput.GetPressedKeys(List<MyKeys> keys) { ((IMyInput)this).GetPressedKeys(keys); }

        public void AddDefaultControl(MyStringId stringId, MyControl control) { }

        #region Key Button States

        //  Return true if new key pressed right now. Don't care if it was pressed in previous update too.
        bool ModAPI.IMyInput.IsKeyPress(MyKeys key) { return ((IMyInput)this).IsKeyPress(key); }

        //  Return true if new key was pressed, that means this key was pressed now. During previous Update it wasn't pressed at all.
        bool ModAPI.IMyInput.IsNewKeyPressed(MyKeys key) { return ((IMyInput)this).IsNewKeyPressed(key); }

        //  Return true if key was pressed in previous update and now it is not.
        bool ModAPI.IMyInput.IsNewKeyReleased(MyKeys key) { return ((IMyInput)this).IsNewKeyReleased(key); }

        #endregion

        #region Mouse Button States

        bool ModAPI.IMyInput.IsMousePressed(MyMouseButtonsEnum button) { return ((IMyInput)this).IsMousePressed(button); }

        bool ModAPI.IMyInput.IsMouseReleased(MyMouseButtonsEnum button) { return ((IMyInput)this).IsMouseReleased(button); }

        bool ModAPI.IMyInput.IsNewMousePressed(MyMouseButtonsEnum button) { return ((IMyInput)this).IsNewMousePressed(button); }

        #endregion

        #region Left Mouse Button States

        //  True if LEFT mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool ModAPI.IMyInput.IsNewLeftMousePressed() { return ((IMyInput)this).IsNewLeftMousePressed(); }

        //  True if LEFT mouse is released right now, but previous update wasn't pressed. So this is one-time release.
        bool ModAPI.IMyInput.IsNewLeftMouseReleased() { return ((IMyInput)this).IsNewLeftMouseReleased(); }

        //  True if LEFT mouse is pressed right now. Don't care if it was pressed in previous update too.
        bool ModAPI.IMyInput.IsLeftMousePressed() { return ((IMyInput)this).IsLeftMousePressed(); }

        //  True if LEFT mouse is released (not pressed) right now. Don't care if it was pressed/released in previous update too.
        bool ModAPI.IMyInput.IsLeftMouseReleased() { return ((IMyInput)this).IsLeftMouseReleased(); }

        #endregion

        #region Right Mouse Button states

        //  True if RIGHT mouse is pressed right now. Don't care if it was pressed in previous update too.
        bool ModAPI.IMyInput.IsRightMousePressed() { return ((IMyInput)this).IsRightMousePressed(); }

        //  True if RIGHT mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool ModAPI.IMyInput.IsNewRightMousePressed() { return ((IMyInput)this).IsNewRightMousePressed(); }

        //  True if RIGHT mouse is released right now, but previous update wasn't pressed. So this is one-time release.
        bool ModAPI.IMyInput.IsNewRightMouseReleased() { return ((IMyInput)this).IsNewRightMouseReleased(); }

        bool ModAPI.IMyInput.WasRightMousePressed() { return ((IMyInput)this).WasRightMousePressed(); }

        bool ModAPI.IMyInput.WasRightMouseReleased() { return ((IMyInput)this).WasRightMouseReleased(); }

        #endregion

        #region Middle Mouse Button States

        //  True if MIDDLE mouse is pressed right now. Don't care if it was pressed in previous update too.
        bool ModAPI.IMyInput.IsMiddleMousePressed() { return ((IMyInput)this).IsMiddleMousePressed(); }

        //  True if MIDDLE mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool ModAPI.IMyInput.IsNewMiddleMousePressed() { return ((IMyInput)this).IsNewMiddleMousePressed(); }

        //  True if MIDDLE mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool ModAPI.IMyInput.IsNewMiddleMouseReleased() { return ((IMyInput)this).IsNewMiddleMouseReleased(); }

        bool ModAPI.IMyInput.WasMiddleMousePressed() { return ((IMyInput)this).WasMiddleMousePressed(); }

        bool ModAPI.IMyInput.WasMiddleMouseReleased() { return ((IMyInput)this).WasMiddleMouseReleased(); }

        #endregion

        #region XButton1 Mouse Button States

        //  True if XButton1 mouse is pressed right now. Don't care if it was pressed in previous update too.
        bool ModAPI.IMyInput.IsXButton1MousePressed() { return ((IMyInput)this).IsXButton1MousePressed(); }

        //  True if XButton1 mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool ModAPI.IMyInput.IsNewXButton1MousePressed() { return ((IMyInput)this).IsNewXButton1MousePressed(); }

        bool ModAPI.IMyInput.IsNewXButton1MouseReleased() { return ((IMyInput)this).IsNewXButton1MouseReleased(); }

        bool ModAPI.IMyInput.WasXButton1MousePressed() { return ((IMyInput)this).WasXButton1MousePressed(); }

        bool ModAPI.IMyInput.WasXButton1MouseReleased() { return ((IMyInput)this).WasXButton1MouseReleased(); }

        #endregion

        #region XButton2 Mouse Button States

        //  True if XButton2 mouse is pressed right now. Don't care if it was pressed in previous update too.
        bool ModAPI.IMyInput.IsXButton2MousePressed() { return ((IMyInput)this).IsXButton2MousePressed(); }

        //  True if XButton2 mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool ModAPI.IMyInput.IsNewXButton2MousePressed() { return ((IMyInput)this).IsNewXButton2MousePressed(); }

        bool ModAPI.IMyInput.IsNewXButton2MouseReleased() { return ((IMyInput)this).IsNewXButton2MouseReleased(); }

        bool ModAPI.IMyInput.WasXButton2MousePressed() { return ((IMyInput)this).WasXButton2MousePressed(); }

        bool ModAPI.IMyInput.WasXButton2MouseReleased() { return ((IMyInput)this).WasXButton2MouseReleased(); }

        #endregion

        #region Joystick button States

        //  Check to see if a specific button on the joystick is pressed.
        bool ModAPI.IMyInput.IsJoystickButtonPressed(MyJoystickButtonsEnum button) { return ((IMyInput)this).IsJoystickButtonPressed(button); }

        //  Check to see if a specific button on the joystick is currently pressed and was not pressed during the last update. 
        bool ModAPI.IMyInput.IsJoystickButtonNewPressed(MyJoystickButtonsEnum button) { return ((IMyInput)this).IsJoystickButtonNewPressed(button); }

        bool ModAPI.IMyInput.IsNewJoystickButtonReleased(MyJoystickButtonsEnum button) { return ((IMyInput)this).IsNewJoystickButtonReleased(button); }

        #endregion

        #region Joystick axis States

        //  Find out how much a specific joystick half-axis is pressed.
        //  Return a number between 0 and 1 (taking deadzone, sensitivity and non-linearity into account).
        float ModAPI.IMyInput.GetJoystickAxisStateForGameplay(MyJoystickAxesEnum axis) { return ((IMyInput)this).GetJoystickAxisStateForGameplay(axis); }

        #region Joystick analog axes used for digital controls

        bool ModAPI.IMyInput.IsJoystickAxisPressed(MyJoystickAxesEnum axis) { return ((IMyInput)this).IsJoystickAxisPressed(axis); }

        //  Check to see if a specific button on the joystick is currently pressed and was not pressed during the last update. 
        bool ModAPI.IMyInput.IsJoystickAxisNewPressed(MyJoystickAxesEnum axis) { return ((IMyInput)this).IsJoystickAxisNewPressed(axis); }

        bool ModAPI.IMyInput.IsNewJoystickAxisReleased(MyJoystickAxesEnum axis) { return ((IMyInput)this).IsNewJoystickAxisReleased(axis); }

        #endregion

        #endregion

        #region Mouse and joystick shared states

        bool ModAPI.IMyInput.IsAnyMouseOrJoystickPressed() { return ((IMyInput)this).IsAnyMouseOrJoystickPressed(); }
        bool ModAPI.IMyInput.IsAnyNewMouseOrJoystickPressed() { return ((IMyInput)this).IsAnyNewMouseOrJoystickPressed(); }
        bool ModAPI.IMyInput.IsNewPrimaryButtonPressed() { return ((IMyInput)this).IsNewPrimaryButtonPressed(); }
        bool ModAPI.IMyInput.IsNewSecondaryButtonPressed() { return ((IMyInput)this).IsNewSecondaryButtonPressed(); }
        bool ModAPI.IMyInput.IsNewPrimaryButtonReleased() { return ((IMyInput)this).IsNewPrimaryButtonReleased(); }
        bool ModAPI.IMyInput.IsNewSecondaryButtonReleased() { return ((IMyInput)this).IsNewSecondaryButtonReleased(); }
        bool ModAPI.IMyInput.IsPrimaryButtonReleased() { return ((IMyInput)this).IsPrimaryButtonReleased(); }
        bool ModAPI.IMyInput.IsSecondaryButtonReleased() { return ((IMyInput)this).IsSecondaryButtonReleased(); }
        bool ModAPI.IMyInput.IsPrimaryButtonPressed() { return ((IMyInput)this).IsPrimaryButtonPressed(); }
        bool ModAPI.IMyInput.IsSecondaryButtonPressed() { return ((IMyInput)this).IsSecondaryButtonPressed(); }

        bool ModAPI.IMyInput.IsNewButtonPressed(MySharedButtonsEnum button) { return ((IMyInput)this).IsNewButtonPressed(button); }
        bool ModAPI.IMyInput.IsButtonPressed(MySharedButtonsEnum button) { return ((IMyInput)this).IsButtonPressed(button); }
        bool ModAPI.IMyInput.IsNewButtonReleased(MySharedButtonsEnum button) { return ((IMyInput)this).IsNewButtonReleased(button); }
        bool ModAPI.IMyInput.IsButtonReleased(MySharedButtonsEnum button) { return ((IMyInput)this).IsButtonReleased(button); }
        #endregion

        //  Current mouse scrollwheel value.
        int ModAPI.IMyInput.MouseScrollWheelValue() { return ((IMyInput)this).MouseScrollWheelValue(); }

        //  Previous mouse scrollwheel value.
        int ModAPI.IMyInput.PreviousMouseScrollWheelValue() { return ((IMyInput)this).PreviousMouseScrollWheelValue(); }

        //  Delta mouse scrollwheel value.
        int ModAPI.IMyInput.DeltaMouseScrollWheelValue() { return ((IMyInput)this).DeltaMouseScrollWheelValue(); }

        int ModAPI.IMyInput.GetMouseXForGamePlay() { return ((IMyInput)this).GetMouseXForGamePlay(); }
        int ModAPI.IMyInput.GetMouseYForGamePlay() { return ((IMyInput)this).GetMouseYForGamePlay(); }
        int ModAPI.IMyInput.GetMouseX() { return ((IMyInput)this).GetMouseX(); }
        int ModAPI.IMyInput.GetMouseY() { return ((IMyInput)this).GetMouseY(); }

        bool ModAPI.IMyInput.GetMouseXInversion() { return ((IMyInput)this).GetMouseXInversion(); }

        bool ModAPI.IMyInput.GetMouseYInversion() { return ((IMyInput)this).GetMouseYInversion(); }

        float ModAPI.IMyInput.GetMouseSensitivity() { return ((IMyInput)this).GetMouseSensitivity(); }

        Vector2 ModAPI.IMyInput.GetMousePosition() { return ((IMyInput)this).GetMousePosition(); }

        Vector2 ModAPI.IMyInput.GetMouseAreaSize() { return ((IMyInput)this).GetMouseAreaSize(); }

        //  Check if an assigned control for game is new pressed.
        bool ModAPI.IMyInput.IsNewGameControlPressed(MyStringId controlEnum) { return ((IMyInput)this).IsNewGameControlPressed(controlEnum); }

        //  Check if an assigned control for game is currently pressed.
        bool ModAPI.IMyInput.IsGameControlPressed(MyStringId controlEnum) { return ((IMyInput)this).IsGameControlPressed(controlEnum); }

        //  Check if an assigned control for game is new pressed.
        bool ModAPI.IMyInput.IsNewGameControlReleased(MyStringId controlEnum) { return ((IMyInput)this).IsNewGameControlReleased(controlEnum); }

        //  Check if an assigned control for game is currently pressed.
        float ModAPI.IMyInput.GetGameControlAnalogState(MyStringId controlEnum) { return ((IMyInput)this).GetGameControlAnalogState(controlEnum); }

        //  Check is an assigned game control is released
        bool ModAPI.IMyInput.IsGameControlReleased(MyStringId controlEnum) { return ((IMyInput)this).IsGameControlReleased(controlEnum); }

        //  Return true if key is valid for user controls
        bool ModAPI.IMyInput.IsKeyValid(MyKeys key) { return ((IMyInput)this).IsKeyValid(key); }

        bool ModAPI.IMyInput.IsKeyDigit(MyKeys key) { return ((IMyInput)this).IsKeyDigit(key); }

        //  Return true if mouse button is valid for user controls
        bool ModAPI.IMyInput.IsMouseButtonValid(MyMouseButtonsEnum button) { return ((IMyInput)this).IsMouseButtonValid(button); }

        //  Return true if joystick button is valid for user controls
        bool ModAPI.IMyInput.IsJoystickButtonValid(MyJoystickButtonsEnum button) { return ((IMyInput)this).IsJoystickButtonValid(button); }

        //  Return true if joystick axis is valid for user controls
        bool ModAPI.IMyInput.IsJoystickAxisValid(MyJoystickAxesEnum axis) { return ((IMyInput)this).IsJoystickAxisValid(axis); }

        //  Return true if a joystick was selected in the controller options and it is connected
        bool ModAPI.IMyInput.IsJoystickConnected() { return ((IMyInput)this).IsJoystickConnected(); }

        //  Used for enabling cursor movement with joystick analog
        bool ModAPI.IMyInput.JoystickAsMouse { get { return ((IMyInput)this).JoystickAsMouse; } }

        // Used for finding out whether joystick has been used last time. Otherwise, it returns false. 
        bool ModAPI.IMyInput.IsJoystickLastUsed { get { return ((IMyInput)this).IsJoystickLastUsed; } }

        // event which fires when you select joystick
        event Action<bool> ModAPI.IMyInput.JoystickConnected
        {
            add { ((IMyInput)this).JoystickConnected += value; }
            remove { ((IMyInput)this).JoystickConnected -= value; }
        }

        //  Return true if key is used by some user control
        ModAPI.IMyControl ModAPI.IMyInput.GetControl(MyKeys key) { return ((IMyInput)this).GetControl(key); }

        //  Return true if mouse button is used by some user control
        ModAPI.IMyControl ModAPI.IMyInput.GetControl(MyMouseButtonsEnum button) { return ((IMyInput)this).GetControl(button); }

        void ModAPI.IMyInput.GetListOfPressedKeys(List<MyKeys> keys) { ((IMyInput)this).GetListOfPressedKeys(keys); }

        void ModAPI.IMyInput.GetListOfPressedMouseButtons(List<MyMouseButtonsEnum> result) { ((IMyInput)this).GetListOfPressedMouseButtons(result); }

        //  Returns an array ModAPI.IMyControl that contains every assigned control for game.
        //DictionaryValuesReader<MyStringId, ModAPI.IMyControl> ModAPI.IMyInput.GetGameControlsList() { return ((IMyInput)this).GetGameControlsList(); }

        ModAPI.IMyControl ModAPI.IMyInput.GetGameControl(MyStringId controlEnum) { return ((IMyInput)this).GetGameControl(controlEnum); }

        string ModAPI.IMyInput.GetKeyName(MyKeys key) { return ((IMyInput)this).GetKeyName(key); }
        string ModAPI.IMyInput.GetName(MyMouseButtonsEnum mouseButton) { return ((IMyInput)this).GetName(mouseButton); }
        string ModAPI.IMyInput.GetName(MyJoystickButtonsEnum joystickButton) { return ((IMyInput)this).GetName(joystickButton); }
        string ModAPI.IMyInput.GetName(MyJoystickAxesEnum joystickAxis) { return ((IMyInput)this).GetName(joystickAxis); }
        string ModAPI.IMyInput.GetUnassignedName() { return ((IMyInput)this).GetUnassignedName(); }
    }
}
