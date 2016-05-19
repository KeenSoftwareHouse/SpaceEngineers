using VRage.Utils;
using VRageMath;

namespace Sandbox.ModAPI.Interfaces.Terminal
{
    /// <summary>
    /// This is a color control.  This terminal controls allows you to select colors. 
    /// </summary>
    public interface IMyTerminalControlColor : IMyTerminalControl, IMyTerminalValueControl<Color>, IMyTerminalControlTitleTooltip
    {

    }
}
