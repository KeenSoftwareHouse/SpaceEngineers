using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VRage.Input;
using VRage.Utils;

namespace Sandbox.ModAPI
{
    public class MyControlWrapper : IMyControl
    {
        private readonly MyControl control;

        public static IMyControl Get(MyControl control)
        {
            return (control == null ? null : new MyControlWrapper(control));
        }

        private MyControlWrapper(MyControl control)
        {
            this.control = control;
        }

        MyStringId IMyControl.ControlId
        {
            get { return control.GetGameControlEnum(); }
        }

        string IMyControl.Name
        {
            get { return control.GetControlName().ToString(); }
        }

        string IMyControl.Description
        {
            get { return control.GetControlDescription().GetValueOrDefault(MyStringId.NullOrEmpty).ToString(); }
        }

        MyGuiControlTypeEnum IMyControl.ControlType
        {
            get { return control.GetControlTypeEnum(); }
        }

        MyKeys IMyControl.AssignedKey1
        {
            get { return control.GetKeyboardControl(); }
        }

        MyKeys IMyControl.AssignedKey2
        {
            get { return control.GetSecondKeyboardControl(); }
        }

        MyMouseButtonsEnum IMyControl.AssignedMouseButton
        {
            get { return control.GetMouseControl(); }
        }

        bool IMyControl.IsAssigned()
        {
            return control.IsControlAssigned();
        }

        bool IMyControl.IsStatus(MyPressEnum check)
        {
            switch (check)
            {
                case MyPressEnum.PRESSED: return control.IsPressed();
                case MyPressEnum.WAS_PRESSED: return control.WasPressed();
                case MyPressEnum.JUST_PRESSED: return control.IsNewPressed();
                case MyPressEnum.JUST_RELEASED: return control.IsNewReleased();
            }

            return false;
        }
    }
}
