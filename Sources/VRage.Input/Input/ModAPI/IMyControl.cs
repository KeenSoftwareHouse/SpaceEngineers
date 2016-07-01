using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Input;
using VRage.Utils;

namespace VRage.ModAPI
{
    public interface IMyControl
    {
        MyKeys GetKeyboardControl();

        MyKeys GetSecondKeyboardControl();

        MyMouseButtonsEnum GetMouseControl();

        bool IsPressed();

        bool IsNewPressed();

        bool IsNewReleased();

        bool IsJoystickPressed();

        bool IsNewJoystickPressed();

        bool IsNewJoystickReleased();

        /// <summary>
        /// Return the analog state between 0 (not pressed at all) and 1 (fully pressed).
        /// If a digital button is mapped to an analog control, it can return only 0 or 1.
        /// </summary>
        float GetAnalogState();

        MyStringId GetControlName();

        MyStringId? GetControlDescription();

        MyGuiControlTypeEnum GetControlTypeEnum();

        MyStringId GetGameControlEnum();

        bool IsControlAssigned();

        string GetControlButtonName(MyGuiInputDeviceEnum deviceType);

    }
}
