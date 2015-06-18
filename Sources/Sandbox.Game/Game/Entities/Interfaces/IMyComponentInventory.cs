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
	public interface IMyComponentInventory
    {
        MyStringId InventoryName { get; }

        MyFixedPoint CurrentMass { get; }

        MyFixedPoint MaxMass { get; }

        MyFixedPoint CurrentVolume { get; }

        MyFixedPoint MaxVolume { get; }

        //TODO: This will be removed, this ownership will be determined by the entity which is holding this component
		Sandbox.Game.IMyInventoryOwner Owner { get; }

		MyFixedPoint ComputeAmountThatFits(MyDefinitionId contentId);

		MyFixedPoint GetItemAmount(MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None);

        /// <summary>
        /// Adds item to inventory
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="objectBuilder"></param>
        /// <param name="index"></param>
        /// <returns>true if items were added, false if items didn't fit</returns>
        bool AddItems(MyFixedPoint amount, MyObjectBuilder_Base objectBuilder, int index = -1);

		void RemoveItemsOfType(MyFixedPoint amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool spawn = false);

        /// <summary>
        /// Called when items were added or removed, or their amount has changed
        /// </summary>
        event Action<IMyComponentInventory> ContentsChanged;

        /// <summary>
        /// Called if this inventory changed it's owner
        /// </summary>
        event Action<IMyComponentInventory, IMyInventoryOwner> OwnerChanged;

        /// <summary>
        /// Get all items in the inventory
        /// </summary>
        /// <returns>items in the inventory or empty list</returns>
        List<IMyInventoryItem> GetItems();

        
    }
}
