using VRage.Utils;

namespace Sandbox.ModAPI.Interfaces.Terminal
{
    /// <summary>
    /// This is an on / off switch or toggle button.  It gives two options that a user can toggled between.
    /// </summary>
    public interface IMyTerminalControlOnOffSwitch : IMyTerminalControl, IMyTerminalValueControl<bool>, IMyTerminalControlTitleTooltip
    {
        /// <summary>
        /// The label for the "on" switch
        /// </summary>
        MyStringId OnText { get; set; }
        /// <summary>
        /// The label for the "off" switch
        /// </summary>
        MyStringId OffText { get; set; }
    }
}
