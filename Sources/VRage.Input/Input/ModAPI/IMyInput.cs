using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace VRage.ModAPI
{
    public interface IMyInput
    {
        bool IsCapsLock { get; }

        string JoystickInstanceName { get; }

        ListReader<char> TextInput { get; }

        List<string> EnumerateJoystickNames();

        //  Return true if ANY key IS pressed, that means that the key was pressed now. During previous Update it wasn't pressed at all.
        bool IsAnyKeyPress();

        //  Return true if ANY mouse key IS pressed.
        bool IsAnyMousePressed();

        bool IsAnyNewMousePressed();

        //True if any SHIFT key is pressed
        bool IsAnyShiftKeyPressed();

        //True if any ALT key is pressed
        bool IsAnyAltKeyPressed();

        //True if any CTRL key is pressed
        bool IsAnyCtrlKeyPressed();

        //  Gets an array of values that correspond to the keyboard keys that are currently
        //  being pressed. Reference page contains links to related code samples.
        void GetPressedKeys(List<MyKeys> keys);

        #region Key Button States

        //  Return true if new key pressed right now. Don't care if it was pressed in previous update too.
        bool IsKeyPress(MyKeys key);

        //  Return true if new key was pressed, that means this key was pressed now. During previous Update it wasn't pressed at all.
        bool IsNewKeyPressed(MyKeys key);

        //  Return true if key was pressed in previous update and now it is not.
        bool IsNewKeyReleased(MyKeys key);

        #endregion

        #region Mouse Button States

        bool IsMousePressed(MyMouseButtonsEnum button);

        bool IsMouseReleased(MyMouseButtonsEnum button);

        bool IsNewMousePressed(MyMouseButtonsEnum button);

        #endregion

        #region Left Mouse Button States

        //  True if LEFT mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool IsNewLeftMousePressed();

        //  True if LEFT mouse is released right now, but previous update wasn't pressed. So this is one-time release.
        bool IsNewLeftMouseReleased();

        //  True if LEFT mouse is pressed right now. Don't care if it was pressed in previous update too.
        bool IsLeftMousePressed();

        //  True if LEFT mouse is released (not pressed) right now. Don't care if it was pressed/released in previous update too.
        bool IsLeftMouseReleased();

        #endregion

        #region Right Mouse Button states

        //  True if RIGHT mouse is pressed right now. Don't care if it was pressed in previous update too.
        bool IsRightMousePressed();

        //  True if RIGHT mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool IsNewRightMousePressed();

        //  True if RIGHT mouse is released right now, but previous update wasn't pressed. So this is one-time release.
        bool IsNewRightMouseReleased();

        bool WasRightMousePressed();

        bool WasRightMouseReleased();

        #endregion

        #region Middle Mouse Button States

        //  True if MIDDLE mouse is pressed right now. Don't care if it was pressed in previous update too.
        bool IsMiddleMousePressed();

        //  True if MIDDLE mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool IsNewMiddleMousePressed();

        //  True if MIDDLE mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool IsNewMiddleMouseReleased();

        bool WasMiddleMousePressed();

        bool WasMiddleMouseReleased();

        #endregion

        #region XButton1 Mouse Button States

        //  True if XButton1 mouse is pressed right now. Don't care if it was pressed in previous update too.
        bool IsXButton1MousePressed();

        //  True if XButton1 mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool IsNewXButton1MousePressed();

        bool IsNewXButton1MouseReleased();

        bool WasXButton1MousePressed();

        bool WasXButton1MouseReleased();

        #endregion

        #region XButton2 Mouse Button States

        //  True if XButton2 mouse is pressed right now. Don't care if it was pressed in previous update too.
        bool IsXButton2MousePressed();

        //  True if XButton2 mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        bool IsNewXButton2MousePressed();

        bool IsNewXButton2MouseReleased();

        bool WasXButton2MousePressed();

        bool WasXButton2MouseReleased();

        #endregion

        #region Joystick button States

        //  Check to see if a specific button on the joystick is pressed.
        bool IsJoystickButtonPressed(MyJoystickButtonsEnum button);

        //  Check to see if a specific button on the joystick is currently pressed and was not pressed during the last update. 
        bool IsJoystickButtonNewPressed(MyJoystickButtonsEnum button);

        bool IsNewJoystickButtonReleased(MyJoystickButtonsEnum button);

        #endregion

        #region Joystick axis States

        //  Find out how much a specific joystick half-axis is pressed.
        //  Return a number between 0 and 1 (taking deadzone, sensitivity and non-linearity into account).
        float GetJoystickAxisStateForGameplay(MyJoystickAxesEnum axis);

        #region Joystick analog axes used for digital controls

        bool IsJoystickAxisPressed(MyJoystickAxesEnum axis);

        //  Check to see if a specific button on the joystick is currently pressed and was not pressed during the last update. 
        bool IsJoystickAxisNewPressed(MyJoystickAxesEnum axis);

        bool IsNewJoystickAxisReleased(MyJoystickAxesEnum axis);

        #endregion

        #endregion

        #region Mouse and joystick shared states

        bool IsAnyMouseOrJoystickPressed();
        bool IsAnyNewMouseOrJoystickPressed();
        bool IsNewPrimaryButtonPressed();
        bool IsNewSecondaryButtonPressed();
        bool IsNewPrimaryButtonReleased();
        bool IsNewSecondaryButtonReleased();
        bool IsPrimaryButtonReleased();
        bool IsSecondaryButtonReleased();
        bool IsPrimaryButtonPressed();
        bool IsSecondaryButtonPressed();

        bool IsNewButtonPressed(MySharedButtonsEnum button);
        bool IsButtonPressed(MySharedButtonsEnum button);
        bool IsNewButtonReleased(MySharedButtonsEnum button);
        bool IsButtonReleased(MySharedButtonsEnum button);
        #endregion

        //  Current mouse scrollwheel value.
        int MouseScrollWheelValue();

        //  Previous mouse scrollwheel value.
        int PreviousMouseScrollWheelValue();

        //  Delta mouse scrollwheel value.
        int DeltaMouseScrollWheelValue();

        int GetMouseXForGamePlay();
        int GetMouseYForGamePlay();
        int GetMouseX();
        int GetMouseY();

        bool GetMouseXInversion();

        bool GetMouseYInversion();

        float GetMouseSensitivity();

        Vector2 GetMousePosition();

        Vector2 GetMouseAreaSize();

        //  Check if an assigned control for game is new pressed.
        bool IsNewGameControlPressed(MyStringId controlEnum);

        //  Check if an assigned control for game is currently pressed.
        bool IsGameControlPressed(MyStringId controlEnum);

        //  Check if an assigned control for game is new pressed.
        bool IsNewGameControlReleased(MyStringId controlEnum);

        //  Check if an assigned control for game is currently pressed.
        float GetGameControlAnalogState(MyStringId controlEnum);

        //  Check is an assigned game control is released
        bool IsGameControlReleased(MyStringId controlEnum);

        //  Return true if key is valid for user controls
        bool IsKeyValid(MyKeys key);

        bool IsKeyDigit(MyKeys key);

        //  Return true if mouse button is valid for user controls
        bool IsMouseButtonValid(MyMouseButtonsEnum button);

        //  Return true if joystick button is valid for user controls
        bool IsJoystickButtonValid(MyJoystickButtonsEnum button);

        //  Return true if joystick axis is valid for user controls
        bool IsJoystickAxisValid(MyJoystickAxesEnum axis);

        //  Return true if a joystick was selected in the controller options and it is connected
        bool IsJoystickConnected();

        //  Used for enabling cursor movement with joystick analog
        bool JoystickAsMouse { get; }

        // Used for finding out whether joystick has been used last time. Otherwise, it returns false. 
        bool IsJoystickLastUsed { get; }

        // event which fires when you select joystick
        event Action<bool> JoystickConnected;

        //  Return true if key is used by some user control
        IMyControl GetControl(MyKeys key);

        //  Return true if mouse button is used by some user control
        IMyControl GetControl(MyMouseButtonsEnum button);

        void GetListOfPressedKeys(List<MyKeys> keys);

        void GetListOfPressedMouseButtons(List<MyMouseButtonsEnum> result);

        //  Returns an array IMyControl that contains every assigned control for game.
        //DictionaryValuesReader<MyStringId, IMyControl> GetGameControlsList();

        IMyControl GetGameControl(MyStringId controlEnum);

        string GetKeyName(MyKeys key);
        string GetName(MyMouseButtonsEnum mouseButton);
        string GetName(MyJoystickButtonsEnum joystickButton);
        string GetName(MyJoystickAxesEnum joystickAxis);
        string GetUnassignedName();
    }
}
