using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyAssembler : IMyProductionBlock
    {
        bool DisassembleEnabled { get; }
        List<AssemblerQueueItem> GetQueueItems();
        bool AddQueueItem(string itemType, string subtypeName, int amount);
        bool RemoveQueueItem(AssemblerQueueItem queueItem);
        void ClearQueue();
    }
    public struct AssemblerQueueItem
    {
        public int idx;
        public string itemType, subtypeName;
        public int amount;
    }
}
