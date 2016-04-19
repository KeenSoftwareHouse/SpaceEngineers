using Sandbox.ModAPI.Ingame;

namespace SpaceEngineers.Game.ModAPI.Ingame
{
    public interface IMyLargeConveyorTurretBase : IMyLargeTurretBase
    {
        bool UseConveyorSystem { get; }
    }
}
