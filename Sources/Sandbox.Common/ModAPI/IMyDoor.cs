using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyDoor : Sandbox.ModAPI.Ingame.IMyDoor
    {
        event Action<bool> DoorStateChanged;
    }
}
