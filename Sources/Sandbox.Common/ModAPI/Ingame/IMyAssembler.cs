using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Interface for the assembler block
    /// </summary>
    public interface IMyAssembler : IMyProductionBlock
    {
        /// <summary>
        /// True if assembler set to disassemble (read only)
        /// </summary>
        bool DisassembleEnabled { get; }

        /// <summary>
        /// Adds an item to the assembler queue
        /// </summary>
        /// <param name="itemType">type of item, i.e. 'MyObjectBuilder_Component'</param>
        /// <param name="subtypeName">Shortname of component, i.e. SteelPlate, Reactor, MetalGrid, etc.</param>
        /// <param name="amount">Quantity of specified item to add to queue</param>
        /// <param name="index">Index/Position item should be added to queue<br />-1 (default) adds to the end of queue<br />0 is first slot, 1 is second slot etc.</param>
        /// <returns>
        /// true - item added successfully
        /// false - item not added - probably because itemType or subtypeName are invalid
        /// </returns>
        bool AddQueueItem(string itemType, string subtypeName, int amount, int index = -1);

        /// <summary>
        /// Retrieves the number of items in the assembler queue
        /// </summary>
        /// <returns></returns>
        int GetQueueCount();

        /// <summary>
        /// Retrieves an item at index
        /// </summary>
        /// <param name="index"></param>
        /// <returns>IMyAssemblerQueueItem with details of queued item. Returns null if index is not valid.
        /// </returns>
        IMyAssemblerQueueItem GetQueueItemAt(int index);

        /// <summary>
        /// Removes a queue item at specific index
        /// </summary>
        /// <param name="idx"></param>
        void RemoveQueueItemAt(int index);

        /// <summary>
        /// Removes all items in the assembler queue
        /// </summary>
        void ClearQueue();

        /// <summary>
        /// Returns the total amount of an item in the queue
        /// </summary>
        /// <param name="itemType">the item type of item, i.e. MyObjectBuilder_Component</param>
        /// <param name="subtypeName">The subtype name of item to count, i.e. SteelPlate</param>
        /// <returns>int - amount queued in total</returns>
        int CountQueueItems(string itemType, string subtypeName);

        /// <summary>
        /// True if assembler is unable to acquire components/items to process first in queue.
        /// </summary>
        bool MissingItems { get; }
        
    }

    public interface IMyAssemblerQueueItem
    {
        int Amount { get; }
        string Type { get; }
        string SubtypeName { get; }
    }
}
