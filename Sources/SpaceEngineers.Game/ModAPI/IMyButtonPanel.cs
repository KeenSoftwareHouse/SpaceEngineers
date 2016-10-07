using Sandbox.ModAPI;
using System;

namespace SpaceEngineers.Game.ModAPI
{
    public interface IMyButtonPanel : IMyTerminalBlock, Ingame.IMyButtonPanel
    {
        event Action<int> ButtonPressed;
    }
}