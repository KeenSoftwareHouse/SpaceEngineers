using System;

namespace SpaceEngineers.Game.ModAPI
{
    public interface IMyButtonPanel : Ingame.IMyButtonPanel
    {
        event Action<int> ButtonPressed;
    }
}