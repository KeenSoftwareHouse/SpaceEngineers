using VRage.Utils;

namespace Sandbox.ModAPI.Interfaces.Terminal
{
    /// <summary>
    /// This is a base interface for other interfaces.  Almost all controls implement this, and allows you to set the label (Title) of the control and also
    /// the tooltip that appears when hovering over the control.
    /// </summary>
    public interface IMyTerminalControlTitleTooltip
    {
        /// <summary>
        /// Allows you to get or set the Label that appears on the control
        /// </summary>
        MyStringId Title { get; set; }
        /// <summary>
        /// Allows you to get or set the tooltip that appears when you hover over the control
        /// </summary>
        MyStringId Tooltip { get; set; }
    }
}
