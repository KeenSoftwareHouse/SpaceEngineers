using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Sandbox.ModAPI.Interfaces
{
    public interface IMyOxygenBottle
    {
        /// <summary>
        /// Level of oxygen in the bottle as float 0-1.
        /// I.e. 0.5 = 50%
        /// </summary>
        /// <example>
        ///    var OxyGen = GridTerminalSystem.GetBlockWithName("Oxygen Generator"); 
        ///    var inv = OxyGen.GetInventory(0); 
        ///    var items = inv.GetItems(); 
        ///    items.ForEach(item => 
        ///    { 
        ///        if (item.Content is IMyOxygenBottle) 
        ///            Echo("OxyLevel:" + (item.Content as IMyOxygenBottle).OxygenLevel); 
        ///    }); 
        /// </example>
        float OxygenLevel { get; }
    }
}
