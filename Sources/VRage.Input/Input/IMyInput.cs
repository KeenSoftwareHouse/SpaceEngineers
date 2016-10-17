using System;
using System.Collections.Generic;
using System.Text;
using VRage.Collections;
using VRage.Library.Utils;
using VRage.Serialization;
using VRage.Utils;
using VRageMath;

namespace VRage.Input
{
    public interface IMyInput 
    {
        bool IsCapsLock { get; }

        string JoystickInstanceName
        {
            get;
            set;
        }

        void LoadData(SerializableDictionary<string, object> controlsGeneral, SerializableDictionary<string, object> controlsButtons);
#if !XB1
        void LoadContent(IntPtr windowHandle);

        IntPtr WindowHandle
        {
            get; 
        }
#else // XB1
        void LoadContent();
#endif // XB1

        ListReader<char> TextInput { get; }

        void UnloadData();

        List<string> EnumerateJoystickNames();

        bool Update(bool gameFocused);

        // Whitelists/Blacklists given control
        void SetControlBlock(MyStringId controlEnum, bool block = false);

        // Retruns true if the control was blacklisted
        bool IsControlBlocked(MyStringId controlEnum);

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

        #region Joystick settings

        float GetJoystickSensitivity();

        void SetJoystickSensitivity(float newSensitivity);

        float GetJoystickExponent();

        void SetJoystickExponent(float newExponent);

        float GetJoystickDeadzone();

        void SetJoystickDeadzone(float newDeadzone);

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

        void SetMouseXInversion(bool inverted);

        void SetMouseYInversion(bool inverted);

        float GetMouseSensitivity();

        void SetMouseSensitivity(float sensitivity);

        Vector2 GetMousePosition();

        Vector2 GetMouseAreaSize();

        void SetMousePosition(int x, int y);

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
        bool JoystickAsMouse { get; set; }

        // Used for finding out whether joystick has been used last time. Otherwise, it returns false. 
        bool IsJoystickLastUsed { get; }

        // event which fires when you select joystick
        event Action<bool> JoystickConnected;

        //  Return true if key is used by some user control
        MyControl GetControl(MyKeys key);

        //  Return true if mouse button is used by some user control
        MyControl GetControl(MyMouseButtonsEnum button);

        void GetListOfPressedKeys(List<MyKeys> keys);

        void GetListOfPressedMouseButtons(List<MyMouseButtonsEnum> result);

        //  Returns an array MyControl that contains every assigned control for game.
        DictionaryValuesReader<MyStringId, MyControl> GetGameControlsList();

        //  IMPORTANT! Use this function before attempting to assign new controls so that the controls can be re-set if the user does not like the changes.
        void TakeSnapshot();

        //  IMPORTANT! Only call this method after calling TakeSnapshot() to revert any changes made since TakeSnapshot() was last called. 
        void RevertChanges();

        MyControl GetGameControl(MyStringId controlEnum);

        void RevertToDefaultControls();

        void AddDefaultControl(MyStringId stringId, MyControl control);

        //  Save all controls to the Config File.
        void SaveControls(SerializableDictionary<string, object> controlsGeneral, SerializableDictionary<string, object> controlsButtons);

        bool ENABLE_DEVELOPER_KEYS
        {
            get;
        }

        string GetKeyName(MyKeys key);
        string GetName(MyMouseButtonsEnum mouseButton);
        string GetName(MyJoystickButtonsEnum joystickButton);
        string GetName(MyJoystickAxesEnum joystickAxis);
        string GetUnassignedName();

        // mk:TODO Delete all of the following once not used.
        bool IsGamepadKeyRightPressed();
        bool IsGamepadKeyLeftPressed();
        bool IsNewGamepadKeyDownPressed();
        bool IsNewGamepadKeyUpPressed();
        void GetActualJoystickState(StringBuilder text);
        bool IsNewGameControlJoystickOnlyPressed(MyStringId controlId);
    }
}
