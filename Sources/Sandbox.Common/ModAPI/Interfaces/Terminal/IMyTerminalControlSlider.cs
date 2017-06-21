using System;
using System.Text;
using VRage.Utils;

namespace Sandbox.ModAPI.Interfaces.Terminal
{
    /// <summary>
    /// This is a value slider.  A slider can be moved by a user and a value selected.  
    /// </summary>
    public interface IMyTerminalControlSlider : IMyTerminalControl, IMyTerminalValueControl<float>, IMyTerminalControlTitleTooltip
    {
        /// <summary>
        /// Allows you to set the upper and lower limits of the slider
        /// </summary>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        void SetLimits(float min, float max);
        /// <summary>
        /// Allows you to set the upper and lower limits of the slider, interpolating on a logarithmic scale
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        void SetLogLimits(float min, float max);
        /// <summary>
        /// Allows you to set the upper and lower limits of the slider, interpolating on a logarithmic scale at both ends
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        void SetDualLogLimits(float absMin, float absMax, float centerBand);
        /// <summary>
        /// Allows you to set the upper and lower limits of the slider with delegates
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        void SetLimits(Func<IMyTerminalBlock, float> minGetter, Func<IMyTerminalBlock, float> maxGetter);
        /// <summary>
        /// Allows you to set the upper and lower limits of the slider with delegates, interpolating on a logarithmic scale
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        void SetLogLimits(Func<IMyTerminalBlock, float> minGetter, Func<IMyTerminalBlock, float> maxGetter);
        /// <summary>
        /// Allows you to set the upper and lower limits of the slider with delegates, interpolating on a logarithmic scale at both ends
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        void SetDualLogLimits(Func<IMyTerminalBlock, float> minGetter, Func<IMyTerminalBlock, float> maxGetter, float centerBand);
        /// <summary>
        /// This is triggered when a slider value is modified.  Appending to the stringbuilder allows you to modify the text that is displayed along
        /// side the slider when it updates.
        /// </summary>
        Action<IMyTerminalBlock, StringBuilder> Writer { get; set; }
    }
}
