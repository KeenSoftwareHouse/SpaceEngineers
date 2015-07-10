using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Components;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Sandbox.Game
{
    public abstract class MyInventoryBase : MyEntityComponentBase
    {

        protected static MySyncInventory SyncObject;

        /// <summary>
        /// This is for the purpose of identifying the inventory in aggregates (i.e. "Backpack", "LeftHand", ...)
        /// </summary>
        public abstract MyStringId InventoryId { get; }

        public abstract MyFixedPoint CurrentMass { get; }

        public abstract MyFixedPoint MaxMass { get; }

        public abstract MyFixedPoint CurrentVolume { get; }

        public abstract MyFixedPoint MaxVolume { get; }

        public abstract MyFixedPoint ComputeAmountThatFits(MyDefinitionId contentId);

        public abstract MyFixedPoint GetItemAmount(MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None);

        public abstract bool ItemsCanBeAdded(MyFixedPoint amount, IMyInventoryItem item);

        public abstract bool ItemsCanBeRemoved(MyFixedPoint amount, IMyInventoryItem item);

        public abstract bool Add(IMyInventoryItem item, MyFixedPoint amount);

        public abstract bool Remove(IMyInventoryItem item, MyFixedPoint amount);
                
        //TODO: This should be deprecated, we shoud add IMyInventoryItems objects only , instead of items based on their objectbuilder
        /// <summary>
        /// Adds item to inventory
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="objectBuilder"></param>
        /// <param name="index"></param>
        /// <returns>true if items were added, false if items didn't fit</returns>
        public abstract bool AddItems(MyFixedPoint amount, MyObjectBuilder_Base objectBuilder, int index = -1);        

        /// <summary>
        /// Remove items of a given amount and definition
        /// </summary>
        /// <param name="amount">amount ot remove</param>
        /// <param name="contentId">definition id of items to be removed</param>
        /// <param name="spawn">Set tru to spawn object in the world, after it was removed</param>
        /// <returns>true if the selected amount was removd</returns>
        public abstract bool RemoveItemsOfType(MyFixedPoint amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool spawn = false);


        /// <summary>
        /// Transfers safely given item from one to another inventory, uses ItemsCanBeAdded and ItemsCanBeRemoved checks
        /// </summary>
        /// <returns>true if items were succesfully transfered, otherwise, false</returns>
        public static bool TransferItems(MyInventoryBase sourceInventory, MyInventoryBase destinationInventory, IMyInventoryItem item, MyFixedPoint amount)
        {
            if (sourceInventory == null)
            {
                System.Diagnostics.Debug.Fail("Source inventory is null!");
                return false;
            }
            if (destinationInventory == null)
            {
                System.Diagnostics.Debug.Fail("Destionation inventory is null!");
                return false;
            }
            if (item == null)
            {
                System.Diagnostics.Debug.Fail("Item is null!");
                return false;
            }
            if (amount == 0)
            {
                return true;
            }


            if (destinationInventory.ItemsCanBeAdded(amount, item) && sourceInventory.ItemsCanBeRemoved(amount, item))
            {
                if (Sync.IsServer)
                {
                    if (destinationInventory.Add(item, amount))
                    {
                        if (sourceInventory.Remove(item, amount))
                        {
                            // successfull transaction
                            return true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Fail("Error! Items were added to inventory, but can't be removed!");
                        }
                    }
                }
                else
                {
                    MySyncInventory.SendTransferItemsMessage(sourceInventory, destinationInventory, item, amount);
                }
            }
            
            return false;
        }

        /// <summary>
        /// Called when items were added or removed, or their amount has changed
        /// </summary>
        public event Action<MyInventoryBase> ContentsChanged;
        public void OnContentsChanged()
        {
            var handler = ContentsChanged;
            if (handler != null)
                handler(this);
        }

        /// <summary>
        /// Called if this inventory changed its owner
        /// </summary>
        public event Action<MyInventoryBase, MyComponentContainer> OwnerChanged;
        protected void OnOwnerChanged()
        {
            var handler = OwnerChanged;
            if (handler != null)
                handler(this, Container);
        }

        /// <summary>
        /// Get all items in the inventory
        /// </summary>
        /// <returns>items in the inventory or empty list</returns>
        public abstract List<MyPhysicalInventoryItem> GetItems();

		public override bool IsSerialized()
		{
			return true;
		}

        // CH: TODO: New methods
        public abstract void CollectItems(Dictionary<MyDefinitionId, int> itemCounts);
    }

    public static class MyInventoryBaseExtension
    {
        static List<MyComponentBase> m_tmpList = new List<MyComponentBase>();

        public static MyInventoryBase GetInventory(this MyEntity entity, MyStringHash inventoryId)
        {
            MyInventoryBase inventory = null;

            if (entity is IMyInventoryOwner)
            {
                var inventoryOwner = entity as IMyInventoryOwner;
                for (int i = 0; i < inventoryOwner.InventoryCount; ++i)
                {
                    var iteratedInventory = inventoryOwner.GetInventory(i);                    
                    if (inventoryId.Equals(MyStringHash.GetOrCompute(iteratedInventory.InventoryId.ToString())))
                    {
                        return iteratedInventory;
                    }
                }
            }

            inventory = entity.Components.Get<MyInventoryBase>();
            if (inventory != null)
            {
                if (inventoryId.Equals(MyStringHash.GetOrCompute(inventory.InventoryId.ToString())))
                {
                    return inventory;
                }
            }

            if (inventory is MyInventoryAggregate)
            {
                var aggregate = inventory as MyInventoryAggregate;
                m_tmpList.Clear();
                aggregate.GetComponentsFlattened(m_tmpList);                
                foreach (var component in m_tmpList)
                {
                    var componentInventory = component as MyInventoryBase;
                    if (inventoryId.Equals(MyStringHash.GetOrCompute(componentInventory.InventoryId.ToString())))
                    {
                        return componentInventory;
                    }
                }
            }

            return null;
        }

    }
}
