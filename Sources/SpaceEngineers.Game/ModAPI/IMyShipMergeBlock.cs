using System;

namespace SpaceEngineers.Game.ModAPI
{
    public interface IMyShipMergeBlock:Ingame.IMyShipMergeBlock
    {
        event Action BeforeMerge;
    }
}
