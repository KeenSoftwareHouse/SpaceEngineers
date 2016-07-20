using System;
using System.Collections.Generic;
using VRage.Collections;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyGridTerminalSystem
    {
        void GetBlocks(List<IMyTerminalBlock> blocks);
        void GetBlockGroups(List<IMyBlockGroup> blockGroups, Func<IMyBlockGroup, bool> collect = null);
        void GetBlocksOfType<T>(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null) where T: class;
        void GetBlocksOfType<T>(List<T> blocks, Func<T, bool> collect = null) where T: class;
        void SearchBlocksOfName(string name, List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null);
        IMyTerminalBlock GetBlockWithName(string name);
        IMyBlockGroup GetBlockGroupWithName(string name);
        IMyTerminalBlock GetBlockWithId(long id);
    }
}
