using System;

namespace SpaceEngineers.Game.ModAPI
{
    public interface IMyLandingGear: Ingame.IMyLandingGear
    {
        event Action<bool> StateChanged;
    }
}
