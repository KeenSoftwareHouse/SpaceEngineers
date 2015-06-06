using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.ModAPI
{
    public interface IMyControl
    {
        /// <summary>
        /// The control ID.
        /// </summary>
        MyStringId ControlId { get; }

        /// <summary>
        /// The control's friendly name, never null.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The control description, can be empty but never null.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The category where this control sits in the options menu.
        /// </summary>
        MyGuiControlTypeEnum ControlType { get; }

        MyKeys AssignedKey1 { get; }

        MyKeys AssignedKey2 { get; }

        MyMouseButtonsEnum AssignedMouseButton { get; }

        /// <summary>
        /// If this control is asigned to any device.
        /// </summary>
        /// <returns></returns>
        bool IsAssigned();

        /// <summary>
        /// Get the control status, default is pressed.
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        bool IsStatus(MyPressEnum check = MyPressEnum.PRESSED);
    }
}
