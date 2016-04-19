using Sandbox.ModAPI.Ingame;

namespace SpaceEngineers.Game.ModAPI.Ingame
{
    public interface IMyButtonPanel : IMyTerminalBlock
    {
        bool AnyoneCanUse
        {
            get;
        }
    }
}
