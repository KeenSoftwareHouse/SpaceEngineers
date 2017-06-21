using Sandbox.ModAPI.Ingame;

namespace SpaceEngineers.Game.ModAPI.Ingame
{
    public interface IMyOxygenFarm : IMyTerminalBlock
    {
        float GetOutput();
    }
}
