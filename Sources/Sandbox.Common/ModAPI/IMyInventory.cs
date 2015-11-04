using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace Sandbox.ModAPI
{
    public interface IMyInventory : Sandbox.ModAPI.Interfaces.IMyInventory
    {
        bool Empty();

        void Clear(bool sync = true);

        void AddItems(VRage.MyFixedPoint amount, Sandbox.Common.ObjectBuilders.MyObjectBuilder_PhysicalObject objectBuilder, int index = -1);

        void RemoveItemsOfType(VRage.MyFixedPoint amount, Sandbox.Common.ObjectBuilders.MyObjectBuilder_PhysicalObject objectBuilder, bool spawn = false);
        void RemoveItemsOfType(VRage.MyFixedPoint amount, SerializableDefinitionId contentId, Sandbox.Common.ObjectBuilders.MyItemFlags flags = Sandbox.Common.ObjectBuilders.MyItemFlags.None, bool spawn = false);
        void RemoveItemsAt(int itemIndex, VRage.MyFixedPoint? amount = null, bool sendEvent = true, bool spawn = false);
        void RemoveItems(uint itemId, VRage.MyFixedPoint? amount = null, bool sendEvent = true, bool spawn = false);

        bool TransferItemTo(Sandbox.ModAPI.Interfaces.IMyInventory dst, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, VRage.MyFixedPoint? amount = null, bool checkConnection = true);
        bool TransferItemFrom(Sandbox.ModAPI.Interfaces.IMyInventory sourceInventory, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, VRage.MyFixedPoint? amount = null, bool checkConnection = true);
    }
}
