using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyAssembler : IMyProductionBlock
    {
        bool DisassembleEnabled { get; }
        bool QueueItem(string itemType, string subtypeName, int amount);
        void ClearQueue();
    }
}
