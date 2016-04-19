using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.ObjectBuilders;

namespace VRage.Game.ModAPI
{
    public interface IMyInventory : Ingame.IMyInventory
    {
        bool Empty();

        void Clear(bool sync = true);

        void AddItems(VRage.MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder, int index = -1);

        void RemoveItemsOfType(VRage.MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder, bool spawn = false);
        void RemoveItemsOfType(VRage.MyFixedPoint amount, SerializableDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool spawn = false);
        void RemoveItemsAt(int itemIndex, VRage.MyFixedPoint? amount = null, bool sendEvent = true, bool spawn = false);
        void RemoveItems(uint itemId, VRage.MyFixedPoint? amount = null, bool sendEvent = true, bool spawn = false);

        bool TransferItemTo(VRage.Game.ModAPI.Ingame.IMyInventory dst, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, VRage.MyFixedPoint? amount = null, bool checkConnection = true);
        bool TransferItemFrom(VRage.Game.ModAPI.Ingame.IMyInventory sourceInventory, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, VRage.MyFixedPoint? amount = null, bool checkConnection = true);
    }
}
