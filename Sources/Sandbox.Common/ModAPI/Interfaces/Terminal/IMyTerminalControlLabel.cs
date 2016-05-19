using VRage.Utils;

namespace Sandbox.ModAPI.Interfaces.Terminal
{
    /// <summary>
    /// This is a simple label control.  
    /// </summary>
    public interface IMyTerminalControlLabel : IMyTerminalControl
    {
        /// <summary>
        /// The text on the label
        /// </summary>
        MyStringId Label { get; set; }
    }
}
