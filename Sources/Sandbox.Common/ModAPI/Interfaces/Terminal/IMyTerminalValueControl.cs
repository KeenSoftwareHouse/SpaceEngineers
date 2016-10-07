using System;

namespace Sandbox.ModAPI.Interfaces.Terminal
{
    /// <summary>
    /// This is a value control interface that a few controls implement.  When a value changes, the Setter action is performed.  When a value is queried the Getter action
    /// is performed.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public interface IMyTerminalValueControl<TValue> : ITerminalProperty
    {
        /// <summary>
        /// This is triggered when the value of the control is required.
        /// </summary>
        Func<IMyTerminalBlock, TValue> Getter { get; set; }
        /// <summary>
        /// This is triggered when the value of the control is set by the user.  Depending on the control, this may be called a lot.
        /// </summary>
        Action<IMyTerminalBlock, TValue> Setter { get; set; }
    }
}
