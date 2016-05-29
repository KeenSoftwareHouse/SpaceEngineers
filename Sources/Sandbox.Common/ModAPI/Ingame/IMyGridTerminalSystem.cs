using System;
using System.Collections.Generic;
using VRage.Collections;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyGridTerminalSystem
    {
        void GetBlocks(List<IMyTerminalBlock> blocks);
        List<IMyTerminalBlock> GetBlocks();
        void GetBlockGroups(List<IMyBlockGroup> blockGroups);
        List<IMyBlockGroup> GetBlockGroups();
        void GetBlocksOfType<T>(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null);
        void GetBlocksOfType<T>(List<T> blocks, Func<IMyTerminalBlock, bool> collect) where T : class;
        List<T> GetBlocksOfType<T>(Func<IMyTerminalBlock, bool> collect) where T : class;
        void SearchBlocksOfName(string name,List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null);
        List<IMyTerminalBlock> SearchBlocksOfName(string name, Func<IMyTerminalBlock, bool> collect = null);
        List<T> SearchBlocksOfTypeWithName<T>(string name, Func<IMyTerminalBlock, bool> collect = null) where T : class;
        IMyTerminalBlock GetBlockWithName(string name);
        T GetBlockOfTypeWithName<T>(string name) where T : class;
        IMyBlockGroup GetBlockGroupWithName(string name);
    }
}
