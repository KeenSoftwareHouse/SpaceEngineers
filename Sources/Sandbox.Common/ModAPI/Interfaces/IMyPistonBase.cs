using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyPistonBase : IMyFunctionalBlock
    {
        /// <summary>
        /// Param - limit is top
        /// </summary>
        event Action<bool> LimitReached;
    }
}
