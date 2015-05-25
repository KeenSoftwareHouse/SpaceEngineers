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

        /// <summary>
        /// Gets the level of an oxygen bottle
        /// 0-1 representing % full, i.e. 0.5 = 50%
        /// -1 if not an oxygen bottle
        /// </summary>
        float OxygenBottleLevel { get; }

    }
}
