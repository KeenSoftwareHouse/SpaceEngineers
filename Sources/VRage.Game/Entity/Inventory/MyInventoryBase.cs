using VRage.Game.ObjectBuilders.ComponentSystem;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace VRage.Game.Entity
{
    [MyComponentType(typeof(MyInventoryBase))]
    [StaticEventOwner]
    public abstract class MyInventoryBase : MyEntityComponentBase, IMyEventProxy
    {               
        /// <summary>
        /// Setting this flag to true causes to call Close() on the Entity of Container, when the GetItemsCount() == 0.
        /// This causes to remove entity from the world, when this inventory is empty.
        /// </summary>
        public bool RemoveEntityOnEmpty = false;

        /// <summary>
        /// This is for the purpose of identifying the inventory in aggregates (i.e. "Backpack", "LeftHand", ...)
        /// </summary>
        public MyStringHash InventoryId { get; private set; }

        public abstract MyFixedPoint CurrentMass { get; }
        public abstract MyFixedPoint MaxMass { get; }
        public abstract int MaxItemCount { get; }

        public abstract MyFixedPoint CurrentVolume { get; }
        public abstract MyFixedPoint MaxVolume { get; }

        public abstract float? ForcedPriority { get; set; }
        public override string ComponentTypeDebugString { get { return "Inventory"; } }

        /// <summary>
        /// Called when items were added or removed, or their amount has changed
        /// </summary>
        public event Action<MyInventoryBase> ContentsChanged;
        public event Action<MyInventoryBase> BeforeContentsChanged;

        /// <summary>
        /// Called if this inventory changed its owner
        /// </summary>
        public event Action<MyInventoryBase, MyComponentContainer> OwnerChanged;

        public MyInventoryBase(string inventoryId)
        {
            InventoryId = MyStringHash.GetOrCompute(inventoryId);
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);

            var ob = builder as MyObjectBuilder_InventoryBase;
            InventoryId = MyStringHash.GetOrCompute(ob.InventoryId ?? "Inventory");
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var ob = base.Serialize() as MyObjectBuilder_InventoryBase;
            ob.InventoryId = InventoryId.ToString();

            return ob;
        }

        public override string ToString()
        {
            return base.ToString() + " - " + InventoryId.ToString();
        }

        public abstract MyFixedPoint ComputeAmountThatFits(MyDefinitionId contentId, float volumeRemoved = 0, float massRemoved = 0);
        public abstract MyFixedPoint GetItemAmount(MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool substitute = false);

        public abstract bool ItemsCanBeAdded(MyFixedPoint amount, IMyInventoryItem item);
        public abstract bool ItemsCanBeRemoved(MyFixedPoint amount, IMyInventoryItem item);

        public abstract bool Add(IMyInventoryItem item, MyFixedPoint amount);
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
        public abstract bool AddItems(MyFixedPoint amount, MyObjectBuilder_Base objectBuilder); 

        /// <summary>
        /// Remove items of a given amount and definition
        /// </summary>
        /// <param name="amount">amount ot remove</param>
        /// <param name="contentId">definition id of items to be removed</param>
        /// <param name="spawn">Set tru to spawn object in the world, after it was removed</param>
        /// <returns>Returns the actually removed amount</returns>
        public abstract MyFixedPoint RemoveItemsOfType(MyFixedPoint amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool spawn = false);

        /// <summary>
        /// Transfers safely given item from inventory given as parameter to this instance.
        /// </summary>
        /// <returns>true if items were succesfully transfered, otherwise, false</returns>
        public abstract bool TransferItemsFrom(MyInventoryBase sourceInventory, IMyInventoryItem item, MyFixedPoint amount);

        public abstract void OnContentsChanged();

        public abstract void OnBeforeContentsChanged();

        /// <summary>
        /// Returns the number of items in the inventory. This needs to be overrided, otherwise it returns 0!
        /// </summary>
        /// <returns>int - number of items in inventory</returns>
        public virtual int GetItemsCount()
        {
            return 0;
        }

        /// <summary>
        /// Returns number of embedded inventories - this inventory can be aggregation of other inventories.
        /// </summary>
        /// <returns>Return one for simple inventory, different number when this instance is an aggregation.</returns>
        public abstract int GetInventoryCount();

        /// <summary>
        /// Search for inventory having given search index. 
        /// Aggregate inventory: Iterates through aggregate inventory until simple inventory with matching index is found.
        /// Simple inventory: Returns itself if currentIndex == searchIndex.
        /// 
        /// Usage: searchIndex = index of inventory being searched, leave currentIndex = 0.
        /// </summary>
        public abstract MyInventoryBase IterateInventory(int searchIndex, int currentIndex = 0);

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

        public abstract void ConsumeItem(MyDefinitionId itemId, MyFixedPoint amount, long consumerEntityId = 0);

        public void RaiseContentsChanged()
        {
            if (ContentsChanged != null)
                ContentsChanged(this);
        }

        public void RaiseBeforeContentsChanged()
        {
            if (BeforeContentsChanged != null)
                BeforeContentsChanged(this);
        }
    }
}
