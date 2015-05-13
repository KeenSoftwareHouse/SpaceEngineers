using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyDoor : IMyFunctionalBlock
    {
        /// <summary>
        /// Param - opening
        /// </summary>
        event Action<bool> DoorStateChanged;
        bool Open { get;}
    }
}
