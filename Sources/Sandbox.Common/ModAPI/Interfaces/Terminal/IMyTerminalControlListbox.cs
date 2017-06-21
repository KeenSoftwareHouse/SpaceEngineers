using System;
using System.Collections.Generic;
using VRage.Utils;
using VRage.ModAPI;

namespace Sandbox.ModAPI.Interfaces.Terminal
{
    /// <summary>
    /// This is a list box.  It contains a list of items that a user can select.
    /// </summary>
    public interface IMyTerminalControlListbox : IMyTerminalControl, IMyTerminalControlTitleTooltip
    {
        /// <summary>
        /// This allows you to enable/disable multiple item selection
        /// </summary>
        bool Multiselect { get; set; }
        /// <summary>
        /// This allows you to set how many rows are visible in the list box.
        /// </summary>
        int VisibleRowsCount { get; set; }
        /// <summary>
        /// This is triggered when you need to populate the list with list items.  The first list is the items in the list box, and the second list is 
        /// the selected items in the list.
        /// </summary>
        Action<IMyTerminalBlock, List<MyTerminalControlListBoxItem>, List<MyTerminalControlListBoxItem>> ListContent { set; }
        /// <summary>
        /// This is triggered when an item is selected.  Can contain more than one item if Multiselect is true.
        /// </summary>
        Action<IMyTerminalBlock, List<MyTerminalControlListBoxItem>> ItemSelected { set; }
    }

}
