using Sandbox.ModAPI;
using System;

namespace SpaceEngineers.Game.ModAPI
{
    public interface IMyShipMergeBlock : IMyFunctionalBlock, Ingame.IMyShipMergeBlock
    {
        event Action BeforeMerge;
    }
}
