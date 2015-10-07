using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.ComponentSystem;
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
        public MyInventoryBase(string inventoryId)
        {
            InventoryId = MyStringId.GetOrCompute(inventoryId);
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);

            var ob = builder as MyObjectBuilder_InventoryBase;
            InventoryId = MyStringId.GetOrCompute(ob.InventoryId ?? "Inventory");
        }

        public override MyObjectBuilder_ComponentBase Serialize()
        {
            var ob = base.Serialize() as MyObjectBuilder_InventoryBase;
            ob.InventoryId = InventoryId.ToString();

            return ob;
        }

        public override string ToString()
        {
            return base.ToString() + " - " + InventoryId.ToString();
        }

        protected static MySyncInventory SyncObject;

        /// <summary>
        /// This is for the purpose of identifying the inventory in aggregates (i.e. "Backpack", "LeftHand", ...)
        /// </summary>
        public MyStringId InventoryId { get; private set; }

        public abstract MyFixedPoint CurrentMass { get; }
        public abstract MyFixedPoint MaxMass { get; }

        public abstract MyFixedPoint CurrentVolume { get; }
        public abstract MyFixedPoint MaxVolume { get; }

        public abstract MyFixedPoint ComputeAmountThatFits(MyDefinitionId contentId);
        public abstract MyFixedPoint GetItemAmount(MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None);

        public abstract bool ItemsCanBeAdded(MyFixedPoint amount, IMyInventoryItem item);
        public abstract bool ItemsCanBeRemoved(MyFixedPoint amount, IMyInventoryItem item);

        public abstract bool Add(IMyInventoryItem item, MyFixedPoint amount, bool stack = true);
        public abstract bool Remove(IMyInventoryItem item, MyFixedPoint amount);

        public abstract void CountItems(Dictionary<MyDefinitionId, MyFixedPoint> itemCounts);
        public abstract void ApplyChanges(List<MyComponentChange> changes);

        public abstract List<MyPhysicalInventoryItem> GetItems();

        //TODO: This should be deprecated, we shoud add IMyInventoryItems objects only , instead of items based on their objectbuilder
        /// <summary>
        /// Adds item to inventory
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="objectBuilder"></param>
        /// <param name="index"></param>
        /// <returns>true if items were added, false if items didn't fit</returns>
        public abstract bool AddItems(MyFixedPoint amount, MyObjectBuilder_Base objectBuilder, int index = -1, bool stack = true);        

        /// <summary>
        /// Remove items of a given amount and definition
        /// </summary>
        /// <param name="amount">amount ot remove</param>
        /// <param name="contentId">definition id of items to be removed</param>
        /// <param name="spawn">Set tru to spawn object in the world, after it was removed</param>
        /// <returns>Returns the actually removed amount</returns>
        public abstract MyFixedPoint RemoveItemsOfType(MyFixedPoint amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool spawn = false);

        /// <summary>
        /// Transfers safely given item from one to another inventory, uses ItemsCanBeAdded and ItemsCanBeRemoved checks. If sourceInventory == destionationInventory, it splits the amount.
        /// </summary>
        /// <returns>true if items were succesfully transfered, otherwise, false</returns>
        public static bool TransferItems(MyInventoryBase sourceInventory, MyInventoryBase destinationInventory, IMyInventoryItem item, MyFixedPoint amount, bool stack)
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

            if ((destinationInventory.ItemsCanBeAdded(amount, item) || destinationInventory == sourceInventory ) && sourceInventory.ItemsCanBeRemoved(amount, item))
            {
                if (Sync.IsServer)
                {
                    if (destinationInventory != sourceInventory)
                    {
                        // try to add first and then remove to ensure this items don't disappear
                        if (destinationInventory.Add(item, amount, stack))
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
                        // same inventory transfer = splitting amount, need to remove first and add second
                        if (sourceInventory.Remove(item, amount) && destinationInventory.Add(item, amount, stack))
                        {
                            return true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Fail("Error! Unsuccesfull splitting!");
                        }
                    }
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
            if (RemoveEntityOnEmpty && GetItemsCount() == 0)
            {
                Container.Entity.Close();
            }
        }

        /// <summary>
        /// Returns the number of items in the inventory. This needs to be overrided, otherwise it returns 0!
        /// </summary>
        /// <returns>int - number of items in inventory</returns>
        virtual public int GetItemsCount()
        {
            return 0;
        }

        /// <summary>
        /// Called if this inventory changed its owner
        /// </summary>
        public event Action<MyInventoryBase, MyComponentContainer> OwnerChanged;
        /// <summary>
        /// Setting this flag to true causes to call Close() on the Entity of Container, when the GetItemsCount() == 0.
        /// This causes to remove entity from the world, when this inventory is empty.
        /// </summary>
        public bool RemoveEntityOnEmpty = false;

        protected void OnOwnerChanged()
        {
            var handler = OwnerChanged;
            if (handler != null)
                handler(this, Container);
        }

		public override bool IsSerialized()
		{
			return true;
		}

        public override string ComponentTypeDebugString
        {
            get { return "Inventory"; }
        }
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
