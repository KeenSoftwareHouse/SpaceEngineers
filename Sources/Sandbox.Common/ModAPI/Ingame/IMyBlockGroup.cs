using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyBlockGroup
    {
        void GetBlocks(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null);
        void GetBlocksOfType<T>(List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null) where T : class;
        void GetBlocksOfType<T>(List<T> blocks, Func<T, bool> collect = null) where T : class;

        string Name { get;}
    }
}
