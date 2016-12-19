using Sandbox.ModAPI;
using System;
using VRage.ModAPI;

namespace SpaceEngineers.Game.ModAPI
{
    public interface IMyLandingGear: IMyFunctionalBlock, Ingame.IMyLandingGear
    {
        event Action<bool> StateChanged;
        IMyEntity GetAttachedEntity();
    }
}
