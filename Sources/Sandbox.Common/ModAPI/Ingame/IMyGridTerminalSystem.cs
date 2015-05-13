using System;
using System.Collections.Generic;
using VRage.Collections;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyGridTerminalSystem
    {
        List<IMyTerminalBlock> Blocks{get;}
        List<IMyBlockGroup> BlockGroups { get; }
        void GetBlocksOfType<T>(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null);
        void SearchBlocksOfName(string name,List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null);
        IMyTerminalBlock GetBlockWithName(string name);
    }
}
