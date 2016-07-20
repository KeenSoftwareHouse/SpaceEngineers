using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage;

namespace Sandbox.ModAPI
{
    public interface IMyProductionBlock : IMyFunctionalBlock, ModAPI.Ingame.IMyProductionBlock
    {
        event Action StartedProducing;
        event Action StoppedProducing;

        /// <summary>
        /// Can this production block produce this blueprint?
        /// </summary>
        /// <param name="blueprint">A MyBlueprintDefinition that defines the blueprint</param>
        /// <returns></returns>
        bool CanUseBlueprint(MyDefinitionBase blueprint);
        /// <summary>
        /// Adds a blueprint to the production queue
        /// </summary>
        /// <param name="blueprint">A MyBlueprintDefinition that defines the blueprint</param>
        /// <param name="amount">Amount of items</param>
        void AddQueueItem(MyDefinitionBase blueprint, MyFixedPoint amount);
        /// <summary>
        /// Inserts a blueprint into the production queue
        /// </summary>
        /// <param name="idx">Index of the item</param>
        /// <param name="blueprint">A MyBlueprintDefinition that defines the blueprint</param>
        /// <param name="amount">Amount of items</param>
        void InsertQueueItem(int idx, MyDefinitionBase blueprint, MyFixedPoint amount);
        /// <summary>
        /// Gets the current production queue
        /// </summary>
        /// <returns>List of MyProductionQueueItems</returns>
        List<MyProductionQueueItem> GetQueue();
        /// <summary>
        /// Removes an item from the queue
        /// </summary>
        /// <param name="idx">Index of the item</param>
        /// <param name="amount">Amount to remove</param>
        void RemoveQueueItem(int idx, MyFixedPoint amount);
        /// <summary>
        /// Clears the Queue
        /// </summary>
        void ClearQueue();
    }

    public struct MyProductionQueueItem
    {
        public MyFixedPoint Amount;
        public MyDefinitionBase Blueprint;
        public uint ItemId;
    }
}
