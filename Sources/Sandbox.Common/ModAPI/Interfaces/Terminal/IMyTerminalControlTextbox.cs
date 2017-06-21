using System;
using System.Text;
using VRage.Utils;

namespace Sandbox.ModAPI.Interfaces.Terminal
{
    /// <summary>
    /// This is a textbox where a user can enter values into.
    /// </summary>
    public interface IMyTerminalControlTextbox : IMyTerminalControl, IMyTerminalValueControl<StringBuilder>, IMyTerminalControlTitleTooltip
    {

    }
}
