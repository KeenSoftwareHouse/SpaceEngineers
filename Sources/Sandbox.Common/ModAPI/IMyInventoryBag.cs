using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ModAPI;

namespace Sandbox.ModAPI
{
    /// <summary>
    /// Inventory bag spawned when character died, container breaks, or when entity from other inventory cannot be spawned then bag spawned with the item in its inventory.
    /// </summary>
    public interface IMyInventoryBag : IMyEntity
    {
    }
}
