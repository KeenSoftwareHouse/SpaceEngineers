using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Interfaces
{
    public interface IMyInventory
    {
        bool IsFull
        {
            get;
        }

        VRageMath.Vector3 Size
        {
            get;
        }

        VRage.MyFixedPoint CurrentMass
        {
            get;
        }

        VRage.MyFixedPoint MaxVolume
        {
            get;
        }

        VRage.MyFixedPoint CurrentVolume 
        {
            get;
        }


        bool IsItemAt(int position);

        bool CanItemsBeAdded(VRage.MyFixedPoint amount, Sandbox.Common.ObjectBuilders.Definitions.SerializableDefinitionId contentId);

        bool ContainItems(VRage.MyFixedPoint amount, Sandbox.Common.ObjectBuilders.MyObjectBuilder_PhysicalObject ob);
 
        VRage.MyFixedPoint GetItemAmount(Sandbox.Common.ObjectBuilders.Definitions.SerializableDefinitionId contentId, Sandbox.Common.ObjectBuilders.MyItemFlags flags = Sandbox.Common.ObjectBuilders.MyItemFlags.None);      

        bool TransferItemTo(IMyInventory dst, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, VRage.MyFixedPoint? amount = null);
        bool TransferItemFrom(IMyInventory sourceInventory, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, VRage.MyFixedPoint? amount = null);

        List<IMyInventoryItem> GetItems();
        IMyInventoryItem GetItemByID(uint id);
        IMyInventoryItem FindItem(Sandbox.Common.ObjectBuilders.Definitions.SerializableDefinitionId contentId);

        Sandbox.ModAPI.Interfaces.IMyInventoryOwner Owner
        {
            get;
        }
        bool IsConnectedTo(IMyInventory dst);

        /// <summary>
        /// Counts the amount of an item in inventory
        /// </summary>
        /// <param name="itemType">
        ///  The item type, i.e. MyObjectBuilder_Component
        ///  Can be part string i.e. Component, or empty string to search all
        ///  E.g. inventory.CountItems("Component","") will count all components
        /// </param>
        /// <param name="subtypeName">
        ///  The subtype name, i.e. SteelPlate, Medical, MetalGrid, etc.
        ///  Can also be part string, or empty string to search all
        ///  E.g. inventory.CountItems("","iron") will count all items with 'iron' in subtypeName
        /// </param>
        /// <returns>VRage.MyFixedPoint number of items found</returns>
        VRage.MyFixedPoint CountItems(string itemType, string subtypeName);
    }
}
