using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Sandbox.Game
{
    partial class MyInventory : IMyComponentInventory
    {
        IMyInventoryOwner IMyComponentInventory.Owner
        {
            get { return Owner; }
        }

        MyFixedPoint IMyComponentInventory.ComputeAmountThatFits(MyDefinitionId contentId)
        {
            return ComputeAmountThatFits(contentId);
        }

        MyFixedPoint IMyComponentInventory.GetItemAmount(MyDefinitionId contentId, MyItemFlags flags)
        {
            return GetItemAmount(contentId, flags);
        }

        bool IMyComponentInventory.AddItems(MyFixedPoint amount, MyObjectBuilder_Base objectBuilder, int index)
        {
            return AddItems(amount, objectBuilder, index);
        }

        void IMyComponentInventory.RemoveItemsOfType(MyFixedPoint amount, MyDefinitionId contentId, MyItemFlags flags, bool spawn)
        {
            RemoveItemsOfType(amount, contentId, flags, spawn);
        }

        event Action<IMyComponentInventory> IMyComponentInventory.ContentsChanged
        {
            add
            {
                ComponentContentsChanged += value;
            }
            remove
            {
                ComponentContentsChanged -= value;
            }
        }

        event Action<IMyComponentInventory, IMyInventoryOwner> IMyComponentInventory.OwnerChanged
        {
            add { OwnerChanged += value; }
            remove { OwnerChanged -= value; }
        }

        List<ModAPI.Interfaces.IMyInventoryItem> IMyComponentInventory.GetItems()
        {
            return GetItems().Cast<ModAPI.Interfaces.IMyInventoryItem>().ToList();
        }
        MyFixedPoint IMyComponentInventory.CurrentMass
        {
            get { return CurrentMass; }
        }

        MyFixedPoint IMyComponentInventory.MaxMass
        {
            get { return MaxMass; }
        }

        MyFixedPoint IMyComponentInventory.CurrentVolume
        {
            get { return CurrentVolume; }
        }

        MyFixedPoint IMyComponentInventory.MaxVolume
        {
            get { return MaxVolume; }
        }

        // This is not nice handling, but I didn't cleaner way..
        private void OnContentsChanged(MyInventory obj)
        {
            if (ComponentContentsChanged != null)
                ComponentContentsChanged(this);
        }

        public MyStringId InventoryName { get { return MyStringId.GetOrCompute("Default"); } }
    }
}
