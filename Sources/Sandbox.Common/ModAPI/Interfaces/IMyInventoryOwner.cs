using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Interfaces
{
    public interface IMyInventoryOwner 
    {
        int InventoryCount { get; }
        IMyInventory GetInventory(int index);
        long EntityId { get; }
        bool UseConveyorSystem { get; set; }
    }
}
