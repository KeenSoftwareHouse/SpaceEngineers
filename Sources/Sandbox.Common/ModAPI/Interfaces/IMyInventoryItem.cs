using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Interfaces
{
    public interface IMyInventoryItem
    {
        VRage.MyFixedPoint Amount
        {
            get;
            set;
        }

        Sandbox.Common.ObjectBuilders.MyObjectBuilder_PhysicalObject Content
        {
            get;
            set;
        }

        uint ItemId
        {
            get;
            set;
        }

    }
}
