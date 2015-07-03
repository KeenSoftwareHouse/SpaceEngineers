using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Game
{
    public partial struct MyPhysicalInventoryItem : Sandbox.ModAPI.Interfaces.IMyInventoryItem
    {
        VRage.MyFixedPoint ModAPI.Interfaces.IMyInventoryItem.Amount
        {
            get
            {
                return Amount;
            }
            set
            {
                Amount = value;
            }
        }

        VRage.ObjectBuilders.MyObjectBuilder_Base ModAPI.Interfaces.IMyInventoryItem.Content
        {
            get
            {
                return Content;
            }
            set
            {
                Debug.Assert(value is Sandbox.Common.ObjectBuilders.MyObjectBuilder_PhysicalObject, "Content is not physical object!");
                Content = value as Sandbox.Common.ObjectBuilders.MyObjectBuilder_PhysicalObject;
            }
        }

        uint ModAPI.Interfaces.IMyInventoryItem.ItemId
        {
            get
            {
                return ItemId;
            }
            set
            {
                ItemId = value;
            }
        }
    }
}
