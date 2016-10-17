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
    public partial class MyNullInput : IMyInput
    {
        private MyControl m_nullControl = new MyControl(
            controlId: default(MyStringId),
            name: default(MyStringId),
            controlType: MyGuiControlTypeEnum.General,
            defaultControlMouse: null,
            defaultControlKey: null);

        bool IMyInput.IsCapsLock { get { return false; } }

        string IMyInput.JoystickInstanceName { get { return ""; } set { } }

        void IMyInput.LoadData(SerializableDictionary<string, object> controlsGeneral, SerializableDictionary<string, object> controlsButtons) { }
#if !XB1
        void IMyInput.LoadContent(IntPtr windowHandle) { }
        IntPtr IMyInput.WindowHandle { get { return IntPtr.Zero; } }
#else // XB1
        void IMyInput.LoadContent() { }
#endif // XB1
        ListReader<char> IMyInput.TextInput { get { return new ListReader<char>(); } }
        void IMyInput.UnloadData() { }
        List<string> IMyInput.EnumerateJoystickNames() { return new List<string>(); }
        bool IMyInput.Update(bool gameFocused) { return false; }

        public void SetControlBlock(MyStringId controlEnum, bool block = false){}
        public bool IsControlBlocked(MyStringId controlEnum){ return false; }

        bool IMyInput.IsAnyKeyPress() { return false; }
        bool IMyInput.IsAnyMousePressed() { return false; }
        bool IMyInput.IsAnyNewMousePressed() { return false; }
        bool IMyInput.IsAnyShiftKeyPressed() { return false; }
        bool IMyInput.IsAnyAltKeyPressed() { return false; }
        bool IMyInput.IsAnyCtrlKeyPressed() { return false; }
        void IMyInput.GetPressedKeys(List<MyKeys> keys) { }

        bool IMyInput.IsKeyPress(MyKeys key) { return false; }
        bool IMyInput.IsNewKeyPressed(MyKeys key) { return false; }
        bool IMyInput.IsNewKeyReleased(MyKeys key) { return false; }

        bool IMyInput.IsMousePressed(MyMouseButtonsEnum button) { return false; }
        bool IMyInput.IsMouseReleased(MyMouseButtonsEnum button) { return false; }
        bool IMyInput.IsNewMousePressed(MyMouseButtonsEnum button) { return false; }

        bool IMyInput.IsNewLeftMousePressed() { return false; }
        bool IMyInput.IsNewLeftMouseReleased() { return false; }
        bool IMyInput.IsLeftMousePressed() { return false; }
        bool IMyInput.IsLeftMouseReleased() { return false; }

        bool IMyInput.IsRightMousePressed() { return false; }
        bool IMyInput.IsNewRightMousePressed() { return false; }
        bool IMyInput.IsNewRightMouseReleased() { return false; }
        bool IMyInput.WasRightMousePressed() { return false; }
        bool IMyInput.WasRightMouseReleased() { return false; }

        bool IMyInput.IsMiddleMousePressed() { return false; }
        bool IMyInput.IsNewMiddleMousePressed() { return false; }
        bool IMyInput.IsNewMiddleMouseReleased() { return false; }
        bool IMyInput.WasMiddleMousePressed() { return false; }
        bool IMyInput.WasMiddleMouseReleased() { return false; }

        bool IMyInput.IsXButton1MousePressed() { return false; }
        bool IMyInput.IsNewXButton1MousePressed() { return false; }
        bool IMyInput.IsNewXButton1MouseReleased() { return false; }
        bool IMyInput.WasXButton1MousePressed() { return false; }
        bool IMyInput.WasXButton1MouseReleased() { return false; }

        bool IMyInput.IsXButton2MousePressed() { return false; }
        bool IMyInput.IsNewXButton2MousePressed() { return false; }
        bool IMyInput.IsNewXButton2MouseReleased() { return false; }
        bool IMyInput.WasXButton2MousePressed() { return false; }
        bool IMyInput.WasXButton2MouseReleased() { return false; }

        bool IMyInput.IsJoystickButtonPressed(MyJoystickButtonsEnum button) { return false; }
        bool IMyInput.IsJoystickButtonNewPressed(MyJoystickButtonsEnum button) { return false; }
        bool IMyInput.IsNewJoystickButtonReleased(MyJoystickButtonsEnum button) { return false; }

        float IMyInput.GetJoystickAxisStateForGameplay(MyJoystickAxesEnum axis) { return 0; }
        bool IMyInput.IsJoystickAxisPressed(MyJoystickAxesEnum axis) { return false; }
        bool IMyInput.IsJoystickAxisNewPressed(MyJoystickAxesEnum axis) { return false; }
        bool IMyInput.IsNewJoystickAxisReleased(MyJoystickAxesEnum axis) { return false; }
        bool IMyInput.IsNewGameControlJoystickOnlyPressed(MyStringId controlId) { return false; }

        float IMyInput.GetJoystickSensitivity() { return 0; }
        void IMyInput.SetJoystickSensitivity(float newSensitivity) { }
        float IMyInput.GetJoystickExponent() { return 0; }
        void IMyInput.SetJoystickExponent(float newExponent) { }
        float IMyInput.GetJoystickDeadzone() { return 0; }
        void IMyInput.SetJoystickDeadzone(float newDeadzone) { }

        int IMyInput.MouseScrollWheelValue() { return 0; }
        int IMyInput.PreviousMouseScrollWheelValue() { return 0; }
        int IMyInput.DeltaMouseScrollWheelValue() { return 0; }
        int IMyInput.GetMouseXForGamePlay() { return 0; }
        int IMyInput.GetMouseYForGamePlay() { return 0; }
        int IMyInput.GetMouseX() { return 0; }
        int IMyInput.GetMouseY() { return 0; }
        bool IMyInput.GetMouseXInversion() { return false; }
        bool IMyInput.GetMouseYInversion() { return false; }
        void IMyInput.SetMouseXInversion(bool inverted) { }
        void IMyInput.SetMouseYInversion(bool inverted) { }
        float IMyInput.GetMouseSensitivity() { return 0; }
        void IMyInput.SetMouseSensitivity(float sensitivity) { }
        Vector2 IMyInput.GetMousePosition() { return Vector2.Zero; }
        void IMyInput.SetMousePosition(int x, int y) { }
        bool IMyInput.IsGamepadKeyRightPressed() { return false; }
        bool IMyInput.IsGamepadKeyLeftPressed() { return false; }
        bool IMyInput.IsNewGamepadKeyDownPressed() { return false; }
        bool IMyInput.IsNewGamepadKeyUpPressed() { return false; }
        void IMyInput.GetActualJoystickState(StringBuilder text) { }

        bool IMyInput.IsAnyMouseOrJoystickPressed() { return false; }
        bool IMyInput.IsAnyNewMouseOrJoystickPressed() { return false; }
        bool IMyInput.IsNewPrimaryButtonPressed() { return false; }
        bool IMyInput.IsNewSecondaryButtonPressed() { return false; }
        bool IMyInput.IsNewPrimaryButtonReleased() { return false; }
        bool IMyInput.IsNewSecondaryButtonReleased() { return false; }
        bool IMyInput.IsPrimaryButtonReleased() { return false; }
        bool IMyInput.IsSecondaryButtonReleased() { return false; }
        bool IMyInput.IsPrimaryButtonPressed() { return false; }
        bool IMyInput.IsSecondaryButtonPressed() { return false; }
        bool IMyInput.IsNewButtonPressed(MySharedButtonsEnum button) { return false; }
        bool IMyInput.IsButtonPressed(MySharedButtonsEnum button) { return false; }
        bool IMyInput.IsNewButtonReleased(MySharedButtonsEnum button) { return false; }
        bool IMyInput.IsButtonReleased(MySharedButtonsEnum button) { return false; }

        bool IMyInput.IsNewGameControlPressed(MyStringId controlEnum) { return false; }
        bool IMyInput.IsGameControlPressed(MyStringId controlEnum) { return false; }
        bool IMyInput.IsNewGameControlReleased(MyStringId controlEnum) { return false; }
        float IMyInput.GetGameControlAnalogState(MyStringId controlEnum) { return 0; }
        bool IMyInput.IsGameControlReleased(MyStringId controlEnum) { return false; }
        bool IMyInput.IsKeyValid(MyKeys key) { return false; }
        bool IMyInput.IsKeyDigit(MyKeys key) { return false; }
        bool IMyInput.IsMouseButtonValid(MyMouseButtonsEnum button) { return false; }
        bool IMyInput.IsJoystickButtonValid(MyJoystickButtonsEnum button) { return false; }
        bool IMyInput.IsJoystickAxisValid(MyJoystickAxesEnum axis) { return false; }
        bool IMyInput.IsJoystickConnected() { return false; }
        bool IMyInput.JoystickAsMouse { get { return false; } set { } }
        bool IMyInput.IsJoystickLastUsed { get { return false; } }
        event Action<bool> IMyInput.JoystickConnected { add { } remove { } }
        MyControl IMyInput.GetControl(MyKeys key) { return null; }
        MyControl IMyInput.GetControl(MyMouseButtonsEnum button) { return null; }
        void IMyInput.GetListOfPressedKeys(List<MyKeys> keys) { }
        void IMyInput.GetListOfPressedMouseButtons(List<MyMouseButtonsEnum> result) { }
        DictionaryValuesReader<MyStringId, MyControl> IMyInput.GetGameControlsList() { return null; }
        void IMyInput.TakeSnapshot() { }
        void IMyInput.RevertChanges() { }
        MyControl IMyInput.GetGameControl(MyStringId controlEnum) { return m_nullControl; }

        void IMyInput.RevertToDefaultControls() { }
        void IMyInput.SaveControls(SerializableDictionary<string, object> controlsGeneral, SerializableDictionary<string, object> controlsButtons) { }
        bool IMyInput.ENABLE_DEVELOPER_KEYS { get { return false; } }
        Vector2 IMyInput.GetMouseAreaSize() { return Vector2.Zero; }
        string IMyInput.GetName(MyMouseButtonsEnum mouseButton) { return ""; }
        string IMyInput.GetName(MyJoystickButtonsEnum joystickButton) { return ""; }
        string IMyInput.GetName(MyJoystickAxesEnum joystickAxis) { return ""; }
        string IMyInput.GetUnassignedName() { return ""; }
        string IMyInput.GetKeyName(MyKeys key) { return ""; }
    }
}
