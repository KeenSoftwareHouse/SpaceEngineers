using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    /// <summary>
    /// State groups, used to limit bandwidth by group.
    /// </summary>
    public enum StateGroupEnum
    {
        PositionVerification,
        Terminal,
        Properties,   
        Inventory, 

        Physics,
       
        Streaming,
        FracturedPiece,

        FloatingObjectPhysics,
    }
}
