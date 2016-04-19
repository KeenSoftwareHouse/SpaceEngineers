using Sandbox.ModAPI.Ingame;

namespace SpaceEngineers.Game.ModAPI.Ingame
{
    public interface IMyTimerBlock : IMyFunctionalBlock
    {
        bool IsCountingDown { get; }
        float TriggerDelay { get; }
    }
}
