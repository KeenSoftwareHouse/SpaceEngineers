using System;
using System.Collections.Generic;
using VRage.Collections;

namespace Sandbox.ModAPI
{
    public interface IMyGridTerminalSystem : Ingame.IMyGridTerminalSystem
    {
        void GetBlocks(List<IMyTerminalBlock> blocks);
        void GetBlockGroups(List<IMyBlockGroup> blockGroups);
        void GetBlocksOfType<T>(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null);
        void SearchBlocksOfName(string name,List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null);
        new IMyTerminalBlock GetBlockWithName(string name);
        new IMyBlockGroup GetBlockGroupWithName(string name);
    }
}
