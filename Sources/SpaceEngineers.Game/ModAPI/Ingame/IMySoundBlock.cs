using Sandbox.ModAPI.Ingame;

namespace SpaceEngineers.Game.ModAPI.Ingame
{
    public interface IMySoundBlock : IMyFunctionalBlock
    {
        float Volume { get; }
        float Range { get; }
        bool IsSoundSelected{ get; }
        float LoopPeriod { get; }
    }
}
