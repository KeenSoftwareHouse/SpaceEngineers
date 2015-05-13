using System;
using System.Collections.Generic;
using VRage.Collections;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyGridTerminalSystem
    {
        HashSet<IMyTerminalBlock> Blocks{get;}
        List<IMyBlockGroup> BlockGroups { get; }
        void GetBlocksOfType<T>(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null);
    }
}
