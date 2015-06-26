using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

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

        bool CanItemsBeAdded(VRage.MyFixedPoint amount, SerializableDefinitionId contentId);

        bool ContainItems(VRage.MyFixedPoint amount, Sandbox.Common.ObjectBuilders.MyObjectBuilder_PhysicalObject ob);
 
        VRage.MyFixedPoint GetItemAmount(SerializableDefinitionId contentId, Sandbox.Common.ObjectBuilders.MyItemFlags flags = Sandbox.Common.ObjectBuilders.MyItemFlags.None);      

        bool TransferItemTo(IMyInventory dst, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, VRage.MyFixedPoint? amount = null);
        bool TransferItemFrom(IMyInventory sourceInventory, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, VRage.MyFixedPoint? amount = null);

        List<IMyInventoryItem> GetItems();
        IMyInventoryItem GetItemByID(uint id);
        IMyInventoryItem FindItem(SerializableDefinitionId contentId);

        Sandbox.ModAPI.Interfaces.IMyInventoryOwner Owner
        {
            get;
        }
        bool IsConnectedTo(IMyInventory dst);
    }
}
