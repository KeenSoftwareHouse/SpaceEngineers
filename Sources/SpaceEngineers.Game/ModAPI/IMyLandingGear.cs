using Sandbox.ModAPI;
using System;

namespace SpaceEngineers.Game.ModAPI
{
    public interface IMyLandingGear: IMyFunctionalBlock, Ingame.IMyLandingGear
    {
        event Action<bool> StateChanged;
    }
}
