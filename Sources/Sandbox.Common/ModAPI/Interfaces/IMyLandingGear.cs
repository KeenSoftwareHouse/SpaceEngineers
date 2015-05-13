using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyLandingGear : IMyFunctionalBlock
    {
        /// <summary>
        /// Param - locked
        /// </summary>
        event Action<bool> StateChanged;
    }
}
