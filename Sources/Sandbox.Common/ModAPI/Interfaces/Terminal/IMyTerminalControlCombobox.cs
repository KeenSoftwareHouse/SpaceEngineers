using System;
using System.Collections.Generic;
using VRage.Utils;
using VRage.ModAPI;

namespace Sandbox.ModAPI.Interfaces.Terminal
{
    /// <summary>
    /// This is a combobox control.  It is a field that gives a drop down list that contains options that you can select.
    /// </summary>
    public interface IMyTerminalControlCombobox : IMyTerminalControl, IMyTerminalValueControl<long>, IMyTerminalControlTitleTooltip
    {
        /// <summary>
        /// This allows you to set the content of the combo box itself.
        /// </summary>
        Action<List<MyTerminalControlComboBoxItem>> ComboBoxContent { get; set; }
    }

}
