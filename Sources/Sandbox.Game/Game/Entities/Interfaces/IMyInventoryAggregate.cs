using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;

namespace Sandbox.Game.Entities.Interfaces
{
    public interface IMyInventoryAggregate : IMyComponentInventory
    {
        /// <summary>
        /// return all inventories wich have the same owner as the aggregate
        /// </summary>
        List<IMyComponentInventory> OwnerInventories { get; }

        /// <summary>
        /// return all inventories in this aggregate, including the areainventories available to the entity etc.
        /// </summary>
        List<IMyComponentInventory> Inventories { get; }

        /// <summary>
        /// Use to get the inventory by the string identification
        /// </summary>
        /// <param name="id">stringid identifiyng the inventory</param>
        /// <returns>null or the inventory</returns>
        IMyComponentInventory GetInventoryTypeId(MyStringId id);
        
        /// <summary>
        /// Adds inventory to the list of inventories or the another child aggregate
        /// </summary>
        /// <param name="Inventory"></param>
        void AddInventory(IMyComponentInventory inventory);

        /// <summary>
        /// Remove the inventory from the list of inventories or from the child aggregate
        /// </summary>
        /// <param name="Inventory"></param>
        void RemoveInventory(IMyComponentInventory inventory);
    }
}
