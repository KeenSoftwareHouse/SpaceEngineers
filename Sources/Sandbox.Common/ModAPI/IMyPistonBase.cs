using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyPistonBase : Sandbox.ModAPI.Ingame.IMyPistonBase
    {
        event Action<bool> LimitReached;
    }
}
