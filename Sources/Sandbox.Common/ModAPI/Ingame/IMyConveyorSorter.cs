using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Determines the current mode of a conveyor sorter.
    /// </summary>
    public enum MyConveyorSorterMode
    {
        /// <summary>
        /// The items in the filter list are the only items allowed through this sorter.
        /// </summary>
        Whitelist,

        /// <summary>
        /// The items in the filter list are not allowed through this sorter.
        /// </summary>
        Blacklist
    }

    [Serializable]
    public struct MyInventoryItemFilter
    {
        public static implicit operator MyInventoryItemFilter(MyDefinitionId definitionId)
        {
            return new MyInventoryItemFilter(definitionId);
        }

        /// <summary>
        /// Determines whether all subtypes of the given item ID should pass this filter check.
        /// </summary>
        public readonly bool AllSubTypes;

        /// <summary>
        /// Specifies an item to filter. Set <see cref="AllSubTypes"/> to true to only check the main type part of this ID.
        /// </summary>
        public readonly MyDefinitionId ItemId;

        public MyInventoryItemFilter(string itemId, bool allSubTypes = false) : this()
        {
            ItemId = MyDefinitionId.Parse(itemId);
            AllSubTypes = allSubTypes;
        }

        public MyInventoryItemFilter(MyDefinitionId itemId, bool allSubTypes = false) : this()
        {
            ItemId = itemId;
            AllSubTypes = allSubTypes;
        }
    }

    public interface IMyConveyorSorter : IMyFunctionalBlock
    {
        /// <summary>
        /// Determines whether the sorter should drain any inventories connected to it and push them to the other side - as long
        /// as the items passes the filtering as defined by the filter list (<see cref="GetFilterList"/>) and <see cref="Mode"/>.
        /// </summary>
        bool DrainAll { get; set; }

        /// <summary>
        /// Determines the current mode of this sorter. Use <see cref="SetWhitelist"/> or <see cref="SetBlacklist"/> to change the mode.
        /// </summary>
        MyConveyorSorterMode Mode { get; }

        /// <summary>
        /// Gets the items currently being allowed through or rejected, depending on the <see cref="Mode"/>.
        /// </summary>
        /// <param name="items"></param>
        void GetFilterList(List<MyInventoryItemFilter> items);

        /// <summary>
        /// Adds a single item to the filter list. See <see cref="SetFilter"/> to change the filter mode and/or fill
        /// the entire list in one go.
        /// </summary>
        /// <param name="item"></param>
        void AddItem(MyInventoryItemFilter item);

        /// <summary>
        /// Removes a single item from the filter list. See <see cref="SetFilter"/> to change the filter mode and/or clear
        /// the entire list in one go.
        /// </summary>
        /// <param name="item"></param>
        void RemoveItem(MyInventoryItemFilter item);

        /// <summary>
        /// Determines whether a given item type is allowed through the sorter, depending on the filter list (<see cref="GetFilterList"/>) and <see cref="Mode"/>.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        bool IsAllowed(MyDefinitionId id);

        /// <summary>
        /// Changes the sorter to desired mode and filters the provided items. You can pass in <c>null</c> to empty the list.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="items"></param>
        void SetFilter(MyConveyorSorterMode mode, List<MyInventoryItemFilter> items);
    }
}
