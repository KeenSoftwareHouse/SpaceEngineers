using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyAssembler : IMyProductionBlock
    {
        /// <summary>
        /// Assembler set to disassemble
        /// </summary>
        /// <returns>true if set to disassemble</returns>
        bool DisassembleEnabled { get; }

        /// <summary>
        /// Gets a snapshot of the current assembler queue
        /// </summary>
        /// <returns>
        /// A list of queue items - (List<AssemblerQueueItem>)
        /// Modifying the returned list does not change the actual queue
        /// </returns>
        List<AssemblerQueueItem> GetQueueItems();

        /// <summary>
        /// Adds an item to the assembler queue
        /// </summary>
        /// <param name="itemType">type of item, i.e. 'MyObjectBuilder_Component'</param>
        /// <param name="subtypeName">Shortname of component, i.e. SteelPlate, Reactor, MetalGrid, etc.</param>
        /// <param name="amount">Quantity of specified item to add to queue</param>
        /// <returns>
        /// true - item added successfully
        /// false - item not added - probably because itemType or subtypeName are invalid
        /// </returns>
        bool AddQueueItem(string itemType, string subtypeName, int amount);

        /// <summary>
        /// Removes an item from queue. Items from GetQueueItems();
        /// </summary>
        /// <param name="queueItem">AssemblerQueueItem from list from GetQueueItems()</param>
        /// <returns>
        /// true - item removed
        /// false - item not removed
        /// </returns>
        bool RemoveQueueItem(AssemblerQueueItem queueItem);

        /// <summary>
        /// Removes all items in the assembler queue
        /// </summary>
        void ClearQueue();

        /// <summary>
        /// Returns the total amount of an item in the queue by subtype name
        /// </summary>
        /// <param name="subtypeName">The subtype name of item to count, i.e. SteelPlate</param>
        /// <returns>int - amount queued in total</returns>
        int GetQueueItemAmount(string subtypeName);
    }

    public struct AssemblerQueueItem
    {
        public int idx;
        public string itemType, subtypeName;
        public int amount;
    }
}
