using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
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

        /// <summary>
        /// Adds item to inventory
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="objectBuilder"></param>
        /// <param name="index"></param>
        /// <returns>true if items were added, false if items didn't fit</returns>
        public abstract bool AddItems(MyFixedPoint amount, MyObjectBuilder_Base objectBuilder, int index = -1);

        public abstract void RemoveItemsOfType(MyFixedPoint amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool spawn = false);

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
    }
}
