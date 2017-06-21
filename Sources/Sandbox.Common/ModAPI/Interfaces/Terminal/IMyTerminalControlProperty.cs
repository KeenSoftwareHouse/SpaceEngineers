namespace Sandbox.ModAPI.Interfaces.Terminal
{
    /// <summary>
    /// This is a control property.  This is use to set properties on a block that can be referenced in the ProgrammableBlock.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public interface IMyTerminalControlProperty<TValue> : IMyTerminalControl, IMyTerminalValueControl<TValue>
    {

    }
}
