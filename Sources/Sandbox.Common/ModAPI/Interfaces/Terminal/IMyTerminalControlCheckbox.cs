using VRage.Utils;

namespace Sandbox.ModAPI.Interfaces.Terminal
{
    /// <summary>
    /// This is a checkbox.  A label appears and a box appears next to it
    /// </summary>
    public interface IMyTerminalControlCheckbox : IMyTerminalControl, IMyTerminalValueControl<bool>, IMyTerminalControlTitleTooltip
    {
        /// <summary>
        /// The "on" label text
        /// </summary>
        MyStringId OnText { get; set; }
        /// <summary>
        /// The "off" label text
        /// </summary>
        MyStringId OffText { get; set; }
    }
}
