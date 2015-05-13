using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyMotorStator : IMyFunctionalBlock
    {
        /// <summary>
        /// Param - Limit is maximum
        /// </summary>
        event Action<bool> LimitReached;
    }
}
