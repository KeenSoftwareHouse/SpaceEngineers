using System;
using VRage.Utils;

namespace Sandbox.ModAPI.Interfaces.Terminal
{
    /// <summary>
    /// This is a control button.  When a button is clicked an action is performed.
    /// </summary>
    public interface IMyTerminalControlButton : IMyTerminalControl, IMyTerminalControlTitleTooltip
    {
        /// <summary>
        /// The action taken when a button is clicked
        /// </summary>
        Action<IMyTerminalBlock> Action { get; set; }
    }
}
