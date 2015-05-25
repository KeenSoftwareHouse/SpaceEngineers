using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace Sandbox.Game
{
    public partial struct MyInventoryItem : Sandbox.ModAPI.Interfaces.IMyInventoryItem
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

        Common.ObjectBuilders.MyObjectBuilder_PhysicalObject ModAPI.Interfaces.IMyInventoryItem.Content
        {
            get
            {
                return Content;
            }
            set
            {
                Content = value;
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

        float ModAPI.Interfaces.IMyInventoryItem.OxygenBottleLevel
        {
            get
            {
                if (this.Content.TypeId != typeof(MyObjectBuilder_OxygenContainerObject))
                    return -1;
                else
                    return (this.Content as MyObjectBuilder_OxygenContainerObject).OxygenLevel;
            }
        }
    }
}
